using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Server.Migrations
{
    /// <inheritdoc />
    public partial class ChangeStockToDDDStyle : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_StockDataUpdateSources_Stocks_StockId",
                table: "StockDataUpdateSources");

            migrationBuilder.DropPrimaryKey(
                name: "PK_StockDataUpdateSources",
                table: "StockDataUpdateSources");

            migrationBuilder.DropColumn(
                name: "DataLastUpdated",
                table: "Stocks");

            migrationBuilder.DropColumn(
                name: "DataUpdateSource",
                table: "Stocks");

            migrationBuilder.DropColumn(
                name: "PriceLastUpdated",
                table: "Stocks");

            migrationBuilder.DropColumn(
                name: "PriceSource",
                table: "Stocks");

            migrationBuilder.RenameTable(
                name: "StockDataUpdateSources",
                newName: "StockDataUpdateSource");

            migrationBuilder.RenameIndex(
                name: "IX_StockDataUpdateSources_StockId",
                table: "StockDataUpdateSource",
                newName: "IX_StockDataUpdateSource_StockId");

            migrationBuilder.AlterColumn<string>(
                name: "Ticker",
                table: "Stocks",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "nvarchar(max)",
                oldNullable: true);

            /*migrationBuilder.AlterColumn<byte[]>(
                name: "RowVersion",
                table: "Stocks",
                type: "rowversion",
                rowVersion: true,
                nullable: false,
                defaultValue: new byte[0],
                oldClrType: typeof(byte[]),
                oldType: "rowversion",
                oldRowVersion: true,
                oldNullable: true);*/
            migrationBuilder.DropColumn("RowVersion", "Stocks");
            migrationBuilder.AddColumn<byte[]>(
                name: "RowVersion",
                table: "Stocks",
                type: "rowversion",
                rowVersion: true,
                nullable: false);

            migrationBuilder.AlterColumn<string>(
                name: "Name",
                table: "Stocks",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "nvarchar(max)",
                oldNullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "CccStock",
                table: "Stocks",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddPrimaryKey(
                name: "PK_StockDataUpdateSource",
                table: "StockDataUpdateSource",
                column: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_StockDataUpdateSource_Stocks_StockId",
                table: "StockDataUpdateSource",
                column: "StockId",
                principalTable: "Stocks",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_StockDataUpdateSource_Stocks_StockId",
                table: "StockDataUpdateSource");

            migrationBuilder.DropPrimaryKey(
                name: "PK_StockDataUpdateSource",
                table: "StockDataUpdateSource");

            migrationBuilder.DropColumn(
                name: "CccStock",
                table: "Stocks");

            migrationBuilder.RenameTable(
                name: "StockDataUpdateSource",
                newName: "StockDataUpdateSources");

            migrationBuilder.RenameIndex(
                name: "IX_StockDataUpdateSource_StockId",
                table: "StockDataUpdateSources",
                newName: "IX_StockDataUpdateSources_StockId");

            migrationBuilder.AlterColumn<string>(
                name: "Ticker",
                table: "Stocks",
                type: "nvarchar(max)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

            /*migrationBuilder.AlterColumn<byte[]>(
                name: "RowVersion",
                table: "Stocks",
                type: "rowversion",
                rowVersion: true,
                nullable: true,
                oldClrType: typeof(byte[]),
                oldType: "rowversion",
                oldRowVersion: true);*/
            migrationBuilder.DropColumn("RowVersion", "Stocks");
            migrationBuilder.AddColumn<byte[]>(
                name: "RowVersion",
                table: "Stocks",
                type: "rowversion",
                rowVersion: true,
                nullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Name",
                table: "Stocks",
                type: "nvarchar(max)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

            migrationBuilder.AddColumn<DateTime>(
                name: "DataLastUpdated",
                table: "Stocks",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "DataUpdateSource",
                table: "Stocks",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<DateTime>(
                name: "PriceLastUpdated",
                table: "Stocks",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "PriceSource",
                table: "Stocks",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddPrimaryKey(
                name: "PK_StockDataUpdateSources",
                table: "StockDataUpdateSources",
                column: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_StockDataUpdateSources_Stocks_StockId",
                table: "StockDataUpdateSources",
                column: "StockId",
                principalTable: "Stocks",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
