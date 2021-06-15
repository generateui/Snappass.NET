using System;
using Fernet;

namespace Snappass
{
    public class Encryption
    {
        private static readonly char tokenSeparator = '~';

        public static (string encryptedPassword, string encryptionKey) Encrypt(string password)
        {
            byte[] EncryptionKeyBytes = SimpleFernet.GenerateKey().UrlSafe64Decode();
            var passwordBytes = System.Text.Encoding.Unicode.GetBytes(password);
            string encryptedPassword = SimpleFernet.Encrypt(EncryptionKeyBytes, passwordBytes);
            string encryptionKey = EncryptionKeyBytes.UrlSafe64Encode();
            return (encryptedPassword, encryptionKey);
        }

        public static string Decrypt(string encryptedPassword, string encryptionKey)
        {
            var encryptionKeyBytes = encryptionKey.UrlSafe64Decode();
            var decryptedBytes = SimpleFernet.Decrypt(encryptionKeyBytes, encryptedPassword, out DateTime _);
            var decrypted = decryptedBytes.UrlSafe64Encode().FromBase64String().Replace("\0", "");
            return decrypted;
        }

        public static (string storageKey, string decryptionKey) ParseToken(string token)
        { 
            var tokenFragments = token.Split(tokenSeparator, 2);
            var storageKey = tokenFragments[0];
            var decryptionKey = string.Empty;

            if (tokenFragments.Length > 1)
                decryptionKey = tokenFragments[1];

            return (storageKey, decryptionKey);
        }
        public static string CreateToken(string storageKey, string encryptionKey)
        {
            var token = string.Join(tokenSeparator, storageKey, encryptionKey);

            return token;

        }
    }
}
