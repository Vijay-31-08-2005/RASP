using LibGit2Sharp;
using Newtonsoft.Json.Linq;
using System.Text.RegularExpressions;

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
            { Commands._branch, new BranchCommand() },
        };

        private static List<string> repoCommands = new() {
            Commands.drop,
            Commands.add,
            Commands.commit,
            Commands._profile, 
            Commands.revert, 
            Commands._rollback, 
            Commands.rollback, 
            Commands.status,  
            Commands._branch,
            Commands.logs,
        };

        private static void Main( string[] args ) {

            if( args.Length == 0 ) {
                ShowHelp(args);
            }

            if ( repoCommands.Contains(args[commandIndex]) ) {
                RaspUtils.DisplayMessage("Error: Initialize the Rasp repository", Color.Red);
                return;
            }

            if ( args[commandIndex] != Commands.init ) {
                ExecuteCommand(args);
                return;
            }

            do {
                if ( Directory.Exists(Path.Combine(Directory.GetCurrentDirectory(), ".rasp")) ) {
                    string configFile = Path.Combine(Directory.GetCurrentDirectory(), ".rasp/config.json");
                    Dictionary<string, string> config = RaspUtils.LoadJson<string>(configFile);
                    string? branch = config["branch"];
                    Console.Write($"{Directory.GetCurrentDirectory()}:");
                    if ( !string.IsNullOrEmpty(branch) ) {
                        Console.ForegroundColor = ConsoleColor.Cyan;
                        Console.Write($" ({branch}) > ");
                        Console.ResetColor();
                    } else {
                        Console.Write(" > ");
                    }
                } else {
                    if ( args[commandIndex] == Commands.init ) {
                        try {
                            commands[Commands.init].Execute(args);
                        } catch ( Exception ex ) {
                            RaspUtils.DisplayMessage($"Error: {ex.Message}", Color.Red);
                        }
                        continue;
                    }
                }

                string? input = Console.ReadLine()?.Trim();
                if ( string.IsNullOrEmpty(input) ) {
                    if ( !Directory.Exists(Path.Combine(Directory.GetCurrentDirectory(), ".rasp")) ) {
                        break;
                    }
                    continue; 
                }
                if ( input == Commands.esc ) break;
                args = input.Split(' ');
                args = [.. Regex.Matches(input, @"[\""].+?[\""]|[^ ]+")
                                    .Select(m => m.Value.Trim('"'))
                                    ];
                if ( args[commandIndex] == Commands.rasp ) {
                    string[] newArgs = [.. args.Where(arg => arg != "rasp")];

                    if ( newArgs.Length == 0 ) {
                        ShowHelp(newArgs);
                        continue;
                    }
                    ExecuteCommand(newArgs);
                } else {
                    RaspUtils.DisplayMessage($"Error: Unknown command '{args[commandIndex]}'. See '{Commands.rasp} {Commands.help}'.", Color.Red);
                }

            } while ( true );
        }

        private static void ExecuteCommand( string[] args) {
            string command = args[commandIndex];
            if ( commands.TryGetValue(command, out ICommand? value) ) {
                try {
                    value.Execute(args);
                } catch ( Exception ex ) {
                    RaspUtils.DisplayMessage($"Error: {ex.Message}", Color.Red);
                }
            } else {
                RaspUtils.DisplayMessage($"Error: Unknown command '{command}'. See '{Commands.rasp} {Commands.help}'.", Color.Red);
            }
        }

        private static void ShowHelp( string[] args ) {
            new HelpCommand().Execute(args);
            return;
        }
    }
}