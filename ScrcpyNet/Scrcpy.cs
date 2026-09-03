using FFmpeg.AutoGen;
using Serilog;
using SharpAdbClient;
using System;
using System.Buffers;
using System.Buffers.Binary;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;

namespace ScrcpyNet
{
    public class Scrcpy
    {
        public string DeviceName { get; private set; } = "";
        public int Width { get; internal set; }
        public int Height { get; internal set; }
        public long Bitrate { get; set; } = 8000000;
        public string ScrcpyServerFile { get; set; } = "ScrcpyNet/scrcpy-server.jar";

        public bool Connected { get; private set; }
        public VideoStreamDecoder VideoStreamDecoder { get; }

        private Thread? videoThread;
        private Thread? controlThread;
        private Thread? bufferThread;
        private TcpClient? videoClient;
        private TcpClient? controlClient;
        private TcpListener? listener;
        private CancellationTokenSource? cts;

        private readonly AdbClient adb;
        private readonly DeviceData device;
        private readonly Channel<IControlMessage> controlChannel = Channel.CreateUnbounded<IControlMessage>();
        // Bounded channel — nếu decoder chậm hơn producer (multi-view 4-8 tile),
        // mỗi packet giữ buffer H264 vài KB-vài MB cloned → tích vô hạn = 24GB RAM sau vài giờ.
        // Dùng FullMode=Wait + check count ở producer side để có thể av_packet_free packet bị drop
        // (DropOldest sẽ "nuốt" packet → không có chỗ free → vẫn leak).
        private const int BufferChannelCapacity = 30;
        private readonly Channel<IntPtr> bufferChannel = Channel.CreateBounded<IntPtr>(new BoundedChannelOptions(BufferChannelCapacity)
        {
            FullMode = BoundedChannelFullMode.Wait,
            SingleReader = true,
            SingleWriter = true,
        });
        private static readonly ArrayPool<byte> pool = ArrayPool<byte>.Shared;
        private static readonly ILogger log = Log.ForContext<VideoStreamDecoder>();
        private int port = 27183;

        public Scrcpy(DeviceData device, int port, VideoStreamDecoder? videoStreamDecoder = null)
        {
            adb = new AdbClient();
            this.device = device;
            this.port = port;
            VideoStreamDecoder = videoStreamDecoder ?? new VideoStreamDecoder();
            VideoStreamDecoder.Scrcpy = this;
        }

        //public void SetDecoder(VideoStreamDecoder videoStreamDecoder)
        //{
        //    this.videoStreamDecoder = videoStreamDecoder;
        //    this.videoStreamDecoder.Scrcpy = this;
        //}

        public void Start(long timeoutMs = 5000)
        {
            if (Connected)
                throw new Exception("Already connected.");

            bool started = false;
            try
            {
                MobileServerSetup();

                listener = new TcpListener(IPAddress.Loopback, this.port);
                listener.Start();

                MobileServerStart();

                int waitTimeMs = 0;
                while (!listener.Pending())
                {
                    Thread.Sleep(10);
                    waitTimeMs += 10;

                    if (waitTimeMs > timeoutMs)
                        throw new Exception($"[{device.Serial}] Timeout while waiting for server to connect on port {this.port}.");
                }

                videoClient = listener.AcceptTcpClient();
                log.Information("[{Serial}] Video socket connected (port {Port}).", device.Serial, this.port);

                // Wait briefly for the second (control) socket — server opens them sequentially
                int ctrlWait = 0;
                while (!listener.Pending() && ctrlWait < 2000)
                {
                    Thread.Sleep(10);
                    ctrlWait += 10;
                }
                if (!listener.Pending())
                    throw new Exception($"[{device.Serial}] Server is not sending a second connection request. Is 'control' disabled?");

                controlClient = listener.AcceptTcpClient();
                log.Information("[{Serial}] Control socket connected.", device.Serial);

                ReadDeviceInfo();

                cts = new CancellationTokenSource();

                bufferThread = new Thread(BufferMain) { Name = $"ScrcpyNet Buffer {device.Serial}", IsBackground = true };
                bufferThread.Start();
                videoThread = new Thread(VideoMain) { Name = $"ScrcpyNet Video {device.Serial}", IsBackground = true };
                controlThread = new Thread(ControllerMain) { Name = $"ScrcpyNet Controller {device.Serial}", IsBackground = true };

                videoThread.Start();
                controlThread.Start();

                Connected = true;
                started = true;
            }
            finally
            {
                // ADB reverse not needed once both TCP sockets are established.
                // Also cleanup khi Start() fail giữa chừng để tránh leak reverse forward + listener.
                try { MobileServerCleanup(); } catch { }
                if (!started)
                {
                    try { listener?.Stop(); } catch { }
                    try { videoClient?.Close(); } catch { }
                    try { controlClient?.Close(); } catch { }
                    listener = null;
                    videoClient = null;
                    controlClient = null;
                }
            }
        }

        private void UpdatePort()
        {
            //check port ,more deivce can connect
            for (int i = this.port; i <= 65535; i++)
            {
                if (!PortInUse(i))
                {
                    this.port = i;
                    return;
                }
            }
            //This is a nonsense
            throw new Exception("No port can use");
        }

        private bool PortInUse(int port)
        {
            var ipPorperties = IPGlobalProperties.GetIPGlobalProperties();
            var ipEndPoints = ipPorperties.GetActiveTcpListeners();
            var first = ipEndPoints.FirstOrDefault(i => i.Port == port);
            if (first != null)
                return true;

            ipEndPoints = ipPorperties.GetActiveUdpListeners();
            first = ipEndPoints.FirstOrDefault(i => i.Port == port);
            return first != null;

        }

        private async void BufferMain()
        {
            if (this.cts == null) return;
            try
            {
                await foreach (var item in this.bufferChannel.Reader.ReadAllAsync(this.cts.Token))
                {
                    if (item == IntPtr.Zero) continue;
                    try
                    {
                        this.VideoStreamDecoder?.DecodePacket(item);
                    }
                    catch (Exception ex)
                    {
                        // DecodePacket luôn free packet trong finally (kể cả khi throw).
                        // Không free lại ở đây vì item vẫn trỏ đến địa chỉ đã bị free → double-free.
                        log.Error(ex, "[{Serial}] DecodePacket threw — packet already freed by DecodePacket finally", device.Serial);
                    }
                }
            }
            catch (OperationCanceledException) { }
            finally
            {
                // Drain channel khi shutdown — mọi packet còn lại phải được free
                DrainBufferChannel();
            }
        }

        private void DrainBufferChannel()
        {
            while (this.bufferChannel.Reader.TryRead(out var leftover))
            {
                if (leftover == IntPtr.Zero) continue;
                unsafe
                {
                    AVPacket* p = (AVPacket*)leftover;
                    try { ffmpeg.av_packet_free(&p); } catch { }
                }
            }
        }

        public void Stop()
        {
            // Make Stop idempotent
            if (cts == null && !Connected)
                return;

            try
            {
                try { cts?.Cancel(); } catch { }

                // Complete writer để BufferMain thoát await foreach + drain channel free các packet còn lại
                try { bufferChannel.Writer.TryComplete(); } catch { }

                // Đóng socket trước để các Read/Write đang block bị wake-up
                try { videoClient?.Close(); } catch { }
                try { controlClient?.Close(); } catch { }
                try { listener?.Stop(); } catch { }

                // Join với timeout 1s — tránh treo UI khi đóng form
                try { if (videoThread != null && !videoThread.Join(1000)) log.Warning("[{Serial}] Video thread did not exit in time.", device.Serial); } catch { }
                try { if (controlThread != null && !controlThread.Join(1000)) log.Warning("[{Serial}] Control thread did not exit in time.", device.Serial); } catch { }
                try { if (bufferThread != null && !bufferThread.Join(1000)) log.Warning("[{Serial}] Buffer thread did not exit in time.", device.Serial); } catch { }

                videoThread = null;
                controlThread = null;
                bufferThread = null;
                listener = null;
                videoClient = null;
                controlClient = null;

                Connected = false;
            }
            finally
            {
                try { cts?.Dispose(); } catch { }
                cts = null;
                // Best-effort drain — phòng trường hợp BufferMain không kịp drain (Join timeout 1s)
                DrainBufferChannel();
                // Best-effort cleanup of ADB routes
                try { MobileServerCleanup(); } catch { }
            }
        }

        public void SendControlCommand(IControlMessage msg)
        {
            if (controlClient == null)
                log.Warning("SendControlCommand() called, but controlClient is null.");
            else
                controlChannel.Writer.TryWrite(msg);
        }

        public event Action<Size> OnLoadSizeEvent;
        private void ReadDeviceInfo()
        {
            if (videoClient == null)
                throw new Exception("Can't read device info when videoClient is null.");

            var infoStream = videoClient.GetStream();
            infoStream.ReadTimeout = 2000;

            // Read 68-byte header.
            var deviceInfoBuf = pool.Rent(68);
            int bytesRead = infoStream.Read(deviceInfoBuf, 0, 68);

            if (bytesRead != 68)
                throw new Exception($"Expected to read exactly 68 bytes, but got {bytesRead} bytes.");

            // Decode device name from header.
            var deviceInfoSpan = deviceInfoBuf.AsSpan();
            DeviceName = Encoding.UTF8.GetString(deviceInfoSpan[..64]).TrimEnd(new[] { '\0' });
            log.Information("Device name: " + DeviceName);

            Width = BinaryPrimitives.ReadInt16BigEndian(deviceInfoSpan[64..]);
            Height = BinaryPrimitives.ReadInt16BigEndian(deviceInfoSpan[66..]);
            log.Information($"Initial texture: {Width}x{Height}");
            this.OnLoadSizeEvent?.Invoke(new Size(Width, Height));
            pool.Return(deviceInfoBuf);
        }

        private async void VideoMain()
        {
            // Both of these should never happen.
            if (videoClient == null || VideoStreamDecoder == null) throw new Exception("videoClient is null.");
            if (cts == null) throw new Exception("cts is null.");

            var videoStream = videoClient.GetStream();

            int bytesRead;
            var metaBuf = pool.Rent(12);

            Stopwatch sw = new();

            try
            {
                while (!cts.Token.IsCancellationRequested)
                {
                    // Read metadata (each packet starts with some metadata)
                    try
                    {
                        bytesRead = await videoStream.ReadAsync(metaBuf, 0, 12, this.cts.Token);
                    }
                    catch (OperationCanceledException)
                    {
                        break;
                    }
                    catch (IOException ex)
                    {
                        // Ignore timeout errors.
                        if (ex.InnerException is SocketException x && x.SocketErrorCode == SocketError.TimedOut)
                            continue;
                        throw ex;
                    }

                    // Remote closed the connection gracefully.
                    if (bytesRead == 0)
                    {
                        log.Information("Video stream closed by remote (read 0 bytes for metadata).");
                        break;
                    }

                    if (bytesRead != 12)
                        throw new Exception($"Expected to read exactly 12 bytes, but got {bytesRead} bytes.");

                    sw.Restart();

                    // Decode metadata
                    var presentationTimeUs = BinaryPrimitives.ReadInt64BigEndian(metaBuf);
                    var packetSize = BinaryPrimitives.ReadInt32BigEndian(metaBuf[8..]);

                    // Read the whole frame, this might require more than one .Read() call.
                    var packetBuf = pool.Rent(packetSize);
                    var pos = 0;
                    var bytesToRead = packetSize;
                    bool remoteClosedDuringPacket = false;

                    try
                    {
                        while (bytesToRead != 0 && !cts.Token.IsCancellationRequested)
                        {
                            bytesRead = await videoStream.ReadAsync(packetBuf, pos, bytesToRead, this.cts.Token);

                            if (bytesRead == 0)
                            {
                                // Remote closed stream while sending packet.
                                log.Information("Video stream closed by remote while reading packet data.");
                                remoteClosedDuringPacket = true;
                                break;
                            }

                            pos += bytesRead;
                            bytesToRead -= bytesRead;
                        }

                        if (remoteClosedDuringPacket || cts.Token.IsCancellationRequested)
                            break;

                        // Only decode if we successfully read the full packet.
                        if (!cts.Token.IsCancellationRequested)
                        {
                            var packets = VideoStreamDecoder.Decode(packetBuf, packetSize, presentationTimeUs);

                            if (packets.Count != 0)
                            {
                                foreach (var info in packets)
                                {
                                    // TryWrite trả false khi channel full (Wait mode) hoặc closed.
                                    // Khi đó packet đã được clone → phải av_packet_free hoặc leak buffer H264.
                                    bool written = false;
                                    try
                                    {
                                        written = this.bufferChannel.Writer.TryWrite(info);
                                        if (!written)
                                        {
                                            // Channel full → drop packet này (consumer chậm). Free để tránh leak.
                                            unsafe
                                            {
                                                AVPacket* p = (AVPacket*)info;
                                                ffmpeg.av_packet_free(&p);
                                            }
                                        }
                                    }
                                    catch
                                    {
                                        if (!written)
                                        {
                                            unsafe
                                            {
                                                AVPacket* p = (AVPacket*)info;
                                                try { ffmpeg.av_packet_free(&p); } catch { }
                                            }
                                        }
                                    }
                                }
                            }

                            log.Verbose("Received and decoded a packet in {@ElapsedMilliseconds} ms", sw.ElapsedMilliseconds);
                        }
                        else
                        {
                            // Cancelled trước khi decode → vẫn cần parse để biết, nhưng packets không tạo.
                            // Không có gì để free.
                        }
                    }
                    finally
                    {
                        pool.Return(packetBuf);
                    }

                    sw.Stop();

                    await Task.Delay(1);
                }
            }
            finally
            {
                pool.Return(metaBuf);
            }
        }

        private async void ControllerMain()
        {
            // Both of these should never happen.
            if (controlClient == null) throw new Exception("controlClient is null.");
            if (cts == null) throw new Exception("cts is null.");

            var stream = controlClient.GetStream();

            try
            {
                await foreach (var cmd in controlChannel.Reader.ReadAllAsync(cts.Token))
                {
                    ControllerSend(stream, cmd);
                }
            }
            catch (OperationCanceledException) { }
        }

        // This needs to be in a separate method, because we can't use a Span<byte> inside an async function.
        private void ControllerSend(NetworkStream stream, IControlMessage cmd)
        {
            if (stream == null || !stream.CanWrite)
            {
                return;
            }

            try
            {
                var bytes = cmd.ToBytes();
                stream.Write(bytes);
                log.Debug("Sent control message: {0}", cmd.Type);
            }
            catch (IOException ex)
            {
                log.Error(ex, "IOException while sending control message: {0}", cmd.Type);
            }
            catch (ObjectDisposedException)
            {
                log.Error("Stream already disposed when sending: {0}", cmd.Type);
            }
        }

        // scrcpy-server v1.23 hard-code abstract socket name = "scrcpy"
        // Mỗi device có namespace abstract socket riêng (theo serial) → không đè nhau giữa các device
        // Chỉ cần đảm bảo reverse forward cũ được xoá đúng cách trước khi tạo lại
        private const string AbstractSocket = "localabstract:scrcpy";

        private void MobileServerSetup()
        {
            MobileServerCleanup();

            // Push scrcpy-server.jar
            UploadMobileServer();

            // Create reverse: device's localabstract:scrcpy ↔ host tcp:{port}
            adb.CreateReverseForward(device, AbstractSocket, $"tcp:{this.port}", true);
        }

        /// <summary>
        /// Xóa reverse forward của device này (chỉ tác động lên device hiện tại,
        /// không ảnh hưởng các device khác đang chạy song song).
        /// </summary>
        private void MobileServerCleanup()
        {
            try { adb.RemoveReverseForward(device, AbstractSocket); } catch { }
        }

        /// <summary>
        /// Start the scrcpy server on the android device.
        /// </summary>
        /// <param name="bitrate"></param>
        private void MobileServerStart()
        {
            log.Information("Starting scrcpy server...");

            var cts = new CancellationTokenSource();
            var receiver = new SerilogOutputReceiver();

            string version = "1.23";
            int maxFramerate = 60;
            // Khoá stream về portrait natural (0). Kể cả khi device chưa kịp xoay
            // dọc theo settings ADB hoặc app FB tự request landscape, scrcpy server
            // vẫn rotate frame về portrait → user không thấy frame landscape.
            ScrcpyLockVideoOrientation orientation = ScrcpyLockVideoOrientation.Orientation0;
            bool control = true;
            bool showTouches = false;
            bool stayAwake = false;

            var cmds = new List<string>
                {
                    "CLASSPATH=/data/local/tmp/scrcpy-server.jar",
                    "app_process",

                    // Unused
                    "/",

                    // App entry point, or something like that.
                    "com.genymobile.scrcpy.Server",

                    version,
                    "log_level=debug",
                    $"bit_rate={Bitrate}"
                };

            if (maxFramerate != 0)
                cmds.Add($"max_fps={maxFramerate}");

            if (orientation != ScrcpyLockVideoOrientation.Unlocked)
                cmds.Add($"lock_video_orientation={(int)orientation}");

            cmds.Add("tunnel_forward=false");
            //cmds.Add("crop=-");
            cmds.Add($"control={control}");
            cmds.Add("display_id=0");
            cmds.Add($"show_touches={showTouches}");
            cmds.Add($"stay_awake={stayAwake}");
            cmds.Add("power_off_on_close=false");
            cmds.Add("downsize_on_error=true");
            cmds.Add("cleanup=true");

            string command = string.Join(" ", cmds);

            log.Information("Start command: " + command);
            _ = adb.ExecuteRemoteCommandAsync(command, device, receiver, cts.Token);
        }

        private void UploadMobileServer()
        {
            using SyncService service = new(new AdbSocket(new IPEndPoint(IPAddress.Loopback, AdbClient.AdbServerPort)), device);
            using Stream stream = File.OpenRead(ScrcpyServerFile);
            service.Push(stream, "/data/local/tmp/scrcpy-server.jar", 444, DateTime.Now, null, CancellationToken.None);
        }

        public struct BufferInfo
        {
            public byte[]? Buffer { get; set; }
            public long Pts { get; set; }
        }
    }
}
