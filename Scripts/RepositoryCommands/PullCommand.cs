using Azure.Storage.Blobs;
using System.Text.RegularExpressions;

namespace Rasp {
    public class PullCommand : ICommand {
        public string Usage => $"{Commands.pull}";

        public void Execute( string[] args ) {
            string path = Directory.GetCurrentDirectory();
            string containerName = SanitizeContainerName(Path.GetFileName(path));
            string azConfigFile = Paths.AzConfigFile;
            string logsFile = Paths.LogsFile;

            if ( !File.Exists(azConfigFile) ) {
                RaspUtils.DisplayMessage($"Error: Storage file {azConfigFile} is missing.", ConsoleColor.Red);
                return;
            }
            Dictionary<string, string> azConfig = RaspUtils.LoadJson<string>(azConfigFile);
            if ( azConfig == null || azConfig.Count == 0 ) {
                RaspUtils.DisplayMessage("Error: Could not load Connection string", ConsoleColor.Red);
                return;
            }
            if ( RaspUtils.IsMissing(logsFile) )
                return;
            var logs = RaspUtils.LoadJson<string>(logsFile);
            string connectionString = Encryption.Decrypt(azConfig[Key.connectionString], azConfig[Key.key]);

            try {
                BlobServiceClient blobServiceClient = new(connectionString);
                BlobContainerClient containerClient = blobServiceClient.GetBlobContainerClient(containerName);

                if ( !containerClient.Exists() ) {
                    RaspUtils.DisplayMessage($"Error: Container '{containerName}' does not exist.", ConsoleColor.Red);
                    return;
                }

                Console.WriteLine($"Fetching files from '{containerName}' container...");

                Directory.CreateDirectory(Path.GetDirectoryName(Directory.GetCurrentDirectory())!);

                foreach ( var blob in containerClient.GetBlobs() ) {
                    try {
                        BlobClient fileBlobClient = containerClient.GetBlobClient(blob.Name);
                        string filePath = Path.Combine(path, blob.Name);
                        string? directoryPath = Path.GetDirectoryName(filePath);
                        if ( !string.IsNullOrEmpty(directoryPath) ) {
                            Directory.CreateDirectory(directoryPath);
                        }
                        using FileStream downloadStream = File.OpenWrite(filePath);
                        fileBlobClient.DownloadTo(downloadStream);
                        Console.WriteLine($"|- {blob.Name}");
                    } catch (Exception) {
                        RaspUtils.DisplayMessage($"Error: Failed to fetch {blob}", ConsoleColor.Red);
                    }
                }
                RaspUtils.DisplayMessage("Files downloaded successfully!", ConsoleColor.Green);

            } catch ( Exception ex ) {
                RaspUtils.DisplayError(ex);
            }

            logs[DateTime.Now.ToString()] = $"{azConfig[Key.author]} pulled files from '{containerName}' to '{path}'";
            RaspUtils.SaveJson(logsFile, logs);
        }
        private static string SanitizeContainerName( string name ) {
            return Regex.Replace(name.ToLower(), @"[^a-z0-9-]", "-");
        }

        public void Info() {
            Console.WriteLine("  Downloads all files from the corresponding Azure Blob Storage container.");
            Console.WriteLine("  The container name is derived from the current directory.");
            Console.WriteLine();
            Console.WriteLine($"Usage: {Commands.rasp} {Commands.pull}");
            Console.WriteLine();
            Console.WriteLine("Options:");
            Console.WriteLine("  -h, --help        Show this help message.");
            Console.WriteLine();
        }
    }
}