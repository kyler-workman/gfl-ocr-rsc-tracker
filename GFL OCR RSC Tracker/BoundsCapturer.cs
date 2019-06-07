using System;
using System.Drawing;
using System.Windows.Forms;

namespace GFL_OCR_RSC_Tracker
{
    class BoundsCapturer
    {
        private Rectangle generatedBounds;
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
            if (RSCTracker.LOG) Console.WriteLine(generatedBounds);
        }

        private void CaptureArea()
        {
            Bitmap captureBmp = new Bitmap(generatedBounds.Width, generatedBounds.Height);
            Graphics g = Graphics.FromImage(captureBmp);
            g.CopyFromScreen(generatedBounds.X, generatedBounds.Y, 0, 0, generatedBounds.Size);

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

            Console.WriteLine(generatedBounds);
            while (1 > 0)
            {
                gr.DrawRectangle(pen, generatedBounds);
            }
        }

        private void GetFormBounds(object sender, FormClosingEventArgs e)
        {
            Form finder = (Form)sender;
            generatedBounds = finder.RectangleToScreen(finder.ClientRectangle);
        }

        public int[] GetValuesFromBound()
        {
            if (generatedBounds != null)
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
