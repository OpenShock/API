using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;

namespace ShockLink.Common.ShockLinkDb;

public partial class ShockLinkContext : DbContext
{
    public ShockLinkContext()
    {
    }

    public ShockLinkContext(DbContextOptions<ShockLinkContext> options)
        : base(options)
    {
    }

    public virtual DbSet<Device> Devices { get; set; }

    public virtual DbSet<DeviceShare> DeviceShares { get; set; }

    public virtual DbSet<User> Users { get; set; }
    
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Device>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("devices_pkey");

            entity.ToTable("devices", "ShockLink");

            entity.Property(e => e.Id)
                .ValueGeneratedNever()
                .HasColumnName("id");
            entity.Property(e => e.Name)
                .HasMaxLength(64)
                .IsFixedLength()
                .HasColumnName("name");
            entity.Property(e => e.Owner).HasColumnName("owner");
            entity.Property(e => e.RfId).HasColumnName("rf_id");

            entity.HasOne(d => d.OwnerNavigation).WithMany(p => p.Devices)
                .HasForeignKey(d => d.Owner)
                .HasConstraintName("owner_user_id");
        });

        modelBuilder.Entity<DeviceShare>(entity =>
        {
            entity.HasKey(e => new { e.DeviceId, e.SharedWith }).HasName("device_shares_pkey");

            entity.ToTable("device_shares", "ShockLink");

            entity.Property(e => e.DeviceId).HasColumnName("device_id");
            entity.Property(e => e.SharedWith).HasColumnName("shared_with");
            entity.Property(e => e.CreatedOn)
                .HasDefaultValueSql("CURRENT_TIMESTAMP")
                .HasColumnType("timestamp without time zone")
                .HasColumnName("created_on");

            entity.HasOne(d => d.Device).WithMany(p => p.DeviceShares)
                .HasForeignKey(d => d.DeviceId)
                .HasConstraintName("ref_device_id");

            entity.HasOne(d => d.SharedWithNavigation).WithMany(p => p.DeviceShares)
                .HasForeignKey(d => d.SharedWith)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("shared_with_user_id");
        });

        modelBuilder.Entity<User>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("users_pkey");

            entity.ToTable("users", "ShockLink");

            entity.HasIndex(e => e.Email, "email").IsUnique();

            entity.HasIndex(e => e.Name, "username").IsUnique();

            entity.Property(e => e.Id)
                .ValueGeneratedNever()
                .HasColumnName("id");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("CURRENT_TIMESTAMP")
                .HasColumnType("timestamp without time zone")
                .HasColumnName("created_at");
            entity.Property(e => e.Email)
                .HasColumnType("character varying")
                .HasColumnName("email");
            entity.Property(e => e.EmailActived).HasColumnName("email_actived");
            entity.Property(e => e.Name)
                .HasColumnType("character varying")
                .HasColumnName("name");
            entity.Property(e => e.Password)
                .HasColumnType("character varying")
                .HasColumnName("password");
        });
    }
}
