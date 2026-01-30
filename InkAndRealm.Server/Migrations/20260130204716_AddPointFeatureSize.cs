using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace InkAndRealm.Server.Migrations
{
    /// <inheritdoc />
    public partial class AddPointFeatureSize : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<float>(
                name: "Size",
                table: "Features",
                type: "real",
                nullable: true);

            migrationBuilder.AddColumn<float>(
                name: "TreeFeatureEntity_Size",
                table: "Features",
                type: "real",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Size",
                table: "Features");

            migrationBuilder.DropColumn(
                name: "TreeFeatureEntity_Size",
                table: "Features");
        }
    }
}
