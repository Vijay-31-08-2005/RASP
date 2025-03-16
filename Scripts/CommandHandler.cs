using System.Diagnostics;
using System.Text;
using System.Text.RegularExpressions;

namespace Rasp {
    public static class CommandHandler {

        private static readonly int commandIndex = 0;

        private static readonly Dictionary<string, ICommand> commands = new(StringComparer.OrdinalIgnoreCase) {
            { Commands.add, new AddCommand() },
            { Commands.clone, new CloneCommand() },
            { Commands.commit, new CommitCommand() },
            { Commands.copy, new CopyCommand() },
            { Commands._delete, new DeleteCommand() },
            { Commands.delete, new DeleteCommand() },
            { Commands._displayMessage, new DisplayCommand() },
            { Commands.display, new DisplayCommand() },
            { Commands.drop, new DropCommand() },
            { Commands._help, new HelpCommand() },
            { Commands.help, new HelpCommand() },
            { Commands.info, new InfoCommand() },
            { Commands.init, new InitCommand() },
            { Commands.history, new HistoryCommmand() },
            { Commands.move, new MoveCommand() },
            { Commands.pull, new PullCommand() },
            { Commands.push, new PushCommand() },
            { Commands._profile, new ProfileCommand() },
            { Commands.revert, new RevertCommand() },
            { Commands._rollback, new RollbackCommand() },
            { Commands.rollback, new RollbackCommand() },
            { Commands.status, new StatusCommand() },
            { Commands._version, new VersionCommand() },
            { Commands.version, new VersionCommand() },
            { Commands.branch, new BranchCommand() },
            { Commands.set, new SetCommand() },
            { Commands.checkout, new CheckoutCommand() },
            { Commands.merge, new MergeCommand() },
        };

        private static readonly HashSet<string> repoCommands = [
            Commands.init,
            Commands.drop,
            Commands.add,
            Commands.commit,
            Commands.revert,
            Commands._rollback,
            Commands.rollback,
            Commands.status,
            Commands.branch,
            Commands.history,
            Commands._in,
            Commands._out,
            Commands.push,
            Commands.pull,
            Commands.set,
        ];

        private static void Main( string[] args ) {
            if ( args.Length == 0 ) {
                ShowHelp(args);
                return;
            }

            string raspDir = Path.Combine(Directory.GetCurrentDirectory(), ".rasp");

            if ( repoCommands.Contains(args[commandIndex] )&& args[commandIndex] != Commands.init ) {
                if ( !Directory.Exists(raspDir)) {
                    RaspUtils.DisplayMessage("Error: Initialize the Rasp repository first using 'rasp init'.", Color.Red);
                    return;
                } else if ( args[commandIndex] != Commands._in ) {
                    RaspUtils.DisplayMessage("Warning: Shell mode isn't activated, Use 'rasp -i'.", Color.Yellow);
                    return;
                }
            }

            if ( !repoCommands.Contains(args[commandIndex]) || args[commandIndex] == Commands.init ) {
                ExecuteCommand(args);
                return;
            }

            if ( args[commandIndex] == Commands._in ) {
                EnterShellMode();
            }
        }

        private static void EnterShellMode() {
            RaspUtils.DisplayMessage("Shell mode activated", Color.Green);
            do {
                string raspDir = Path.Combine(Directory.GetCurrentDirectory(), ".rasp");

                if ( Directory.Exists(raspDir) ) {
                    string configFile = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "rasp/config.json");

                    if ( !File.Exists(configFile) ) {
                        RaspUtils.DisplayMessage("Error: Missing config file.", Color.Red);
                        break;
                    }

                    Dictionary<string, string>? config = RaspUtils.LoadJson<string>(configFile);
                    string branch = config?.GetValueOrDefault("branch", "unknown") ?? "unknown";

                    Console.Write($"{Directory.GetCurrentDirectory()}: ");
                    Console.ForegroundColor = ConsoleColor.Cyan;
                    Console.Write($"({branch}) > ");
                    Console.ResetColor();
                } else {
                    Console.WriteLine("     --enter--");  
                }

                string? input = Console.ReadLine()?.Trim();
                if ( !Directory.Exists(raspDir) ) break;
                if ( string.IsNullOrEmpty(input) ) continue;

                string[] args = [.. Regex.Matches(input, @"[\""].+?[\""]|[^ ]+").Select(m => m.Value.Trim('"')) ];

                if ( args.Length == 0 ) continue;

                if ( args[commandIndex] == Commands.rasp ) {
                    List<string> tempArgs = [.. args];
                    tempArgs.RemoveAt(commandIndex);
                    string[] newArgs = [.. tempArgs];

                    if ( newArgs.Length == 0 ) {
                        ShowHelp(newArgs);
                        continue;
                    }

                    if ( newArgs[commandIndex] == Commands._in ) {
                        RaspUtils.DisplayMessage("Warning: Already in shell mode", Color.Yellow);
                        continue;
                    }

                    if ( newArgs[commandIndex] == Commands._out ) {
                        RaspUtils.DisplayMessage("Shell mode diactivated", Color.Green);
                        break;
                    }

                    ExecuteCommand(newArgs);
                } else {
                    ExecuteExternalCommand(args);
                }
            } while ( true );
        }

        private static void ExecuteExternalCommand( string[] args ) {
            string command = string.Join(" ", args);

            ProcessStartInfo psi = new() {
                FileName = Environment.OSVersion.Platform == PlatformID.Win32NT ? "cmd.exe" : "bash",
                Arguments = Environment.OSVersion.Platform == PlatformID.Win32NT ? $"/c {command}" : $"-c \"{command}\"",
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            try {
                using Process process = new() { StartInfo = psi };
                process.Start();

                string output = process.StandardOutput.ReadToEnd();
                string error = process.StandardError.ReadToEnd();

                process.WaitForExit();

                if ( !string.IsNullOrEmpty(output) )
                    Console.WriteLine(output);
                if ( !string.IsNullOrEmpty(error) )
                    RaspUtils.DisplayMessage($"Error: {error}", Color.Red);
            } catch ( Exception ex ) {
                RaspUtils.DisplayMessage($"Execution error: {ex.Message}", Color.Red);
            }
        }

        private static void ExecuteCommand( string[] args ) {
            string command = args[commandIndex];
            if ( commands.TryGetValue(command, out ICommand? commandInstance) ) {
                try {
                    commandInstance.Execute(args);
                } catch ( Exception ex ) {
                    RaspUtils.DisplayMessage($"Error: {ex.Message}", Color.Red);
                }
            } else {
                RaspUtils.DisplayMessage($"Error: Unknown command '{command}'. See '{Commands.rasp} {Commands.help}'.", Color.Red);
            }
        }

        private static void ShowHelp( string[] args ) {
            new HelpCommand().Execute(args);
        }
    }
}
