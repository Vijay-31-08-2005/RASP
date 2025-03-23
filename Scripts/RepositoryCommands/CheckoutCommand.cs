namespace Rasp {
    public class CheckoutCommand : ICommand {
        public string Usage => $"{Commands.checkout} <branch>";

        public void Execute( string[] args ) {

            string branch = args[1];
            string configFile = Paths.ConfigFile;
            string branchFile = Paths.BranchesFile;

            Dictionary<string, HashSet<string>>? branches = RaspUtils.LoadJson<HashSet<string>>(branchFile);

            if ( !branches[Key.branches].Contains(branch) ) {
                RaspUtils.DisplayMessage($"Error: Branch '{branch}' does not exist.", ConsoleColor.Red);
                return;
            }

            if ( RaspUtils.IsMissing(configFile) )
                return;

            Dictionary<string, string>? config = RaspUtils.LoadJson<string>(configFile);
            if ( config.Equals(null) || !config.TryGetValue(Key.branch, out string? currentBranch) ) {
                RaspUtils.DisplayMessage("Error: No active branch found. Initialize repository first.", ConsoleColor.Red);
                return;
            }

            if ( currentBranch.Equals(branch) ) {
                RaspUtils.DisplayMessage($"Warning: Already in '{branch}' branch.", ConsoleColor.Yellow);
                return;
            }

            string newIndexFile = Path.Combine(Paths.BranchesDir, branch, Files.index);
            string currentIndexFile = Path.Combine(Paths.BranchesDir, currentBranch, Files.index);

            if ( !File.Exists(newIndexFile) ) {
                RaspUtils.DisplayMessage($"Warning: No index found for branch '{branch}'.", ConsoleColor.Yellow);
            } else if ( !File.Exists(currentIndexFile) ) {
                RaspUtils.DisplayMessage($"Warning: No index found for branch '{currentBranch}'.", ConsoleColor.Yellow);
            } else {

                Dictionary<string, Dictionary<string, string>>? newIndex = RaspUtils.LoadJson<Dictionary<string, string>>(newIndexFile);
                Dictionary<string, Dictionary<string, string>>? currentIndex = RaspUtils.LoadJson<Dictionary<string, string>>(currentIndexFile);

                if ( newIndex.Equals(null) ) {
                    RaspUtils.DisplayMessage($"Error: Index file '{newIndexFile}' is corrupted.", ConsoleColor.Red);
                    return;
                } else if ( currentIndexFile.Equals(null) ) {
                    RaspUtils.DisplayMessage($"Error: Index file '{currentBranch}' is corrupted.", ConsoleColor.Red);
                    return;
                }

                bool hasUncommittedChanges = currentIndex.Values.Any(file => file[Key.status] != Status.committed);
                if ( hasUncommittedChanges ) {
                    RaspUtils.DisplayMessage("Warning: You have uncommitted changes. Switching branches may overwrite your work.", ConsoleColor.Yellow);
                    Console.Write("Do you want to continue? (yes/no): ");
                    string? response = Console.ReadLine()?.Trim().ToLower();
                    if ( response != "yes" ) {
                        RaspUtils.DisplayMessage("Branch switch aborted.", ConsoleColor.Yellow);
                        return;
                    }
                }

                foreach ( var entry in newIndex ) {
                    if ( entry.Value[Key.status].Equals(Status.committed) ) {
                        string source = Path.Combine(Paths.BranchesDir, branch, Dir.commits, entry.Value[Key.lastCommit], entry.Key);
                        string destination = Path.Combine(Directory.GetCurrentDirectory(), entry.Key);
                        RaspUtils.SafeFileCopy(source, destination);
                    }
                }
            }

            config[Key.branch] = branch;
            RaspUtils.SaveJson(configFile, config);
            RaspUtils.DisplayMessage($"Switched to branch '{branch}'.", ConsoleColor.Green);
        }

        public void Info() {
            Console.WriteLine("  Switches to the specified branch in the repository.");
            Console.WriteLine("  If there are uncommitted changes, a warning is displayed before switching.");
            Console.WriteLine();
            Console.WriteLine($"Usage: {Commands.rasp} {Commands.checkout} [options]");
            Console.WriteLine();
            Console.WriteLine("Options:");
            Console.WriteLine("  <branch-name>    The name of the branch to switch to.");
            Console.WriteLine("  -h, --help       Show this help message");
            Console.WriteLine();
        }
    }
}
