using Newtonsoft.Json;
using System;
using System.IO;

namespace GFL_OCR_RSC_Tracker
{
    public class TrackerConfig
    {
        public DateTime LastPushDate { get; set; }
        public int LastPushLine { get; set; }
        public string PushToColumn { get; set; }
        public string SheetName { get; set; }
        public string SpreadSheetId { get; set; }

        public static TrackerConfig GetConfig()
        {
            if (!File.Exists("config.json")) InitializeConfig();
            return JsonConvert.DeserializeObject<TrackerConfig>(File.ReadAllText("config.json"));
        }

        private static TrackerConfig InitializeConfig()
        {
            var t = new TrackerConfig()
            {
                LastPushDate = DateTime.MinValue.Date,
                LastPushLine = 0,
                PushToColumn = "A",
                SheetName = "",
                SpreadSheetId = ""
            };

            File.WriteAllText("config.json", JsonConvert.SerializeObject(t,Formatting.Indented));
            return t;
        }
        public void UpdateConfig()
        {
            File.WriteAllText("config.json", JsonConvert.SerializeObject(this, Formatting.Indented));
        }
    }
}