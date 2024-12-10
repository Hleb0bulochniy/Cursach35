using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MS_Back_Maps.Migrations
{
    /// <inheritdoc />
    public partial class First_migration : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "CustomMaps",
                columns: table => new
                {
                    id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    mapName = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    bombCount = table.Column<int>(type: "int", nullable: false),
                    mapSize = table.Column<int>(type: "int", nullable: false),
                    mapType = table.Column<int>(type: "int", nullable: false),
                    creatorId = table.Column<int>(type: "int", nullable: false),
                    creationDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ratingSum = table.Column<int>(type: "int", nullable: false),
                    ratingCount = table.Column<int>(type: "int", nullable: false),
                    downloads = table.Column<int>(type: "int", nullable: false),
                    about = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CustomMaps", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "Maps",
                columns: table => new
                {
                    id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    mapName = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    bombCount = table.Column<int>(type: "int", nullable: false),
                    mapSize = table.Column<int>(type: "int", nullable: false),
                    mapType = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Maps", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "CustomMapsInUsers",
                columns: table => new
                {
                    id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    userID = table.Column<int>(type: "int", nullable: false),
                    customMapID = table.Column<int>(type: "int", nullable: false),
                    gamesSum = table.Column<int>(type: "int", nullable: false),
                    wins = table.Column<int>(type: "int", nullable: false),
                    loses = table.Column<int>(type: "int", nullable: false),
                    openedTiles = table.Column<int>(type: "int", nullable: false),
                    openedNumberTiles = table.Column<int>(type: "int", nullable: false),
                    openedBlankTiles = table.Column<int>(type: "int", nullable: false),
                    flagsSum = table.Column<int>(type: "int", nullable: false),
                    flagsOnBombs = table.Column<int>(type: "int", nullable: false),
                    timeSpentSum = table.Column<int>(type: "int", nullable: false),
                    averageTime = table.Column<int>(type: "int", nullable: false),
                    lastGameData = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    lastGameTime = table.Column<int>(type: "int", nullable: false),
                    isAdded = table.Column<bool>(type: "bit", nullable: false),
                    isFavourite = table.Column<bool>(type: "bit", nullable: false),
                    rate = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CustomMapsInUsers", x => x.id);
                    table.ForeignKey(
                        name: "FK_CustomMapsInUsers_CustomMaps_customMapID",
                        column: x => x.customMapID,
                        principalTable: "CustomMaps",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "MapsInUsers",
                columns: table => new
                {
                    id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    userID = table.Column<int>(type: "int", nullable: false),
                    mapID = table.Column<int>(type: "int", nullable: false),
                    gamesSum = table.Column<int>(type: "int", nullable: false),
                    wins = table.Column<int>(type: "int", nullable: false),
                    loses = table.Column<int>(type: "int", nullable: false),
                    openedTiles = table.Column<int>(type: "int", nullable: false),
                    openedNumberTiles = table.Column<int>(type: "int", nullable: false),
                    openedBlankTiles = table.Column<int>(type: "int", nullable: false),
                    flagsSum = table.Column<int>(type: "int", nullable: false),
                    flagsOnBombs = table.Column<int>(type: "int", nullable: false),
                    timeSpentSum = table.Column<int>(type: "int", nullable: false),
                    averageTime = table.Column<int>(type: "int", nullable: false),
                    lastGameData = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    lastGameTime = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MapsInUsers", x => x.id);
                    table.ForeignKey(
                        name: "FK_MapsInUsers_Maps_mapID",
                        column: x => x.mapID,
                        principalTable: "Maps",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_CustomMapsInUsers_customMapID",
                table: "CustomMapsInUsers",
                column: "customMapID");

            migrationBuilder.CreateIndex(
                name: "IX_CustomMapsInUsers_userID_customMapID",
                table: "CustomMapsInUsers",
                columns: new[] { "userID", "customMapID" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_MapsInUsers_mapID",
                table: "MapsInUsers",
                column: "mapID");

            migrationBuilder.CreateIndex(
                name: "IX_MapsInUsers_userID_mapID",
                table: "MapsInUsers",
                columns: new[] { "userID", "mapID" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CustomMapsInUsers");

            migrationBuilder.DropTable(
                name: "MapsInUsers");

            migrationBuilder.DropTable(
                name: "CustomMaps");

            migrationBuilder.DropTable(
                name: "Maps");
        }
    }
}
