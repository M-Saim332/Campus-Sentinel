using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using CampusSentinel.Data;
using CampusSentinel.Models;

namespace CampusSentinel.Services
{
    public class AuthService : IAuthService
    {
        private readonly ApplicationDbContext _context;

        public AuthService(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<User> SignInAsync(string username, string password)
        {
            // NOTE: In production, use BCrypt or PBKDF2 for password hashing
            var user = await _context.Users
                .FirstOrDefaultAsync(u => u.Username == username
                                       && u.PasswordHash == password
                                       && u.IsActive);
            return user;
        }

        /// <summary>
        /// Registers a new user.
        /// Returns null (and sets <paramref name="errorMessage"/>) if the username already exists.
        /// </summary>
        public async Task<User> SignUpAsync(User user, string password)
        {
            // ── Duplicate-username guard ──────────────────────────────────────
            bool usernameExists = await _context.Users
                .AnyAsync(u => u.Username == user.Username);

            if (usernameExists)
            {
                return null;   // caller must handle this and show a warning
            }
            // ──────────────────────────────────────────────────────────────────

            // NOTE: In production, hash the password before saving
            user.PasswordHash = password;

            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            return user;
        }
    }
}
