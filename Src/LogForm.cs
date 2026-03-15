namespace ZenitharClient.Src
{
    public partial class LogForm : Form
    {
        private SynchronizationContext uiContext;

        public static RollingStringBuffer LogBuffer = new RollingStringBuffer();
        public string LogBufferText => string.Join(Environment.NewLine, LogBuffer.GetItems());

        //public static event EventHandler? LogTextChanged;

        static WeakReference<LogForm>? frm = null;

        public LogForm()
        {
            frm = new WeakReference<LogForm>(this);

            InitializeComponent();

            uiContext = SynchronizationContext.Current ?? throw new InvalidOperationException("No SynchronizationContext found. Ensure this form is created on the UI thread.");

            this.FormClosing += (sender, e) =>
            {
                frm = null;
            };

            UpdateLogText();

            // Bind TextBox.Text to BufferText
            //logTextBox.DataBindings.Add("Text", this, nameof(LogBufferText));

            //logTextBox.TextChanged += (_, __) =>
            //{
            //    logTextBox.Text = LogBufferText;
            //    logTextBox.SelectionStart = logTextBox.TextLength;
            //    logTextBox.ScrollToCaret();
            //};

            //LogTextChanged += (_, __) => Notify(nameof(logTextBox));
        }

        private void richTextBox1_TextChanged(object sender, EventArgs e)
        {

        }

        public static void Log(string message)
        {
            string timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");

            LogBuffer.Add($"{timestamp}: {message}");

            if (frm != null && frm.TryGetTarget(out var target))
            {
                target.Invoke((MethodInvoker)(() =>
                {
                    target.UpdateLogText();
                }));
            }
        }

        public void UpdateLogText()
        {
            logTextBox.Text = LogBufferText;
            logTextBox.SelectionStart = logTextBox.TextLength;
            logTextBox.ScrollToCaret();
        }

        //private void logTextBox_KeyDown(object sender, KeyEventArgs e)
        //{
        //    Log("Key down: " + e.KeyCode);
        //}

        /*public void AddLog(string message, Color? color = null)
        {
            TrayApplicationContext.logBuffer.Add(message);

            buffer.Add(message);

            string result = string.Join("\n", buffer.GetItems());

            uiContext.Post(_ =>
            {
                int start = logTextBox.TextLength;
                logTextBox.Text = result;
                logTextBox.Select(start, message.Length);
                logTextBox.SelectionColor = color ?? Color.White;
                logTextBox.SelectionLength = 0;
                logTextBox.ScrollToCaret();
            }, null);
        }*/

    }
}
