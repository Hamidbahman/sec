using System.Threading.Tasks;
using Authentication.Domain.Entities;

namespace Authentication.Domain.Repositories
{
    /// <summary>
    /// Repository interface for ConfigurationSession entities
    /// </summary>
    public interface IConfigurationSessionRepository
    {
        /// <summary>
        /// Adds a new ConfigurationSession
        /// </summary>
        /// <param name="configurationSession">The configuration session to add</param>
        /// <returns>The added configuration session</returns>
        Task<ConfigurationSession> AddAsync(ConfigurationSession configurationSession);

        /// <summary>
        /// Updates an existing ConfigurationSession
        /// </summary>
        /// <param name="configurationSession">The configuration session to update</param>
        /// <returns>The updated configuration session</returns>
        Task<ConfigurationSession> UpdateAsync(ConfigurationSession configurationSession);

        /// <summary>
        /// Deletes a ConfigurationSession
        /// </summary>
        /// <param name="configurationSession">The configuration session to delete</param>
        /// <returns>A task representing the asynchronous operation</returns>
        Task DeleteAsync(ConfigurationSession configurationSession);

        /// <summary>
        /// Retrieves a ConfigurationSession by its ID
        /// </summary>
        /// <param name="id">The ID of the configuration session</param>
        /// <returns>The found configuration session or null</returns>
        Task<ConfigurationSession> GetByIdAsync(long id);

        /// <summary>
        /// Retrieves a ConfigurationSession by Application ID
        /// </summary>
        /// <param name="applicationId">The ID of the application</param>
        /// <returns>ConfigurationSession for the given application</returns>
        Task<ConfigurationSession> GetByApplicationIdAsync(long applicationId);

        /// <summary>
        /// Checks if a configuration session exists for a specific application
        /// </summary>
        /// <param name="applicationId">The ID of the application</param>
        /// <returns>True if a configuration session exists, otherwise false</returns>
        Task<bool> ExistsByApplicationIdAsync(long applicationId);

        /// <summary>
        /// Retrieves all ConfigurationSessions
        /// </summary>
        /// <returns>A collection of all configuration sessions</returns>
        Task<IEnumerable<ConfigurationSession>> GetAllAsync();

        Task<bool> SaveChangesAsync();
    }
}