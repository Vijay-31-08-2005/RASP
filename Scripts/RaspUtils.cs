using Newtonsoft.Json;
using System.Security.Cryptography;

namespace Rasp {
    /// <summary>
    /// Provides utility methods for various operations such as JSON handling, file operations, and console messaging.
    /// </summary>
    public static class RaspUtils {
        /// <summary>
        /// Loads a JSON file and deserializes it into a dictionary.
        /// </summary>
        /// <typeparam name="T">The type of the values in the dictionary.</typeparam>
        /// <param name="path">The path to the JSON file.</param>
        /// <returns>A dictionary containing the deserialized data.</returns>
        public static Dictionary<string, T> LoadJson<T>( string path ) {
            return File.Exists(path) ? JsonConvert.DeserializeObject<Dictionary<string, T>>(File.ReadAllText(path)) ?? []: [];
        }

        /// <summary>
        /// Serializes a dictionary and saves it to a JSON file.
        /// </summary>
        /// <typeparam name="T">The type of the values in the dictionary.</typeparam>
        /// <param name="path">The path to the JSON file.</param>
        /// <param name="data">The dictionary to serialize and save.</param>
        public static void SaveJson<T>( string path, Dictionary<string, T> data ) {
            if(!Directory.Exists(Path.GetDirectoryName(path)!) ) {
                Directory.CreateDirectory(Path.GetDirectoryName(path)!);
            }
            File.WriteAllText(path, JsonConvert.SerializeObject(data, Formatting.Indented));
        }

        /// <summary>
        /// Safely copies a file to a destination, creating the destination directory if it does not exist.
        /// </summary>
        /// <param name="source">The source file path.</param>
        /// <param name="destination">The destination file path.</param>
        public static void SafeFileCopy( string source, string destination ) {
            string? destDir = Path.GetDirectoryName(destination);
            if ( !Directory.Exists(destDir) ) {
                Directory.CreateDirectory(destDir!);
            }
            File.Copy(source, destination, true);
        }

        /// <summary>
        /// Computes the SHA-1 hash of a stream and returns it as a hexadecimal string.
        /// </summary>
        /// <param name="data">The stream to compute the hash for.</param>
        /// <returns>The SHA-1 hash as a hexadecimal string.</returns>
        public static string ComputeHashCode( Stream data ) {
            using SHA1 sha1 = SHA1.Create();
            byte[] hashBytes = sha1.ComputeHash(data);
            return Convert.ToHexString(hashBytes).ToLower();
        }

        /// <summary>
        /// Displays a message in the console with the specified color.
        /// </summary>
        /// <param name="message">The message to display.</param>
        /// <param name="color">The color to use for the message.</param>
        public static void DisplayMessage( string message, System.ConsoleColor color ) {
            Console.ForegroundColor = color;
            Console.WriteLine(message);
            Console.ResetColor();
        }

        /// <summary>
        /// Checks if a file or directory is missing and optionally displays a custom message.
        /// </summary>
        /// <param name="path">The path to check.</param>
        /// <param name="customMessage">An optional custom message to display if the path is missing.</param>
        /// <returns>True if the path is missing, otherwise false.</returns>
        public static bool IsMissing( string path, string? customMessage = null ) {
            bool isMissing = !File.Exists(path) && !Directory.Exists(path);

            if ( isMissing ) {
                string fullPath = Path.GetFullPath(path);
                string message = customMessage ?? $"Error: '{fullPath}' is missing.";
                DisplayMessage(message, ConsoleColor.Red);
            }

            return isMissing;
        }

        /// <summary>
        /// Validates the command-line arguments against the expected usage pattern.
        /// </summary>
        /// <param name="args">The command-line arguments.</param>
        /// <param name="usage">The expected usage pattern.</param>
        /// <returns>True if the arguments are not valid, otherwise false.</returns>
        public static bool IsNotValid( this string[] args, string usage ) {
            string[] split = usage.Split(' ');
            int requiredArgs = split.Count(s => !s.StartsWith('['));
            int optionalArgs = split.Count(s => s.StartsWith('['));

            if ( args.Length < requiredArgs || args.Length > requiredArgs + optionalArgs ) {
                Console.WriteLine($"Usage: {Commands.rasp} {usage}");
                return true;
            }

            return false;
        }
      
        public static void WriteColor(string message, ConsoleColor color) {
            Console.ForegroundColor = color;
            Console.Write(message);
            Console.ResetColor();
        }

        public static void DisplayError( Exception exception) {
            DisplayMessage($"Error: {exception.Message}", ConsoleColor.Red);
        }
    }

    public static class Paths {
        public static string RaspDir => Path.Combine(Directory.GetCurrentDirectory(), Dir.dotRasp);
        public static string LogsFile => Path.Combine(RaspDir, Files.logs);
        public static string BranchesDir => Path.Combine(RaspDir, Dir.branches);
        public static string BackupDir => Path.Combine(RaspDir, Dir.backup);
        public static string BranchesFile => Path.Combine(BranchesDir, Files.branches);
        public static string MainDir => Path.Combine(BranchesDir, Value.main);
        public static string MainIndexFile => Path.Combine(MainDir, Files.index);
        public static string MainCommitsDir => Path.Combine(MainDir, Dir.commits);
        public static string RaspAppDir => Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), Dir.rasp);
        public static string ConfigFile => Path.Combine(RaspAppDir, Files.config);
        public static string AzConfigFile => Path.Combine(RaspAppDir, Files.azConfig);

    }

    public static class Status {
        public const string tracked = "tracked";
        public const string committed = "committed";
    }


    public static class Files {
        public const string commit = "commit.json";
        public const string index = "index.json";
        public const string config = "config.json";
        public const string azConfig = "azConfig.json";
        public const string branches = "branches.json";
        public const string logs = ".logs.json";
        public const string history = ".history.json";
    }

    public static class Dir {
        public const string backup = "backup";
        public const string dotRasp = ".rasp";
        public const string rasp = "rasp";
        public const string branches = "branches";
        public const string initialCommit = "initialCommit";
        public const string commits = "commits";
    }

    public static class Key {
        public const string author = "author";
        public const string email = "email";
        public const string branch = "branch";
        public const string branches = "branches";
        public const string message = "message";
        public const string timeStamp = "timestamp";
        public const string status = "status";
        public const string hash = "hash";
        public const string lastCommit = "lastCommit";
        public const string id = "id";
        public const string files = "files";
        public const string key = "key";
        public const string connectionString = "connectionString";
    }

    public static class Value {
        public const string unknown = "unknown";
        public const string main = "main";
        public const string guest = "guest";
        public const string y = "y";
    }
}
