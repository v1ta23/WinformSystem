namespace App.WinForms.ViewModels;

internal sealed class InspectionImportResultViewModel
{
    public string BatchKeyword { get; init; } = string.Empty;

    public string SourceFileName { get; init; } = string.Empty;

    public int ImportedCount { get; init; }

    public int NormalCount { get; init; }

    public int WarningCount { get; init; }

    public int AbnormalCount { get; init; }

    public int PendingCount => WarningCount + AbnormalCount;

    public int TemplateCreatedCount { get; init; }

    public int TemplateUpdatedCount { get; init; }

    public DateTime ImportedAt { get; init; }
}
