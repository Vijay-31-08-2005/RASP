using Azure.Storage.Blobs;
using System.Text.RegularExpressions;

namespace Rasp {
    public class PushCommand : ICommand {
        public string Usage => $"{Commands.rasp} {Commands.push} <containerName> <connectionString> <filePath>";

        public void Execute( string[] args ) {

            if ( args.Length < 4 ) {
                Console.WriteLine("Usage: " + Usage);
                return;
            }

            string containerName = args[1];
            string connectionString = args[2];
            string filePath = args[3];

            if ( !File.Exists(filePath) ) {
                Console.WriteLine($"Error: File {Path.GetFileName(filePath)} not found.");
                return;
            }

            try {
                BlobServiceClient blobServiceClient = new(connectionString);

                BlobContainerClient containerClient = blobServiceClient.GetBlobContainerClient(containerName);
                string blobName = Path.GetFileName(filePath);
                BlobClient blobClient = containerClient.GetBlobClient(blobName);

                using FileStream fs = File.OpenRead(filePath);
                blobClient.Upload(fs, true);
                fs.Close();

                Console.WriteLine($"{blobName} uploaded successfully to Azure Blob Storage!");
            } catch ( Exception ex ) {
                Console.WriteLine($"Upload Failed: {ex.Message}");
            }
        }
    }

    public class PullCommand : ICommand {
        public string Usage => $"{Commands.rasp} {Commands.pull} <blobName> <containerName> <connectionString> <directory>";

        public void Execute( string[] args ) {
            if ( args.Length < 4 ) {
                Console.WriteLine("Usage: " + Usage);
                return;
            }

            string blobName = args[1];
            string containerName = args[2];
            string connectionString = args[3];
            string directory = args.Length > 4 ? args[4] : Directory.GetCurrentDirectory();

            try {
                BlobServiceClient blobServiceClient = new(connectionString);
                BlobContainerClient containerClient = blobServiceClient.GetBlobContainerClient(containerName);
                BlobClient blobClient = containerClient.GetBlobClient(blobName);

                if ( !Directory.Exists(directory) ) {
                    Directory.CreateDirectory(directory);
                }

                string filePath = Path.Combine(directory, blobName);

                if ( File.Exists(filePath) ) {
                    filePath = GetUniqueFilePath(directory, blobName);
                }

                blobClient.DownloadTo(filePath);
                Console.WriteLine($"{blobName} downloaded successfully as {Path.GetFileName(filePath)} to {directory}");
            } catch ( Exception ex ) {
                Console.WriteLine($"Download Failed: {ex.Message}");
            }
        }

        private static string GetUniqueFilePath( string directory, string fileName ) {
            string fileNameWithoutExt = Path.GetFileNameWithoutExtension(fileName);
            string extension = Path.GetExtension(fileName);

            Match match = Regex.Match(fileNameWithoutExt, @"^(.*) \((\d+)\)$");

            string baseName = fileNameWithoutExt;
            int count = 1;

            if ( match.Success ) {
                baseName = match.Groups[1].Value;
                count = int.Parse(match.Groups[2].Value) + 1;
            }

            string newFilePath;

            do {
                newFilePath = Path.Combine(directory, $"{baseName} ({count}){extension}");
                count++;
            } while ( File.Exists(newFilePath) );

            return newFilePath;
        }


    }
}