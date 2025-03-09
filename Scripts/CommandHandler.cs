namespace Rasp {
    public static class CommandHandler {

        private static readonly int commandIndex = 0;

        private static readonly Dictionary<string, ICommand> commands = new(StringComparer.OrdinalIgnoreCase) {
            { Commands.add, new AddCommand() },
            { Commands.clone,  new CloneCommand() },
            { Commands.commit, new CommitCommand() },
            { Commands.copy,  new CopyCommand() },
            { Commands._delete, new DeleteCommand() },
            { Commands.delete,  new DeleteCommand() },
            { Commands._displayMessage, new DisplayCommand() },
            { Commands.display, new DisplayCommand() },
            { Commands.drop, new DropCommand() },            
            { Commands._help,  new HelpCommand() },
            { Commands.help,  new HelpCommand() },
            { Commands.info, new InfoCommand() },
            { Commands.init, new InitCommand() },
            { Commands.logs, new LogsCommand() },
            { Commands.move,  new MoveCommand() },
            { Commands.pull, new PullCommand() },
            { Commands.push, new PushCommand() },
            { Commands._profile, new ProfileCommand() },
            { Commands.revert, new RevertCommand() },
            { Commands._rollback, new RollbackCommand() },
            { Commands.rollback, new RollbackCommand() },
            { Commands.status, new StatusCommand() },
            { Commands._version,  new VersionCommand() },
            { Commands.version,  new VersionCommand() },
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
                    RaspUtils.DisplayMessage($"Error: {ex.Message}", Color.Red);
                }
            } else {
                RaspUtils.DisplayMessage($"Error: Unknown command '{command}'. See '{Commands.rasp} {Commands.help}'.", Color.Red);
            }
        }
    }
}