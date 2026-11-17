using InvestmentCoPilot.MarketData.Stocks;
using InvestmentCoPilot.SharedKernel;
using InvestmentCoPilot.SharedKernel.Persistence;
using Microsoft.EntityFrameworkCore;

namespace InvestmentCoPilot.MarketData.Persistence;

internal sealed class MarketDataDbContext(DbContextOptions<MarketDataDbContext> options, ICurrentUser currentUser)
    : ModuleDbContext<MarketDataDbContext>(options, currentUser), IModuleDbContext
{
    public static string Schema => "market_data";

    public DbSet<CachedStockFundamentals> StockFundamentals => Set<CachedStockFundamentals>();

    protected override void ConfigureModel(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<CachedStockFundamentals>(stock =>
        {
            stock.ToTable("stock_fundamentals");
            stock.HasKey(s => s.Symbol);
            stock.Property(s => s.Symbol).HasColumnName("symbol").HasMaxLength(TickerSymbol.MaxLength);
            stock.Property(s => s.CompanyName).HasColumnName("company_name").HasMaxLength(CachedStockFundamentals.MaxCompanyNameLength);
            stock.Property(s => s.Currency).HasColumnName("currency").HasMaxLength(3).IsFixedLength();
            stock.ComplexProperty(s => s.Price, price =>
            {
                MoneyMapping.Configure(price);
                price.Property(m => m.Amount).HasColumnName("price_amount");
                price.Property(m => m.Currency).HasColumnName("price_currency");
            });
            stock.ComplexProperty(s => s.MarketCap, marketCap =>
            {
                MoneyMapping.Configure(marketCap);
                marketCap.Property(m => m.Amount).HasColumnName("market_cap_amount");
                marketCap.Property(m => m.Currency).HasColumnName("market_cap_currency");
            });

            // Ratios and fractions: up to 10^10 with six decimal places is far beyond any real value.
            stock.Property(s => s.TrailingPe).HasColumnName("trailing_pe").HasPrecision(16, 6);
            stock.Property(s => s.ForwardPe).HasColumnName("forward_pe").HasPrecision(16, 6);
            stock.Property(s => s.EpsGrowth).HasColumnName("eps_growth").HasPrecision(16, 6);
            stock.Property(s => s.InstitutionalOwnership).HasColumnName("institutional_ownership").HasPrecision(16, 6);
            stock.Property(s => s.InsiderOwnership).HasColumnName("insider_ownership").HasPrecision(16, 6);
            stock.Property(s => s.DebtToEquity).HasColumnName("debt_to_equity").HasPrecision(16, 6);

            stock.Property(s => s.Source).HasColumnName("source").HasMaxLength(CachedStockFundamentals.MaxSourceLength);
            stock.Property(s => s.AsOf).HasColumnName("as_of");
            stock.Property(s => s.FetchedAt).HasColumnName("fetched_at");
        });
    }
}
