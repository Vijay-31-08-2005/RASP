namespace Rasp {
    public class DropCommand : ICommand {
        public string Usage => $"{Commands.drop}";

        public void Execute( string[] args ) {

            string raspDirectory = Paths.RaspDir;
            string configFile = Paths.ConfigFile;
            string azConfigFile = Paths.AzConfigFile;

            if ( RaspUtils.IsMissing(configFile) )
                return;

            Dictionary<string, string> config = RaspUtils.LoadJson<string>(configFile);

            if ( !Directory.Exists(Paths.MainDir) ) {
                RaspUtils.DisplayMessage($"Error: The detected '{Dir.dotRasp}' folder does not contain a valid Rasp repository.", ConsoleColor.Red);
                return;
            }

            if ( Directory.Exists(raspDirectory) ) {
                Console.ForegroundColor = System.ConsoleColor.Yellow;
                Console.Write("Do you want to delete the Rasp repository? (y/n): ");
                string? input = Console.ReadLine()?.Trim().ToLower();
                Console.ResetColor();

                if ( input != Value.y )
                    return;

                config[Key.branch] = Value.main;
                RaspUtils.SaveJson<string>(configFile, config);
                Directory.Delete(raspDirectory, true);
                File.Delete(azConfigFile);
                RaspUtils.DisplayMessage("Rasp repository deleted successfully.", ConsoleColor.Green);
            } else {
                RaspUtils.DisplayMessage("Error: No Rasp repository was innitialized in current directory.", ConsoleColor.Red);
            }
        }

        public void Info() {
            Console.WriteLine("  Deletes the entire Rasp repository from the current directory.");
            Console.WriteLine("  This action cannot be undone, so a confirmation prompt is provided before deletion.");
            Console.WriteLine();
            Console.WriteLine($"Usage: {Commands.rasp} {Commands.drop}");
            Console.WriteLine();
            Console.WriteLine("Options:");
            Console.WriteLine("  -h, --help        Show this help message.");
            Console.WriteLine();
        }
    }
}
