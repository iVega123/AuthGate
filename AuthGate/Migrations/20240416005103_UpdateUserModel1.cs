using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace AuthGate.Migrations
{
    /// <inheritdoc />
    public partial class UpdateUserModel1 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "AspNetRoles",
                keyColumn: "Id",
                keyValue: "334854c6-1829-408f-ab02-cb9629c002ca");

            migrationBuilder.DeleteData(
                table: "AspNetRoles",
                keyColumn: "Id",
                keyValue: "60d9ada2-226d-4909-8d58-ed766c46c782");

            migrationBuilder.AddColumn<string>(
                name: "UserType",
                table: "AspNetUsers",
                type: "text",
                nullable: true);

            migrationBuilder.InsertData(
                table: "AspNetRoles",
                columns: new[] { "Id", "ConcurrencyStamp", "Name", "NormalizedName" },
                values: new object[,]
                {
                    { "4ecccc42-91c0-47f4-9ac9-9728dffc854f", "2", "Rider", "RIDER" },
                    { "c85cbd12-b40e-40e0-86d3-b756b7766dcb", "1", "Admin", "ADMIN" }
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "AspNetRoles",
                keyColumn: "Id",
                keyValue: "4ecccc42-91c0-47f4-9ac9-9728dffc854f");

            migrationBuilder.DeleteData(
                table: "AspNetRoles",
                keyColumn: "Id",
                keyValue: "c85cbd12-b40e-40e0-86d3-b756b7766dcb");

            migrationBuilder.DropColumn(
                name: "UserType",
                table: "AspNetUsers");

            migrationBuilder.InsertData(
                table: "AspNetRoles",
                columns: new[] { "Id", "ConcurrencyStamp", "Name", "NormalizedName" },
                values: new object[,]
                {
                    { "334854c6-1829-408f-ab02-cb9629c002ca", "1", "Admin", "Admin" },
                    { "60d9ada2-226d-4909-8d58-ed766c46c782", "2", "User", "User" }
                });
        }
    }
}
