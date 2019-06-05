using IronOcr;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace GFL_OCR_RSC_Tracker
{
    class RSCTracker
    {
        [STAThread]
        static void Main(string[] args)
        {
            Application.EnableVisualStyles();
            BoundsCapturer b = new BoundsCapturer();
            b.DoYourShit();

            Console.WriteLine("starting parsing");
            ReadAndParse();
        }

        private static void ReadAndParse()
        {
            var Ocr = new AdvancedOcr()
            {
                DetectWhiteTextOnDarkBackgrounds = true
            };

            var testDocument = @"Capture.png";
            var Results = Ocr.Read(testDocument);
            Console.WriteLine(Results.Text);
        }
    }

    class BoundsCapturer
    {
        Rectangle generatedBounds;
        bool hangOnCapturedBound;

        public BoundsCapturer(bool hangOnCapturedBound=false)
        {
            this.hangOnCapturedBound = hangOnCapturedBound;
        }

        public void DoYourShit()
        {
            var boundsFinder = new Form
            {
                Width=882,
                Height=103,
                BackColor = Color.Firebrick,
                TransparencyKey = Color.Firebrick,
                Text = "Drag this to capture bounds, close to capture area, this can also be resized"
            };
            boundsFinder.FormClosing += GetFormBounds;
            Application.Run(boundsFinder);
            CaptureArea();
            if (hangOnCapturedBound) ShowCapture();
            Console.WriteLine(generatedBounds);
        }

        private void CaptureArea()
        {
            Bitmap captureBmp = new Bitmap(generatedBounds.Width, generatedBounds.Height);
            Graphics g = Graphics.FromImage(captureBmp);
            g.CopyFromScreen(generatedBounds.X, generatedBounds.Y, 0, 0, generatedBounds.Size);


            for (int x = 0; x < captureBmp.Width; x++)
            {
                for (int y = 0; y < captureBmp.Height; y++)
                {
                    Color pixelColor = captureBmp.GetPixel(x, y);
                    Color newColor = Color.FromArgb(pixelColor.R > 128 ? 255 : 0, pixelColor.R > 128 ? 255 : 0, pixelColor.R > 128 ? 255 : 0);
                    captureBmp.SetPixel(x, y, newColor);
                }
            }

            captureBmp.Save("Capture.png", ImageFormat.Png);
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

    }
}
