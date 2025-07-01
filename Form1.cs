namespace FpsOSC
{
    using CoreOSC;
    using FpsOSC.RTSS;
    using System.Net.Sockets;
    using System.Runtime.InteropServices;
    using Timer = System.Windows.Forms.Timer;

    public partial class Form1 : Form
    {
        private static RTSSSM rtss = new RTSSSM();
        private static UDPSender sender = new CoreOSC.UDPSender("127.0.0.1", 9000);
        private static UDPListener reciever = new UDPListener(9001);
        private static bool typingCheck = true;
        private static bool notTyping = true;

        // Import user32.dll functions
        [DllImport("user32.dll")]
        public static extern int SendMessage(IntPtr hWnd, int Msg, int wParam, int lParam);

        [DllImport("user32.dll")]
        public static extern bool ReleaseCapture();

        public Form1()
        {
            InitializeComponent();

            // Attach MouseDown event handler to the Form
            this.MouseDown += new MouseEventHandler(Form_MouseDown);

            // Optionally, attach to all child controls if you want to drag from them too:
            // AttachMouseDownToControls(this.Controls);
        }
        private void Form_MouseDown(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left)
            {
                ReleaseCapture();
                SendMessage(this.Handle, 0xA1, 0x2, 0);
            }
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            button2.BackColor = Color.Transparent;
            pictureBox1.Load("https://avatars.githubusercontent.com/u/69168805?");
            pictureBox1.SizeMode = PictureBoxSizeMode.Zoom;
            button2.FlatAppearance.BorderSize = 0;
            Task.Run(() => UDPListener());
            /*            Timer recieve = new Timer();
                        recieve.Interval = 1000;
                        recieve.Tick += (s, ea) =>
                        {
                        };*/
            //No idea how to recieve every UDP packet live yet.
            //recieve.Start();
            Timer timer = new Timer();
            timer.Interval = 3000;
            timer.Tick += (s, ea) =>
            {
                try
                {
                    rtss.Update();
                    UpdateStats("VRChat.exe");
                }
                catch (Exception ex)
                {
                    Console.WriteLine("Error: " + ex.Message);
                }
            };
            rtss.Start();
            timer.Start();
        }
        void UDPListener()
        {
            //chatbox/typing b just doenst exsist???????
        }
        void UpdateStats(string exe)
        {
            foreach (var Application in rtss.APPEntries)
            {
                if (Application.szName != null && Application.szName.Contains(exe) && (typingCheck == notTyping))
                {
                    var message = new OscMessage(
                        "/chatbox/input",
                        $"{textBox2.Text}{(Application.dwFrames)}",
                        true,
                        false);

                    sender.Send(message);
                    break;
                }
            }
        }

        private void pictureBox1_Click(object sender, EventArgs e)
        {
            System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
            {
                FileName = "https://denelix.github.io/",
                UseShellExecute = true
            });
        }
        private void checkBox1_CheckedChanged(object sender, EventArgs e)
        {
            typingCheck = checkBox1.Checked;
        }

        private void button2_Click(object sender, EventArgs e)
        {
            Application.Exit();
        }

        private void button1_Click(object sender, EventArgs e)
        {

        }

        private void textBox1_TextChanged(object sender, EventArgs e)
        {

        }

        private void textBox2_TextChanged(object sender, EventArgs e)
        {

        }

        private void label1_Click(object sender, EventArgs e)
        {

        }

    }
}
