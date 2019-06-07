using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Windows.Forms;
using Tesseract;
using Newtonsoft.Json;
using System.IO;
using Google.Apis.Sheets.v4.Data;
using System.Timers;

namespace GFL_OCR_RSC_Tracker
{
    class RSCTracker
    {
        public static bool LOG = false;
        public static TrackerConfig cfg;

        public const bool KILLIMAGE = true;

        [STAThread]
        static void Main(string[] args)
        {
            var waitHandle = new EventWaitHandle(false, EventResetMode.AutoReset, "GFL-OCR-RSC-Tracker", out bool createdNew);
            var signaled = false;
            
            if (!createdNew)
            {
                waitHandle.Set();
                return;
            }

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

            do
            {
                signaled = waitHandle.WaitOne();
            } while (!signaled);
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
}