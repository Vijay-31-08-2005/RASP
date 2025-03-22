namespace Rasp {
    public class StatusCommand : ICommand {
        public string Usage => $"{Commands.status}";
        public void Execute( string[] args ) {

            string configFile = Paths.ConfigFile;
            if ( RaspUtils.IsMissing(configFile) )
                return;
            Dictionary<string, string> config = RaspUtils.LoadJson<string>(configFile);

            if ( config!.Equals(null) ) {
                RaspUtils.DisplayMessage("Error: Config file is missing or corrupted.", ConsoleColor.Red);
                return;
            }

            string indexFile = Path.Combine(Paths.BranchesDir, $"{config[Key.branch]}/{Files.index}");
            if ( RaspUtils.IsMissing(indexFile) )
                return;
            Dictionary<string, Dictionary<string, string>>? index = RaspUtils.LoadJson<Dictionary<string, string>>(indexFile);
            List<string> stagedList = [];
            List<string> committedList = [];
            List<string> untrackedList = [];

            if ( index!.Equals(null) ) {
                RaspUtils.DisplayMessage("Error: Index file is missing or corrupted.", ConsoleColor.Red);
                return;
            }

            foreach ( var entry in index ) {
                string relativePath = entry.Key;
                string status = entry.Value[Key.status];
                if ( status!.Equals(Status.tracked) ) {
                    stagedList.Add(relativePath);
                } else if ( status!.Equals(Status.committed) ) {
                    committedList.Add(relativePath);
                }
            }

            foreach ( var file in Directory.GetFiles(Directory.GetCurrentDirectory(), "*", SearchOption.AllDirectories) ) {
                string relativePath = Path.GetRelativePath(Directory.GetCurrentDirectory(), file);
                if ( !index.ContainsKey(relativePath) ) {
                    untrackedList.Add(relativePath);
                }
            }

            Console.WriteLine("Staging area:");
            RaspUtils.DisplayMessage($" Tracked: {stagedList.Count}", ConsoleColor.Yellow);
            foreach ( var staged in stagedList ) {
                Console.WriteLine($" |- {staged}");
            }

            RaspUtils.DisplayMessage($" Committed: {committedList.Count}", ConsoleColor.Green);
            foreach ( var committed in committedList ) {
                Console.WriteLine($" |- {committed}");
            }

            RaspUtils.DisplayMessage($" Untracked: {untrackedList.Count}", ConsoleColor.Red);
            foreach ( var untracked in untrackedList ) {
                Console.WriteLine($" |- {untracked}");
            }
        }

        public void Info() {
            Console.WriteLine("Displays the current status of the repository, including:");
            Console.WriteLine("  - Staged files (tracked files ready to be committed)");
            Console.WriteLine("  - Committed files (already committed to the repository)");
            Console.WriteLine("  - Untracked files (new files not yet added to version control)");
            Console.WriteLine();
            Console.WriteLine($"Usage: {Commands.rasp} {Commands.status}");
            Console.WriteLine();
            Console.WriteLine("Options:");
            Console.WriteLine("  -h, --help    Show this help message");
            Console.WriteLine();
        }
    }
}
