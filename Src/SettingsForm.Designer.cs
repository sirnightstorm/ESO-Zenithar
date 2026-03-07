namespace ZenitharClient.Src
{
    partial class SettingsForm
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Clean up any resources being used.
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
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            groupBox1 = new GroupBox();
            txtGuildToken = new TextBox();
            label2 = new Label();
            txtServerEndpoint = new TextBox();
            label1 = new Label();
            groupBox1.SuspendLayout();
            SuspendLayout();
            // 
            // groupBox1
            // 
            groupBox1.Controls.Add(txtGuildToken);
            groupBox1.Controls.Add(label2);
            groupBox1.Controls.Add(txtServerEndpoint);
            groupBox1.Controls.Add(label1);
            groupBox1.Location = new Point(12, 12);
            groupBox1.Name = "groupBox1";
            groupBox1.Size = new Size(400, 136);
            groupBox1.TabIndex = 0;
            groupBox1.TabStop = false;
            groupBox1.Text = "Server Settings";
            // 
            // txtGuildToken
            // 
            txtGuildToken.Location = new Point(15, 93);
            txtGuildToken.Margin = new Padding(12, 3, 12, 3);
            txtGuildToken.Name = "txtGuildToken";
            txtGuildToken.Size = new Size(370, 23);
            txtGuildToken.TabIndex = 3;
            txtGuildToken.TextChanged += txtGuildToken_TextChanged;
            // 
            // label2
            // 
            label2.AutoSize = true;
            label2.Location = new Point(15, 75);
            label2.Margin = new Padding(12, 6, 12, 0);
            label2.Name = "label2";
            label2.Size = new Size(70, 15);
            label2.TabIndex = 2;
            label2.Text = "Guild Token";
            // 
            // txtServerEndpoint
            // 
            txtServerEndpoint.Location = new Point(15, 43);
            txtServerEndpoint.Margin = new Padding(12, 3, 12, 3);
            txtServerEndpoint.Name = "txtServerEndpoint";
            txtServerEndpoint.Size = new Size(370, 23);
            txtServerEndpoint.TabIndex = 1;
            txtServerEndpoint.TextChanged += txtServerEndpoint_TextChanged;
            // 
            // label1
            // 
            label1.AutoSize = true;
            label1.Location = new Point(15, 25);
            label1.Margin = new Padding(12, 6, 12, 0);
            label1.Name = "label1";
            label1.Size = new Size(90, 15);
            label1.TabIndex = 0;
            label1.Text = "Server Endpoint";
            // 
            // SettingsForm
            // 
            AutoScaleDimensions = new SizeF(7F, 15F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(424, 159);
            Controls.Add(groupBox1);
            Name = "SettingsForm";
            Text = "SettingsForm";
            FormClosed += SettingsForm_FormClosed;
            Load += SettingsForm_Load;
            groupBox1.ResumeLayout(false);
            groupBox1.PerformLayout();
            ResumeLayout(false);
        }

        #endregion

        private GroupBox groupBox1;
        private Label label1;
        private TextBox txtGuildToken;
        private Label label2;
        private TextBox txtServerEndpoint;
    }
}