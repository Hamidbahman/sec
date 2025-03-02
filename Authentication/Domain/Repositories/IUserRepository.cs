using System.Threading.Tasks;
using Authentication.Domain.Entities;

namespace Authentication.Domain.Repositories
{
    public interface IUserRepository
    {
        Task<User?> GetByUsernameAsync(string username);
        Task<bool> ValidatePasswordAsync(string username, string password);
        Task<bool> CheckLoginPolicyAsync(string username);
        Task<LoginPolicy?> GetLoginPoliciesByUserID(string userId);
        Task<(string Username, string Password)?> GetUserCredentialsAsync(string username);
        Task<bool> SaveChangesAsync();
        Task<User> GetUserByPhoneNumber(string phoneNumber);
        Task<User> GetUserById(long userId);
        Task<UserRole> GetUserRolesByUserId(long id);
}
}