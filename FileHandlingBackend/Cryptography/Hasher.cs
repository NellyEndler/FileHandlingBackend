using Microsoft.AspNetCore.Cryptography.KeyDerivation;
using System.Security.Cryptography;

namespace FileHandlingBackend.Cryptography
{
    public class Hasher:IHasher
    {
        private const int SALT_BYTE_SIZE = 16;
        private const int HASH_BYTE_SIZE = 32; 
        private const int PBKDF2_ITERATIONS = 100000;

        public string CreateHash(string password)
        {
            byte[] salt = new byte[SALT_BYTE_SIZE];
            RandomNumberGenerator.Fill(salt);
            byte[] hash = KeyDerivation.Pbkdf2(password, salt, KeyDerivationPrf.HMACSHA256, PBKDF2_ITERATIONS, HASH_BYTE_SIZE);
            return $"{PBKDF2_ITERATIONS}:{Convert.ToBase64String(salt)}:{Convert.ToBase64String(hash)}";
        }

        public bool ValidatePassword(string password, string correctHash)
        {
            try
            {
                char[] delimiter = { ':' };
                string[] split = correctHash.Split(delimiter);
                int iterations = Int32.Parse(split[0]); 
                byte[] salt = Convert.FromBase64String(split[1]);
                byte[] storedHash = Convert.FromBase64String(split[2]);
                byte[] computedHash = KeyDerivation.Pbkdf2(password, salt, KeyDerivationPrf.HMACSHA256, iterations, HASH_BYTE_SIZE);
                return SlowEquals(storedHash, computedHash);
            }
            catch (Exception)
            {
                return false;
            }
        }

        public bool SlowEquals(byte[] a, byte[] b)
        {
            uint diff = (uint)a.Length ^ (uint)b.Length;
            for (int i = 0; i < a.Length && i < b.Length; i++)
                diff |= (uint)(a[i] ^ b[i]);
            return diff == 0;
        }
    }
}

