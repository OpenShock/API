﻿// <auto-generated />
using System;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;
using OpenShock.Common.OpenShockDb;

#nullable disable

namespace OpenShock.Common.Migrations
{
    [DbContext(typeof(OpenShockContext))]
    [Migration("20240319160003_DbCleanup")]
    partial class DbCleanup
    {
        /// <inheritdoc />
        protected override void BuildTargetModel(ModelBuilder modelBuilder)
        {
#pragma warning disable 612, 618
            modelBuilder
                .HasAnnotation("Npgsql:CollationDefinition:public.ndcoll", "und-u-ks-level2,und-u-ks-level2,icu,False")
                .HasAnnotation("ProductVersion", "8.0.2")
                .HasAnnotation("Relational:MaxIdentifierLength", 63);

            NpgsqlModelBuilderExtensions.HasPostgresEnum(modelBuilder, "control_type", new[] { "sound", "vibrate", "shock", "stop" });
            NpgsqlModelBuilderExtensions.HasPostgresEnum(modelBuilder, "ota_update_status", new[] { "started", "running", "finished", "error", "timeout" });
            NpgsqlModelBuilderExtensions.HasPostgresEnum(modelBuilder, "permission_type", new[] { "shockers.use" });
            NpgsqlModelBuilderExtensions.HasPostgresEnum(modelBuilder, "rank_type", new[] { "user", "support", "staff", "admin", "system" });
            NpgsqlModelBuilderExtensions.HasPostgresEnum(modelBuilder, "shocker_model_type", new[] { "caiXianlin", "petTrainer" });
            NpgsqlModelBuilderExtensions.UseIdentityByDefaultColumns(modelBuilder);

            modelBuilder.Entity("OpenShock.Common.OpenShockDb.ApiToken", b =>
            {
                b.Property<Guid>("Id")
                    .HasColumnType("uuid")
                    .HasColumnName("id");

                b.Property<string>("CreatedByIp")
                    .IsRequired()
                    .HasColumnType("character varying")
                    .HasColumnName("created_by_ip");

                b.Property<DateTime>("CreatedOn")
                    .ValueGeneratedOnAdd()
                    .HasColumnType("timestamp with time zone")
                    .HasColumnName("created_on")
                    .HasDefaultValueSql("CURRENT_TIMESTAMP");

                b.Property<string>("Name")
                    .IsRequired()
                    .HasMaxLength(64)
                    .HasColumnType("character varying(64)")
                    .HasColumnName("name");

                b.Property<int[]>("Permissions")
                    .IsRequired()
                    .HasColumnType("permission_type[]")
                    .HasColumnName("permissions");

                b.Property<string>("Token")
                    .IsRequired()
                    .HasMaxLength(256)
                    .HasColumnType("character varying(256)")
                    .HasColumnName("token");

                b.Property<Guid>("UserId")
                    .HasColumnType("uuid")
                    .HasColumnName("user_id");

                b.Property<DateOnly?>("ValidUntil")
                    .HasColumnType("date")
                    .HasColumnName("valid_until");

                b.HasKey("Id")
                    .HasName("api_tokens_pkey");

                b.HasIndex("UserId");

                b.ToTable("api_tokens", (string)null);
            });

            modelBuilder.Entity("OpenShock.Common.OpenShockDb.Device", b =>
            {
                b.Property<Guid>("Id")
                    .HasColumnType("uuid")
                    .HasColumnName("id");

                b.Property<DateTime>("CreatedOn")
                    .ValueGeneratedOnAdd()
                    .HasColumnType("timestamp with time zone")
                    .HasColumnName("created_on")
                    .HasDefaultValueSql("CURRENT_TIMESTAMP");

                b.Property<string>("Name")
                    .IsRequired()
                    .HasColumnType("character varying")
                    .HasColumnName("name");

                b.Property<Guid>("Owner")
                    .HasColumnType("uuid")
                    .HasColumnName("owner");

                b.Property<string>("Token")
                    .IsRequired()
                    .HasMaxLength(256)
                    .HasColumnType("character varying(256)")
                    .HasColumnName("token");

                b.HasKey("Id")
                    .HasName("devices_pkey");

                b.HasIndex("Owner");

                b.ToTable("devices", (string)null);
            });

            modelBuilder.Entity("OpenShock.Common.OpenShockDb.DeviceOtaUpdate", b =>
            {
                b.Property<Guid>("Device")
                    .HasColumnType("uuid")
                    .HasColumnName("device");

                b.Property<int>("UpdateId")
                    .HasColumnType("integer")
                    .HasColumnName("update_id");

                b.Property<DateTime>("CreatedOn")
                    .ValueGeneratedOnAdd()
                    .HasColumnType("timestamp with time zone")
                    .HasColumnName("created_on")
                    .HasDefaultValueSql("CURRENT_TIMESTAMP");

                b.Property<string>("Message")
                    .HasColumnType("character varying")
                    .HasColumnName("message");

                b.Property<int>("Status")
                    .HasColumnType("ota_update_status")
                    .HasColumnName("status");

                b.Property<string>("Version")
                    .IsRequired()
                    .HasColumnType("character varying")
                    .HasColumnName("version");

                b.HasKey("Device", "UpdateId")
                    .HasName("device_ota_updates_pkey");

                b.HasIndex(new[] { "CreatedOn" }, "device_ota_updates_created_on_idx")
                    .HasAnnotation("Npgsql:StorageParameter:deduplicate_items", "true");

                b.ToTable("device_ota_updates", (string)null);
            });

            modelBuilder.Entity("OpenShock.Common.OpenShockDb.PasswordReset", b =>
            {
                b.Property<Guid>("Id")
                    .HasColumnType("uuid")
                    .HasColumnName("id");

                b.Property<DateTime>("CreatedOn")
                    .ValueGeneratedOnAdd()
                    .HasColumnType("timestamp with time zone")
                    .HasColumnName("created_on")
                    .HasDefaultValueSql("CURRENT_TIMESTAMP");

                b.Property<string>("Secret")
                    .IsRequired()
                    .HasColumnType("character varying")
                    .HasColumnName("secret");

                b.Property<DateTimeOffset?>("UsedOn")
                    .HasColumnType("time with time zone")
                    .HasColumnName("used_on");

                b.Property<Guid>("UserId")
                    .HasColumnType("uuid")
                    .HasColumnName("user_id");

                b.HasKey("Id")
                    .HasName("password_resets_pkey");

                b.HasIndex("UserId");

                b.ToTable("password_resets", (string)null);
            });

            modelBuilder.Entity("OpenShock.Common.OpenShockDb.Shocker", b =>
            {
                b.Property<Guid>("Id")
                    .HasColumnType("uuid")
                    .HasColumnName("id");

                b.Property<DateTime>("CreatedOn")
                    .ValueGeneratedOnAdd()
                    .HasColumnType("timestamp with time zone")
                    .HasColumnName("created_on")
                    .HasDefaultValueSql("CURRENT_TIMESTAMP");

                b.Property<Guid>("Device")
                    .HasColumnType("uuid")
                    .HasColumnName("device");

                b.Property<int>("Model")
                    .HasColumnType("shocker_model_type")
                    .HasColumnName("model");

                b.Property<string>("Name")
                    .IsRequired()
                    .HasMaxLength(64)
                    .HasColumnType("character varying(64)")
                    .HasColumnName("name");

                b.Property<bool>("Paused")
                    .ValueGeneratedOnAdd()
                    .HasColumnType("boolean")
                    .HasDefaultValue(false)
                    .HasColumnName("paused");

                b.Property<int>("RfId")
                    .HasColumnType("integer")
                    .HasColumnName("rf_id");

                b.HasKey("Id")
                    .HasName("shockers_pkey");

                b.HasIndex("Device");

                b.ToTable("shockers", (string)null);
            });

            modelBuilder.Entity("OpenShock.Common.OpenShockDb.ShockerControlLog", b =>
            {
                b.Property<Guid>("Id")
                    .HasColumnType("uuid")
                    .HasColumnName("id");

                b.Property<Guid?>("ControlledBy")
                    .HasColumnType("uuid")
                    .HasColumnName("controlled_by");

                b.Property<DateTime>("CreatedOn")
                    .ValueGeneratedOnAdd()
                    .HasColumnType("timestamp with time zone")
                    .HasColumnName("created_on")
                    .HasDefaultValueSql("CURRENT_TIMESTAMP");

                b.Property<string>("CustomName")
                    .HasColumnType("character varying")
                    .HasColumnName("custom_name");

                b.Property<long>("Duration")
                    .HasColumnType("bigint")
                    .HasColumnName("duration");

                b.Property<byte>("Intensity")
                    .HasColumnType("smallint")
                    .HasColumnName("intensity");

                b.Property<bool>("LiveControl")
                    .ValueGeneratedOnAdd()
                    .HasColumnType("boolean")
                    .HasDefaultValue(false)
                    .HasColumnName("live_control");

                b.Property<Guid>("ShockerId")
                    .HasColumnType("uuid")
                    .HasColumnName("shocker_id");

                b.Property<int>("Type")
                    .HasColumnType("control_type")
                    .HasColumnName("type");

                b.HasKey("Id")
                    .HasName("shocker_control_logs_pkey");

                b.HasIndex("ControlledBy");

                b.HasIndex("ShockerId");

                b.ToTable("shocker_control_logs", (string)null);
            });

            modelBuilder.Entity("OpenShock.Common.OpenShockDb.ShockerShare", b =>
            {
                b.Property<Guid>("ShockerId")
                    .HasColumnType("uuid")
                    .HasColumnName("shocker_id");

                b.Property<Guid>("SharedWith")
                    .HasColumnType("uuid")
                    .HasColumnName("shared_with");

                b.Property<DateTime>("CreatedOn")
                    .ValueGeneratedOnAdd()
                    .HasColumnType("timestamp with time zone")
                    .HasColumnName("created_on")
                    .HasDefaultValueSql("CURRENT_TIMESTAMP");

                b.Property<int?>("LimitDuration")
                    .HasColumnType("integer")
                    .HasColumnName("limit_duration");

                b.Property<byte?>("LimitIntensity")
                    .HasColumnType("smallint")
                    .HasColumnName("limit_intensity");

                b.Property<bool>("Paused")
                    .ValueGeneratedOnAdd()
                    .HasColumnType("boolean")
                    .HasDefaultValue(false)
                    .HasColumnName("paused");

                b.Property<bool>("PermLive")
                    .ValueGeneratedOnAdd()
                    .HasColumnType("boolean")
                    .HasDefaultValue(true)
                    .HasColumnName("perm_live");

                b.Property<bool>("PermShock")
                    .ValueGeneratedOnAdd()
                    .HasColumnType("boolean")
                    .HasDefaultValue(true)
                    .HasColumnName("perm_shock");

                b.Property<bool>("PermSound")
                    .ValueGeneratedOnAdd()
                    .HasColumnType("boolean")
                    .HasDefaultValue(true)
                    .HasColumnName("perm_sound");

                b.Property<bool>("PermVibrate")
                    .ValueGeneratedOnAdd()
                    .HasColumnType("boolean")
                    .HasDefaultValue(true)
                    .HasColumnName("perm_vibrate");

                b.HasKey("ShockerId", "SharedWith")
                    .HasName("shocker_shares_pkey");

                b.HasIndex("SharedWith");

                b.ToTable("shocker_shares", (string)null);
            });

            modelBuilder.Entity("OpenShock.Common.OpenShockDb.ShockerShareCode", b =>
            {
                b.Property<Guid>("Id")
                    .HasColumnType("uuid")
                    .HasColumnName("id");

                b.Property<DateTime>("CreatedOn")
                    .ValueGeneratedOnAdd()
                    .HasColumnType("timestamp with time zone")
                    .HasColumnName("created_on")
                    .HasDefaultValueSql("CURRENT_TIMESTAMP");

                b.Property<int?>("LimitDuration")
                    .HasColumnType("integer")
                    .HasColumnName("limit_duration");

                b.Property<byte?>("LimitIntensity")
                    .HasColumnType("smallint")
                    .HasColumnName("limit_intensity");

                b.Property<bool>("PermShock")
                    .ValueGeneratedOnAdd()
                    .HasColumnType("boolean")
                    .HasDefaultValue(true)
                    .HasColumnName("perm_shock");

                b.Property<bool>("PermSound")
                    .ValueGeneratedOnAdd()
                    .HasColumnType("boolean")
                    .HasDefaultValue(true)
                    .HasColumnName("perm_sound");

                b.Property<bool>("PermVibrate")
                    .ValueGeneratedOnAdd()
                    .HasColumnType("boolean")
                    .HasDefaultValue(true)
                    .HasColumnName("perm_vibrate");

                b.Property<Guid>("ShockerId")
                    .HasColumnType("uuid")
                    .HasColumnName("shocker_id");

                b.HasKey("Id")
                    .HasName("shocker_share_codes_pkey");

                b.HasIndex("ShockerId");

                b.ToTable("shocker_share_codes", (string)null);
            });

            modelBuilder.Entity("OpenShock.Common.OpenShockDb.ShockerSharesLink", b =>
            {
                b.Property<Guid>("Id")
                    .HasColumnType("uuid")
                    .HasColumnName("id");

                b.Property<DateTime>("CreatedOn")
                    .ValueGeneratedOnAdd()
                    .HasColumnType("timestamp with time zone")
                    .HasColumnName("created_on")
                    .HasDefaultValueSql("CURRENT_TIMESTAMP");

                b.Property<DateTime?>("ExpiresOn")
                    .HasColumnType("timestamp with time zone")
                    .HasColumnName("expires_on");

                b.Property<string>("Name")
                    .IsRequired()
                    .HasColumnType("character varying")
                    .HasColumnName("name");

                b.Property<Guid>("OwnerId")
                    .HasColumnType("uuid")
                    .HasColumnName("owner_id");

                b.HasKey("Id")
                    .HasName("shocker_shares_links_pkey");

                b.HasIndex("OwnerId");

                b.ToTable("shocker_shares_links", (string)null);
            });

            modelBuilder.Entity("OpenShock.Common.OpenShockDb.ShockerSharesLinksShocker", b =>
            {
                b.Property<Guid>("ShareLinkId")
                    .HasColumnType("uuid")
                    .HasColumnName("share_link_id");

                b.Property<Guid>("ShockerId")
                    .HasColumnType("uuid")
                    .HasColumnName("shocker_id");

                b.Property<int?>("Cooldown")
                    .HasColumnType("integer")
                    .HasColumnName("cooldown");

                b.Property<int?>("LimitDuration")
                    .HasColumnType("integer")
                    .HasColumnName("limit_duration");

                b.Property<byte?>("LimitIntensity")
                    .HasColumnType("smallint")
                    .HasColumnName("limit_intensity");

                b.Property<bool>("Paused")
                    .ValueGeneratedOnAdd()
                    .HasColumnType("boolean")
                    .HasDefaultValue(false)
                    .HasColumnName("paused");

                b.Property<bool>("PermLive")
                    .ValueGeneratedOnAdd()
                    .HasColumnType("boolean")
                    .HasDefaultValue(true)
                    .HasColumnName("perm_live");

                b.Property<bool>("PermShock")
                    .HasColumnType("boolean")
                    .HasColumnName("perm_shock");

                b.Property<bool>("PermSound")
                    .HasColumnType("boolean")
                    .HasColumnName("perm_sound");

                b.Property<bool>("PermVibrate")
                    .HasColumnType("boolean")
                    .HasColumnName("perm_vibrate");

                b.HasKey("ShareLinkId", "ShockerId")
                    .HasName("shocker_shares_links_shockers_pkey");

                b.HasIndex("ShockerId");

                b.ToTable("shocker_shares_links_shockers", (string)null);
            });

            modelBuilder.Entity("OpenShock.Common.OpenShockDb.User", b =>
            {
                b.Property<Guid>("Id")
                    .HasColumnType("uuid")
                    .HasColumnName("id");

                b.Property<DateTime>("CreatedAt")
                    .ValueGeneratedOnAdd()
                    .HasColumnType("timestamp with time zone")
                    .HasColumnName("created_at")
                    .HasDefaultValueSql("CURRENT_TIMESTAMP");

                b.Property<string>("Email")
                    .IsRequired()
                    .HasColumnType("character varying")
                    .HasColumnName("email");

                b.Property<bool>("EmailActived")
                    .ValueGeneratedOnAdd()
                    .HasColumnType("boolean")
                    .HasDefaultValue(false)
                    .HasColumnName("email_actived");

                b.Property<string>("Name")
                    .IsRequired()
                    .HasColumnType("character varying")
                    .HasColumnName("name")
                    .UseCollation("ndcoll");

                b.Property<string>("PasswordHash")
                    .IsRequired()
                    .HasColumnType("character varying")
                    .HasColumnName("password_hash");

                b.Property<int>("Rank")
                    .HasColumnType("rank_type")
                    .HasColumnName("rank");

                b.HasKey("Id")
                    .HasName("users_pkey");

                b.HasIndex(new[] { "Email" }, "email")
                    .IsUnique();

                b.HasIndex(new[] { "Email" }, "idx_email");

                b.HasIndex(new[] { "Name" }, "idx_name");

                NpgsqlIndexBuilderExtensions.UseCollation(b.HasIndex(new[] { "Name" }, "idx_name"), new[] { "ndcoll" });

                b.HasIndex(new[] { "Name" }, "username")
                    .IsUnique();

                b.ToTable("users", (string)null);
            });

            modelBuilder.Entity("OpenShock.Common.OpenShockDb.UsersActivation", b =>
            {
                b.Property<Guid>("Id")
                    .HasColumnType("uuid")
                    .HasColumnName("id");

                b.Property<DateTime>("CreatedOn")
                    .ValueGeneratedOnAdd()
                    .HasColumnType("timestamp with time zone")
                    .HasColumnName("created_on")
                    .HasDefaultValueSql("CURRENT_TIMESTAMP");

                b.Property<string>("Secret")
                    .IsRequired()
                    .HasColumnType("character varying")
                    .HasColumnName("secret");

                b.Property<DateTime?>("UsedOn")
                    .HasColumnType("timestamp with time zone")
                    .HasColumnName("used_on");

                b.Property<Guid>("UserId")
                    .HasColumnType("uuid")
                    .HasColumnName("user_id");

                b.HasKey("Id")
                    .HasName("users_activation_pkey");

                b.HasIndex("UserId");

                b.ToTable("users_activation", (string)null);
            });

            modelBuilder.Entity("OpenShock.Common.OpenShockDb.ApiToken", b =>
            {
                b.HasOne("OpenShock.Common.OpenShockDb.User", "User")
                    .WithMany("ApiTokens")
                    .HasForeignKey("UserId")
                    .OnDelete(DeleteBehavior.Cascade)
                    .IsRequired()
                    .HasConstraintName("fk_user_id");

                b.Navigation("User");
            });

            modelBuilder.Entity("OpenShock.Common.OpenShockDb.Device", b =>
            {
                b.HasOne("OpenShock.Common.OpenShockDb.User", "OwnerNavigation")
                    .WithMany("Devices")
                    .HasForeignKey("Owner")
                    .OnDelete(DeleteBehavior.Cascade)
                    .IsRequired()
                    .HasConstraintName("owner_user_id");

                b.Navigation("OwnerNavigation");
            });

            modelBuilder.Entity("OpenShock.Common.OpenShockDb.DeviceOtaUpdate", b =>
            {
                b.HasOne("OpenShock.Common.OpenShockDb.Device", "DeviceNavigation")
                    .WithMany("DeviceOtaUpdates")
                    .HasForeignKey("Device")
                    .OnDelete(DeleteBehavior.Cascade)
                    .IsRequired()
                    .HasConstraintName("device_ota_updates_device");

                b.Navigation("DeviceNavigation");
            });

            modelBuilder.Entity("OpenShock.Common.OpenShockDb.PasswordReset", b =>
            {
                b.HasOne("OpenShock.Common.OpenShockDb.User", "User")
                    .WithMany("PasswordResets")
                    .HasForeignKey("UserId")
                    .OnDelete(DeleteBehavior.Cascade)
                    .IsRequired()
                    .HasConstraintName("user_id");

                b.Navigation("User");
            });

            modelBuilder.Entity("OpenShock.Common.OpenShockDb.Shocker", b =>
            {
                b.HasOne("OpenShock.Common.OpenShockDb.Device", "DeviceNavigation")
                    .WithMany("Shockers")
                    .HasForeignKey("Device")
                    .OnDelete(DeleteBehavior.Cascade)
                    .IsRequired()
                    .HasConstraintName("device_id");

                b.Navigation("DeviceNavigation");
            });

            modelBuilder.Entity("OpenShock.Common.OpenShockDb.ShockerControlLog", b =>
            {
                b.HasOne("OpenShock.Common.OpenShockDb.User", "ControlledByNavigation")
                    .WithMany("ShockerControlLogs")
                    .HasForeignKey("ControlledBy")
                    .OnDelete(DeleteBehavior.Cascade)
                    .HasConstraintName("fk_controlled_by");

                b.HasOne("OpenShock.Common.OpenShockDb.Shocker", "Shocker")
                    .WithMany("ShockerControlLogs")
                    .HasForeignKey("ShockerId")
                    .OnDelete(DeleteBehavior.Cascade)
                    .IsRequired()
                    .HasConstraintName("fk_shocker_id");

                b.Navigation("ControlledByNavigation");

                b.Navigation("Shocker");
            });

            modelBuilder.Entity("OpenShock.Common.OpenShockDb.ShockerShare", b =>
            {
                b.HasOne("OpenShock.Common.OpenShockDb.User", "SharedWithNavigation")
                    .WithMany("ShockerShares")
                    .HasForeignKey("SharedWith")
                    .OnDelete(DeleteBehavior.Cascade)
                    .IsRequired()
                    .HasConstraintName("shared_with_user_id");

                b.HasOne("OpenShock.Common.OpenShockDb.Shocker", "Shocker")
                    .WithMany("ShockerShares")
                    .HasForeignKey("ShockerId")
                    .OnDelete(DeleteBehavior.Cascade)
                    .IsRequired()
                    .HasConstraintName("ref_shocker_id");

                b.Navigation("SharedWithNavigation");

                b.Navigation("Shocker");
            });

            modelBuilder.Entity("OpenShock.Common.OpenShockDb.ShockerShareCode", b =>
            {
                b.HasOne("OpenShock.Common.OpenShockDb.Shocker", "Shocker")
                    .WithMany("ShockerShareCodes")
                    .HasForeignKey("ShockerId")
                    .OnDelete(DeleteBehavior.Cascade)
                    .IsRequired()
                    .HasConstraintName("fk_shocker_id");

                b.Navigation("Shocker");
            });

            modelBuilder.Entity("OpenShock.Common.OpenShockDb.ShockerSharesLink", b =>
            {
                b.HasOne("OpenShock.Common.OpenShockDb.User", "Owner")
                    .WithMany("ShockerSharesLinks")
                    .HasForeignKey("OwnerId")
                    .OnDelete(DeleteBehavior.Cascade)
                    .IsRequired()
                    .HasConstraintName("owner_id");

                b.Navigation("Owner");
            });

            modelBuilder.Entity("OpenShock.Common.OpenShockDb.ShockerSharesLinksShocker", b =>
            {
                b.HasOne("OpenShock.Common.OpenShockDb.ShockerSharesLink", "ShareLink")
                    .WithMany("ShockerSharesLinksShockers")
                    .HasForeignKey("ShareLinkId")
                    .OnDelete(DeleteBehavior.Cascade)
                    .IsRequired()
                    .HasConstraintName("share_link_id");

                b.HasOne("OpenShock.Common.OpenShockDb.Shocker", "Shocker")
                    .WithMany("ShockerSharesLinksShockers")
                    .HasForeignKey("ShockerId")
                    .OnDelete(DeleteBehavior.Cascade)
                    .IsRequired()
                    .HasConstraintName("shocker_id");

                b.Navigation("ShareLink");

                b.Navigation("Shocker");
            });

            modelBuilder.Entity("OpenShock.Common.OpenShockDb.UsersActivation", b =>
            {
                b.HasOne("OpenShock.Common.OpenShockDb.User", "User")
                    .WithMany("UsersActivations")
                    .HasForeignKey("UserId")
                    .OnDelete(DeleteBehavior.Cascade)
                    .IsRequired()
                    .HasConstraintName("user_id");

                b.Navigation("User");
            });

            modelBuilder.Entity("OpenShock.Common.OpenShockDb.Device", b =>
            {
                b.Navigation("DeviceOtaUpdates");

                b.Navigation("Shockers");
            });

            modelBuilder.Entity("OpenShock.Common.OpenShockDb.Shocker", b =>
            {
                b.Navigation("ShockerControlLogs");

                b.Navigation("ShockerShareCodes");

                b.Navigation("ShockerShares");

                b.Navigation("ShockerSharesLinksShockers");
            });

            modelBuilder.Entity("OpenShock.Common.OpenShockDb.ShockerSharesLink", b =>
            {
                b.Navigation("ShockerSharesLinksShockers");
            });

            modelBuilder.Entity("OpenShock.Common.OpenShockDb.User", b =>
            {
                b.Navigation("ApiTokens");

                b.Navigation("Devices");

                b.Navigation("PasswordResets");

                b.Navigation("ShockerControlLogs");

                b.Navigation("ShockerShares");

                b.Navigation("ShockerSharesLinks");

                b.Navigation("UsersActivations");
            });
#pragma warning restore 612, 618
        }
    }
}