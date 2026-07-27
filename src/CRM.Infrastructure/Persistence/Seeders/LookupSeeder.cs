namespace CRM.Infrastructure.Persistence.Seeders;

using Microsoft.EntityFrameworkCore;
using System.Threading.Tasks;

/// <summary>
/// Seeds all 7 lookup tables via raw SQL with ON CONFLICT DO NOTHING, per Section 3.4
/// of the design document. NOTE: these same 7 tables are also currently seeded via
/// EF Core .HasData() in the InitialCreate migration — this seeder is idempotent
/// against that too (ON CONFLICT DO NOTHING means re-running this after the migration
/// already seeded the data is a no-op). A separate follow-up ticket will consolidate
/// this to a single seed source; do not attempt that consolidation here.
/// </summary>
public static class LookupSeeder
{
    public static async Task SeedAsync(CrmDbContext context)
    {
        await context.Database.ExecuteSqlRawAsync(@"
            INSERT INTO lookup_company_types (name, is_active, sort_order) VALUES
                ('Manufacturer', true, 1), ('Farmer', true, 2), ('Distributor', true, 3),
                ('Retailer', true, 4), ('Packhouse', true, 5), ('Financial', true, 6),
                ('Market', true, 7), ('Prepared Food', true, 8), ('Butcher', true, 9),
                ('Dairy', true, 10), ('Broker', true, 11), ('Mill', true, 12),
                ('Cold Storage', true, 13), ('Packaging', true, 14), ('Other', true, 15)
            ON CONFLICT (name) DO NOTHING;
        ");

        await context.Database.ExecuteSqlRawAsync(@"
            INSERT INTO lookup_entity_types (name, is_active, sort_order) VALUES
                ('Private Company', true, 1), ('Public Company', true, 2),
                ('Close Corporation', true, 3), ('Trust', true, 4),
                ('Non Profit', true, 5), ('Association', true, 6)
            ON CONFLICT (name) DO NOTHING;
        ");

        await context.Database.ExecuteSqlRawAsync(@"
            INSERT INTO lookup_operational_regions (code, name, is_active, sort_order) VALUES
                ('JHB', 'Johannesburg', true, 1), ('CPT', 'Cape Town', true, 2),
                ('KZN', 'KwaZulu-Natal', true, 3), ('EC', 'Eastern Cape', true, 4),
                ('BFN', 'Bloemfontein', true, 5), ('MPU', 'Mpumalanga', true, 6),
                ('LIM', 'Limpopo', true, 7)
            ON CONFLICT (name) DO NOTHING;
        ");

        await context.Database.ExecuteSqlRawAsync(@"
            INSERT INTO lookup_provinces (code, name, is_active, sort_order) VALUES
                ('EC', 'Eastern Cape', true, 1), ('FS', 'Free State', true, 2),
                ('GP', 'Gauteng', true, 3), ('KZN', 'KwaZulu-Natal', true, 4),
                ('LP', 'Limpopo', true, 5), ('MP', 'Mpumalanga', true, 6),
                ('NC', 'Northern Cape', true, 7), ('NW', 'North West', true, 8),
                ('WC', 'Western Cape', true, 9)
            ON CONFLICT (name) DO NOTHING;
        ");

        await context.Database.ExecuteSqlRawAsync(@"
            INSERT INTO lookup_donation_types (name, is_active, sort_order) VALUES
                ('Bakery', true, 1), ('Beverages', true, 2), ('Dairy', true, 3),
                ('Dry Goods', true, 4), ('Financial', true, 5), ('Fruit', true, 6),
                ('Meat', true, 7), ('Non Food', true, 8), ('Prepared Food', true, 9),
                ('Vegetables', true, 10), ('Other', true, 11)
            ON CONFLICT (name) DO NOTHING;
        ");

        await context.Database.ExecuteSqlRawAsync(@"
            INSERT INTO lookup_donation_frequencies (name, is_active, sort_order) VALUES
                ('Ad Hoc', true, 1), ('Once-off', true, 2), ('Weekly', true, 3),
                ('Monthly', true, 4), ('Seasonal', true, 5)
            ON CONFLICT (name) DO NOTHING;
        ");

        await context.Database.ExecuteSqlRawAsync(@"
            INSERT INTO lookup_bbbee_statuses (name, description, is_active, sort_order) VALUES
                ('Level 1', NULL, true, 1), ('Level 2', NULL, true, 2), ('Level 3', NULL, true, 3),
                ('Level 4', NULL, true, 4), ('Level 5', NULL, true, 5), ('Level 6', NULL, true, 6),
                ('Level 7', NULL, true, 7), ('Level 8', NULL, true, 8),
                ('Exempt Micro Enterprise', NULL, true, 9), ('Non-Compliant', NULL, true, 10),
                ('Not Applicable', NULL, true, 11)
            ON CONFLICT (name) DO NOTHING;
        ");
    }
}
