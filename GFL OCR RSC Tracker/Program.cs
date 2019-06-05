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
        Rectangle generatedBounds;

        [STAThread]
        static void Main(string[] args)
        {
            RSCTracker t = new RSCTracker();
            Application.EnableVisualStyles();
            var boundsFinder = new Form
            {
                BackColor = Color.Firebrick,
                TransparencyKey = Color.Firebrick,
                Text = "Drag this to capture bounds"
            };
            boundsFinder.FormClosing += t.GetFormBounds;
            Application.Run(boundsFinder);
            //t.ShowCapture();
            t.CaptureArea();
        }

        private void CaptureArea()
        {
            Bitmap captureBmp = new Bitmap(generatedBounds.Width, generatedBounds.Height);
            Graphics g = Graphics.FromImage(captureBmp);
            g.CopyFromScreen(generatedBounds.X, generatedBounds.Y, 0, 0, generatedBounds.Size);
            captureBmp.Save("Capture.png", ImageFormat.Png);
            Console.WriteLine("done");
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
