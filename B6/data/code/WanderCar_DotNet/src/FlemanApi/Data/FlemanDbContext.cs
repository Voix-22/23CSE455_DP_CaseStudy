using FlemanApi.Models;
using Microsoft.EntityFrameworkCore;

namespace FlemanApi.Data;

public class FlemanDbContext : DbContext
{
    public FlemanDbContext(DbContextOptions<FlemanDbContext> options) : base(options) { }

    public DbSet<State> States => Set<State>();
    public DbSet<City> Cities => Set<City>();
    public DbSet<Hub> Hubs => Set<Hub>();
    public DbSet<Airport> Airports => Set<Airport>();
    public DbSet<Addon> Addons => Set<Addon>();
    public DbSet<CarType> CarTypes => Set<CarType>();
    public DbSet<Car> Cars => Set<Car>();
    public DbSet<Customer> Customers => Set<Customer>();
    public DbSet<BookingHeader> BookingHeaders => Set<BookingHeader>();
    public DbSet<BookingDetail> BookingDetails => Set<BookingDetail>();
    public DbSet<InvoiceHeader> InvoiceHeaders => Set<InvoiceHeader>();
    public DbSet<InvoiceDetail> InvoiceDetails => Set<InvoiceDetail>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<State>().HasIndex(s => s.StateName).IsUnique();
        modelBuilder.Entity<Addon>().HasIndex(a => a.AddonName).IsUnique();
        modelBuilder.Entity<Airport>().HasIndex(a => a.AirportCode).IsUnique();
        modelBuilder.Entity<CarType>().HasIndex(c => c.CarTypeName).IsUnique();
        modelBuilder.Entity<Car>().HasIndex(c => c.VehicleNumber).IsUnique();
        modelBuilder.Entity<BookingHeader>().HasIndex(b => b.ConfirmationNo).IsUnique();

        modelBuilder.Entity<Customer>().HasIndex(c => c.Email).IsUnique();
        modelBuilder.Entity<Customer>().HasIndex(c => c.Phone).IsUnique();
        modelBuilder.Entity<Customer>().HasIndex(c => c.DrivingLicenseNo).IsUnique();

        // Every enum is stored as its string name, matching the Java
        // @Enumerated(EnumType.STRING) columns exactly (same values on disk,
        // readable by a DBA, safe to add new values without renumbering).
        modelBuilder.Entity<Customer>().Property(c => c.Status).HasConversion<string>();
        modelBuilder.Entity<Customer>().Property(c => c.Provider).HasConversion<string>();
        modelBuilder.Entity<Car>().Property(c => c.FuelType).HasConversion<string>();
        modelBuilder.Entity<Car>().Property(c => c.Status).HasConversion<string>();
        modelBuilder.Entity<BookingHeader>().Property(b => b.BookingStatus).HasConversion<string>();
        modelBuilder.Entity<InvoiceHeader>().Property(i => i.PaymentType).HasConversion<string>();
        modelBuilder.Entity<InvoiceHeader>().Property(i => i.PaymentStatus).HasConversion<string>();
    }
}
