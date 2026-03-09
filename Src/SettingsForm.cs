using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Configuration;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.Text;
using System.Windows.Forms;

namespace ZenitharClient.Src
{
    internal partial class SettingsForm : Form
    {
        private bool hadValidSettingsOnLoad = Program.config.IsValid();

        internal SettingsForm()
        {
            InitializeComponent();
        }

        private void SettingsForm_Load(object sender, EventArgs e)
        {
            // Load settings into the DataTable  
            LoadSettings();
        }

        private void LoadSettings()
        {
            txtServerEndpoint.Text = Program.config.ServerEndpoint;
            txtGuildToken.Text = Program.config.GuildToken;

            //settingsTable.Rows.Clear(); // Clear existing data  

            //// Get all app settings and add to DataTable  
            ////foreach (string key in Properties.Settings.Default.AllKeys)
            ////{
            ////    settingsTable.Rows.Add(key, ConfigurationManager.AppSettings[key]);
            ////}
            //settingsTable.Rows.Add("Server Endpoint", Properties.Settings.Default.ServerEndpoint);
            //settingsTable.Rows.Add("Guild Token", Properties.Settings.Default.GuildToken);

            //// Bind DataTable to DataGridView  
            //dgvSettings.DataSource = settingsTable;
            //dgvSettings.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
            ////lblStatus.Text = $"Loaded {settingsTable.Rows.Count} settings.";
            //}
            //catch (Exception ex)
            //{
            //    //lblStatus.Text = $"Error loading settings: {ex.Message}";
            //}
        }

        private void txtServerEndpoint_TextChanged(object sender, EventArgs e)
        {
        }

        private void txtGuildToken_TextChanged(object sender, EventArgs e)
        {
        }

        private void SettingsForm_FormClosed(object sender, FormClosedEventArgs e)
        {
        }

        private void SettingsForm_FormClosing(object sender, FormClosingEventArgs e)
        {
            Program.config.ServerEndpoint = txtServerEndpoint.Text;
            Program.config.GuildToken = txtGuildToken.Text;
            Program.config.Save();

            if (!hadValidSettingsOnLoad && Program.config.IsValid())
            {
                Program.context?.StartWatcher();
            }
            else if (!Program.config.IsValid())
            {
                Program.context?.SetIcon(ClientState.Error);
                Program.context?.SetTooltip("Invalid settings");
            }
        }
    }
}
