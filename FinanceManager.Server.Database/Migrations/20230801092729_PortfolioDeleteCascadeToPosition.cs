using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Server.Migrations
{
    /// <inheritdoc />
    public partial class PortfolioDeleteCascadeToPosition : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_PortfolioPosition_Portfolios_PortfolioId",
                table: "PortfolioPosition");

            migrationBuilder.AlterColumn<int>(
                name: "PortfolioId",
                table: "PortfolioPosition",
                type: "int",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "int",
                oldNullable: true);

            migrationBuilder.AddForeignKey(
                name: "FK_PortfolioPosition_Portfolios_PortfolioId",
                table: "PortfolioPosition",
                column: "PortfolioId",
                principalTable: "Portfolios",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_PortfolioPosition_Portfolios_PortfolioId",
                table: "PortfolioPosition");

            migrationBuilder.AlterColumn<int>(
                name: "PortfolioId",
                table: "PortfolioPosition",
                type: "int",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "int");

            migrationBuilder.AddForeignKey(
                name: "FK_PortfolioPosition_Portfolios_PortfolioId",
                table: "PortfolioPosition",
                column: "PortfolioId",
                principalTable: "Portfolios",
                principalColumn: "Id");
        }
    }
}
