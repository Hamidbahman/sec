using System;
using Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Infrastructure.Data;

public class AutheDbContextFactory : IDesignTimeDbContextFactory<AutheDbContext>
{
    public AutheDbContext CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<AutheDbContext>();
        optionsBuilder.UseSqlServer("Server=REVISION-PC\\HAMI;Database=Authentication;Integrated Security=True;TrustServerCertificate=True;");

        return new AutheDbContext(optionsBuilder.Options);
    }
}
