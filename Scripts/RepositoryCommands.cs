using Newtonsoft.Json;
using System.Globalization;
using System.Text;

namespace Rasp {
    public class InitCommand : ICommand {
        public string Usage => $"{Commands.init}";


        public void Execute( string[] args ) {

            if ( args.IsNotValid(Usage) ) return;

            string raspDir = Paths.RaspDir;
            string commitsDir = Paths.MainCommitsDir;
            string mainDir = Paths.MainDir;
            string configFile = Paths.ConfigFile;
            string branchesFile = Paths.BranchesFile;
            string indexFile = Paths.MainIndexFile;
            string logsFile = Paths.LogsFile;

            if ( Directory.Exists(raspDir) ) {
                RaspUtils.DisplayMessage("Error: Rasp repository already initialized.", Color.Red);
                return;
            }

            Dictionary<string, string> config = new() {
                { "author", "guest" },
                { "email", "unknown" },
                { "branch", "main" }
            };
            
            Dictionary<string, string> logs = new() {
                { DateTime.Now.ToString() , "Repository Initialized" },
            };

            Dictionary<string, HashSet<string>> set = new() {
                { "branches" , new HashSet<string> { "main" } } 
            };

            try {

                if ( !File.Exists(configFile) )
                    RaspUtils.SaveJson(configFile, config);

                if ( ( File.Exists(configFile) && !Directory.Exists(mainDir) ) ) {
                    config = RaspUtils.LoadJson<string>(configFile);
                    config["branch"] = "main";
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
                

                RaspUtils.DisplayMessage("Initialized Rasp in the current directory.", Color.Green);
            } catch ( Exception ex ) {
                RaspUtils.DisplayMessage($"Error: {ex.Message}", Color.Red);
            }
        }
    }

    public class BranchCommand : ICommand {
        public string Usage => $"{Commands.branch} <branch>";

        public void Execute( string[] args ) {
            if ( args.IsNotValid(Usage) ) return;

            string branch = args[1];
            string configFile = Paths.ConfigFile;
            string branchesDir = Paths.BranchesDir;
            string branchFile = Paths.BranchesFile;
            string branchDir = Path.Combine(branchesDir, branch);
            string initialHashFile = Path.Combine(branchDir, "initialHash.json");

            Directory.CreateDirectory(branchesDir);
            var branches = RaspUtils.LoadJson<HashSet<string>>(branchFile);

            var config = RaspUtils.LoadJson<string>(configFile);
            if ( config == null || !config.TryGetValue("branch", out string? currentBranch) ) {
                RaspUtils.DisplayMessage("Error: No active branch found. Initialize repository first.", Color.Red);
                return;
            }

            string currentBranchDir = Path.Combine(branchesDir, currentBranch);

            if ( !branches["branches"].Add(branch) ) {
                RaspUtils.DisplayMessage("Error: Branch already exists.", Color.Red);
                return;
            }

            try {
                Directory.CreateDirectory(branchDir);
                Dictionary<string, string> initialHashes = [];

                if ( Directory.Exists(currentBranchDir) ) {
                    foreach ( var file in Directory.GetFiles(currentBranchDir, "*", SearchOption.AllDirectories) ) {
                        string filePath = Path.GetRelativePath(currentBranchDir, file);
                        using var stream = File.OpenRead(file);
                        string hash = RaspUtils.ComputeHashCode(stream);
                        RaspUtils.SafeFileCopy(file, Path.Combine(branchDir, filePath));
                        initialHashes[filePath] = hash;
                    }
                } else {
                    RaspUtils.DisplayMessage($"Warning: Current branch '{currentBranch}' has no directory. Creating empty branch.", Color.Yellow);
                }

                RaspUtils.SaveJson(initialHashFile, initialHashes);
                RaspUtils.SaveJson(branchFile, branches);

                RaspUtils.DisplayMessage($"Branch '{branch}' successfully created.", Color.Green);
            } catch ( Exception ex ) {
                branches["branches"].Remove(branch);
                RaspUtils.SaveJson(branchFile, branches); 
                RaspUtils.DisplayMessage($"Error: {ex.Message}", Color.Red);
            }
        }
    }


    public class CheckoutCommand : ICommand {
        public string Usage => $"{Commands.checkout} <branch>";

        public void Execute( string[] args ) {
            if ( args.IsNotValid(Usage) ) return;

            string branch = args[1];
            string configFile = Paths.ConfigFile;
            string branchFile = Paths.BranchesFile;

            Dictionary<string, HashSet<string>>? branches = RaspUtils.LoadJson<HashSet<string>>(branchFile);

            if ( !branches["branches"].Contains(branch) ) {
                RaspUtils.DisplayMessage($"Error: Branch '{branch}' does not exist.", Color.Red);
                return;
            }

            if ( !File.Exists(configFile) ) {
                RaspUtils.DisplayMessage($"Error: Config file '{configFile}' is missing.", Color.Red);
                return;
            }

            Dictionary<string, string>? config = RaspUtils.LoadJson<string>(configFile);
            if ( config == null || !config.TryGetValue("branch", out string? currentBranch) ) {
                RaspUtils.DisplayMessage("Error: No active branch found. Initialize repository first.", Color.Red);
                return;
            }

            if ( currentBranch == branch ) {
                RaspUtils.DisplayMessage($"Warning: Already in '{branch}' branch.", Color.Yellow);
                return;
            }

            string newIndexFile = Path.Combine(Directory.GetCurrentDirectory(), $".rasp/branches/{branch}/index.json");
            string currentIndexFile = Path.Combine(Directory.GetCurrentDirectory(), $".rasp/branches/{currentBranch}/index.json");

            if ( !File.Exists(newIndexFile) ) {
                RaspUtils.DisplayMessage($"Warning: No index found for branch '{branch}'.", Color.Yellow);
            } else if ( !File.Exists(currentIndexFile) ) {
                RaspUtils.DisplayMessage($"Warning: No index found for branch '{currentBranch}'.", Color.Yellow);
            } else {

                Dictionary<string, Dictionary<string, string>>? newIndex = RaspUtils.LoadJson<Dictionary<string, string>>(newIndexFile);
                Dictionary<string, Dictionary<string, string>>? currentIndex = RaspUtils.LoadJson<Dictionary<string, string>>(currentIndexFile);

                if ( newIndex == null ) {
                    RaspUtils.DisplayMessage($"Error: Index file '{newIndexFile}' is corrupted.", Color.Red);
                    return;
                } else if ( currentIndexFile == null ) {
                    RaspUtils.DisplayMessage($"Error: Index file '{currentBranch}' is corrupted.", Color.Red);
                    return;
                }

                    bool hasUncommittedChanges = currentIndex.Values.Any(file => file["status"] != "committed");
                if ( hasUncommittedChanges ) {
                    RaspUtils.DisplayMessage("Warning: You have uncommitted changes. Switching branches may overwrite your work.", Color.Yellow);
                    Console.Write("Do you want to continue? (yes/no): ");
                    string? response = Console.ReadLine()?.Trim().ToLower();
                    if ( response != "yes" ) {
                        RaspUtils.DisplayMessage("Branch switch aborted.", Color.Yellow);
                        return;
                    }
                }

                foreach ( var entry in newIndex ) {
                    if ( entry.Value["status"] == "committed" ) {
                        string source = Path.Combine(Directory.GetCurrentDirectory(), $".rasp/branches/{branch}/commits/{entry.Value["lastCommit"]}/{entry.Key}");
                        string destination = Path.Combine(Directory.GetCurrentDirectory(), entry.Key);
                        RaspUtils.SafeFileCopy(source, destination);
                    }
                }
            }

            config["branch"] = branch;
            RaspUtils.SaveJson(configFile, config);
            RaspUtils.DisplayMessage($"Switched to branch '{branch}'.", Color.Green);
        }
    }

    public class MergeCommand : ICommand {
        public string Usage => $"{Commands.merge} <branch>";

        public void Execute( string[] args ) {
            if ( args.IsNotValid(Usage) ) return;

            string mergeBranch = args[1];

            if ( mergeBranch.Equals("main", StringComparison.OrdinalIgnoreCase) ) {
                RaspUtils.DisplayMessage("Error: Cannot merge main branch to any branch", Color.Red);
                return;
            }

            string configFile = Paths.ConfigFile;
            string branchesFile = Paths.BranchesFile;
            string mergeBranchDir = Path.Combine(Paths.BranchesDir, mergeBranch);
            string initialHashFile = Path.Combine(mergeBranchDir, "initialHash.json");
            string mergeIndexFile = Path.Combine(mergeBranchDir, "index.json");

            if( RaspUtils.IsMissing(mergeIndexFile) ) return;
            if ( RaspUtils.IsMissing(branchesFile) || RaspUtils.IsMissing(configFile) || RaspUtils.IsMissing(initialHashFile) )
                return;

            var branches = RaspUtils.LoadJson<HashSet<string>>(branchesFile);
            var config = RaspUtils.LoadJson<string>(configFile);
            var initialHashes = RaspUtils.LoadJson<string>(initialHashFile);

            if ( branches == null || config == null || initialHashes == null ||
                !branches.ContainsKey("branches") || !config.ContainsKey("branch") ) {
                RaspUtils.DisplayMessage("Error: Invalid repository configuration.", Color.Red);
                return;
            }

            if ( !branches["branches"].Contains(mergeBranch) ) {
                RaspUtils.DisplayMessage($"Error: Branch '{mergeBranch}' does not exist.", Color.Red);
                return;
            }

            string currentBranch = config["branch"];
            if ( currentBranch.Equals(mergeBranch, StringComparison.OrdinalIgnoreCase) ) {
                RaspUtils.DisplayMessage("Error: Cannot merge into the same branch", Color.Red);
                return;
            }

            string currentBranchDir = Path.Combine(Paths.BranchesDir, $"{currentBranch}");
            string currentIndexFile = Path.Combine(currentBranchDir, "index.json");

            if ( !Directory.Exists(currentBranchDir) || !Directory.Exists(mergeBranchDir) ) {
                RaspUtils.DisplayMessage("Error: One of the branch directories is missing.", Color.Red);
                return;
            }

            if ( RaspUtils.IsMissing(currentIndexFile) ) return;

            Dictionary<string, Dictionary<string, string>> currentIndex = RaspUtils.LoadJson<Dictionary<string, string>>(currentIndexFile);
            Dictionary<string, Dictionary<string, string>> mergeIndex = RaspUtils.LoadJson<Dictionary<string, string>>(mergeIndexFile);
            Dictionary<string, string> currentHashes = [];
            int mergedCount = 0, skippedCount = 0, conflictCount = 0, failedCount = 0;

            try {
                foreach ( var file in Directory.GetFiles(currentBranchDir, "*", SearchOption.AllDirectories) ) {
                    string relativePath = Path.GetRelativePath(currentBranchDir, file);
                    using var stream = File.OpenRead(file);
                    currentHashes[relativePath] = RaspUtils.ComputeHashCode(stream);
                }

                foreach ( var file in Directory.GetFiles(mergeBranchDir, "*", SearchOption.AllDirectories) ) {
                    try {
                        string mergeRelativePath = Path.GetRelativePath(mergeBranchDir, file);
                        using var stream = File.OpenRead(file);
                        string mergeHash = RaspUtils.ComputeHashCode(stream);

                        if ( !currentHashes.ContainsKey(mergeRelativePath) ) {
                            RaspUtils.SafeFileCopy(file, Path.Combine(currentBranchDir, mergeRelativePath));
                            currentHashes[mergeRelativePath] = mergeHash;
                            currentIndex[mergeRelativePath]["hash"] = mergeIndex[mergeRelativePath]["hash"];
                            currentIndex[mergeRelativePath]["status"] = mergeIndex[mergeRelativePath]["status"];
                            currentIndex[mergeRelativePath]["lastCommit"] = mergeIndex[mergeRelativePath]["lastCommit"];
                            mergedCount++;
                            continue;
                        }

                        if ( currentHashes.TryGetValue(mergeRelativePath, out var currentHash) ) {
                            if ( currentHash == mergeHash ) {
                                skippedCount++;
                                continue;
                            }

                            if ( initialHashes.TryGetValue(mergeRelativePath, out var initialHash) &&
                                currentHash == initialHash ) {
                                File.Copy(file, Path.Combine(currentBranchDir, mergeRelativePath), true);
                                currentHashes[mergeRelativePath] = mergeHash;
                                currentIndex[mergeRelativePath]["hash"] = mergeIndex[mergeRelativePath]["hash"];
                                currentIndex[mergeRelativePath]["status"] = mergeIndex[mergeRelativePath]["status"];
                                currentIndex[mergeRelativePath]["lastCommit"] = mergeIndex[mergeRelativePath]["lastCommit"];
                                mergedCount++;
                            } else {
                                conflictCount++;
                                RaspUtils.DisplayMessage($"Conflict in '{mergeRelativePath}'.", Color.Yellow);
                            }
                        }
                    } catch ( Exception ) {
                        failedCount++;
                    }
                }

                RaspUtils.SaveJson(currentIndexFile, currentIndex); 
                RaspUtils.DisplayMessage($"Merge complete: {mergedCount} merged, {skippedCount} skipped, {conflictCount} conflicts, {failedCount} failed.", Color.Green);
                
                if ( failedCount == 0 && conflictCount == 0 ) {
                    Console.WriteLine($"{mergeBranch} branch can be safely deleted now.");
                    Console.Write($"Do you want to delete '{mergeBranch}' branch ? (y/n) : ");
                    string input = Console.ReadLine()!.Trim().ToLower();
                    if ( input == "y" ) {
                        Directory.Delete(mergeBranchDir, true);
                        RaspUtils.DisplayMessage($"'{mergeBranch}' branch deleted successfully.", Color.Green);
                    }
                }
            } catch ( Exception ex ) {
                RaspUtils.DisplayMessage($"Error: {ex.Message}", Color.Red);
            }
        }
    }



    public class AddCommand : ICommand {
        public string Usage => $"{Commands.add} <file/directory>";

        public void Execute( string[] args ) {
            if ( args.IsNotValid(Usage) ) return;

            string currentDir = Directory.GetCurrentDirectory();
            string configFile = Paths.ConfigFile;

            if ( RaspUtils.IsMissing(configFile) ) return;

            Dictionary<string, string> config = RaspUtils.LoadJson<string>(configFile)!;
            string indexFile = Path.Combine(Paths.BranchesDir, $"{config["branch"]}/index.json");

            if ( RaspUtils.IsMissing(indexFile) ) return;
            var index = RaspUtils.LoadJson<Dictionary<string, string>>(indexFile);

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
            int addedCount = 0, failedCount = 0, skippedCount = 0;

            foreach ( string filePath in filesToAdd ) {
                try {
                    string relativePath = Path.GetRelativePath(currentDir, filePath);

                    if ( index.TryGetValue(relativePath, out var existingEntry) && existingEntry["status"] == "tracked" ) {
                        skippedCount++;
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
            RaspUtils.SaveJson(indexFile, index);
            Console.WriteLine("Add operation completed:");
            Console.ForegroundColor = ConsoleColor.Green;
            Console.Write($"Added: {addedCount}   ");
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.Write($"Skipped: {skippedCount}   ");
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine($"Failed: {failedCount}");
            Console.ResetColor();
        }
    }



    public class CommitCommand : ICommand {
        public string Usage => $"{Commands.commit} {Commands._message} <message>";

        public void Execute( string[] args ) {
            if ( args.IsNotValid(Usage) ) return;

            string message = args[2];
            string configFile = Paths.ConfigFile;
            string logsFile = Paths.LogsFile;
            Dictionary<string, string>? logs = RaspUtils.LoadJson<string>(logsFile);

            try {

                Dictionary<string, string>? config = RaspUtils.LoadJson<string>(configFile);
                if ( config == null ) {
                    RaspUtils.DisplayMessage("Error: Config file is missing or corrupted.", Color.Red);
                    return;
                }
                string indexFile = Path.Combine(Paths.BranchesDir, $"{config["branch"]}/index.json");
                string commitsDir = Path.Combine(Paths.BranchesDir, $"{config["branch"]}/commits");
                if ( RaspUtils.IsMissing(indexFile) ) return;
                if ( RaspUtils.IsMissing(commitsDir) ) return;
                    
                Dictionary<string, Dictionary<string, string>>? index = RaspUtils.LoadJson<Dictionary<string,string>>(indexFile);
                using Stream stream = new MemoryStream(Encoding.UTF8.GetBytes($"{config["author"]} {message} {DateTime.Now}"));
                string hash = RaspUtils.ComputeHashCode(stream);
                string hashDir = Path.Combine(commitsDir, hash);
                Directory.CreateDirectory(hashDir);

                string historyFile = Path.Combine(Paths.BranchesDir, $"{config["branch"]}/commits/.history.json");
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
                            entry.Value["lastCommit"] = hash;
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

                logs[DateTime.Now.ToString()] = $"{config["author"]} committed {count} changes in {config["branch"]} branch.";
                RaspUtils.SaveJson(logsFile, logs);
                string commitFile = Path.Combine(hashDir, "commit.json");
                RaspUtils.SaveJson(commitFile, commit);
                RaspUtils.DisplayMessage("Changes committed.", Color.Green);
            } catch ( Exception ex ) {
                RaspUtils.DisplayMessage($"Error: {ex.Message}", Color.Red);
            }
        }
    }


    public class RollbackCommand : ICommand {
        public string Usage => $"{Commands.rollback} or {Commands.rollback}";

        public void Execute( string[] args ) {
            if ( args.IsNotValid(Usage) ) return;

            string configFile = Paths.ConfigFile;

            if ( RaspUtils.IsMissing(configFile) ) return;
            Dictionary<string, string> config = RaspUtils.LoadJson<string>(configFile);
            string indexFile = Path.Combine(Paths.BranchesDir, $"{config["branch"]}/index.json");
            string commitsDir = Path.Combine(Paths.BranchesDir, $"{config["branch"]}/commits");
            string historyFile = Path.Combine(Paths.BranchesDir, $"{config["branch"]}/commits/.history.json");

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
        public string Usage => $"{Commands.status}";
        public void Execute( string[] args ) {
            if ( args.IsNotValid(Usage) ) return;

            string configFile = Paths.ConfigFile;
            if ( RaspUtils.IsMissing(configFile) ) return;
            Dictionary<string, string> config = RaspUtils.LoadJson<string>(configFile);

            if ( config == null ) {
                RaspUtils.DisplayMessage("Error: Config file is missing or corrupted.", Color.Red);
                return;
            }

            string indexFile = Path.Combine(Paths.BranchesDir, $"{config["branch"]}/index.json");
            if ( RaspUtils.IsMissing(indexFile) ) return;
            Dictionary<string, Dictionary<string, string>>? index = RaspUtils.LoadJson<Dictionary<string, string>>(indexFile);
            List<string> stagedList = [];
            List<string> committedList = [];
            List<string> untrackedList = [];

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

            foreach(var file in Directory.GetFiles(Directory.GetCurrentDirectory(), "*", SearchOption.AllDirectories) ) {
                string relativePath = Path.GetRelativePath(Directory.GetCurrentDirectory(), file);
                if ( !index.ContainsKey(relativePath) ) {
                    untrackedList.Add(relativePath);
                }
            }

            Console.WriteLine("Staging area:");
            RaspUtils.DisplayMessage($" Tracked: {stagedList.Count}", Color.Yellow);
            foreach ( var staged in stagedList ) {
                Console.WriteLine($" |- {staged}");
            }

            RaspUtils.DisplayMessage($" Committed: {committedList.Count}", Color.Green);
            foreach ( var committed in committedList ) {
                Console.WriteLine($" |- {committed}");
            }

            RaspUtils.DisplayMessage($" Untracked: {untrackedList.Count}", Color.Red);
            foreach ( var untracked in untrackedList ) {
                Console.WriteLine($" |- {untracked}");
            }
        }
    }

    public class RevertCommand : ICommand {
        public string Usage => $"{Commands.revert} <file>";

        public void Execute( string[] args ) {

            if (args.IsNotValid(Usage)) return;

            string filepath = args[1];

            string configFile = Paths.ConfigFile;
            if ( RaspUtils.IsMissing(configFile) ) return;
            Dictionary<string, string> config = RaspUtils.LoadJson<string>(configFile);

            if ( config == null ) {
                RaspUtils.DisplayMessage("Error: Config file is missing or corrupted.", Color.Red);
                return;
            }

            string indexFile = Path.Combine(Paths.BranchesDir, $"{config["branch"]}/index.json");
            Dictionary<string, Dictionary<string, string>>? index = RaspUtils.LoadJson<Dictionary<string, string>>(indexFile);


            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine($"Are you sure you want to revert '{filepath}'? (y/n)");
            string? input = Console.ReadLine()?.Trim().ToLower();
            Console.ResetColor();

            if ( input != "y" ) return;

            if ( index == null || !index.ContainsKey(filepath) ) {
                RaspUtils.DisplayMessage($"Error: File '{filepath}' not found in the index or Index file is missing.", Color.Red);
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
        public string Usage => $"{Commands._profile} <author> <mail>";

        public void Execute( string[] args ) {
            if (args.IsNotValid(Usage)) return;

            try {
                string configFile = Paths.ConfigFile;
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

    public class HistoryCommmand : ICommand {
        public string Usage => $"{Commands.history}";
        public void Execute( string[] args ) {
            if ( args.IsNotValid(Usage)) return;    

            string configFile = Paths.ConfigFile;
            if ( RaspUtils.IsMissing(configFile) ) return;

            Dictionary<string, string> config = RaspUtils.LoadJson<string>(configFile)!;
            string commitsDir = Path.Combine(Paths.BranchesDir, $"{config["branch"]}/commits");
            string historyFile = Path.Combine(commitsDir, ".history.json");

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

                var commitData = RaspUtils.LoadJson<object>(commitFile);
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

    public class LogsCommand : ICommand {
        public string Usage => $"{Commands.logs}";

        public void Execute( string[] args ) {
            if (args.IsNotValid(Usage)) return;

            string logsFile = Paths.LogsFile;

            if( RaspUtils.IsMissing(logsFile) ) return;

            Dictionary<string, string> logs = RaspUtils.LoadJson<string>(logsFile);

            foreach ( var log in logs ) {
                Console.WriteLine(log.Key);
                Console.WriteLine(log.Value);
                Console.WriteLine();
            }
        }
    }

    public class DropCommand : ICommand {
        public string Usage => $"{Commands.drop}";

        public void Execute( string[] args ) {
            if(args.IsNotValid(Usage)) return;
            
            string raspDirectory = Paths.RaspDir;
            string configFile = Paths.ConfigFile;
            string azConfigFile = Paths.AzConfigFile;

            if ( RaspUtils.IsMissing(configFile) ) return;

            Dictionary<string, string> config = RaspUtils.LoadJson<string>(configFile);

            if ( !Directory.Exists(Paths.MainDir)) {
                RaspUtils.DisplayMessage("Error: The detected '.rasp' folder does not contain a valid Rasp repository.", Color.Red);
                return;
            }

            if ( Directory.Exists(raspDirectory) ) {
                Console.ForegroundColor = ConsoleColor.Yellow;
                Console.Write("Do you want to delete the Rasp repository? (y/n): ");
                string? input = Console.ReadLine()?.Trim().ToLower();
                Console.ResetColor();

                if ( input != "y" ) return;
                
                config["branch"] = "main";
                RaspUtils.SaveJson<string>(configFile, config);
                Directory.Delete(raspDirectory, true);
                File.Delete(azConfigFile);
                RaspUtils.DisplayMessage("Rasp repository deleted successfully.", Color.Green);
            } else {
                RaspUtils.DisplayMessage("Error: No Rasp repository was innitialized in current directory.", Color.Red);
            }
        }
    }
}
