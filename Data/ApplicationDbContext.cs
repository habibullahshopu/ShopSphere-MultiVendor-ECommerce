using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using ShopSphere.Models;
using System.Reflection.Emit;

namespace ShopSphere.Data;

public class ApplicationDbContext : IdentityDbContext<ApplicationUser>
{
    public ApplicationDbContext(
        DbContextOptions<ApplicationDbContext> options)
        : base(options)
    {
    }

    public DbSet<Product> Products { get; set; }
    public DbSet<Category> Categories { get; set; }

    public DbSet<Wishlist> Wishlists { get; set; }
    public DbSet<CustomerAddress> CustomerAddresses { get; set; }

    public DbSet<Cart> Carts { get; set; }
    public DbSet<CartItem> CartItems { get; set; }

    public DbSet<Order> Orders { get; set; }
    public DbSet<OrderItem> OrderItems { get; set; }

    public DbSet<Payment> Payments { get; set; }

    public DbSet<Vendor> Vendors { get; set; }
    public DbSet<Coupon> Coupons { get; set; }
    public DbSet<ReturnRequest> ReturnRequests { get; set; }
    public DbSet<SupportTicket> SupportTickets { get; set; }

    // =====================================================
    // Product Reviews
    // =====================================================

    public DbSet<ProductReview> ProductReviews { get; set; }


    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);


        // =====================================================
        // Wishlist
        // =====================================================

        builder.Entity<Wishlist>()
            .HasIndex(w => new
            {
                w.UserId,
                w.ProductId
            })
            .IsUnique();

        builder.Entity<Wishlist>()
            .HasOne(w => w.User)
            .WithMany()
            .HasForeignKey(w => w.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Entity<Wishlist>()
            .HasOne(w => w.Product)
            .WithMany()
            .HasForeignKey(w => w.ProductId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Entity<ReturnRequest>()
    .HasOne(r => r.Order)
    .WithMany()
    .HasForeignKey(r => r.OrderId)
    .OnDelete(DeleteBehavior.Restrict);

        builder.Entity<ReturnRequest>()
            .HasOne(r => r.User)
            .WithMany()
            .HasForeignKey(r => r.UserId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Entity<ReturnRequest>()
            .HasIndex(r => new { r.OrderId, r.UserId });

        //coupon
        builder.Entity<Coupon>()
    .HasIndex(c => c.Code)
    .IsUnique();
        // =====================================================
        // Customer Address
        // =====================================================

        builder.Entity<CustomerAddress>()
            .HasOne(a => a.User)
            .WithMany()
            .HasForeignKey(a => a.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Entity<CustomerAddress>()
            .HasIndex(a => a.UserId);


        // =====================================================
        // Payment → Order
        // =====================================================

        builder.Entity<Payment>()
            .HasOne(p => p.Order)
            .WithMany()
            .HasForeignKey(p => p.OrderId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Entity<Payment>()
            .HasIndex(p => p.OrderId)
            .IsUnique();


        // =====================================================
        // Order → OrderItems
        // =====================================================

        builder.Entity<Order>()
            .HasMany(o => o.OrderItems)
            .WithOne(oi => oi.Order)
            .HasForeignKey(oi => oi.OrderId)
            .OnDelete(DeleteBehavior.Cascade);


        // =====================================================
        // OrderItem → Product
        // =====================================================

        builder.Entity<OrderItem>()
            .HasOne(oi => oi.Product)
            .WithMany()
            .HasForeignKey(oi => oi.ProductId)
            .OnDelete(DeleteBehavior.Restrict);


        // =====================================================
        // Cart → CartItems
        // =====================================================

        builder.Entity<Cart>()
            .HasMany(c => c.CartItems)
            .WithOne(ci => ci.Cart)
            .HasForeignKey(ci => ci.CartId)
            .OnDelete(DeleteBehavior.Cascade);


        // =====================================================
        // Product → CartItems
        // =====================================================

        builder.Entity<Product>()
            .HasMany<CartItem>()
            .WithOne(ci => ci.Product)
            .HasForeignKey(ci => ci.ProductId)
            .OnDelete(DeleteBehavior.Restrict);


        // =====================================================
        // Vendor → ApplicationUser
        // =====================================================

        builder.Entity<Vendor>()
            .HasOne(v => v.User)
            .WithOne()
            .HasForeignKey<Vendor>(v => v.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Entity<Vendor>()
            .HasIndex(v => v.UserId)
            .IsUnique();


        // =====================================================
        // Vendor → Products
        // =====================================================

        builder.Entity<Product>()
            .HasOne(p => p.Vendor)
            .WithMany()
            .HasForeignKey(p => p.VendorId)
            .OnDelete(DeleteBehavior.SetNull);


        // =====================================================
        // Cart duplicate protection
        // =====================================================

        builder.Entity<CartItem>()
            .HasIndex(ci => new
            {
                ci.CartId,
                ci.ProductId
            })
            .IsUnique();


        // =====================================================
        // ProductReview
        // =====================================================

        // One customer can review the same product only once.
        builder.Entity<ProductReview>()
            .HasIndex(r => new
            {
                r.UserId,
                r.ProductId
            })
            .IsUnique();


        // =====================================================
        // ProductReview → ApplicationUser
        // =====================================================

        builder.Entity<ProductReview>()
            .HasOne(r => r.User)
            .WithMany()
            .HasForeignKey(r => r.UserId)
            .OnDelete(DeleteBehavior.Cascade);


        // =====================================================
        // ProductReview → Product
        // =====================================================

        builder.Entity<ProductReview>()
            .HasOne(r => r.Product)
            .WithMany()
            .HasForeignKey(r => r.ProductId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}