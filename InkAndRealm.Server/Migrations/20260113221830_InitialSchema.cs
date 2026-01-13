using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace InkAndRealm.Server.Migrations
{
    /// <inheritdoc />
    public partial class InitialSchema : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Users",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Username = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    UsernameNormalized = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    PasswordHash = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Users", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Maps",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UserId = table.Column<int>(type: "int", nullable: true),
                    Name = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Width = table.Column<float>(type: "real", nullable: false),
                    Height = table.Column<float>(type: "real", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Maps", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Maps_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "Features",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    MapId = table.Column<int>(type: "int", nullable: false),
                    AllowPointPlacement = table.Column<bool>(type: "bit", nullable: false),
                    ZIndex = table.Column<int>(type: "int", nullable: false),
                    FeatureType = table.Column<string>(type: "nvarchar(13)", maxLength: 13, nullable: false),
                    BridgeType = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CharacterType = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Name = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Background = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Occupation = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Personality = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    HouseType = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    LandType = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ElevationType = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    TitleFeatureEntity_Name = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Description = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    TownType = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    TreeType = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    WaterType = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Features", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Features_Maps_MapId",
                        column: x => x.MapId,
                        principalTable: "Maps",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "FeaturePoints",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    FeatureId = table.Column<int>(type: "int", nullable: false),
                    X = table.Column<float>(type: "real", nullable: false),
                    Y = table.Column<float>(type: "real", nullable: false),
                    SortOrder = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FeaturePoints", x => x.Id);
                    table.ForeignKey(
                        name: "FK_FeaturePoints_Features_FeatureId",
                        column: x => x.FeatureId,
                        principalTable: "Features",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "FeatureRelationships",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    SourceCharacterId = table.Column<int>(type: "int", nullable: false),
                    TargetFeatureId = table.Column<int>(type: "int", nullable: false),
                    RelationshipType = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Description = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FeatureRelationships", x => x.Id);
                    table.ForeignKey(
                        name: "FK_FeatureRelationships_Features_SourceCharacterId",
                        column: x => x.SourceCharacterId,
                        principalTable: "Features",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_FeatureRelationships_Features_TargetFeatureId",
                        column: x => x.TargetFeatureId,
                        principalTable: "Features",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "TownStructures",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    TownFeatureId = table.Column<int>(type: "int", nullable: false),
                    TownStructureType = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    RelativeX = table.Column<float>(type: "real", nullable: false),
                    RelativeY = table.Column<float>(type: "real", nullable: false),
                    TextureKey = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TownStructures", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TownStructures_Features_TownFeatureId",
                        column: x => x.TownFeatureId,
                        principalTable: "Features",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_FeaturePoints_FeatureId",
                table: "FeaturePoints",
                column: "FeatureId");

            migrationBuilder.CreateIndex(
                name: "IX_FeatureRelationships_SourceCharacterId",
                table: "FeatureRelationships",
                column: "SourceCharacterId");

            migrationBuilder.CreateIndex(
                name: "IX_FeatureRelationships_TargetFeatureId",
                table: "FeatureRelationships",
                column: "TargetFeatureId");

            migrationBuilder.CreateIndex(
                name: "IX_Features_MapId",
                table: "Features",
                column: "MapId");

            migrationBuilder.CreateIndex(
                name: "IX_Maps_UserId",
                table: "Maps",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_TownStructures_TownFeatureId",
                table: "TownStructures",
                column: "TownFeatureId");

            migrationBuilder.CreateIndex(
                name: "IX_Users_UsernameNormalized",
                table: "Users",
                column: "UsernameNormalized",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "FeaturePoints");

            migrationBuilder.DropTable(
                name: "FeatureRelationships");

            migrationBuilder.DropTable(
                name: "TownStructures");

            migrationBuilder.DropTable(
                name: "Features");

            migrationBuilder.DropTable(
                name: "Maps");

            migrationBuilder.DropTable(
                name: "Users");
        }
    }
}
