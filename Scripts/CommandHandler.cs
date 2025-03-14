using Newtonsoft.Json.Linq;
using System.Diagnostics;
using System.Text;
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
            { Commands.branch, new BranchCommand() },
            { Commands.set, new SetCommand() },
            { Commands.checkout, new CheckoutCommand() }
        };

        private readonly static List<string> repoCommands = [
            Commands.drop,
            Commands.add,
            Commands.commit,
            Commands._profile, 
            Commands.revert, 
            Commands._rollback, 
            Commands.rollback, 
            Commands.status,  
            Commands.branch,
            Commands.logs,
        ];

        private static void Main( string[] args ) {

            if ( args.Length == 0 ) {
                ShowHelp(args);
            }

            if ( repoCommands.Contains(args[commandIndex]) && args[commandIndex] != Commands.init ) {
                RaspUtils.DisplayMessage("Error: Initialize the Rasp repository first using 'rasp init'.", Color.Red);
                return;
            }

            if ( args[commandIndex] != Commands.init ) {
                ExecuteCommand(args);
                return;
            }

            do {
                if ( Directory.Exists(Path.Combine(Directory.GetCurrentDirectory(), ".rasp")) ) {
                    string configFile = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "rasp/config.json");
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
                args = [.. Regex.Matches(input, @"[\""].+?[\""]|[^ ]+").Select(m => m.Value.Trim('"'))];

                if ( args[commandIndex] == Commands.rasp ) {
                    List<string> temp = [.. args];
                    temp.RemoveAt(commandIndex);
                    string[] newArgs = [.. temp];

                    if ( newArgs.Length == 0 ) {
                        ShowHelp(newArgs);
                        continue;
                    }
                    ExecuteCommand(newArgs);
                } else {
                    ExecuteExternalCommand(args);
                }
            } while (true);
        }

        private static void ExecuteExternalCommand( string[] args ) {

            StringBuilder sb = new();
            foreach ( string str in args ) {
                sb.Append(str);
                sb.Append(' ');
            }

            string command = sb.ToString();

            ProcessStartInfo psi = new() {
                FileName = Environment.OSVersion.Platform == PlatformID.Win32NT ? "cmd.exe" : "bash",
                Arguments = Environment.OSVersion.Platform == PlatformID.Win32NT ? $"/c {command}" : $"-c \"{command}\"",
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            Process process = new() { StartInfo = psi };
            process.Start();

            string output = process.StandardOutput.ReadToEnd();
            string error = process.StandardError.ReadToEnd();

            process.WaitForExit();

            if ( !string.IsNullOrEmpty(output) )
                Console.WriteLine(output);
            if ( !string.IsNullOrEmpty(error) )
                RaspUtils.DisplayMessage($"Error: {error}", Color.Red);
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