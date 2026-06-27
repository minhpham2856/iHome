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

    public virtual DbSet<AppSetting> AppSettings { get; set; }

    public virtual DbSet<AuditLog> AuditLogs { get; set; }

    public virtual DbSet<Building> Buildings { get; set; }

    public virtual DbSet<Contract> Contracts { get; set; }

    public virtual DbSet<ContractTenant> ContractTenants { get; set; }

    public virtual DbSet<ElectricityTier> ElectricityTiers { get; set; }

    public virtual DbSet<Invoice> Invoices { get; set; }

    public virtual DbSet<InvoiceItem> InvoiceItems { get; set; }

    public virtual DbSet<NotificationLog> NotificationLogs { get; set; }

    public virtual DbSet<NotificationTemplate> NotificationTemplates { get; set; }

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
        => optionsBuilder.UseSqlServer("Server=DESKTOP-2JNFOT5;uid=sa;password=123;database=iHomeDB;Encrypt=True;TrustServerCertificate=True;");

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<AppSetting>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__AppSetti__3214EC077A4F71F5");

            entity.HasIndex(e => e.SettingKey, "UQ__AppSetti__01E719AD02FD6ED7").IsUnique();

            entity.Property(e => e.Description).HasMaxLength(500);
            entity.Property(e => e.SettingKey).HasMaxLength(100);
            entity.Property(e => e.UpdatedAt).HasDefaultValueSql("(getdate())");
        });

        modelBuilder.Entity<AuditLog>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__AuditLog__3214EC0754CA7DB0");

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
            entity.HasKey(e => e.Id).HasName("PK__Building__3214EC0737B0523A");

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
            entity.HasKey(e => e.Id).HasName("PK__Contract__3214EC07DA29C411");

            entity.HasIndex(e => e.RoomId, "IX_Contracts_RoomId");

            entity.HasIndex(e => new { e.StartDate, e.EndDate }, "IX_Contracts_StartEnd");

            entity.HasIndex(e => e.Status, "IX_Contracts_Status");

            entity.HasIndex(e => e.ContractCode, "UQ__Contract__CBECF8333B8DA1FF").IsUnique();

            entity.Property(e => e.ContractCode)
                .HasMaxLength(30)
                .IsUnicode(false);
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("(getdate())");
            entity.Property(e => e.DepositAmount).HasColumnType("decimal(15, 2)");
            entity.Property(e => e.MonthlyRent).HasColumnType("decimal(15, 2)");
            entity.Property(e => e.Status)
                .HasMaxLength(20)
                .IsUnicode(false);
            entity.Property(e => e.TerminationReason).HasMaxLength(300);

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

        modelBuilder.Entity<ElectricityTier>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__Electric__3214EC07566A1EEC");

            entity.HasIndex(e => e.BuildingId, "IX_ElectricityTiers_BuildingId");

            entity.Property(e => e.PricePerUnit).HasColumnType("decimal(15, 2)");

            entity.HasOne(d => d.Building).WithMany(p => p.ElectricityTiers)
                .HasForeignKey(d => d.BuildingId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_ElectricityTiers_Buildings");
        });

        modelBuilder.Entity<Invoice>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__Invoices__3214EC07A235776D");

            entity.HasIndex(e => e.ContractId, "IX_Invoices_ContractId");

            entity.HasIndex(e => e.DueDate, "IX_Invoices_DueDate");

            entity.HasIndex(e => e.Status, "IX_Invoices_Status");

            entity.HasIndex(e => new { e.ContractId, e.Month, e.Year }, "UQ_Invoices_ContractMonth").IsUnique();

            entity.Property(e => e.CreatedAt).HasDefaultValueSql("(getdate())");
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
            entity.HasKey(e => e.Id).HasName("PK__InvoiceI__3214EC07A475803E");

            entity.Property(e => e.Amount).HasColumnType("decimal(15, 2)");
            entity.Property(e => e.Description).HasMaxLength(200);
            entity.Property(e => e.Quantity).HasColumnType("decimal(15, 2)");
            entity.Property(e => e.UnitPrice).HasColumnType("decimal(15, 2)");

            entity.HasOne(d => d.Invoice).WithMany(p => p.InvoiceItems)
                .HasForeignKey(d => d.InvoiceId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_InvoiceItems_Invoices");
        });

        modelBuilder.Entity<NotificationLog>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__Notifica__3214EC070594B144");

            entity.HasIndex(e => e.RecipientId, "IX_NotificationLogs_RecipientId");

            entity.HasIndex(e => e.SentAt, "IX_NotificationLogs_SentAt");

            entity.Property(e => e.ErrorMessage).HasMaxLength(500);
            entity.Property(e => e.RecipientEmail)
                .HasMaxLength(150)
                .IsUnicode(false);
            entity.Property(e => e.RecipientPhone)
                .HasMaxLength(20)
                .IsUnicode(false);
            entity.Property(e => e.SentAt).HasDefaultValueSql("(getdate())");
            entity.Property(e => e.Status)
                .HasMaxLength(20)
                .IsUnicode(false);
            entity.Property(e => e.Subject).HasMaxLength(200);
            entity.Property(e => e.Type)
                .HasMaxLength(10)
                .IsUnicode(false);

            entity.HasOne(d => d.Recipient).WithMany(p => p.NotificationLogs)
                .HasForeignKey(d => d.RecipientId)
                .HasConstraintName("FK_NotificationLogs_Tenants");
        });

        modelBuilder.Entity<NotificationTemplate>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__Notifica__3214EC07BE9384CD");

            entity.Property(e => e.CreatedAt).HasDefaultValueSql("(getdate())");
            entity.Property(e => e.Name).HasMaxLength(100);
            entity.Property(e => e.Placeholders).HasMaxLength(500);
            entity.Property(e => e.Subject).HasMaxLength(200);
            entity.Property(e => e.Type)
                .HasMaxLength(10)
                .IsUnicode(false);
        });

        modelBuilder.Entity<Payment>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__Payments__3214EC078F3A37B0");

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
            entity.HasKey(e => e.Id).HasName("PK__Properti__3214EC07910CE272");

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
            entity.HasKey(e => e.Id).HasName("PK__Rooms__3214EC07A7456F68");

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
            entity.HasKey(e => e.Id).HasName("PK__RoomType__3214EC0772FA5606");

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
            entity.HasKey(e => e.Id).HasName("PK__Services__3214EC077A2FAB8D");

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
            entity.HasKey(e => e.Id).HasName("PK__ServiceR__3214EC0708234410");

            entity.HasIndex(e => new { e.Month, e.Year }, "IX_ServiceReadings_MonthYear");

            entity.HasIndex(e => new { e.RoomId, e.ServiceId }, "IX_ServiceReadings_RoomService");

            entity.HasIndex(e => new { e.RoomId, e.ServiceId, e.Month, e.Year }, "UQ_ServiceReadings_RoomServiceMonth").IsUnique();

            entity.Property(e => e.Consumption)
                .HasComputedColumnSql("([CurrentReading]-[PreviousReading])", false)
                .HasColumnType("decimal(16, 2)");
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("(getdate())");
            entity.Property(e => e.CurrentReading).HasColumnType("decimal(15, 2)");
            entity.Property(e => e.PreviousReading).HasColumnType("decimal(15, 2)");

            entity.HasOne(d => d.RoomService).WithMany(p => p.ServiceReadings)
                .HasForeignKey(d => new { d.RoomId, d.ServiceId })
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_ServiceReadings_RoomServices");
        });

        modelBuilder.Entity<Tenant>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__Tenants__3214EC07C3EDF358");

            entity.HasIndex(e => e.IdCardNumber, "UQ__Tenants__713A7B91DE688DB7").IsUnique();

            entity.Property(e => e.CreatedAt).HasDefaultValueSql("(getdate())");
            entity.Property(e => e.Email)
                .HasMaxLength(150)
                .IsUnicode(false);
            entity.Property(e => e.FullName).HasMaxLength(100);
            entity.Property(e => e.IdCardImagePath).HasMaxLength(500);
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
            entity.HasKey(e => e.Id).HasName("PK__Users__3214EC07BEECA2B9");

            entity.HasIndex(e => e.Role, "IX_Users_Role");

            entity.HasIndex(e => e.Username, "IX_Users_Username");

            entity.HasIndex(e => e.Username, "UQ__Users__536C85E40FC39C06").IsUnique();

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
