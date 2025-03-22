namespace Rasp {

    public class SetCommand : ICommand {
        public string Usage => $"{Commands.set} <connection-string>";

        public void Execute( string[] args ) {

            string appDataPath = Paths.RaspAppDir;
            string azConfigFile = Paths.AzConfigFile;
            string configFile = Paths.ConfigFile;
            string logsFile = Paths.LogsFile;

            string connectionString = args[1];

            if ( !IsValidAzureConnectionString(connectionString) ) {
                RaspUtils.DisplayMessage("Error: Invalid Connection string", ConsoleColor.Red);
                return;
            }

            if ( RaspUtils.IsMissing(configFile) ) 
                return;
            
            if ( RaspUtils.IsMissing(logsFile) ) 
                return;

            if ( !Directory.Exists(appDataPath) ) {
                Directory.CreateDirectory(appDataPath);
            }

            if ( !File.Exists(azConfigFile) ) {
                File.WriteAllText(azConfigFile, "{}");
            }

            var logs = RaspUtils.LoadJson<string>(logsFile);

            try {
                var azConfig = RaspUtils.LoadJson<string>(azConfigFile);
                var config = RaspUtils.LoadJson<string>(configFile);

                if ( config == null ) {
                    RaspUtils.DisplayMessage("Error: Config file is corrupted or missing.", ConsoleColor.Red); 
                    return;
                }

                azConfig[Key.key] = $"Rasp {config[Key.author]} {config[Key.email]}";
                azConfig[Key.connectionString] = Encryption.Encrypt(connectionString, azConfig[Key.key]);

                RaspUtils.SaveJson(azConfigFile, azConfig);
                RaspUtils.DisplayMessage("Connection String Saved", ConsoleColor.Green);
            } catch ( IOException ioEx ) {
                RaspUtils.DisplayMessage($"File Error: {ioEx.Message}", ConsoleColor.Red);
            } catch ( Exception ex ) {
                RaspUtils.DisplayMessage($"Unexpected Error: {ex.Message}", ConsoleColor.Red);
            }

            logs[DateTime.Now.ToString()] = $"Connection string saved in azConfig file";
            RaspUtils.SaveJson(logsFile, logs);
        }

        private static bool IsValidAzureConnectionString( string connectionString ) {
            var requiredKeys = new List<string> { "DefaultEndpointsProtocol", "AccountName", "AccountKey", "EndpointSuffix" };

            var keyPairs = connectionString.Split(';', StringSplitOptions.RemoveEmptyEntries)
                                           .Select(pair => pair.Split('='))
                                           .Where(pair => pair.Length >= 2)
                                           .ToDictionary(pair => pair[0].Trim(), pair => pair[1].Trim());
            return requiredKeys.All(key => keyPairs.ContainsKey(key));
        }

        public void Info() {
            Console.WriteLine("  Configures the Azure connection string for remote storage.");
            Console.WriteLine("  Encrypts and stores the connection string securely in the Rasp config.");
            Console.WriteLine();
            Console.WriteLine($"Usage: {Commands.rasp} {Commands.set} <connection-string>");
            Console.WriteLine();
            Console.WriteLine("Options:");
            Console.WriteLine(" <connection-string>     Connection string of Storage account.");
            Console.WriteLine("  -h, --help             Show this help message.");
            Console.WriteLine();
        }

    }
}