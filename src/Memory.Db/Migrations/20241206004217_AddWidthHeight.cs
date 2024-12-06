using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Memory.Db.Migrations
{
    /// <inheritdoc />
    public partial class AddWidthHeight : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "Height",
                table: "MemoryMetas",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "Width",
                table: "MemoryMetas",
                type: "INTEGER",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Height",
                table: "MemoryMetas");

            migrationBuilder.DropColumn(
                name: "Width",
                table: "MemoryMetas");
        }
    }
}
