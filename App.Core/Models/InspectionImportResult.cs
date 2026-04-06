namespace App.Core.Models;

public sealed record InspectionImportResult(
    int ImportedCount,
    int NormalCount,
    int WarningCount,
    int AbnormalCount,
    int TemplateCreatedCount,
    int TemplateUpdatedCount,
    DateTime ImportedAt);
