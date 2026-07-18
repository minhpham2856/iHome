using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;

namespace iHome.DAL.Entities;

public partial class IHomeDbContext : DbContext
{
    public IHomeDbContext()
    {
    }

    public IHomeDbContext(DbContextOptions<IHomeDbContext> options)
        : base(options)
    {
    }

    public virtual DbSet<AuditLog> AuditLogs { get; set; }

    public virtual DbSet<Building> Buildings { get; set; }

    public virtual DbSet<Contract> Contracts { get; set; }

    public virtual DbSet<ContractTenant> ContractTenants { get; set; }

    public virtual DbSet<Invoice> Invoices { get; set; }

    public virtual DbSet<InvoiceItem> InvoiceItems { get; set; }

    public virtual DbSet<Payment> Payments { get; set; }

    public virtual DbSet<Property> Properties { get; set; }

    public virtual DbSet<Room> Rooms { get; set; }

    public virtual DbSet<RoomService> RoomServices { get; set; }

    public virtual DbSet<RoomType> RoomTypes { get; set; }

    public virtual DbSet<Service> Services { get; set; }

    public virtual DbSet<ServiceReading> ServiceReadings { get; set; }

    public virtual DbSet<Tenant> Tenants { get; set; }

    public virtual DbSet<User> Users { get; set; }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
#warning To protect potentially sensitive information in your connection string, you should move it out of source code. You can avoid scaffolding the connection string by using the Name= syntax to read it from configuration - see https://go.microsoft.com/fwlink/?linkid=2131148. For more guidance on storing connection strings, see https://go.microsoft.com/fwlink/?LinkId=723263.
        => optionsBuilder.UseSqlServer("Server=(local);uid=sa;password=123;database=iHomeDB;Encrypt=True;TrustServerCertificate=True;");

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<AuditLog>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__AuditLog__3214EC07EA75D0E3");

            entity.HasIndex(e => e.Timestamp, "IX_AuditLogs_Timestamp");

            entity.HasIndex(e => e.UserId, "IX_AuditLogs_UserId");

            entity.Property(e => e.Action).HasMaxLength(100);
            entity.Property(e => e.RecordId).HasMaxLength(50);
            entity.Property(e => e.TableName).HasMaxLength(100);
            entity.Property(e => e.Timestamp).HasDefaultValueSql("(getdate())");

            entity.HasOne(d => d.User).WithMany(p => p.AuditLogs)
                .HasForeignKey(d => d.UserId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_AuditLogs_Users");
        });

        modelBuilder.Entity<Building>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__Building__3214EC07077E5BEA");

            entity.HasIndex(e => e.ManagerId, "IX_Buildings_ManagerId");

            entity.HasIndex(e => e.PropertyId, "IX_Buildings_PropertyId");

            entity.Property(e => e.Description).HasMaxLength(500);
            entity.Property(e => e.IsActive).HasDefaultValue(true);
            entity.Property(e => e.Name).HasMaxLength(100);

            entity.HasOne(d => d.Manager).WithMany(p => p.Buildings)
                .HasForeignKey(d => d.ManagerId)
                .HasConstraintName("FK_Buildings_Users");

            entity.HasOne(d => d.Property).WithMany(p => p.Buildings)
                .HasForeignKey(d => d.PropertyId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Buildings_Properties");
        });

        modelBuilder.Entity<Contract>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__Contract__3214EC07BDC69C71");

            entity.HasIndex(e => e.RoomId, "IX_Contracts_RoomId");

            entity.HasIndex(e => new { e.StartDate, e.EndDate }, "IX_Contracts_StartEnd");

            entity.HasIndex(e => e.Status, "IX_Contracts_Status");

            entity.Property(e => e.CreatedAt).HasDefaultValueSql("(getdate())");
            entity.Property(e => e.DepositAmount).HasColumnType("decimal(15, 2)");
            entity.Property(e => e.MonthlyRent).HasColumnType("decimal(15, 2)");
            entity.Property(e => e.Notes).HasMaxLength(300);
            entity.Property(e => e.Status)
                .HasMaxLength(20)
                .IsUnicode(false);

            entity.HasOne(d => d.CreatedByNavigation).WithMany(p => p.Contracts)
                .HasForeignKey(d => d.CreatedBy)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Contracts_Users");

            entity.HasOne(d => d.Room).WithMany(p => p.Contracts)
                .HasForeignKey(d => d.RoomId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Contracts_Rooms");
        });

        modelBuilder.Entity<ContractTenant>(entity =>
        {
            entity.HasKey(e => new { e.ContractId, e.TenantId });

            entity.HasIndex(e => e.TenantId, "IX_ContractTenants_TenantId");

            entity.HasOne(d => d.Contract).WithMany(p => p.ContractTenants)
                .HasForeignKey(d => d.ContractId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_ContractTenants_Contracts");

            entity.HasOne(d => d.Tenant).WithMany(p => p.ContractTenants)
                .HasForeignKey(d => d.TenantId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_ContractTenants_Tenants");
        });

        modelBuilder.Entity<Invoice>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__Invoices__3214EC07A64D3CB4");

            entity.HasIndex(e => e.ContractId, "IX_Invoices_ContractId");

            entity.HasIndex(e => e.DueDate, "IX_Invoices_DueDate");

            entity.HasIndex(e => e.Status, "IX_Invoices_Status");

            entity.HasIndex(e => new { e.ContractId, e.InvoiceDate }, "UQ_Invoices_ContractDate").IsUnique();

            entity.Property(e => e.InvoiceDate).HasDefaultValueSql("(getdate())");
            entity.Property(e => e.Status)
                .HasMaxLength(20)
                .IsUnicode(false);
            entity.Property(e => e.TotalAmount).HasColumnType("decimal(15, 2)");

            entity.HasOne(d => d.Contract).WithMany(p => p.Invoices)
                .HasForeignKey(d => d.ContractId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Invoices_Contracts");
        });

        modelBuilder.Entity<InvoiceItem>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__InvoiceI__3214EC0746E50CFC");

            entity.Property(e => e.Amount).HasColumnType("decimal(15, 2)");
            entity.Property(e => e.Description).HasMaxLength(200);
            entity.Property(e => e.Quantity).HasColumnType("decimal(15, 2)");
            entity.Property(e => e.UnitPrice).HasColumnType("decimal(15, 2)");

            entity.HasOne(d => d.Invoice).WithMany(p => p.InvoiceItems)
                .HasForeignKey(d => d.InvoiceId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_InvoiceItems_Invoices");
        });

        modelBuilder.Entity<Payment>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__Payments__3214EC071BFFADFD");

            entity.HasIndex(e => e.InvoiceId, "IX_Payments_InvoiceId");

            entity.Property(e => e.Amount).HasColumnType("decimal(15, 2)");
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("(getdate())");
            entity.Property(e => e.Method)
                .HasMaxLength(20)
                .IsUnicode(false);
            entity.Property(e => e.Notes).HasMaxLength(300);

            entity.HasOne(d => d.Invoice).WithMany(p => p.Payments)
                .HasForeignKey(d => d.InvoiceId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Payments_Invoices");

            entity.HasOne(d => d.ReceivedByNavigation).WithMany(p => p.Payments)
                .HasForeignKey(d => d.ReceivedBy)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Payments_Users");
        });

        modelBuilder.Entity<Property>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__Properti__3214EC07A0632B4C");

            entity.HasIndex(e => e.LandlordId, "IX_Properties_LandlordId");

            entity.Property(e => e.Address).HasMaxLength(300);
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("(getdate())");
            entity.Property(e => e.Description).HasMaxLength(500);
            entity.Property(e => e.IsActive).HasDefaultValue(true);
            entity.Property(e => e.Name).HasMaxLength(150);

            entity.HasOne(d => d.Landlord).WithMany(p => p.Properties)
                .HasForeignKey(d => d.LandlordId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Properties_Users");
        });

        modelBuilder.Entity<Room>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__Rooms__3214EC07ACEE2830");

            entity.HasIndex(e => e.BuildingId, "IX_Rooms_BuildingId");

            entity.HasIndex(e => e.RoomTypeId, "IX_Rooms_RoomTypeId");

            entity.HasIndex(e => e.Status, "IX_Rooms_Status");

            entity.Property(e => e.Area).HasColumnType("decimal(8, 2)");
            entity.Property(e => e.BaseRent).HasColumnType("decimal(15, 2)");
            entity.Property(e => e.Notes).HasMaxLength(300);
            entity.Property(e => e.RoomNumber).HasMaxLength(20);
            entity.Property(e => e.Status)
                .HasMaxLength(20)
                .IsUnicode(false);

            entity.HasOne(d => d.Building).WithMany(p => p.Rooms)
                .HasForeignKey(d => d.BuildingId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Rooms_Buildings");

            entity.HasOne(d => d.RoomType).WithMany(p => p.Rooms)
                .HasForeignKey(d => d.RoomTypeId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Rooms_RoomTypes");
        });

        modelBuilder.Entity<RoomService>(entity =>
        {
            entity.HasKey(e => new { e.RoomId, e.ServiceId });

            entity.HasIndex(e => e.ServiceId, "IX_RoomServices_ServiceId");

            entity.Property(e => e.IsActive).HasDefaultValue(true);

            entity.HasOne(d => d.Room).WithMany(p => p.RoomServices)
                .HasForeignKey(d => d.RoomId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_RoomServices_Rooms");

            entity.HasOne(d => d.Service).WithMany(p => p.RoomServices)
                .HasForeignKey(d => d.ServiceId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_RoomServices_Services");
        });

        modelBuilder.Entity<RoomType>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__RoomType__3214EC07F94F5D00");

            entity.HasIndex(e => e.BuildingId, "IX_RoomTypes_BuildingId");

            entity.Property(e => e.Description).HasMaxLength(300);
            entity.Property(e => e.TypeName).HasMaxLength(100);

            entity.HasOne(d => d.Building).WithMany(p => p.RoomTypes)
                .HasForeignKey(d => d.BuildingId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_RoomTypes_Buildings");
        });

        modelBuilder.Entity<Service>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__Services__3214EC079990BFF3");

            entity.HasIndex(e => e.BuildingId, "IX_Services_BuildingId");

            entity.Property(e => e.CalculationMethod)
                .HasMaxLength(20)
                .IsUnicode(false);
            entity.Property(e => e.IsActive).HasDefaultValue(true);
            entity.Property(e => e.ServiceName).HasMaxLength(100);
            entity.Property(e => e.Unit).HasMaxLength(20);
            entity.Property(e => e.UnitPrice).HasColumnType("decimal(15, 2)");

            entity.HasOne(d => d.Building).WithMany(p => p.Services)
                .HasForeignKey(d => d.BuildingId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Services_Buildings");
        });

        modelBuilder.Entity<ServiceReading>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__ServiceR__3214EC07537F0AAF");

            entity.HasIndex(e => new { e.RoomId, e.ServiceId }, "IX_ServiceReadings_RoomService");

            entity.Property(e => e.CurrentReading).HasColumnType("decimal(15, 2)");
            entity.Property(e => e.ReadingDate).HasDefaultValueSql("(getdate())");

            entity.HasOne(d => d.RoomService).WithMany(p => p.ServiceReadings)
                .HasForeignKey(d => new { d.RoomId, d.ServiceId })
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_ServiceReadings_RoomServices");
        });

        modelBuilder.Entity<Tenant>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__Tenants__3214EC0764B5CC17");

            entity.HasIndex(e => e.IdCardNumber, "UQ__Tenants__713A7B91DE688DB7").IsUnique();

            entity.Property(e => e.CreatedAt).HasDefaultValueSql("(getdate())");
            entity.Property(e => e.Email)
                .HasMaxLength(150)
                .IsUnicode(false);
            entity.Property(e => e.FullName).HasMaxLength(100);
            entity.Property(e => e.IdCardNumber)
                .HasMaxLength(20)
                .IsUnicode(false);
            entity.Property(e => e.PermanentAddress).HasMaxLength(300);
            entity.Property(e => e.PhoneNumber)
                .HasMaxLength(20)
                .IsUnicode(false);
        });

        modelBuilder.Entity<User>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__Users__3214EC07C20D73A2");

            entity.HasIndex(e => e.Role, "IX_Users_Role");

            entity.HasIndex(e => e.Username, "UQ__Users__536C85E4DBDBAB74").IsUnique();

            entity.Property(e => e.CreatedAt).HasDefaultValueSql("(getdate())");
            entity.Property(e => e.Email)
                .HasMaxLength(150)
                .IsUnicode(false);
            entity.Property(e => e.FullName).HasMaxLength(100);
            entity.Property(e => e.IsActive).HasDefaultValue(true);
            entity.Property(e => e.PasswordHash)
                .HasMaxLength(256)
                .IsUnicode(false);
            entity.Property(e => e.PhoneNumber)
                .HasMaxLength(20)
                .IsUnicode(false);
            entity.Property(e => e.Role)
                .HasMaxLength(20)
                .IsUnicode(false);
            entity.Property(e => e.Username)
                .HasMaxLength(50)
                .IsUnicode(false);
        });

        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}
