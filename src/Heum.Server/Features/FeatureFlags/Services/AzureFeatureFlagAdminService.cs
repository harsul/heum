using System.Text.Json;
using System.Text.Json.Serialization;
using Azure.Data.AppConfiguration;
using Heum.Server.Features.FeatureFlags.Models;

namespace Heum.Server.Features.FeatureFlags.Services;

internal sealed class AzureFeatureFlagAdminService : IFeatureFlagAdminService
{
    private static readonly JsonSerializerOptions JsonOpts = new()
    {
        PropertyNameCaseInsensitive = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
    };

    private readonly ConfigurationClient _client;

    public AzureFeatureFlagAdminService(ConfigurationClient client) => _client = client;

    public bool IsManageable => true;

    public async Task<IReadOnlyList<FeatureFlagResponse>> GetAllAsync(CancellationToken ct)
    {
        var results = new List<FeatureFlagResponse>();
        var selector = new SettingSelector { KeyFilter = FeatureFlagConfigurationSetting.KeyPrefix + "*" };

        await foreach (var setting in _client.GetConfigurationSettingsAsync(selector, ct))
        {
            if (setting is FeatureFlagConfigurationSetting ff)
                results.Add(ParseFeatureFlag(ff));
        }

        return results;
    }

    public async Task<FeatureFlagResponse?> GetAsync(string name, CancellationToken ct)
    {
        try
        {
            var setting = await _client.GetConfigurationSettingAsync(
                FeatureFlagConfigurationSetting.KeyPrefix + name, label: null, ct);
            return setting.Value is FeatureFlagConfigurationSetting ff ? ParseFeatureFlag(ff) : null;
        }
        catch (Azure.RequestFailedException ex) when (ex.Status == 404)
        {
            return null;
        }
    }

    public async Task<FeatureFlagResponse> CreateAsync(CreateFeatureFlagRequest request, CancellationToken ct)
    {
        var json = BuildFlagJson(request.Name, request.Description, isEnabled: false, [], [], 0);
        await SetRawAsync(request.Name, json, ct);
        return new FeatureFlagResponse(request.Name, request.Description, false, [], [], 0);
    }

    public async Task UpdateAsync(string name, UpdateFeatureFlagRequest request, CancellationToken ct)
    {
        var json = BuildFlagJson(
            name,
            request.Description,
            request.IsEnabled,
            request.TargetedTenantIds,
            request.TargetedUserIds,
            request.DefaultRolloutPercentage);
        await SetRawAsync(name, json, ct);
    }

    public Task DeleteAsync(string name, CancellationToken ct) =>
        _client.DeleteConfigurationSettingAsync(
            FeatureFlagConfigurationSetting.KeyPrefix + name, label: null, ct);

    // ── Helpers ─────────────────────────────────────────────────────────────

    private Task SetRawAsync(string name, string json, CancellationToken ct)
    {
        var setting = new ConfigurationSetting(FeatureFlagConfigurationSetting.KeyPrefix + name, json)
        {
            ContentType = "application/vnd.microsoft.appconfig.ff+json;charset=utf-8",
        };
        return _client.SetConfigurationSettingAsync(setting, onlyIfUnchanged: false, ct);
    }

    private static string BuildFlagJson(
        string name,
        string? description,
        bool isEnabled,
        IEnumerable<string> tenantIds,
        IEnumerable<string> userIds,
        int defaultRolloutPercentage)
    {
        var groups = tenantIds
            .Select(id => new GroupJsonModel { Name = $"tenant:{id}", RolloutPercentage = 100 })
            .ToList();
        var users = userIds.Select(id => $"user:{id}").ToList();

        var hasTargeting = groups.Count > 0 || users.Count > 0 || defaultRolloutPercentage > 0;

        var model = new FeatureFlagJsonModel
        {
            Id = name,
            Description = description,
            Enabled = isEnabled,
            Conditions = new ConditionsJsonModel
            {
                ClientFilters = hasTargeting
                    ?
                    [
                        new ClientFilterJsonModel
                        {
                            Name = "Microsoft.Targeting",
                            Parameters = new TargetingParamsJsonModel
                            {
                                Audience = new AudienceJsonModel
                                {
                                    Groups = groups,
                                    Users = users,
                                    DefaultRolloutPercentage = defaultRolloutPercentage,
                                },
                            },
                        },
                    ]
                    : [],
            },
        };

        return JsonSerializer.Serialize(model, JsonOpts);
    }

    private static FeatureFlagResponse ParseFeatureFlag(FeatureFlagConfigurationSetting ff)
    {
        var name = ff.Key[FeatureFlagConfigurationSetting.KeyPrefix.Length..];

        FeatureFlagJsonModel? model = null;
        try { model = JsonSerializer.Deserialize<FeatureFlagJsonModel>(ff.Value, JsonOpts); }
        catch { /* use defaults */ }

        var targetedTenants = new List<string>();
        var targetedUsers = new List<string>();
        var defaultRollout = 0;

        var targeting = model?.Conditions?.ClientFilters
            .FirstOrDefault(f => string.Equals(f.Name, "Microsoft.Targeting", StringComparison.OrdinalIgnoreCase));

        if (targeting?.Parameters?.Audience is { } audience)
        {
            targetedTenants = audience.Groups
                .Where(g => g.Name.StartsWith("tenant:", StringComparison.OrdinalIgnoreCase))
                .Select(g => g.Name["tenant:".Length..])
                .ToList();
            targetedUsers = audience.Users
                .Where(u => u.StartsWith("user:", StringComparison.OrdinalIgnoreCase))
                .Select(u => u["user:".Length..])
                .ToList();
            defaultRollout = audience.DefaultRolloutPercentage;
        }

        return new FeatureFlagResponse(
            name,
            model?.Description,
            ff.IsEnabled,
            targetedTenants,
            targetedUsers,
            defaultRollout);
    }

    // ── Internal JSON models matching Azure App Configuration feature flag schema ──

    private sealed class FeatureFlagJsonModel
    {
        [JsonPropertyName("id")] public string Id { get; set; } = "";
        [JsonPropertyName("description")] public string? Description { get; set; }
        [JsonPropertyName("enabled")] public bool Enabled { get; set; }
        [JsonPropertyName("conditions")] public ConditionsJsonModel Conditions { get; set; } = new();
    }

    private sealed class ConditionsJsonModel
    {
        [JsonPropertyName("client_filters")] public List<ClientFilterJsonModel> ClientFilters { get; set; } = [];
    }

    private sealed class ClientFilterJsonModel
    {
        [JsonPropertyName("name")] public string Name { get; set; } = "";
        [JsonPropertyName("parameters")] public TargetingParamsJsonModel? Parameters { get; set; }
    }

    private sealed class TargetingParamsJsonModel
    {
        [JsonPropertyName("Audience")] public AudienceJsonModel Audience { get; set; } = new();
    }

    private sealed class AudienceJsonModel
    {
        [JsonPropertyName("Groups")] public List<GroupJsonModel> Groups { get; set; } = [];
        [JsonPropertyName("Users")] public List<string> Users { get; set; } = [];
        [JsonPropertyName("DefaultRolloutPercentage")] public int DefaultRolloutPercentage { get; set; }
    }

    private sealed class GroupJsonModel
    {
        [JsonPropertyName("Name")] public string Name { get; set; } = "";
        [JsonPropertyName("RolloutPercentage")] public int RolloutPercentage { get; set; } = 100;
    }
}
