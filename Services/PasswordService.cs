using System;
using BCrypt.Net;

namespace IPS.Services
{
    /// <summary>
    /// Service for password hashing and verification using BCrypt
    /// </summary>
    public class PasswordService
    {
        /// <summary>
        /// Hash a plain text password using BCrypt
        /// </summary>
        /// <param name="plainPassword">The plain text password to hash</param>
        /// <returns>BCrypt hashed password</returns>
        public string HashPassword(string plainPassword)
        {
            if (string.IsNullOrWhiteSpace(plainPassword))
                throw new ArgumentException("Password cannot be empty", nameof(plainPassword));

            return BCrypt.Net.BCrypt.HashPassword(plainPassword, BCrypt.Net.BCrypt.GenerateSalt());
        }

        /// <summary>
        /// Verify a plain text password against a BCrypt hash
        /// </summary>
        /// <param name="plainPassword">The plain text password to verify</param>
        /// <param name="hashedPassword">The BCrypt hash to verify against</param>
        /// <returns>True if password matches, false otherwise</returns>
        public bool VerifyPassword(string plainPassword, string hashedPassword)
        {
            if (string.IsNullOrWhiteSpace(plainPassword) || string.IsNullOrWhiteSpace(hashedPassword))
                return false;

            try
            {
                return BCrypt.Net.BCrypt.Verify(plainPassword, hashedPassword);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[PasswordService] Password verification error: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Create default admin PIN hash
        /// Default PIN is "0000"
        /// </summary>
        /// <returns>BCrypt hash of the default PIN "0000"</returns>
        public string GetDefaultPasswordHash()
        {
            return HashPassword("0000");
        }
    }
}
