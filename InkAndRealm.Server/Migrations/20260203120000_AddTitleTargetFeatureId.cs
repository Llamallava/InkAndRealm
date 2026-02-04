using InkAndRealm.Server.Data;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace InkAndRealm.Server.Migrations
{
    [DbContext(typeof(DemoMapContext))]
    [Migration("20260203120000_AddTitleTargetFeatureId")]
    public partial class AddTitleTargetFeatureId : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "TargetFeatureId",
                table: "Features",
                type: "int",
                nullable: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "TargetFeatureId",
                table: "Features");
        }
    }
}
