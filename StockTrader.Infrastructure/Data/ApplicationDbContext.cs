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
        }

    }
}
