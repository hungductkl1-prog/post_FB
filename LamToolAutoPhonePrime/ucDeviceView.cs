using AutoAndroid;
using FFmpeg.AutoGen;
using ScrcpyNet;
using SDL2;
using Sunny.Subdy.Data.Models;
using System.Diagnostics;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;
using Font = System.Drawing.Font;
using Point = System.Drawing.Point;

namespace LamToolAutoPhonePrime
{
    public partial class ucDeviceView : UserControl
    {
        private Scrcpy? instance;
        private IntPtr sdlWinPtr;
        private IntPtr sdlRender;
        private IntPtr sdlTexture;

        private Size renderSize;
        private SDL.SDL_Rect updateRect = new SDL.SDL_Rect();

        private bool isPointerDown;
        private ulong currentPointerId = 1;
        private readonly Stopwatch moveThrottle = Stopwatch.StartNew();
        private const int moveIntervalMs = 16;
        private bool isResize;
        private static readonly object locker = new object();

        public readonly DeviceModel device;

        private static int sdlInitCount = 0;
        private static readonly object sdlInitLock = new();
        private int frameIntervalMs = 1000 / 30;
        private long lastFrameTimestamp = 0;
        private int processingFrame = 0;
        private bool firstFrameLogged = false;

        // Drag & Drop support
        private bool isDragging = false;
        private Point dragStartPoint;
        private Point dragStartLocation;
        private string textRender = "";
        private bool showOverlayText = true;
        private int overlayTextAlpha = 255;

        /// <summary>
        /// Giữ tương thích với Form1.cs (main project) — subscribe để nhận click
        /// vào menu Settings trong context menu của device. Sự kiện không còn fire
        /// từ SDL button (đã bỏ), chỉ fire khi user chọn item trong popover menu.
        /// </summary>
        public event EventHandler? SettingsButtonClicked;

        /// <summary>
        /// Fire khi user touch/drag trên view. Toạ độ normalized (0..1)
        /// theo render size. fMultiView broadcast sang tile khác khi sync ON.
        /// </summary>
        public event Action<NormalizedTouch>? UserTouch;
        public event Action<NormalizedScroll>? UserScroll;
        public event Action<KeycodeControlMessage>? UserKey;

        public readonly record struct NormalizedTouch(
            AndroidMotionEventAction Action,
            double Nx, double Ny,
            ulong PointerId,
            AndroidMotionEventButtons Buttons);

        public readonly record struct NormalizedScroll(
            double Nx, double Ny,
            AndroidMotionEventButtons Buttons);
        public ucDeviceView(DeviceModel device, bool showOverlayText, string textRender, Scrcpy scrcpy)
        {
            InitializeComponent();
            this.device = device;
            this.textRender = textRender;
            this.showOverlayText = showOverlayText;
            instance = scrcpy;
            this.Load += UcDeviceView_Load;
            this.Disposed += UcDeviceView_Disposed;
            this.VisibleChanged += UcDeviceView_VisibleChanged;

            pictureBox1.MouseDown += PictureBox1_MouseDown;
            pictureBox1.MouseUp += PictureBox1_MouseUp;
            pictureBox1.MouseMove += PictureBox1_MouseMove;
            pictureBox1.MouseLeave += PictureBox1_MouseLeave;
            pictureBox1.MouseWheel += PictureBox1_MouseWheel;
            pictureBox1.MouseEnter += PictureBox1_MouseEnter;
            pictureBox1.SizeChanged += PictureBox1_SizeChanged;
            // Setup drag & drop cho panel1 (để di chuyển UserControl)
            panel1.MouseDown += Panel1_MouseDown;
            panel1.MouseMove += Panel1_MouseMove;
            panel1.MouseUp += Panel1_MouseUp;

            pictureBox1.PreviewKeyDown += PictureBox1_PreviewKeyDown;
        }

        private async void PictureBox1_PreviewKeyDown(object? sender, PreviewKeyDownEventArgs e)
        {

            e.IsInputKey = true;
            if (e.Control && !e.Shift && !e.Alt && e.KeyCode == Keys.V)
            {
                string clipboardText = Clipboard.GetText();
                if (!string.IsNullOrEmpty(clipboardText))
                {
                    ADBClient client = new ADBClient(device);
                    await client.TurnOnADBKeyboard();
                    client.ADBKeyboardService.Input(clipboardText);
                }
                return;
            }
            if (e.Control && !e.Shift && !e.Alt && e.KeyCode == Keys.C)
            {
                ADBClient client = new ADBClient(device);
                string clipboardText = await client.GetClipboardText();
                Clipboard.SetText(clipboardText);
                return;
            }

            // Xử lý phím bình thường
            var msg = new KeycodeControlMessage
            {
                KeyCode = KeycodeHelper.ConvertKey(e.KeyCode),
                Metastate = KeycodeHelper.ConvertModifiers(e.Modifiers)
            };
            instance.SendControlCommand(msg);
            UserKey?.Invoke(msg);
        }

        private void RoundPictureBox(PictureBox pic, int radius)
        {
            var path = new System.Drawing.Drawing2D.GraphicsPath();
            int diameter = radius * 2;

            path.AddArc(0, 0, diameter, diameter, 180, 90);
            path.AddArc(pic.Width - diameter, 0, diameter, diameter, 270, 90);
            path.AddArc(pic.Width - diameter, pic.Height - diameter, diameter, diameter, 0, 90);
            path.AddArc(0, pic.Height - diameter, diameter, diameter, 90, 90);
            path.CloseFigure();

            pic.Region = new Region(path);
            pic.Invalidate();
        }

        public void SetRenderSize(int height)
        {
            int width = (int)(height * 9.0 / 16.0);
            this.Size = new Size(width, height);
            this.Refresh();
        }

        private void EnsureSdlInitialized()
        {
            lock (sdlInitLock)
            {
                if (sdlInitCount == 0)
                {
                    SDL.SDL_Init(SDL.SDL_INIT_VIDEO);
                }
                sdlInitCount++;
            }
        }

        private void EnsureSdlShutdown()
        {
            lock (sdlInitLock)
            {
                sdlInitCount--;
                if (sdlInitCount <= 0)
                {
                    try { SDL.SDL_Quit(); } catch { }
                    sdlInitCount = 0;
                }
            }
        }

        private void UcDeviceView_Load(object? sender, EventArgs e)
        {
            try
            {
                Trace.WriteLine($"[ucDeviceView/{device.Serial}] Load begin. PB size={pictureBox1.Width}x{pictureBox1.Height}, handle={pictureBox1.IsHandleCreated}");
                RoundPictureBox(pictureBox1, 4);

                EnsureSdlInitialized();

                // Force-create handle nếu chưa có để SDL có HWND hợp lệ
                if (!pictureBox1.IsHandleCreated)
                {
                    var _ = pictureBox1.Handle; // chạm property để force CreateHandle
                }
                sdlWinPtr = SDL.SDL_CreateWindowFrom(pictureBox1.Handle);
                Trace.WriteLine($"[ucDeviceView/{device.Serial}] SDL_CreateWindowFrom => {sdlWinPtr}");

                if (instance == null) { Trace.WriteLine($"[ucDeviceView/{device.Serial}] instance is null"); return; }

                // Nếu Scrcpy đã có size sẵn (đã connect xong trước khi UC được Add) → init render ngay
                if (instance.Width > 0 && instance.Height > 0)
                {
                    renderSize = new Size(instance.Width, instance.Height);
                    Trace.WriteLine($"[ucDeviceView/{device.Serial}] Pre-known size {renderSize.Width}x{renderSize.Height}, calling InitRender");
                    lock (locker) { InitRender(); }
                    Trace.WriteLine($"[ucDeviceView/{device.Serial}] After InitRender: sdlRender={sdlRender}, sdlTexture={sdlTexture}");
                }

                instance.OnLoadSizeEvent += Scrcpy_OnLoadSizeEvent;
                instance.VideoStreamDecoder.NewFrameEvent += VideoStreamDecoder_NewFrameEvent;
                Trace.WriteLine($"[ucDeviceView/{device.Serial}] Subscribed events");
                // Ẩn button4, button5 từ Designer (chúng ta dùng SDL buttons)
                if (button4 != null) button4.Visible = false;
                if (button5 != null) button5.Visible = false;
            }
            catch (Exception ex)
            {
                Trace.WriteLine($"[ucDeviceView/{device.Serial}] Load EXCEPTION: {ex}");
            }
        }

        private void UcDeviceView_Disposed(object? sender, EventArgs e)
        {
            try
            {
                if (instance != null)
                {
                    try
                    {
                        instance.OnLoadSizeEvent -= Scrcpy_OnLoadSizeEvent;
                        instance.VideoStreamDecoder.NewFrameEvent -= VideoStreamDecoder_NewFrameEvent;
                    }
                    catch { }
                }

                lock (locker)
                {
                    if (sdlTexture != IntPtr.Zero)
                    {
                        SDL.SDL_DestroyTexture(sdlTexture);
                        sdlTexture = IntPtr.Zero;
                    }
                    if (sdlRender != IntPtr.Zero)
                    {
                        SDL.SDL_DestroyRenderer(sdlRender);
                        sdlRender = IntPtr.Zero;
                    }
                    if (sdlWinPtr != IntPtr.Zero)
                    {
                        try { SDL.SDL_DestroyWindow(sdlWinPtr); } catch { }
                        sdlWinPtr = IntPtr.Zero;
                    }
                }

                EnsureSdlShutdown();
            }
            catch { }
        }

        #region Mouse Event Handlers

        private void PictureBox1_MouseEnter(object? sender, EventArgs e)
        {
            try { pictureBox1.Focus(); panel1.BorderColor = Color.Green; } catch { }
        }

        private void PictureBox1_MouseDown(object? sender, MouseEventArgs e)
        {
            if (showOverlayText && e.Button == MouseButtons.Right)
            {
                var ucMenuscrip = new ucMenuscripDevice(false) { Height = this.Size.Height };
                var config = new AntdUI.Popover.Config(
       pictureBox1,
      ucMenuscrip
   )
                {
                    ArrowAlign = AntdUI.TAlign.Right,
                    Offset = 4
                };

                var f = AntdUI.Popover.open(config);
                return;
            }
            // Touch handling
            if (!IsInsideRender(e.Location)) return;

            if (panel1.BorderColor != Color.Green) return;

            var pos = GetTouchPosition(e.Location);
            SendTouch(AndroidMotionEventAction.AMOTION_EVENT_ACTION_DOWN, pos, e.Button);
            isPointerDown = true;
            moveThrottle.Restart();
            pictureBox1.Capture = true;
        }

        private void PictureBox1_MouseMove(object? sender, MouseEventArgs e)
        {
            // Touch move
            if (!isPointerDown || moveThrottle.ElapsedMilliseconds < moveIntervalMs)
                return;
            if (panel1.BorderColor != Color.Green) return;

            var pos = GetTouchPosition(e.Location);
            SendTouch(AndroidMotionEventAction.AMOTION_EVENT_ACTION_MOVE, pos, e.Button);
            moveThrottle.Restart();
        }

        private void PictureBox1_MouseUp(object? sender, MouseEventArgs e)
        {
            if (!isPointerDown)
                return;
            if (panel1.BorderColor != Color.Green) return;
            var pos = GetTouchPosition(e.Location);
            SendTouch(AndroidMotionEventAction.AMOTION_EVENT_ACTION_UP, pos, e.Button);
            isPointerDown = false;
            currentPointerId++;
            pictureBox1.Capture = false;
        }

        private void PictureBox1_MouseLeave(object? sender, EventArgs e)
        {
            pictureBox1.Cursor = Cursors.Default;
            panel1.BorderColor = Color.RoyalBlue;
            if (!isPointerDown)
                return;
            var pos = GetTouchPosition(PointToClient(Cursor.Position));
            SendTouch(AndroidMotionEventAction.AMOTION_EVENT_ACTION_UP, pos, MouseButtons.Left);
            isPointerDown = false;
            currentPointerId++;
            pictureBox1.Capture = false;
        }

        private void PictureBox1_MouseWheel(object? sender, MouseEventArgs e)
        {
            if (panel1.BorderColor != Color.Green) return;
            var pos = GetTouchPosition(e.Location);
            var buttons = e.Delta > 0
                ? AndroidMotionEventButtons.AMOTION_EVENT_BUTTON_FORWARD
                : AndroidMotionEventButtons.AMOTION_EVENT_BUTTON_BACK;
            var msg = new TouchEventControlMessage
            {
                Action = AndroidMotionEventAction.AMOTION_EVENT_ACTION_SCROLL,
                Position = pos,
                PointerId = currentPointerId,
                Buttons = buttons
            };
            SafeSend(msg);

            if (renderSize.Width > 0 && renderSize.Height > 0 && UserScroll != null)
            {
                double nx = pos.Point.X / (double)renderSize.Width;
                double ny = pos.Point.Y / (double)renderSize.Height;
                UserScroll.Invoke(new NormalizedScroll(nx, ny, buttons));
            }
        }



        #endregion

        #region Drag & Drop Support

        private void Panel1_MouseDown(object sender, MouseEventArgs e)
        {
            // Chỉ drag khi click vào vùng border của panel1 (không phải pictureBox)
            if (e.Button == MouseButtons.Left)
            {
                // Kiểm tra nếu click vào border area (7px border)
                var panel = sender as System.Windows.Forms.Panel;
                if (panel != null)
                {
                    bool isInBorder = e.X < 7 || e.X > panel.Width - 7 ||
                                      e.Y < 7 || e.Y > panel.Height - 7;

                    if (isInBorder)
                    {
                        isDragging = true;
                        dragStartPoint = e.Location;
                        dragStartLocation = this.Location;
                        panel.Cursor = Cursors.SizeAll;
                    }
                }
            }
        }

        private void Panel1_MouseMove(object sender, MouseEventArgs e)
        {
            if (isDragging)
            {
                // Tính toán vị trí mới
                Point newLocation = new Point(
                    dragStartLocation.X + (e.X - dragStartPoint.X),
                    dragStartLocation.Y + (e.Y - dragStartPoint.Y)
                );

                // Đảm bảo không kéo ra ngoài form
                if (this.Parent != null)
                {
                    newLocation.X = Math.Max(0, Math.Min(newLocation.X, this.Parent.ClientSize.Width - this.Width));
                    newLocation.Y = Math.Max(0, Math.Min(newLocation.Y, this.Parent.ClientSize.Height - this.Height));
                }

                this.Location = newLocation;
            }
        }

        private void Panel1_MouseUp(object sender, MouseEventArgs e)
        {
            if (isDragging)
            {
                isDragging = false;
                var panel = sender as System.Windows.Forms.Panel;
                if (panel != null)
                {
                    panel.Cursor = Cursors.Default;
                }
            }
        }

        // Alternative: Drag bằng PageHeader
        private void PageHeader_MouseDown(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left)
            {
                isDragging = true;
                dragStartPoint = e.Location;
                dragStartLocation = this.Location;
            }
        }

        private void PageHeader_MouseMove(object sender, MouseEventArgs e)
        {
            if (isDragging)
            {
                Point newLocation = new Point(
                    dragStartLocation.X + (e.X - dragStartPoint.X),
                    dragStartLocation.Y + (e.Y - dragStartPoint.Y)
                );

                if (this.Parent != null)
                {
                    newLocation.X = Math.Max(0, Math.Min(newLocation.X, this.Parent.ClientSize.Width - this.Width));
                    newLocation.Y = Math.Max(0, Math.Min(newLocation.Y, this.Parent.ClientSize.Height - this.Height));
                }

                this.Location = newLocation;
            }
        }

        private void PageHeader_MouseUp(object sender, MouseEventArgs e)
        {
            isDragging = false;
        }

        #endregion

        #region Render

        private void Scrcpy_OnLoadSizeEvent(Size size)
        {
            Trace.WriteLine($"[ucDeviceView/{device.Serial}] OnLoadSizeEvent {size.Width}x{size.Height}, handle={IsHandleCreated}");
            // Event này fire từ background thread của Scrcpy.ReadDeviceInfo
            // Phải marshal về UI thread vì SDL_CreateWindowFrom/CreateRenderer
            // cần HWND ở thread sở hữu cửa sổ.
            if (IsDisposed || !IsHandleCreated) return;
            try
            {
                BeginInvoke((MethodInvoker)delegate
                {
                    if (IsDisposed) return;
                    renderSize = size;
                    lock (locker)
                    {
                        InitRender();
                    }
                });
            }
            catch (ObjectDisposedException) { }
            catch (InvalidOperationException) { /* handle bị destroy */ }
        }

        private IntPtr CreateOutlinedTextTexture(IntPtr renderer, string text, Font font, Color fillColor, Color borderColor, int borderThickness = 2, int shadowOffset = 3)
        {
            if (string.IsNullOrEmpty(text) || renderer == IntPtr.Zero || font == null)
                return IntPtr.Zero;

            SizeF textSize;
            using (var tmpBmp = new Bitmap(1, 1))
            using (var g = Graphics.FromImage(tmpBmp))
            {
                g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.AntiAliasGridFit;
                textSize = g.MeasureString(text, font, PointF.Empty, StringFormat.GenericTypographic);
            }

            int radius = Math.Max(1, borderThickness);
            int padding = radius + shadowOffset + 4;
            int w = (int)Math.Ceiling(textSize.Width) + padding * 2;
            int h = (int)Math.Ceiling(textSize.Height) + padding * 2;

            using (var bmp = new Bitmap(w, h, PixelFormat.Format32bppArgb))
            using (var g = Graphics.FromImage(bmp))
            using (var brushFill = new SolidBrush(fillColor))
            using (var brushBorder = new SolidBrush(borderColor))
            using (var brushShadow = new SolidBrush(Color.FromArgb(150, borderColor)))
            {
                g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
                g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.AntiAliasGridFit;
                g.Clear(Color.Transparent);

                var sf = StringFormat.GenericTypographic;

                g.DrawString(text, font, brushShadow, new PointF(padding + shadowOffset, padding + shadowOffset), sf);

                for (int dy = -radius; dy <= radius; dy++)
                {
                    for (int dx = -radius; dx <= radius; dx++)
                    {
                        if (dx * dx + dy * dy <= radius * radius)
                        {
                            g.DrawString(text, font, brushBorder, new PointF(padding + dx, padding + dy), sf);
                        }
                    }
                }

                g.DrawString(text, font, brushFill, new PointF(padding, padding), sf);

                var rect = new Rectangle(0, 0, bmp.Width, bmp.Height);
                var bmpData = bmp.LockBits(rect, ImageLockMode.ReadOnly, PixelFormat.Format32bppArgb);
                IntPtr texture = SDL.SDL_CreateTexture(
                    renderer,
                    SDL.SDL_PIXELFORMAT_ARGB8888,
                    (int)SDL.SDL_TextureAccess.SDL_TEXTUREACCESS_STATIC,
                    bmp.Width, bmp.Height);

                if (texture != IntPtr.Zero)
                {
                    SDL.SDL_UpdateTexture(texture, IntPtr.Zero, bmpData.Scan0, bmpData.Stride);
                    SDL.SDL_SetTextureBlendMode(texture, SDL.SDL_BlendMode.SDL_BLENDMODE_BLEND);
                }

                bmp.UnlockBits(bmpData);
                return texture;
            }
        }

        private void DrawOverlayText(IntPtr renderer, string line1, string line2)
        {
            int availableHeight = updateRect.h > 0 ? updateRect.h :
                                 (pictureBox1?.ClientSize.Height > 0 ? pictureBox1.ClientSize.Height :
                                 (renderSize.Height > 0 ? renderSize.Height : 720));

            const double referenceHeight = 720.0;
            double scale = Math.Max(0.5, Math.Min(availableHeight / referenceHeight, 3.0));

            const float baseBig = 42f;
            const float baseSmall = 32f;

            float bigSize = Math.Max(12f, Math.Min((float)Math.Round(baseBig * scale), 120f));
            float smallSize = Math.Max(10f, Math.Min((float)Math.Round(baseSmall * scale), 88f));

            using var fontBig = new Font("Segoe UI", bigSize, FontStyle.Bold, GraphicsUnit.Pixel);
            using var fontSmall = new Font("Segoe UI", smallSize, FontStyle.Bold, GraphicsUnit.Pixel);

            var tex1 = CreateOutlinedTextTexture(renderer, line1, fontBig, Color.White, Color.FromArgb(60, 60, 60), 2, 3);
            var tex2 = CreateOutlinedTextTexture(renderer, line2, fontSmall, Color.White, Color.FromArgb(60, 60, 60), 2, 3);

            if (tex1 == IntPtr.Zero && tex2 == IntPtr.Zero)
                return;

            byte alpha = (byte)Math.Max(0, Math.Min(255, overlayTextAlpha));
            if (tex1 != IntPtr.Zero) SDL.SDL_SetTextureAlphaMod(tex1, alpha);
            if (tex2 != IntPtr.Zero) SDL.SDL_SetTextureAlphaMod(tex2, alpha);

            int w1 = 0, h1 = 0, w2 = 0, h2 = 0;
            if (tex1 != IntPtr.Zero)
                SDL.SDL_QueryTexture(tex1, out _, out _, out w1, out h1);
            if (tex2 != IntPtr.Zero)
                SDL.SDL_QueryTexture(tex2, out _, out _, out w2, out h2);

            int containerX = updateRect.w > 0 ? updateRect.x : 10;
            int containerW = updateRect.w > 0 ? updateRect.w : Math.Max(Math.Max(w1, w2) + 20, 200);
            int centerX = containerX + containerW / 2;
            int startY = (updateRect.h > 0 ? updateRect.y : 10) + 10;

            if (tex1 != IntPtr.Zero)
            {
                SDL.SDL_Rect dst1 = new SDL.SDL_Rect { x = centerX - w1 / 2, y = startY, w = w1, h = h1 };
                SDL.SDL_RenderCopy(renderer, tex1, IntPtr.Zero, ref dst1);
                SDL.SDL_DestroyTexture(tex1);
            }
            if (tex2 != IntPtr.Zero)
            {
                SDL.SDL_Rect dst2 = new SDL.SDL_Rect { x = centerX - w2 / 2, y = startY + h1, w = w2, h = h2 };
                SDL.SDL_RenderCopy(renderer, tex2, IntPtr.Zero, ref dst2);
                SDL.SDL_DestroyTexture(tex2);
            }
        }
        private void DrawBottomText(IntPtr renderer, string text)
        {
            if (string.IsNullOrWhiteSpace(text) || renderer == IntPtr.Zero)
                return;

            // Tính tỷ lệ font dựa trên chiều cao khung hình
            int availableHeight = updateRect.h > 0 ? updateRect.h :
                                 (pictureBox1?.ClientSize.Height > 0 ? pictureBox1.ClientSize.Height :
                                 (renderSize.Height > 0 ? renderSize.Height : 720));

            const double referenceHeight = 720.0;
            double scale = Math.Max(0.5, Math.Min(availableHeight / referenceHeight, 3.0));
            float fontSize = Math.Max(10f, Math.Min((float)Math.Round(16f * scale), 72f));

            using var font = new Font("Segoe UI", fontSize, FontStyle.Bold, GraphicsUnit.Pixel);

            // Tạo texture có viền + bóng mờ
            var tex = CreateOutlinedTextTexture(renderer, text, font,
                Color.White, Color.FromArgb(50, 50, 50), 2, 3);

            if (tex == IntPtr.Zero)
                return;

            // Lấy kích thước text
            SDL.SDL_QueryTexture(tex, out _, out _, out int w, out int h);

            // Tính vị trí hiển thị: giữa chiều ngang, sát mép dưới
            int containerX = updateRect.w > 0 ? updateRect.x : 0;
            int containerW = updateRect.w > 0 ? updateRect.w :
                             (pictureBox1?.ClientSize.Width > 0 ? pictureBox1.ClientSize.Width : w);

            int containerY = updateRect.h > 0 ? updateRect.y : 0;
            int containerH = updateRect.h > 0 ? updateRect.h :
                             (pictureBox1?.ClientSize.Height > 0 ? pictureBox1.ClientSize.Height : h);

            int x = containerX + (containerW - w) / 2;
            int y = containerY + containerH - h - 20; // cách mép dưới 20px

            // Render text
            SDL.SDL_Rect dst = new SDL.SDL_Rect { x = x, y = y, w = w, h = h };
            SDL.SDL_RenderCopy(renderer, tex, IntPtr.Zero, ref dst);

            SDL.SDL_DestroyTexture(tex);
        }
        private unsafe void VideoStreamDecoder_NewFrameEvent(AVFrame frame)
        {
            if (!firstFrameLogged)
            {
                firstFrameLogged = true;
                Trace.WriteLine($"[ucDeviceView/{device.Serial}] FIRST FRAME received {frame.width}x{frame.height}. handle={IsHandleCreated} visible={Visible} pbVisible={pictureBox1.Visible} pbHandle={pictureBox1.IsHandleCreated}");
            }

            if (!pictureBox1.IsHandleCreated || IsDisposed)
                return;

            long now = Stopwatch.GetTimestamp();
            long elapsedMs = (now - lastFrameTimestamp) * 1000 / Stopwatch.Frequency;
            if (elapsedMs < frameIntervalMs)
                return;

            if (Interlocked.Exchange(ref processingFrame, 1) == 1)
                return;

            try
            {
                bool sizeChanged = frame.width != renderSize.Width || frame.height != renderSize.Height;
                bool needInit = sdlRender == IntPtr.Zero || sdlTexture == IntPtr.Zero || sizeChanged;

                // InitRender gọi SDL_CreateWindowFrom(pictureBox1.Handle) — phải chạy trên UI thread.
                // Nếu đang ở background thread (FFmpeg callback), schedule InitRender trên UI thread
                // và bỏ qua frame hiện tại. Frame tiếp theo sẽ render bình thường.
                if (needInit)
                {
                    if (sizeChanged)
                        renderSize = new Size(frame.width, frame.height);
                    else if (renderSize.Width <= 0 || renderSize.Height <= 0)
                        renderSize = new Size(frame.width, frame.height);

                    ScheduleInitRender();
                    return;
                }

                lock (locker)
                {
                    if (IsDisposed || isResize)
                        return;

                    if (sdlTexture == IntPtr.Zero || sdlRender == IntPtr.Zero)
                        return;

                    int rc = SDL_UpdateYUVTexture(sdlTexture, IntPtr.Zero,
                        new IntPtr(frame.data[0]), frame.linesize[0],
                        new IntPtr(frame.data[1]), frame.linesize[1],
                        new IntPtr(frame.data[2]), frame.linesize[2]);
                    if (rc != 0)
                    {
                        Trace.WriteLine($"[ucDeviceView/{device.Serial}] UpdateYUV fail rc={rc} err={SDL.SDL_GetError()}");
                    }

                    SDL.SDL_RenderClear(sdlRender);
                    SDL_RenderCopy(sdlRender, sdlTexture, IntPtr.Zero, ref updateRect);
                    if (showOverlayText && overlayTextAlpha > 0)
                    {
                        try
                        {
                            DrawOverlayText(sdlRender, device.Id.ToString(), device.NameDevice ?? device.Serial ?? "");
                        }
                        catch (Exception exOverlay)
                        {
                            Trace.WriteLine($"[ucDeviceView/{device.Serial}] Overlay error: {exOverlay.Message}");
                        }
                    }
                    if (!string.IsNullOrEmpty(textRender))
                    {
                        try { DrawBottomText(sdlRender, textRender); }
                        catch (Exception exBottom) { Trace.WriteLine($"[ucDeviceView/{device.Serial}] Bottom text error: {exBottom.Message}"); }
                    }

                    SDL.SDL_RenderPresent(sdlRender);

                    lastFrameTimestamp = Stopwatch.GetTimestamp();
                }
            }
            catch (Exception ex)
            {
                Trace.WriteLine($"[ucDeviceView/{device.Serial}] Frame render EXCEPTION: {ex.GetType().Name}: {ex.Message}");
            }
            finally
            {
                Interlocked.Exchange(ref processingFrame, 0);
            }
        }

        private int _initRenderPending = 0;

        /// <summary>
        /// Schedule InitRender on UI thread. Thread-safe — multiple callers are coalesced.
        /// </summary>
        private void ScheduleInitRender()
        {
            if (IsDisposed || !IsHandleCreated) return;
            if (Interlocked.Exchange(ref _initRenderPending, 1) == 1) return;
            try
            {
                BeginInvoke((MethodInvoker)delegate
                {
                    _initRenderPending = 0;
                    if (IsDisposed) return;
                    lock (locker)
                    {
                        InitRender();
                    }
                });
            }
            catch (ObjectDisposedException) { _initRenderPending = 0; }
            catch (InvalidOperationException) { _initRenderPending = 0; }
        }

        private void PictureBox1_SizeChanged(object? sender, EventArgs e)
        {
            if (pictureBox1.Width <= 0 || pictureBox1.Height <= 0) return;

            try
            {
                RoundPictureBox(pictureBox1, 4);
            }
            catch { }

            lock (locker)
            {
                isResize = true;
                // KHÔNG destroy sdlWinPtr — SDL_CreateWindowFrom chỉ wrap HWND của PictureBox,
                // SDL_DestroyWindow có thể destroy luôn HWND gốc → crash.
                // Chỉ cần recreate renderer/texture; updateRect dùng ClientSize trực tiếp.
                InitRender();
                isResize = false;
            }
        }

        private void UcDeviceView_VisibleChanged(object? sender, EventArgs e)
        {
            if (Visible && pictureBox1.Visible)
            {
                lastFrameTimestamp = 0;
            }
        }

        private void InitRender()
        {
            if (sdlTexture != IntPtr.Zero)
            {
                try { SDL.SDL_DestroyTexture(sdlTexture); } catch { }
                sdlTexture = IntPtr.Zero;
            }
            if (sdlRender != IntPtr.Zero)
            {
                try { SDL.SDL_DestroyRenderer(sdlRender); } catch { }
                sdlRender = IntPtr.Zero;
            }

            if (sdlWinPtr == IntPtr.Zero && pictureBox1.IsHandleCreated)
            {
                sdlWinPtr = SDL.SDL_CreateWindowFrom(pictureBox1.Handle);
            }

            if (sdlWinPtr == IntPtr.Zero)
                return;

            // Lấy size thực tế từ Win32 window (PictureBox.ClientSize) — SDL_GetWindowSize
            // chỉ cache size lúc CreateWindowFrom, không update khi HWND resize.
            int winW = pictureBox1.ClientSize.Width;
            int winH = pictureBox1.ClientSize.Height;
            if (winW <= 0) winW = pictureBox1.Width;
            if (winH <= 0) winH = pictureBox1.Height;
            updateRect = MakeThumb(renderSize.Width, renderSize.Height, winW, winH);

            // Try ACCELERATED first; fallback SOFTWARE nếu GPU/D3D context fail
            // (xảy ra khi nhiều SDL window con chia sẻ cùng GPU context trong multi-view)
            sdlRender = SDL.SDL_CreateRenderer(sdlWinPtr, -1, SDL.SDL_RendererFlags.SDL_RENDERER_ACCELERATED);
            if (sdlRender == IntPtr.Zero)
            {
                Trace.WriteLine($"[ucDeviceView/{device.Serial}] ACCELERATED renderer failed: {SDL.SDL_GetError()}, fallback to SOFTWARE");
                sdlRender = SDL.SDL_CreateRenderer(sdlWinPtr, -1, SDL.SDL_RendererFlags.SDL_RENDERER_SOFTWARE);
            }
            if (sdlRender == IntPtr.Zero)
            {
                Trace.WriteLine($"[ucDeviceView/{device.Serial}] SDL_CreateRenderer failed entirely: {SDL.SDL_GetError()}");
                return;
            }

            if (renderSize.Width <= 0 || renderSize.Height <= 0)
            {
                Trace.WriteLine($"[ucDeviceView/{device.Serial}] InitRender: renderSize invalid {renderSize.Width}x{renderSize.Height}, skip texture create");
                return;
            }

            sdlTexture = SDL.SDL_CreateTexture(sdlRender, SDL.SDL_PIXELFORMAT_IYUV,
                (int)SDL.SDL_TextureAccess.SDL_TEXTUREACCESS_STREAMING,
                renderSize.Width, renderSize.Height);
            if (sdlTexture == IntPtr.Zero)
            {
                Trace.WriteLine($"[ucDeviceView/{device.Serial}] SDL_CreateTexture failed: {SDL.SDL_GetError()}");
            }
            else
            {
                Trace.WriteLine($"[ucDeviceView/{device.Serial}] InitRender OK: render={sdlRender}, tex={sdlTexture}, size={renderSize.Width}x{renderSize.Height}, win={winW}x{winH}");
            }
        }

        private SDL.SDL_Rect MakeThumb(int pw, int ph, int ww, int wh)
        {
            if (pw <= 0 || ph <= 0 || ww <= 0 || wh <= 0)
                return new SDL.SDL_Rect { x = 0, y = 0, w = Math.Max(1, ww), h = Math.Max(1, wh) };

            double scaleX = ww / (double)pw;
            double scaleY = wh / (double)ph;
            double scale = Math.Max(scaleX, scaleY);

            int destW = (int)Math.Ceiling(pw * scale);
            int destH = (int)Math.Ceiling(ph * scale);

            int destX = (ww - destW) / 2;
            int destY = (wh - destH) / 2;

            return new SDL.SDL_Rect
            {
                x = destX,
                y = destY,
                w = destW,
                h = destH
            };
        }

        #endregion

        #region Touch Helpers

        private void SendTouch(AndroidMotionEventAction action, Position pos, MouseButtons button)
        {
            var buttons = button == MouseButtons.Right
                ? AndroidMotionEventButtons.AMOTION_EVENT_BUTTON_SECONDARY
                : AndroidMotionEventButtons.AMOTION_EVENT_BUTTON_PRIMARY;

            var msg = new TouchEventControlMessage
            {
                Action = action,
                Position = pos,
                PointerId = currentPointerId,
                Buttons = buttons
            };
            SafeSend(msg);

            // Broadcast normalized coords (chỉ khi pos hợp lệ)
            if (renderSize.Width > 0 && renderSize.Height > 0 && UserTouch != null)
            {
                double nx = pos.Point.X / (double)renderSize.Width;
                double ny = pos.Point.Y / (double)renderSize.Height;
                UserTouch.Invoke(new NormalizedTouch(action, nx, ny, currentPointerId, buttons));
            }
        }

        private void SafeSend(TouchEventControlMessage msg)
        {
            try { instance?.SendControlCommand(msg); } catch { }
        }

        private bool IsInsideRender(Point p)
        {
            return p.X >= updateRect.x && p.Y >= updateRect.y &&
                   p.X <= updateRect.x + updateRect.w && p.Y <= updateRect.y + updateRect.h;
        }

        private Position GetTouchPosition(Point p)
        {
            var pos = new Position();
            if (renderSize.Width <= 0 || renderSize.Height <= 0)
            {
                pos.Point = new ScrcpyNet.Point { X = 0, Y = 0 };
                pos.ScreenSize = new ScreenSize();
                return pos;
            }

            double scaleX = (double)updateRect.w / renderSize.Width;
            double scaleY = (double)updateRect.h / renderSize.Height;
            double mx = (p.X - updateRect.x) / scaleX;
            double my = (p.Y - updateRect.y) / scaleY;

            mx = Math.Max(0, Math.Min(mx, renderSize.Width - 1));
            my = Math.Max(0, Math.Min(my, renderSize.Height - 1));

            pos.Point = new ScrcpyNet.Point { X = (int)mx, Y = (int)my };
            pos.ScreenSize = new ScreenSize { Width = (ushort)renderSize.Width, Height = (ushort)renderSize.Height };
            return pos;
        }

        #endregion

        #region SDL Externs

        [DllImport("SDL2", CallingConvention = CallingConvention.Cdecl)]
        public static extern int SDL_UpdateYUVTexture(IntPtr texture, IntPtr rect,
            IntPtr yPlane, int yPitch, IntPtr uPlane, int uPitch, IntPtr vPlane, int vPitch);

        [DllImport("SDL2", CallingConvention = CallingConvention.Cdecl)]
        public static extern int SDL_RenderCopy(IntPtr renderer, IntPtr texture,
            IntPtr srcrect, ref SDL.SDL_Rect dstrect);

        #endregion

        #region Public Methods

        public void SetFrameRate(int fps)
        {
            if (fps <= 0 || fps > 120)
                throw new ArgumentException("FPS must be between 1 and 120");

            frameIntervalMs = 1000 / fps;
        }

        public DeviceModel GetDevice()
        {
            return device;
        }

        public bool IsConnected()
        {
            return instance != null && sdlRender != IntPtr.Zero;
        }

        /// <summary>
        /// Set độ trong suốt overlay text (deviceId + name). 0 = ẩn hẳn, 255 = đậm.
        /// </summary>
        public void SetOverlayAlpha(int alpha)
        {
            overlayTextAlpha = Math.Max(0, Math.Min(255, alpha));
        }

        /// <summary>
        /// Bật/tắt hiển thị overlay text.
        /// </summary>
        public void SetOverlayVisible(bool visible)
        {
            showOverlayText = visible;
        }

        /// <summary>
        /// Gửi 1 keycode (down + up) tới device qua scrcpy control channel.
        /// Dùng cho các button Back/Home/Switch trên tile.
        /// </summary>
        public void SendKeycode(AndroidKeycode keycode)
        {
            if (instance == null) return;
            try
            {
                var down = new KeycodeControlMessage
                {
                    Action = AndroidKeyEventAction.AKEY_EVENT_ACTION_DOWN,
                    KeyCode = keycode
                };
                var up = new KeycodeControlMessage
                {
                    Action = AndroidKeyEventAction.AKEY_EVENT_ACTION_UP,
                    KeyCode = keycode
                };
                instance.SendControlCommand(down);
                instance.SendControlCommand(up);
            }
            catch { }
        }

        public void ReplayKey(KeycodeControlMessage src)
        {
            if (instance == null) return;
            try { instance.SendControlCommand(src); } catch { }
        }

        public Scrcpy? GetScrcpy() => instance;

        /// <summary>
        /// Replay 1 touch event từ tile khác (sync mode). Toạ độ normalized 0..1
        /// được map sang renderSize của device này.
        /// </summary>
        public void ReplayTouch(NormalizedTouch t)
        {
            if (instance == null || renderSize.Width <= 0 || renderSize.Height <= 0) return;
            int x = (int)Math.Max(0, Math.Min(t.Nx * renderSize.Width, renderSize.Width - 1));
            int y = (int)Math.Max(0, Math.Min(t.Ny * renderSize.Height, renderSize.Height - 1));
            try
            {
                instance.SendControlCommand(new TouchEventControlMessage
                {
                    Action = t.Action,
                    PointerId = t.PointerId,
                    Buttons = t.Buttons,
                    Position = new Position
                    {
                        Point = new ScrcpyNet.Point { X = x, Y = y },
                        ScreenSize = new ScreenSize
                        {
                            Width = (ushort)renderSize.Width,
                            Height = (ushort)renderSize.Height
                        }
                    }
                });
            }
            catch { }
        }

        public void ReplayScroll(NormalizedScroll s)
        {
            if (instance == null || renderSize.Width <= 0 || renderSize.Height <= 0) return;
            int x = (int)Math.Max(0, Math.Min(s.Nx * renderSize.Width, renderSize.Width - 1));
            int y = (int)Math.Max(0, Math.Min(s.Ny * renderSize.Height, renderSize.Height - 1));
            try
            {
                instance.SendControlCommand(new TouchEventControlMessage
                {
                    Action = AndroidMotionEventAction.AMOTION_EVENT_ACTION_SCROLL,
                    Buttons = s.Buttons,
                    Position = new Position
                    {
                        Point = new ScrcpyNet.Point { X = x, Y = y },
                        ScreenSize = new ScreenSize
                        {
                            Width = (ushort)renderSize.Width,
                            Height = (ushort)renderSize.Height
                        }
                    }
                });
            }
            catch { }
        }

        #endregion
    }
}
