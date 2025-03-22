using Newtonsoft.Json;

namespace Rasp {
    public class HistoryCommmand : ICommand {
        public string Usage => $"{Commands.history}";
        public void Execute( string[] args ) {

            string configFile = Paths.ConfigFile;
            if ( RaspUtils.IsMissing(configFile) )
                return;

            Dictionary<string, string> config = RaspUtils.LoadJson<string>(configFile)!;
            string commitsDir = Path.Combine(Paths.BranchesDir, config[Key.branch], Dir.commits);
            string historyFile = Path.Combine(commitsDir, Files.history);

            if ( !File.Exists(historyFile) ) {
                RaspUtils.DisplayMessage($"Warning: No commits found.", ConsoleColor.Yellow);
                return;
            }

            Dictionary<string, string>? history = RaspUtils.LoadJson<string>(historyFile);
            if ( history!.Equals(null) || history.Count == 0 ) {
                RaspUtils.DisplayMessage($"Warning: No commits found.", ConsoleColor.Yellow);
                return;
            }

            foreach ( var commit in history ) {

                string hash = commit.Value;
                string hashDir = Path.Combine(commitsDir, hash);
                string commitFile = Path.Combine(hashDir, $"{Files.commit}");

                if ( !Directory.Exists(hashDir) || !File.Exists(commitFile) ) {
                    RaspUtils.DisplayMessage($"Error: Commit {hash} data is missing or corrupted.", ConsoleColor.Red);
                    history.Remove(commit.Key);
                    RaspUtils.SaveJson(historyFile, history);
                    return;
                }

                var commitData = RaspUtils.LoadJson<object>(commitFile);
                if ( commitData!.Equals(null) ) {
                    RaspUtils.DisplayMessage($"Error: Commit {hash} data is missing or corrupted.", ConsoleColor.Red);
                    return;
                }

                Console.WriteLine($"Author: {commitData[Key.author]}    Message: {commitData[Key.message]}    Timestamp: {commitData[Key.timeStamp]}");
                Console.WriteLine("Files:");
                foreach ( var file in JsonConvert.DeserializeObject<List<string>>(commitData["files"].ToString()!)! ) {
                    Console.WriteLine($" |- {file}");
                }
                Console.WriteLine();
            }
        }

        public void Info() {
            Console.WriteLine("  Displays the commit history of the current branch.");
            Console.WriteLine("  Shows details such as commit author, message, timestamp, and affected files.");
            Console.WriteLine();
            Console.WriteLine($"Usage: {Commands.rasp} {Commands.history}");
            Console.WriteLine();
            Console.WriteLine("Options:");
            Console.WriteLine("  -h, --help        Show this help message.");
            Console.WriteLine();
        }
    }
}
