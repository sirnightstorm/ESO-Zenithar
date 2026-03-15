using ZenitharClient.Properties;

namespace ZenitharClient.Src
{
    public class TrayApplicationContext : ApplicationContext, IObserver<Service>
    {
        internal SavedVarsWatcher watcher;
        private NotifyIcon trayIcon;
        private SynchronizationContext synchronizationContext;

        private DB db;

        private LogForm? logForm;
        private SettingsForm? settingsForm;

        private List<Service> services = new List<Service>();

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

            string dbPath = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "Zenithar", "app.sqlite"
            );
            this.db = new DB(dbPath);

            Update();
            //SetIcon(Program.config.IsValid() ? ServiceState.Idle : ServiceState.Error);

            watcher = new SavedVarsWatcher(this, db);

            synchronizationContext = SynchronizationContext.Current!;

            watcher.Subscribe(this);
            services.Add(watcher);

            db.Subscribe(this);
            services.Add(db);

            JSONUploader.Instance.Subscribe(this);
            services.Add(JSONUploader.Instance);

            StartWatcher();
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

        public void Update()
        {
            var overallState = ServiceState.Idle;
            var tooltipList = new List<string>();

            if (!Program.config.IsValid())
            {
                overallState = ServiceState.Error;
                tooltipList.Add("Configuration is invalid. Please open settings.");
            }

            foreach (var service in services)
            {
                if ((int)service.State > (int)overallState)
                {
                    overallState = service.State;
                }
                if (service.Activity != null)
                {
                    tooltipList.Add(service.Activity);
                }
                else
                {
                    tooltipList.Add(service.State.ToString());
                }
            }

            SetIcon(overallState);
            SetTooltip(string.Join("\n", tooltipList));
        }

        private void SetTooltip(string text)
        {
            synchronizationContext?.Post(_ =>
            {
                trayIcon.Text = "Zenithar:\n" + text;
            }, null);
        }

        private void SetIcon(ServiceState state)
        {
            synchronizationContext?.Post(_ =>
            {
                byte[] iconData;
                switch (state)
                {
                    case ServiceState.Active:
                        iconData = Resources.zenithar_star_active;
                        break;
                    case ServiceState.Waiting:
                        iconData = Resources.zenithar_star_waiting;
                        break;
                    case ServiceState.Error:
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

        public virtual void OnCompleted()
        {
        }

        public virtual void OnError(Exception error)
        {
        }

        public virtual void OnNext(Service value)
        {
            Update();
            //Console.WriteLine($"The temperature is {value.Degrees}°C at {value.Date:g}");
            //if (first)
            //{
            //    last = value;
            //    first = false;
            //}
            //else
            //{
            //    Console.WriteLine($"   Change: {value.Degrees - last.Degrees}° in {value.Date.ToUniversalTime() - last.Date.ToUniversalTime():g}");
            //}
        }
    }
}