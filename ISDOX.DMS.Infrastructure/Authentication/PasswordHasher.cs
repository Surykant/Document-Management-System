using ISDOX.DMS.Application.Interfaces;
using BC = BCrypt.Net.BCrypt;

namespace ISDOX.DMS.Infrastructure.Authentication
{
    public class PasswordHasher : IPasswordHasher
    {
        public string HashPassword(string password)
        {
            return BC.HashPassword(password);
        }

        public bool VerifyPassword(string password, string hashedPassword)
        {
            return BC.Verify(password, hashedPassword);
        }
    }
}
