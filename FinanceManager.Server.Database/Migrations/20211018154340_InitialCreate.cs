using System;
using Microsoft.EntityFrameworkCore.Migrations;

namespace Server.Migrations
{
    public partial class InitialCreate : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "CurrencyRates",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Date = table.Column<DateTime>(type: "datetime2", nullable: false),
                    NoData = table.Column<bool>(type: "bit", nullable: false),
                    EurUsd = table.Column<double>(type: "float", nullable: false),
                    EurCad = table.Column<double>(type: "float", nullable: false),
                    EurDkk = table.Column<double>(type: "float", nullable: false),
                    EurGbp = table.Column<double>(type: "float", nullable: false),
                    EurSek = table.Column<double>(type: "float", nullable: false),
                    EurNok = table.Column<double>(type: "float", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CurrencyRates", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "DividendPlans",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UserId = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    StartYear = table.Column<int>(type: "int", nullable: false),
                    Years = table.Column<int>(type: "int", nullable: false),
                    StartDividends = table.Column<double>(type: "float", nullable: false),
                    Yield = table.Column<double>(type: "float", nullable: false),
                    CurrentDivGrowth = table.Column<double>(type: "float", nullable: false),
                    NewDivGrowth = table.Column<double>(type: "float", nullable: false),
                    YearlyInvestment = table.Column<double>(type: "float", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DividendPlans", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Dividends",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UserId = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Symbol = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CompanyTicker = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ShareCount = table.Column<int>(type: "int", nullable: true),
                    AmountPerShare = table.Column<double>(type: "float", nullable: true),
                    PaymentDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Currency = table.Column<int>(type: "int", nullable: false),
                    Broker = table.Column<int>(type: "int", nullable: true),
                    FxRate = table.Column<double>(type: "float", nullable: true),
                    TotalReceived = table.Column<double>(type: "float", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Dividends", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "IexUpdateRuns",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    TimeStamp = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdateType = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CreditsUsed = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_IexUpdateRuns", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Portfolios",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: true),
                    UserId = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Portfolios", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Stocks",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: true),
                    Ticker = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CurrentPrice = table.Column<double>(type: "float", nullable: false),
                    PriceLastUpdated = table.Column<DateTime>(type: "datetime2", nullable: true),
                    DataLastUpdated = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Name = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Sector = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Dividend = table.Column<double>(type: "float", nullable: false),
                    DGYears = table.Column<int>(type: "int", nullable: false),
                    EpsTtm = table.Column<double>(type: "float", nullable: false),
                    DivGrowth1 = table.Column<double>(type: "float", nullable: false),
                    DivGrowth3 = table.Column<double>(type: "float", nullable: false),
                    DivGrowth5 = table.Column<double>(type: "float", nullable: false),
                    DivGrowth10 = table.Column<double>(type: "float", nullable: false),
                    EpsGrowth5 = table.Column<double>(type: "float", nullable: false),
                    DeptToEquity = table.Column<double>(type: "float", nullable: false),
                    PriceSource = table.Column<int>(type: "int", nullable: false),
                    DataUpdateSource = table.Column<int>(type: "int", nullable: false),
                    Currency = table.Column<int>(type: "int", nullable: false),
                    Exchange = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Stocks", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Watchlists",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: true),
                    UserId = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Name = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Watchlists", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "PortfolioPositions",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: true),
                    StockId = table.Column<int>(type: "int", nullable: true),
                    PortfolioId = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PortfolioPositions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PortfolioPositions_Portfolios_PortfolioId",
                        column: x => x.PortfolioId,
                        principalTable: "Portfolios",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PortfolioPositions_Stocks_StockId",
                        column: x => x.StockId,
                        principalTable: "Stocks",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "WatchlistStocks",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: true),
                    StockId = table.Column<int>(type: "int", nullable: true),
                    TargetPrice = table.Column<double>(type: "float", nullable: true),
                    Notify = table.Column<bool>(type: "bit", nullable: false),
                    AlarmSent = table.Column<bool>(type: "bit", nullable: false),
                    WatchlistId = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WatchlistStocks", x => x.Id);
                    table.ForeignKey(
                        name: "FK_WatchlistStocks_Stocks_StockId",
                        column: x => x.StockId,
                        principalTable: "Stocks",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_WatchlistStocks_Watchlists_WatchlistId",
                        column: x => x.WatchlistId,
                        principalTable: "Watchlists",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "StockPurchases",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: true),
                    PurchaseDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Amount = table.Column<double>(type: "float", nullable: false),
                    Price = table.Column<double>(type: "float", nullable: false),
                    Broker = table.Column<int>(type: "int", nullable: true),
                    PortfolioPositionId = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StockPurchases", x => x.Id);
                    table.ForeignKey(
                        name: "FK_StockPurchases_PortfolioPositions_PortfolioPositionId",
                        column: x => x.PortfolioPositionId,
                        principalTable: "PortfolioPositions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_PortfolioPositions_PortfolioId",
                table: "PortfolioPositions",
                column: "PortfolioId");

            migrationBuilder.CreateIndex(
                name: "IX_PortfolioPositions_StockId",
                table: "PortfolioPositions",
                column: "StockId");

            migrationBuilder.CreateIndex(
                name: "IX_StockPurchases_PortfolioPositionId",
                table: "StockPurchases",
                column: "PortfolioPositionId");

            migrationBuilder.CreateIndex(
                name: "IX_WatchlistStocks_StockId",
                table: "WatchlistStocks",
                column: "StockId");

            migrationBuilder.CreateIndex(
                name: "IX_WatchlistStocks_WatchlistId",
                table: "WatchlistStocks",
                column: "WatchlistId");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CurrencyRates");

            migrationBuilder.DropTable(
                name: "DividendPlans");

            migrationBuilder.DropTable(
                name: "Dividends");

            migrationBuilder.DropTable(
                name: "IexUpdateRuns");

            migrationBuilder.DropTable(
                name: "StockPurchases");

            migrationBuilder.DropTable(
                name: "WatchlistStocks");

            migrationBuilder.DropTable(
                name: "PortfolioPositions");

            migrationBuilder.DropTable(
                name: "Watchlists");

            migrationBuilder.DropTable(
                name: "Portfolios");

            migrationBuilder.DropTable(
                name: "Stocks");
        }
    }
}
