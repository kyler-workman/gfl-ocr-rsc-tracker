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
using Newtonsoft.Json;
using Google.Apis.Sheets.v4;
using Google.Apis.Auth.OAuth2;
using System.IO;
using Google.Apis.Util.Store;
using Google.Apis.Services;
using Google.Apis.Sheets.v4.Data;
using System.Timers;

namespace GFL_OCR_RSC_Tracker
{
    class RSCTracker
    {
        public static bool LOG = false;
        private static string STAR = "Best";
        public static TrackerConfig cfg;

        [STAThread]
        static void Main(string[] args)
        {
            LOG = args.Contains("log");
            cfg = TrackerConfig.GetConfig();
            BoundsCapturer b = new BoundsCapturer();

            Application.EnableVisualStyles();
            int[] values = null;
            bool accepted = false;
            while (!accepted)
            {
                values = null;
                while (values == null)
                {
                    b.CreateFinder();
                    if (LOG) Console.WriteLine("starting parsing");
                    values = b.GetValuesFromBound();
                    if (values == null && LOG) Console.WriteLine("Couldn't find values, reopening capture window");
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

            System.Timers.Timer adjutant = new System.Timers.Timer();
            adjutant.Elapsed += (s, a) => TryWrite(s, a, b);
            adjutant.Interval = 1000 * 60 * 60 * 1; //60 minute interval

            TryWrite(null, null, b);
            adjutant.Start();

            while (STAR == "Best");
        }

        private static void TryWrite(object sender, ElapsedEventArgs e, BoundsCapturer b)
        {
            int[] values = b.GetValuesFromBound();
            if (values == null)
            {
                if (LOG) Console.WriteLine("Bad values, skipping");
                return;
            }

            var response = PushData(values);
            if (LOG) Console.WriteLine(JsonConvert.SerializeObject(response));
        }

        private static BatchUpdateValuesResponse PushData(int[] values, bool ignoredatevalidation = false)
        {
            SpreadSheetConnector conn = new SpreadSheetConnector(cfg.SpreadSheetId);
            DateTime now = DateTime.Now;
            if (!ignoredatevalidation && now < cfg.LastPushDate.AddDays(1).Date)
            {
                if (LOG) Console.WriteLine("Day has not passed, not writing to Sheet");
                return null;
            }

            List<object> ToIn = new List<object>() { now.ToShortDateString() };
            ToIn.AddRange(values.Cast<object>());
            var response = conn.UpdateData(
                cfg.SheetName,
                cfg.PushToColumn,
                cfg.LastPushLine + 1,
                new List<IList<object>>()
                {
                    ToIn
                });
            if (response.TotalUpdatedCells != null && response.TotalUpdatedCells > 0)
            {
                cfg.LastPushDate = now;
                cfg.LastPushLine++;
            }
            cfg.UpdateConfig();
            return response;
        }

        private static void TestWritingToSheets()
        {
            SpreadSheetConnector conn = new SpreadSheetConnector("1mnJfrsSXrCRLTrqeqdQ5b2uf05MZMp2bknHNTndgqqg");
            Console.WriteLine(conn.UpdateData("TestSheet", "A", 1, new List<IList<object>>()
            {
                new List<object>()
                {
                    DateTime.Now,
                    41
                },
                new List<object>()
                {
                    "test",
                    42
                }
            }));
        }

        public static int[] ParseImageValues(string a)
        {
            if (LOG) Console.WriteLine("Tesseract saw: " + a);
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

        public static string GetValuesFromImg(bool AR15 = false)
        {
            var testImagePath = "Capture.png";
            var engine = new TesseractEngine(@"./tessdata", "eng", EngineMode.Default);
            var img = Pix.LoadFromFile(testImagePath);
            var page = engine.Process(img);
            var text = page.GetText();
            if (LOG) Console.WriteLine("Mean confidence: {0}", page.GetMeanConfidence());

            if (AR15) File.Delete(testImagePath);

            return text.Replace('\n', ' ').Replace("  ", " ");
        }
    }

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
                return RSCTracker.ParseImageValues(RSCTracker.GetValuesFromImg());
            }
            else
            {
                throw new InvalidOperationException("Use CreateFinder to determine bounds first");
            }
        }
    }
}