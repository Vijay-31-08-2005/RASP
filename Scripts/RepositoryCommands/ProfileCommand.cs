namespace Rasp {
    public class ProfileCommand : ICommand {
        public string Usage => $"{Commands._profile} <author> <mail>";

        public void Execute( string[] args ) {

            try {
                string configFile = Paths.ConfigFile;
                string logsFile = Paths.LogsFile;
                if ( RaspUtils.IsMissing(configFile) || RaspUtils.IsMissing(logsFile) )
                    return;
                Dictionary<string, string> config = RaspUtils.LoadJson<string>(configFile)!;
                Dictionary<string, string> logs = RaspUtils.LoadJson<string>(logsFile)!;

                if ( !args[2].Contains('@') || !args[2].Contains('.') ) {
                    RaspUtils.DisplayMessage("Error: Invalid email format.", ConsoleColor.Red);
                    return;
                }
                config[Key.author] = args[1];
                config[Key.email] = args[2];

                logs[DateTime.Now.ToString()] = $"Profile updated: {config[Key.author]} <{config[Key.email]}>";
                RaspUtils.SaveJson(configFile, config);
                RaspUtils.SaveJson(logsFile, logs);
                RaspUtils.DisplayMessage("Profile updated successfully.", ConsoleColor.Green);
            } catch ( Exception ex ) {
                RaspUtils.DisplayMessage($"Error: {ex.Message}", ConsoleColor.Red);
            }
        }

        public void Info() {
            Console.WriteLine("  Sets the author name and email for the repository.");
            Console.WriteLine("  This information is used in commit metadata.");
            Console.WriteLine();
            Console.WriteLine($"Usage: {Commands.rasp} {Commands._profile} [options]");
            Console.WriteLine();
            Console.WriteLine("Options:");
            Console.WriteLine("  <author> <email>    User's name and E-mail.");
            Console.WriteLine("  -h, --help          Show this help message.");
            Console.WriteLine();
        }
    }
}
