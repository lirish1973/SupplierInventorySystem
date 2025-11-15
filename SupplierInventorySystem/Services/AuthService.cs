using Microsoft.EntityFrameworkCore;
using SupplierInventorySystem.Data;
using SupplierInventorySystem.Models;
using BCrypt.Net;

namespace SupplierInventorySystem.Services
{
    public interface IAuthService
    {
        Task<(bool Success, string Message, User? User)> LoginAsync(string username, string password);
        Task<(bool Success, string Message)> RegisterAsync(User user, string password);
        Task<bool> IsUsernameTakenAsync(string username);
        Task<bool> IsEmailTakenAsync(string email);
        Task LogoutAsync(int userId);
        Task<User?> GetUserByIdAsync(int userId);
        Task<User?> GetUserByUsernameAsync(string username);
        Task<bool> ChangePasswordAsync(int userId, string oldPassword, string newPassword);
        Task<(bool Success, string Message)> ResetPasswordRequestAsync(string email);
        Task<(bool Success, string Message)> ResetPasswordAsync(string token, string newPassword);
        Task UnlockUserAsync(int userId);
        string HashPassword(string password);
        bool VerifyPassword(string password, string hash);
    }

    public class AuthService : IAuthService
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<AuthService> _logger;
        private const int MaxFailedAttempts = 5;
        private const int LockoutMinutes = 15;

        public AuthService(ApplicationDbContext context, ILogger<AuthService> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task<(bool Success, string Message, User? User)> LoginAsync(string username, string password)
        {
            try
            {
                var user = await _context.Users
                    .Include(u => u.Role)
                    .FirstOrDefaultAsync(u => u.Username == username);

                if (user == null)
                {
                    return (false, "שם משתמש או סיסמה שגויים", null);
                }

                // בדיקת חסימה
                if (user.LockoutEnd.HasValue && user.LockoutEnd.Value > DateTime.Now)
                {
                    var remainingMinutes = (int)(user.LockoutEnd.Value - DateTime.Now).TotalMinutes + 1;
                    return (false, $"החשבון נעול. נסה שוב בעוד {remainingMinutes} דקות", null);
                }

                // בדיקת משתמש פעיל
                if (!user.IsActive)
                {
                    return (false, "המשתמש אינו פעיל. פנה למנהל המערכת", null);
                }

                // אימות סיסמה
                if (!VerifyPassword(password, user.PasswordHash))
                {
                    user.FailedLoginAttempts++;

                    if (user.FailedLoginAttempts >= MaxFailedAttempts)
                    {
                        user.LockoutEnd = DateTime.Now.AddMinutes(LockoutMinutes);
                        user.FailedLoginAttempts = 0;
                        await _context.SaveChangesAsync();

                        _logger.LogWarning($"User {username} locked out after {MaxFailedAttempts} failed attempts");
                        return (false, $"יותר מדי ניסיונות כושלים. החשבון ננעל ל-{LockoutMinutes} דקות", null);
                    }

                    await _context.SaveChangesAsync();
                    return (false, $"שם משתמש או סיסמה שגויים. נותרו {MaxFailedAttempts - user.FailedLoginAttempts} ניסיונות", null);
                }

                // התחברות מצליחה
                user.LastLogin = DateTime.Now;
                user.FailedLoginAttempts = 0;
                user.LockoutEnd = null;
                await _context.SaveChangesAsync();

                _logger.LogInformation($"User {username} logged in successfully");
                return (true, "התחברות בוצעה בהצלחה", user);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error during login for user {username}");
                return (false, "אירעה שגיאה במהלך ההתחברות", null);
            }
        }

        public async Task<(bool Success, string Message)> RegisterAsync(User user, string password)
        {
            try
            {
                // בדיקות קיום
                if (await IsUsernameTakenAsync(user.Username))
                {
                    return (false, "שם המשתמש כבר קיים במערכת");
                }

                if (await IsEmailTakenAsync(user.Email))
                {
                    return (false, "כתובת הדוא״ל כבר רשומה במערכת");
                }

                // Hash סיסמה
                user.PasswordHash = HashPassword(password);
                user.CreatedAt = DateTime.Now;
                user.IsActive = true;

                // אם אין משתמשים - הראשון הוא Admin
                var usersCount = await _context.Users.CountAsync();
                if (usersCount == 0)
                {
                    var adminRole = await _context.Roles.FirstOrDefaultAsync(r => r.Name == "Admin");
                    if (adminRole != null)
                    {
                        user.RoleId = adminRole.Id;
                    }
                }

                _context.Users.Add(user);
                await _context.SaveChangesAsync();

                _logger.LogInformation($"New user registered: {user.Username}");
                return (true, "ההרשמה בוצעה בהצלחה");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error during registration for user {user.Username}");
                return (false, "אירעה שגיאה במהלך ההרשמה");
            }
        }

        public async Task<bool> IsUsernameTakenAsync(string username)
        {
            return await _context.Users.AnyAsync(u => u.Username == username);
        }

        public async Task<bool> IsEmailTakenAsync(string email)
        {
            return await _context.Users.AnyAsync(u => u.Email == email);
        }

        public async Task LogoutAsync(int userId)
        {
            _logger.LogInformation($"User {userId} logged out");
            await Task.CompletedTask;
        }

        public async Task<User?> GetUserByIdAsync(int userId)
        {
            return await _context.Users
                .Include(u => u.Role)
                .FirstOrDefaultAsync(u => u.Id == userId);
        }

        public async Task<User?> GetUserByUsernameAsync(string username)
        {
            return await _context.Users
                .Include(u => u.Role)
                .FirstOrDefaultAsync(u => u.Username == username);
        }

        public async Task<bool> ChangePasswordAsync(int userId, string oldPassword, string newPassword)
        {
            try
            {
                var user = await _context.Users.FindAsync(userId);
                if (user == null) return false;

                if (!VerifyPassword(oldPassword, user.PasswordHash))
                {
                    return false;
                }

                user.PasswordHash = HashPassword(newPassword);
                await _context.SaveChangesAsync();

                _logger.LogInformation($"Password changed for user {user.Username}");
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error changing password for user {userId}");
                return false;
            }
        }

        public async Task<(bool Success, string Message)> ResetPasswordRequestAsync(string email)
        {
            try
            {
                var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == email);
                if (user == null)
                {
                    // אל תחשוף שהמייל לא קיים - אבטחה
                    return (true, "אם כתובת הדוא״ל קיימת במערכת, נשלח אליך קישור לאיפוס סיסמה");
                }

                // יצירת טוקן
                user.ResetToken = Guid.NewGuid().ToString("N");
                user.ResetTokenExpiry = DateTime.Now.AddHours(24);
                await _context.SaveChangesAsync();

                // כאן תוסיף שליחת מייל
                _logger.LogInformation($"Password reset requested for {email}. Token: {user.ResetToken}");

                return (true, "אם כתובת הדוא״ל קיימת במערכת, נשלח אליך קישור לאיפוס סיסמה");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error requesting password reset for {email}");
                return (false, "אירעה שגיאה. נסה שוב מאוחר יותר");
            }
        }

        public async Task<(bool Success, string Message)> ResetPasswordAsync(string token, string newPassword)
        {
            try
            {
                var user = await _context.Users.FirstOrDefaultAsync(u => u.ResetToken == token);
                if (user == null)
                {
                    return (false, "קישור לא תקין או שפג תוקפו");
                }

                if (user.ResetTokenExpiry < DateTime.Now)
                {
                    return (false, "קישור פג תוקף. בקש איפוס סיסמה חדש");
                }

                user.PasswordHash = HashPassword(newPassword);
                user.ResetToken = null;
                user.ResetTokenExpiry = null;
                user.FailedLoginAttempts = 0;
                user.LockoutEnd = null;
                await _context.SaveChangesAsync();

                _logger.LogInformation($"Password reset successfully for user {user.Username}");
                return (true, "הסיסמה אופסה בהצלחה");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error resetting password");
                return (false, "אירעה שגיאה. נסה שוב מאוחר יותר");
            }
        }

        public async Task UnlockUserAsync(int userId)
        {
            var user = await _context.Users.FindAsync(userId);
            if (user != null)
            {
                user.LockoutEnd = null;
                user.FailedLoginAttempts = 0;
                await _context.SaveChangesAsync();
                _logger.LogInformation($"User {user.Username} unlocked");
            }
        }

        public string HashPassword(string password)
        {
            return BCrypt.Net.BCrypt.HashPassword(password, BCrypt.Net.BCrypt.GenerateSalt(12));
        }

        public bool VerifyPassword(string password, string hash)
        {
            try
            {
                return BCrypt.Net.BCrypt.Verify(password, hash);
            }
            catch
            {
                return false;
            }
        }
    }
}