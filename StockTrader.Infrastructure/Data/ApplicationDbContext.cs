using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using StockTrader.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace StockTrader.Infrastructure.Data
{
    public class ApplicationDbContext : IdentityDbContext<ApplicationUser>
    {
        public DbSet<Stock> Stocks { get; set; }
        public DbSet<UserStockHolding> UserStockHoldings { get; set; }
        public DbSet<Order> Orders { get; set; }

        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options)
        {

        }

        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);

            builder.Entity<ApplicationUser>(entity =>
            {
                entity.Property(u => u.CashAmount).HasColumnType("decimal(18,2)");
                entity.Property(u => u.RowVersion).IsRowVersion();
            });

            builder.Entity<Stock>(entity =>
            {
                entity.HasKey(s => s.Id);
                entity.HasIndex(s => s.Symbol).IsUnique();
                entity.Property(s => s.Symbol).IsRequired().HasMaxLength(10);
                entity.Property(s => s.CompanyName).IsRequired().HasMaxLength(100);
                entity.Property(s => s.CurrentPrice).HasColumnType("decimal(18,2)");
            });

            builder.Entity<UserStockHolding>(entity =>
            {
                entity.HasKey(ush => new { ush.ApplicationUserId, ush.StockId });

                entity.HasOne(ush => ush.applicationUser)
                      .WithMany(u => u.userstockholdings)
                      .HasForeignKey(u => u.ApplicationUserId)
                      .IsRequired()
                      .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(s => s.stock)
                      .WithMany(s => s.userstockholdings)
                      .HasForeignKey(s => s.StockId)
                      .IsRequired()
                      .OnDelete(DeleteBehavior.Cascade);

                entity.Property(ush => ush.Quantity);
                entity.Property(u => u.RowVersion).IsRowVersion();
                entity.Property(ush => ush.AveragePurchasePrice).HasColumnType("decimal(18,4)").IsRequired();


            });

            builder.Entity<Order>(entity =>
            {
                entity.HasKey(o => o.Id);
                entity.Property(o => o.Quantity).IsRequired();
                entity.Property(o => o.TimeOrderedAt).IsRequired();
                entity.Property(o => o.PriceAtOrdered).HasColumnType("decimal(65,30)").IsRequired();
                entity.Property(o => o.OrderStatus).IsRequired();
                entity.Property(o => o.OrderType).IsRequired();

                entity.HasOne(o => o.ApplicationUser)
                      .WithMany(o => o.Orders)
                      .HasForeignKey(o => o.ApplicationUserId)
                      .IsRequired().OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(o => o.Stock)
                      .WithMany(o => o.Orders)
                      .HasForeignKey(o => o.StockId)
                      .IsRequired()
                      .OnDelete(DeleteBehavior.Restrict);


            });
        }

    }
}
