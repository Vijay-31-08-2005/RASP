using System.Security.Cryptography;
using System.Text;

namespace Rasp {
    public static class Encryption {
        private static readonly byte[] IV = Encoding.UTF8.GetBytes("1234567890123456");

        public static string Encrypt( string plainText, string passphrase ) {
            using Aes aes = Aes.Create();
            aes.Key = DeriveKey(passphrase);
            aes.IV = IV;
            aes.Mode = CipherMode.CBC;
            aes.Padding = PaddingMode.PKCS7;

            using ICryptoTransform encryptor = aes.CreateEncryptor();
            byte[] plainBytes = Encoding.UTF8.GetBytes(plainText);
            byte[] encryptedBytes = encryptor.TransformFinalBlock(plainBytes, 0, plainBytes.Length);
            return Convert.ToBase64String(encryptedBytes);
        }

        public static string Decrypt( string encryptedText, string passphrase ) {
            using Aes aes = Aes.Create();
            aes.Key = DeriveKey(passphrase);
            aes.IV = IV;
            aes.Mode = CipherMode.CBC;
            aes.Padding = PaddingMode.PKCS7;

            using ICryptoTransform decryptor = aes.CreateDecryptor();
            byte[] encryptedBytes = Convert.FromBase64String(encryptedText);
            byte[] decryptedBytes = decryptor.TransformFinalBlock(encryptedBytes, 0, encryptedBytes.Length);
            return Encoding.UTF8.GetString(decryptedBytes);
        }

        private static byte[] DeriveKey( string passphrase ) {
            return SHA256.HashData(Encoding.UTF8.GetBytes(passphrase));
        }
    }
}