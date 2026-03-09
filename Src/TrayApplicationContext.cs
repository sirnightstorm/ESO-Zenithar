using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Resources;
using System.Text;
using WinFormsApp;
using ZenitharClient;
using ZenitharClient.Properties;
using static System.Net.WebRequestMethods;

namespace ZenitharClient.Src
{
    public enum ClientState
    {
        Inactive,
        Active,
        Waiting,
        Error
    }

    public class TrayApplicationContext : ApplicationContext
    {
        public SavedVarsWatcher watcher;
        private NotifyIcon trayIcon;
        private SynchronizationContext synchronizationContext;

        private LogForm? logForm;
        private SettingsForm? settingsForm;

        public TrayApplicationContext()
        {
            trayIcon = new NotifyIcon()
            {
                //Icon = Resources.zenithar_star_inactive,
                ContextMenuStrip = new ContextMenuStrip()
                {
                    Items = {
                        new ToolStripMenuItem("Open", null, Open),
                        new ToolStripMenuItem("Settings", null, Settings),
                        new ToolStripMenuItem("Exit", null, Exit),
                    }
                },
                Visible = true
            };

            trayIcon.MouseDoubleClick += (sender, e) =>
            {
                if (e.Button == MouseButtons.Left)
                {
                    if (Program.config.IsValid())
                    {
                        Open(sender, e);
                    }
                    else
                    {
                        Settings(sender, e);
                    }
                }
            };


            SetIcon(Program.config.IsValid() ? ClientState.Inactive : ClientState.Error);

            watcher = new SavedVarsWatcher(this);
            StartWatcher();

            synchronizationContext = SynchronizationContext.Current!;

            //SynchronizationContext.Current?.Post(async _ =>
            //{
            //    await watcher.Start();
            //}, null);
        }
        public void StartWatcher()
        {
            Task.Run(async () => await watcher.Start());
        }

        internal void WaitForESOExit(string svFile, DB db)
        {
            Task.Run(async () =>
            {
                if (await AppExitWatcher.WaitForESOExit())
                {
                    SetTooltip("Marking transactions processed.");
                    await Task.Delay(1000);
                    SavedVarsParser.SetProcessed(svFile);
                    await db.RemoveUploadedTransactions();
                }
            });
        }

        public void StopWatcher()
        {
            watcher.Stop();
        }

        private void Settings(object? sender, EventArgs e)
        {
            if (settingsForm == null || settingsForm.IsDisposed)
            {
                settingsForm = new SettingsForm();
                settingsForm.FormClosed += (_, __) => settingsForm = null;
                settingsForm.ShowDialog();
            }
            else
            {
                settingsForm.Activate();
            }
        }

        void Open(object? sender, EventArgs e)
        {
            if (logForm == null || logForm.IsDisposed)
            {
                logForm = new LogForm();
                logForm.FormClosed += (_, __) => logForm = null;
                logForm.ShowDialog();
            }
            else
            {
                logForm.Activate();
            }
        }

        void Exit(object? sender, EventArgs e)
        {
            trayIcon.Visible = false;
            Application.Exit();
        }

        public void SetTooltip(string text)
        {
            LogForm.Log(text);

            synchronizationContext?.Post(_ =>
            {
                trayIcon.Text = "Zenithar: " + text;
            }, null);
        }

        public void SetIcon(ClientState state)
        {
            synchronizationContext?.Post(_ =>
            {
                byte[] iconData;
                switch (state)
                {
                    case ClientState.Active:
                        iconData = Resources.zenithar_star_active;
                        break;
                    case ClientState.Waiting:
                        iconData = Resources.zenithar_star_waiting;
                        break;
                    case ClientState.Error:
                        iconData = Resources.zenithar_star_error;
                        break;
                    default:
                        iconData = Resources.zenithar_star_inactive;
                        break;
                }

                using var ms = new MemoryStream(iconData);
                trayIcon.Icon = new Icon(ms);
            }, null);
        }
    }
}