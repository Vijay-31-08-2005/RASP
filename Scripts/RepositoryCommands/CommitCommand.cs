using System.Text;

namespace Rasp {
    public class CommitCommand : ICommand {
        public string Usage => $"{Commands.commit} [{Commands._message}] [message]";

        public void Execute( string[] args ) {

            string message = args.Length == 3 ? args[2] : "Default commit";
            string configFile = Paths.ConfigFile;
            string logsFile = Paths.LogsFile;
            Dictionary<string, string>? logs = RaspUtils.LoadJson<string>(logsFile);

            try {

                Dictionary<string, string>? config = RaspUtils.LoadJson<string>(configFile);
                if ( config.Equals(null) ) {
                    RaspUtils.DisplayMessage("Error: Config file is missing or corrupted.", ConsoleColor.Red);
                    return;
                }
                string indexFile = Path.Combine(Paths.BranchesDir, config[Key.branch], Files.index);
                string commitsDir = Path.Combine(Paths.BranchesDir, config[Key.branch], Dir.commits);
                if ( RaspUtils.IsMissing(indexFile) )
                    return;
                if ( RaspUtils.IsMissing(commitsDir) )
                    return;

                Dictionary<string, Dictionary<string, string>>? index = RaspUtils.LoadJson<Dictionary<string, string>>(indexFile);
                using Stream stream = new MemoryStream(Encoding.UTF8.GetBytes($"{config[Key.author]} {message} {DateTime.Now}"));
                string hash = RaspUtils.ComputeHashCode(stream);
                string hashDir = Path.Combine(commitsDir, hash);
                Directory.CreateDirectory(hashDir);

                string historyFile = Path.Combine(Paths.BranchesDir, config[Key.branch], Dir.commits, Files.history);
                Dictionary<string, string>? history = RaspUtils.LoadJson<string>(historyFile);

                if ( history != null ) {
                    File.WriteAllText(historyFile, "{}");
                }

                List<string> filesList = [];
                int count = 0;

                if ( index != null ) {
                    foreach ( var entry in index ) {
                        string relativePath = entry.Key;
                        string fileHash = entry.Value[Key.hash];
                        string status = entry.Value[Key.status];

                        if ( status.Equals(Status.tracked) ) {
                            count++;
                            string source = Path.Combine(Directory.GetCurrentDirectory(), relativePath);
                            string destination = Path.Combine(hashDir, relativePath);

                            if ( !File.Exists(source) ) {
                                RaspUtils.DisplayMessage($"Error: File '{source}' not found.", ConsoleColor.Red);
                                return;
                            }

                            RaspUtils.SafeFileCopy(source, destination);
                            filesList.Add(relativePath);
                            entry.Value[Key.lastCommit] = hash;
                            entry.Value[Key.status] = Status.committed;
                        }
                    }
                    history![DateTime.Now.ToString()] = hash;
                }

                RaspUtils.SaveJson(indexFile, index!);
                if ( count == 0 ) {
                    RaspUtils.DisplayMessage("Warning: No changes to commit.", ConsoleColor.Yellow);
                    Directory.Delete(hashDir);
                    return;
                }

                if ( history!.Count > 0 ) {
                    RaspUtils.SaveJson(historyFile, history);
                } else {
                    File.Delete(historyFile);
                }

                Dictionary<string, object> commit = new() {
                    { Key.id, hash },
                    { Key.message, message },
                    { Key.author, config.TryGetValue(Key.author, out string? user) ? user : Value.unknown },
                    { Key.timeStamp, DateTime.Now.ToString() },
                    { Key.files, filesList }
                };

                string log = $"{config[Key.author]} committed {count} changes to {config[Key.branch]} branch.";
                logs[DateTime.Now.ToString()] = log;
                RaspUtils.SaveJson(logsFile, logs);
                string commitFile = Path.Combine(hashDir, $"{Files.commit}");
                RaspUtils.SaveJson(commitFile, commit);
                RaspUtils.DisplayMessage("Changes committed.", ConsoleColor.Green);
            } catch ( Exception ex ) {
                RaspUtils.DisplayMessage($"Error: {ex.Message}", ConsoleColor.Red);
            }
        }

        public void Info() {
            Console.WriteLine("  Commits the tracked changes in the repository.");
            Console.WriteLine("  Creates a unique commit hash and stores the committed files in the branch's commit history.");
            Console.WriteLine();
            Console.WriteLine($"Usage: {Commands.rasp} {Commands.commit} [options]");
            Console.WriteLine();
            Console.WriteLine("Options:");
            Console.WriteLine("  -m <message>         A custom commit message (optional, defaults to 'Default commit').");
            Console.WriteLine("  -h, --help        Show this help message");
            Console.WriteLine();
        }
    }
}
