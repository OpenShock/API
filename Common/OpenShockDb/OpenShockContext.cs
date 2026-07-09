using System.Text.Json;
using Microsoft.AspNetCore.DataProtection.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using OpenShock.Common.Constants;
using OpenShock.Common.Extensions;
using OpenShock.Common.Models;

namespace OpenShock.Common.OpenShockDb;

/// <summary>
/// This is meant for use in migrations only.
/// </summary>
public sealed class MigrationOpenShockContext : OpenShockContext
{
    private readonly string? _connectionString;
    private readonly bool _debug;
    private readonly bool _migrationTool;
    private readonly ILoggerFactory? _loggerFactory;
    
    public MigrationOpenShockContext()
    {
        _migrationTool = true;
    }
    
    public MigrationOpenShockContext(string connectionString, bool debug, ILoggerFactory loggerFactory)
    {
        _connectionString = connectionString;
        _debug = debug;
        _loggerFactory = loggerFactory;
    }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        if (_migrationTool)
        {
            ConfigureOptionsBuilder(optionsBuilder, "Host=localhost;Database=openshock;Username=openshock;Password=openshock", true);
            return;
        }
        if(string.IsNullOrWhiteSpace(_connectionString))
            throw new InvalidOperationException("Connection string is not set.");
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
    public OpenShockContext()
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
    
    public DbSet<DataProtectionKey> DataProtectionKeys { get; set; }

    public DbSet<UserAuditLog> UserAuditLogs { get; set; }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        if (!optionsBuilder.IsConfigured)
        {
            optionsBuilder.UseNpgsql("Host=localhost;Database=openshock;Username=openshock;Password=openshock");
        }
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder
            .RegisterPgEnums()
            .HasCollation("public", "ndcoll", "und-u-ks-level2", "icu", false); // Add case-insensitive, accent-sensitive comparison collation

        modelBuilder.Entity<ApiToken>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("api_tokens_pkey");

            entity.ToTable("api_tokens");

            entity.HasIndex(e => e.UserId);
            entity.HasIndex(e => e.ValidUntil);
            entity.HasIndex(e => e.TokenHash).IsUnique();

            entity.Property(e => e.Id)
                .ValueGeneratedNever()
                .HasColumnName("id");
            entity.Property(e => e.CreatedByIp)
                .HasColumnName("created_by_ip");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("CURRENT_TIMESTAMP")
                .HasColumnName("created_at");
            entity.Property(e => e.LastUsed)
                .HasColumnName("last_used");
            entity.Property(e => e.Name)
                .HasMaxLength(HardLimits.ApiKeyNameMaxLength)
                .HasColumnName("name");
            entity.Property(e => e.TokenHash)
                .UseCollation("C")
                .VarCharWithLength(HardLimits.Sha256HashHexLength)
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
                .ValueGeneratedNever()
                .HasColumnName("id");
        });
        
        modelBuilder.Entity<ApiTokenReport>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("api_token_reports_pkey");

            entity.ToTable("api_token_reports");

            entity.Property(e => e.Id)
                .ValueGeneratedNever()
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
                .ValueGeneratedNever()
                .HasColumnName("id");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("CURRENT_TIMESTAMP")
                .HasColumnName("created_at");
            entity.Property(e => e.Name)
                .VarCharWithLength(HardLimits.HubNameMaxLength)
                .HasColumnName("name");
            entity.Property(e => e.OwnerId).HasColumnName("owner_id");
            entity.Property(e => e.Token)
                .UseCollation("C")
                .HasMaxLength(HardLimits.HubTokenMaxLength)
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
                .VarCharWithLength(HardLimits.OtaUpdateMessageMaxLength)
                .HasColumnName("message");
            entity.Property(e => e.Version)
                .VarCharWithLength(HardLimits.SemVerMaxLength)
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
                .ValueGeneratedNever()
                .HasColumnName("id");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("CURRENT_TIMESTAMP")
                .HasColumnName("created_at");
            entity.Property(e => e.TokenHash)
                .UseCollation("C")
                .VarCharWithLength(HardLimits.PasswordResetSecretMaxLength)
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
                .ValueGeneratedNever()
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
                .HasDefaultValue(true)
                .HasColumnName("allow_livecontrol");
            entity.Property(e => e.MaxIntensity)
                .HasColumnName("max_intensity");
            entity.Property(e => e.MaxDuration)
                .HasColumnName("max_duration");
            entity.Property(e => e.IsPaused)
                .HasDefaultValue(false)
                .HasColumnName("is_paused");

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

            entity.ToTable("shockers");

            entity.HasIndex(e => e.DeviceId);

            entity.Property(e => e.Id)
                .ValueGeneratedNever()
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
            entity.HasKey(e => e.Id).HasName("shocker_control_logs_pkey");

            entity.ToTable("shocker_control_logs");

            entity.HasIndex(e => e.ShockerId);

            entity.Property(e => e.Id)
                .ValueGeneratedNever()
                .HasColumnName("id");
            entity.Property(e => e.ControlledByUserId).HasColumnName("controlled_by_user_id");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("CURRENT_TIMESTAMP")
                .HasColumnName("created_at");
            entity.Property(e => e.CustomName)
                .VarCharWithLength(HardLimits.ShockerControlLogCustomNameMaxLength)
                .HasColumnName("custom_name");
            entity.Property(e => e.Duration).HasColumnName("duration");
            entity.Property(e => e.Intensity).HasColumnName("intensity");
            entity.Property(e => e.LiveControl)
                .HasDefaultValue(false)
                .HasColumnName("live_control");
            entity.Property(e => e.ShockerId).HasColumnName("shocker_id");
            entity.Property(e => e.Type).HasColumnType("control_type").HasColumnName("type");

            entity.HasOne(d => d.ControlledByUser).WithMany(p => p.ShockerControlLogs)
                .HasForeignKey(d => d.ControlledByUserId)
                .OnDelete(DeleteBehavior.Cascade)
                .HasConstraintName("fk_shocker_control_logs_controlled_by_user_id");

            entity.HasOne(d => d.Shocker).WithMany(p => p.ShockerControlLogs)
                .HasForeignKey(d => d.ShockerId)
                .HasConstraintName("fk_shocker_control_logs_shocker_id");
        });

        modelBuilder.Entity<UserShare>(entity =>
        {
            entity.HasKey(e => new { e.SharedWithUserId, e.ShockerId }).HasName("user_shares_pkey");

            entity.ToTable("user_shares");

            entity.HasIndex(e => e.SharedWithUserId);
            entity.Property(e => e.SharedWithUserId).HasColumnName("shared_with_user_id");
            entity.Property(e => e.ShockerId).HasColumnName("shocker_id");
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
                .HasDefaultValue(true)
                .HasColumnName("allow_livecontrol");
            entity.Property(e => e.MaxIntensity)
                .HasColumnName("max_intensity");
            entity.Property(e => e.MaxDuration)
                .HasColumnName("max_duration");
            entity.Property(e => e.IsPaused)
                .HasDefaultValue(false)
                .HasColumnName("is_paused");
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
                .ValueGeneratedNever()
                .HasColumnName("id");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("CURRENT_TIMESTAMP")
                .HasColumnName("created_at");
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
                .HasDefaultValue(true)
                .HasColumnName("allow_livecontrol");
            entity.Property(e => e.MaxIntensity)
                .HasColumnName("max_intensity");
            entity.Property(e => e.MaxDuration)
                .HasColumnName("max_duration");
            entity.Property(e => e.IsPaused)
                .HasDefaultValue(false)
                .HasColumnName("is_paused");
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
                .ValueGeneratedNever()
                .HasColumnName("id");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("CURRENT_TIMESTAMP")
                .HasColumnName("created_at");
            entity.Property(e => e.ExpiresAt).HasColumnName("expires_at");
            entity.Property(e => e.Name)
                .VarCharWithLength(HardLimits.PublicShareNameMaxLength)
                .HasColumnName("name");
            entity.Property(e => e.OwnerId).HasColumnName("owner_id");

            entity.HasOne(d => d.Owner).WithMany(p => p.OwnedPublicShares)
                .HasForeignKey(d => d.OwnerId)
                .HasConstraintName("fk_public_shares_owner_id");
        });

        modelBuilder.Entity<PublicShareShocker>(entity =>
        {
            entity.HasKey(e => new { e.PublicShareId, e.ShockerId }).HasName("public_share_shockers_pkey");

            entity.ToTable("public_share_shockers");

            entity.Property(e => e.PublicShareId).HasColumnName("public_share_id");
            entity.Property(e => e.ShockerId).HasColumnName("shocker_id");
            entity.Property(e => e.Cooldown).HasColumnName("cooldown");
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
                .HasDefaultValue(false)
                .HasColumnName("allow_livecontrol");
            entity.Property(e => e.MaxIntensity)
                .HasColumnName("max_intensity");
            entity.Property(e => e.MaxDuration)
                .HasColumnName("max_duration");
            entity.Property(e => e.IsPaused)
                .HasDefaultValue(false)
                .HasColumnName("is_paused");

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

            entity.HasIndex(e => e.Email).IsUnique();
            entity.HasIndex(e => e.Name).UseCollation("ndcoll").IsUnique();

            entity.Property(e => e.Id)
                .ValueGeneratedNever()
                .HasColumnName("id");
            entity.Property(e => e.Name)
                .UseCollation("ndcoll")
                .VarCharWithLength(HardLimits.UsernameMaxLength)
                .HasColumnName("name");
            entity.Property(e => e.Email)
                .VarCharWithLength(HardLimits.EmailAddressMaxLength)
                .HasColumnName("email");
            entity.Property(e => e.PasswordHash)
                .UseCollation("C")
                .VarCharWithLength(HardLimits.PasswordHashMaxLength)
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
                .HasColumnName("provider_key");
            entity.Property(e => e.ExternalId)
                .HasColumnName("external_id");
            entity.Property(e => e.DisplayName)
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
                .VarCharWithLength(HardLimits.UserActivationRequestSecretMaxLength)
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
                .ValueGeneratedNever()
                .HasColumnName("id");
            entity.Property(e => e.UserId)
                .HasColumnName("user_id");
            entity.Property(e => e.OldEmail)
                .VarCharWithLength(HardLimits.EmailAddressMaxLength)
                .HasColumnName("email_old");
            entity.Property(e => e.NewEmail)
                .VarCharWithLength(HardLimits.EmailAddressMaxLength)
                .HasColumnName("email_new");
            entity.Property(e => e.TokenHash)
                .UseCollation("C")
                .VarCharWithLength(HardLimits.UserEmailChangeSecretMaxLength)
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
            entity.HasKey(e => new { e.Id, e.UserId }).HasName("user_name_changes_pkey");

            entity.ToTable("user_name_changes");

            entity.HasIndex(e => e.CreatedAt);
            entity.HasIndex(e => e.OldName);
            entity.HasIndex(e => e.UserId);

            entity.Property(e => e.Id)
                .ValueGeneratedOnAdd()
                .UseIdentityAlwaysColumn()
                .HasColumnName("id");
            entity.Property(e => e.UserId).HasColumnName("user_id");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("CURRENT_TIMESTAMP")
                .HasColumnName("created_at");
            entity.Property(e => e.OldName)
                .VarCharWithLength(HardLimits.UsernameMaxLength)
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
                .HasColumnName("name");
            entity.Property(e => e.WebhookId)
                .HasColumnName("webhook_id");
            entity.Property(e => e.WebhookToken)
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
                .HasColumnName("name");
            entity.Property(e => e.Description)
                .HasColumnName("description");
            entity.Property(e => e.Type)
                .HasColumnName("type");
            entity.Property(e => e.Value)
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
                .ValueGeneratedNever()
                .HasColumnName("id");
            entity.Property(e => e.Value)
                .UseCollation("ndcoll")
                .VarCharWithLength(HardLimits.UsernameMaxLength)
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
                .ValueGeneratedNever()
                .HasColumnName("id");
            entity.Property(e => e.Domain)
                .UseCollation("ndcoll")
                .VarCharWithLength(HardLimits.EmailProviderDomainMaxLength)
                .HasColumnName("domain");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("CURRENT_TIMESTAMP")
                .HasColumnName("created_at");
        });

        modelBuilder.Entity<EmailOutboxMessage>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("email_outbox_pkey");

            entity.ToTable("email_outbox");

            // The consumer claims due rows ordered by next_attempt_at; (status, next_attempt_at) serves
            // both the status filter and the ordering for the FOR UPDATE SKIP LOCKED claim query.
            entity.HasIndex(e => new { e.Status, e.NextAttemptAt });
            entity.HasIndex(e => e.Recipient);
            // The delivery job resolves newest-wins coalescing by looking up siblings that share a
            // coalesce key, so index it.
            entity.HasIndex(e => e.CoalesceKey);

            entity.Property(e => e.Id)
                .ValueGeneratedNever()
                .HasColumnName("id");
            entity.Property(e => e.Type)
                .HasColumnName("type");
            entity.Property(e => e.Recipient)
                .VarCharWithLength(HardLimits.EmailAddressMaxLength)
                .HasColumnName("recipient");
            entity.Property(e => e.RecipientName)
                .VarCharWithLength(HardLimits.UsernameMaxLength)
                .HasColumnName("recipient_name");
            entity.Property(e => e.Payload)
                .HasColumnType("jsonb")
                .HasColumnName("payload");
            entity.Property(e => e.CoalesceKey)
                .VarCharWithLength(HardLimits.EmailOutboxCoalesceKeyMaxLength)
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
                .VarCharWithLength(HardLimits.EmailOutboxLastErrorMaxLength)
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
                .ValueGeneratedNever()
                .HasColumnName("id");
            entity.Property(e => e.UserId).HasColumnName("user_id");
            entity.Property(e => e.ActorId).HasColumnName("actor_id");
            entity.Property(e => e.Action)
                .HasColumnType("audit_action")
                .HasColumnName("action");
            entity.Property(e => e.Reason)
                .VarCharWithLength(HardLimits.AuditReasonMaxLength)
                .HasColumnName("reason");
            entity.Property(e => e.IpAddress)
                .VarCharWithLength(HardLimits.IpAddressMaxLength)
                .HasColumnName("ip_address");
            entity.Property(e => e.UserAgent)
                .VarCharWithLength(HardLimits.UserAgentMaxLength)
                .HasColumnName("user_agent");
            entity.Property(e => e.Metadata)
                .HasColumnType("jsonb")
                .HasColumnName("metadata")
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
}
