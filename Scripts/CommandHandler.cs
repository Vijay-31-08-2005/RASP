namespace RASP {
    public static class CommandHandler {

        private static readonly int commandIndex = 0;

        private static readonly Dictionary<string, ICommand> commands = new(StringComparer.OrdinalIgnoreCase) {
            { Commands.DISPLAY, new DisplayCommand() },
            { Commands.MOVE,  new MoveCommand() },
            { Commands.COPY,  new CopyCommand() },
            { Commands.DELETE,  new DeleteCommand() },
            { Commands.HELP,  new HelpCommand() },
            { Commands.VERSION,  new VersionCommand() },
            { Commands.README, new ReadmeCommand() },
            { Commands.CLONE,  new CloneCommand() },
            { Commands.UPLOAD, new UploadCommand() },
            { Commands.DOWNLOAD, new DownloadCommand() }
        };


        private static void Main( string[] args ) {
            if ( args.Length == 0 ) {
                new HelpCommand().Execute(args);
                return;
            }

            string command = args[commandIndex];

            if ( commands.TryGetValue(command, out ICommand ? value) ) {
                try {
                    value.Execute(args);
                } catch ( Exception ex ) {
                    Console.WriteLine($"Error: {ex.Message}");
                }
            } else {
                Console.WriteLine($"Error: Unknown command '{command}'. See '{Commands.RASP} {Commands.HELP}'.");
            }
        }
    }
}