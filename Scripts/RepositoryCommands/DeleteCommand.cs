namespace Rasp {
    public class DeleteCommand : ICommand {

        public string Usage => $"{Commands.delete} <file/directory/branch>";

        public void Execute( string[] args ) {

            string item = args[1];
            string branchesFile = Paths.BranchesFile;
            string branchesDir = Paths.BranchesDir;
            string configFile = Paths.ConfigFile;
            string logsFile = Paths.LogsFile;

            if( RaspUtils.IsMissing(configFile) || RaspUtils.IsMissing(branchesFile) || 
                RaspUtils.IsMissing(branchesDir) || RaspUtils.IsMissing(logsFile) || RaspUtils.IsMissing(item) ) 
                return;

            var config = RaspUtils.LoadJson<string> (configFile);
            var logs = RaspUtils.LoadJson<string>(logsFile);
            var branches = RaspUtils.LoadJson<HashSet<string>>(branchesFile);

            if ( item.Equals(config[Key.branch]) ) {
                RaspUtils.DisplayMessage("Error: Cannot delete the current branch.", ConsoleColor.Red);
                return;
            }  
            
            if ( item.Equals(Value.main) ) {
                RaspUtils.DisplayMessage("Error: Cannot delete the 'main' branch.", ConsoleColor.Red);
                return;
            }

            try {
                if ( branches.TryGetValue(Key.branches, out var branchesList) ) {
                    if ( branchesList.Contains(item) ) {

                        string branchDir = Path.Combine(branchesDir, item);
                        Directory.Delete(branchDir, true);
                        branchesList.Remove(item);

                        string log = $"{config[Key.author]} deleted the branch '{item}'.";
                        logs[DateTime.Now.ToString()] = log;

                        RaspUtils.SaveJson(logsFile, logs);
                        RaspUtils.SaveJson(branchesFile, branches);
                        return;
                    }
                }

                string name = Path.GetFileName(item);

                if ( File.Exists(item) ) {
                    File.Delete(item);
                    RaspUtils.DisplayMessage($"File '{name}' deleted successfully.", ConsoleColor.Green);
                } else if ( Directory.Exists(item) ) {
                    Directory.Delete(item, true);
                    RaspUtils.DisplayMessage($"File '{name}' deleted successfully.", ConsoleColor.Green);
                }

            } catch ( Exception ex ) {
                RaspUtils.DisplayMessage($"Error: {ex.Message}", ConsoleColor.Red);
            }
        }

        public void Info() {
            Console.WriteLine("  Deletes a branch, file, or directory from the repository.");
            Console.WriteLine("  If deleting a branch, it must not be the current or 'main' branch.");
            Console.WriteLine();
            Console.WriteLine($"Usage: {Commands.rasp} {Commands.delete} [options]");
            Console.WriteLine($"       {Commands.rasp} {Commands._delete} [options]");
            Console.WriteLine();
            Console.WriteLine("Options:");
            Console.WriteLine("  <branch-name>     The name of the branch to delete.");
            Console.WriteLine("  <file-path>       The path to the file or directory to delete.");
            Console.WriteLine("  -h, --help        Show this help message.");
            Console.WriteLine();
        }
    }
}
