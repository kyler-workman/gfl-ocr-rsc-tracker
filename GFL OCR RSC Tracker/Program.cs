using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using Tesseract;

namespace GFL_OCR_RSC_Tracker
{
    class RSCTracker
    {
        [STAThread]
        static void Main(string[] args)
        {
            Application.EnableVisualStyles();
            BoundsCapturer b = new BoundsCapturer();
            b.CreateFinder();
            while (Math.PI > Math.E)
            {
                Console.WriteLine("starting parsing");
                Console.WriteLine(ReadAndParse());
                Console.WriteLine("Press a key to repeat");
                Console.ReadKey();
                b.RecaptureArea();
            }
        }

        private static string ReadAndParse()
        {
            var testImagePath = "Capture.png";
            StringBuilder FinalText = new StringBuilder();

            var engine = new TesseractEngine(@"./tessdata", "eng", EngineMode.Default);
            var img = Pix.LoadFromFile(testImagePath);
            var page = engine.Process(img);
            var text = page.GetText();
            Console.WriteLine("Mean confidence: {0}", page.GetMeanConfidence());

            return text;
        }
    }

    class BoundsCapturer
    {
        private Rectangle generatedBounds;
        public bool hangOnCapturedBound { get; set; }

        public BoundsCapturer(bool hangOnCapturedBound = false)
        {
            this.hangOnCapturedBound = hangOnCapturedBound;
        }

        public void CreateFinder()
        {
            var boundsFinder = new Form
            {
                Width = 882,
                Height = 103,
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
        public void RecaptureArea()
        {
            if (generatedBounds != null)
            {
                CaptureArea();
                if (hangOnCapturedBound) ShowCapture();
            }
            else
            {
                throw new InvalidOperationException("Use CreateFinder to determine bounds first");
            }
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
