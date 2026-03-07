namespace WinFormsApp
{
    partial class LogForm
    {
        /// <summary>
        ///  Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        ///  Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        ///  Required method for Designer support - do not modify
        ///  the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            logTextBox = new RichTextBox();
            SuspendLayout();
            // 
            // logTextBox
            // 
            logTextBox.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
            logTextBox.BackColor = Color.Black;
            logTextBox.BorderStyle = BorderStyle.None;
            logTextBox.Font = new Font("Consolas", 11F);
            logTextBox.ForeColor = Color.White;
            logTextBox.Location = new Point(0, 0);
            logTextBox.Name = "logTextBox";
            logTextBox.ReadOnly = true;
            logTextBox.Size = new Size(914, 545);
            logTextBox.TabIndex = 0;
            logTextBox.Text = "";
            // 
            // LogForm
            // 
            AutoScaleDimensions = new SizeF(7F, 15F);
            AutoScaleMode = AutoScaleMode.Font;
            BackColor = Color.Black;
            ClientSize = new Size(911, 543);
            Controls.Add(logTextBox);
            Name = "LogForm";
            Text = "Zenithar";
            ResumeLayout(false);
        }

        #endregion

        private RichTextBox logTextBox;
    }
}
