using LibGit2Sharp;
using System;
using System.Text;

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
  rasp display 'Hello World'   - Prints 'Hello World' to the console.
  rasp move file.txt folder/   - Moves 'file.txt' to 'folder/'.
  rasp copy file.txt backup/   - Copies 'file.txt' to 'backup/'.
  rasp delete old.txt          - Deletes 'old.txt'.
  rasp clone user repo folder  - Clones 'repo' from GitHub into 'folder'.

For more information, use 'rasp help'.
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

        private readonly int messageIndex = 2;
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
}