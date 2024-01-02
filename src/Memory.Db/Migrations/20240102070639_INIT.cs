using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Memory.Db.Migrations
{
    /// <inheritdoc />
    public partial class INIT : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Memories",
                columns: table => new
                {
                    Id = table.Column<long>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    FilePath = table.Column<string>(type: "TEXT", maxLength: 300, nullable: false),
                    FilePathHash = table.Column<string>(type: "TEXT", maxLength: 32, nullable: false),
                    FileExtension = table.Column<string>(type: "TEXT", nullable: false),
                    FileSize = table.Column<long>(type: "INTEGER", nullable: false),
                    FileContentHash = table.Column<string>(type: "TEXT", nullable: true),
                    CreationTime = table.Column<DateTime>(type: "TEXT", nullable: false),
                    Year = table.Column<int>(type: "INTEGER", nullable: false, computedColumnSql: "CAST(strftime('%Y', CreationTime) AS INTEGER)"),
                    Month = table.Column<int>(type: "INTEGER", nullable: false, computedColumnSql: "CAST(strftime('%m', CreationTime) AS INTEGER)"),
                    Day = table.Column<int>(type: "INTEGER", nullable: false, computedColumnSql: "CAST(strftime('%d', CreationTime) AS INTEGER)"),
                    Likes = table.Column<int>(type: "INTEGER", nullable: false),
                    Views = table.Column<int>(type: "INTEGER", nullable: false),
                    IsTaggedByFace = table.Column<bool>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Memories", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Tags",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Name = table.Column<string>(type: "TEXT", maxLength: 20, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Tags", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Users",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Name = table.Column<string>(type: "TEXT", nullable: false),
                    Password = table.Column<string>(type: "TEXT", nullable: false),
                    LockoutRetryCount = table.Column<int>(type: "INTEGER", nullable: false),
                    LockoutTime = table.Column<DateTime>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Users", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "MemoryMetas",
                columns: table => new
                {
                    Id = table.Column<long>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    MemoryId = table.Column<long>(type: "INTEGER", nullable: false),
                    DateTimeOriginal = table.Column<DateTime>(type: "TEXT", nullable: true),
                    OffsetTimeOriginal = table.Column<string>(type: "TEXT", nullable: true),
                    Make = table.Column<string>(type: "TEXT", nullable: true),
                    Modal = table.Column<string>(type: "TEXT", nullable: true),
                    LensModal = table.Column<string>(type: "TEXT", nullable: true),
                    Latitude = table.Column<double>(type: "REAL", nullable: true),
                    Longitude = table.Column<double>(type: "REAL", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MemoryMetas", x => x.Id);
                    table.ForeignKey(
                        name: "FK_MemoryMetas_Memories_MemoryId",
                        column: x => x.MemoryId,
                        principalTable: "Memories",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "MemoryTags",
                columns: table => new
                {
                    TagId = table.Column<int>(type: "INTEGER", nullable: false),
                    MemoryId = table.Column<long>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MemoryTags", x => new { x.TagId, x.MemoryId });
                    table.ForeignKey(
                        name: "FK_MemoryTags_Memories_MemoryId",
                        column: x => x.MemoryId,
                        principalTable: "Memories",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_MemoryTags_Tags_TagId",
                        column: x => x.TagId,
                        principalTable: "Tags",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Memories_CreationTime",
                table: "Memories",
                column: "CreationTime");

            migrationBuilder.CreateIndex(
                name: "IX_Memories_Day",
                table: "Memories",
                column: "Day");

            migrationBuilder.CreateIndex(
                name: "IX_Memories_FileContentHash",
                table: "Memories",
                column: "FileContentHash");

            migrationBuilder.CreateIndex(
                name: "IX_Memories_FilePathHash",
                table: "Memories",
                column: "FilePathHash");

            migrationBuilder.CreateIndex(
                name: "IX_Memories_Month",
                table: "Memories",
                column: "Month");

            migrationBuilder.CreateIndex(
                name: "IX_Memories_Year",
                table: "Memories",
                column: "Year");

            migrationBuilder.CreateIndex(
                name: "IX_MemoryMetas_MemoryId",
                table: "MemoryMetas",
                column: "MemoryId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_MemoryTags_MemoryId",
                table: "MemoryTags",
                column: "MemoryId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "MemoryMetas");

            migrationBuilder.DropTable(
                name: "MemoryTags");

            migrationBuilder.DropTable(
                name: "Users");

            migrationBuilder.DropTable(
                name: "Memories");

            migrationBuilder.DropTable(
                name: "Tags");
        }
    }
}
