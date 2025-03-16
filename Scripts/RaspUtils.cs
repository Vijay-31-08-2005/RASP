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
            return File.Exists(path)
                ? JsonConvert.DeserializeObject<Dictionary<string, T>>(File.ReadAllText(path)) ?? new Dictionary<string, T>()
                : new Dictionary<string, T>();
        }

        /// <summary>
        /// Serializes a dictionary and saves it to a JSON file.
        /// </summary>
        /// <typeparam name="T">The type of the values in the dictionary.</typeparam>
        /// <param name="path">The path to the JSON file.</param>
        /// <param name="data">The dictionary to serialize and save.</param>
        public static void SaveJson<T>( string path, Dictionary<string, T> data ) {
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
        public static void DisplayMessage( string message, Color color ) {
            Console.ForegroundColor = color switch {
                Color.Red => ConsoleColor.Red,
                Color.Yellow => ConsoleColor.Yellow,
                Color.Green => ConsoleColor.Green,
                _ => ConsoleColor.White
            };
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
                string name = Path.GetFileName(path);
                string message = customMessage ?? $"Error: '{name}' is missing.";
                DisplayMessage(message, Color.Red);
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
            int requiredArgs = split.Count(s => !s.StartsWith("["));
            int optionalArgs = split.Count(s => s.StartsWith("["));

            if ( args.Length < requiredArgs || args.Length > requiredArgs + optionalArgs ) {
                Console.WriteLine($"Usage: {Commands.rasp} {usage}");
                Console.WriteLine($"Expected {requiredArgs} to {requiredArgs + optionalArgs} arguments, but got {args.Length}.");
                return true;
            }

            return false;
        }
    }

    /// <summary>
    /// Represents colors for console messages.
    /// </summary>
    public enum Color {
        Red,
        Yellow,
        Green,
    }
}
