using Emgu.CV;
using Emgu.CV.Reg;
using Emgu.CV.Structure;
using OpenCvSharp;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;
using Tesseract;
using Mat = OpenCvSharp.Mat;
using Point = System.Drawing.Point;
namespace AutoAndroid
{
    public static class ImageScanOpenCV
    {
        public static Bitmap GetImage(string path)
        {
            return new Bitmap(path);
        }

        public static Bitmap Find(string main, string sub, double percent = 0.9)
        {
            Bitmap mainImg = ImageScanOpenCV.GetImage(main);
            Bitmap subImg = ImageScanOpenCV.GetImage(sub);
            return ImageScanOpenCV.Find(mainImg, subImg, percent);
        }
        public static List<RegionResult> FindColorCoordinates(Bitmap bitmap, Color targetColor, int tolerance = 10, int regionWidth = 120, int regionHeight = 120)
        {
            List<RegionResult> results = new List<RegionResult>();

            int regionsX = (int)Math.Ceiling(bitmap.Width / (double)regionWidth);
            int regionsY = (int)Math.Ceiling(bitmap.Height / (double)regionHeight);

            for (int rx = 0; rx < regionsX; rx++)
            {
                for (int ry = 0; ry < regionsY; ry++)
                {
                    int startX = rx * regionWidth;
                    int startY = ry * regionHeight;
                    int endX = Math.Min(startX + regionWidth, bitmap.Width);
                    int endY = Math.Min(startY + regionHeight, bitmap.Height);

                    List<Point> regionPixels = new List<Point>();

                    for (int x = startX; x < endX; x++)
                    {
                        for (int y = startY; y < endY; y++)
                        {
                            Color pixelColor = bitmap.GetPixel(x, y);
                            if (IsColorSimilar(pixelColor, targetColor, tolerance))
                            {
                                regionPixels.Add(new Point(x, y));
                            }
                        }
                    }

                    if (regionPixels.Count > 0)
                    {
                        // Tính điểm trung bình vùng
                        int sumX = 0, sumY = 0;
                        foreach (var p in regionPixels)
                        {
                            sumX += p.X;
                            sumY += p.Y;
                        }
                        Point center = new Point(sumX / regionPixels.Count, sumY / regionPixels.Count);

                        results.Add(new RegionResult
                        {
                            Rx = rx,
                            Ry = ry,
                            PixelCount = regionPixels.Count,
                            Center = center
                        });

                        //  Debug.WriteLine($"Region ({rx},{ry}) - found {regionPixels.Count} pixels, center at ({center.X},{center.Y})");
                    }
                }
            }

            return results;
        }

        private static bool IsColorSimilar(Color c1, Color c2, int tolerance)
        {
            int rDiff = Math.Abs(c1.R - c2.R);
            int gDiff = Math.Abs(c1.G - c2.G);
            int bDiff = Math.Abs(c1.B - c2.B);

            return rDiff <= tolerance && gDiff <= tolerance && bDiff <= tolerance;
        }
        public static Point? FindClosestColorHSV(Bitmap bmp, Color target, double tolerance = 6.0)
        {
            if (bmp.PixelFormat != PixelFormat.Format24bppRgb)
            {
                var tmp = new Bitmap(bmp.Width, bmp.Height, PixelFormat.Format24bppRgb);
                using (var g = Graphics.FromImage(tmp))
                    g.DrawImage(bmp, 0, 0, bmp.Width, bmp.Height);
                bmp = tmp;
            }

            var rect = new Rectangle(0, 0, bmp.Width, bmp.Height);
            var data = bmp.LockBits(rect, ImageLockMode.ReadOnly, PixelFormat.Format24bppRgb);
            try
            {
                int stride = Math.Abs(data.Stride);
                int width = bmp.Width;
                int height = bmp.Height;
                byte[] buffer = new byte[stride * height];
                Marshal.Copy(data.Scan0, buffer, 0, buffer.Length);

                List<(int X, int Y, double Diff)> matches = new();

                for (int y = 0; y < height; y++)
                {
                    int row = y * stride;
                    for (int x = 0; x < width; x++)
                    {
                        int i = row + x * 3;
                        byte b = buffer[i];
                        byte g = buffer[i + 1];
                        byte r = buffer[i + 2];

                        double diff = Math.Sqrt(
                            (r - target.R) * (r - target.R) +
                            (g - target.G) * (g - target.G) +
                            (b - target.B) * (b - target.B)
                        );

                        if (diff <= tolerance)
                            matches.Add((x, y, diff));
                    }
                }

                if (matches.Count == 0)
                    return null;

                // Lấy nhóm pixel có độ lệch nhỏ nhất (ví dụ top 0.5%)
                int keep = Math.Max(1, (int)(matches.Count * 0.005));
                var top = matches.OrderBy(m => m.Diff).Take(keep).ToList();

                double avgX = top.Average(m => m.X);
                double avgY = top.Average(m => m.Y);

                //  Debug.WriteLine($"Top {keep}/{matches.Count} pixels avg: ({avgX:F2},{avgY:F2})");

                return new Point((int)Math.Round(avgX), (int)Math.Round(avgY));
            }
            finally
            {
                bmp.UnlockBits(data);
            }
        }

        public static Point? FindClosestColorPosition(Bitmap bmp, Color target, double tolerance = 6.0)
        {
            if (bmp.PixelFormat != PixelFormat.Format24bppRgb)
            {
                // clone sang 24bpp để LockBits an toàn
                var tmp = new Bitmap(bmp.Width, bmp.Height, PixelFormat.Format24bppRgb);
                using (var g = Graphics.FromImage(tmp)) g.DrawImage(bmp, 0, 0, bmp.Width, bmp.Height);
                bmp = tmp;
            }

            var rect = new Rectangle(0, 0, bmp.Width, bmp.Height);
            var data = bmp.LockBits(rect, ImageLockMode.ReadOnly, PixelFormat.Format24bppRgb);
            try
            {
                int stride = Math.Abs(data.Stride);
                int width = bmp.Width;
                int height = bmp.Height;
                int bytes = stride * height;
                byte[] buffer = new byte[bytes];
                Marshal.Copy(data.Scan0, buffer, 0, bytes);

                double bestDiff = double.MaxValue;
                Point bestPt = new Point(0, 0);
                long sumX = 0, sumY = 0;
                int count = 0;

                for (int y = 0; y < height; y++)
                {
                    int row = y * stride;
                    for (int x = 0; x < width; x++)
                    {
                        int i = row + x * 3;
                        byte b = buffer[i + 0];
                        byte g = buffer[i + 1];
                        byte r = buffer[i + 2];

                        double diff = Math.Sqrt(
                            (r - target.R) * (r - target.R) +
                            (g - target.G) * (g - target.G) +
                            (b - target.B) * (b - target.B)
                        );

                        if (diff < bestDiff)
                        {
                            bestDiff = diff;
                            bestPt = new Point(x, y);
                            //  Debug.WriteLine($"Found {bestPt} pixels");
                        }

                        if (diff <= tolerance)
                        {
                            sumX += x; sumY += y; count++;
                        }
                    }
                }

                if (count > 0)
                {
                    int cx = (int)Math.Round(sumX / (double)count);
                    int cy = (int)Math.Round(sumY / (double)count);
                    //  Debug.WriteLine($"Found {count} pixels within tolerance. Average position: ({cx}, {cy})");
                    return new Point(cx, cy);
                }
                return bestPt; // fallback
            }
            finally
            {
                bmp.UnlockBits(data);
            }
        }
        public static Bitmap Find(Bitmap mainBitmap, Bitmap subBitmap, double percent = 0.9)
        {
            // Convert Bitmaps to Mat
            OpenCvSharp.Mat source = BitmapToMat(mainBitmap);
            OpenCvSharp.Mat template = BitmapToMat(subBitmap);
            OpenCvSharp.Mat imageToShow = source.Clone();
            OpenCvSharp.Mat result = new OpenCvSharp.Mat();
            try
            {
                // Perform template matching
                Cv2.MatchTemplate(source, template, result, TemplateMatchModes.CCoeffNormed);

                // Get the min/max values
                Cv2.MinMaxLoc(result, out _, out double maxVal, out _, out OpenCvSharp.Point maxLoc);

                if (maxVal <= percent)
                    return null;

                // Draw rectangle around match
                Cv2.Rectangle(imageToShow, maxLoc, new OpenCvSharp.Point(maxLoc.X + template.Width, maxLoc.Y + template.Height), new Scalar(0, 0, 255), 2);
                return MatToBitmap(imageToShow);
            }
            finally
            {
                source.Dispose();
                template.Dispose();
                imageToShow.Dispose();
                result.Dispose();
            }
        }

        [DllImport("gdi32.dll")]
        public static extern bool DeleteObject(IntPtr hObject);

        public static Point? FindOutPoint(Bitmap mainBitmap, Bitmap subBitmap, double percent = 0.9)
        {
            if (subBitmap == null || mainBitmap == null)
                return null;

            if (subBitmap.Width > mainBitmap.Width || subBitmap.Height > mainBitmap.Height)
                return null;

            OpenCvSharp.Mat source = BitmapToMat(mainBitmap);
            OpenCvSharp.Mat template = BitmapToMat(subBitmap);
            OpenCvSharp.Mat result = new OpenCvSharp.Mat();
            try
            {
                Cv2.MatchTemplate(source, template, result, TemplateMatchModes.CCoeffNormed);

                Cv2.MinMaxLoc(result, out _, out double maxVal, out _, out OpenCvSharp.Point maxLoc);

                if (maxVal > percent)
                {
                    // Convert OpenCvSharp.Point to System.Drawing.Point
                    return new System.Drawing.Point(maxLoc.X, maxLoc.Y);
                }

                return null;
            }
            finally
            {
                source.Dispose();
                template.Dispose();
                result.Dispose();
            }
        }

        public static List<Point> FindOutPoints(Bitmap mainBitmap, Bitmap subBitmap, double percent = 0.9)
        {
            OpenCvSharp.Mat source = BitmapToMat(mainBitmap);
            OpenCvSharp.Mat template = BitmapToMat(subBitmap);
            List<Point> resPoints = new List<Point>();

            try
            {
                while (true)
                {
                    using OpenCvSharp.Mat result = new OpenCvSharp.Mat();
                    Cv2.MatchTemplate(source, template, result, TemplateMatchModes.CCoeffNormed);
                    Cv2.MinMaxLoc(result, out _, out double maxVal, out _, out OpenCvSharp.Point maxLoc);

                    if (maxVal <= percent)
                        break;

                    // Draw rectangle around match and add the location
                    Cv2.Rectangle(source, maxLoc, new OpenCvSharp.Point(maxLoc.X + template.Width, maxLoc.Y + template.Height), new Scalar(0, 0, 255), 2);
                    resPoints.Add(new System.Drawing.Point(maxLoc.X, maxLoc.Y));
                }
            }
            finally
            {
                source.Dispose();
                template.Dispose();
            }

            return resPoints;
        }

        public static List<Point> FindColor(Bitmap mainBitmap, System.Drawing.Color color)
        {
            int searchValue = color.ToArgb();
            List<Point> result = new List<Point>();

            try
            {
                using OpenCvSharp.Mat mat = BitmapToMat(mainBitmap);
                for (int y = 0; y < mat.Rows; y++)
                {
                    for (int x = 0; x < mat.Cols; x++)
                    {
                        Vec3b pixel = mat.Get<Vec3b>(y, x);
                        int pixelArgb = Color.FromArgb(pixel.Item2, pixel.Item1, pixel.Item0).ToArgb();

                        if (searchValue == pixelArgb)
                        {
                            result.Add(new Point(x, y));
                        }
                    }
                }
            }
            finally
            {
                if (mainBitmap != null)
                {
                    mainBitmap.Dispose();
                }
            }

            return result;
        }

        public static void TestDilate(Bitmap bmp)
        {
            OpenCvSharp.Mat mat = BitmapToMat(bmp);
            OpenCvSharp.Mat dilated = new OpenCvSharp.Mat();

            // Perform dilation
            Cv2.Dilate(mat, dilated, new OpenCvSharp.Mat(), iterations: 1);

            // Save the result
            MatToBitmap(dilated).Save("dilated.png");
        }

        public static Bitmap ThreshHoldBinary(Bitmap bmp, byte threshold = 190)
        {
            using OpenCvSharp.Mat img = BitmapToMat(bmp);
            using OpenCvSharp.Mat thresholded = new OpenCvSharp.Mat();

            // Apply binary threshold
            Cv2.Threshold(img, thresholded, threshold, 255, ThresholdTypes.Binary);

            return MatToBitmap(thresholded);
        }

        public static Bitmap NotWhiteToTransparentPixelReplacement(Bitmap bmp)
        {
            return PixelReplacement(bmp, color => color.R > 200 && color.G > 200 && color.B > 200, System.Drawing.Color.Transparent);
        }

        public static Bitmap WhiteToBlackPixelReplacement(Bitmap bmp)
        {
            return PixelReplacement(bmp, color => color.R > 20 && color.G > 230 && color.B > 230, System.Drawing.Color.Black);
        }

        public static Bitmap TransparentToWhitePixelReplacement(Bitmap bmp)
        {
            return PixelReplacement(bmp, color => color.A >= 1, System.Drawing.Color.White);
        }

        private static Bitmap PixelReplacement(Bitmap bmp, Func<System.Drawing.Color, bool> condition, System.Drawing.Color replacementColor)
        {
            using OpenCvSharp.Mat mat = BitmapToMat(bmp);
            for (int y = 0; y < mat.Rows; y++)
            {
                for (int x = 0; x < mat.Cols; x++)
                {
                    Vec3b pixel = mat.Get<Vec3b>(y, x);
                    System.Drawing.Color color = System.Drawing.Color.FromArgb(pixel.Item2, pixel.Item1, pixel.Item0);
                    if (condition(color))
                    {
                        mat.Set(y, x, new Vec3b(replacementColor.B, replacementColor.G, replacementColor.R));
                    }
                }
            }
            return MatToBitmap(mat);
        }

        public static Bitmap CreateNonIndexedImage(Image src)
        {
            Bitmap newBmp = new Bitmap(src.Width, src.Height, System.Drawing.Imaging.PixelFormat.Format32bppArgb);
            using (Graphics gfx = Graphics.FromImage(newBmp))
            {
                gfx.DrawImage(src, 0, 0);
            }
            return newBmp;
        }

        private static OpenCvSharp.Mat BitmapToMat(Bitmap bmp)
        {
            try
            {
                using (var stream = new System.IO.MemoryStream())
                {
                    bmp.Save(stream, System.Drawing.Imaging.ImageFormat.Png);
                    return Cv2.ImDecode(stream.ToArray(), ImreadModes.Color);
                }
            }
            catch
            {

            }
            return null;
        }

        private static Bitmap MatToBitmap(Mat mat)
        {
            using (var stream = new System.IO.MemoryStream())
            {
                Cv2.ImEncode(".png", mat, out byte[] imageData);
                stream.Write(imageData, 0, imageData.Length);
                return new Bitmap(stream);
            }
        }
        public static string GetTextFromImage(Bitmap bitmap)
        {
            if (bitmap == null)
                return string.Empty;

            try
            {
                // Convert Bitmap to Mat
                using Mat image = BitmapToMat(bitmap);

                // Check if image is successfully loaded
                if (image.Empty())
                {
                    //  Debug.WriteLine("Image data is empty after conversion.");
                    return string.Empty;
                }
                // Preprocess image: convert to grayscale and blur
                Cv2.CvtColor(image, image, ColorConversionCodes.BGR2GRAY);
                Cv2.GaussianBlur(image, image, new OpenCvSharp.Size(5, 5), 0);

                // Apply binary thresholding
                Cv2.Threshold(image, image, 0, 255, ThresholdTypes.Binary | ThresholdTypes.Otsu);

                // Optional: Save the processed image for verification
                Cv2.ImWrite("processed_image.png", image);

                // OCR with Tesseract
                using (var engine = new TesseractEngine(@"./tessdata", "eng", EngineMode.Default))
                {
                    using (var pix = MatToPix(image))
                    {
                        if (pix == null)
                        {
                            //  Debug.WriteLine("Failed to convert Mat to Pix.");
                            return string.Empty;
                        }

                        using (var result = engine.Process(pix))
                            return result.GetText();
                    }
                }
            }
            catch (Exception ex)
            {
                //  Debug.WriteLine("Error during OCR: " + ex.Message);
                return string.Empty;
            }
        }

        // Chuyển Mat sang Pix (Tesseract)
        private static Pix MatToPix(Mat mat)
        {
            using (var stream = mat.ToMemoryStream())
            {
                return Pix.LoadFromMemory(stream.ToArray());
            }
        }

    }
    public class RegionResult
    {
        public int Rx { get; set; }
        public int Ry { get; set; }
        public int PixelCount { get; set; }
        public Point Center { get; set; }
    }
}
