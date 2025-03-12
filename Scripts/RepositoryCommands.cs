using Azure.Storage.Blobs;
using LibGit2Sharp;
using Newtonsoft.Json;
using System.IO;
using System.Text;

namespace Rasp {
    public class InitCommand : ICommand {
        public string Usage => $"{Commands.rasp} {Commands.init}";


        public void Execute( string[] args ) {
            if ( args.Length != 1 ) {
                Console.WriteLine("Usage: " + Usage);
                return;
            }

            string raspDir = Path.Combine(Directory.GetCurrentDirectory(), ".rasp");
            string commitsDir = Path.Combine(raspDir, "commits");
            string indexFile = Path.Combine(raspDir, "index.json");
            string configFile = Path.Combine(raspDir, "config.json");
            string hashFilePath = Path.Combine(raspDir, "hash.json");
            string branchDir = Path.Combine(raspDir, $"branches/main");

            if ( Directory.Exists(raspDir) ) {
                RaspUtils.DisplayMessage("Error: Rasp repository already initialized.", Color.Red);
                return;
            }

            Dictionary<string, string> config = [];
            config["remote"] = "local";
            config["author"] = "guest";
            config["email"] = "unknown";
            config["branch"] = "main";

            try {
                Directory.CreateDirectory(commitsDir);
                Directory.CreateDirectory(branchDir);
                
                if ( !File.Exists(indexFile) )
                    File.WriteAllText(indexFile, "{}");

                if ( !File.Exists(configFile) )
                    File.WriteAllText(configFile, JsonConvert.SerializeObject(config, Formatting.Indented));

                if ( !File.Exists(hashFilePath) )
                    File.WriteAllText(hashFilePath, "{}");

                RaspUtils.DisplayMessage("Initialized Rasp in the current directory.", Color.Green);
            } catch ( Exception ex ) {
                RaspUtils.DisplayMessage($"Error: {ex.Message}", Color.Red);
            }
        }

    }

    public class BranchCommand : ICommand {
        public string Usage => $"{Commands.rasp} {Commands._branch} <branch>";
        public void Execute( string[] args ) {
            if ( args.Length != 2 ) {
                Console.WriteLine("Usage: " + Usage);
                return;
            }
            string branch = args[1];
            string indexFile = Path.Combine(Directory.GetCurrentDirectory(), ".rasp/index.json");
            string configFile = Path.Combine(Directory.GetCurrentDirectory(), ".rasp/config.json");
            if ( !File.Exists(indexFile) || !File.Exists(configFile) ) {
                RaspUtils.DisplayMessage("Error: Rasp repository not initialized. Run 'rasp init' first.", Color.Red);
                return;
            }

            Dictionary<string, string> config = RaspUtils.LoadJson<string>(configFile)!;
            if ( config["branch"] == branch) {
                RaspUtils.DisplayMessage($"Warning: Already in ({branch}) branch", Color.Yellow);
                return;
            }
            try {

                string branchDir = Path.Combine(Directory.GetCurrentDirectory(), $".rasp/branches/{branch}");
                if ( !Directory.Exists(branchDir) ) {
                    Directory.CreateDirectory(branchDir);
                    string[] files = Directory.GetFiles(Directory.GetCurrentDirectory());
                    foreach ( string source in files ) {
                        string destination = Path.Combine(branchDir, source);
                        RaspUtils.SafeFileCopy(source, destination);
                    }  
                }

                config["branch"] = branch;
                RaspUtils.SaveJson(configFile, config);
                RaspUtils.DisplayMessage($"Switched to branch '{branch}'.", Color.Green);

            } catch (Exception ex){
                RaspUtils.DisplayMessage($"Error: {ex.Message}", Color.Red);
            }
        }
    }

    public class AddCommand : ICommand {
        public string Usage => $"{Commands.rasp} {Commands.add} <file/directory>";

        public void Execute( string[] args ) {
            if ( args.Length < 2 ) {
                Console.WriteLine("Usage: " + Usage);
                return;
            }

            string currentDir = Directory.GetCurrentDirectory();
            string indexFilePath = Path.Combine(currentDir, ".rasp/index.json");

            if ( !Directory.Exists(Path.Combine(currentDir, ".rasp")) ) {
                RaspUtils.DisplayMessage("Error: Rasp repository not initialized. Run 'rasp init' first.", Color.Red);
                return;
            }

            // Load existing index or create a new one
            var index = RaspUtils.LoadJson<Dictionary<string, string>>(indexFilePath);

            List<string> filesToAdd = [];
            string targetPath = Path.Combine(currentDir, args[1]);

            if ( Directory.Exists(targetPath) ) {
                filesToAdd.AddRange(Directory.GetFiles(targetPath, "*", SearchOption.AllDirectories));
            } else if ( File.Exists(targetPath) ) {
                filesToAdd.Add(targetPath);
            } else {
                RaspUtils.DisplayMessage($"Error: '{args[1]}' does not exist.", Color.Red);
                return;
            }
            int addedCount = 0, failedCount = 0;

            foreach ( string filePath in filesToAdd ) {
                try {
                    string relativePath = Path.GetRelativePath(currentDir, filePath);

                    if ( index.TryGetValue(relativePath, out var existingEntry) && existingEntry["status"] == "tracked" ) {
                        continue;
                    }

                    using FileStream stream = File.OpenRead(filePath);
                    string fileHash = RaspUtils.ComputeHashCode(stream);

                    index[relativePath] = new Dictionary<string, string> {
                        { "hash", fileHash },
                        { "status", "tracked" }
                    };
                    addedCount++;
                } catch {
                    failedCount++;
                }
            }
            RaspUtils.SaveJson(indexFilePath, index);
            RaspUtils.DisplayMessage($"Add operation completed: {addedCount} added, {failedCount} failed.", Color.Yellow);
        }
    }



    public class CommitCommand : ICommand {
        public string Usage => $"{Commands.rasp} {Commands.commit} {Commands._message} <message>";

        public void Execute( string[] args ) {
            if ( args.Length < 3 ) {
                Console.WriteLine("Usage: " + Usage);
                return;
            }

            string message = args[2];
            string indexFile = Path.Combine(Directory.GetCurrentDirectory(), ".rasp/index.json");
            string configFile = Path.Combine(Directory.GetCurrentDirectory(), ".rasp/config.json");

            try {

                Dictionary<string, Dictionary<string, string>>? index = RaspUtils.LoadJson<Dictionary<string,string>>(indexFile);
                Dictionary<string, string>? config = RaspUtils.LoadJson<string>(configFile);

                if ( config == null ) {
                    RaspUtils.DisplayMessage("Error: Config file is missing or corrupted.", Color.Red);
                    return;
                }

                string commitsDir = Path.Combine(Directory.GetCurrentDirectory(), $".rasp/branches/{config["branch"]}/commits");
                using Stream stream = new MemoryStream(Encoding.UTF8.GetBytes($"{config["author"]} {message} {DateTime.Now}"));
                string hash = RaspUtils.ComputeHashCode(stream);
                string hashDir = Path.Combine(commitsDir, hash);
                Directory.CreateDirectory(hashDir);

                string historyFile = Path.Combine(Directory.GetCurrentDirectory(), $".rasp/branches/{config["branch"]}/commits/.history.json");
                Dictionary<string, string>? history = RaspUtils.LoadJson<string>(historyFile);

                if ( history != null ) {
                    File.WriteAllText(historyFile, "{}");
                }

                List<string> filesList = [];
                int count = 0;

                if ( index != null ) {
                    foreach ( var entry in index ) {
                        string relativePath = entry.Key;
                        string fileHash = entry.Value["hash"];
                        string status = entry.Value["status"];

                        if ( status == "tracked" ) {
                            count++;
                            string source = Path.Combine(Directory.GetCurrentDirectory(), relativePath);
                            string destination = Path.Combine(hashDir, relativePath);

                            if ( !File.Exists(source) ) {
                                RaspUtils.DisplayMessage($"Error: File '{source}' not found.", Color.Red);
                                return;
                            }

                            RaspUtils.SafeFileCopy(source, destination);
                            filesList.Add(relativePath);
                            entry.Value["status"] = "committed";
                        }
                    }
                    history![DateTime.Now.ToString()] = hash;
                }

                RaspUtils.SaveJson(indexFile, index!);
                if ( count == 0 ) {
                    RaspUtils.DisplayMessage("Warning: No changes to commit.", Color.Yellow);
                    Directory.Delete(hashDir);
                    return;
                }

                if (history!.Count > 0 ) {
                    RaspUtils.SaveJson(historyFile, history);
                } else {                   
                    File.Delete(historyFile);
                }
               
                Dictionary<string, object> commit = new() {
                    { "id", hash },
                    { "message", message },
                    { "author", config.ContainsKey("author") ? config["author"] : "unknown" },
                    { "timestamp", DateTime.Now.ToString() },
                    { "files", filesList }
                };

                string commitFile = Path.Combine(hashDir, "commit.json");
                RaspUtils.SaveJson(commitFile, commit);
                RaspUtils.DisplayMessage("Changes committed.", Color.Green);
            } catch ( Exception ex ) {
                RaspUtils.DisplayMessage($"Error: {ex.Message}", Color.Red);
            }
        }
    }


    public class RollbackCommand : ICommand {
        public string Usage => $"'{Commands.rasp} {Commands.rollback}' or '{Commands.rasp} {Commands.rollback}'";

        public void Execute( string[] args ) {
            if ( args.Length != 1 ) {
                Console.WriteLine("Usage: " + Usage);
                return;
            }
            string configFile = Path.Combine(Directory.GetCurrentDirectory(), ".rasp/config.json");
            string indexFile = Path.Combine(Directory.GetCurrentDirectory(), ".rasp/index.json");

            Dictionary<string, string> config = RaspUtils.LoadJson<string>(configFile);
            string commitsDir = Path.Combine(Directory.GetCurrentDirectory(), $".rasp/branches/{config["branch"]}/commits");
            string historyFile = Path.Combine(Directory.GetCurrentDirectory(), $".rasp/branches/{config["branch"]}/commits/.history.json");

            if ( !File.Exists(historyFile) ) {
                RaspUtils.DisplayMessage("Warning: No commits to rollback.", Color.Yellow);
                return;
            }

            Dictionary<string, string>? history = RaspUtils.LoadJson<string>(historyFile);
            Dictionary<string, Dictionary<string, string>>? index = RaspUtils.LoadJson<Dictionary<string, string>>(indexFile);

            if ( history.Count == 0 ) {
                RaspUtils.DisplayMessage("Warning: No commits to rollback.", Color.Yellow);
                return;
            }

            string lastCommitKey = history.Last().Key;
            string lastCommitHash = history.Last().Value;
            string hashDir = Path.Combine(commitsDir, lastCommitHash);
            string commitFile = Path.Combine(hashDir, "commit.json");

            if ( !Directory.Exists(hashDir) || !File.Exists(commitFile) ) {
                RaspUtils.DisplayMessage($"Error: Commit {lastCommitHash} data is missing or corrupted.", Color.Red);
                history.Remove(lastCommitKey);
                File.WriteAllText(historyFile, JsonConvert.SerializeObject(history, Formatting.Indented));
                return;
            }

            try {
                var commit = JsonConvert.DeserializeObject<Dictionary<string, object>>(File.ReadAllText(commitFile));
                if ( commit == null || !commit.ContainsKey("files") ) {
                    RaspUtils.DisplayMessage($"Error: Commit {lastCommitHash} has no files recorded.", Color.Red);
                    return;
                }

                Console.WriteLine($"Rolling back commit: {commit["author"]} \"{commit["message"]}\" ({commit["timestamp"]})");
                
                var filesList = JsonConvert.DeserializeObject<List<string>>(commit["files"].ToString()!);

                Console.ForegroundColor = ConsoleColor.Yellow;
                Console.Write($"Do you want to backup current version? (y/n): ");
                string? input = Console.ReadLine()?.Trim().ToLower();
                Console.ResetColor();

                if ( input == "y" ) {
                    string backupDir = Path.Combine(".rasp", "backup", $"backup_{DateTime.Now:yyyyMMdd_HHmmss}");
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
                        index![file]["status"] = "tracked";
                    } else {
                        RaspUtils.DisplayMessage($" |- Warning: File {file} missing in commit {lastCommitHash}", Color.Yellow);
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

                RaspUtils.DisplayMessage("Rollback complete.", Color.Green);

            } catch ( Exception ex ) {
                
                RaspUtils.DisplayMessage($"Error: {ex.Message}", Color.Red);
                
            }
            
        }
    }

    public class StatusCommand : ICommand {
        public string Usage => $"{Commands.rasp} {Commands.status}";
        public void Execute( string[] args ) {
            if ( args.Length != 1 ) {
                Console.WriteLine("Usage: " + Usage);
                return;
            }
            string indexFile = Path.Combine(Directory.GetCurrentDirectory(), ".rasp/index.json");
            Dictionary<string, Dictionary<string, string>>? index = RaspUtils.LoadJson<Dictionary<string, string>>(indexFile);
            List<string> stagedList = [];
            List<string> committedList = [];

            if ( index == null ) {
                RaspUtils.DisplayMessage("Error: Index file is missing or corrupted.", Color.Red);
                return;
            }
            
            foreach ( var entry in index ) {
                string relativePath = entry.Key;
                string status = entry.Value["status"];
                if ( status == "tracked" ) {
                    stagedList.Add(relativePath);
                } else if ( status == "committed" ) {
                    committedList.Add(relativePath);
                }
            }

            Console.WriteLine("Staging area:");
            RaspUtils.DisplayMessage($" Staged: {stagedList.Count}", Color.Yellow);
            foreach ( var staged in stagedList ) {
                Console.WriteLine($" |- {staged}");
            }

            RaspUtils.DisplayMessage($" Committed: {committedList.Count}", Color.Green);
            foreach ( var committed in committedList ) {
                Console.WriteLine($" |- {committed}");
            }

        }
    }

    public class RevertCommand : ICommand {
        public string Usage => $"{Commands.rasp} {Commands.revert} <file>";

        public void Execute( string[] args ) {
            if ( args.Length != 2 ) {
                Console.WriteLine("Usage: " + Usage);
                return;
            }

            string filepath = args[1];
            string indexFile = Path.Combine(Directory.GetCurrentDirectory(), ".rasp/index.json");
            Dictionary<string, Dictionary<string, string>>? index = RaspUtils.LoadJson<Dictionary<string, string>>(indexFile);


            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine($"Are you sure you want to revert '{filepath}'? (y/n)");
            string? input = Console.ReadLine()?.Trim().ToLower();
            Console.ResetColor();

            if ( input != "y" ) return;

            if ( index == null || !index.ContainsKey(filepath) ) {
                RaspUtils.DisplayMessage($"Error: File '{filepath}' not found in the index.", Color.Red);
                return;
            }

            if ( index[filepath]["status"] == "tracked" ) {
                if ( index.Remove(filepath) ) {
                    RaspUtils.DisplayMessage($"File '{filepath}' reverted.", Color.Yellow);                    
                }
            } else if ( index[filepath]["status"] == "committed" ) {     
                RaspUtils.DisplayMessage($"Warning: File '{filepath}' is already committed, Use 'rasp rollback' instead.", Color.Yellow);
            }

            RaspUtils.SaveJson(indexFile, index!);

        }
    }

    public class ProfileCommand : ICommand {
        public string Usage => $"{Commands.rasp} {Commands._profile} <author> <mail>";

        public void Execute( string[] args ) {
            if ( args.Length != 3 ) {
                Console.WriteLine("Usage: " + Usage);
                return;
            }
            try {
                string configFile = Path.Combine(Directory.GetCurrentDirectory(), ".rasp/config.json");
                Dictionary<string, string> config = RaspUtils.LoadJson<string>(configFile)!;

                if ( !args[2].Contains('@') || !args[2].Contains('.') ) {
                    RaspUtils.DisplayMessage("Error: Invalid email format.", Color.Red);
                    return;
                }
                config["author"] = args[1];
                config["email"] = args[2];
                
                RaspUtils.SaveJson(configFile, config);
                RaspUtils.DisplayMessage("Profile updated successfully.", Color.Green);
            } catch (Exception ex) {
                RaspUtils.DisplayMessage($"Error: {ex.Message}", Color.Red);
            }
        }
    }

    public class LogsCommand : ICommand {
        public string Usage => $"{Commands.rasp} {Commands.logs}";
        public void Execute( string[] args ) {
            if ( args.Length != 1 ) {
                Console.WriteLine("Usage: " + Usage);
                return;
            }
            string commitsDir = Path.Combine(Directory.GetCurrentDirectory(), ".rasp/commits");
            string historyFile = Path.Combine(Directory.GetCurrentDirectory(), ".rasp/commits/.history.json");
            if ( !File.Exists(historyFile) ) {
                RaspUtils.DisplayMessage($"Warning: No commits found.", Color.Yellow);
                return;
            }
            Dictionary<string, string>? history = RaspUtils.LoadJson<string>(historyFile);
            if ( history == null || history.Count == 0 ) {
                RaspUtils.DisplayMessage($"Warning: No commits found.", Color.Yellow);
                return;
            }
            foreach ( var commit in history ) {
                string hash = commit.Value;
                string hashDir = Path.Combine(commitsDir, hash);
                string commitFile = Path.Combine(hashDir, "commit.json");

                if ( !Directory.Exists(hashDir) || !File.Exists(commitFile) ) {
                    RaspUtils.DisplayMessage($"Error: Commit {hash} data is missing or corrupted.", Color.Red);
                    history.Remove(commit.Key);
                    RaspUtils.SaveJson(historyFile,history);
                    return;
                }

                var commitData = RaspUtils.LoadJson<Dictionary<string, object>>(commitFile);
                if ( commitData == null ) {
                    RaspUtils.DisplayMessage($"Error: Commit {hash} data is missing or corrupted.", Color.Red);
                    return;
                }

                Console.WriteLine($"Author: {commitData["author"]}    Message: {commitData["message"]}    Timestamp: {commitData["timestamp"]}");
                Console.WriteLine("Files:");
                foreach ( var file in JsonConvert.DeserializeObject<List<string>>(commitData["files"].ToString()!)! ) {
                    Console.WriteLine($" |- {file}");
                }
                Console.WriteLine();
            }
        }
    }

    public class DropCommand : ICommand {
        public string Usage => $"{Commands.rasp} {Commands.drop}";

        public void Execute( string[] args ) {
            if(args.Length != 1) {
                Console.WriteLine("Usage: " + Usage);
                return;
            }
            string raspDirectory = Path.Combine(Directory.GetCurrentDirectory(), ".rasp");

            if ( !File.Exists(Path.Combine(raspDirectory, "config.json")) ) {
                RaspUtils.DisplayMessage("Error: The detected '.rasp' folder does not contain a valid Rasp repository.", Color.Red);
                return;
            }

            if ( Directory.Exists(raspDirectory) ) {
                Console.ForegroundColor = ConsoleColor.Yellow;
                Console.Write("Do you want to delete the Rasp repository? (y/n): ");
                string? input = Console.ReadLine()?.Trim().ToLower();
                Console.ResetColor();
                if ( input != "y" ) return;
                
                Directory.Delete(raspDirectory, true);
                RaspUtils.DisplayMessage("Rasp repository deleted successfully.", Color.Green);
            } else {
                RaspUtils.DisplayMessage("Error: No Rasp repository was innitialized in current directory.", Color.Red);
            }
        }
    }
}
