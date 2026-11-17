
using System;
using System.Security.Cryptography;

namespace _25DH110266_MyStore.Models
{
    public static class PasswordHasher
    {
        private const int SaltSize = 16;
        private const int HashSize = 32;
        private const int Iterations = 100000;

        public static string HashPassword(string password)
        {
            if (string.IsNullOrEmpty(password))
                throw new ArgumentException("Mật khẩu không được để trống.");

            byte[] salt = new byte[SaltSize];

            using (var rng = RandomNumberGenerator.Create())
            {
                rng.GetBytes(salt);
            }

            using (var pbkdf2 = new Rfc2898DeriveBytes(
                password, salt, Iterations))
            {
                byte[] hash = pbkdf2.GetBytes(HashSize);

                return "PBKDF2$" + Iterations + "$"
                    + Convert.ToBase64String(salt) + "$"
                    + Convert.ToBase64String(hash);
            }
        }

        public static bool VerifyPassword(
            string password, string storedPassword)
        {
            if (string.IsNullOrEmpty(password) ||
                string.IsNullOrEmpty(storedPassword))
                return false;

            string[] parts = storedPassword.Split('$');

            if (parts.Length != 4 || parts[0] != "PBKDF2")
                return false;

            int iterations;

            if (!int.TryParse(parts[1], out iterations) ||
                iterations < 10000 || iterations > 1000000)
                return false;

            try
            {
                byte[] salt = Convert.FromBase64String(parts[2]);
                byte[] expectedHash = Convert.FromBase64String(parts[3]);

                if (salt.Length != SaltSize ||
                    expectedHash.Length != HashSize)
                    return false;

                using (var pbkdf2 = new Rfc2898DeriveBytes(
                    password, salt, iterations))
                {
                    byte[] actualHash = pbkdf2.GetBytes(HashSize);

                    int difference = 0;

                    for (int i = 0; i < HashSize; i++)
                    {
                        difference |= actualHash[i] ^ expectedHash[i];
                    }

                    return difference == 0;
                }
            }
            catch (FormatException)
            {
                return false;
            }
        }

        public static string UpgradePassword(string password)
        {
            return HashPassword(password);
        }
    }
}
