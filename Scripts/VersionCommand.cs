namespace Rasp {
    public class VersionCommand : ICommand {

        private readonly string versionText = $" {Commands.rasp} 1.1.0";

        public string Usage => $"{Commands.version}";

        public void Execute( string[] args ) => Console.WriteLine(versionText);

        public void Info() {
            Console.WriteLine("  Displays the current version of Rasp.");
            Console.WriteLine();
            Console.WriteLine($"Usage: {Commands.rasp} {Commands.version}");
            Console.WriteLine($"       {Commands.rasp} {Commands._version}");
            Console.WriteLine();
            Console.WriteLine("Options:");
            Console.WriteLine("  -h, --help        Show this help message.");
            Console.WriteLine();
        }
    }   
}
