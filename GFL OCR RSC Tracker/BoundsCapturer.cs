using System;
using System.Drawing;
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
                BackColor = System.Drawing.Color.Firebrick,
                TransparencyKey = System.Drawing.Color.Firebrick,
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
                    System.Drawing.Color pixelColor = captureBmp.GetPixel(x, y);
                    int value = pixelColor.R > 220 && pixelColor.B < 20 ? 255 : 0;
                    System.Drawing.Color newColor = System.Drawing.Color.FromArgb(value, value, value);
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
                return RSCTracker.ParseImageValues(RSCTracker.GetValuesFromImg(RSCTracker.KILLIMAGE));
            }
            else
            {
                throw new InvalidOperationException("Use CreateFinder to determine bounds first");
            }
        }
    }
}
