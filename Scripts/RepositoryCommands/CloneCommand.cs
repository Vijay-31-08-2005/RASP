using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;

namespace Rasp {
    public class CloneCommand : ICommand {
        public string Usage => $"{Commands.pull} <container-name>";

        public void Execute( string[] args ) {

            string path = Directory.GetCurrentDirectory();
            string azConfigFile = Paths.AzConfigFile;
            string logsFile = Paths.LogsFile;
            string configFile = Paths.ConfigFile;
            if ( !File.Exists(azConfigFile) ) {
                RaspUtils.DisplayMessage($"Error: Storage file {azConfigFile} is missing.", ConsoleColor.Red); 
                return;
            }

            Dictionary<string, string> azConfig = RaspUtils.LoadJson<string>(azConfigFile);

            if ( azConfig.Count == 0 ) {
                RaspUtils.DisplayMessage($"Warning: connection string not setted.", ConsoleColor.Yellow);

            }
            string connectionString = azConfig["connectionString"];
            string containerName = args[0];

            Console.WriteLine($"Cloning all files from '{containerName}' repository...");

            try {
                BlobServiceClient blobServiceClient = new(connectionString);
                BlobContainerClient containerClient = blobServiceClient.GetBlobContainerClient(containerName);

                if ( !containerClient.Exists() ) {
                    RaspUtils.DisplayMessage($"Error: Repository '{containerName}' does not exist.", ConsoleColor.Red);
                    return;
                }

                foreach ( BlobItem blobItem in containerClient.GetBlobs() ) {
                    string localFilePath = Path.Combine(path, blobItem.Name);

                    string? directoryPath = Path.GetDirectoryName(localFilePath);
                    if ( !string.IsNullOrEmpty(directoryPath) ) {
                        Directory.CreateDirectory(directoryPath);
                    }

                    BlobClient blobClient = containerClient.GetBlobClient(blobItem.Name);
                    using FileStream downloadStream = File.OpenWrite(localFilePath);
                    blobClient.DownloadTo(downloadStream);

                    Console.Write($"/r{blobItem.Name}");
                    Console.Write("/r");
                    Thread.Sleep(500);
                }
                RaspUtils.DisplayMessage($"Repository {containerName} cloned successfully!", ConsoleColor.Green);
            } catch ( Exception ex ) {
                RaspUtils.DisplayMessage($"Cloning Failed: {ex.Message}", ConsoleColor.Red);
            }

            if ( !File.Exists(logsFile) || !File.Exists(configFile))
                return;
            var config = RaspUtils.LoadJson<string>(configFile);
            var logs = RaspUtils.LoadJson<string>(logsFile);
            string log = $"{config[Key.author]} cloned repository '{containerName}' to '{path}'";
            logs[DateTime.Now.ToString()] = log;
            RaspUtils.SaveJson(logsFile, logs);
        }

        public void Info() {
            Console.WriteLine("  Clones all files from the specified Azure Blob Storage container to the local directory.");
            Console.WriteLine("  Ensures the necessary directory structure is created before downloading files.");
            Console.WriteLine();
            Console.WriteLine($"Usage: {Commands.rasp} {Commands.clone} [container-name]");
            Console.WriteLine();
            Console.WriteLine("Options:");
            Console.WriteLine("  <container-name>  The name of the Azure Blob Storage container to clone.");
            Console.WriteLine("  -h, --help        Show this help message");
            Console.WriteLine();
        }
    }
}