using System;
using Microsoft.EntityFrameworkCore.Migrations;

namespace Server.Migrations
{
    public partial class AddSSDScores : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "SSDSafetyScoreId",
                table: "Stocks",
                type: "int",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "SSDScore",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    DivSafetyScore = table.Column<int>(type: "int", nullable: false),
                    ScoreDate = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SSDScore", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Stocks_SSDSafetyScoreId",
                table: "Stocks",
                column: "SSDSafetyScoreId");

            migrationBuilder.AddForeignKey(
                name: "FK_Stocks_SSDScore_SSDSafetyScoreId",
                table: "Stocks",
                column: "SSDSafetyScoreId",
                principalTable: "SSDScore",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Stocks_SSDScore_SSDSafetyScoreId",
                table: "Stocks");

            migrationBuilder.DropTable(
                name: "SSDScore");

            migrationBuilder.DropIndex(
                name: "IX_Stocks_SSDSafetyScoreId",
                table: "Stocks");

            migrationBuilder.DropColumn(
                name: "SSDSafetyScoreId",
                table: "Stocks");
        }
    }
}
