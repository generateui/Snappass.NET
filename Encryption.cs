using System;
using Fernet;

namespace Snappass
{
    public class Encryption
    {
        public static (string encryptedPassword, string key) Encrypt(string password)
        {
            byte[] keyBytes = SimpleFernet.GenerateKey().UrlSafe64Decode();
            var passwordBytes = System.Text.Encoding.Unicode.GetBytes(password);
            string encrypted = SimpleFernet.Encrypt(keyBytes, passwordBytes);
            string key = keyBytes.UrlSafe64Encode();
            return (encrypted, key);
        }

        public static string Decrypt(string encrypted, string key)
        {
            var keyBytes = key.UrlSafe64Decode();
            var decryptedBytes = SimpleFernet.Decrypt(keyBytes, encrypted, out DateTime _);
            var decrypted = decryptedBytes.UrlSafe64Encode().FromBase64String().Replace("\0", "");
            return decrypted;
        }
    }
}
