using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace KinectCare.API.Migrations
{
    /// <inheritdoc />
    public partial class AddPlanFilePath : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "PlanFilePath",
                table: "RehabilitationPlans",
                type: "nvarchar(max)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "PlanFilePath",
                table: "RehabilitationPlans");
        }
    }
}
