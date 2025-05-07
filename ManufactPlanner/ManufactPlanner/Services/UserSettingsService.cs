// Services/UserSettingsService.cs
using ManufactPlanner.Models;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace ManufactPlanner.Services
{
    public class UserSettingsService
    {
        private readonly PostgresContext _dbContext;

        public UserSettingsService(PostgresContext dbContext)
        {
            _dbContext = dbContext;
        }

        public async Task<User> GetUserProfileAsync(Guid userId)
        {
            return await System.Threading.Tasks.Task.Run(() =>
                _dbContext.Users.FirstOrDefault(u => u.Id == userId)
            );
        }

        public async Task<bool> UpdateUserProfileAsync(User user)
        {
            try
            {
                var existingUser = await GetUserProfileAsync(user.Id);
                if (existingUser == null)
                {
                    return false;
                }

                // Обновляем только необходимые поля
                existingUser.FirstName = user.FirstName;
                existingUser.LastName = user.LastName;
                existingUser.Email = user.Email;
                existingUser.UpdatedAt = DateTime.Now;

                await System.Threading.Tasks.Task.Run(() => _dbContext.SaveChanges());
                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }

        public async Task<bool> ChangePasswordAsync(Guid userId, string currentPassword, string newPassword)
        {
            try
            {
                var user = await GetUserProfileAsync(userId);
                if (user == null || user.Password != currentPassword)
                {
                    return false;
                }

                user.Password = newPassword;
                user.UpdatedAt = DateTime.Now;

                await System.Threading.Tasks.Task.Run(() => _dbContext.SaveChanges());
                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }
    }
}