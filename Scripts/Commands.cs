
namespace Rasp {
    public static class Commands {

        public const string rasp = "rasp";
        public const string display = "display";
        public const string _displayMessage = "-dm";
        public const string set = "set";

        // Flags
        public const string help = "--help";
        public const string _help = "-h";
        public const string version = "--version";
        public const string _version = "-v";
        public const string info = "info";
        public const string logs = "logs";
        public const string status = "status";
        public const string rollback = "rollback";
        public const string _rollback = "-rb";

        public const string move = "move";
        public const string delete = "delete";
        public const string _delete = "-del";
        public const string copy = "copy";

        public const string clone = "clone";
        public const string push = "push";
        public const string pull = "pull";

        public const string init = "init";
        public const string add = "add";
        public const string commit = "commit";
        public const string _message = "-m"; 
        public const string revert = "revert";
        public const string _profile = "-p";
        public const string drop = "drop";
        public const string _branch = "-b";
        public const string esc = "/esc";
    }

    public interface ICommand {
        public string Usage {get;}
        public abstract void Execute( string[] args);
    }

    public class HelpCommand : ICommand {

        public string Usage => $"'{Commands.rasp} {Commands.help}' or '{Commands.rasp} {Commands.help}'";

        public void Execute( string[] args ) {
            if ( args.Length > 1 ) {
                Console.WriteLine("Usage: " + Usage);
                return;
            }

            Console.WriteLine("Usage: rasp <command> [options]");
            Console.WriteLine("\nAvailable Commands:");

            // Core Commands
            Console.WriteLine("  init               Initialize a new Rasp repository");
            Console.WriteLine("  add                Add a file to the staging area");
            Console.WriteLine("  commit             Commit staged changes with a message");
            Console.WriteLine("  revert             Revert a specific file in the staging area");
            Console.WriteLine("  drop               Delete the current Rasp repository");
            Console.WriteLine("  display, -dm       Show custom message");

            // File Operations
            Console.WriteLine("\nFile Operations:");
            Console.WriteLine("  move               Move a file");
            Console.WriteLine("  copy               Copy a file");
            Console.WriteLine("  delete             Delete a file");

            // Remote Commands
            Console.WriteLine("\nRemote Commands:");
            Console.WriteLine("  clone              Clone a repository");
            Console.WriteLine("  push               Upload files to the Azure server");
            Console.WriteLine("  pull               Download files from the Azure server");

            // Information Commands
            Console.WriteLine("\nInformation Commands:");
            Console.WriteLine("  logs               Show commit history");
            Console.WriteLine("  status             Show the current status of the repository");
            

            // Flags and Options
            Console.WriteLine("\nOptions and Flags:");
            Console.WriteLine("  -h, --help         Show help");
            Console.WriteLine("  -v, --version      Show the current version of Rasp");
            Console.WriteLine("  -m                 Specify a commit message");
            Console.WriteLine("  -p                 Set the user profile details");
            Console.WriteLine("  -rb, rollback      Rollback the latest commit");
            Console.WriteLine("  info               Show Rasp information");

        }
    }

    public class InfoCommand : ICommand {

        private readonly string readmeText = @$"
Remote Access Storage Pool (RASP)
==================================

RASP is a command-line tool for managing files and repositories.

Examples:
  rasp display 'Hello World'                           - Prints 'Hello World' to the console.
  rasp move file.txt folder/                           - Moves 'file.txt' to 'folder/'.
  rasp copy file.txt backup/                           - Copies 'file.txt' to 'backup/'.
  rasp delete old.txt                                  - Deletes 'old.txt'.
  rasp clone user repo folder                          - Clones 'repo' from GitHub into 'folder'.
  rasp push container conn%asdfawe file.txt          - Uploads 'file' to Azure Blob Storage.
  rasp pull blob container conn%asdfawe folder     - Downloads 'blob' from Azure Blob Storage.

For more information, use '{Commands.help}' or '{Commands._help}'.
";
        public string Usage => $"{Commands.rasp} {Commands.info}";

        public void Execute( string[] args ) {
            if ( args.Length > 1 ) {
                Console.WriteLine("Usage: " + Usage);
                return;
            }
            Console.WriteLine(readmeText);
        }
    }

    public class VersionCommand : ICommand {

        private readonly string versionText = $"{Commands.rasp} 1.0.1";
        public string Usage => $"'{Commands.rasp} {Commands.version}' or '{Commands.rasp} {Commands._version}'";

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
        public string Usage => $"'{Commands.rasp} {Commands.display} <message>' or '{Commands.rasp} {Commands._displayMessage} <message>'";

        public void Execute(string[] args ) {
            if ( args.Length < 2 ) {
                Console.WriteLine("Usage: " + Usage);
                return;
            }
            Console.WriteLine(args[messageIndex]);
        }
    }


      
}
