using Microsoft.EntityFrameworkCore;
using OfertaBot.Models;

namespace OfertaBot.Data
{
    public class OfertaBotDbContext : DbContext
    {
        public DbSet<AffiliateCommission> Commissions { get; set; }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            optionsBuilder.UseSqlite("Data Source=ofertabot.db");
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<AffiliateCommission>()
                .HasKey(c => c.TransactionId);

            modelBuilder.Entity<AffiliateCommission>()
                .Property(c => c.CommissionAmount)
                .HasColumnType("decimal(18,2)");
        }
    }
}