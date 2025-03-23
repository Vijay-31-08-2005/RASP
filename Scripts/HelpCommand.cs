namespace Rasp {
    public class HelpCommand : ICommand {

        public string Usage => $"{Commands.help}";

        public void Execute( string[] args ) {
            Console.WriteLine("Usage: rasp <command> [options]");

            Console.WriteLine("\nAvailable Commands:");
            Console.WriteLine("  init               Initialize a new Rasp repository");
            Console.WriteLine("  add                Add a file to the staging area");
            Console.WriteLine("  commit             Commit staged changes with a message");
            Console.WriteLine("  revert             Revert a specific file in the staging area");
            Console.WriteLine("  branch             Create a new branch");
            Console.WriteLine("  checkout           Switch to a specified branch");
            Console.WriteLine("  merge              Merge a specified branch into the current branch");
            Console.WriteLine("  drop               Delete the current Rasp repository");

            Console.WriteLine("\nFile Operations:");
            Console.WriteLine("  mv                 Move a file");
            Console.WriteLine("  cp                 Copy a file");
            Console.WriteLine("  delete, -d         Delete a file, description, or branch");

            Console.WriteLine("\nRemote Commands:");
            Console.WriteLine("  clone              Clone a repository");
            Console.WriteLine("  push               Upload files to the Azure server");
            Console.WriteLine("  pull               Download files from the Azure server");
            Console.WriteLine("  set                Configure the Azure connection string for remote storage");

            Console.WriteLine("\nInformation Commands:");
            Console.WriteLine("  history            Show current branch's commit history");
            Console.WriteLine("  status             Show the current repository status");
            Console.WriteLine("  logs               Show the history of repository actions");

            Console.WriteLine("\nOptions and Flags:");
            Console.WriteLine("  -h, --help         Show help for a command");
            Console.WriteLine("  -v, --version      Display the current version of Rasp");
            Console.WriteLine("  -m                 Specify a commit message");
            Console.WriteLine("  -p                 Set user profile details");
            Console.WriteLine("  -i                 Activate shell mode");
            Console.WriteLine("  -o                 Deactivate shell mode");
            Console.WriteLine("  -l                 List existing branches");
            Console.WriteLine("  -rb, rollback      Roll back the latest commit");

            Console.WriteLine("\nFor more details on a specific command, use: 'rasp <command> --help'");
        }

        public void Info() { }
    }


      
}
