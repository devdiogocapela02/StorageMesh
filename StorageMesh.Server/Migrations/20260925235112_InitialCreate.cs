using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace StorageMesh.Server.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "LocalFiles",
                columns: table => new
                {
                    FileKey = table.Column<string>(type: "TEXT", nullable: false),
                    StoredAt = table.Column<string>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LocalFiles", x => x.FileKey);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "LocalFiles");
        }
    }
}
