using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace aiolib
{
    public class AuthLib
    {
        public static string GenerateChallege(string data, string passw_hash, byte[] salt)
        {
            return "";
        }

        public static string? Encrypt(string data, string passw_hash, byte[] salt)
        {
            try
            { 
                //byte[] buffer = new byte[1024];
                using (MemoryStream buffer = new MemoryStream())
                {
                    using (Aes aes = Aes.Create())                    {

                        // PBKDF2 (password-based key derivation function)
                        const int Iterations = 300;
                        var keyGenerator = new Rfc2898DeriveBytes(passw_hash, salt, Iterations);                  
                        aes.Key = keyGenerator.GetBytes(aes.KeySize / 8);

                        // Generate a random IV
                        aes.GenerateIV();
                        // Pack the IV as the first few bytes unencrypted
                        byte[] iv = aes.IV;
                        buffer.Write(iv, 0, iv.Length);
                        // Create the stream transformer using key and IV
                        ICryptoTransform encryptor = aes.CreateEncryptor(aes.Key, aes.IV); 
                        // Write UTf8 encoded bytes through the stream causing it to encrypt.
                        using CryptoStream cryptoStream = new(buffer, encryptor, CryptoStreamMode.Write);
                        using (StreamWriter cryptwriter = new(cryptoStream))
                        {
                            cryptwriter.Write(data);
                        }
                        // Convert the stream to a byte array and base64 encode it
                        byte[] array = buffer.ToArray();
                        return Convert.ToBase64String(array);
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"The encryption failed. {ex}");
                return null;
            }
        }
        public static string? Decrypt(string cipherText, string passw_hash, byte[] salt)
        {
            try
            {
                byte[] buffer = Convert.FromBase64String(cipherText);
                using (MemoryStream encryptedStream = new(buffer))
                {
                    using (Aes aes = Aes.Create())
                    {
                        byte[] iv = new byte[aes.IV.Length];
                        int numBytesToRead = aes.IV.Length;
                        int numBytesRead = 0;
                        while (numBytesToRead > 0)
                        {
                            int n = encryptedStream.Read(iv, numBytesRead, numBytesToRead);
                            if (n == 0) break;

                            numBytesRead += n;
                            numBytesToRead -= n;
                        }

                        aes.IV = iv;

                        const int Iterations = 300;
                        byte[] user_key = Encoding.UTF8.GetBytes(passw_hash);
                        var keyGenerator = new Rfc2898DeriveBytes(passw_hash, salt, Iterations);

                        aes.Key = keyGenerator.GetBytes(aes.KeySize / 8);

                        ICryptoTransform decryptor = aes.CreateDecryptor(aes.Key, aes.IV);
                        using (CryptoStream cryptoStream = new(encryptedStream, decryptor, CryptoStreamMode.Read))
                        {
                            using (StreamReader cryptoreader = new(cryptoStream))
                            {
                                string decryptedMessage = cryptoreader.ReadToEnd();
                                return decryptedMessage;
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"The decryption failed. {ex}");
                return null;
            }            
        }
    }
}
