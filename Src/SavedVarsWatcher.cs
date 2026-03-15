using System.Text.Json;

namespace ZenitharClient.Src
{
    internal class SavedVarsWatcher : Service
    {
        private readonly TrayApplicationContext context;

        private FileSystemWatcher? watcher;
        private readonly Dictionary<string, System.Timers.Timer> _timers = new();

        private DB db;

        internal SavedVarsWatcher(TrayApplicationContext context, DB db) : base("SavedVars")
        {
            this.context = context;
            this.db = db;
        }

        internal async Task Start()
        {
            var svPath = GetSavedVarsPath();

            if (Program.config.IsValid())
            {
                await Process(Path.Combine(svPath, "Zenithar.lua"));
            }
            //else
            //{
            //    context.SetIcon(ClientState.Error);
            //    context.SetTooltip("Invalid Settings");
            //}

            watcher = new FileSystemWatcher();

            watcher.Path = svPath;      // Folder to watch
            watcher.Filter = "Zenithar.lua";            // File to watch
            watcher.NotifyFilter = NotifyFilters.LastWrite | NotifyFilters.Size;

            watcher.Changed += OnChanged;
            watcher.Created += OnChanged;

            watcher.EnableRaisingEvents = true;
        }

        internal void Stop()
        {
            watcher?.Dispose();
            foreach (var timer in _timers.Values)
            {
                timer.Dispose();
            }
            _timers.Clear();
        }

        private async Task Process(string svFile)
        {
            SetState(ServiceState.Active, "Reading transactions...");

            try
            {
                var data = SavedVarsParser.ParseSavedVars(svFile);
                if (data != null)
                {
                    var language = SavedVarsProcessor.GetLanguage(data);

                    if (!SavedVarsParser.IsProcessed(data))
                    {
                        SetState(ServiceState.Active, "Processing transactions...");
                        await SavedVarsProcessor.Process(data, db, context);

                        SetState(ServiceState.Waiting, "Waiting for ESO to exit...");

                        Program.context?.WaitForESOExit(svFile, db);
                        await Task.Delay(1000);

                        /*if (await AppExitWatcher.WaitForESOExit())
                        {
                            context.SetTooltip("Marking transactions processed.");
                            await Task.Delay(1000);
                            SavedVarsParser.SetProcessed(svFile);
                            await db.RemoveUploadedTransactions();
                        }*/
                    }

                    // We hopefully have a language now - start JSONUploader!
                    _ = JSONUploader.Instance.Process(db, language, context);
                }

                if (!AppExitWatcher.active)
                {
                    SetState(ServiceState.Idle, "Watching for changes...");
                }
            }
            catch (FileNotFoundException)
            {
                LogForm.Log($"Saved variables file not found at '{svFile}'");
                SetState(ServiceState.Error, "Saved variables file not found");
            }
            catch (JsonException ex)
            {
                LogForm.Log($"Exception parsing saved variables: {ex.Message}");
                SetState(ServiceState.Error, "Failed to parse saved variables");
            }
        }

        public String GetSavedVarsPath()
        {
            String path = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "Elder Scrolls Online", "live", "SavedVariables");
            LogForm.Log("Saved vars: " + path);

            return path;
        }

        private void OnChanged(object sender, FileSystemEventArgs e)
        {
            if (!Program.config.IsValid())
            {
                return;
            }

            if (_timers.TryGetValue(e.FullPath, out var timer))
            {
                timer.Stop();
                timer.Start();
                return;
            }

            SetState(ServiceState.Waiting, "File change detected, waiting for completion...");

            timer = new System.Timers.Timer(500); // wait 500ms of quiet time
            timer.AutoReset = false;
            timer.Elapsed += async (_, __) =>
            {
                _timers.Remove(e.FullPath);
                await WaitForFileReadyAsync(e.FullPath);
            };

            _timers[e.FullPath] = timer;
            timer.Start();
        }

        private async Task WaitForFileReadyAsync(string path)
        {
            while (true)
            {
                try
                {
                    using var stream = new FileStream(
                        path,
                        FileMode.Open,
                        FileAccess.Read,
                        FileShare.None); // requires exclusive access

                    break; // success: file is no longer locked
                }
                catch (IOException)
                {
                    await Task.Delay(200); // wait and retry
                }
            }

            // Optional: also check for size stability
            long lastSize = -1;
            while (true)
            {
                long size = new FileInfo(path).Length;
                if (size == lastSize)
                    break;

                lastSize = size;
                await Task.Delay(200);
            }

            // Now the file is fully written
            await Process(path);
        }

    }
}
