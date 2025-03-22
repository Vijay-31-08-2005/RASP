namespace Rasp {
    public class BranchCommand : ICommand {
        public string Usage => $"{Commands.branch} <branch-name>";

        public void Execute( string[] args ) {

            string option = args[1];
            string configFile = Paths.ConfigFile;
            string branchesDir = Paths.BranchesDir;
            string branchFile = Paths.BranchesFile;
            string branchDir = Path.Combine(branchesDir, option);
            string initialCommit = Path.Combine(branchDir, Dir.commits, Dir.initialCommit);
            string logsFile = Paths.LogsFile;
            var branches = RaspUtils.LoadJson<HashSet<string>>(branchFile);

            if ( option.Equals(Commands._list) ) {
                Console.WriteLine("Branches:");
                foreach ( var branch in branches[Key.branches] ) {
                    Console.WriteLine($" |- {branch}");
                }
                return;
            }

            var config = RaspUtils.LoadJson<string>(configFile);
            if ( config.Equals(null) || !config.TryGetValue(Key.branch, out string? currentBranch) ) {
                RaspUtils.DisplayMessage("Error: No active branch found. Initialize repository first.", ConsoleColor.Red);
                return;
            }

            string currentBranchDir = Path.Combine(branchesDir, currentBranch);

            if ( !branches[Key.branches].Add(option) ) {
                RaspUtils.DisplayMessage("Error: Branch already exists.", ConsoleColor.Red);
                return;
            }

            if ( RaspUtils.IsMissing(logsFile) )
                return;
            Dictionary<string, string> logs = RaspUtils.LoadJson<string>(logsFile)!;

            try {
                Directory.CreateDirectory(branchDir);

                if ( Directory.Exists(currentBranchDir) ) {
                    foreach ( var file in Directory.GetFiles(currentBranchDir, "*", SearchOption.AllDirectories) ) {
                        string filePath = Path.GetRelativePath(currentBranchDir, file);
                        using var stream = File.OpenRead(file);
                        string hash = RaspUtils.ComputeHashCode(stream);
                        RaspUtils.SafeFileCopy(file, Path.Combine(branchDir, filePath));
                        RaspUtils.SafeFileCopy(file, Path.Combine(initialCommit, filePath));
                    }
                } else {
                    RaspUtils.DisplayMessage($"Warning: Current branch '{currentBranch}' has no directory. Creating empty branch.", ConsoleColor.Yellow);
                }

                RaspUtils.SaveJson(branchFile, branches);

                RaspUtils.DisplayMessage($"Branch '{option}' successfully created.", ConsoleColor.Green);
            } catch ( Exception ex ) {
                branches[Key.branches].Remove(option);
                RaspUtils.SaveJson(branchFile, branches);
                RaspUtils.DisplayMessage($"Error: {ex.Message}", ConsoleColor.Red);
            }

            string log = $"{config[Key.author]} created the branch '{option}'.";
            logs[DateTime.Now.ToString()] = log;
            RaspUtils.SaveJson(logsFile, logs);
        }

        public void Info() {
            Console.WriteLine("  Manages branches in the repository.");
            Console.WriteLine("  Creates a new branch or lists existing branches.");
            Console.WriteLine();
            Console.WriteLine($"Usage: {Commands.rasp} {Commands.branch} [options]");
            Console.WriteLine();
            Console.WriteLine("Options:");
            Console.WriteLine("  <branch-name>    Creates a new branch with the specified name.");
            Console.WriteLine("  /list            Lists all available branches.");
            Console.WriteLine("  -h, --help       Show this help message");
            Console.WriteLine();
        }
    }
}
