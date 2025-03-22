namespace Rasp {
    public class AddCommand : ICommand {
        public string Usage => $"{Commands.add} <file/directory>";

        public void Execute( string[] args ) {

            string currentDir = Directory.GetCurrentDirectory();
            string configFile = Paths.ConfigFile;

            if ( RaspUtils.IsMissing(configFile) )
                return;

            Dictionary<string, string> config = RaspUtils.LoadJson<string>(configFile)!;
            string indexFile = Path.Combine(Paths.BranchesDir, $"{config[Key.branch]}/{Files.index}");

            if ( RaspUtils.IsMissing(indexFile) )
                return;
            var index = RaspUtils.LoadJson<Dictionary<string, string>>(indexFile);

            List<string> filesToAdd = [];
            string targetPath = Path.Combine(currentDir, args[1]);

            if ( Directory.Exists(targetPath) ) {
                filesToAdd.AddRange(Directory.GetFiles(targetPath, "*", SearchOption.AllDirectories));
            } else if ( File.Exists(targetPath) ) {
                filesToAdd.Add(targetPath);
            } else {
                RaspUtils.DisplayMessage($"Error: '{args[1]}' does not exist.", ConsoleColor.Red);
                return;
            }
            int addedCount = 0, failedCount = 0, skippedCount = 0;

            foreach ( string filePath in filesToAdd ) {
                try {
                    string relativePath = Path.GetRelativePath(currentDir, filePath);

                    if ( index.TryGetValue(relativePath, out var existingEntry) && existingEntry[Key.status].Equals(Status.tracked) ) {
                        skippedCount++;
                        continue;
                    }

                    using FileStream stream = File.OpenRead(filePath);
                    string fileHash = RaspUtils.ComputeHashCode(stream);

                    index[relativePath] = new Dictionary<string, string> {
                        { Key.hash, fileHash },
                        { Key.status, Status.tracked }
                    };
                    addedCount++;
                } catch (Exception ex){
                    RaspUtils.DisplayMessage($"Failed to add '{filePath}': {ex.Message}", ConsoleColor.Red);
                    failedCount++;
                }
            }


            RaspUtils.SaveJson(indexFile, index);

            Console.WriteLine("Add operation completed:");
            RaspUtils.WriteColor($"Added: {addedCount}   ", ConsoleColor.Green);
            RaspUtils.WriteColor($"Skipped: {skippedCount}   ", ConsoleColor.Yellow);
            RaspUtils.WriteColor($"Failed: {failedCount}", ConsoleColor.Red);
            Console.WriteLine();
        }

        public void Info() {
            Console.WriteLine("  Adds the specified file or directory to the tracking index.");
            Console.WriteLine("  If a directory is specified, all files inside it will be added recursively.");
            Console.WriteLine();
            Console.WriteLine($"Usage: {Commands.rasp} [options]");
            Console.WriteLine();
            Console.WriteLine("Options:");
            Console.WriteLine("  <file/directory>   Directory or file to be added.");
            Console.WriteLine("  -h, --help         Show this help message.");
            Console.WriteLine();
        }
    }
}
