namespace VideoSaaS.Domain.Entities;

public sealed class TenantSettings
{
    public string DefaultVoice { get; set; } = "pt_BR-cadu-medium";
    public string DefaultStyle { get; set; } = "educativo";
    public int MonthlyVideoLimit { get; set; } = 10;
    public int MaxScenesPerVideo { get; set; } = 8;
    public int MaxDurationSeconds { get; set; } = 90;
}
