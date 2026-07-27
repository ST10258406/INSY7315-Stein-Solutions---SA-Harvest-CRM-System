namespace CRM.Infrastructure.Persistence.Seeders;

using CRM.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using System;
using System.Threading.Tasks;

public static class RoleSeeder
{
    public static async Task SeedAsync(CrmDbContext context)
    {
        var roles = new[]
        {
            new { Name = "SuperAdmin", Description = "Full system access including role assignment" },
            new { Name = "Admin", Description = "Approvals, reports, BBBEE documents, user management" },
            new { Name = "Procurement", Description = "Full donor management, interactions, tasks" },
            new { Name = "Marketing", Description = "Read access to donors, marketing fields" },
        };

        foreach (var r in roles)
        {
            if (!await context.Roles.AnyAsync(x => x.Name == r.Name))
            {
                context.Roles.Add(new Role
                {
                    Id = Guid.NewGuid(),
                    Name = r.Name,
                    Description = r.Description,
                });
            }
        }

        await context.SaveChangesAsync();
    }
}
