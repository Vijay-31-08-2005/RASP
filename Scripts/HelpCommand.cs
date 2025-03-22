namespace Rasp {
    public class HelpCommand : ICommand {

        public string Usage => $"{Commands.help}";

        public void Execute( string[] args ) {
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

        }

        public void Info() { }
    }


      
}
