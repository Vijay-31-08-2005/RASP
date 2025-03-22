using Azure.Storage.Blobs;
using System.Text.RegularExpressions;

namespace Rasp {
    public class PushCommand : ICommand {
        public string Usage => $"{Commands.push}";

        public void Execute( string[] args ) {

            int success = 0, failed = 0, skipped = 0;
            string path = Directory.GetCurrentDirectory();
            string containerName = SanitizeContainerName(Path.GetFileName(path));
            string configFile = Paths.ConfigFile;
            string branchesDir = Paths.BranchesDir;
            string raspDir = Paths.RaspDir;
            var config = RaspUtils.LoadJson<string>(configFile);
            string indexFile = Path.Combine(branchesDir, $"{config[Key.branch]}/{Files.index}");
            string azConfigFile = Paths.AzConfigFile;
            string logsFile = Paths.LogsFile;

            if ( !File.Exists(azConfigFile) ) {
                RaspUtils.DisplayMessage($"Error: Storage file {azConfigFile} is missing.", ConsoleColor.Red);
                return;
            }

            if ( !File.Exists(indexFile) ) {
                RaspUtils.DisplayMessage($"Error: Index file {indexFile} is missing.", ConsoleColor.Red);
                return;
            }

            if ( !File.Exists(logsFile) ) {
                RaspUtils.DisplayMessage($"Error: Logs file {logsFile} is missing.", ConsoleColor.Red);
                return;
            }

            Dictionary<string,string> azConfig = RaspUtils.LoadJson<string>(azConfigFile);
            Dictionary<string,string> logs = RaspUtils.LoadJson<string>(logsFile);
            Dictionary<string, Dictionary<string, string>> index = RaspUtils.LoadJson<Dictionary<string, string>>(indexFile);

            if ( azConfig == null || azConfig.Count == 0) {
                RaspUtils.DisplayMessage("Error: Could not load Connection string", ConsoleColor.Red);
                return;
            }

            string connectionString = Encryption.Decrypt(azConfig[Key.connectionString], azConfig[Key.key]);

            if ( index == null ) {
                RaspUtils.DisplayMessage("Error: Could not load index.json", ConsoleColor.Red);
                return;
            }

            Console.WriteLine($"Uploading committed files to '{containerName}' in Azure Blob Storage...");

            try {
                BlobServiceClient blobServiceClient = new(connectionString);
                BlobContainerClient containerClient = blobServiceClient.GetBlobContainerClient(containerName);

                if ( containerClient.CreateIfNotExists() != null ) {
                    RaspUtils.DisplayMessage($"Container '{containerClient.Name}' created successfully!", ConsoleColor.Yellow);
                }

                RaspUtils.DisplayMessage("Uploading files:", ConsoleColor.Yellow);
                foreach (var file in Directory.GetFiles(raspDir, "*", SearchOption.AllDirectories) ) {
                    UploadFile(containerClient, file, Path.GetRelativePath(Directory.GetCurrentDirectory(), file));
                }                
                foreach ( var entry in index ) {
                    string relativePath = entry.Key;
                    Dictionary<string, string> metadata = entry.Value;
                    string filePath = Path.Combine(path, relativePath);

                    if ( !File.Exists(filePath) ) {
                        RaspUtils.DisplayMessage($"Warning: File '{relativePath}' not found, skipping.", ConsoleColor.Yellow);
                        skipped++;
                        continue;
                    }

                    if ( metadata[Key.status] == "committed" ) {
                        try {
                            UploadFile(containerClient, filePath, relativePath);
                            success++;
                        } catch ( Exception ) {                         
                            failed++;
                        }
                    } else {
                        skipped++;
                    }
                }

                RaspUtils.DisplayMessage($"Upload Summary - Success: {success} | Skipped: {skipped} | Failed: {failed}", ConsoleColor.Green);
            } catch ( Exception ex ) {
                RaspUtils.DisplayMessage($"Upload Failed: {ex.Message}", ConsoleColor.Red);
            }

            logs[DateTime.Now.ToString()] = $"Pushed committed files to '{containerName}' in Azure Blob Storage.";
            RaspUtils.SaveJson(logsFile, logs);
        }

        private static void UploadFile( BlobContainerClient containerClient, string filePath, string blobName ) {
            BlobClient blobClient = containerClient.GetBlobClient(blobName);
            using FileStream fs = File.OpenRead(filePath);
            blobClient.Upload(fs, true);
            Console.WriteLine($" |- {blobName}");
        }

        private static string SanitizeContainerName( string name ) {
            return Regex.Replace(name.ToLower(), @"[^a-z0-9-]", "-");
        }

        public void Info() {
            Console.WriteLine("  Uploads committed files to the corresponding Azure Blob Storage container.");
            Console.WriteLine("  The container name is derived from the current directory.");
            Console.WriteLine();
            Console.WriteLine($"Usage: {Commands.rasp} {Commands.push}");
            Console.WriteLine();
            Console.WriteLine("Options:");
            Console.WriteLine("  -h, --help        Show this help message.");
            Console.WriteLine();
        }

    }
}