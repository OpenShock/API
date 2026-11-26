using System.Text.Json;
using Microsoft.AspNetCore.DataProtection.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using OpenShock.Common.Constants;
using OpenShock.Common.Models;
using OpenShock.Common.OpenShockDb.Converters;
using OpenShock.Internal.Common.Constants;

namespace OpenShock.Common.OpenShockDb;

/// <summary>
/// This is meant for use in migrations only.
/// </summary>
public sealed class MigrationOpenShockContext : OpenShockContext
{
    private readonly string _connectionString;
    private readonly bool _debug;
    private readonly ILoggerFactory? _loggerFactory;

    public MigrationOpenShockContext(string connectionString, bool debug, ILoggerFactory? loggerFactory = null)
    {
        _connectionString = connectionString;
        _debug = debug;
        _loggerFactory = loggerFactory;
    }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        ConfigureOptionsBuilder(optionsBuilder, _connectionString, _debug);

        if (_loggerFactory is not null)
            optionsBuilder.UseLoggerFactory(_loggerFactory);
    }
}

/// <summary>
/// Main OpenShock DB Context
/// </summary>
public class OpenShockContext : DbContext, IDataProtectionKeyContext
{
    protected OpenShockContext()
    {
    }

    public OpenShockContext(DbContextOptions<OpenShockContext> options)
        : base(options)
    {
    }
    
    public static void ConfigureOptionsBuilder(DbContextOptionsBuilder optionsBuilder, string connectionString,
        bool debug)
    {
        optionsBuilder.UseNpgsql(connectionString, npgsqlBuilder =>
        {
            // Required so Npgsql can serialize the email outbox's Dictionary<string,string> payload
            // to its jsonb column. Without dynamic JSON, Npgsql maps Dictionary<string,string> to
            // hstore by default and writing it to a jsonb column fails at runtime.
            npgsqlBuilder.ConfigureDataSource(dataSourceBuilder => dataSourceBuilder.EnableDynamicJson());
            npgsqlBuilder.MapPgEnums();
        });

        if (debug)
        {
            optionsBuilder.EnableSensitiveDataLogging();
            optionsBuilder.EnableDetailedErrors();
        }
    }

    public DbSet<ApiToken> ApiTokens { get; set; }

    public DbSet<ApiTokenReport> ApiTokenReports { get; set; }

    public DbSet<Device> Devices { get; set; }

    public DbSet<DeviceOtaUpdate> DeviceOtaUpdates { get; set; }

    public DbSet<UserPasswordReset> UserPasswordResets { get; set; }

    public DbSet<UserShareInvite> UserShareInvites { get; set; }

    public DbSet<UserShareInviteShocker> UserShareInviteShockers { get; set; }

    public DbSet<Shocker> Shockers { get; set; }

    public DbSet<ShockerControlLog> ShockerControlLogs { get; set; }

    public DbSet<ControlStat> ControlStats { get; set; }

    public DbSet<UserShare> UserShares { get; set; }

    public DbSet<ShockerShareCode> ShockerShareCodes { get; set; }

    public DbSet<PublicShare> PublicShares { get; set; }

    public DbSet<PublicShareShocker> PublicShareShockerMappings { get; set; }

    public DbSet<User> Users { get; set; }
    
    public DbSet<UserOAuthConnection> UserOAuthConnections { get; set; }

    public DbSet<UserActivationRequest> UserActivationRequests { get; set; }

    public DbSet<UserDeactivation> UserDeactivations { get; set; }

    public DbSet<UserEmailChange> UserEmailChanges { get; set; }

    public DbSet<UserNameChange> UserNameChanges { get; set; }
    
    public DbSet<DiscordWebhook> DiscordWebhooks { get; set; }

    public DbSet<ConfigurationItem> Configuration { get; set; }

    public DbSet<AdminUsersView> AdminUsersViews { get; set; }

    public DbSet<UserNameBlacklist> UserNameBlacklists { get; set; }

    public DbSet<EmailProviderBlacklist> EmailProviderBlacklists { get; set; }

    public DbSet<EmailOutboxMessage> EmailOutbox { get; set; }

    public DbSet<AutomationToken> AutomationTokens { get; set; }
    
    public DbSet<DataProtectionKey> DataProtectionKeys { get; set; }

    public DbSet<UserAuditLog> UserAuditLogs { get; set; }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        if (!optionsBuilder.IsConfigured)
        {
            optionsBuilder.UseNpgsql("Host=localhost;Database=openshock;Username=openshock;Password=openshock");
        }
    }

    protected override void ConfigureConventions(ModelConfigurationBuilder configurationBuilder)
    {
        // An out of range stored value clamps on read, it never wraps. See SaturatingConverters.
        configurationBuilder.Properties<ushort>().HaveConversion<SaturatingUInt16Converter>();
        configurationBuilder.Properties<uint>().HaveConversion<SaturatingUInt32Converter>();
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder
            .RegisterPgEnums()
            .HasPostgresExtension("timescaledb") // Community edition
            .HasCollation("public", "ndcoll", "und-u-ks-level2", "icu", false); // Add case-insensitive, accent-sensitive comparison collation

        modelBuilder.Entity<ApiToken>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("api_tokens_pkey");

            entity.ToTable("api_tokens", t =>
            {
                t.HasCheckConstraint("ck_api_tokens_shocker_control_intensity",
                    $"shocker_control_intensity_min >= {HardLimits.MinControlIntensity} AND shocker_control_intensity_max <= {HardLimits.MaxControlIntensity} AND shocker_control_intensity_min <= shocker_control_intensity_max");
                t.HasCheckConstraint("ck_api_tokens_shocker_control_duration",
                    $"shocker_control_duration_min >= {HardLimits.MinControlDuration} AND shocker_control_duration_max <= {HardLimits.MaxControlDuration} AND shocker_control_duration_min <= shocker_control_duration_max");
            });

            entity.HasIndex(e => e.UserId);
            entity.HasIndex(e => e.ValidUntil);
            entity.HasIndex(e => e.TokenHash).IsUnique();

            entity.Property(e => e.Id)
                .ValueGeneratedOnAdd()
                .HasColumnName("id");
            entity.Property(e => e.CreatedByIp)
                .HasColumnName("created_by_ip");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("CURRENT_TIMESTAMP")
                .HasColumnName("created_at");
            entity.Property(e => e.LastUsed)
                .HasColumnName("last_used");
            entity.Property(e => e.Name)
                .HasMaxLength(ApiHardLimits.ApiKeyNameMaxLength)
                .HasColumnName("name");
            entity.Property(e => e.TokenHash)
                .UseCollation("C")
                .HasMaxLength(HardLimits.Sha256HashHexLength)
                .HasColumnName("token_hash");
            entity.Property(e => e.UserId).HasColumnName("user_id");
            entity.Property(e => e.ValidUntil).HasColumnName("valid_until");

            entity.Property(e => e.Permissions).HasColumnType("permission_type[]").HasColumnName("permissions");

            entity.Property(e => e.ShockerControlPaused)
                .HasDefaultValue(false)
                .HasColumnName("shocker_control_paused");
            entity.Property(e => e.ShockerControlIntensityMin)
                .HasDefaultValue(HardLimits.MinControlIntensity)
                .HasColumnName("shocker_control_intensity_min");
            entity.Property(e => e.ShockerControlIntensityMax)
                .HasDefaultValue(HardLimits.MaxControlIntensity)
                .HasColumnName("shocker_control_intensity_max");
            entity.Property(e => e.ShockerControlIntensityMode)
                .HasColumnType("control_limit_mode")
                .HasDefaultValue(ControlLimitMode.Clamp)
                .HasColumnName("shocker_control_intensity_mode");
            entity.Property(e => e.ShockerControlDurationMin)
                .HasDefaultValue(HardLimits.MinControlDuration)
                .HasColumnName("shocker_control_duration_min");
            entity.Property(e => e.ShockerControlDurationMax)
                .HasDefaultValue(HardLimits.MaxControlDuration)
                .HasColumnName("shocker_control_duration_max");
            entity.Property(e => e.ShockerControlDurationMode)
                .HasColumnType("control_limit_mode")
                .HasDefaultValue(ControlLimitMode.Clamp)
                .HasColumnName("shocker_control_duration_mode");

            entity.HasOne(d => d.User).WithMany(p => p.ApiTokens)
                .HasForeignKey(d => d.UserId)
                .HasConstraintName("fk_api_tokens_user_id");
        });

        modelBuilder.Entity<ApiTokenReport>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("api_token_reports_pkey");

            entity.ToTable("api_token_reports");

            entity.Property(e => e.Id)
                .ValueGeneratedOnAdd()
                .HasColumnName("id");
            entity.Property(e => e.SubmittedCount)
                .HasColumnName("submitted_count");
            entity.Property(e => e.AffectedCount)
                .HasColumnName("affected_count");
            entity.Property(e => e.UserId)
                .HasColumnName("user_id");
            entity.Property(e => e.IpAddress)
                .HasColumnName("ip_address");
            entity.Property(e => e.IpCountry)
                .HasMaxLength(4)
                .HasColumnName("ip_country");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("CURRENT_TIMESTAMP")
                .HasColumnName("created_at");

            entity.HasOne(r => r.ReportedByUser).WithMany(u => u.ReportedApiTokens)
                .HasForeignKey(r => r.UserId)
                .HasConstraintName("fk_api_token_reports_reported_by_user_id");
        });

        modelBuilder.Entity<Device>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("devices_pkey");

            entity.ToTable("devices");

            entity.HasIndex(e => e.OwnerId);
            entity.HasIndex(e => e.Token).IsUnique();

            entity.Property(e => e.Id)
                .ValueGeneratedOnAdd()
                .HasColumnName("id");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("CURRENT_TIMESTAMP")
                .HasColumnName("created_at");
            entity.Property(e => e.Name)
                .HasMaxLength(HardLimits.HubNameMaxLength)
                .HasColumnName("name");
            entity.Property(e => e.OwnerId).HasColumnName("owner_id");
            entity.Property(e => e.Token)
                .UseCollation("C")
                .HasMaxLength(ApiHardLimits.HubTokenMaxLength)
                .HasColumnName("token");

            entity.HasOne(d => d.Owner).WithMany(p => p.Devices)
                .HasForeignKey(d => d.OwnerId)
                .HasConstraintName("fk_devices_owner_id");
        });

        modelBuilder.Entity<DeviceOtaUpdate>(entity =>
        {
            entity.HasKey(e => new { e.DeviceId, e.UpdateId }).HasName("device_ota_updates_pkey");

            entity.ToTable("device_ota_updates");

            entity.HasIndex(e => e.CreatedAt, "device_ota_updates_created_at_idx");

            entity.Property(e => e.DeviceId).HasColumnName("device_id");
            entity.Property(e => e.UpdateId).HasColumnName("update_id");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("CURRENT_TIMESTAMP")
                .HasColumnName("created_at");
            entity.Property(e => e.Message)
                .HasMaxLength(ApiHardLimits.OtaUpdateMessageMaxLength)
                .HasColumnName("message");
            entity.Property(e => e.Version)
                .HasMaxLength(HardLimits.SemVerMaxLength)
                .HasColumnName("version");

            entity.Property(e => e.Status).HasColumnType("ota_update_status").HasColumnName("status");


            entity.HasOne(d => d.Device).WithMany(p => p.OtaUpdates)
                .HasForeignKey(d => d.DeviceId)
                .HasConstraintName("fk_device_ota_updates_device_id");
        });

        modelBuilder.Entity<UserPasswordReset>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("user_password_resets_pkey");

            entity.ToTable("user_password_resets");

            entity.HasIndex(e => e.UserId);

            entity.Property(e => e.Id)
                .ValueGeneratedOnAdd()
                .HasColumnName("id");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("CURRENT_TIMESTAMP")
                .HasColumnName("created_at");
            entity.Property(e => e.TokenHash)
                .UseCollation("C")
                .HasMaxLength(ApiHardLimits.PasswordResetSecretMaxLength)
                .HasColumnName("token_hash");
            entity.Property(e => e.SecurityStampAtCreate)
                .HasColumnName("security_stamp_at_create");
            entity.Property(e => e.UsedAt)
                .HasColumnName("used_at");
            entity.Property(e => e.UserId).HasColumnName("user_id");

            entity.HasOne(d => d.User).WithMany(p => p.PasswordResets)
                .HasForeignKey(d => d.UserId)
                .HasConstraintName("fk_user_password_resets_user_id");
        });

        modelBuilder.Entity<UserShareInvite>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("user_share_invites_pkey");

            entity.ToTable("user_share_invites");

            entity.HasIndex(e => e.OwnerId);

            entity.Property(e => e.Id)
                .ValueGeneratedOnAdd()
                .HasColumnName("id");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("CURRENT_TIMESTAMP")
                .HasColumnName("created_at");
            entity.Property(e => e.OwnerId).HasColumnName("owner_id");
            entity.Property(e => e.RecipientUserId).HasColumnName("user_id");

            entity.HasOne(d => d.Owner).WithMany(p => p.OutgoingUserShareInvites)
                .HasForeignKey(d => d.OwnerId)
                .HasConstraintName("fk_user_share_invites_owner_id");

            entity.HasOne(d => d.RecipientUser).WithMany(p => p.IncomingUserShareInvites)
                .HasForeignKey(d => d.RecipientUserId)
                .OnDelete(DeleteBehavior.Cascade)
                .HasConstraintName("fk_user_share_invites_recipient_user_id");
        });

        modelBuilder.Entity<UserShareInviteShocker>(entity =>
        {
            entity.HasKey(e => new { e.InviteId, e.ShockerId }).HasName("user_share_invite_shockers_pkey");

            entity.ToTable("user_share_invite_shockers");

            entity.Property(e => e.InviteId).HasColumnName("invite_id");
            entity.Property(e => e.ShockerId).HasColumnName("shocker_id");
            ConfigureSafetySettings(entity);

            entity.HasOne(d => d.Invite).WithMany(p => p.ShockerMappings)
                .HasForeignKey(d => d.InviteId)
                .HasConstraintName("fk_user_share_invite_shockers_invite_id");

            entity.HasOne(d => d.Shocker).WithMany(p => p.UserShareInviteShockerMappings)
                .HasForeignKey(d => d.ShockerId)
                .HasConstraintName("fk_user_share_invite_shockers_shocker_id");
        });

        modelBuilder.Entity<Shocker>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("shockers_pkey");

            entity.ToTable("shockers", t =>
                t.HasCheckConstraint("ck_shockers_rf_id", $"rf_id BETWEEN {ushort.MinValue} AND {ushort.MaxValue}"));

            entity.HasIndex(e => e.DeviceId);

            entity.Property(e => e.Id)
                .ValueGeneratedOnAdd()
                .HasColumnName("id");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("CURRENT_TIMESTAMP")
                .HasColumnName("created_at");
            entity.Property(e => e.DeviceId).HasColumnName("device_id");
            entity.Property(e => e.Name)
                .HasMaxLength(HardLimits.ShockerNameMaxLength)
                .HasColumnName("name");
            entity.Property(e => e.IsPaused)
                .HasDefaultValue(false)
                .HasColumnName("is_paused");
            entity.Property(e => e.Model).HasColumnType("shocker_model_type").HasColumnName("model");
            entity.Property(e => e.RfId).HasColumnName("rf_id");

            entity.HasOne(d => d.Device).WithMany(p => p.Shockers)
                .HasForeignKey(d => d.DeviceId)
                .HasConstraintName("fk_shockers_device_id");
        });

        modelBuilder.Entity<ShockerControlLog>(entity =>
        {
            // A hypertable's unique indexes must include the column it is partitioned by
            entity.HasKey(e => new { e.Id, e.CreatedAt }).HasName("shocker_control_logs_pkey");

            entity.ToTable("shocker_control_logs");

            entity.HasIndex(e => new { e.ShockerId, e.CreatedAt });
            entity.HasIndex(e => new { e.OwnerUserId, e.CreatedAt });
            entity.HasIndex(e => e.ControlledByUserId);

            entity.Property(e => e.Id)
                .ValueGeneratedOnAdd()
                .HasColumnName("id");
            entity.Property(e => e.ControlledByUserId).HasColumnName("controlled_by_user_id");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("CURRENT_TIMESTAMP")
                .HasColumnName("created_at");
            entity.Property(e => e.CustomName)
                .HasMaxLength(HardLimits.ShockerControlLogCustomNameMaxLength)
                .HasColumnName("custom_name");
            entity.Property(e => e.Duration).HasColumnName("duration");
            entity.Property(e => e.Intensity).HasColumnName("intensity");
            entity.Property(e => e.MaxIntensity).HasColumnName("max_intensity");
            entity.Property(e => e.LiveControl)
                .HasDefaultValue(false)
                .HasColumnName("live_control");
            entity.Property(e => e.OwnerUserId).HasColumnName("owner_user_id");
            entity.Property(e => e.ShockerId).HasColumnName("shocker_id");
            entity.Property(e => e.Type).HasColumnType("control_type").HasColumnName("type");
            entity.Property(e => e.InsertedAt)
                .HasDefaultValueSql("now()")
                .HasColumnName("inserted_at");

            // Only the owner, the logs outlive their shocker and controller
            entity.HasOne<User>().WithMany()
                .HasForeignKey(e => e.OwnerUserId)
                .OnDelete(DeleteBehavior.Cascade)
                .HasConstraintName("fk_shocker_control_logs_owner_user_id");
        });

        modelBuilder.Entity<ControlStat>(entity =>
        {
            entity.HasNoKey().ToView("control_stats");

            entity.Property(e => e.Bucket).HasColumnName("bucket");
            entity.Property(e => e.OwnerUserId).HasColumnName("owner_user_id");
            entity.Property(e => e.ShockerId).HasColumnName("shocker_id");
            entity.Property(e => e.ControllerUserId).HasColumnName("controller_user_id");
            entity.Property(e => e.Type).HasColumnType("control_type").HasColumnName("type");
            entity.Property(e => e.Live).HasColumnName("live");
            entity.Property(e => e.Count).HasColumnName("count");
            entity.Property(e => e.DurationMs).HasColumnName("duration_ms");
            entity.Property(e => e.IntensityMs).HasColumnName("intensity_ms");
            entity.Property(e => e.MaxIntensity).HasColumnName("max_intensity");
        });

        modelBuilder.Entity<UserShare>(entity =>
        {
            entity.HasKey(e => new { e.SharedWithUserId, e.ShockerId }).HasName("user_shares_pkey");

            entity.ToTable("user_shares");

            entity.Property(e => e.SharedWithUserId).HasColumnName("shared_with_user_id");
            entity.Property(e => e.ShockerId).HasColumnName("shocker_id");
            ConfigureSafetySettings(entity);
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("CURRENT_TIMESTAMP")
                .HasColumnName("created_at");

            entity.HasOne(d => d.SharedWithUser).WithMany(p => p.IncomingUserShares)
                .HasForeignKey(d => d.SharedWithUserId)
                .HasConstraintName("fk_user_shares_shared_with_user_id");

            entity.HasOne(d => d.Shocker).WithMany(p => p.UserShares)
                .HasForeignKey(d => d.ShockerId)
                .HasConstraintName("fk_user_shares_shocker_id");
        });

        modelBuilder.Entity<ShockerShareCode>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("shocker_share_codes_pkey");

            entity.ToTable("shocker_share_codes");

            entity.Property(e => e.Id)
                .ValueGeneratedOnAdd()
                .HasColumnName("id");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("CURRENT_TIMESTAMP")
                .HasColumnName("created_at");
            ConfigureSafetySettings(entity);
            entity.Property(e => e.ShockerId).HasColumnName("shocker_id");

            entity.HasOne(d => d.Shocker).WithMany(p => p.ShockerShareCodes)
                .HasForeignKey(d => d.ShockerId)
                .HasConstraintName("fk_shocker_share_codes_shocker_id");
        });

        modelBuilder.Entity<PublicShare>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("public_shares_pkey");

            entity.ToTable("public_shares");

            entity.HasIndex(e => e.OwnerId);

            entity.Property(e => e.Id)
                .ValueGeneratedOnAdd()
                .HasColumnName("id");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("CURRENT_TIMESTAMP")
                .HasColumnName("created_at");
            entity.Property(e => e.ExpiresAt).HasColumnName("expires_at");
            entity.Property(e => e.Name)
                .HasMaxLength(HardLimits.PublicShareNameMaxLength)
                .HasColumnName("name");
            entity.Property(e => e.OwnerId).HasColumnName("owner_id");

            entity.HasOne(d => d.Owner).WithMany(p => p.OwnedPublicShares)
                .HasForeignKey(d => d.OwnerId)
                .HasConstraintName("fk_public_shares_owner_id");
        });

        modelBuilder.Entity<PublicShareShocker>(entity =>
        {
            entity.HasKey(e => new { e.PublicShareId, e.ShockerId }).HasName("public_share_shockers_pkey");

            entity.ToTable("public_share_shockers", t =>
                t.HasCheckConstraint("ck_public_share_shockers_cooldown", "cooldown >= 0"));

            entity.Property(e => e.PublicShareId).HasColumnName("public_share_id");
            entity.Property(e => e.ShockerId).HasColumnName("shocker_id");
            entity.Property(e => e.Cooldown).HasColumnName("cooldown");
            ConfigureSafetySettings(entity, allowLiveControlByDefault: false);

            entity.HasOne(d => d.PublicShare).WithMany(p => p.ShockerMappings)
                .HasForeignKey(d => d.PublicShareId)
                .HasConstraintName("fk_public_share_shockers_public_share_id");

            entity.HasOne(d => d.Shocker).WithMany(p => p.PublicShareMappings)
                .HasForeignKey(d => d.ShockerId)
                .HasConstraintName("fk_public_share_shockers_shocker_id");
        });

        modelBuilder.Entity<User>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("users_pkey");

            entity.ToTable("users");

            entity.HasIndex(e => e.Email).UseCollation("ndcoll").IsUnique();
            entity.HasIndex(e => e.Name).UseCollation("ndcoll").IsUnique();

            entity.Property(e => e.Id)
                .ValueGeneratedOnAdd()
                .HasColumnName("id");
            entity.Property(e => e.Name)
                .UseCollation("ndcoll")
                .HasMaxLength(HardLimits.UsernameMaxLength)
                .HasColumnName("name");
            entity.Property(e => e.Email)
                .UseCollation("ndcoll")
                .HasMaxLength(HardLimits.EmailAddressMaxLength)
                .HasColumnName("email");
            entity.Property(e => e.PasswordHash)
                .UseCollation("C")
                .HasMaxLength(ApiHardLimits.PasswordHashMaxLength)
                .HasColumnName("password_hash");
            entity.Property(e => e.SecurityStamp)
                .HasDefaultValueSql("gen_random_uuid()")
                .HasColumnName("security_stamp");
            entity.Property(e => e.Roles)
                .HasColumnType("role_type[]")
                .HasColumnName("roles");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("CURRENT_TIMESTAMP")
                .HasColumnName("created_at");
            entity.Property(e => e.ActivatedAt)
                .HasColumnName("activated_at");
            entity.Property(e => e.CreatedByAutomationTokenId)
                .HasColumnName("created_by_automation_token_id");

            entity.HasIndex(e => e.CreatedByAutomationTokenId);

            entity.HasOne(d => d.CreatedByAutomationToken).WithMany(p => p.CreatedUsers)
                .HasForeignKey(d => d.CreatedByAutomationTokenId)
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("fk_users_created_by_automation_token_id");
        });

        modelBuilder.Entity<UserOAuthConnection>(entity =>
        {
            entity.HasKey(e => new { e.ProviderKey, e.ExternalId }).HasName("user_oauth_connections_pkey");

            entity.HasIndex(e => e.UserId);

            entity.ToTable("user_oauth_connections");

            entity.Property(e => e.UserId)
                .HasColumnName("user_id");
            entity.Property(e => e.ProviderKey)
                .UseCollation("C")
                .HasMaxLength(ApiHardLimits.OAuthProviderKeyMaxLength)
                .HasColumnName("provider_key");
            entity.Property(e => e.ExternalId)
                .HasMaxLength(ApiHardLimits.OAuthExternalIdMaxLength)
                .HasColumnName("external_id");
            entity.Property(e => e.DisplayName)
                .HasMaxLength(ApiHardLimits.OAuthDisplayNameMaxLength)
                .HasColumnName("display_name");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("CURRENT_TIMESTAMP")
                .HasColumnName("created_at");

            entity.HasOne(c => c.User).WithMany(u => u.OAuthConnections)
                .HasForeignKey(d => d.UserId)
                .HasConstraintName("fk_user_oauth_connections_user_id");
        });

        modelBuilder.Entity<UserActivationRequest>(entity =>
        {
            entity.HasKey(e => e.UserId).HasName("user_activation_requests_pkey");

            entity.HasIndex(e => e.TokenHash).IsUnique();

            entity.ToTable("user_activation_requests");

            entity.Property(e => e.UserId)
                .HasColumnName("user_id");
            entity.Property(e => e.TokenHash)
                .UseCollation("C")
                .HasMaxLength(ApiHardLimits.UserActivationRequestSecretMaxLength)
                .HasColumnName("token_hash");
            entity.Property(e => e.EmailSendAttempts)
                .HasColumnName("email_send_attempts");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("CURRENT_TIMESTAMP")
                .HasColumnName("created_at");

            entity.HasOne(d => d.User).WithOne(p => p.UserActivationRequest)
                .HasForeignKey<UserActivationRequest>(d => d.UserId)
                .HasConstraintName("fk_user_activation_requests_user_id");
        });

        modelBuilder.Entity<UserDeactivation>(entity =>
        {
            entity.HasKey(e => e.DeactivatedUserId).HasName("user_deactivations_pkey");

            entity.ToTable("user_deactivations");

            entity.Property(e => e.DeactivatedUserId)
                .HasColumnName("deactivated_user_id");
            entity.Property(e => e.DeactivatedByUserId)
                .HasColumnName("deactivated_by_user_id");
            entity.Property(e => e.DeleteLater)
                .HasColumnName("delete_later");
            entity.Property(e => e.UserModerationId)
                .HasColumnName("user_moderation_id");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("CURRENT_TIMESTAMP")
                .HasColumnName("created_at");

            entity.HasOne(d => d.DeactivatedUser).WithOne(p => p.UserDeactivation)
                .HasForeignKey<UserDeactivation>(d => d.DeactivatedUserId)
                .HasConstraintName("fk_user_deactivations_deactivated_user_id");
            entity.HasOne(d => d.DeactivatedByUser).WithMany()
                .HasForeignKey(d => d.DeactivatedByUserId)
                .HasConstraintName("fk_user_deactivations_deactivated_by_user_id");
        });

        modelBuilder.Entity<UserEmailChange>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("user_email_changes_pkey");

            entity.ToTable("user_email_changes");

            entity.HasIndex(e => e.UserId);

            entity.HasIndex(e => e.UsedAt);
            entity.HasIndex(e => e.CreatedAt);

            entity.Property(e => e.Id)
                .ValueGeneratedOnAdd()
                .HasColumnName("id");
            entity.Property(e => e.UserId)
                .HasColumnName("user_id");
            entity.Property(e => e.OldEmail)
                .HasMaxLength(HardLimits.EmailAddressMaxLength)
                .HasColumnName("email_old");
            entity.Property(e => e.NewEmail)
                .HasMaxLength(HardLimits.EmailAddressMaxLength)
                .HasColumnName("email_new");
            entity.Property(e => e.TokenHash)
                .UseCollation("C")
                .HasMaxLength(ApiHardLimits.UserEmailChangeSecretMaxLength)
                .HasColumnName("token_hash");
            entity.Property(e => e.SecurityStampAtCreate)
                .HasColumnName("security_stamp_at_create");
            entity.Property(e => e.UsedAt)
                .HasColumnName("used_at");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("CURRENT_TIMESTAMP")
                .HasColumnName("created_at");

            entity.HasOne(d => d.User).WithMany(p => p.EmailChanges)
                .HasForeignKey(d => d.UserId)
                .HasConstraintName("fk_user_email_changes_user_id");
        });

        modelBuilder.Entity<UserNameChange>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("user_name_changes_pkey");

            entity.ToTable("user_name_changes");

            // Serves both the foreign key and the name change cooldown lookup
            entity.HasIndex(e => new { e.UserId, e.CreatedAt });

            entity.Property(e => e.Id)
                .ValueGeneratedOnAdd()
                .UseIdentityAlwaysColumn()
                .HasColumnName("id");
            entity.Property(e => e.UserId).HasColumnName("user_id");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("CURRENT_TIMESTAMP")
                .HasColumnName("created_at");
            entity.Property(e => e.OldName)
                .HasMaxLength(HardLimits.UsernameMaxLength)
                .HasColumnName("old_name");

            entity.HasOne(d => d.User).WithMany(p => p.NameChanges)
                .HasForeignKey(d => d.UserId)
                .HasConstraintName("fk_user_name_changes_user_id");
        });

        modelBuilder.Entity<DiscordWebhook>(entity =>
        {
            entity.HasKey(e => e.Name).HasName("discord_webhooks_pkey");
            
            entity.ToTable("discord_webhooks");
            
            entity.Property(e => e.Id)
                .HasColumnName("id");
            entity.Property(e => e.Name)
                .HasMaxLength(ApiHardLimits.DiscordWebhookNameMaxLength)
                .HasColumnName("name");
            entity.Property(e => e.WebhookId)
                .HasColumnName("webhook_id");
            entity.Property(e => e.WebhookToken)
                .HasMaxLength(ApiHardLimits.DiscordWebhookTokenMaxLength)
                .HasColumnName("webhook_token");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("CURRENT_TIMESTAMP")
                .HasColumnName("created_at");
        });

        modelBuilder.Entity<ConfigurationItem>(entity =>
        {
            entity.HasKey(e => e.Name).HasName("configuration_pkey");

            entity.ToTable("configuration");

            entity.Property(e => e.Name)
                .UseCollation("C")
                .HasMaxLength(ApiHardLimits.ConfigurationNameMaxLength)
                .HasColumnName("name");
            entity.Property(e => e.Description)
                .HasMaxLength(ApiHardLimits.ConfigurationDescriptionMaxLength)
                .HasColumnName("description");
            entity.Property(e => e.Type)
                .HasColumnName("type");
            entity.Property(e => e.Value)
                .HasMaxLength(ApiHardLimits.ConfigurationValueMaxLength)
                .HasColumnName("value");
            entity.Property(e => e.UpdatedAt)
                .HasColumnName("updated_at");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("CURRENT_TIMESTAMP")
                .HasColumnName("created_at");
        });

        modelBuilder.Entity<UserNameBlacklist>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("user_name_blacklist_pkey");

            entity.ToTable("user_name_blacklist");

            entity.HasIndex(e => e.Value).UseCollation("ndcoll").IsUnique();

            entity.Property(e => e.Id)
                .ValueGeneratedOnAdd()
                .HasColumnName("id");
            entity.Property(e => e.Value)
                .UseCollation("ndcoll")
                .HasMaxLength(HardLimits.UsernameMaxLength)
                .HasColumnName("value");
            entity.Property(e => e.MatchType)
                .HasColumnName("match_type");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("CURRENT_TIMESTAMP")
                .HasColumnName("created_at");
        });

        modelBuilder.Entity<EmailProviderBlacklist>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("email_provider_blacklist_pkey");

            entity.ToTable("email_provider_blacklist");

            entity.HasIndex(e => e.Domain).UseCollation("ndcoll").IsUnique();

            entity.Property(e => e.Id)
                .ValueGeneratedOnAdd()
                .HasColumnName("id");
            entity.Property(e => e.Domain)
                .UseCollation("ndcoll")
                .HasMaxLength(ApiHardLimits.EmailProviderDomainMaxLength)
                .HasColumnName("domain");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("CURRENT_TIMESTAMP")
                .HasColumnName("created_at");
        });

        modelBuilder.Entity<AutomationToken>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("automation_tokens_pkey");

            entity.ToTable("automation_tokens");

            entity.HasIndex(e => e.TokenHash).IsUnique();

            entity.Property(e => e.Id)
                .ValueGeneratedOnAdd()
                .HasColumnName("id");
            entity.Property(e => e.Name)
                .HasMaxLength(ApiHardLimits.ApiKeyNameMaxLength)
                .HasColumnName("name");
            entity.Property(e => e.TokenHash)
                .UseCollation("C")
                .HasMaxLength(HardLimits.Sha256HashHexLength)
                .HasColumnName("token_hash");
            entity.Property(e => e.Types)
                .HasColumnType("automation_token_type[]")
                .HasColumnName("types");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("CURRENT_TIMESTAMP")
                .HasColumnName("created_at");
            entity.Property(e => e.LastUsedAt).HasColumnName("last_used_at");
            entity.Property(e => e.LastRotatedAt).HasColumnName("last_rotated_at");
            entity.Property(e => e.UseCount)
                .HasDefaultValue(0L)
                .HasColumnName("use_count");
            entity.Property(e => e.AutoCleanupUsers)
                .HasDefaultValue(false)
                .HasColumnName("auto_cleanup_users");
            entity.Property(e => e.AutoCleanupAfter).HasColumnName("auto_cleanup_after");
        });

        modelBuilder.Entity<EmailOutboxMessage>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("email_outbox_pkey");

            entity.ToTable("email_outbox");

            // The consumer claims due rows ordered by next_attempt_at. Rows are never deleted, so the index
            // only covers the claimable ones and stays the size of the backlog. The claim query repeats
            // this predicate, which is what lets the planner use the index.
            entity.HasIndex(e => e.NextAttemptAt)
                .HasFilter(EmailOutboxQueries.ClaimableFilter);
            entity.HasIndex(e => e.Recipient);
            // The delivery job resolves newest-wins coalescing by looking up siblings that share a
            // coalesce key, so index it. Rows that never coalesce have none.
            entity.HasIndex(e => e.CoalesceKey)
                .HasFilter("coalesce_key IS NOT NULL");

            entity.Property(e => e.Id)
                .ValueGeneratedOnAdd()
                .HasColumnName("id");
            entity.Property(e => e.Type)
                .HasColumnName("type");
            entity.Property(e => e.Recipient)
                .HasMaxLength(HardLimits.EmailAddressMaxLength)
                .HasColumnName("recipient");
            entity.Property(e => e.RecipientName)
                .HasMaxLength(HardLimits.UsernameMaxLength)
                .HasColumnName("recipient_name");
            entity.Property(e => e.Payload)
                .HasColumnType("jsonb")
                .HasColumnName("payload");
            entity.Property(e => e.CoalesceKey)
                .HasMaxLength(ApiHardLimits.EmailOutboxCoalesceKeyMaxLength)
                .HasColumnName("coalesce_key");
            entity.Property(e => e.Status)
                .HasColumnName("status");
            // attempt_count doubles as the delivery lease's fencing token: every claim increments it, and
            // the deferred terminal write is guarded by it (IsConcurrencyToken). If a lapsed lease lets
            // another run reclaim a Sending row mid-batch (bumping attempt_count), the original run's
            // UPDATE matches no row and throws DbUpdateConcurrencyException instead of clobbering the
            // reclaimer's claim / terminal state.
            entity.Property(e => e.AttemptCount)
                .HasDefaultValue(0)
                .IsConcurrencyToken()
                .HasColumnName("attempt_count");
            entity.Property(e => e.NextAttemptAt)
                .HasDefaultValueSql("CURRENT_TIMESTAMP")
                .HasColumnName("next_attempt_at");
            entity.Property(e => e.LastError)
                .HasMaxLength(ApiHardLimits.EmailOutboxLastErrorMaxLength)
                .HasColumnName("last_error");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("CURRENT_TIMESTAMP")
                .HasColumnName("created_at");
            entity.Property(e => e.SentAt)
                .HasColumnName("sent_at");
            entity.Property(e => e.FailedAt)
                .HasColumnName("failed_at");
        });

        modelBuilder.Entity<AdminUsersView>(entity =>
        {
            entity
                .HasNoKey()
                .ToView("admin_users_view");

            entity.Property(e => e.Id)
                .HasColumnName("id");
            entity.Property(e => e.Name)
                .HasColumnType("character varying")
                .HasColumnName("name");
            entity.Property(e => e.Email)
                .HasColumnType("character varying")
                .HasColumnName("email");
            entity.Property(e => e.PasswordHashType)
                .HasColumnType("character varying")
                .HasColumnName("password_hash_type");
            entity.Property(e => e.Roles)
                .HasColumnType("role_type[]")
                .HasColumnName("roles")
                .HasConversion(x => x.ToArray(), x => x.ToList());
            entity.Property(e => e.CreatedAt)
                .HasColumnName("created_at");
            entity.Property(e => e.ActivatedAt)
                .HasColumnName("activated_at");
            entity.Property(e => e.CreatedByAutomationTokenId)
                .HasColumnName("created_by_automation_token_id");
            entity.Property(e => e.DeactivatedAt)
                .HasColumnName("deactivated_at");
            entity.Property(e => e.DeactivatedByUserId)
                .HasColumnName("deactivated_by_user_id");
            entity.Property(e => e.ApiTokenCount)
                .HasColumnName("api_token_count");
            entity.Property(e => e.PasswordResetCount)
                .HasColumnName("password_reset_count");
            entity.Property(e => e.ShockerUserShareCount)
                .HasColumnName("shocker_user_share_count");
            entity.Property(e => e.ShockerPublicShareCount)
                .HasColumnName("shocker_public_share_count");
            entity.Property(e => e.EmailChangeRequestCount)
                .HasColumnName("email_change_request_count");
            entity.Property(e => e.NameChangeRequestCount)
                .HasColumnName("name_change_request_count");
            entity.Property(e => e.DeviceCount)
                .HasColumnName("device_count");
            entity.Property(e => e.ShockerCount)
                .HasColumnName("shocker_count");
            entity.Property(e => e.ShockerControlLogCount)
                .HasColumnName("shocker_control_log_count");
        });

        modelBuilder.Entity<UserAuditLog>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("user_audit_logs_pkey");

            entity.ToTable("user_audit_logs");

            entity.HasIndex(e => e.UserId);
            entity.HasIndex(e => e.ActorId);
            entity.HasIndex(e => e.CreatedAt);

            entity.Property(e => e.Id)
                .ValueGeneratedOnAdd()
                .HasColumnName("id");
            entity.Property(e => e.UserId).HasColumnName("user_id");
            entity.Property(e => e.ActorId).HasColumnName("actor_id");
            entity.Property(e => e.Action)
                .HasColumnType("audit_action")
                .HasColumnName("action");
            entity.Property(e => e.Reason)
                .HasMaxLength(ApiHardLimits.AuditReasonMaxLength)
                .HasColumnName("reason");
            entity.Property(e => e.IpAddress)
                .HasColumnName("ip_address");
            entity.Property(e => e.UserAgent)
                .HasMaxLength(HardLimits.UserAgentMaxLength)
                .HasColumnName("user_agent");
            entity.Property(e => e.Metadata)
                .HasColumnType("jsonb")
                .HasColumnName("metadata")
                // Serialized as the declared base type, which is what writes the type discriminator. Left to
                // Npgsql's own JSON mapping the runtime type is serialized and the discriminator is lost.
                .HasConversion(
                    v => JsonSerializer.Serialize(v, (JsonSerializerOptions?)null),
                    v => JsonSerializer.Deserialize<AuditMetadata>(v, (JsonSerializerOptions?)null));
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("CURRENT_TIMESTAMP")
                .HasColumnName("created_at");

            entity.HasOne(e => e.User).WithMany(u => u.AuditLogs)
                .HasForeignKey(e => e.UserId)
                .OnDelete(DeleteBehavior.Cascade)
                .HasConstraintName("fk_user_audit_logs_user_id");

            entity.HasOne(e => e.Actor).WithMany(u => u.ActorAuditLogs)
                .HasForeignKey(e => e.ActorId)
                .OnDelete(DeleteBehavior.SetNull)
                .HasConstraintName("fk_user_audit_logs_actor_id");
        });
    }

    /// <summary>
    /// Columns and limits shared by every table that carries <see cref="SafetySettings"/>. Call it after
    /// <c>ToTable</c>, the check constraints are named after the table.
    /// </summary>
    private static void ConfigureSafetySettings<T>(EntityTypeBuilder<T> entity, bool allowLiveControlByDefault = true)
        where T : SafetySettings
    {
        var table = entity.Metadata.GetTableName();

        entity.ToTable(t =>
        {
            t.HasCheckConstraint($"ck_{table}_max_intensity",
                $"max_intensity BETWEEN {HardLimits.MinControlIntensity} AND {HardLimits.MaxControlIntensity}");
            t.HasCheckConstraint($"ck_{table}_max_duration",
                $"max_duration BETWEEN {HardLimits.MinControlDuration} AND {HardLimits.MaxControlDuration}");
        });

        entity.Property(e => e.AllowShock)
            .HasDefaultValue(true)
            .HasColumnName("allow_shock");
        entity.Property(e => e.AllowVibrate)
            .HasDefaultValue(true)
            .HasColumnName("allow_vibrate");
        entity.Property(e => e.AllowSound)
            .HasDefaultValue(true)
            .HasColumnName("allow_sound");
        entity.Property(e => e.AllowLiveControl)
            .HasDefaultValue(allowLiveControlByDefault)
            .HasColumnName("allow_livecontrol");
        entity.Property(e => e.MaxIntensity)
            .HasColumnName("max_intensity");
        entity.Property(e => e.MaxDuration)
            .HasColumnName("max_duration");
        entity.Property(e => e.IsPaused)
            .HasDefaultValue(false)
            .HasColumnName("is_paused");
    }
}
