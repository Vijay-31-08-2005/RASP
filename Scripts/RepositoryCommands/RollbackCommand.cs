using Newtonsoft.Json;

namespace Rasp {
    public class RollbackCommand : ICommand {
        public string Usage => $"{Commands.rollback}";

        public void Execute( string[] args ) {

            string configFile = Paths.ConfigFile;

            if ( RaspUtils.IsMissing(configFile) )
                return;
            Dictionary<string, string> config = RaspUtils.LoadJson<string>(configFile);
            string indexFile = Path.Combine(Paths.BranchesDir, config[Key.branch], Files.index);
            string commitsDir = Path.Combine(Paths.BranchesDir, config[Key.branch], Dir.commits);
            string historyFile = Path.Combine(commitsDir, Files.history);
            string logsFile = Paths.LogsFile;

            if ( !File.Exists(historyFile) ) {
                RaspUtils.DisplayMessage("Warning: No commits to rollback.", ConsoleColor.Yellow);
                return;
            }

            if ( RaspUtils.IsMissing(logsFile) )
                return;

            Dictionary<string, string>? history = RaspUtils.LoadJson<string>(historyFile);
            Dictionary<string, string>? logs = RaspUtils.LoadJson<string>(logsFile);
            Dictionary<string, Dictionary<string, string>>? index = RaspUtils.LoadJson<Dictionary<string, string>>(indexFile);

            if ( history.Count == 0 ) {
                RaspUtils.DisplayMessage("Warning: No commits to rollback.", ConsoleColor.Yellow);
                return;
            }

            string lastCommitKey = history.Last().Key;
            string lastCommitHash = history.Last().Value;
            string hashDir = Path.Combine(commitsDir, lastCommitHash);
            string commitFile = Path.Combine(hashDir, $"{Files.commit}");

            if ( !Directory.Exists(hashDir) || !File.Exists(commitFile) ) {
                RaspUtils.DisplayMessage($"Error: Commit {lastCommitHash} data is missing or corrupted.", ConsoleColor.Red);
                history.Remove(lastCommitKey);
                File.WriteAllText(historyFile, JsonConvert.SerializeObject(history, Formatting.Indented));
                return;
            }

            try {
                var commit = JsonConvert.DeserializeObject<Dictionary<string, object>>(File.ReadAllText(commitFile));
                if ( commit!.Equals(null) || !commit.TryGetValue("files", out object? files) ) {
                    RaspUtils.DisplayMessage($"Error: Commit {lastCommitHash} has no files recorded.", ConsoleColor.Red);
                    return;
                }

                Console.WriteLine($"Rolling back commit: {commit[Key.author]} \"{commit[Key.message]}\" ({commit[Key.timeStamp]})");

                var filesList = JsonConvert.DeserializeObject<List<string>>(files.ToString()!);

                RaspUtils.WriteColor($"Do you want to backup current version? (y/n): ", ConsoleColor.Yellow);
                string? input = Console.ReadLine()?.Trim().ToLower();

                if ( input!.Equals(Value.y) ) {
                    string backupDir = Path.Combine(Paths.BackupDir, $"backup_{DateTime.Now:yyyyMMdd_HHmmss}");
                    Directory.CreateDirectory(backupDir);

                    foreach ( var file in filesList! ) {
                        string sourceFile = Path.Combine(Directory.GetCurrentDirectory(), file);
                        string backupFile = Path.Combine(backupDir, file);

                        if ( File.Exists(sourceFile) ) {
                            Directory.CreateDirectory(Path.GetDirectoryName(backupFile)!);
                            File.Copy(sourceFile, backupFile, true);
                        }
                    }
                    Console.WriteLine($"Backup created at: {backupDir}");
                }

                Console.WriteLine("Restoring files...");
                foreach ( var file in filesList! ) {
                    string sourceFile = Path.Combine(hashDir, file);
                    string destinationFile = Path.Combine(Directory.GetCurrentDirectory(), file);

                    if ( File.Exists(sourceFile) ) {
                        RaspUtils.SafeFileCopy(sourceFile, destinationFile);
                        Console.WriteLine($" |- Restored: {file}");
                        index![file][Key.status] = Status.tracked;
                    } else {
                        RaspUtils.DisplayMessage($" |- Warning: File {file} missing in commit {lastCommitHash}", ConsoleColor.Yellow);
                    }
                }

                RaspUtils.SaveJson(indexFile, index!);
                Directory.Delete(hashDir, true);
                history.Remove(lastCommitKey);

                if ( history.Count == 0 ) {
                    File.Delete(historyFile);
                } else {
                    RaspUtils.SaveJson(historyFile, history);
                }
                RaspUtils.DisplayMessage("Rollback complete.", ConsoleColor.Green);
            } catch ( Exception ex ) {
                RaspUtils.DisplayMessage($"Error: {ex.Message}", ConsoleColor.Red);
            }
            logs![DateTime.Now.ToString()] = $"{config[Key.author]} rolled back to commit {lastCommitHash}";
            RaspUtils.SaveJson(logsFile, logs);
        }

        public void Info() {
            Console.WriteLine("  Rolls back the most recent commit, restoring the previous state of files.");
            Console.WriteLine("  If files exist, a backup option is provided before rollback.");
            Console.WriteLine();
            Console.WriteLine($"Usage: {Commands.rasp} {Commands.rollback}");
            Console.WriteLine($"       {Commands.rasp} {Commands._rollback}");
            Console.WriteLine();
            Console.WriteLine("Options:");
            Console.WriteLine("  -h, --help        Show this help message.");
            Console.WriteLine();
        }
    }
}
