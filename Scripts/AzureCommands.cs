using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using System.Text.RegularExpressions;

namespace Rasp {

    public class PushCommand : ICommand {
        public string Usage => $"{Commands.rasp} {Commands.push}";

        public void Execute( string[] args ) {
            if ( args.Length != 1 ) {
                Console.WriteLine("Usage: " + Usage);
                return;
            }

            int success = 0, failed = 0, skipped = 0;
            string path = Directory.GetCurrentDirectory();
            string containerName = SanitizeContainerName(Path.GetFileName(path));
            string indexFile = Path.Combine(path, ".rasp/index.json");
            string azConfigFile = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "rasp/azConfig.json");

            if( !File.Exists(azConfigFile) ) {
                RaspUtils.DisplayMessage($"Error: Storage file {azConfigFile} is missing.", Color.Red);
                return;
            }

            if ( !File.Exists(indexFile) ) {
                RaspUtils.DisplayMessage($"Error: Index file {indexFile} is missing.", Color.Red);
                return;
            }
            Dictionary<string,string> azConfig = RaspUtils.LoadJson<string>(azConfigFile);
            Dictionary<string, Dictionary<string, string>> index = RaspUtils.LoadJson<Dictionary<string, string>>(indexFile);

            if ( azConfig == null || azConfig.Count == 0) {
                RaspUtils.DisplayMessage("Error: Could not load Connection string", Color.Red);
                return;
            }

            string connectionString = azConfig["connectionString"];

            if ( index == null ) {
                RaspUtils.DisplayMessage("Error: Could not load index.json", Color.Red);
                return;
            }

            Console.WriteLine($"Uploading committed files to '{containerName}' in Azure Blob Storage...");

            try {
                BlobServiceClient blobServiceClient = new(connectionString);
                BlobContainerClient containerClient = blobServiceClient.GetBlobContainerClient(containerName);

                if ( containerClient.CreateIfNotExists() != null ) {
                    RaspUtils.DisplayMessage($"Container '{containerClient.Name}' created successfully!", Color.Yellow);
                }

                RaspUtils.DisplayMessage("Uploading files:", Color.Yellow);

                foreach ( var entry in index ) {
                    string relativePath = entry.Key;
                    Dictionary<string, string> metadata = entry.Value;
                    string filePath = Path.Combine(path, relativePath);

                    if ( !File.Exists(filePath) ) {
                        RaspUtils.DisplayMessage($"Warning: File '{relativePath}' not found, skipping.", Color.Yellow);
                        skipped++;
                        continue;
                    }

                    if ( metadata["status"] == "committed" ) {
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

                RaspUtils.DisplayMessage($"Upload Summary - Success: {success} | Skipped: {skipped} | Failed: {failed}", Color.Green);
            } catch ( Exception ex ) {
                RaspUtils.DisplayMessage($"Upload Failed: {ex.Message}", Color.Red);
            }
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
    }


    public class PullCommand : ICommand {
        public string Usage => $"{Commands.rasp} {Commands.pull} <connectionString>";

        public void Execute( string[] args ) {
            if ( args.Length != 1 ) {
                Console.WriteLine("Usage: " + Usage);
                return;
            }

            Console.WriteLine("Fetching files from Azure Blob Storage...");
            string path = Directory.GetCurrentDirectory();
            string containerName = SanitizeContainerName(path);
            string azConfigFile = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "rasp/azConfig.json");

            if ( !File.Exists(azConfigFile) ) {
                RaspUtils.DisplayMessage($"Error: Storage file {azConfigFile} is missing.", Color.Red);
                return;
            }
            Dictionary<string, string> azConfig = RaspUtils.LoadJson<string>(azConfigFile);
            if ( azConfig == null || azConfig.Count == 0 ) {
                RaspUtils.DisplayMessage("Error: Could not load Connection string", Color.Red);
                return;
            }

            string connectionString = azConfig["connectionString"];


            try {
                BlobServiceClient blobServiceClient = new(connectionString);
                BlobContainerClient containerClient = blobServiceClient.GetBlobContainerClient(containerName);

                if ( !containerClient.Exists() ) {
                    RaspUtils.DisplayMessage($"Error: Container '{containerName}' does not exist.", Color.Red);
                    return;
                }

                Console.WriteLine($"Downloading files from '{containerName}' container...");


                Directory.CreateDirectory(Path.GetDirectoryName(Directory.GetCurrentDirectory())!);

                RaspUtils.DisplayMessage("Downloading updated files:", Color.Yellow);
                foreach ( var blob in containerClient.GetBlobs() ) {
                    BlobClient fileBlobClient = containerClient.GetBlobClient(blob.Name);
                    string filePath = Path.Combine(path, blob.Name);
                    string? directoryPath = Path.GetDirectoryName(filePath);
                    if ( !string.IsNullOrEmpty(directoryPath) ) {
                        Directory.CreateDirectory(directoryPath);
                    }
                    using FileStream downloadStream = File.OpenWrite(filePath);
                    fileBlobClient.DownloadTo(downloadStream);
                    Console.WriteLine($"|- {blob}");
                }
                RaspUtils.DisplayMessage("Files downloaded successfully!", Color.Green);


            } catch ( Exception ex ) {
                RaspUtils.DisplayMessage($"Download Failed: {ex.Message}", Color.Red);
            }
        }
        private static string SanitizeContainerName( string name ) {
            return Regex.Replace(name.ToLower(), @"[^a-z0-9-]", "-");
        }
    }

    public class CloneCommand : ICommand {
        public string Usage => $"{Commands.rasp} {Commands.pull} <container>";

        public void Execute( string[] args ) {
            if ( args.Length < 2 ) {
                Console.WriteLine("Usage: " + Usage);
                return;
            }

            string path = Directory.GetCurrentDirectory();
            string azConfigFile = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "rasp/azConfig.json");
            if ( !File.Exists(azConfigFile) ) {
                RaspUtils.DisplayMessage($"Error: Storage file {azConfigFile} is missing.", Color.Red); 
                return;
            }

            Dictionary<string, string> azConfig = RaspUtils.LoadJson<string>(azConfigFile);

            if ( azConfig.Count == 0 ) {
                RaspUtils.DisplayMessage($"Warning: connection string not setted.", Color.Yellow);

            }

            string connectionString = azConfig["connectionString"];
            string containerName = args[0];

            Console.WriteLine($"Cloning all files from '{containerName}' container...");

            try {
                BlobServiceClient blobServiceClient = new(connectionString);
                BlobContainerClient containerClient = blobServiceClient.GetBlobContainerClient(containerName);

                if ( !containerClient.Exists() ) {
                    RaspUtils.DisplayMessage($"Error: Container '{containerName}' does not exist.", Color.Red);
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
                RaspUtils.DisplayMessage("All files downloaded successfully!", Color.Green);
            } catch ( Exception ex ) {
                RaspUtils.DisplayMessage($"Download Failed: {ex.Message}", Color.Red);
            }
        }
    }

    public class SetCommand : ICommand {
        public string Usage => $"{Commands.rasp} {Commands.set} <Connection string>";

        public void Execute( string[] args ) {
            if ( args.Length != 2 ) {
                Console.WriteLine($"Usage: {Usage}");
                return;
            }

            string appDataPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "rasp");
            string azConfigFile = Path.Combine(appDataPath, "azConfig.json");

            string connectionString = args[1];

            if ( !IsValidAzureConnectionString(connectionString) ) {
                RaspUtils.DisplayMessage("Error: Invalid Connection string", Color.Red);
                return;
            }

            if ( !Directory.Exists(appDataPath) ) {
                Directory.CreateDirectory(appDataPath);
            }

            if ( !File.Exists(azConfigFile) ) {
                File.WriteAllText(azConfigFile, "{}");
            }

            try {
                var azConfig = RaspUtils.LoadJson<string>(azConfigFile);
                azConfig["connectionString"] = connectionString;

                RaspUtils.SaveJson(azConfigFile, azConfig);
                RaspUtils.DisplayMessage("Connection String Saved", Color.Green);
            } catch ( IOException ioEx ) {
                RaspUtils.DisplayMessage($"File Error: {ioEx.Message}", Color.Red);
            } catch ( Exception ex ) {
                RaspUtils.DisplayMessage($"Unexpected Error: {ex.Message}", Color.Red);
            }
        }

        private static bool IsValidAzureConnectionString( string connectionString ) {
            var requiredKeys = new List<string> { "DefaultEndpointsProtocol", "AccountName", "AccountKey", "EndpointSuffix" };

            var keyPairs = connectionString.Split(';', StringSplitOptions.RemoveEmptyEntries)
                                           .Select(pair => pair.Split('='))
                                           .Where(pair => pair.Length >= 2)
                                           .ToDictionary(pair => pair[0].Trim(), pair => pair[1].Trim());
            return requiredKeys.All(key => keyPairs.ContainsKey(key));
        }
    }
}