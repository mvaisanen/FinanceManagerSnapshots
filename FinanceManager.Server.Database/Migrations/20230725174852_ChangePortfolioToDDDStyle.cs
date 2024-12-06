using Financemanager.Server.Database.Domain;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Server.Migrations
{
    /// <inheritdoc />
    public partial class ChangePortfolioToDDDStyle : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_PortfolioPositions_Portfolios_PortfolioId",
                table: "PortfolioPositions");

            migrationBuilder.DropForeignKey(
                name: "FK_PortfolioPositions_Stocks_StockId",
                table: "PortfolioPositions");

            migrationBuilder.DropForeignKey(
                name: "FK_StockPurchases_PortfolioPositions_PortfolioPositionId",
                table: "StockPurchases");

            migrationBuilder.DropPrimaryKey(
                name: "PK_StockPurchases",
                table: "StockPurchases");

            migrationBuilder.DropPrimaryKey(
                name: "PK_PortfolioPositions",
                table: "PortfolioPositions");

            migrationBuilder.RenameTable(
                name: "StockPurchases",
                newName: "StockPurchase");

            migrationBuilder.RenameTable(
                name: "PortfolioPositions",
                newName: "PortfolioPosition");

            migrationBuilder.RenameIndex(
                name: "IX_StockPurchases_PortfolioPositionId",
                table: "StockPurchase",
                newName: "IX_StockPurchase_PortfolioPositionId");

            migrationBuilder.RenameIndex(
                name: "IX_PortfolioPositions_StockId",
                table: "PortfolioPosition",
                newName: "IX_PortfolioPosition_StockId");

            migrationBuilder.RenameIndex(
                name: "IX_PortfolioPositions_PortfolioId",
                table: "PortfolioPosition",
                newName: "IX_PortfolioPosition_PortfolioId");

            migrationBuilder.AlterColumn<string>(
                name: "UserId",
                table: "Portfolios",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "nvarchar(max)",
                oldNullable: true);

            /*migrationBuilder.AlterColumn<byte[]>(
                name: "RowVersion",
                table: "Portfolios",
                type: "rowversion",
                rowVersion: true,
                nullable: false,
                defaultValue: new byte[0],
                oldClrType: typeof(byte[]),
                oldType: "rowversion",
                oldRowVersion: true,
                oldNullable: true);*/
            migrationBuilder.DropColumn("RowVersion", "Portfolios");
            migrationBuilder.AddColumn<byte[]>(
                name: "RowVersion",
                table: "Portfolios",
                type: "rowversion",
                rowVersion: true,
                nullable: false);

            migrationBuilder.AlterColumn<int>(
                name: "StockId",
                table: "PortfolioPosition",
                type: "int",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "int",
                oldNullable: true);

            /*migrationBuilder.AlterColumn<byte[]>(
                name: "RowVersion",
                table: "PortfolioPosition",
                type: "rowversion",
                rowVersion: true,
                nullable: false,
                defaultValue: new byte[0],
                oldClrType: typeof(byte[]),
                oldType: "rowversion",
                oldRowVersion: true,
                oldNullable: true);*/
            migrationBuilder.DropColumn("RowVersion", "PortfolioPosition");
            migrationBuilder.AddColumn<byte[]>(
                name: "RowVersion",
                table: "PortfolioPosition",
                type: "rowversion",
                rowVersion: true,
                nullable: false);

            migrationBuilder.AddPrimaryKey(
                name: "PK_StockPurchase",
                table: "StockPurchase",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_PortfolioPosition",
                table: "PortfolioPosition",
                column: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_PortfolioPosition_Portfolios_PortfolioId",
                table: "PortfolioPosition",
                column: "PortfolioId",
                principalTable: "Portfolios",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_PortfolioPosition_Stocks_StockId",
                table: "PortfolioPosition",
                column: "StockId",
                principalTable: "Stocks",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_StockPurchase_PortfolioPosition_PortfolioPositionId",
                table: "StockPurchase",
                column: "PortfolioPositionId",
                principalTable: "PortfolioPosition",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_PortfolioPosition_Portfolios_PortfolioId",
                table: "PortfolioPosition");

            migrationBuilder.DropForeignKey(
                name: "FK_PortfolioPosition_Stocks_StockId",
                table: "PortfolioPosition");

            migrationBuilder.DropForeignKey(
                name: "FK_StockPurchase_PortfolioPosition_PortfolioPositionId",
                table: "StockPurchase");

            migrationBuilder.DropPrimaryKey(
                name: "PK_StockPurchase",
                table: "StockPurchase");

            migrationBuilder.DropPrimaryKey(
                name: "PK_PortfolioPosition",
                table: "PortfolioPosition");

            migrationBuilder.RenameTable(
                name: "StockPurchase",
                newName: "StockPurchases");

            migrationBuilder.RenameTable(
                name: "PortfolioPosition",
                newName: "PortfolioPositions");

            migrationBuilder.RenameIndex(
                name: "IX_StockPurchase_PortfolioPositionId",
                table: "StockPurchases",
                newName: "IX_StockPurchases_PortfolioPositionId");

            migrationBuilder.RenameIndex(
                name: "IX_PortfolioPosition_StockId",
                table: "PortfolioPositions",
                newName: "IX_PortfolioPositions_StockId");

            migrationBuilder.RenameIndex(
                name: "IX_PortfolioPosition_PortfolioId",
                table: "PortfolioPositions",
                newName: "IX_PortfolioPositions_PortfolioId");

            migrationBuilder.AlterColumn<string>(
                name: "UserId",
                table: "Portfolios",
                type: "nvarchar(max)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

            /*migrationBuilder.AlterColumn<byte[]>(
                name: "RowVersion",
                table: "Portfolios",
                type: "rowversion",
                rowVersion: true,
                nullable: true,
                oldClrType: typeof(byte[]),
                oldType: "rowversion",
                oldRowVersion: true);*/
            migrationBuilder.DropColumn("RowVersion", "Portfolios");
            migrationBuilder.AddColumn<byte[]>(
                name: "RowVersion",
                table: "Portfolios",
                type: "rowversion",
                rowVersion: true,
                nullable: true);

            migrationBuilder.AlterColumn<int>(
                name: "StockId",
                table: "PortfolioPositions",
                type: "int",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "int");

            /*migrationBuilder.AlterColumn<byte[]>(
                name: "RowVersion",
                table: "PortfolioPositions",
                type: "rowversion",
                rowVersion: true,
                nullable: true,
                oldClrType: typeof(byte[]),
                oldType: "rowversion",
                oldRowVersion: true);*/
            migrationBuilder.DropColumn("RowVersion", "PortfolioPositions");
            migrationBuilder.AddColumn<byte[]>(
                name: "RowVersion",
                table: "PortfolioPositions",
                type: "rowversion",
                rowVersion: true,
                nullable: true);

            migrationBuilder.AddPrimaryKey(
                name: "PK_StockPurchases",
                table: "StockPurchases",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_PortfolioPositions",
                table: "PortfolioPositions",
                column: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_PortfolioPositions_Portfolios_PortfolioId",
                table: "PortfolioPositions",
                column: "PortfolioId",
                principalTable: "Portfolios",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_PortfolioPositions_Stocks_StockId",
                table: "PortfolioPositions",
                column: "StockId",
                principalTable: "Stocks",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_StockPurchases_PortfolioPositions_PortfolioPositionId",
                table: "StockPurchases",
                column: "PortfolioPositionId",
                principalTable: "PortfolioPositions",
                principalColumn: "Id");
        }
    }
}
