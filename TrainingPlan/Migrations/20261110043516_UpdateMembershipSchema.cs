using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TrainingPlan.Migrations
{
    /// <inheritdoc />
    public partial class UpdateMembershipSchema : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IsActive",
                table: "Memberships");

            migrationBuilder.DropColumn(
            name: "MemberName",
            table: "Memberships");

            migrationBuilder.AddColumn<string>(
                name: "Status",
                table: "Memberships",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "Unknown");


            migrationBuilder.RenameColumn(
                name: "ExpirationDate",
                table: "Memberships",
                newName: "RenewalDueDate");

            migrationBuilder.RenameColumn(
                name: "MembershipId",
                table: "Memberships",
                newName: "MemberId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Status",
                table: "Memberships");

            migrationBuilder.AddColumn<string>(
                name: "MemberName",
                table: "Memberships",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");


            migrationBuilder.RenameColumn(
                name: "RenewalDueDate",
                table: "Memberships",
                newName: "ExpirationDate");

            migrationBuilder.RenameColumn(
                name: "MemberId",
                table: "Memberships",
                newName: "MembershipId");

            migrationBuilder.AddColumn<bool>(
                name: "IsActive",
                table: "Memberships",
                type: "bit",
                nullable: false,
                defaultValue: false);
        }
    }
}
