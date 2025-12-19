using InvoiceService.Models;
using Microsoft.EntityFrameworkCore;

namespace InvoiceService.Data
{
    public class ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : DbContext(options)
    {

        // PREDEFINED DATABASE TABLES STRUCTURE
        public DbSet<User> Users { get; set; }
        public DbSet<Customer> Customers { get; set; }
        public DbSet<Invoice> Invoices { get; set; }
        public DbSet<InvoiceItem> InvoiceItems { get; set; }
        public DbSet<PaymentInfo> PaymentInfo { get; set; }
        public DbSet<Business> Businesses { get; set; }
        public DbSet<AuditLog> AuditLogs { get; set; }
        public DbSet<BusinessUser> BusinessUsers { get; set; }


        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // ---------------- USERS ----------------
            modelBuilder.Entity<User>(entity =>
            {
                entity.HasKey(u => u.Id);

                entity.HasIndex(u => u.Email)
                    .IsUnique();
            });

            // ---------------- BUSINESSES ----------------
            modelBuilder.Entity<Business>(entity =>
            {
                entity.HasKey(b => b.Id);

                entity.HasIndex(b => b.Name)
                    .IsUnique();

                // 1-TO-1 RELATIONSHIP WITH PAYMENT INFO
                entity.HasOne(u => u.PaymentInfo)
                    .WithOne(p => p.Business)
                    .HasForeignKey<PaymentInfo>(p => p.BusinessId)
                    .OnDelete(DeleteBehavior.Cascade);

                // 1-TO-1 BUSINESS RELATIONSHIP WITH CUSTOMERS
                entity.HasMany(u => u.Customers)
                    .WithOne(c => c.Business)
                    .HasForeignKey(c => c.BusinessId)
                    .OnDelete(DeleteBehavior.Restrict);

                // 1-TO-MANY BUSINESS RELATIONSHIP WITH INVOICES
                entity.HasMany(u => u.Invoices)
                    .WithOne(i => i.Business)
                    .HasForeignKey(i => i.BusinessId)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            // ---------------- CUSTOMERS ----------------
            modelBuilder.Entity<Customer>(entity =>
            {
                entity.HasKey(c => c.Id);

                entity.Property(c => c.Name)
                .IsRequired()
                .HasMaxLength(200);

                entity.HasIndex(c => new { c.Email, c.BusinessId })
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

                // Invoice -> Business
                entity.HasOne(i => i.Business)
                    .WithMany(u => u.Invoices)
                    .HasForeignKey(i => i.BusinessId)
                    .OnDelete(DeleteBehavior.Restrict);

                // Invoice -> Customer
                entity.HasOne(i => i.Customer)
                    .WithMany(c => c.Invoices)
                    .HasForeignKey(i => i.CustomerId)
                    .OnDelete(DeleteBehavior.Restrict);

                // Invoice -> Items
                entity.HasMany(i => i.Items)
                    .WithOne(it => it.Invoice)
                    .HasForeignKey(it => it.InvoiceId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasQueryFilter(i => !i.IsDeleted);
            });

            // ---------------- INVOICE ITEMS ----------------
            modelBuilder.Entity<InvoiceItem>(entity =>
            {
                entity.HasKey(it => it.Id);

                entity.Property(it => it.Description)
                .IsRequired()
                .HasMaxLength(255);

                entity.HasQueryFilter(it => !it.IsDeleted);
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