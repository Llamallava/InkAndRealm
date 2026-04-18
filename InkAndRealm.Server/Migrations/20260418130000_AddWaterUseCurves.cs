using InkAndRealm.Server.Data;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace InkAndRealm.Server.Migrations
{
    [DbContext(typeof(DemoMapContext))]
    [Migration("20260418130000_AddWaterUseCurves")]
    public partial class AddWaterUseCurves : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "UseCurves",
                table: "Features",
                type: "bit",
                nullable: true,
                defaultValue: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "UseCurves",
                table: "Features");
        }
    }
}
