using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Server.Migrations
{
    /// <inheritdoc />
    public partial class FixStockPurchaseCascadeDelete : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_StockPurchase_PortfolioPosition_PortfolioPositionId",
                table: "StockPurchase");

            migrationBuilder.RenameColumn(
                name: "Id",
                table: "StockPurchase",
                newName: "StockPurchaseId");

            migrationBuilder.RenameColumn(
                name: "Id",
                table: "PortfolioPosition",
                newName: "PortfolioPositionId");

            /*migrationBuilder.AlterColumn<byte[]>(
                name: "RowVersion",
                table: "StockPurchase",
                type: "rowversion",
                rowVersion: true,
                nullable: false,
                defaultValue: new byte[0],
                oldClrType: typeof(byte[]),
                oldType: "rowversion",
                oldRowVersion: true,
                oldNullable: true);*/
            migrationBuilder.DropColumn("RowVersion", "StockPurchase");
            migrationBuilder.AddColumn<byte[]>(
                name: "RowVersion",
                table: "StockPurchase",
                type: "rowversion",
                rowVersion: true,
                nullable: false);

            migrationBuilder.AlterColumn<int>(
                name: "PortfolioPositionId",
                table: "StockPurchase",
                type: "int",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "int",
                oldNullable: true);

            migrationBuilder.AddForeignKey(
                name: "FK_StockPurchase_PortfolioPosition_PortfolioPositionId",
                table: "StockPurchase",
                column: "PortfolioPositionId",
                principalTable: "PortfolioPosition",
                principalColumn: "PortfolioPositionId",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_StockPurchase_PortfolioPosition_PortfolioPositionId",
                table: "StockPurchase");

            migrationBuilder.RenameColumn(
                name: "StockPurchaseId",
                table: "StockPurchase",
                newName: "Id");

            migrationBuilder.RenameColumn(
                name: "PortfolioPositionId",
                table: "PortfolioPosition",
                newName: "Id");

            /*migrationBuilder.AlterColumn<byte[]>(
                name: "RowVersion",
                table: "StockPurchase",
                type: "rowversion",
                rowVersion: true,
                nullable: true,
                oldClrType: typeof(byte[]),
                oldType: "rowversion",
                oldRowVersion: true);*/
            migrationBuilder.DropColumn("RowVersion", "StockPurchase");
            migrationBuilder.AddColumn<byte[]>(
                name: "RowVersion",
                table: "StockPurchase",
                type: "rowversion",
                rowVersion: true,
                nullable: true);

            migrationBuilder.AlterColumn<int>(
                name: "PortfolioPositionId",
                table: "StockPurchase",
                type: "int",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "int");

            migrationBuilder.AddForeignKey(
                name: "FK_StockPurchase_PortfolioPosition_PortfolioPositionId",
                table: "StockPurchase",
                column: "PortfolioPositionId",
                principalTable: "PortfolioPosition",
                principalColumn: "Id");
        }
    }
}
