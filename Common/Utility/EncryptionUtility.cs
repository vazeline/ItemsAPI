using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;

namespace Common.Utility
{
    public static class EncryptionUtility
    {
        /// <param name="key">Valid key lengths are 16, 24 or 32 characters.</param>
        public static string EncryptStringToBase64UsingAes(string plainText, string key)
        {
            var iv = new byte[16];

            using (var aes = Aes.Create())
            {
                aes.Key = Encoding.UTF8.GetBytes(key);
                aes.IV = iv;

                var encryptor = aes.CreateEncryptor(aes.Key, aes.IV);

                using (var memoryStream = new MemoryStream())
                {
                    using (var cryptoStream = new CryptoStream(memoryStream, encryptor, CryptoStreamMode.Write))
                    {
                        using (var streamWriter = new StreamWriter(cryptoStream))
                        {
                            streamWriter.Write(plainText);
                        }

                        return Convert.ToBase64String(memoryStream.ToArray());
                    }
                }
            }
        }

        /// <param name="key">Valid key lengths are 16, 24 or 32 characters.</param>
        public static bool TryDecryptBase64StringUsingAes(string base64EncryptedText, string key, out string plainText)
        {
            plainText = null;

            var iv = new byte[16];
            var buffer = Convert.FromBase64String(base64EncryptedText);

            using (var aes = Aes.Create())
            {
                aes.Key = Encoding.UTF8.GetBytes(key);
                aes.IV = iv;

                var decryptor = aes.CreateDecryptor(aes.Key, aes.IV);

                try
                {
                    using (var memoryStream = new MemoryStream(buffer))
                    {
                        using (var cryptoStream = new CryptoStream(memoryStream, decryptor, CryptoStreamMode.Read))
                        {
                            using (var streamReader = new StreamReader(cryptoStream))
                            {
                                plainText = streamReader.ReadToEnd();
                                return true;
                            }
                        }
                    }
                }
                catch
                {
                    return false;
                }
            }
        }
    }
}