using Newtonsoft.Json;
using System.Security.Cryptography;

namespace Rasp {
    public static class RaspUtils {
        public static Dictionary<string, T> LoadJson<T>( string path ) {
            return File.Exists(path)
                ? JsonConvert.DeserializeObject<Dictionary<string, T>>(File.ReadAllText(path)) ?? new Dictionary<string, T>()
                : [];
        }

        public static void SaveJson<T>( string path, Dictionary<string, T> data ) {
            File.WriteAllText(path, JsonConvert.SerializeObject(data, Formatting.Indented));
        }

        public static void SafeFileCopy( string source, string destination ) {
            string? destDir = Path.GetDirectoryName(destination);
            if ( !Directory.Exists(destDir) ) {
                Directory.CreateDirectory(destDir!);
            }
            File.Copy(source, destination, true);
        }

        public static string ComputeHashCode( Stream data ) {
            using SHA1 sha1 = SHA1.Create();
            byte[] hashBytes = sha1.ComputeHash(data);
            return Convert.ToHexString(hashBytes).ToLower();
        }

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
    }

    public enum Color {
        Red,
        Yellow,
        Green,
    }
}