using Google.Apis.Auth.OAuth2;
using Google.Apis.Services;
using Google.Apis.Sheets.v4;
using Google.Apis.Sheets.v4.Data;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GFL_OCR_RSC_Tracker
{
    public class SpreadSheetConnector
    {
        private string[] Scopes = { SheetsService.Scope.Spreadsheets };
        private string ApplicationName = "GFL OCR RSC Tracker";
        private string SpreadsheetId;
        private SheetsService SheetsService;

        public SpreadSheetConnector(string sheetid)
        {
            SpreadsheetId = sheetid;
            ConnectToGoogle();
        }

        private void ConnectToGoogle()
        {
            GoogleCredential credential;
            
            using (FileStream stream = new FileStream(@"credentials.json",
                FileMode.Open, FileAccess.Read))
            {
                credential = GoogleCredential.FromStream(stream).CreateScoped(Scopes);
            }
            
            SheetsService = new SheetsService(new BaseClientService.Initializer()
            {
                HttpClientInitializer = credential,
                ApplicationName = ApplicationName
            });
        }
        
        public BatchUpdateValuesResponse UpdateData(string sheetname, string column, int row, List<IList<object>> data)
        {
            List<ValueRange> updateData = new List<ValueRange>();
            var dataValueRange = new ValueRange();
            dataValueRange.Range = $"{sheetname}!{column.ToUpper()}{row}";
            dataValueRange.Values = data;
            updateData.Add(dataValueRange);

            BatchUpdateValuesRequest requestBody = new BatchUpdateValuesRequest();
            requestBody.ValueInputOption = "USER_ENTERED";
            requestBody.Data = updateData;

            var request = SheetsService.Spreadsheets.Values.BatchUpdate(requestBody, SpreadsheetId);

            BatchUpdateValuesResponse response = request.Execute();

            return response;
        }
    }
}
