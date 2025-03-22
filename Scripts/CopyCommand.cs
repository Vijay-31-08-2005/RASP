namespace Rasp {

    public class CopyCommand : ICommand {

        private readonly int sourceIndex = 1;
        private readonly int destinationIndex = 2;
        public string Usage => $"{Commands._copy} <source> <destination>";

        public void Execute( string[] args ) {

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

        public void Info() {
            Console.WriteLine("  Copies a file from the source path to the destination.");
            Console.WriteLine("  If the destination is a directory, the file is copied inside it.");
            Console.WriteLine();
            Console.WriteLine($"Usage: {Commands.rasp} {Commands._copy} [options]");
            Console.WriteLine();
            Console.WriteLine("Options:");
            Console.WriteLine("  <source> <destination>    Source and destination for file transfer.");
            Console.WriteLine("  -h, --help                Show this help message.");
            Console.WriteLine();
        }
    } 
}
