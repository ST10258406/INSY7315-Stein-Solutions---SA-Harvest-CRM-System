namespace CRM.Infrastructure.Persistence.Seeders;

using CRM.Domain.Constants;
using CRM.Domain.Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using System.Security.Cryptography;

/// <summary>
/// Seeds the well-known "system actor" users (see <see cref="SystemUsers"/>) that
/// own records created by a process rather than a logged-in person — today, just
/// the public-form actor used by SubmitPublicDonorCommandHandler. Runs in every
/// environment (not gated to Development like DevDataSeeder): production needs
/// this row to exist before the public form can ever be submitted.
/// </summary>
public static class SystemUserSeeder
{
    public static async Task SeedAsync(CrmDbContext context)
    {
        if (await context.Users.AnyAsync(u => u.Email == SystemUsers.PublicFormEmail))
        {
            return;
        }

        var user = new User
        {
            Id = Guid.NewGuid(),
            FirstName = "Public",
            LastName = "Form Submission",
            Email = SystemUsers.PublicFormEmail,
            // Nobody knows this password and nobody ever will — it's discarded
            // immediately after hashing. IsActive = false as a second, belt-and-
            // braces guard against this account ever being used to authenticate.
            PasswordHash = new PasswordHasher<User>().HashPassword(
                null!, Convert.ToBase64String(RandomNumberGenerator.GetBytes(32))),
            IsActive = false,
        };

        context.Users.Add(user);
        await context.SaveChangesAsync();
    }
}
