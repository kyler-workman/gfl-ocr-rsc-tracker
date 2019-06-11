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
        public static string LogFile;
        public static TrackerConfig cfg;

        public static System.Timers.Timer Adjutant;
        public static System.Timers.Timer Pusher;

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

            LogFile = "logs\\" + DateTime.Now.ToString("yyyyMMddHHmmss") + ".txt";
            Directory.CreateDirectory("logs");
            File.Create(LogFile).Dispose();

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
                    ToFile(b.GeneratedBounds.ToString());
                    ToFile("Starting parsing");
                    values = b.GetValuesFromBound();
                    if (values == null) ToFile("Couldn't find values, reopening capture window");
                }

                DialogResult r = MessageBox.Show("Manpower\tAmmo\t\tRations\t\tParts\n" + values[0] + "\t\t" + values[1] + "\t\t" + values[2] + "\t\t" + values[3]
                    + "\n\nDo these values look correct?", "Value Confirmation", MessageBoxButtons.YesNoCancel);
                if (r == DialogResult.Cancel) Environment.Exit(0);
                accepted = r == DialogResult.Yes;
            }

            Pusher = new System.Timers.Timer();
            Pusher.Elapsed += (s, a) => TryWrite(s, a, b);
            Pusher.Interval = 1000 * 60 * 1; //1 minute interval

            Adjutant = new System.Timers.Timer();
            Adjutant.Elapsed += StartTrying;
            Adjutant.Interval = (DateTime.Today.AddDays(1) - DateTime.Now).TotalMilliseconds; //runs midnight tomorrow
            Adjutant.AutoReset = false;
            Adjutant.Start();


            TryWrite(null, null, b);

            do
            {
                signaled = waitHandle.WaitOne();
            } while (!signaled);
        }

        public static void ToFile(string str)
        {
            try
            {

                using (StreamWriter w = File.AppendText(LogFile))
                {
                    w.WriteLine(DateTime.Now.ToLongTimeString() + " " + DateTime.Now.ToLongDateString() + " : " + str);
                    w.Close();
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }
        }

        private static void StartTrying(object s, ElapsedEventArgs a)
        {
            Pusher.Start();
        }

        private static void TryWrite(object sender, ElapsedEventArgs e, BoundsCapturer b)
        {
            int[] values = b.GetValuesFromBound();
            if (values == null)
            {
                ToFile("Bad values, skipping");
                return;
            }

            try
            {
                var response = PushData(values);
                ToFile(JsonConvert.SerializeObject(response));
                if (response != null && response.TotalUpdatedCells != null && response.TotalUpdatedCells > 0)
                {
                    Pusher.Stop();
                    Adjutant.Stop();
                    Adjutant.Interval = (DateTime.Today.AddDays(1) - DateTime.Now).TotalMilliseconds;
                    Adjutant.Start();
                }
            }
            catch (Exception ex)
            {
                ToFile(ex.Message);
                ToFile("Config file uninitialized");
                Environment.Exit(1);
            }
        }

        private static BatchUpdateValuesResponse PushData(int[] values, bool ignoredatevalidation = false)
        {
            SpreadSheetConnector conn = new SpreadSheetConnector(cfg.SpreadSheetId);
            DateTime now = DateTime.Now;
            if (!ignoredatevalidation && now < cfg.LastPushDate.AddDays(1).Date)
            {
                ToFile("Day has not passed, not writing to Sheet");
                return null;
            }

            List<object> ToIn = new List<object>() { now.ToShortDateString() };
            ToIn.AddRange(values.Cast<object>());
            ToIn.Add("");
            ToIn.Add("Auto push at " + now.ToString("H:mm"));

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
            ToFile("Tesseract saw: " + a);
            Regex p = new Regex(@"(\d{1,6})\s(\d{1,6})\s(\d{1,6})\s(\d{1,6})");
            if (!p.IsMatch(a)) return null;
            Match m = p.Match(a);
            int[] ret = new int[4];
            for (int i = 1; i <= 4; i++)
            {
                ret[i - 1] = int.Parse(m.Groups[i].Value);
            }
            ToFile("Values parsed to " + ret[0] + " " + ret[1] + " " + ret[2] + " " + ret[3]);
            return ret;
        }

        public static string GetValuesFromImg(bool AR15 = false)
        {
            var testImagePath = "Capture.png";
            var engine = new TesseractEngine(@"./tessdata", "eng", EngineMode.Default);
            var img = Pix.LoadFromFile(testImagePath);
            var page = engine.Process(img);
            var text = page.GetText();
            ToFile("Mean confidence: " + page.GetMeanConfidence());

            if (AR15) File.Delete(testImagePath);

            return text.Replace('\n', ' ').Replace("  ", " ");
        }
    }
}