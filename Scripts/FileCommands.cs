namespace Rasp {
    public class MoveCommand : ICommand {

        private readonly int sourceIndex = 1;
        private readonly int destinationIndex = 2;
        public string Usage => $"{Commands.rasp} {Commands.move} <source> <destination>";

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
        public string Usage => $"{Commands.rasp} {Commands.copy} <source> <destination>";

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
        public string Usage => $"'{Commands.rasp} {Commands.delete} <file>' or '{Commands.rasp} {Commands._delete} <file>'";

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
}
