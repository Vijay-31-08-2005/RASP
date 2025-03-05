using LibGit2Sharp;
using Azure.Storage.Blobs;

namespace RASP {
    public static class Commands {
        public const string RASP = "rasp";
        public const string DISPLAY = "display";
        public const string HELP = "-help";
        public const string VERSION = "--version";
        public const string MOVE = "move";
        public const string DELETE = "delete";
        public const string COPY = "copy";
        public const string README = "readme";
        public const string CLONE = "clone";
        public const string UPLOAD = "upload";
        public const string DOWNLOAD = "download";
    }

    public interface ICommand {
        public string Usage {get;}
        public abstract void Execute( string[] args);
    }

    public class HelpCommand : ICommand {

        private readonly string helpText = @$"
Usage:
  rasp<command>[options]

Commands:
  {Commands.DISPLAY}     - Displays a message.
  {Commands.HELP}       - Show help information.
  {Commands.README}      - Show this README file.
  {Commands.VERSION}   - Show the version of RASP.
  {Commands.MOVE}        - Move a file from one location to another.
  {Commands.COPY}        - Copy a file.
  {Commands.DELETE}      - Delete a file.
  {Commands.CLONE}       - Clone a GitHub repository.
  {Commands.UPLOAD}      - Upload a file to Azure Blob Storage.
  {Commands.DOWNLOAD}    - Download a file from Azure Blob Storage.
";

        public string Usage => $"{Commands.RASP} {Commands.HELP}";

        public void Execute( string[] args ) {
            if ( args.Length > 1 ) {
                Console.WriteLine("Usage: " + Usage);
                return;
            }
            Console.WriteLine(helpText);
        }
    }

    public class ReadmeCommand : ICommand {

        private readonly string readmeText = @"
Remote Access Storage Pool (RASP)
==================================

RASP is a command-line tool for managing files and repositories.

Examples:
  rasp display 'Hello World'                           - Prints 'Hello World' to the console.
  rasp move file.txt folder/                           - Moves 'file.txt' to 'folder/'.
  rasp copy file.txt backup/                           - Copies 'file.txt' to 'backup/'.
  rasp delete old.txt                                  - Deletes 'old.txt'.
  rasp clone user repo folder                          - Clones 'repo' from GitHub into 'folder'.
  rasp upload container conn%asdfawe file.txt          - Uploads 'file' to Azure Blob Storage.
  rasp download blob container conn%asdfawe folder     - Downloads 'blob' from Azure Blob Storage.

For more information, use 'rasp -help'.
";
        public string Usage => $"{Commands.RASP} {Commands.README}";

        public void Execute( string[] args ) {
            if ( args.Length > 1 ) {
                Console.WriteLine("Usage: " + Usage);
                return;
            }
            Console.WriteLine(readmeText);
        }
    }

    public class VersionCommand : ICommand {

        private readonly string versionText = $"{Commands.RASP} 1.0.0";
        public string Usage => $"{Commands.RASP} {Commands.VERSION}";

        

        public void Execute( string[] args ) {
            if (args.Length > 1 ) {
                Console.WriteLine("Usage: " + Usage);
                return;
            }

            Console.WriteLine(versionText);
        }
    }

    public class DisplayCommand : ICommand {

        private readonly int messageIndex = 1;
        public string Usage => $"{Commands.RASP} {Commands.DISPLAY} <message>";

        public void Execute(string[] args ) {
            if ( args.Length < 2 ) {
                Console.WriteLine("Usage: " + Usage);
                return;
            }
            Console.WriteLine(args[messageIndex]);
        }
    }


    public class MoveCommand : ICommand {

        private readonly int sourceIndex = 1;
        private readonly int destinationIndex = 2;
        public string Usage => $"{Commands.RASP} {Commands.MOVE} <source> <destination>";

        public void Execute( string[] args ) {
            if ( args.Length < 3 ) {
                Console.WriteLine("Usage: " + Usage);
                return;
            }

            string source = args[sourceIndex];
            string destination = args[destinationIndex];

            if ( !File.Exists(source) ) {
                throw new FileNotFoundException($"Source file '{source}' not found.");
            }

            string finalDestination = destination;
            if ( Directory.Exists(destination) ) {
                finalDestination = Path.Combine(destination, Path.GetFileName(source));
            }

            try {
                File.Move(source, finalDestination);
                Console.WriteLine($"File moved successfully from '{source}' to '{finalDestination}'");
            } catch ( Exception ex ) {
                Console.WriteLine($"Error: {ex.Message}");
            }
        }
    }
    
    public class CopyCommand : ICommand {

        private readonly int sourceIndex = 1;
        private readonly int destinationIndex = 2;
        public string Usage => $"{Commands.RASP} {Commands.COPY} <source> <destination>";

        public void Execute( string[] args ) {
            if ( args.Length < 3 ) {
                Console.WriteLine("Usage: " + Usage);
                return;
            }

            string source = args[sourceIndex];
            string destination = args[destinationIndex];

            if ( !File.Exists(source) ) {
                throw new FileNotFoundException($"Source file '{source}' not found.");
            }

            string finalDestination = destination;
            if ( Directory.Exists(destination) ) {
                finalDestination = Path.Combine(destination, Path.GetFileName(source));
            }

            try {
                File.Copy(source, finalDestination, true);
                Console.WriteLine($"File copied successfully to '{finalDestination}'");
            } catch ( Exception ex ) {
                Console.WriteLine($"Error : {ex.Message}");
            }
        }
    }

    public class DeleteCommand : ICommand {

        private readonly int fileIndex = 1;
        public string Usage => $"{Commands.RASP} {Commands.DELETE} <file>";

        public void Execute( string[] args ) {
            if ( args.Length < 2 ) {
                Console.WriteLine("Usage: " + Usage);
                return;
            }

            string file = args[fileIndex];

            if ( !File.Exists(file) ) {
                throw new FileNotFoundException($"File '{file}' not found.");
            }

            try {
                string fileName = Path.GetFileName(file);
                File.Delete(file);
                Console.WriteLine($"File '{fileName}' deleted successfully.");
            } catch ( Exception ex ) {
                Console.WriteLine($"Error: {ex.Message}");
            }
        }
    }

    public class CloneCommand : ICommand {

        private readonly int usernameIndex = 1;
        private readonly int repositoryIndex = 2;
        private readonly int directoryIndex = 3;
        public string Usage => $"{Commands.RASP} {Commands.CLONE} <username> <repository-name> <directory>";

        public void Execute( string[] args ) {
            if ( args.Length < 3 ) {
                Console.WriteLine("Usage: " + Usage);
                return;
            }

            string url = $"https://github.com/{args[usernameIndex]}/{args[repositoryIndex]}.git";
            string directory = ( args.Length > directoryIndex ) ? args[directoryIndex] : Directory.GetCurrentDirectory();

            try {
                Repository.Clone(url, directory);
                Console.WriteLine($"Repository cloned successfully from '{url}' to '{Path.GetFullPath(directory)}'");
            } catch ( Exception ex ) {
                Console.WriteLine($"Error: {ex.Message}");
            }
        }
    }

    public class UploadCommand : ICommand {
        public string Usage => $"{Commands.RASP} {Commands.UPLOAD} <containerName> <connectionString> <filePath>";

        public void Execute( string[] args ) {

            if ( args.Length < 4) {
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
            } catch (Exception ex) {
                Console.WriteLine($"Upload Failed: {ex.Message}");
            }
        }
    }

    public class DownloadCommand : ICommand {
        public string Usage => $"{Commands.RASP} {Commands.DOWNLOAD} <blobName> <containerName> <connectionString> <directory>";

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
                    Console.Write("File already exists. Overwrite? (y/n): ");

                    if ( Console.ReadLine() != "y" || Console.ReadLine() != "Y" ) {
                        string fileNameWithoutExt = Path.GetFileNameWithoutExtension(blobName);
                        string extension = Path.GetExtension(blobName);
                        string newFileName = $"{fileNameWithoutExt}_copy{extension}";

                        filePath = Path.Combine(directory, newFileName);
                    }
                }

                blobClient.DownloadTo(filePath);
                Console.WriteLine($"{blobName} downloaded successfully as {Path.GetFileName(filePath)} to {directory}");
            } catch ( Exception ex ) {
                Console.WriteLine($"Download Failed: {ex.Message}");
            }
        }
    }


}
