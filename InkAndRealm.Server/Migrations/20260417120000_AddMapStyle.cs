using InkAndRealm.Server.Data;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace InkAndRealm.Server.Migrations
{
    [DbContext(typeof(DemoMapContext))]
    [Migration("20260417120000_AddMapStyle")]
    public partial class AddMapStyle : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "MapStyle",
                table: "Maps",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "FullColor");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "MapStyle",
                table: "Maps");
        }
    }
}
