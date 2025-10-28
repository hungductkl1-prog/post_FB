using SDL2;
using Sunny.Subdy.Data.Models;
using System.Diagnostics;
using System.Drawing.Imaging;
using System.Windows.Forms;

namespace StreamAndroid
{
    public partial class ucThongBaoDeviceView : UserControl
    {
        private IntPtr sdlWinPtr;
        private IntPtr sdlRender;
        private IntPtr sdlTexture;

        private Size renderSize = new Size(1080, 1920);
        private SDL.SDL_Rect updateRect = new SDL.SDL_Rect();

        private static readonly object locker = new object();
        private static int sdlInitCount = 0;
        private static readonly object sdlInitLock = new();

        // Rotation support
        private int angle = 0;

        // Text overlay settings
        private int overlayTextOpacity = 100; // 10-100
        private string topLine1 = "";
        private string topLine2 = "";
        private string centerText = "";
        private Color centerTextColor = Color.White;
        private bool isResize = false;
        private DeviceModel deviceModel;
        public ucThongBaoDeviceView(DeviceModel device)
        {
            InitializeComponent();
            deviceModel = device;
            topLine1 = device.Id.ToString();
            topLine2 = device.NameDevice;

            this.Load += UcThongBaoDeviceView_Load;
            this.Disposed += UcThongBaoDeviceView_Disposed;
            pictureBox1.SizeChanged += PictureBox1_SizeChanged;
        }

        #region Initialization & Cleanup
        private List<string> WrapText(string text, Font font, int maxWidth, out List<SizeF> lineSizes)
        {
            var lines = new List<string>();
            lineSizes = new List<SizeF>();

            using (var tmpBmp = new Bitmap(1, 1))
            using (var g = Graphics.FromImage(tmpBmp))
            {
                g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.AntiAliasGridFit;
                var sf = StringFormat.GenericTypographic;

                // Xử lý xuống dòng thủ công nếu có \n
                var paragraphs = text.Split(new[] { '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries);

                foreach (var paragraph in paragraphs)
                {
                    var words = paragraph.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                    string currentLine = "";

                    foreach (var word in words)
                    {
                        string testLine = string.IsNullOrEmpty(currentLine) ? word : currentLine + " " + word;
                        var size = g.MeasureString(testLine, font, PointF.Empty, sf);

                        if (size.Width > maxWidth && !string.IsNullOrEmpty(currentLine))
                        {
                            // Lưu dòng hiện tại
                            var currentSize = g.MeasureString(currentLine, font, PointF.Empty, sf);
                            lines.Add(currentLine);
                            lineSizes.Add(currentSize);
                            currentLine = word;
                        }
                        else
                        {
                            currentLine = testLine;
                        }
                    }

                    // Thêm dòng cuối của paragraph
                    if (!string.IsNullOrEmpty(currentLine))
                    {
                        var currentSize = g.MeasureString(currentLine, font, PointF.Empty, sf);
                        lines.Add(currentLine);
                        lineSizes.Add(currentSize);
                    }
                }
            }

            return lines;
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
            int padding = radius + shadowOffset + 6;
            int w = (int)Math.Ceiling(textSize.Width) + padding * 2;
            int h = (int)Math.Ceiling(textSize.Height) + padding * 2;

            using (var bmp = new Bitmap(w, h, PixelFormat.Format32bppArgb))
            using (var g = Graphics.FromImage(bmp))
            using (var brushFill = new SolidBrush(fillColor))
            using (var brushBorder = new SolidBrush(borderColor))
            using (var brushShadow = new SolidBrush(Color.FromArgb(180, borderColor)))
            {
                g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.HighQuality;
                g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.AntiAliasGridFit;
                g.CompositingQuality = System.Drawing.Drawing2D.CompositingQuality.HighQuality;
                g.Clear(Color.Transparent);

                var sf = StringFormat.GenericTypographic;

                // Vẽ shadow đậm hơn
                g.DrawString(text, font, brushShadow,
                    new PointF(padding + shadowOffset, padding + shadowOffset), sf);

                // Vẽ border dày và rõ hơn (nhiều lớp)
                for (int layer = radius; layer > 0; layer--)
                {
                    for (int angle = 0; angle < 360; angle += 30)
                    {
                        double rad = angle * Math.PI / 180.0;
                        int dx = (int)(Math.Cos(rad) * layer);
                        int dy = (int)(Math.Sin(rad) * layer);
                        g.DrawString(text, font, brushBorder,
                            new PointF(padding + dx, padding + dy), sf);
                    }
                }

                // Vẽ fill color
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
        private volatile bool isDisposing = false;
        private volatile bool isInitialized = false;
        private readonly object initLock = new object();
        private void UcThongBaoDeviceView_Load(object? sender, EventArgs e)
        {
            if (isDisposing) return;

            try
            {
                lock (initLock)
                {
                    if (isInitialized || isDisposing)
                        return;

                    RoundPictureBox(pictureBox1, 4);
                    EnsureSdlInitialized();

                    // Đợi handle được tạo
                    if (!pictureBox1.IsHandleCreated)
                    {
                        pictureBox1.CreateControl();
                    }

                    // Kiểm tra lại sau khi CreateControl
                    if (!pictureBox1.IsHandleCreated)
                    {
                        Debug.WriteLine("⚠️ PictureBox handle not created");
                        return;
                    }

                    sdlWinPtr = SDL.SDL_CreateWindowFrom(pictureBox1.Handle);

                    if (sdlWinPtr == IntPtr.Zero)
                    {
                        Debug.WriteLine($"⚠️ SDL_CreateWindowFrom failed: {SDL.SDL_GetError()}");
                        return;
                    }

                    InitRender();

                    isInitialized = true;

                    // Hook events SAU KHI khởi tạo xong
                    pictureBox1.SizeChanged += PictureBox1_SizeChanged;
                    this.VisibleChanged += ucThongBaoDeviceView_VisibleChanged;
                }

                // Render SAU KHI load xong, KHÔNG dùng BeginInvoke
                RenderFrame();
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"⚠️ Load error: {ex.Message}\n{ex.StackTrace}");
            }
        }

        private void UcThongBaoDeviceView_Disposed(object? sender, EventArgs e)
        {
            isDisposing = true;

            // Unhook events TRƯỚC KHI dispose SDL
            try
            {
                pictureBox1.SizeChanged -= PictureBox1_SizeChanged;
                this.VisibleChanged -= ucThongBaoDeviceView_VisibleChanged;
            }
            catch { }

            // Đợi một chút để đảm bảo không còn BeginInvoke đang chạy
            System.Threading.Thread.Sleep(50);

            try
            {
                lock (locker)
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

        #endregion

        #region Size & Rotation

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

        public void SetRenderSize(int height, int rotationAngle = 0)
        {
            angle = rotationAngle;

            // Tính toán kích thước control dựa trên góc xoay
            int width;
            if (angle == 90 || angle == 270)
            {
                width = (int)(height * 16.0 / 9.0);
            }
            else
            {
                width = (int)(height * 9.0 / 16.0);
            }

            this.Size = new Size(width, height);

            // Áp dụng xoay cho renderer
            lock (locker)
            {
                isResize = true;
                InitRender();
                isResize = false;
            }

            this.Refresh();

            // Render sau khi resize xong
            System.Threading.Tasks.Task.Delay(50).ContinueWith(_ =>
            {
                if (this.IsHandleCreated && !this.IsDisposed)
                {
                    try
                    {
                        this.Invoke(new Action(() =>
                        {
                            Debug.WriteLine($"🎨 Render after SetRenderSize - Size:{width}x{height}, Angle:{angle}");
                            RenderFrame();
                        }));
                    }
                    catch { }
                }
            });
        }

        private void PictureBox1_SizeChanged(object? sender, EventArgs e)
        {
            try
            {
                RoundPictureBox(pictureBox1, 4);
            }
            catch { }

            lock (locker)
            {
                isResize = true;
                InitRender();
                isResize = false;
            }

            // Render sau một khoảng delay nhỏ
            System.Threading.Tasks.Task.Delay(50).ContinueWith(_ =>
            {
                if (this.IsHandleCreated && !this.IsDisposed)
                {
                    try
                    {
                        this.Invoke(new Action(() => RenderFrame()));
                    }
                    catch { }
                }
            });
        }

        #endregion

        #region Render Management

        private void InitRender()
        {
            if (isDisposing) return;

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

            // CRITICAL: Kiểm tra sdlWinPtr trước khi dùng
            if (sdlWinPtr == IntPtr.Zero)
            {
                if (!pictureBox1.IsHandleCreated)
                {
                    Debug.WriteLine("⚠️ InitRender: PictureBox handle not created yet");
                    return;
                }

                sdlWinPtr = SDL.SDL_CreateWindowFrom(pictureBox1.Handle);

                if (sdlWinPtr == IntPtr.Zero)
                {
                    Debug.WriteLine($"⚠️ InitRender: SDL_CreateWindowFrom failed - {SDL.SDL_GetError()}");
                    return;
                }
            }

            SDL.SDL_GetWindowSize(sdlWinPtr, out int winW, out int winH);

            if (winW <= 0 || winH <= 0)
            {
                Debug.WriteLine($"⚠️ Invalid window size: {winW}x{winH}");
                return;
            }

            updateRect = MakeThumb(renderSize.Width, renderSize.Height, winW, winH);

            // Dùng SOFTWARE renderer cho placeholder
            sdlRender = SDL.SDL_CreateRenderer(sdlWinPtr, -1,
                SDL.SDL_RendererFlags.SDL_RENDERER_SOFTWARE);

            if (sdlRender == IntPtr.Zero)
            {
                Debug.WriteLine($"⚠️ SDL_CreateRenderer failed: {SDL.SDL_GetError()}");
                return;
            }

            if (renderSize.Width <= 0 || renderSize.Height <= 0)
            {
                Debug.WriteLine($"⚠️ Invalid renderSize: {renderSize.Width}x{renderSize.Height}");
                return;
            }

            sdlTexture = SDL.SDL_CreateTexture(sdlRender, SDL.SDL_PIXELFORMAT_ARGB8888,
                (int)SDL.SDL_TextureAccess.SDL_TEXTUREACCESS_TARGET,
                renderSize.Width, renderSize.Height);

            if (sdlTexture == IntPtr.Zero)
            {
                Debug.WriteLine($"⚠️ SDL_CreateTexture failed: {SDL.SDL_GetError()}");
            }
        }


        private SDL.SDL_Rect MakeThumb(int pw, int ph, int ww, int wh)
        {
            if (pw <= 0 || ph <= 0 || ww <= 0 || wh <= 0)
                return new SDL.SDL_Rect { x = 0, y = 0, w = ww, h = wh };

            // Nếu xoay 90° hoặc 270°, đổi chiều rộng/cao của render size
            int effectiveW = pw;
            int effectiveH = ph;

            if (angle == 90 || angle == 270)
            {
                effectiveW = ph;
                effectiveH = pw;
            }

            double scaleX = ww / (double)effectiveW;
            double scaleY = wh / (double)effectiveH;
            double scale = Math.Min(scaleX, scaleY);

            int destW = (int)Math.Ceiling(effectiveW * scale);
            int destH = (int)Math.Ceiling(effectiveH * scale);

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

        #region Rendering

        public void RenderFrame()
        {
            if (isDisposing || !isInitialized)
                return;

            if (!IsHandleCreated || !Visible || !pictureBox1.Visible)
                return;

            // KHÔNG dùng lock nếu đang dispose
            if (isDisposing)
                return;

            if (!Monitor.TryEnter(locker, 10)) // Timeout 10ms
            {
                Debug.WriteLine("⚠️ RenderFrame: Could not acquire lock");
                return;
            }

            try
            {
                if (isResize || sdlRender == IntPtr.Zero || sdlTexture == IntPtr.Zero)
                    return;

                SDL.SDL_SetRenderDrawColor(sdlRender, 45, 45, 45, 255);
                SDL.SDL_RenderClear(sdlRender);

                SDL.SDL_Rect fullRect = new SDL.SDL_Rect
                {
                    x = 0,
                    y = 0,
                    w = pictureBox1.ClientSize.Width,
                    h = pictureBox1.ClientSize.Height
                };
                SDL.SDL_RenderFillRect(sdlRender, ref fullRect);

                DrawBackground();

                if (angle != 0)
                {
                    RenderWithRotation();
                }
                else
                {
                    DrawOverlayText(sdlRender, topLine1, topLine2);
                    if (!string.IsNullOrEmpty(centerText))
                    {
                        DrawCenterText(sdlRender, centerText, centerTextColor);
                    }
                }

                SDL.SDL_RenderPresent(sdlRender);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"❌ RenderFrame error: {ex.Message}");
            }
            finally
            {
                Monitor.Exit(locker);
            }
        }

        private void DrawBackground()
        {
            // Vẽ background màu tối như hình mẫu
            SDL.SDL_SetRenderDrawColor(sdlRender, 45, 45, 45, 255);
            SDL.SDL_RenderFillRect(sdlRender, ref updateRect);
        }


        // Thay thế phương thức DrawCenterText() - tối ưu hoàn toàn

        private void RenderWithRotation()
        {
            if (angle == 90 || angle == 270)
            {
                int centerX = pictureBox1.ClientSize.Width / 2;
                int centerY = pictureBox1.ClientSize.Height / 2;

                SDL.SDL_Rect rotatedRect = new SDL.SDL_Rect
                {
                    x = centerX - updateRect.h / 2,
                    y = centerY - updateRect.w / 2,
                    w = updateRect.h,
                    h = updateRect.w
                };

                // Vẽ background với rotation
                SDL.SDL_Point center = new SDL.SDL_Point
                {
                    x = rotatedRect.w / 2,
                    y = rotatedRect.h / 2
                };

                // Set render target nếu cần vẽ text với rotation
                DrawOverlayText(sdlRender, topLine1, topLine2);
                if (!string.IsNullOrEmpty(centerText))
                {
                    DrawCenterText(sdlRender, centerText, centerTextColor);
                }
            }
            else // 180°
            {
                DrawOverlayText(sdlRender, topLine1, topLine2);
                if (!string.IsNullOrEmpty(centerText))
                {
                    DrawCenterText(sdlRender, centerText, centerTextColor);
                }
            }
        }

        #endregion

        #region Text Drawing



        private void DrawOverlayText(IntPtr renderer, string line1, string line2)
        {
            if (string.IsNullOrEmpty(line1) && string.IsNullOrEmpty(line2))
                return;

            int availableHeight = updateRect.h > 0 ? updateRect.h :
                                 (pictureBox1?.ClientSize.Height > 0 ? pictureBox1.ClientSize.Height : 720);

            const double referenceHeight = 720.0;
            double scale = Math.Max(0.5, Math.Min(availableHeight / referenceHeight, 3.0));

            const float baseBig = 42f;
            const float baseSmall = 32f;

            float bigSize = Math.Max(12f, Math.Min((float)Math.Round(baseBig * scale), 120f));
            float smallSize = Math.Max(10f, Math.Min((float)Math.Round(baseSmall * scale), 88f));

            using var fontBig = new Font("Segoe UI", bigSize, FontStyle.Bold, GraphicsUnit.Pixel);
            using var fontSmall = new Font("Segoe UI", smallSize, FontStyle.Bold, GraphicsUnit.Pixel);

            var tex1 = !string.IsNullOrEmpty(line1) ? CreateOutlinedTextTexture(renderer, line1, fontBig, Color.White, Color.FromArgb(60, 60, 60), 2, 3) : IntPtr.Zero;
            var tex2 = !string.IsNullOrEmpty(line2) ? CreateOutlinedTextTexture(renderer, line2, fontSmall, Color.White, Color.FromArgb(60, 60, 60), 2, 3) : IntPtr.Zero;

            if (tex1 == IntPtr.Zero && tex2 == IntPtr.Zero)
                return;

            int w1 = 0, h1 = 0, w2 = 0, h2 = 0;
            if (tex1 != IntPtr.Zero)
                SDL.SDL_QueryTexture(tex1, out _, out _, out w1, out h1);
            if (tex2 != IntPtr.Zero)
                SDL.SDL_QueryTexture(tex2, out _, out _, out w2, out h2);

            // Áp dụng độ trong suốt
            byte alpha = (byte)(overlayTextOpacity * 255 / 100);

            if (tex1 != IntPtr.Zero)
                SDL.SDL_SetTextureAlphaMod(tex1, alpha);
            if (tex2 != IntPtr.Zero)
                SDL.SDL_SetTextureAlphaMod(tex2, alpha);

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

        private void DrawCenterText(IntPtr renderer, string text, Color textColor)
        {
            if (string.IsNullOrWhiteSpace(text) || renderer == IntPtr.Zero)
                return;

            int availableHeight = updateRect.h > 0 ? updateRect.h :
                                 (pictureBox1?.ClientSize.Height > 0 ? pictureBox1.ClientSize.Height : 720);

            int availableWidth = updateRect.w > 0 ? updateRect.w :
                                (pictureBox1?.ClientSize.Width > 0 ? pictureBox1.ClientSize.Width : 480);

            // Padding cho text - ưu tiên hiển thị hơn là padding
            int maxWidth = (int)(availableWidth * 0.9);
            int maxHeight = (int)(availableHeight * 0.8);

            // Scale dựa trên chiều cao - cho phép font nhỏ hơn
            const double referenceHeight = 720.0;
            double scale = Math.Max(0.3, Math.Min(availableHeight / referenceHeight, 3.0));
            float baseFontSize = Math.Max(8f, Math.Min((float)Math.Round(42f * scale), 18f));

            // Tìm font size và wrap text phù hợp
            float fontSize = baseFontSize;
            Font font = null;
            List<string> lines = null;
            List<SizeF> lineSizes = null;
            float totalHeight = 0;
            float maxLineWidth = 0;
            float lineSpacing = 0;

            while (fontSize >= 8f)
            {
                font?.Dispose();
                font = new Font("Segoe UI", fontSize, FontStyle.Bold, GraphicsUnit.Pixel);

                // Line spacing tỷ lệ với font size - giảm spacing khi font nhỏ
                lineSpacing = fontSize * 0.08f;

                // Wrap text
                lines = WrapText(text, font, maxWidth, out lineSizes);

                // Tính tổng chiều cao (bao gồm line spacing)
                totalHeight = 0;
                maxLineWidth = 0;

                for (int i = 0; i < lineSizes.Count; i++)
                {
                    totalHeight += lineSizes[i].Height;
                    if (i < lineSizes.Count - 1)
                        totalHeight += lineSpacing; // Thêm khoảng cách giữa các dòng

                    maxLineWidth = Math.Max(maxLineWidth, lineSizes[i].Width);
                }

                // Kiểm tra xem có vừa không
                if (totalHeight <= maxHeight && maxLineWidth <= maxWidth)
                    break;

                // Giảm font size
                fontSize -= 1f;
            }

            if (font == null || lines == null || lines.Count == 0)
                return;

            // Tính vị trí bắt đầu (center theo chiều dọc)
            int containerX = updateRect.w > 0 ? updateRect.x : 0;
            int containerY = updateRect.h > 0 ? updateRect.y : 0;
            int containerW = updateRect.w > 0 ? updateRect.w :
                             (pictureBox1?.ClientSize.Width > 0 ? pictureBox1.ClientSize.Width : 480);
            int containerH = updateRect.h > 0 ? updateRect.h :
                             (pictureBox1?.ClientSize.Height > 0 ? pictureBox1.ClientSize.Height : 720);

            int startY = containerY + (int)((containerH - totalHeight) / 2);
            float currentY = startY;

            byte alpha = (byte)(overlayTextOpacity * 255 / 100);

            // Vẽ từng dòng
            for (int i = 0; i < lines.Count; i++)
            {
                var line = lines[i];
                if (string.IsNullOrWhiteSpace(line))
                    continue;

                // Tăng border thickness để viền rõ hơn - nhưng không quá dày với font nhỏ
                int borderThickness = Math.Max(1, Math.Min((int)(fontSize * 0.08f), 3));
                int shadowOffset = Math.Max(1, Math.Min((int)(fontSize * 0.06f), 2));

                var tex = CreateOutlinedTextTexture(renderer, line, font,
                    textColor, Color.FromArgb(30, 30, 30), borderThickness, shadowOffset);

                if (tex == IntPtr.Zero)
                    continue;

                SDL.SDL_QueryTexture(tex, out _, out _, out int w, out int h);
                SDL.SDL_SetTextureAlphaMod(tex, alpha);

                // Center theo chiều ngang
                int x = containerX + (containerW - w) / 2;

                SDL.SDL_Rect dst = new SDL.SDL_Rect
                {
                    x = x,
                    y = (int)currentY,
                    w = w,
                    h = h
                };
                SDL.SDL_RenderCopy(renderer, tex, IntPtr.Zero, ref dst);
                SDL.SDL_DestroyTexture(tex);

                // Di chuyển đến dòng tiếp theo
                currentY += h + (i < lines.Count - 1 ? lineSpacing : 0);
            }

            font?.Dispose();
        }



        #endregion

        #region Public Methods

        /// <summary>
        /// Thiết lập text hiển thị ở đầu màn hình (2 dòng)
        /// </summary>
        public void SetTopText(string line1, string line2 = "")
        {
            topLine1 = line1 ?? "";
            topLine2 = line2 ?? "";
            RenderFrame();
        }

        /// <summary>
        /// Thiết lập text hiển thị ở giữa màn hình
        /// </summary>
        public void SetCenterText(string text, Color? color = null)
        {
            centerText = text ?? "";
            if (color.HasValue)
                centerTextColor = color.Value;
            RenderFrame();
        }

        /// <summary>
        /// Xóa tất cả text
        /// </summary>
        public void ClearAllText()
        {
            topLine1 = "";
            topLine2 = "";
            centerText = "";
            RenderFrame();
        }

        /// <summary>
        /// Thiết lập độ trong suốt của text (10-100)
        /// </summary>
        public void SetTextOpacity(int opacity)
        {
            overlayTextOpacity = Math.Max(10, Math.Min(100, opacity));
            RenderFrame();
        }

        /// <summary>
        /// Lấy độ trong suốt hiện tại
        /// </summary>
        public int GetTextOpacity()
        {
            return overlayTextOpacity;
        }

        #endregion

        private void ucThongBaoDeviceView_VisibleChanged(object sender, EventArgs e)
        {
            if (isDisposing || !isInitialized)
                return;

            if (!this.Visible)
                return;

            // KHÔNG dùng BeginInvoke, gọi trực tiếp
            lock (locker)
            {
                if (sdlRender != IntPtr.Zero && !isResize)
                {
                    isResize = true;
                    InitRender();
                    isResize = false;
                }
            }

            RenderFrame();
        }
    }
}
