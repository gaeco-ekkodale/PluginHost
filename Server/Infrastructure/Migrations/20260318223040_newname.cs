using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PluginHost.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class newname : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "Name",
                table: "Plugins",
                newName: "DisplayName");

            migrationBuilder.AlterColumn<string>(
                name: "Icon",
                table: "Plugins",
                type: "text",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "text");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "DisplayName",
                table: "Plugins",
                newName: "Name");

            migrationBuilder.AlterColumn<string>(
                name: "Icon",
                table: "Plugins",
                type: "text",
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);
        }
    }
}
