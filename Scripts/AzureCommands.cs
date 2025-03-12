using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using LibGit2Sharp;
using System.Text.RegularExpressions;

namespace Rasp {

    public class PushCommand : ICommand {
        public string Usage => $"{Commands.rasp} {Commands.push} <connectionString>";

        public void Execute( string[] args ) {
            if ( args.Length < 2 ) {
                Console.WriteLine("Usage: " + Usage);
                return;
            }

            string path = Directory.GetCurrentDirectory();
            string containerName = SanitizeContainerName(Path.GetFileName(path));
            string connectionString = args[1];
            string hashFilePath = Path.Combine(path, ".rasp/hash.json");

            if ( !File.Exists(hashFilePath) ) {
                RaspUtils.DisplayMessage($"Error: {hashFilePath} is missing.", Color.Red);
                return;
            }
            Dictionary<string, string> hashData = RaspUtils.LoadJson<string>(hashFilePath);

            Console.WriteLine($"Uploading '{path}' to '{containerName}' container in Azure Blob Storage...");
            try {
                BlobServiceClient blobServiceClient = new(connectionString);
                BlobContainerClient containerClient = blobServiceClient.GetBlobContainerClient(containerName);

                if ( containerClient.CreateIfNotExists() != null ) {
                    RaspUtils.DisplayMessage($"Container '{containerClient.Name}' created successfully!", Color.Yellow);
                }

                if ( Directory.Exists(path) ) {
                    RaspUtils.DisplayMessage("Uploaded files:", Color.Yellow);
                    foreach ( var file in Directory.GetFiles(path, "*", SearchOption.AllDirectories) ) {
                        string relativePath = Path.GetRelativePath(path, file);
                        using FileStream stream = File.OpenRead(file);
                        string fileHash = RaspUtils.ComputeHashCode(stream);

                        if ( hashData.TryGetValue(relativePath, out string? storedHash) && storedHash == fileHash ) {
                            continue;
                        }

                        UploadFile(containerClient, file, relativePath);
                        hashData[relativePath] = fileHash;
                    }

                    RaspUtils.SaveJson(hashFilePath, hashData);
                } else {
                    RaspUtils.DisplayMessage($"Error: File or directory '{path}' not found.", Color.Red);
                    return;
                }

                RaspUtils.DisplayMessage($"{path} uploaded successfully to Azure Blob Storage!", Color.Green);
            } catch ( Exception ex ) {
                RaspUtils.SaveJson(hashFilePath, hashData);
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
            if ( args.Length < 2 ) {
                Console.WriteLine("Usage: " + Usage);
                return;
            }

            Console.WriteLine("Fetching files from Azure Blob Storage...");
            string path = Directory.GetCurrentDirectory();
            string containerName = SanitizeContainerName(path);
            string connectionString = args[1];

            try {
                BlobServiceClient blobServiceClient = new(connectionString);
                BlobContainerClient containerClient = blobServiceClient.GetBlobContainerClient(containerName);

                if ( !containerClient.Exists() ) {
                    RaspUtils.DisplayMessage($"Error: Container '{containerName}' does not exist.", Color.Red);
                    return;
                }

                Console.WriteLine($"Downloading files from '{containerName}' container...");
                string remoteHashFile = ".rasp/hash.json";
                BlobClient hashBlobClient = containerClient.GetBlobClient(remoteHashFile);

                if ( !hashBlobClient.Exists() ) {
                    RaspUtils.DisplayMessage($"Error: '{remoteHashFile}' not found in Azure.", Color.Red);
                    return;
                }

                string localHashPath = Path.Combine(path, remoteHashFile);

                Directory.CreateDirectory(Path.GetDirectoryName(localHashPath)!);
                hashBlobClient.DownloadTo(localHashPath);

                Dictionary<string, string> remoteHashes = RaspUtils.LoadJson<string>(localHashPath);
                Dictionary<string, string> localHashes = RaspUtils.LoadJson<string>(localHashPath);

                HashSet<string> updatedFiles = [];
                foreach ( var entry in remoteHashes ) {
                    string localFilePath = Path.Combine(path, entry.Key);

                    if ( !localHashes.TryGetValue(entry.Key, out string? localHash) || localHash != entry.Value ) {
                        updatedFiles.Add(entry.Key);
                    }
                }

                if ( updatedFiles.Count > 0 ) {
                    RaspUtils.DisplayMessage("Downloading updated files:", Color.Yellow);
                    foreach ( var fileName in updatedFiles ) {
                        BlobClient fileBlobClient = containerClient.GetBlobClient(fileName);
                        string localFilePath = Path.Combine(path, fileName);

                        Directory.CreateDirectory(Path.GetDirectoryName(localFilePath)!);
                        fileBlobClient.DownloadTo(localFilePath);

                        Console.WriteLine($"|- {fileName}");
                    }
                    RaspUtils.DisplayMessage("All updated files downloaded successfully!", Color.Green);
                } else {
                    RaspUtils.DisplayMessage("All files are up to date. No downloads required.", Color.Green);
                }

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
            string azStorage = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "rasp/azStorage.json");
            if ( !File.Exists(azStorage) ) {
                RaspUtils.DisplayMessage($"Error: Storage file {azStorage} is missing.", Color.Red); 
                return;
            }

            Dictionary<string, string> storage = RaspUtils.LoadJson<string>(azStorage);

            if ( storage.Count == 0 ) {
                RaspUtils.DisplayMessage($"Warning: connection string not setted.", Color.Yellow);

            }

            string connectionString = storage["connectionString"];
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
                    Thread.Sleep(500);
                }

                RaspUtils.DisplayMessage("All files downloaded successfully!", Color.Green);
            } catch ( Exception ex ) {
                RaspUtils.DisplayMessage($"Download Failed: {ex.Message}", Color.Red);
            }
        }
    }

    public class SetCommand : ICommand {
        public string Usage => $"{Commands.rasp} {Commands.set} <connectionString>";

        public void Execute( string[] args ) {
            if ( args.Length != 2 ) {
                Console.WriteLine($"Usage: {Usage}");
                return;
            }

            string appDataPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "rasp");
            string azStorage = Path.Combine(appDataPath, "azStorage.json");

            string connectionString = args[1];

            if ( !IsValidAzureConnectionString(connectionString) ) {
                RaspUtils.DisplayMessage("Error: Invalid ConnectionString", Color.Red);
                return;
            }

            if ( !Directory.Exists(appDataPath) ) {
                Directory.CreateDirectory(appDataPath);
            }

            if ( !File.Exists(azStorage) ) {
                File.WriteAllText(azStorage, "{}");
            }

            try {
                var storage = RaspUtils.LoadJson<string>(azStorage) ?? new Dictionary<string, string>();
                storage["connectionString"] = connectionString;

                RaspUtils.SaveJson(azStorage, storage);
                RaspUtils.DisplayMessage("Connection String Saved", Color.Green);
            } catch ( IOException ioEx ) {
                RaspUtils.DisplayMessage($"File Error: {ioEx.Message}", Color.Red);
            } catch ( Exception ex ) {
                RaspUtils.DisplayMessage($"Unexpected Error: {ex.Message}", Color.Red);
            }
        }

        private static bool IsValidAzureConnectionString( string connectionString ) {
            var requiredKeys = new List<string> { "DefaultEndpointsProtocol", "AccountName", "AccountKey" };

            var keyPairs = connectionString.Split(';', StringSplitOptions.RemoveEmptyEntries)
                                           .Select(pair => pair.Split('='))
                                           .Where(pair => pair.Length == 2)
                                           .ToDictionary(pair => pair[0].Trim(), pair => pair[1].Trim());

            return requiredKeys.All(key => keyPairs.ContainsKey(key));
        }
    }
}