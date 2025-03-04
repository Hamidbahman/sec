using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Authentication.Domain.Entities;
using Authentication.Domain.Repositories;
using Data;
using Microsoft.EntityFrameworkCore;

namespace Authentication.Infrastructure.Repositories
{
    public class ConfigurationSessionRepository : IConfigurationSessionRepository
    {
        private readonly AutheDbContext _context;

        public ConfigurationSessionRepository(AutheDbContext context)
        {
            _context = context;
        }

        public async Task<ConfigurationSession> AddAsync(ConfigurationSession configurationSession)
        {
            await _context.ConfigurationSessions.AddAsync(configurationSession);
            await _context.SaveChangesAsync();
            return configurationSession;
        }

        public async Task<ConfigurationSession> UpdateAsync(ConfigurationSession configurationSession)
        {
            _context.ConfigurationSessions.Update(configurationSession);
            await _context.SaveChangesAsync();
            return configurationSession;
        }

        public async Task DeleteAsync(ConfigurationSession configurationSession)
        {
            _context.ConfigurationSessions.Remove(configurationSession);
            await _context.SaveChangesAsync();
        }

        public async Task<ConfigurationSession> GetByIdAsync(long id)
        {
            return await _context.ConfigurationSessions.FindAsync(id);
        }

        public async Task<ConfigurationSession> GetByApplicationIdAsync(long applicationId)
        {
            return await _context.ConfigurationSessions.FirstOrDefaultAsync(cs => cs.ApplicationId == applicationId);
        }

        public async Task<bool> ExistsByApplicationIdAsync(long applicationId)
        {
            return await _context.ConfigurationSessions.AnyAsync(cs => cs.ApplicationId == applicationId);
        }

        public async Task<IEnumerable<ConfigurationSession>> GetAllAsync()
        {
            return await _context.ConfigurationSessions.ToListAsync();
        }
    }
}
