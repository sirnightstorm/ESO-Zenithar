namespace ZenitharClient.Src
{
    internal static class Program
    {
        internal const string ClientSecret = "QWXQeKqtrJWBoECC8LPIMWsZYd4yLCMa";

        public static TrayApplicationContext? context = null;

        public static readonly Config config = new Config(GetConfigPath());

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

            Application.Run(context);
        }

        private static string GetConfigPath()
        {
            return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                                     "Zenithar", "config.json");
        }
    }
}