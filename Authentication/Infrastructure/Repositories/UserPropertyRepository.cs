using System;
using Authentication.Domain.Entities;
using Data;
using Domain.Repositories;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Repositories;

public class UserPropertyRepository : IUserPropertyRepository
{
    private readonly AutheDbContext _context;
    public UserPropertyRepository(AutheDbContext context)
    {
        _context = context;
    }

    public async Task<ConfigurationPassword?> GetConfigurationPasswordByUserIdAsync(long configurationPasswordId)
    {

        return await _context.UserProperties
            .Where(up => up.ConfigurationPasswordId == configurationPasswordId)  // Filter by UserId
            .Select(up => up.ConfigurationPassword) // Select the related ConfigurationPassword
            .FirstOrDefaultAsync(); // Get the first matching record or null

    }

    public async Task<bool> SaveChangesAsync()
    {
        await _context.SaveChangesAsync();
        return true;
    }
}

