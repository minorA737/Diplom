using ManufactPlanner.Models;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ManufactPlanner.Services
{
    public class RoleService
    {
        private static RoleService _instance;
        public static RoleService Instance => _instance ??= new RoleService();

        // Константы ролей
        public const string ROLE_ADMINISTRATOR = "Администратор";
        public const string ROLE_MANAGER = "Менеджер";
        public const string ROLE_EXECUTOR = "Исполнитель";

        // Кэш ролей пользователя для быстрой проверки
        private Dictionary<Guid, List<string>> _userRolesCache = new Dictionary<Guid, List<string>>();

        // Получить роли пользователя
        public async Task<List<string>> GetUserRolesAsync(PostgresContext dbContext, Guid userId)
        {
            // Проверяем кэш
            if (_userRolesCache.ContainsKey(userId))
                return _userRolesCache[userId];

            // Загружаем из БД
            var roles = await dbContext.Users
                .Where(u => u.Id == userId)
                .SelectMany(u => u.Roles.Select(r => r.Name))
                .ToListAsync();

            // Сохраняем в кэш
            _userRolesCache[userId] = roles;
            return roles;
        }

        // Проверка роли пользователя
        public bool HasRole(Guid userId, string roleName)
        {
            if (!_userRolesCache.ContainsKey(userId))
                return false;

            return _userRolesCache[userId].Contains(roleName);
        }

        // Очистка кэша при выходе из системы
        public void ClearCache(Guid userId = default)
        {
            if (userId != default)
                _userRolesCache.Remove(userId);
            else
                _userRolesCache.Clear();
        }

    }
}