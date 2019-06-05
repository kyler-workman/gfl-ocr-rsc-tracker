using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using Tesseract;

namespace GFL_OCR_RSC_Tracker
{
    class RSCTracker
    {
        public static bool LOG=false;
        [STAThread]
        static void Main(string[] args)
        {
            LOG = args.Contains("log");

            Application.EnableVisualStyles();
            int[] values;
            bool accepted = false;
            while (!accepted)
            {
                values = null;
                while (values == null)
                {
                    BoundsCapturer b = new BoundsCapturer();
                    b.CreateFinder();
                    if (LOG) Console.WriteLine("starting parsing");
                    values = ParseValues(GetValuesFromImg());
                    if (values == null) Console.WriteLine("Couldn't find values, reopening capture window");
                }
                if (LOG)
                {
                    Array.ForEach(values, x =>
                    {
                        Console.Write(x + " ");
                    });
                    Console.WriteLine();
                }

                DialogResult r = MessageBox.Show("Manpower\tAmmo\t\tRations\t\tParts\n" + values[0] + "\t\t" + values[1] + "\t\t" + values[2] + "\t\t" + values[3]
                    + "\n\nDo these values look correct?", "Value Confirmation", MessageBoxButtons.YesNoCancel);
                if (r == DialogResult.Cancel) Environment.Exit(0);
                accepted = r == DialogResult.Yes;
            }
        }

        private static int[] ParseValues(string a)
        {
            if (LOG) Console.WriteLine("Tesseract saw: "+a);
            Regex p = new Regex(@"(\d{1,6})\s(\d{1,6})\s(\d{1,6})\s(\d{1,6})");
            if (!p.IsMatch(a)) return null;
            Match m = p.Match(a);
            int[] ret = new int[4];
            for (int i = 1; i <= 4; i++)
            {
                ret[i - 1] = int.Parse(m.Groups[i].Value);
            }
            return ret;
        }

        private static string GetValuesFromImg()
        {
            var testImagePath = "Capture.png";
            var engine = new TesseractEngine(@"./tessdata", "eng", EngineMode.Default);
            var img = Pix.LoadFromFile(testImagePath);
            var page = engine.Process(img);
            var text = page.GetText();
            if (LOG) Console.WriteLine("Mean confidence: {0}", page.GetMeanConfidence());

            return text.Replace('\n', ' ').Replace("  ", " ");
        }
    }

    class BoundsCapturer
    {
        private Rectangle generatedBounds;
        public bool HangOnCapturedBound { get; set; }

        public BoundsCapturer(bool hangOnCapturedBound = false)
        {
            this.HangOnCapturedBound = hangOnCapturedBound;
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
            if (HangOnCapturedBound) ShowCapture();
            if (RSCTracker.LOG) Console.WriteLine(generatedBounds);
        }
        public void RecaptureArea()
        {
            if (generatedBounds != null)
            {
                CaptureArea();
                if (HangOnCapturedBound) ShowCapture();
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
