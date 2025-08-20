using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using UrbanWoolen.Models;

namespace UrbanWoolen.Data
{
    public class ApplicationDbContext : IdentityDbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        public DbSet<Product> Products { get; set; }
        public DbSet<Order> Orders { get; set; }
        public DbSet<OrderItem> OrderItems { get; set; }
        public DbSet<WishlistItem> WishlistItems { get; set; }
        public DbSet<Review> Reviews { get; set; }
        public DbSet<EmailOtpVerification> EmailOtpVerifications { get; set; }

        // Size charts
        public DbSet<SizeChart> SizeCharts { get; set; }
        public DbSet<SizeChartItem> SizeChartItems { get; set; }

        // NEW: Discounts
        public DbSet<Discount> Discounts { get; set; }
        public DbSet<InventoryTransaction> InventoryTransactions { get; set; }


        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);

            // Size chart relations & seed
            builder.Entity<SizeChart>()
                .HasMany(sc => sc.Items)
                .WithOne(i => i.SizeChart)
                .HasForeignKey(i => i.SizeChartId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.Entity<SizeChart>().HasData(
                new SizeChart { Id = 1, Category = ProductCategory.Men, Title = "Men Tops (BD)", Region = "BD", Unit = "cm", ChartType = ChartType.Tops },
                new SizeChart { Id = 2, Category = ProductCategory.Women, Title = "Women Tops (BD)", Region = "BD", Unit = "cm", ChartType = ChartType.Tops }
            );

            builder.Entity<SizeChartItem>().HasData(
                new SizeChartItem { Id = 101, SizeChartId = 1, Size = "S", Chest = 92, Waist = 78, Length = 67 },
                new SizeChartItem { Id = 102, SizeChartId = 1, Size = "M", Chest = 98, Waist = 84, Length = 69 },
                new SizeChartItem { Id = 103, SizeChartId = 1, Size = "L", Chest = 104, Waist = 90, Length = 71 },
                new SizeChartItem { Id = 104, SizeChartId = 1, Size = "XL", Chest = 110, Waist = 96, Length = 73 },

                new SizeChartItem { Id = 201, SizeChartId = 2, Size = "S", Chest = 84, Waist = 66, Length = 62 },
                new SizeChartItem { Id = 202, SizeChartId = 2, Size = "M", Chest = 90, Waist = 72, Length = 64 },
                new SizeChartItem { Id = 203, SizeChartId = 2, Size = "L", Chest = 96, Waist = 78, Length = 66 },
                new SizeChartItem { Id = 204, SizeChartId = 2, Size = "XL", Chest = 102, Waist = 84, Length = 68 }
            );

            // Discounts relation
            builder.Entity<Discount>()
                .HasOne(d => d.Product)
                .WithMany() // change to .WithMany(p => p.Discounts) if you later add a collection to Product
                .HasForeignKey(d => d.ProductId)
                .OnDelete(DeleteBehavior.Cascade);
        }
    }
}
