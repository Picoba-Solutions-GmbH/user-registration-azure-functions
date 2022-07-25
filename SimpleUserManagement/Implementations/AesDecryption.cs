using System;
using System.Security.Cryptography;
using System.IO;
using System.Text;

namespace SimpleUserManagement.Implementations
{
    public class AesDecryption
    {
        public string DecryptCipherText(string cipherText)
        {
            var keybytes = Encoding.UTF8.GetBytes(Environment.GetEnvironmentVariable("AES_KEY"));
            var iv = Encoding.UTF8.GetBytes(Environment.GetEnvironmentVariable("IV_KEY"));
            var encrypted = Convert.FromBase64String(cipherText);
            return DecryptStringFromBytes(encrypted, keybytes, iv);
        }

        public string DecryptCipherText(string cipherText, string key, string iv)
        {
            var keybytes = Encoding.UTF8.GetBytes(key);
            var ivBytes = Encoding.UTF8.GetBytes(iv);
            var encrypted = Convert.FromBase64String(cipherText);
            return DecryptStringFromBytes(encrypted, keybytes, ivBytes);
        }

        private string DecryptStringFromBytes(byte[] cipherText, byte[] key, byte[] iv)
        {
            if (cipherText == null || cipherText.Length <= 0)
            {
                throw new ArgumentNullException("cipherText");
            }
            if (key == null || key.Length <= 0)
            {
                throw new ArgumentNullException("key");
            }
            if (iv == null || iv.Length <= 0)
            {
                throw new ArgumentNullException("key");
            }

            string plaintext = null;
            using (var rijAlg = new RijndaelManaged())
            {
                rijAlg.Mode = CipherMode.CBC;
                rijAlg.Padding = PaddingMode.PKCS7;
                rijAlg.FeedbackSize = 128;
                rijAlg.Key = key;
                rijAlg.IV = iv;

                var decryptor = rijAlg.CreateDecryptor(rijAlg.Key, rijAlg.IV);
                try
                {
                    using (var msDecrypt = new MemoryStream(cipherText))
                    {
                        using (var csDecrypt = new CryptoStream(msDecrypt, decryptor, CryptoStreamMode.Read))
                        {
                            using (var srDecrypt = new StreamReader(csDecrypt))
                            {
                                plaintext = srDecrypt.ReadToEnd();
                            }
                        }
                    }
                }
                catch
                {
                    plaintext = "keyError";
                }
            }
            return plaintext;
        }
    }
}
