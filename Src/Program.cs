using System.Diagnostics;
using WinFormsApp;
using ZenitharClient.Properties;

namespace ZenitharClient.Src
{
    internal static class Program
    {
        internal static string ClientSecret = "QWXQeKqtrJWBoECC8LPIMWsZYd4yLCMa";

        public static TrayApplicationContext? context = null;
        /// <summary>
        ///  The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main()
        {
            bool created;
            using var mutex = new Mutex(true, "ZenitharMutex", out created);
            if (!created)
            {
                // already running
                return;
            }

            // To customize application configuration such as set high DPI settings or default font,
            // see https://aka.ms/applicationconfiguration.
            ApplicationConfiguration.Initialize();

            context = new TrayApplicationContext();

            //Application.Run(new Form1());
            Application.Run(context);
        }

        public static bool HasValidSettings()
        {
            return !string.IsNullOrWhiteSpace(Settings.Default.ServerEndpoint) &&
                   !string.IsNullOrWhiteSpace(Settings.Default.GuildToken);
        }

        //public static string GetClientSecret()
        //{
        //    if (!string.IsNullOrWhiteSpace(Settings.Default.ClientSecret))
        //    {
        //        Debug.WriteLine($"Client secret already exists: {Settings.Default.ClientSecret}");
        //        return Settings.Default.ClientSecret;
        //    }
        //    // Generate a random 64-character hex string for the client secret
        //    var bytes = new byte[32]; // 16 bytes = 128 bits = 32 hex chars
        //    using var rng = System.Security.Cryptography.RandomNumberGenerator.Create();
        //    rng.GetBytes(bytes);
        //    string clientSecret = Convert.ToHexString(bytes).ToLowerInvariant();
        //    Debug.WriteLine($"Generated Client Secret: {clientSecret}");
        //    Settings.Default.ClientSecret = clientSecret;
        //    Settings.Default.Save();

        //    return clientSecret;
        //}

        //public static void UploadJSONTransactions(Queue<JSONTransaction> txnQueue)
        //{
        //    SynchronizationContext.Current.Post(async _ =>
        //    {
        //        await JSONUploader.Process(txnQueue);
        //    }, null);
        //}

    }
}