using System;
using System.Drawing;
using System.Text.RegularExpressions;
using System.Windows.Forms;

namespace GFL_OCR_RSC_Tracker
{
    class BoundsCapturer
    {
        public Rectangle GeneratedBounds { get; set; }
        public bool HangOnCapturedBound { get; set; }

        public BoundsCapturer(bool hangOnCapturedBound = false)
        {
            HangOnCapturedBound = hangOnCapturedBound;
        }

        public void CreateFinder()
        {
            var boundsFinder = new Form
            {
                Width = 882,
                Height = 103,
                BackColor = Color.Firebrick,
                TransparencyKey = Color.Firebrick,
                Text = "Drag this over your main menu resource values, close to capture area, this can also be resized"
            };
            boundsFinder.FormClosing += GetFormBounds;
            Application.Run(boundsFinder);
            CaptureArea();
            if (HangOnCapturedBound) ShowCapture();
        }

        private void CaptureArea()
        {
            Bitmap captureBmp = new Bitmap(GeneratedBounds.Width, GeneratedBounds.Height);
            Graphics g = Graphics.FromImage(captureBmp);
            g.CopyFromScreen(GeneratedBounds.X, GeneratedBounds.Y, 0, 0, GeneratedBounds.Size);

            //butchers this fucking image
            for (int x = 0; x < captureBmp.Width; x++)
            {
                for (int y = 0; y < captureBmp.Height; y++)
                {
                    Color pixelColor = captureBmp.GetPixel(x, y);
                    int value = pixelColor.R > 220 && pixelColor.B < 20 ? 255 : 0;
                    Color newColor = Color.FromArgb(value, value, value);
                    captureBmp.SetPixel(x, y, newColor);
                }
            }
            captureBmp.Save("Capture.png", System.Drawing.Imaging.ImageFormat.Png);
        }

        private void ShowCapture()
        {
            Pen pen = new Pen(Brushes.Red, 2);
            Graphics gr = Graphics.FromHwnd(IntPtr.Zero);

            Console.WriteLine(GeneratedBounds);
            while (1 > 0)
            {
                gr.DrawRectangle(pen, GeneratedBounds);
            }
        }

        private void GetFormBounds(object sender, FormClosingEventArgs e)
        {
            Form finder = (Form)sender;
            GeneratedBounds = finder.RectangleToScreen(finder.ClientRectangle);
        }

        public int[] GetValuesFromBound()
        {
            if (GeneratedBounds != null)
            {
                CaptureArea();
                if (HangOnCapturedBound) ShowCapture();
                return ParseImageValues(RSCTracker.GetValuesFromImg(RSCTracker.KILLIMAGE));
            }
            else
            {
                throw new InvalidOperationException("Use CreateFinder to determine bounds first");
            }
        }
        
        public static int[] ParseImageValues(string a)
        {
            RSCTracker.ToFile("Tesseract saw: " + a);
            Regex p = new Regex(@"(\d{1,6})\s(\d{1,6})\s(\d{1,6})\s(\d{1,6})");
            if (!p.IsMatch(a)) return null;
            Match m = p.Match(a);
            int[] ret = new int[4];
            for (int i = 1; i <= 4; i++)
            {
                ret[i - 1] = int.Parse(m.Groups[i].Value);
            }
            RSCTracker.ToFile("Values parsed to " + ret[0] + " " + ret[1] + " " + ret[2] + " " + ret[3]);
            return ret;
        }
    }
}
