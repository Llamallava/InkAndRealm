using InkAndRealm.Server.Data;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace InkAndRealm.Server.Migrations
{
    [DbContext(typeof(AppDbContext))]
    [Migration("20260203120000_AddTitleTargetFeatureId")]
    public partial class AddTitleTargetFeatureId : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                IF COL_LENGTH('Features', 'TargetFeatureId') IS NULL
                BEGIN
                    ALTER TABLE [Features] ADD [TargetFeatureId] int NULL;
                END
                """);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                IF COL_LENGTH('Features', 'TargetFeatureId') IS NOT NULL
                BEGIN
                    ALTER TABLE [Features] DROP COLUMN [TargetFeatureId];
                END
                """);
        }
    }
}
