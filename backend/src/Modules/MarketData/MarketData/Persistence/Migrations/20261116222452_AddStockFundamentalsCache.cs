using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace InvestmentCoPilot.MarketData.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddStockFundamentalsCache : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "market_data");

            migrationBuilder.CreateTable(
                name: "stock_fundamentals",
                schema: "market_data",
                columns: table => new
                {
                    symbol = table.Column<string>(type: "character varying(15)", maxLength: 15, nullable: false),
                    company_name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    currency = table.Column<string>(type: "character(3)", fixedLength: true, maxLength: 3, nullable: false),
                    trailing_pe = table.Column<decimal>(type: "numeric(16,6)", precision: 16, scale: 6, nullable: true),
                    forward_pe = table.Column<decimal>(type: "numeric(16,6)", precision: 16, scale: 6, nullable: true),
                    eps_growth = table.Column<decimal>(type: "numeric(16,6)", precision: 16, scale: 6, nullable: true),
                    institutional_ownership = table.Column<decimal>(type: "numeric(16,6)", precision: 16, scale: 6, nullable: true),
                    insider_ownership = table.Column<decimal>(type: "numeric(16,6)", precision: 16, scale: 6, nullable: true),
                    debt_to_equity = table.Column<decimal>(type: "numeric(16,6)", precision: 16, scale: 6, nullable: true),
                    source = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    as_of = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    fetched_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    market_cap_amount = table.Column<decimal>(type: "numeric(19,4)", precision: 19, scale: 4, nullable: true),
                    market_cap_currency = table.Column<string>(type: "character(3)", fixedLength: true, maxLength: 3, nullable: true),
                    price_amount = table.Column<decimal>(type: "numeric(19,4)", precision: 19, scale: 4, nullable: true),
                    price_currency = table.Column<string>(type: "character(3)", fixedLength: true, maxLength: 3, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_stock_fundamentals", x => x.symbol);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "stock_fundamentals",
                schema: "market_data");
        }
    }
}
