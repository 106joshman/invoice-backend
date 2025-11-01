using InvoiceService.Models;
using Microsoft.EntityFrameworkCore;

namespace InvoiceService.Data
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        // PREDEFINED DATABASE TABLES STRUCTURE
        public DbSet<User> Users { get; set; }
        public DbSet<Customer> Customers { get; set; }
        public DbSet<Invoice> Invoices { get; set; }
        public DbSet<InvoiceItem> InvoiceItems { get; set; }
        public DbSet<PaymentInfo> PaymentInfo { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // ---------------- USERS ----------------
                modelBuilder.Entity<User>(entity =>
                {
                    entity.HasKey(u => u.Id);

                    entity.HasIndex(u => u.Email)
                        .IsUnique();

                    // 1-TO-1 RELATIONSHIP WITH PAYMENT INFO
                    entity.HasOne(u => u.PaymentInfo)
                        .WithOne(p => p.User)
                        .HasForeignKey<PaymentInfo>(p => p.UserId)
                        .OnDelete(DeleteBehavior.Cascade);

                    // 1-TO-1 RELATIONSHIP WITH CUSTOMERS
                    entity.HasMany(u => u.Customers)
                        .WithOne(c => c.User)
                        .HasForeignKey(c => c.UserId)
                        .OnDelete(DeleteBehavior.Cascade);

                    // 1-TO-MANY RELATIONSHIP WITH INVOICES
                    entity.HasMany(u => u.Invoices)
                        .WithOne(i => i.User)
                        .HasForeignKey(i => i.UserId)
                        .OnDelete(DeleteBehavior.Cascade);
                });

                // ---------------- CUSTOMERS ----------------
                modelBuilder.Entity<Customer>(entity =>
                {
                    entity.HasKey(c => c.Id);
                    entity.Property(c => c.Name)
                    .IsRequired()
                    .HasMaxLength(200);
                    entity.HasIndex(c => new { c.Email, c.UserId })
                    .IsUnique(); // a user can’t duplicate a customer email
                });

                // ---------------- INVOICES ----------------
                modelBuilder.Entity<Invoice>(entity =>
                {
                    entity.HasKey(i => i.Id);

                    entity.HasIndex(i => i.InvoiceNumber)
                        .IsUnique();

                    entity.Property(i => i.InvoiceNumber)
                        .IsRequired()
                        .HasMaxLength(50);

                    // Invoice -> User
                    entity.HasOne(i => i.User)
                        .WithMany(u => u.Invoices)
                        .HasForeignKey(i => i.UserId)
                        .OnDelete(DeleteBehavior.Cascade);

                    // Invoice -> Customer
                    entity.HasOne(i => i.Customer)
                        .WithMany(c => c.Invoices)
                        .HasForeignKey(i => i.CustomerId)
                        .OnDelete(DeleteBehavior.Cascade);

                    // Invoice -> Items
                    entity.HasMany(i => i.Items)
                        .WithOne(it => it.Invoice)
                        .HasForeignKey(it => it.InvoiceId)
                        .OnDelete(DeleteBehavior.Cascade);
                });

                // ---------------- INVOICE ITEMS ----------------
                modelBuilder.Entity<InvoiceItem>(entity =>
                {
                    entity.HasKey(it => it.Id);
                    entity.Property(it => it.Description)
                    .IsRequired()
                    .HasMaxLength(255);
                });

                // ---------------- PAYMENT INFO ----------------
                modelBuilder.Entity<PaymentInfo>(entity =>
                {
                    entity.HasKey(p => p.Id);
                    entity.HasIndex(p => p.AccountNumber);
                });
        }
    }
}