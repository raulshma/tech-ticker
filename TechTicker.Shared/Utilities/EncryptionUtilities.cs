using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;

namespace TechTicker.Shared.Utilities
{
    public static class EncryptionUtilities
    {
        public static string EncryptString(string plainText, string key)
        {
            if (string.IsNullOrEmpty(plainText)) return plainText;
            if (string.IsNullOrEmpty(key)) throw new ArgumentNullException(nameof(key));

            using var aes = Aes.Create();
            var keyBytes = new byte[32];
            Encoding.UTF8.GetBytes(key.PadRight(keyBytes.Length)).CopyTo(keyBytes, 0);
            aes.Key = keyBytes;
            aes.GenerateIV();
            var iv = aes.IV;

            using var encryptor = aes.CreateEncryptor(aes.Key, iv);
            using var ms = new MemoryStream();
            ms.Write(iv, 0, iv.Length);
            using (var cs = new CryptoStream(ms, encryptor, CryptoStreamMode.Write))
            using (var sw = new StreamWriter(cs))
            {
                sw.Write(plainText);
            }
            return Convert.ToBase64String(ms.ToArray());
        }

        public static string DecryptString(string cipherText, string key)
        {
            if (string.IsNullOrEmpty(cipherText)) return cipherText;
            if (string.IsNullOrEmpty(key)) throw new ArgumentNullException(nameof(key));

            var fullCipher = Convert.FromBase64String(cipherText);
            using var aes = Aes.Create();
            var keyBytes = new byte[32];
            Encoding.UTF8.GetBytes(key.PadRight(keyBytes.Length)).CopyTo(keyBytes, 0);
            aes.Key = keyBytes;
            var iv = new byte[16];
            Array.Copy(fullCipher, 0, iv, 0, iv.Length);
            aes.IV = iv;

            using var decryptor = aes.CreateDecryptor(aes.Key, aes.IV);
            using var ms = new MemoryStream(fullCipher, 16, fullCipher.Length - 16);
            using var cs = new CryptoStream(ms, decryptor, CryptoStreamMode.Read);
            using var sr = new StreamReader(cs);
            return sr.ReadToEnd();
        }
    }
} 