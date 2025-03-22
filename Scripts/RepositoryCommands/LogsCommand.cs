namespace Rasp {
    public class LogsCommand : ICommand {
        public string Usage => $"{Commands.logs}";

        public void Execute( string[] args ) {

            string logsFile = Paths.LogsFile;

            if ( RaspUtils.IsMissing(logsFile) )
                return;

            Dictionary<string, string> logs = RaspUtils.LoadJson<string>(logsFile);

            foreach ( var log in logs ) {
                Console.WriteLine(log.Key + " : " + log.Value);
                Console.WriteLine();
            }
        }

        public void Info() {
            Console.WriteLine("  Displays the history of repository actions, including commits and branch changes.");
            Console.WriteLine();
            Console.WriteLine($"Usage: {Commands.rasp} {Commands.logs}");
            Console.WriteLine();
            Console.WriteLine("Options:");
            Console.WriteLine("  -h, --help        Show this help message.");
            Console.WriteLine();
        }
    }
}
