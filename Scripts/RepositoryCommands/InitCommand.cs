namespace Rasp {
    public class InitCommand : ICommand {
        public string Usage => $"{Commands.init}";

        public void Execute( string[] args ) {

            string raspDir = Paths.RaspDir;
            string commitsDir = Paths.MainCommitsDir;
            string mainDir = Paths.MainDir;
            string configFile = Paths.ConfigFile;
            string branchesFile = Paths.BranchesFile;
            string indexFile = Paths.MainIndexFile;
            string logsFile = Paths.LogsFile;

            if ( Directory.Exists(raspDir) ) {
                RaspUtils.DisplayMessage("Error: Rasp repository already initialized.", ConsoleColor.Red);
                return;
            }

            Dictionary<string, string> config = new() {
                { Key.author, Value.guest },
                { Key.email, Value.unknown },
                { Key.branch, Value.main }
            };

            Dictionary<string, string> logs = new() {
                { DateTime.Now.ToString() , "Repository Initialized" },
            };

            Dictionary<string, HashSet<string>> set = new() {
                { Key.branches , new HashSet<string> { Value.main } }
            };

            try {

                if ( !File.Exists(configFile) )
                    RaspUtils.SaveJson(configFile, config);

                if ( ( File.Exists(configFile) && !Directory.Exists(mainDir) ) ) {
                    config = RaspUtils.LoadJson<string>(configFile);
                    config[Key.branch] = Value.main;
                    RaspUtils.SaveJson(configFile, config);
                }

                Directory.CreateDirectory(mainDir);
                Directory.CreateDirectory(commitsDir);

                if ( !File.Exists(indexFile) )
                    File.WriteAllText(indexFile, "{}");

                if ( !File.Exists(branchesFile) )
                    RaspUtils.SaveJson(branchesFile, set);

                if ( !File.Exists(logsFile) )
                    RaspUtils.SaveJson(logsFile, logs);

                File.SetAttributes(raspDir, FileAttributes.Hidden);
                RaspUtils.DisplayMessage("Initialized Rasp in the current directory.", ConsoleColor.Green);
            } catch ( Exception ex ) {
                RaspUtils.DisplayMessage($"Error: {ex.Message}", ConsoleColor.Red);
            }
        }

        public void Info() {
            Console.WriteLine("  Initializes a new Rasp repository in the current directory.");
            Console.WriteLine("  Creates necessary files and directories for version control.");
            Console.WriteLine();
            Console.WriteLine($"Usage: {Commands.rasp} {Commands.init}");
            Console.WriteLine();
            Console.WriteLine("Options:");
            Console.WriteLine("  -h, --help        Show this help message.");
            Console.WriteLine();
        }

    }
}
