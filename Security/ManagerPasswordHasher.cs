using System;
using System.Security.Cryptography;

namespace Rec_Partapgarh.Security
{
    public static class ManagerPasswordHasher
    {
        public const int DefaultIterations = 210000;

        public static PasswordHashResult Hash(string password, int iterations = DefaultIterations)
        {
            if (string.IsNullOrEmpty(password)) throw new ArgumentException("Password is required.", nameof(password));
            var salt = new byte[32];
            using (var rng = RandomNumberGenerator.Create()) rng.GetBytes(salt);
            using (var pbkdf2 = new Rfc2898DeriveBytes(password, salt, iterations, HashAlgorithmName.SHA256))
            {
                return new PasswordHashResult(Convert.ToBase64String(pbkdf2.GetBytes(32)), Convert.ToBase64String(salt), iterations);
            }
        }

        public static bool Verify(string password, string expectedHash, string salt, int iterations)
        {
            if (string.IsNullOrEmpty(password) || string.IsNullOrEmpty(expectedHash) || string.IsNullOrEmpty(salt) || iterations < 10000) return false;
            try
            {
                var expected = Convert.FromBase64String(expectedHash);
                using (var pbkdf2 = new Rfc2898DeriveBytes(password, Convert.FromBase64String(salt), iterations, HashAlgorithmName.SHA256))
                    return FixedTimeEquals(pbkdf2.GetBytes(expected.Length), expected);
            }
            catch (FormatException) { return false; }
        }

        private static bool FixedTimeEquals(byte[] left, byte[] right)
        {
            var difference = left.Length ^ right.Length;
            var length = Math.Min(left.Length, right.Length);
            for (var i = 0; i < length; i++) difference |= left[i] ^ right[i];
            return difference == 0;
        }
    }

    public sealed class PasswordHashResult
    {
        public PasswordHashResult(string hash, string salt, int iterations) { Hash = hash; Salt = salt; Iterations = iterations; }
        public string Hash { get; }
        public string Salt { get; }
        public int Iterations { get; }
    }
}
