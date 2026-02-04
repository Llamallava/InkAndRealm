using InkAndRealm.Server.Data;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace InkAndRealm.Server.Migrations
{
    [DbContext(typeof(DemoMapContext))]
    [Migration("20260204120000_AddTitleFeatureSize")]
    public partial class AddTitleFeatureSize : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                IF COL_LENGTH('Features', 'TitleFeatureEntity_Size') IS NULL
                BEGIN
                    ALTER TABLE [Features] ADD [TitleFeatureEntity_Size] real NULL;
                END
                """);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                IF COL_LENGTH('Features', 'TitleFeatureEntity_Size') IS NOT NULL
                BEGIN
                    ALTER TABLE [Features] DROP COLUMN [TitleFeatureEntity_Size];
                END
                """);
        }
    }
}
