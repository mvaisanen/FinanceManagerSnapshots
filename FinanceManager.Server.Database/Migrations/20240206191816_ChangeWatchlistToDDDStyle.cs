using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Server.Migrations
{
    /// <inheritdoc />
    public partial class ChangeWatchlistToDDDStyle : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_WatchlistStocks_Stocks_StockId",
                table: "WatchlistStocks");

            migrationBuilder.DropForeignKey(
                name: "FK_WatchlistStocks_Watchlists_WatchlistId",
                table: "WatchlistStocks");

            migrationBuilder.DropPrimaryKey(
                name: "PK_WatchlistStocks",
                table: "WatchlistStocks");

            migrationBuilder.RenameTable(
                name: "WatchlistStocks",
                newName: "WatchlistStock");

            migrationBuilder.RenameIndex(
                name: "IX_WatchlistStocks_WatchlistId",
                table: "WatchlistStock",
                newName: "IX_WatchlistStock_WatchlistId");

            migrationBuilder.RenameIndex(
                name: "IX_WatchlistStocks_StockId",
                table: "WatchlistStock",
                newName: "IX_WatchlistStock_StockId");

            migrationBuilder.AddPrimaryKey(
                name: "PK_WatchlistStock",
                table: "WatchlistStock",
                column: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_WatchlistStock_Stocks_StockId",
                table: "WatchlistStock",
                column: "StockId",
                principalTable: "Stocks",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_WatchlistStock_Watchlists_WatchlistId",
                table: "WatchlistStock",
                column: "WatchlistId",
                principalTable: "Watchlists",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_WatchlistStock_Stocks_StockId",
                table: "WatchlistStock");

            migrationBuilder.DropForeignKey(
                name: "FK_WatchlistStock_Watchlists_WatchlistId",
                table: "WatchlistStock");

            migrationBuilder.DropPrimaryKey(
                name: "PK_WatchlistStock",
                table: "WatchlistStock");

            migrationBuilder.RenameTable(
                name: "WatchlistStock",
                newName: "WatchlistStocks");

            migrationBuilder.RenameIndex(
                name: "IX_WatchlistStock_WatchlistId",
                table: "WatchlistStocks",
                newName: "IX_WatchlistStocks_WatchlistId");

            migrationBuilder.RenameIndex(
                name: "IX_WatchlistStock_StockId",
                table: "WatchlistStocks",
                newName: "IX_WatchlistStocks_StockId");

            migrationBuilder.AddPrimaryKey(
                name: "PK_WatchlistStocks",
                table: "WatchlistStocks",
                column: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_WatchlistStocks_Stocks_StockId",
                table: "WatchlistStocks",
                column: "StockId",
                principalTable: "Stocks",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_WatchlistStocks_Watchlists_WatchlistId",
                table: "WatchlistStocks",
                column: "WatchlistId",
                principalTable: "Watchlists",
                principalColumn: "Id");
        }
    }
}
