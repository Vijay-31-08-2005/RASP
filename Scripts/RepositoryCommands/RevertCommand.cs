namespace Rasp {
    public class RevertCommand : ICommand {
        public string Usage => $"{Commands.revert} <file>";

        public void Execute( string[] args ) {

            string file = args[1];

            string configFile = Paths.ConfigFile;
            if ( RaspUtils.IsMissing(configFile) )
                return;
            Dictionary<string, string> config = RaspUtils.LoadJson<string>(configFile);

            if ( config!.Equals(null) ) {
                RaspUtils.DisplayMessage("Error: Config file is missing or corrupted.", ConsoleColor.Red);
                return;
            }

            string indexFile = Path.Combine(Paths.BranchesDir, $"{config[Key.branch]}/{Files.index}");
            Dictionary<string, Dictionary<string, string>>? index = RaspUtils.LoadJson<Dictionary<string, string>>(indexFile);


            Console.ForegroundColor = System.ConsoleColor.Yellow;
            Console.WriteLine($"Are you sure you want to revert '{file}'? (y/n)");
            string? input = Console.ReadLine()?.Trim().ToLower();
            Console.ResetColor();

            if ( input != Value.y )
                return;

            if ( index!.Equals(null) || !index.TryGetValue(file, out Dictionary<string, string>? value) ) {
                RaspUtils.DisplayMessage($"Error: File '{file}' not found in the index or Index file is missing.", ConsoleColor.Red);
                return;
            }

            if ( value[Key.status].Equals(Status.tracked) ) {
                if ( index.Remove(file) ) {
                    RaspUtils.DisplayMessage($"File '{file}' reverted.", ConsoleColor.Yellow);
                }
            } else if ( index[file][Key.status].Equals(Status.committed) ) {
                RaspUtils.DisplayMessage($"Warning: File '{file}' is already committed, Use '{Commands.rasp} {Commands.rollback}' instead.", ConsoleColor.Yellow);
            }
            RaspUtils.SaveJson(indexFile, index!);
        }

        public void Info() {
            Console.WriteLine("  Reverts a tracked file to its previous state or removes it from tracking.");
            Console.WriteLine("  If the file is already committed, a rollback is required instead.");
            Console.WriteLine();
            Console.WriteLine($"Usage: {Commands.rasp} {Commands.revert} [options]");
            Console.WriteLine();
            Console.WriteLine("Options:");
            Console.WriteLine("  <file>            File to be reverted.");
            Console.WriteLine("  -h, --help        Show this help message.");
            Console.WriteLine();
        }
    }
}
