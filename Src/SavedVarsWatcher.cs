using System;
using System.Collections.Generic;
using System.Text;
using System.Diagnostics;
using WinFormsApp;

namespace ZenitharClient.Src
{
    public class SavedVarsWatcher
    {
        private readonly TrayApplicationContext context;
        private Queue<JSONTransaction> txnQueue = new Queue<JSONTransaction>();

        private FileSystemWatcher? watcher;
        private readonly Dictionary<string, System.Timers.Timer> _timers = new();

        //private Config config;

        public SavedVarsWatcher(TrayApplicationContext context)
        {
            this.context = context;

            string configPath = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                "Zenithar",
                "config.json"
            );

            //this.config = new Config(configPath);
        }

        public async Task Start()
        {
            var svPath = GetSavedVarsPath();

            if (Program.HasValidSettings())
            {
                await Process(Path.Combine(svPath, "Zenithar.lua"));
            }
            else
            {
                context.SetIcon(ClientState.Error);
                context.SetTooltip("Invalid Settings");
            }

            watcher = new FileSystemWatcher();

            watcher.Path = svPath;      // Folder to watch
            watcher.Filter = "Zenithar.lua";            // File to watch
            watcher.NotifyFilter = NotifyFilters.LastWrite | NotifyFilters.Size;

            watcher.Changed += OnChanged;
            watcher.Created += OnChanged;

            watcher.EnableRaisingEvents = true;
        }

        public void Stop()
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
            context.SetIcon(ClientState.Active);
            context.SetTooltip("Reading transactions...");
            var data = SavedVarsParser.ParseSavedVars(svFile);
            if (data != null)
            {
                if (!SavedVarsParser.IsProcessed(data))
                {
                    context.SetTooltip("Processing transactions...");
                    SavedVarsProcessor.Process(data, txnQueue, out var language, context);

                    context.SetTooltip("Uploading transactions...");
                    await JSONUploader.Process(txnQueue, language, context);

                    context.SetTooltip("Waiting for ESO to exit...");
                    context.SetIcon(ClientState.Waiting);
                    if (await AppExitWatcher.WaitForESOExit())
                    {
                        context.SetTooltip("Marking transactions processed.");
                        await Task.Delay(1000);
                        SavedVarsParser.SetProcessed(svFile);
                    }
                }
            }

            if (!AppExitWatcher.active)
            {
                context.SetTooltip("Watching for changes...");
                context.SetIcon(ClientState.Inactive);
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
            if (!Program.HasValidSettings())
            {
                context.SetIcon(ClientState.Error);
                context.SetTooltip("Invalid Settings");
                return;
            }

            if (_timers.TryGetValue(e.FullPath, out var timer))
            {
                timer.Stop();
                timer.Start();
                return;
            }

            context.SetTooltip("File change detected, waiting for completion...");

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
