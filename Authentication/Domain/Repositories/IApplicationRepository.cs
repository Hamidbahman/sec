using System.Threading.Tasks;
using Authentication.Domain.Entities;

namespace Authentication.Domain.Repositories
{
    public interface IApplicationRepository
    {
        Task<Application?> GetApplicationByClientIdAsync(string clientId);
        Task<ConfigurationLock?> GetConfigurationLockAsync(string clientId);
        Task<ConfigurationPassword?> GetConfigurationPasswordAsync(string clientId);
        Task<ConfigurationSession?> GetConfigurationSessionAsync(string clientId); // New method

        Task <bool> SaveChangesAsync();
    }
}
