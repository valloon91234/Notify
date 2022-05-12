using Newtonsoft.Json.Linq;
using System;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Text;
using System.Threading;
using System.Windows.Forms;

namespace Notify
{
    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();
            RunThread();
        }

        private void notifyIcon1_MouseDoubleClick(object sender, MouseEventArgs e)
        {
            this.Show();
            this.Activate();
        }

        private static readonly string URL = "https://api.coingecko.com/api/v3/simple/price?ids=solana,tether&vs_currencies=usd";
        private static decimal Price;
        private WMPLib.WindowsMediaPlayer Player = new WMPLib.WindowsMediaPlayer();

        private void PlayFile(String url)
        {
            Player = new WMPLib.WindowsMediaPlayer();
            Player.PlayStateChange += Player_PlayStateChange;
            Player.URL = url;
            Player.controls.play();
        }

        private void Player_PlayStateChange(int NewState)
        {
            if ((WMPLib.WMPPlayState)NewState == WMPLib.WMPPlayState.wmppsStopped)
            {
                //Actions on stop
            }
        }

        private void timer1_Tick(object sender, EventArgs e)
        {
            if (Price == 0)
            {
                this.Text = "Offline";
                return;
            }
            this.Text = Price.ToString("N2");
            if (Price <= numericUpDown1.Value)
            {
                notifyIcon1.ShowBalloonTip(0, $"{Price:N2}\r\n", $"{Price:N2} <= {numericUpDown1.Value:N2}", ToolTipIcon.Info);
                FlashWindow.Flash(this);
                PlayFile("down.mp3");
            }
            if (Price >= numericUpDown2.Value)
            {
                notifyIcon1.ShowBalloonTip(0, $"{Price:N2}\r\n", $"{Price:N2} >= {numericUpDown2.Value:N2}", ToolTipIcon.Info);
                FlashWindow.Flash(this);
                PlayFile("up.mp3");
            }
        }

        public static string HttpGet(string url)
        {
            var httpWebRequest = (HttpWebRequest)WebRequest.Create(url);
            httpWebRequest.Timeout = 15000;
            httpWebRequest.ReadWriteTimeout = 15000;
            httpWebRequest.Method = "Get";
            using (var httpWebResponse = (HttpWebResponse)httpWebRequest.GetResponse())
            {
                string Charset = httpWebResponse.CharacterSet;
                using (var receiveStream = httpWebResponse.GetResponseStream())
                using (var streamReader = new StreamReader(receiveStream, Encoding.GetEncoding(Charset)))
                    return streamReader.ReadToEnd();
            }
        }

        Thread syncThread;

        public void RunThread()
        {
            if (syncThread == null)
            {
                syncThread = new Thread(() => Run());
                syncThread.Start();
            }
            else
            {
                try
                {
                    syncThread.Abort();
                }
                catch (Exception) { }
                syncThread = null;
            }
        }

        public void Run()
        {
            while (true)
            {
                try
                {
                    string response = HttpGet(URL);
                    JObject responseJson = JObject.Parse(response);
                    Price = (decimal)responseJson["solana"]["usd"];
                }
                catch (Exception)
                {
                    Price = 0;
                    //notifyIcon1.ShowBalloonTip(0, "Error !\r\n", ex.ToString(), ToolTipIcon.Error);
                }
                Thread.Sleep(5000);
            }
        }

        private void Form1_Activated(object sender, EventArgs e)
        {
            Player.controls.stop();
        }

        public void Exit()
        {
            notifyIcon1.Visible = false;
            Process.GetCurrentProcess().Kill();
        }

        private void Form1_FormClosed(object sender, FormClosedEventArgs e)
        {
            Exit();
        }

        private void checkBox1_CheckedChanged(object sender, EventArgs e)
        {
            this.TopMost = checkBox1.Checked;
        }
    }
}
