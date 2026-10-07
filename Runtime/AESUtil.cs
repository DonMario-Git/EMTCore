using System;
using System.IO;
using System.Security.Cryptography;

namespace EMT.Core
{
    public static class AESUtil
    {
        private static readonly string password = "ClaveSuperSecretaDelJuego";

        private const int KeySize = 32;
        private const int IvSize = 16;
        private const int SaltSize = 16;
        private const int Iterations = 100000;

        public static string Encrypt(string plainText)
        {
            if (string.IsNullOrEmpty(plainText))
                return "";

            try
            {
                byte[] salt = new byte[SaltSize];
                using (var rng = RandomNumberGenerator.Create())
                    rng.GetBytes(salt);

                using var keyDerivation = new Rfc2898DeriveBytes(password, salt, Iterations, HashAlgorithmName.SHA256);
                byte[] key = keyDerivation.GetBytes(KeySize);

                using var aes = Aes.Create();
                aes.Key = key;
                aes.GenerateIV();

                using var encryptor = aes.CreateEncryptor(aes.Key, aes.IV);
                using var ms = new MemoryStream();

                ms.Write(salt, 0, salt.Length);
                ms.Write(aes.IV, 0, aes.IV.Length);

                using (var cs = new CryptoStream(ms, encryptor, CryptoStreamMode.Write))
                using (var sw = new StreamWriter(cs))
                {
                    sw.Write(plainText);
                }

                return Convert.ToBase64String(ms.ToArray());
            }
            catch (Exception e)
            {
                UnityEngine.Debug.LogError("Encrypt Error: " + e.Message);
                return "";
            }
        }

        public static string Decrypt(string cipherText)
        {
            if (string.IsNullOrEmpty(cipherText))
                return "";

            try
            {
                byte[] fullData = Convert.FromBase64String(cipherText);

                if (fullData.Length < SaltSize + IvSize)
                    throw new Exception("Datos corruptos o incompletos");

                byte[] salt = new byte[SaltSize];
                Array.Copy(fullData, 0, salt, 0, SaltSize);

                byte[] iv = new byte[IvSize];
                Array.Copy(fullData, SaltSize, iv, 0, IvSize);

                int cipherStart = SaltSize + IvSize;
                int cipherLength = fullData.Length - cipherStart;

                if (cipherLength <= 0)
                    throw new Exception("Datos cifrados inválidos");

                byte[] cipherBytes = new byte[cipherLength];
                Array.Copy(fullData, cipherStart, cipherBytes, 0, cipherLength);

                using var keyDerivation = new Rfc2898DeriveBytes(password, salt, Iterations, HashAlgorithmName.SHA256);
                byte[] key = keyDerivation.GetBytes(KeySize);

                using var aes = Aes.Create();
                aes.Key = key;
                aes.IV = iv;

                using var decryptor = aes.CreateDecryptor(aes.Key, aes.IV);
                using var ms = new MemoryStream(cipherBytes);
                using var cs = new CryptoStream(ms, decryptor, CryptoStreamMode.Read);
                using var sr = new StreamReader(cs);

                return sr.ReadToEnd();
            }
            catch (Exception e)
            {
                UnityEngine.Debug.LogWarning("Decrypt Error (posible archivo corrupto): " + e.Message);
                return "";
            }
        }

        public static bool TryDecrypt(string cipherText, out string result)
        {
            result = "";

            if (string.IsNullOrEmpty(cipherText))
                return false;

            try
            {
                result = Decrypt(cipherText);
                return !string.IsNullOrEmpty(result);
            }
            catch
            {
                return false;
            }
        }
    }
}