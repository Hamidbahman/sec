using System;
using System.Threading.Tasks;
using Authentication.Domain.Entities;
using Authentication.Domain.Enums;
using Authentication.Domain.Repositories;
using Data;
using Microsoft.EntityFrameworkCore;

namespace Authentication.Infrastructure.Repositories
{


    public class UserRepository : IUserRepository
    {
        private readonly AutheDbContext _context;

        public UserRepository(AutheDbContext context)
        {
            _context = context;
        }
        public async Task<User?> GetByUsernameAsync(string username)
        {
            return await _context.Users
                .AsSplitQuery()
                .Include(u => u.UserProperty)
                .Include(u => u.LoginPolicy)  
                .Include(u=>u.UserRoles)
                .FirstOrDefaultAsync(u => u.Username == username);
        }

        public async Task<bool> ValidatePasswordAsync(string username, string password)
        {
            var user = await GetByUsernameAsync(username);
            if (user == null || user.UserProperty == null)
                return false;

            return user.UserProperty.Password == password; // ⚠️ Use hashing in production!
        }
        public async Task<bool> CheckLoginPolicyAsync(string username)
        {
            var user = await GetByUsernameAsync(username);
            if (user == null || user.LoginPolicy == null)
                return false;

            var policy = user.LoginPolicy;
            var now = DateTime.UtcNow;

            if (policy.LockTypes == LockTypes.TemporaryLock)
            {
                if (now >= policy.LockStartDateTime && now <= policy.LockEndDateTime)
                {
                    return false;
                }
            }
            else if (policy.LockTypes == LockTypes.PermanentLock)
            {
                return false;
            }

            return true;
        }

        public async Task<LoginPolicy> GetLoginPoliciesByUserID(string userId)
        {
            if (!long.TryParse(userId, out var userIdLong))
                return null; // Invalid ID format

            return await _context.LoginPolicies
                .FirstOrDefaultAsync(lp => lp.UserId == userIdLong); ;
        }

        public async Task<(string Username, string Password)?> GetUserCredentialsAsync(string username)
        {
            var user = await GetByUsernameAsync(username);
            if (user == null || user.UserProperty == null)
                return null;

            return (user.Username, user.UserProperty.Password);
        }

        public async Task<bool> SaveChangesAsync()
        {

            return await _context.SaveChangesAsync() > 0;

        }

        public async Task<User> GetUserByPhoneNumber(string phoneNumber)
        {
            User user = await _context.Users.FirstOrDefaultAsync(u => u.PhoneNumber == phoneNumber);
            return user;
        }

        public async Task<User> GetUserById(long userId)
        {
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Id == userId);
            return user;
        }


        public async Task<ICollection<UserRole>> GetUserRolesAsync(long userId)
        {
            return await _context.UserRoles
                .Include(ur => ur.Role)
                .Where(ur => ur.UserId == userId)
                .ToListAsync();            
        }

        public async Task<ICollection<string>> GetUserRoleTitlesAsync(long userId)
        {
            return await _context.UserRoles
                .Include(ur => ur.Role)
                .Where(ur => ur.UserId == userId)
                .Select(ur => ur.Role.Title)
                .ToListAsync();        
        }

        public async Task<UserRole?> GetDefaultUserRoleAsync(long userId)
        {
            return await _context.UserRoles
                .Include(ur => ur.Role)
                .FirstOrDefaultAsync(ur => ur.UserId == userId && ur.IsDefault);        }

        public async Task<bool> IsUserInRoleAsync(long userId, string roleName)
        {
            return await _context.UserRoles
                .Include(ur => ur.Role)
                .AnyAsync(ur => ur.UserId == userId && ur.Role.Title == roleName);        }

        public async Task AddUserRoleAsync(long userId, long roleId, bool isDefault = false)
        {
            var existingRole = await _context.UserRoles
                .FirstOrDefaultAsync(ur => ur.UserId == userId && ur.RoleId == roleId);

            if (existingRole == null)
            {
                var userRole = new UserRole(
                id: 0, // Let the database generate the ID
                userId: userId,
                roleId: roleId,
                isDefault: isDefault
        );

            _context.UserRoles.Add(userRole);
            await _context.SaveChangesAsync();
    }
        }

        public async Task RemoveUserRoleAsync(long userId, long roleId)
        {
            var userRole = await _context.UserRoles
                .FirstOrDefaultAsync(ur => ur.UserId == userId && ur.RoleId == roleId);

            if (userRole != null)
            {
                _context.UserRoles.Remove(userRole);
                await _context.SaveChangesAsync();
            }
        }

    }
}
