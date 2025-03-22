using System.Text;

namespace Rasp {
    public class MergeCommand : ICommand {
        public string Usage => $"{Commands.merge} <branch>";

        public void Execute( string[] args ) {

            string mergeBranch = args[1];

            if ( mergeBranch.Equals(Value.main, StringComparison.OrdinalIgnoreCase) ) {
                RaspUtils.DisplayMessage("Error: Cannot merge main branch to any branch", ConsoleColor.Red);
                return;
            }

            string configFile = Paths.ConfigFile;
            string branchesFile = Paths.BranchesFile;
            string logsFile = Paths.LogsFile;
            string mergeBranchDir = Path.Combine(Paths.BranchesDir, mergeBranch);
            string initialCommitDir = Path.Combine(mergeBranchDir, Dir.commits, Dir.initialCommit);
            string initialIndexFile = Path.Combine(initialCommitDir, Files.index);
            string mergeIndexFile = Path.Combine(mergeBranchDir, Files.index);

            if ( RaspUtils.IsMissing(branchesFile) || RaspUtils.IsMissing(configFile) )
                return;

            var logs = RaspUtils.LoadJson<string>(logsFile);
            var branches = RaspUtils.LoadJson<HashSet<string>>(branchesFile);
            var config = RaspUtils.LoadJson<string>(configFile);

            if ( (branches == null || config == null || 
                !branches.TryGetValue(Key.branches, out HashSet<string>? branch)) || !config.TryGetValue(Key.branch, out string? currentBranch) ) {
                RaspUtils.DisplayMessage("Error: Invalid repository configuration.", ConsoleColor.Red);
                return;
            }

            if ( !branch.Contains(mergeBranch) ) {
                RaspUtils.DisplayMessage($"Error: Branch '{mergeBranch}' does not exist.", ConsoleColor.Red);
                return;
            }

            if ( RaspUtils.IsMissing(mergeIndexFile) )
                return;
            if ( RaspUtils.IsMissing(logsFile) )
                return;
            if ( currentBranch.Equals(mergeBranch, StringComparison.OrdinalIgnoreCase) ) {
                RaspUtils.DisplayMessage("Error: Cannot merge into the same branch", ConsoleColor.Red);
                return;
            }

            string currentBranchDir = Path.Combine(Paths.BranchesDir, $"{currentBranch}");

            if ( !Directory.Exists(currentBranchDir) || !Directory.Exists(mergeBranchDir) ) {
                RaspUtils.DisplayMessage("Error: One of the branch directories is missing.", ConsoleColor.Red);
                return;
            }
            string currentIndexFile = Path.Combine(currentBranchDir, Files.index);
            string currentHistoryFile = Path.Combine(currentBranchDir, Dir.commits, Files.history);
            string message = $"Merged {mergeBranch} into {currentBranch}.";

            if ( RaspUtils.IsMissing(currentIndexFile) )
                return;
            if ( RaspUtils.IsMissing(currentHistoryFile) )
                return;

            using Stream stream = new MemoryStream(Encoding.UTF8.GetBytes($"{config[Key.author]} {message} {DateTime.Now}"));
            string hash = RaspUtils.ComputeHashCode(stream);
            string hashDir = Path.Combine(currentBranchDir, Dir.commits, hash);
            string commitFile = Path.Combine(hashDir, $"{Files.commit}");

            Directory.CreateDirectory(hashDir);
            File.WriteAllText(commitFile, "{}");

            var currentHistory = RaspUtils.LoadJson<string>(currentHistoryFile);
            var currentIndex = RaspUtils.LoadJson<Dictionary<string, string>>(currentIndexFile);
            var mergeIndex = RaspUtils.LoadJson<Dictionary<string, string>>(mergeIndexFile);
            var initialIndex = RaspUtils.LoadJson<Dictionary<string, string>>(initialIndexFile);

            Dictionary<string, Dictionary<string, string>> mergedIndex = new(currentIndex);
            List<string> filesList = [];
            List<string> conflists = [];

            int mergedCount = 0, conflictCount = 0, failedCount = 0;

            try {

                foreach ( var file in initialIndex.Keys.Union(currentIndex.Keys).Union(mergeIndex.Keys) ) {
                    try {
                        bool inInitial = initialIndex.ContainsKey(file);
                        bool inCurrent = currentIndex.ContainsKey(file);
                        bool inMerge = mergeIndex.ContainsKey(file);

                        string? initialHash = inInitial ? initialIndex[file][Key.hash] : null;
                        string? currentHash = inCurrent ? currentIndex[file][Key.hash] : null;
                        string? mergeHash = inMerge ? mergeIndex[file][Key.hash] : null;

                        bool notChanged = ( currentHash!.Equals(mergeHash) );

                        string initialFile = Path.Combine(initialCommitDir, Dir.commits, initialIndex[file][Key.lastCommit], file);
                        string currentFile = Path.Combine(currentBranchDir, Dir.commits, currentIndex[file][Key.lastCommit], file);
                        string mergeFile = Path.Combine(mergeBranchDir, Dir.commits, mergeIndex[file][Key.lastCommit], file);
                        string finalFile = Path.Combine(hashDir, file);

                        if ( inCurrent && !inMerge ) {
                            continue;
                        } else if ( !inCurrent && inMerge ) {
                            mergedIndex[file] = mergeIndex[file];
                            RaspUtils.SafeFileCopy(mergeFile, finalFile);
                            mergedCount++;
                        } else if ( inCurrent && inMerge && notChanged ) {
                            mergedIndex[file] = currentIndex[file];
                            RaspUtils.SafeFileCopy(currentFile, finalFile);
                            mergedCount++;
                        } else if ( inInitial && inCurrent && inMerge ) {
                            if ( !notChanged ) {
                                if ( ResolveConflict(initialFile, currentFile, mergeFile, finalFile) ) {
                                    conflists.Add(file);
                                    conflictCount++;
                                } else {
                                    mergedCount++;
                                }
                            } else {
                                mergedIndex[file] = currentIndex[file];
                                RaspUtils.SafeFileCopy(currentFile, finalFile);
                                mergedCount++;
                            }
                        }

                        filesList.Add(file);

                        using FileStream fileStream = File.OpenRead(finalFile);
                        mergedIndex[file][Key.hash] = RaspUtils.ComputeHashCode(fileStream);
                        mergedIndex[file][Key.lastCommit] = hash;

                    } catch ( Exception ex) {
                        failedCount++;
                        RaspUtils.DisplayError(ex);
                    }
                }

                Dictionary<string, object> commit = new() {
                    { Key.id, hash },
                    { Key.message, message },
                    { Key.author, config.TryGetValue(Key.author, out string? user) ? user : Value.unknown },
                    { Key.timeStamp, DateTime.Now.ToString() },
                    { Key.files, filesList }
                };

                currentHistory.Add(DateTime.Now.ToString(), hash);
                logs[DateTime.Now.ToString()] = $"{message} by {user}.";

                UpdateCD(mergedIndex, currentBranchDir);

                RaspUtils.DisplayMessage($"Merge complete: ", ConsoleColor.Green);
                RaspUtils.WriteColor($"Merged: {mergedCount}    ", ConsoleColor.Green);
                RaspUtils.WriteColor($"Conflicts: {conflictCount}    ", ConsoleColor.Yellow);
                RaspUtils.WriteColor($"Failed: {failedCount}", ConsoleColor.Red);
                Console.WriteLine();

                if ( failedCount == 0 && conflictCount == 0 ) {
                    Console.WriteLine($"{mergeBranch} branch can be safely deleted now.");
                    Console.Write($"Do you want to delete '{mergeBranch}' branch ? (y/n) : ");
                    string input = Console.ReadLine()!.Trim().ToLower();
                    if ( input.Equals(Value.y) ) {
                        try {
                            Directory.Delete(mergeBranchDir, true);
                            branches.Remove(mergeBranch);
                            RaspUtils.DisplayMessage($"'{mergeBranch}' branch deleted successfully.", ConsoleColor.Green);
                            logs.Add(DateTime.Now.ToString(), $"{user} deleted branch '{mergeBranch}'.");
                        } catch ( Exception ex ) {
                            RaspUtils.DisplayMessage($"Error: {ex.Message}", ConsoleColor.Red);
                        }
                    }
                } else if ( failedCount == 0 ) {
                    RaspUtils.DisplayMessage("Manual Resolution required", ConsoleColor.Yellow);
                    foreach ( var file in conflists ) {
                        Console.WriteLine($" |- {file}");
                    }
                }

                RaspUtils.SaveJson(branchesFile, branches);
                RaspUtils.SaveJson(logsFile, logs);
                RaspUtils.SaveJson(currentHistoryFile, currentHistory);
                RaspUtils.SaveJson(commitFile, commit);
                RaspUtils.SaveJson(currentIndexFile, mergedIndex);

            } catch ( Exception ex ) {
                RaspUtils.DisplayMessage($"Error: {ex.Message}", ConsoleColor.Red);
            }
        }

        private static bool ResolveConflict( string initialFile, string currentFile, string mergeFile, string finalFile ) {

            if ( !IsTextFile(initialFile) || !IsTextFile(currentFile) || !IsTextFile(mergeFile) ) {
                Console.WriteLine($"'{Path.GetFileName(mergeFile)}' is not in readable format, copying...");
                RaspUtils.SafeFileCopy(mergeFile, finalFile);
                return false;
            }

            List<string> initial = [.. File.ReadAllLines(initialFile)];
            List<string> current = [.. File.ReadAllLines(currentFile)];
            List<string> merge = [.. File.ReadAllLines(mergeFile)];

            List<string> final = MergeFiles(initial, current, merge, out bool conflict);

            string? directory = Path.GetDirectoryName(finalFile);
            if( !Directory.Exists(directory) ) {
                Directory.CreateDirectory(directory!);
            }

            File.WriteAllLines(finalFile, final);
            return conflict;
        }

        private static bool IsTextFile( string filePath, int sampleSize = 8000 ) {
            try {
                using var stream = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.Read);
                byte[] buffer = new byte[sampleSize];
                int bytesRead = stream.Read(buffer, 0, sampleSize);

                for ( int i = 0; i < bytesRead; i++ ) {
                    byte b = buffer[i];
                    if ( b == 0 )
                        return false;
                    if ( b < 9 || ( b > 13 && b < 32 ) || b == 127 )
                        return false;
                }
                return true;
            } catch {
                return false;
            }
        }

        private static void UpdateCD( Dictionary<string, Dictionary<string, string>> index, string currentBranchDir ) {
            foreach ( var entry in index ) {
                if ( entry.Value[Key.status].Equals(Status.committed) ) {
                    string source = Path.Combine(currentBranchDir, $"commits/{entry.Value[Key.lastCommit]}/{entry.Key}");
                    string destination = Path.Combine(Directory.GetCurrentDirectory(), entry.Key);
                    RaspUtils.SafeFileCopy(source, destination);
                }
            }
        }

        private static List<string> MergeFiles( List<string> old, List<string> current, List<string> merge, out bool conflict ) {
            int maxLines = Math.Max(Math.Max(old.Count, current.Count), merge.Count);
            bool hasConflict = false;
            List<string> mergedResult = [];

            for ( int i = 0; i < maxLines; i++ ) {
                string oldLine = i < old.Count ? old[i] : "";
                string currentLine = i < current.Count ? current[i] : "";
                string mergeLine = i < merge.Count ? merge[i] : "";

                if ( currentLine.Equals(mergeLine) ) {
                    mergedResult.Add(currentLine);
                    continue;
                } else if ( oldLine.Equals(currentLine) ) {
                    mergedResult.Add(mergeLine);
                    continue;
                } else if ( oldLine.Equals(mergeLine) ) {
                    mergedResult.Add(currentLine);
                    continue;
                } else {
                    hasConflict = true;
                    mergedResult.Add($"<<<<<<< CURRENT\n{currentLine}\n=======\n{mergeLine}\n>>>>>>> MERGE");
                }
            }

            conflict = hasConflict;
            return mergedResult;
        }

        public void Info() {
            Console.WriteLine("  Merges the specified branch into the current branch.");
            Console.WriteLine("  If conflicts occur, they must be resolved manually.");
            Console.WriteLine();
            Console.WriteLine($"Usage: {Commands.rasp} {Commands.merge} [options]");
            Console.WriteLine();
            Console.WriteLine("Options:");
            Console.WriteLine("  <branch>          Branch needs to be merged.");
            Console.WriteLine("  -h, --help        Show this help message.");
            Console.WriteLine();
        }
    }
}
