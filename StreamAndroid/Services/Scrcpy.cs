using FFmpeg.AutoGen;
using Serilog;
using SharpAdbClient;
using System.Buffers;
using System.Buffers.Binary;
using System.Diagnostics;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Text;
using System.Threading.Channels;

namespace StreamAndroid.Services
{
    public class Scrcpy
    {
        public string DeviceName { get; private set; } = "";
        public int Width { get; internal set; }
        public int Height { get; internal set; }
        public long Bitrate { get; set; } = 8000000;
        public static string ScrcpyServerFile { get; set; } = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "App", "scrcpy-server.jar");


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
        private readonly Channel<AVPacket> bufferChannel = Channel.CreateUnbounded<AVPacket>();
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

        public void Start(long timeoutMs = 5000)
        {
            if (Connected)
                throw new Exception("Already connected.");

            // UpdatePort();
            MobileServerSetup();

            listener = new TcpListener(IPAddress.Loopback, port);
            listener.Start();

            MobileServerStart();

            int waitTimeMs = 0;
            while (!listener.Pending())
            {
                Thread.Sleep(10);
                waitTimeMs += 10;

                if (waitTimeMs > timeoutMs)
                    throw new Exception("Timeout while waiting for server to connect.");
            }

            videoClient = listener.AcceptTcpClient();
            log.Information("Video socket connected.");

            if (!listener.Pending())
                throw new Exception("Server is not sending a second connection request. Is 'control' disabled?");

            controlClient = listener.AcceptTcpClient();
            log.Information("Control socket connected.");

            ReadDeviceInfo();

            cts = new CancellationTokenSource();

            bufferThread = new Thread(BufferMain);
            bufferThread.Start();
            videoThread = new Thread(VideoMain) { Name = "ScrcpyNet Video" };
            controlThread = new Thread(ControllerMain) { Name = "ScrcpyNet Controller" };

            videoThread.Start();
            controlThread.Start();

            Connected = true;

            // ADB forward/reverse is not needed anymore.
            MobileServerCleanup();
        }


        private async void BufferMain()
        {
            if (cts == null) return;
            await foreach (var item in bufferChannel.Reader.ReadAllAsync())
            {
                VideoStreamDecoder?.DecodePacket(item);
            }
        }

        public void Stop()
        {
            if (!Connected)
                throw new Exception("Not connected.");

            cts?.Cancel();

            videoThread?.Join();
            controlThread?.Join();
            listener?.Stop();
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
            OnLoadSizeEvent?.Invoke(new Size(Width, Height));
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
                        bytesRead = await videoStream.ReadAsync(metaBuf, 0, 12, cts.Token);
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
                            bytesRead = await videoStream.ReadAsync(packetBuf, pos, bytesToRead, cts.Token);

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
                                    bufferChannel.Writer.TryWrite(info);
                                }
                            }

                            log.Verbose("Received and decoded a packet in {@ElapsedMilliseconds} ms", sw.ElapsedMilliseconds);
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

        private void MobileServerSetup()
        {
            MobileServerCleanup();

            // Push scrcpy-server.jar
            UploadMobileServer();

            // Create port reverse rule
            adb.CreateReverseForward(device, "localabstract:scrcpy", $"tcp:{port}", true);
        }

        /// <summary>
        /// Remove ADB forwards/reverses.
        /// </summary>
        private void MobileServerCleanup()
        {
            // Remove any existing network stuff.
            adb.RemoveAllForwards(device);
            adb.RemoveAllReverseForwards(device);
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
            ScrcpyLockVideoOrientation orientation = ScrcpyLockVideoOrientation.Unlocked; // -1 means allow rotate
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
