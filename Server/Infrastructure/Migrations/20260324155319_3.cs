using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PluginHost.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class _3 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Plugins_PluginGroups_GroupId",
                table: "Plugins");

            migrationBuilder.RenameColumn(
                name: "Module",
                table: "Plugins",
                newName: "ExposedModule");

            migrationBuilder.RenameColumn(
                name: "Icon",
                table: "Plugins",
                newName: "IconPath");

            migrationBuilder.RenameColumn(
                name: "EntryPoint",
                table: "Plugins",
                newName: "EntrypointPath");

            migrationBuilder.RenameColumn(
                name: "PluginId",
                table: "Plugins",
                newName: "Id");

            migrationBuilder.AlterColumn<Guid>(
                name: "GroupId",
                table: "Plugins",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"),
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldNullable: true);

            migrationBuilder.AddForeignKey(
                name: "FK_Plugins_PluginGroups_GroupId",
                table: "Plugins",
                column: "GroupId",
                principalTable: "PluginGroups",
                principalColumn: "GroupId",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Plugins_PluginGroups_GroupId",
                table: "Plugins");

            migrationBuilder.RenameColumn(
                name: "IconPath",
                table: "Plugins",
                newName: "Icon");

            migrationBuilder.RenameColumn(
                name: "ExposedModule",
                table: "Plugins",
                newName: "Module");

            migrationBuilder.RenameColumn(
                name: "EntrypointPath",
                table: "Plugins",
                newName: "EntryPoint");

            migrationBuilder.RenameColumn(
                name: "Id",
                table: "Plugins",
                newName: "PluginId");

            migrationBuilder.AlterColumn<Guid>(
                name: "GroupId",
                table: "Plugins",
                type: "uuid",
                nullable: true,
                oldClrType: typeof(Guid),
                oldType: "uuid");

            migrationBuilder.AddForeignKey(
                name: "FK_Plugins_PluginGroups_GroupId",
                table: "Plugins",
                column: "GroupId",
                principalTable: "PluginGroups",
                principalColumn: "GroupId",
                onDelete: ReferentialAction.SetNull);
        }
    }
}
