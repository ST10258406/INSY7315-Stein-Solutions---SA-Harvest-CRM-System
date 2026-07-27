using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace CRM.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class RemoveLookupHasData : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "lookup_bbbee_statuses",
                keyColumn: "id",
                keyValue: (short)1);

            migrationBuilder.DeleteData(
                table: "lookup_bbbee_statuses",
                keyColumn: "id",
                keyValue: (short)2);

            migrationBuilder.DeleteData(
                table: "lookup_bbbee_statuses",
                keyColumn: "id",
                keyValue: (short)3);

            migrationBuilder.DeleteData(
                table: "lookup_bbbee_statuses",
                keyColumn: "id",
                keyValue: (short)4);

            migrationBuilder.DeleteData(
                table: "lookup_bbbee_statuses",
                keyColumn: "id",
                keyValue: (short)5);

            migrationBuilder.DeleteData(
                table: "lookup_bbbee_statuses",
                keyColumn: "id",
                keyValue: (short)6);

            migrationBuilder.DeleteData(
                table: "lookup_bbbee_statuses",
                keyColumn: "id",
                keyValue: (short)7);

            migrationBuilder.DeleteData(
                table: "lookup_bbbee_statuses",
                keyColumn: "id",
                keyValue: (short)8);

            migrationBuilder.DeleteData(
                table: "lookup_bbbee_statuses",
                keyColumn: "id",
                keyValue: (short)9);

            migrationBuilder.DeleteData(
                table: "lookup_bbbee_statuses",
                keyColumn: "id",
                keyValue: (short)10);

            migrationBuilder.DeleteData(
                table: "lookup_bbbee_statuses",
                keyColumn: "id",
                keyValue: (short)11);

            migrationBuilder.DeleteData(
                table: "lookup_company_types",
                keyColumn: "id",
                keyValue: (short)1);

            migrationBuilder.DeleteData(
                table: "lookup_company_types",
                keyColumn: "id",
                keyValue: (short)2);

            migrationBuilder.DeleteData(
                table: "lookup_company_types",
                keyColumn: "id",
                keyValue: (short)3);

            migrationBuilder.DeleteData(
                table: "lookup_company_types",
                keyColumn: "id",
                keyValue: (short)4);

            migrationBuilder.DeleteData(
                table: "lookup_company_types",
                keyColumn: "id",
                keyValue: (short)5);

            migrationBuilder.DeleteData(
                table: "lookup_company_types",
                keyColumn: "id",
                keyValue: (short)6);

            migrationBuilder.DeleteData(
                table: "lookup_company_types",
                keyColumn: "id",
                keyValue: (short)7);

            migrationBuilder.DeleteData(
                table: "lookup_company_types",
                keyColumn: "id",
                keyValue: (short)8);

            migrationBuilder.DeleteData(
                table: "lookup_company_types",
                keyColumn: "id",
                keyValue: (short)9);

            migrationBuilder.DeleteData(
                table: "lookup_company_types",
                keyColumn: "id",
                keyValue: (short)10);

            migrationBuilder.DeleteData(
                table: "lookup_company_types",
                keyColumn: "id",
                keyValue: (short)11);

            migrationBuilder.DeleteData(
                table: "lookup_company_types",
                keyColumn: "id",
                keyValue: (short)12);

            migrationBuilder.DeleteData(
                table: "lookup_company_types",
                keyColumn: "id",
                keyValue: (short)13);

            migrationBuilder.DeleteData(
                table: "lookup_company_types",
                keyColumn: "id",
                keyValue: (short)14);

            migrationBuilder.DeleteData(
                table: "lookup_company_types",
                keyColumn: "id",
                keyValue: (short)15);

            migrationBuilder.DeleteData(
                table: "lookup_donation_frequencies",
                keyColumn: "id",
                keyValue: (short)1);

            migrationBuilder.DeleteData(
                table: "lookup_donation_frequencies",
                keyColumn: "id",
                keyValue: (short)2);

            migrationBuilder.DeleteData(
                table: "lookup_donation_frequencies",
                keyColumn: "id",
                keyValue: (short)3);

            migrationBuilder.DeleteData(
                table: "lookup_donation_frequencies",
                keyColumn: "id",
                keyValue: (short)4);

            migrationBuilder.DeleteData(
                table: "lookup_donation_frequencies",
                keyColumn: "id",
                keyValue: (short)5);

            migrationBuilder.DeleteData(
                table: "lookup_donation_types",
                keyColumn: "id",
                keyValue: (short)1);

            migrationBuilder.DeleteData(
                table: "lookup_donation_types",
                keyColumn: "id",
                keyValue: (short)2);

            migrationBuilder.DeleteData(
                table: "lookup_donation_types",
                keyColumn: "id",
                keyValue: (short)3);

            migrationBuilder.DeleteData(
                table: "lookup_donation_types",
                keyColumn: "id",
                keyValue: (short)4);

            migrationBuilder.DeleteData(
                table: "lookup_donation_types",
                keyColumn: "id",
                keyValue: (short)5);

            migrationBuilder.DeleteData(
                table: "lookup_donation_types",
                keyColumn: "id",
                keyValue: (short)6);

            migrationBuilder.DeleteData(
                table: "lookup_donation_types",
                keyColumn: "id",
                keyValue: (short)7);

            migrationBuilder.DeleteData(
                table: "lookup_donation_types",
                keyColumn: "id",
                keyValue: (short)8);

            migrationBuilder.DeleteData(
                table: "lookup_donation_types",
                keyColumn: "id",
                keyValue: (short)9);

            migrationBuilder.DeleteData(
                table: "lookup_donation_types",
                keyColumn: "id",
                keyValue: (short)10);

            migrationBuilder.DeleteData(
                table: "lookup_donation_types",
                keyColumn: "id",
                keyValue: (short)11);

            migrationBuilder.DeleteData(
                table: "lookup_entity_types",
                keyColumn: "id",
                keyValue: (short)1);

            migrationBuilder.DeleteData(
                table: "lookup_entity_types",
                keyColumn: "id",
                keyValue: (short)2);

            migrationBuilder.DeleteData(
                table: "lookup_entity_types",
                keyColumn: "id",
                keyValue: (short)3);

            migrationBuilder.DeleteData(
                table: "lookup_entity_types",
                keyColumn: "id",
                keyValue: (short)4);

            migrationBuilder.DeleteData(
                table: "lookup_entity_types",
                keyColumn: "id",
                keyValue: (short)5);

            migrationBuilder.DeleteData(
                table: "lookup_entity_types",
                keyColumn: "id",
                keyValue: (short)6);

            migrationBuilder.DeleteData(
                table: "lookup_operational_regions",
                keyColumn: "id",
                keyValue: (short)1);

            migrationBuilder.DeleteData(
                table: "lookup_operational_regions",
                keyColumn: "id",
                keyValue: (short)2);

            migrationBuilder.DeleteData(
                table: "lookup_operational_regions",
                keyColumn: "id",
                keyValue: (short)3);

            migrationBuilder.DeleteData(
                table: "lookup_operational_regions",
                keyColumn: "id",
                keyValue: (short)4);

            migrationBuilder.DeleteData(
                table: "lookup_operational_regions",
                keyColumn: "id",
                keyValue: (short)5);

            migrationBuilder.DeleteData(
                table: "lookup_operational_regions",
                keyColumn: "id",
                keyValue: (short)6);

            migrationBuilder.DeleteData(
                table: "lookup_operational_regions",
                keyColumn: "id",
                keyValue: (short)7);

            migrationBuilder.DeleteData(
                table: "lookup_provinces",
                keyColumn: "id",
                keyValue: (short)1);

            migrationBuilder.DeleteData(
                table: "lookup_provinces",
                keyColumn: "id",
                keyValue: (short)2);

            migrationBuilder.DeleteData(
                table: "lookup_provinces",
                keyColumn: "id",
                keyValue: (short)3);

            migrationBuilder.DeleteData(
                table: "lookup_provinces",
                keyColumn: "id",
                keyValue: (short)4);

            migrationBuilder.DeleteData(
                table: "lookup_provinces",
                keyColumn: "id",
                keyValue: (short)5);

            migrationBuilder.DeleteData(
                table: "lookup_provinces",
                keyColumn: "id",
                keyValue: (short)6);

            migrationBuilder.DeleteData(
                table: "lookup_provinces",
                keyColumn: "id",
                keyValue: (short)7);

            migrationBuilder.DeleteData(
                table: "lookup_provinces",
                keyColumn: "id",
                keyValue: (short)8);

            migrationBuilder.DeleteData(
                table: "lookup_provinces",
                keyColumn: "id",
                keyValue: (short)9);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.InsertData(
                table: "lookup_bbbee_statuses",
                columns: new[] { "id", "description", "is_active", "name", "sort_order" },
                values: new object[,]
                {
                    { (short)1, null, true, "Level 1", (short)1 },
                    { (short)2, null, true, "Level 2", (short)2 },
                    { (short)3, null, true, "Level 3", (short)3 },
                    { (short)4, null, true, "Level 4", (short)4 },
                    { (short)5, null, true, "Level 5", (short)5 },
                    { (short)6, null, true, "Level 6", (short)6 },
                    { (short)7, null, true, "Level 7", (short)7 },
                    { (short)8, null, true, "Level 8", (short)8 },
                    { (short)9, null, true, "Exempt Micro Enterprise", (short)9 },
                    { (short)10, null, true, "Non-Compliant", (short)10 },
                    { (short)11, null, true, "Not Applicable", (short)11 }
                });

            migrationBuilder.InsertData(
                table: "lookup_company_types",
                columns: new[] { "id", "is_active", "name", "sort_order" },
                values: new object[,]
                {
                    { (short)1, true, "Manufacturer", (short)1 },
                    { (short)2, true, "Farmer", (short)2 },
                    { (short)3, true, "Distributor", (short)3 },
                    { (short)4, true, "Retailer", (short)4 },
                    { (short)5, true, "Packhouse", (short)5 },
                    { (short)6, true, "Financial", (short)6 },
                    { (short)7, true, "Market", (short)7 },
                    { (short)8, true, "Prepared Food", (short)8 },
                    { (short)9, true, "Butcher", (short)9 },
                    { (short)10, true, "Dairy", (short)10 },
                    { (short)11, true, "Broker", (short)11 },
                    { (short)12, true, "Mill", (short)12 },
                    { (short)13, true, "Cold Storage", (short)13 },
                    { (short)14, true, "Packaging", (short)14 },
                    { (short)15, true, "Other", (short)15 }
                });

            migrationBuilder.InsertData(
                table: "lookup_donation_frequencies",
                columns: new[] { "id", "is_active", "name", "sort_order" },
                values: new object[,]
                {
                    { (short)1, true, "Ad Hoc", (short)1 },
                    { (short)2, true, "Once-off", (short)2 },
                    { (short)3, true, "Weekly", (short)3 },
                    { (short)4, true, "Monthly", (short)4 },
                    { (short)5, true, "Seasonal", (short)5 }
                });

            migrationBuilder.InsertData(
                table: "lookup_donation_types",
                columns: new[] { "id", "is_active", "name", "sort_order" },
                values: new object[,]
                {
                    { (short)1, true, "Bakery", (short)1 },
                    { (short)2, true, "Beverages", (short)2 },
                    { (short)3, true, "Dairy", (short)3 },
                    { (short)4, true, "Dry Goods", (short)4 },
                    { (short)5, true, "Financial", (short)5 },
                    { (short)6, true, "Fruit", (short)6 },
                    { (short)7, true, "Meat", (short)7 },
                    { (short)8, true, "Non Food", (short)8 },
                    { (short)9, true, "Prepared Food", (short)9 },
                    { (short)10, true, "Vegetables", (short)10 },
                    { (short)11, true, "Other", (short)11 }
                });

            migrationBuilder.InsertData(
                table: "lookup_entity_types",
                columns: new[] { "id", "is_active", "name", "sort_order" },
                values: new object[,]
                {
                    { (short)1, true, "Private Company", (short)1 },
                    { (short)2, true, "Public Company", (short)2 },
                    { (short)3, true, "Close Corporation", (short)3 },
                    { (short)4, true, "Trust", (short)4 },
                    { (short)5, true, "Non Profit", (short)5 },
                    { (short)6, true, "Association", (short)6 }
                });

            migrationBuilder.InsertData(
                table: "lookup_operational_regions",
                columns: new[] { "id", "code", "is_active", "name", "sort_order" },
                values: new object[,]
                {
                    { (short)1, "JHB", true, "Johannesburg", (short)1 },
                    { (short)2, "CPT", true, "Cape Town", (short)2 },
                    { (short)3, "KZN", true, "KwaZulu-Natal", (short)3 },
                    { (short)4, "EC", true, "Eastern Cape", (short)4 },
                    { (short)5, "BFN", true, "Bloemfontein", (short)5 },
                    { (short)6, "MPU", true, "Mpumalanga", (short)6 },
                    { (short)7, "LIM", true, "Limpopo", (short)7 }
                });

            migrationBuilder.InsertData(
                table: "lookup_provinces",
                columns: new[] { "id", "code", "is_active", "name", "sort_order" },
                values: new object[,]
                {
                    { (short)1, "EC", true, "Eastern Cape", (short)1 },
                    { (short)2, "FS", true, "Free State", (short)2 },
                    { (short)3, "GP", true, "Gauteng", (short)3 },
                    { (short)4, "KZN", true, "KwaZulu-Natal", (short)4 },
                    { (short)5, "LP", true, "Limpopo", (short)5 },
                    { (short)6, "MP", true, "Mpumalanga", (short)6 },
                    { (short)7, "NC", true, "Northern Cape", (short)7 },
                    { (short)8, "NW", true, "North West", (short)8 },
                    { (short)9, "WC", true, "Western Cape", (short)9 }
                });
        }
    }
}
