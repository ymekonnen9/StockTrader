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

        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options)
        {

        }

        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);

            builder.Entity<ApplicationUser>(entity =>
            {
                entity.Property(u => u.CashAmount).HasColumnType("decimal(18,2)");
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

                entity.Property(ush => ush.AveragePrice).HasColumnType("decimal(18,4)").IsRequired();


            });
        }

    }
}
