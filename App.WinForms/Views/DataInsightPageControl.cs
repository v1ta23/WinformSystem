using App.Core.Models;
using App.WinForms.Controllers;
using App.WinForms.ViewModels;
using Microsoft.VisualBasic.FileIO;
using System.Data;
using System.Drawing.Drawing2D;
using System.Text;

namespace App.WinForms.Views;

internal sealed class DataInsightPageControl : UserControl
{
    private const int PreviewLimit = 200;

    private static readonly Color PageBackground = Color.FromArgb(10, 10, 15);
    private static readonly Color SurfaceBackground = Color.FromArgb(28, 30, 40);
    private static readonly Color SurfaceBorder = Color.FromArgb(80, 85, 110);
    private static readonly Color HeaderBackground = Color.FromArgb(22, 24, 33);
    private static readonly Color TextPrimaryColor = Color.FromArgb(255, 255, 255);
    private static readonly Color TextSecondaryColor = Color.FromArgb(210, 215, 230);
    private static readonly Color TextMutedColor = Color.FromArgb(160, 170, 190);
    private static readonly Color AccentBlue = Color.FromArgb(88, 130, 255);
    private static readonly Color AccentGreen = Color.FromArgb(76, 217, 140);
    private static readonly Color AccentOrange = Color.FromArgb(255, 165, 70);
    private static readonly Color AccentRed = Color.FromArgb(231, 76, 60);

    private readonly InspectionController _controller;
    private readonly string _account;
    private readonly Label _generatedAtLabel;
    private Label _fileValueLabel = null!;
    private Label _fileNoteLabel = null!;
    private Label _rowCountValueLabel = null!;
    private Label _rowCountNoteLabel = null!;
    private Label _pendingValueLabel = null!;
    private Label _pendingNoteLabel = null!;
    private Label _importValueLabel = null!;
    private Label _importNoteLabel = null!;
    private Label _workflowSubtitleLabel = null!;
    private Label _workflowValueLabel = null!;
    private Label _workflowNoteLabel = null!;
    private Label _issuesSubtitleLabel = null!;
    private Label _previewSubtitleLabel = null!;
    private Label _analysisSubtitleLabel = null!;
    private readonly Button _selectFileButton;
    private readonly Button _clearButton;
    private readonly Button _importButton;
    private readonly Button _viewImportedButton;
    private readonly Button _viewPendingButton;
    private readonly DataGridView _previewGrid;
    private readonly TextBox _issuesTextBox;
    private readonly TextBox _analysisTextBox;

    private CsvPreviewState? _currentPreview;
    private InspectionImportResultViewModel? _lastImportResult;
    private string? _currentFilePath;

    public DataInsightPageControl(InspectionController controller, string account)
    {
        _controller = controller;
        _account = account;

        Dock = DockStyle.Fill;
        BackColor = PageBackground;
        Font = new Font("Microsoft YaHei UI", 9F);
        Padding = new Padding(30, 20, 30, 20);

        _generatedAtLabel = CreateInfoLabel();
        _selectFileButton = CreateActionButton("选择 CSV 文件", AccentBlue);
        _clearButton = CreateActionButton("清空", Color.FromArgb(98, 109, 129));
        _importButton = CreateActionButton("确认导入", AccentGreen);
        _viewImportedButton = CreateActionButton("查看本批记录", AccentBlue);
        _viewPendingButton = CreateActionButton("查看待闭环", AccentOrange);
        _previewGrid = CreatePreviewGrid();
        _issuesTextBox = CreateAnalysisTextBox();
        _analysisTextBox = CreateAnalysisTextBox();

        var root = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            BackColor = Color.Transparent,
            ColumnCount = 1,
            RowCount = 3
        };
        root.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 112F));
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 124F));
        root.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));

        root.Controls.Add(BuildHeader(), 0, 0);
        root.Controls.Add(BuildSummaryArea(), 0, 1);
        root.Controls.Add(BuildWorkspaceArea(), 0, 2);

        Controls.Add(root);

        _selectFileButton.Click += (_, _) => ChooseCsvFile();
        _clearButton.Click += (_, _) => ResetState();
        _importButton.Click += (_, _) => ImportCurrentPreview();
        _viewImportedButton.Click += (_, _) => ViewImportedRequested?.Invoke(this, EventArgs.Empty);
        _viewPendingButton.Click += (_, _) => ViewPendingRequested?.Invoke(this, EventArgs.Empty);

        ApplyTheme();
        ResetState();
    }

    public event EventHandler? DataChanged;

    public event EventHandler? ViewImportedRequested;

    public event EventHandler? ViewPendingRequested;

    public string? LastImportedBatchKeyword => _lastImportResult?.BatchKeyword;

    public void ApplyTheme()
    {
        BackColor = PageBackground;
        ApplyGridTheme(_previewGrid);
        Invalidate(true);
    }

    private Control BuildHeader()
    {
        var shell = CreateSurfacePanel(new Padding(18, 10, 18, 10));
        shell.Margin = new Padding(0, 0, 0, 12);

        var layout = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            BackColor = Color.Transparent,
            ColumnCount = 2,
            RowCount = 1
        };
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));

        var titlePanel = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            BackColor = Color.Transparent,
            ColumnCount = 1,
            RowCount = 3,
            Margin = Padding.Empty,
            Padding = Padding.Empty
        };
        titlePanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
        titlePanel.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        titlePanel.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        titlePanel.RowStyles.Add(new RowStyle(SizeType.AutoSize));

        titlePanel.Controls.Add(CreateTextLabel("数据导入", 16F, FontStyle.Bold, TextPrimaryColor, new Padding(0, 0, 0, 2)), 0, 0);
        titlePanel.Controls.Add(CreateTextLabel("这个页只负责导入。先校验，再导入，后续处理回巡检页继续做。", 9F, FontStyle.Regular, TextSecondaryColor, new Padding(0, 0, 0, 2)), 0, 1);
        _generatedAtLabel.Dock = DockStyle.Top;
        titlePanel.Controls.Add(_generatedAtLabel, 0, 2);

        var actionPanel = new FlowLayoutPanel
        {
            AutoSize = true,
            AutoSizeMode = AutoSizeMode.GrowAndShrink,
            Dock = DockStyle.Fill,
            FlowDirection = FlowDirection.LeftToRight,
            WrapContents = false,
            BackColor = Color.Transparent,
            Margin = Padding.Empty
        };
        _selectFileButton.Margin = Padding.Empty;
        _clearButton.Margin = new Padding(10, 0, 0, 0);
        _importButton.Margin = new Padding(10, 0, 0, 0);
        _viewImportedButton.Margin = new Padding(10, 0, 0, 0);
        _viewPendingButton.Margin = new Padding(10, 0, 0, 0);
        actionPanel.Controls.Add(_selectFileButton);
        actionPanel.Controls.Add(_importButton);
        actionPanel.Controls.Add(_viewImportedButton);
        actionPanel.Controls.Add(_viewPendingButton);
        actionPanel.Controls.Add(_clearButton);

        layout.Controls.Add(titlePanel, 0, 0);
        layout.Controls.Add(actionPanel, 1, 0);
        shell.Controls.Add(layout);
        return shell;
    }

    private Control BuildSummaryArea()
    {
        var summaryArea = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            BackColor = Color.Transparent,
            ColumnCount = 4,
            RowCount = 1,
            Margin = new Padding(0, 0, 0, 12)
        };
        summaryArea.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
        for (var index = 0; index < 4; index++)
        {
            summaryArea.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 25F));
        }

        summaryArea.Controls.Add(CreateMetricCard("当前文件", AccentBlue, out _fileValueLabel, out _fileNoteLabel), 0, 0);
        summaryArea.Controls.Add(CreateMetricCard("有效记录", AccentGreen, out _rowCountValueLabel, out _rowCountNoteLabel), 1, 0);
        summaryArea.Controls.Add(CreateMetricCard("风险记录", AccentOrange, out _pendingValueLabel, out _pendingNoteLabel), 2, 0);
        summaryArea.Controls.Add(CreateMetricCard("当前状态", AccentBlue, out _importValueLabel, out _importNoteLabel), 3, 0);
        return summaryArea;
    }

    private Control BuildWorkspaceArea()
    {
        var layout = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            BackColor = Color.Transparent,
            ColumnCount = 2,
            RowCount = 1,
            Margin = Padding.Empty
        };
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 32F));
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 68F));
        layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));

        layout.Controls.Add(BuildGuidanceColumn(), 0, 0);
        layout.Controls.Add(BuildPreviewColumn(), 1, 0);
        return layout;
    }

    private Control BuildGuidanceColumn()
    {
        var column = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            BackColor = Color.Transparent,
            ColumnCount = 1,
            RowCount = 2,
            Margin = Padding.Empty,
            Padding = Padding.Empty
        };
        column.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
        column.RowStyles.Add(new RowStyle(SizeType.Absolute, 198F));
        column.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
        column.Controls.Add(BuildWorkflowArea(), 0, 0);
        column.Controls.Add(CreateSectionShell("问题清单", "有错误先修正，没错误再导入。", out _issuesSubtitleLabel, _issuesTextBox, new Padding(0, 12, 12, 0)), 0, 1);
        return column;
    }

    private Control BuildWorkflowArea()
    {
        var content = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            BackColor = Color.Transparent,
            ColumnCount = 1,
            RowCount = 6,
            Margin = Padding.Empty,
            Padding = Padding.Empty
        };
        content.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
        content.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        content.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        content.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        content.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        content.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        content.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));

        _workflowValueLabel = CreateTextLabel("步骤 1 · 选择文件", 14F, FontStyle.Bold, TextPrimaryColor, new Padding(0, 0, 0, 8));
        _workflowNoteLabel = CreateTextLabel("先选择符合模板的 CSV 文件。", 9F, FontStyle.Regular, TextSecondaryColor, new Padding(0, 0, 0, 12));

        content.Controls.Add(_workflowValueLabel, 0, 0);
        content.Controls.Add(_workflowNoteLabel, 0, 1);
        content.Controls.Add(CreateTextLabel("1. 选择 CSV 文件，先看结构和行数。", 8.8F, FontStyle.Regular, TextSecondaryColor, new Padding(0, 0, 0, 6)), 0, 2);
        content.Controls.Add(CreateTextLabel("2. 有错误先改文件；没错误再点“确认导入”。", 8.8F, FontStyle.Regular, TextSecondaryColor, new Padding(0, 0, 0, 6)), 0, 3);
        content.Controls.Add(CreateTextLabel("3. 导入完成后去“本批记录”或“待闭环”继续处理。", 8.8F, FontStyle.Regular, TextSecondaryColor, new Padding(0, 0, 0, 12)), 0, 4);
        content.Controls.Add(CreateHintPanel(), 0, 5);

        return CreateSectionShell("导入流程", "只做导入，不在这里叠别的功能。", out _workflowSubtitleLabel, content, new Padding(0, 0, 12, 0));
    }

    private Control BuildPreviewColumn()
    {
        var column = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            BackColor = Color.Transparent,
            ColumnCount = 1,
            RowCount = 2,
            Margin = Padding.Empty,
            Padding = Padding.Empty
        };
        column.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
        column.RowStyles.Add(new RowStyle(SizeType.Percent, 62F));
        column.RowStyles.Add(new RowStyle(SizeType.Percent, 38F));
        column.Controls.Add(CreateSectionShell("数据预览", "先确认结构、行数和风险记录。", out _previewSubtitleLabel, _previewGrid, Padding.Empty), 0, 0);
        column.Controls.Add(CreateSectionShell("结果说明", "只告诉你当前能不能导入，以及导入后该去哪。", out _analysisSubtitleLabel, _analysisTextBox, new Padding(0, 12, 0, 0)), 0, 1);
        return column;
    }

    private Control CreateHintPanel()
    {
        var panel = new SurfacePanel(14)
        {
            Dock = DockStyle.Top,
            Height = 110,
            Padding = new Padding(14, 12, 14, 12),
            FillColor = Color.FromArgb(18, AccentBlue),
            BorderColor = Color.FromArgb(64, AccentBlue)
        };

        var body = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            BackColor = Color.Transparent,
            ColumnCount = 1,
            RowCount = 2
        };
        body.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
        body.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        body.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        body.Controls.Add(CreateTextLabel("导入成功后不会停在这一页。", 9F, FontStyle.Bold, TextPrimaryColor, new Padding(0, 0, 0, 6)), 0, 0);
        body.Controls.Add(CreateTextLabel("系统会写入巡检记录；有预警和异常时，下一步直接去待闭环处理。", 8.8F, FontStyle.Regular, TextSecondaryColor, Padding.Empty), 0, 1);
        panel.Controls.Add(body);
        return panel;
    }

    private void ChooseCsvFile()
    {
        using var dialog = new OpenFileDialog
        {
            Filter = "CSV 文件 (*.csv)|*.csv|所有文件 (*.*)|*.*",
            Title = "选择要导入的 CSV 文件",
            Multiselect = false,
            RestoreDirectory = true
        };

        if (dialog.ShowDialog(FindForm()) != DialogResult.OK)
        {
            return;
        }

        try
        {
            LoadCsvFile(dialog.FileName);
        }
        catch (Exception ex)
        {
            MessageBox.Show(FindForm(), ex.Message, "加载失败", MessageBoxButtons.OK, MessageBoxIcon.Warning);
        }
    }

    private void LoadCsvFile(string filePath)
    {
        _currentFilePath = filePath;
        _currentPreview = ParseCsv(filePath);
        _lastImportResult = null;

        _previewGrid.DataSource = _currentPreview.PreviewTable;
        _fileValueLabel.Text = _currentPreview.FileName;
        _fileNoteLabel.Text = _currentPreview.CanImport
            ? "模板校验通过，可导入系统"
            : "模板未通过，先修正数据";
        _rowCountValueLabel.Text = _currentPreview.ValidEntryCount.ToString();
        _rowCountNoteLabel.Text = $"原始 {_currentPreview.RowCount} 行 / {_currentPreview.ColumnCount} 列";
        _pendingValueLabel.Text = (_currentPreview.WarningCount + _currentPreview.AbnormalCount).ToString();
        _pendingNoteLabel.Text = $"正常 {_currentPreview.NormalCount} / 预警 {_currentPreview.WarningCount} / 异常 {_currentPreview.AbnormalCount}";
        _importValueLabel.Text = _currentPreview.CanImport ? "待导入" : "不可导入";
        _importNoteLabel.Text = _currentPreview.CanImport
            ? "下一步可以直接确认导入"
            : "先处理问题清单里的错误";
        _previewSubtitleLabel.Text = _currentPreview.RowCount == 0
            ? "文件只有表头，还没有数据行。"
            : $"显示前 {_currentPreview.DisplayedRowCount} 行，空值 {_currentPreview.MissingValueCount} 个。";
        _analysisSubtitleLabel.Text = _currentPreview.CanImport
            ? "校验通过，下一步点“确认导入”。"
            : "当前文件还不能直接导入。";
        _analysisTextBox.Text = BuildPreviewAnalysis(_currentPreview);
        _generatedAtLabel.Text = $"最近加载：{DateTime.Now:yyyy-MM-dd HH:mm:ss}";

        UpdateWorkflowState();
        UpdateIssueSummary();
        UpdateActionState();
    }

    private void ImportCurrentPreview()
    {
        if (_currentPreview is null || !_currentPreview.CanImport || _currentFilePath is null)
        {
            return;
        }

        try
        {
            _lastImportResult = _controller.Import(_currentPreview.Entries, _currentFilePath);
            _importValueLabel.Text = "已导入";
            _importNoteLabel.Text = $"批次 {_lastImportResult.BatchKeyword}";
            _analysisSubtitleLabel.Text = "导入完成，下一步去巡检页继续处理。";
            _analysisTextBox.Text = BuildImportAnalysis(_lastImportResult, _currentPreview);
            _generatedAtLabel.Text = $"最近导入：{_lastImportResult.ImportedAt:yyyy-MM-dd HH:mm:ss}";
            DataChanged?.Invoke(this, EventArgs.Empty);
            UpdateWorkflowState();
            UpdateIssueSummary();
            UpdateActionState();
        }
        catch (Exception ex)
        {
            MessageBox.Show(FindForm(), ex.Message, "导入失败", MessageBoxButtons.OK, MessageBoxIcon.Warning);
        }
    }

    private void ResetState()
    {
        _currentPreview = null;
        _lastImportResult = null;
        _currentFilePath = null;
        _previewGrid.DataSource = null;
        _previewGrid.Columns.Clear();
        _fileValueLabel.Text = "--";
        _fileNoteLabel.Text = "还没有加载文件";
        _rowCountValueLabel.Text = "0";
        _rowCountNoteLabel.Text = "导入后显示有效记录数";
        _pendingValueLabel.Text = "0";
        _pendingNoteLabel.Text = "导入后显示预警和异常";
        _importValueLabel.Text = "未开始";
        _importNoteLabel.Text = "先选文件并校验模板";
        _previewSubtitleLabel.Text = "导入前先预览数据。";
        _analysisSubtitleLabel.Text = "先选择文件，再决定是否导入。";
        _analysisTextBox.Text =
            "这个页只做导入：\r\n" +
            "1. 先选 CSV 文件。\r\n" +
            "2. 看预览和问题清单。\r\n" +
            "3. 导入完成后去巡检页看本批记录或待闭环。";
        _generatedAtLabel.Text = "还没有选择导入文件";
        UpdateWorkflowState();
        UpdateIssueSummary();
        UpdateActionState();
    }

    private void UpdateActionState()
    {
        _importButton.Enabled = _currentPreview?.CanImport == true;
        _importButton.Visible = _lastImportResult is null;
        _viewImportedButton.Enabled = _lastImportResult is not null;
        _viewImportedButton.Visible = _lastImportResult is not null;
        _viewPendingButton.Enabled = (_lastImportResult?.PendingCount ?? 0) > 0;
        _viewPendingButton.Visible = (_lastImportResult?.PendingCount ?? 0) > 0;
        _clearButton.Visible = _currentPreview is not null || _lastImportResult is not null;
    }

    private void UpdateWorkflowState()
    {
        if (_lastImportResult is not null)
        {
            _workflowSubtitleLabel.Text = "第 3 步：导入完成后去后续页面处理。";
            _workflowValueLabel.Text = "步骤 3 · 去巡检页继续处理";
            _workflowNoteLabel.Text = _lastImportResult.PendingCount > 0
                ? "这批数据里有待闭环项，下一步直接点“查看待闭环”。"
                : "这批数据已入库，下一步点“查看本批记录”。";
            return;
        }

        if (_currentPreview is null)
        {
            _workflowSubtitleLabel.Text = "第 1 步：先选择要导入的文件。";
            _workflowValueLabel.Text = "步骤 1 · 选择文件";
            _workflowNoteLabel.Text = "这个页不做分析，只负责把 CSV 导进系统。";
            return;
        }

        if (_currentPreview.CanImport)
        {
            _workflowSubtitleLabel.Text = "第 2 步：校验通过，可以导入。";
            _workflowValueLabel.Text = "步骤 2 · 确认导入";
            _workflowNoteLabel.Text = "预览没问题就点“确认导入”，不要在这里停留太久。";
            return;
        }

        _workflowSubtitleLabel.Text = "第 2 步：先修正问题，再导入。";
        _workflowValueLabel.Text = "步骤 2 · 修正文件";
        _workflowNoteLabel.Text = "当前文件有阻断问题，先按问题清单修正 CSV。";
    }

    private void UpdateIssueSummary()
    {
        if (_currentPreview is null)
        {
            _issuesSubtitleLabel.Text = "先准备模板，再选择文件。";
            _issuesTextBox.Text =
                "固定模板要求：\r\n" +
                "1. 必填列：产线、设备名称、点检项目、状态、点检时间。\r\n" +
                "2. 可选列：点检人、测量值、备注。\r\n" +
                "3. 状态只支持：正常 / 预警 / 异常。";
            return;
        }

        if (_lastImportResult is not null)
        {
            _issuesSubtitleLabel.Text = _lastImportResult.PendingCount > 0
                ? $"导入完成，本批次还有 {_lastImportResult.PendingCount} 条待闭环。"
                : "导入完成，这批数据没有新增待闭环。";
            _issuesTextBox.Text =
                $"批次：{_lastImportResult.BatchKeyword}\r\n" +
                $"来源文件：{_lastImportResult.SourceFileName}\r\n" +
                $"导入记录：{_lastImportResult.ImportedCount} 条\r\n" +
                $"状态分布：正常 {_lastImportResult.NormalCount} / 预警 {_lastImportResult.WarningCount} / 异常 {_lastImportResult.AbnormalCount}\r\n" +
                (_lastImportResult.PendingCount > 0
                    ? "下一步建议：直接点“查看待闭环”。"
                    : "下一步建议：点“查看本批记录”。");
            return;
        }

        var lines = new List<string>();
        if (_currentPreview.ValidationErrors.Count > 0)
        {
            _issuesSubtitleLabel.Text = $"有 {_currentPreview.ValidationErrors.Count} 个阻断问题，先修正。";
            lines.Add("阻断问题：");
            foreach (var error in _currentPreview.ValidationErrors)
            {
                lines.Add($"- {error}");
            }
        }
        else
        {
            _issuesSubtitleLabel.Text = _currentPreview.Warnings.Count > 0
                ? $"没有阻断问题，另有 {_currentPreview.Warnings.Count} 条提醒。"
                : "没有阻断问题，可以直接导入。";
            lines.Add("阻断问题：无");
        }

        lines.Add(string.Empty);
        lines.Add($"风险记录：{_currentPreview.WarningCount + _currentPreview.AbnormalCount} 条");
        lines.Add($"状态分布：正常 {_currentPreview.NormalCount} / 预警 {_currentPreview.WarningCount} / 异常 {_currentPreview.AbnormalCount}");

        if (_currentPreview.Warnings.Count > 0)
        {
            lines.Add(string.Empty);
            lines.Add("提醒：");
            foreach (var warning in _currentPreview.Warnings.Take(5))
            {
                lines.Add($"- {warning}");
            }
        }

        _issuesTextBox.Text = string.Join(Environment.NewLine, lines);
    }

    private string BuildPreviewAnalysis(CsvPreviewState preview)
    {
        var lines = new List<string>();
        if (preview.CanImport)
        {
            lines.Add("当前文件可以导入。下一步直接点“确认导入”。");
        }
        else
        {
            lines.Add("当前文件还不能导入。先按左侧问题清单修正 CSV。");
        }

        lines.Add($"有效记录 {preview.ValidEntryCount} 条，预警 {preview.WarningCount} 条，异常 {preview.AbnormalCount} 条。");
        lines.Add($"原始数据 {preview.RowCount} 行 / {preview.ColumnCount} 列，空值 {preview.MissingValueCount} 个。");

        if (preview.ValidationErrors.Count > 0)
        {
            lines.Add("需要先修正这些问题：");
            foreach (var error in preview.ValidationErrors.Take(5))
            {
                lines.Add($"- {error}");
            }
        }

        if (preview.Warnings.Count > 0)
        {
            lines.Add("导入提醒：");
            foreach (var warning in preview.Warnings.Take(3))
            {
                lines.Add($"- {warning}");
            }
        }

        return string.Join(Environment.NewLine + Environment.NewLine, lines);
    }

    private static string BuildImportAnalysis(InspectionImportResultViewModel result, CsvPreviewState preview)
    {
        var lines = new List<string>
        {
            $"导入完成：{result.ImportedCount} 条记录已写入系统。",
            $"本次批次：{result.BatchKeyword}。这个页到这里结束，后续处理去巡检页。",
            $"状态分布：正常 {result.NormalCount} / 预警 {result.WarningCount} / 异常 {result.AbnormalCount}。",
            $"模板同步：新增 {result.TemplateCreatedCount} 个，更新 {result.TemplateUpdatedCount} 个。",
            $"来源文件：{result.SourceFileName}。"
        };

        if (result.PendingCount > 0)
        {
            lines.Add("这批数据里有待闭环项，建议直接点“查看待闭环”继续处理。");
        }
        else if (preview.ValidEntryCount > 0)
        {
            lines.Add("这批数据没有新增待闭环项，可以直接回巡检页继续查询或导出。");
        }

        return string.Join(Environment.NewLine + Environment.NewLine, lines);
    }

    private CsvPreviewState ParseCsv(string filePath)
    {
        if (!File.Exists(filePath))
        {
            throw new FileNotFoundException("没有找到要导入的 CSV 文件。", filePath);
        }

        using var parser = new TextFieldParser(filePath, Encoding.UTF8, detectEncoding: true)
        {
            TextFieldType = FieldType.Delimited,
            HasFieldsEnclosedInQuotes = true,
            TrimWhiteSpace = false
        };
        parser.SetDelimiters(",");

        if (parser.EndOfData)
        {
            throw new InvalidOperationException("CSV 文件是空的。");
        }

        var rawHeaders = parser.ReadFields();
        if (rawHeaders is null || rawHeaders.Length == 0)
        {
            throw new InvalidOperationException("CSV 表头读取失败。");
        }

        var warnings = new List<string>();
        var headers = BuildHeaders(rawHeaders, warnings);
        var table = new DataTable();
        foreach (var header in headers)
        {
            table.Columns.Add(header);
        }

        var columnMap = ResolveColumns(headers);
        var errors = ValidateRequiredColumns(columnMap);
        var entries = new List<InspectionEntryViewModel>();
        var missingValueCount = 0;
        var rowCount = 0;
        var displayedRowCount = 0;
        var statusCounts = new Dictionary<InspectionStatus, int>
        {
            [InspectionStatus.Normal] = 0,
            [InspectionStatus.Warning] = 0,
            [InspectionStatus.Abnormal] = 0
        };

        while (!parser.EndOfData)
        {
            var fields = parser.ReadFields() ?? [];
            if (fields.All(field => string.IsNullOrWhiteSpace(field)))
            {
                continue;
            }

            EnsureColumnCount(fields.Length, table);
            var values = BuildRowValues(table.Columns.Count, fields, ref missingValueCount);

            if (displayedRowCount < PreviewLimit)
            {
                table.Rows.Add(values.Cast<object>().ToArray());
                displayedRowCount++;
            }

            rowCount++;
            TryCreateEntry(values, rowCount + 1, columnMap, errors, entries, statusCounts);
        }

        return new CsvPreviewState(
            Path.GetFileName(filePath),
            table,
            entries,
            errors,
            warnings,
            rowCount,
            table.Columns.Count,
            missingValueCount,
            displayedRowCount,
            statusCounts[InspectionStatus.Normal],
            statusCounts[InspectionStatus.Warning],
            statusCounts[InspectionStatus.Abnormal]);
    }

    private string[] BuildRowValues(int columnCount, IReadOnlyList<string> fields, ref int missingValueCount)
    {
        var values = new string[columnCount];
        for (var index = 0; index < columnCount; index++)
        {
            var value = index < fields.Count ? fields[index].Trim() : string.Empty;
            values[index] = value;
            if (string.IsNullOrWhiteSpace(value))
            {
                missingValueCount++;
            }
        }

        return values;
    }

    private void TryCreateEntry(
        IReadOnlyList<string> values,
        int displayRowNumber,
        IReadOnlyDictionary<ImportColumn, int> columnMap,
        ICollection<string> errors,
        ICollection<InspectionEntryViewModel> entries,
        IDictionary<InspectionStatus, int> statusCounts)
    {
        if (errors.Any(error => error.StartsWith("缺少", StringComparison.Ordinal)))
        {
            return;
        }

        var lineName = GetRequiredValue(values, columnMap, ImportColumn.LineName);
        var deviceName = GetRequiredValue(values, columnMap, ImportColumn.DeviceName);
        var inspectionItem = GetRequiredValue(values, columnMap, ImportColumn.InspectionItem);
        var statusText = GetRequiredValue(values, columnMap, ImportColumn.Status);
        var checkedAtText = GetRequiredValue(values, columnMap, ImportColumn.CheckedAt);

        if (string.IsNullOrWhiteSpace(lineName) ||
            string.IsNullOrWhiteSpace(deviceName) ||
            string.IsNullOrWhiteSpace(inspectionItem) ||
            string.IsNullOrWhiteSpace(statusText) ||
            string.IsNullOrWhiteSpace(checkedAtText))
        {
            AddError(errors, $"第 {displayRowNumber} 行有必填列为空。");
            return;
        }

        if (!TryParseStatus(statusText, out var status))
        {
            AddError(errors, $"第 {displayRowNumber} 行状态“{statusText}”无法识别。");
            return;
        }

        if (!DateTime.TryParse(checkedAtText, out var checkedAt))
        {
            AddError(errors, $"第 {displayRowNumber} 行点检时间“{checkedAtText}”无法识别。");
            return;
        }

        var measuredValue = 0m;
        var measuredText = GetOptionalValue(values, columnMap, ImportColumn.MeasuredValue);
        if (!string.IsNullOrWhiteSpace(measuredText) && !decimal.TryParse(measuredText, out measuredValue))
        {
            AddError(errors, $"第 {displayRowNumber} 行测量值“{measuredText}”不是有效数字。");
            return;
        }

        var inspector = GetOptionalValue(values, columnMap, ImportColumn.Inspector);
        entries.Add(new InspectionEntryViewModel
        {
            LineName = lineName,
            DeviceName = deviceName,
            InspectionItem = inspectionItem,
            Inspector = string.IsNullOrWhiteSpace(inspector) ? _account : inspector,
            Status = status,
            MeasuredValue = measuredValue,
            CheckedAt = checkedAt,
            Remark = GetOptionalValue(values, columnMap, ImportColumn.Remark)
        });
        statusCounts[status]++;
    }

    private static IReadOnlyDictionary<ImportColumn, int> ResolveColumns(IReadOnlyList<string> headers)
    {
        return new Dictionary<ImportColumn, int>
        {
            [ImportColumn.LineName] = FindColumn(headers, "产线", "line", "line_name"),
            [ImportColumn.DeviceName] = FindColumn(headers, "设备", "设备名称", "device", "device_name"),
            [ImportColumn.InspectionItem] = FindColumn(headers, "点检项目", "巡检项目", "inspection_item", "item"),
            [ImportColumn.Status] = FindColumn(headers, "状态", "结果", "status", "result"),
            [ImportColumn.CheckedAt] = FindColumn(headers, "点检时间", "巡检时间", "时间", "checked_at", "checkedat", "time"),
            [ImportColumn.Inspector] = FindColumn(headers, "点检人", "巡检人", "inspector"),
            [ImportColumn.MeasuredValue] = FindColumn(headers, "测量值", "数值", "measured_value", "value"),
            [ImportColumn.Remark] = FindColumn(headers, "备注", "remark", "comment")
        };
    }

    private static List<string> ValidateRequiredColumns(IReadOnlyDictionary<ImportColumn, int> columnMap)
    {
        var errors = new List<string>();
        foreach (var column in new[]
                 {
                     ImportColumn.LineName,
                     ImportColumn.DeviceName,
                     ImportColumn.InspectionItem,
                     ImportColumn.Status,
                     ImportColumn.CheckedAt
                 })
        {
            if (columnMap[column] < 0)
            {
                errors.Add($"缺少必填列：{GetColumnDisplayName(column)}。");
            }
        }

        return errors;
    }

    private static List<string> BuildHeaders(IReadOnlyList<string> rawHeaders, ICollection<string> warnings)
    {
        var headers = new List<string>(rawHeaders.Count);
        var nameCounts = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);

        for (var index = 0; index < rawHeaders.Count; index++)
        {
            var header = string.IsNullOrWhiteSpace(rawHeaders[index])
                ? $"列{index + 1}"
                : rawHeaders[index].Trim();

            if (string.IsNullOrWhiteSpace(rawHeaders[index]))
            {
                warnings.Add($"第 {index + 1} 列表头为空，系统已自动补名。");
            }

            if (nameCounts.TryGetValue(header, out var count))
            {
                count++;
                nameCounts[header] = count;
                warnings.Add($"表头“{header}”重复，系统已自动追加序号。");
                header = $"{header}_{count}";
            }
            else
            {
                nameCounts[header] = 1;
            }

            headers.Add(header);
        }

        return headers;
    }

    private static void EnsureColumnCount(int fieldCount, DataTable table)
    {
        for (var index = table.Columns.Count; index < fieldCount; index++)
        {
            table.Columns.Add($"扩展列{index + 1}");
        }
    }

    private static int FindColumn(IReadOnlyList<string> headers, params string[] aliases)
    {
        for (var index = 0; index < headers.Count; index++)
        {
            var header = headers[index];
            if (aliases.Any(alias => header.Contains(alias, StringComparison.OrdinalIgnoreCase)))
            {
                return index;
            }
        }

        return -1;
    }

    private static string GetRequiredValue(IReadOnlyList<string> values, IReadOnlyDictionary<ImportColumn, int> columnMap, ImportColumn column)
    {
        var index = columnMap[column];
        return index >= 0 && index < values.Count ? values[index].Trim() : string.Empty;
    }

    private static string GetOptionalValue(IReadOnlyList<string> values, IReadOnlyDictionary<ImportColumn, int> columnMap, ImportColumn column)
    {
        var index = columnMap[column];
        return index >= 0 && index < values.Count ? values[index].Trim() : string.Empty;
    }

    private static bool TryParseStatus(string value, out InspectionStatus status)
    {
        switch (value.Trim().ToLowerInvariant())
        {
            case "正常":
            case "normal":
                status = InspectionStatus.Normal;
                return true;
            case "预警":
            case "warning":
                status = InspectionStatus.Warning;
                return true;
            case "异常":
            case "abnormal":
                status = InspectionStatus.Abnormal;
                return true;
            default:
                status = InspectionStatus.Normal;
                return false;
        }
    }

    private static void AddError(ICollection<string> errors, string message)
    {
        if (errors.Count < 12)
        {
            errors.Add(message);
        }
    }

    private static string GetColumnDisplayName(ImportColumn column)
    {
        return column switch
        {
            ImportColumn.LineName => "产线",
            ImportColumn.DeviceName => "设备名称",
            ImportColumn.InspectionItem => "点检项目",
            ImportColumn.Status => "状态",
            ImportColumn.CheckedAt => "点检时间",
            ImportColumn.Inspector => "点检人",
            ImportColumn.MeasuredValue => "测量值",
            ImportColumn.Remark => "备注",
            _ => "未知列"
        };
    }

    private Panel CreateSectionShell(string title, string subtitle, out Label subtitleLabel, Control content, Padding margin)
    {
        var shell = CreateSurfacePanel(new Padding(18, 14, 18, 18));
        shell.Margin = margin;

        var layout = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            BackColor = Color.Transparent,
            ColumnCount = 1,
            RowCount = 3
        };
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
        layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));

        layout.Controls.Add(CreateTextLabel(title, 11F, FontStyle.Bold, TextPrimaryColor, new Padding(0, 0, 0, 4)), 0, 0);
        subtitleLabel = CreateTextLabel(subtitle, 8.8F, FontStyle.Regular, TextMutedColor, new Padding(0, 0, 0, 12));
        content.Dock = DockStyle.Fill;

        layout.Controls.Add(subtitleLabel, 0, 1);
        layout.Controls.Add(content, 0, 2);
        shell.Controls.Add(layout);
        return shell;
    }

    private Panel CreateMetricCard(string title, Color accent, out Label valueLabel, out Label noteLabel)
    {
        var card = CreateSurfacePanel(new Padding(18, 16, 18, 14));
        card.Margin = new Padding(0, 0, 12, 0);

        var accentBar = new Panel
        {
            Dock = DockStyle.Left,
            Width = 4,
            BackColor = accent
        };

        var body = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            BackColor = Color.Transparent,
            ColumnCount = 1,
            RowCount = 3,
            Padding = new Padding(12, 0, 0, 0)
        };
        body.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
        body.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        body.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
        body.RowStyles.Add(new RowStyle(SizeType.AutoSize));

        body.Controls.Add(CreateTextLabel(title, 9F, FontStyle.Regular, TextMutedColor, new Padding(0, 0, 0, 8)), 0, 0);
        valueLabel = new Label
        {
            AutoEllipsis = true,
            Dock = DockStyle.Fill,
            Font = new Font("Segoe UI", 16F, FontStyle.Bold),
            ForeColor = TextPrimaryColor,
            Margin = Padding.Empty,
            Text = "--"
        };
        noteLabel = CreateTextLabel(string.Empty, 8.5F, FontStyle.Regular, TextSecondaryColor, new Padding(0, 8, 0, 0));
        noteLabel.Dock = DockStyle.Bottom;

        body.Controls.Add(valueLabel, 0, 1);
        body.Controls.Add(noteLabel, 0, 2);
        card.Controls.Add(body);
        card.Controls.Add(accentBar);
        return card;
    }

    private static Label CreateInfoLabel()
    {
        return new Label
        {
            AutoSize = true,
            Font = new Font("Microsoft YaHei UI", 8.8F),
            ForeColor = TextMutedColor
        };
    }

    private static Label CreateTextLabel(string text, float size, FontStyle style, Color color, Padding margin)
    {
        return new Label
        {
            AutoSize = true,
            Dock = DockStyle.Top,
            Font = new Font("Microsoft YaHei UI", size, style),
            ForeColor = color,
            Margin = margin,
            Text = text
        };
    }

    private static Button CreateActionButton(string text, Color accent)
    {
        var button = new Button
        {
            AutoSize = true,
            AutoSizeMode = AutoSizeMode.GrowAndShrink,
            FlatStyle = FlatStyle.Flat,
            Cursor = Cursors.Hand,
            Font = new Font("Microsoft YaHei UI", 9F, FontStyle.Bold),
            ForeColor = TextPrimaryColor,
            BackColor = HeaderBackground,
            Padding = new Padding(14, 8, 14, 8),
            Text = text
        };
        button.FlatAppearance.BorderSize = 1;
        button.FlatAppearance.BorderColor = Color.FromArgb(88, accent);
        button.FlatAppearance.MouseOverBackColor = Color.FromArgb(36, accent);
        button.FlatAppearance.MouseDownBackColor = Color.FromArgb(58, accent);
        return button;
    }

    private static DataGridView CreatePreviewGrid()
    {
        return new DataGridView
        {
            AllowUserToAddRows = false,
            AllowUserToDeleteRows = false,
            AllowUserToResizeRows = false,
            AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
            BackgroundColor = SurfaceBackground,
            BorderStyle = BorderStyle.None,
            ClipboardCopyMode = DataGridViewClipboardCopyMode.EnableWithoutHeaderText,
            ColumnHeadersBorderStyle = DataGridViewHeaderBorderStyle.Single,
            Dock = DockStyle.Fill,
            EnableHeadersVisualStyles = false,
            MultiSelect = false,
            ReadOnly = true,
            RowHeadersVisible = false,
            SelectionMode = DataGridViewSelectionMode.FullRowSelect
        };
    }

    private static TextBox CreateAnalysisTextBox()
    {
        return new TextBox
        {
            BorderStyle = BorderStyle.None,
            Dock = DockStyle.Fill,
            Multiline = true,
            ReadOnly = true,
            ScrollBars = ScrollBars.Vertical,
            BackColor = SurfaceBackground,
            ForeColor = TextSecondaryColor,
            Font = new Font("Microsoft YaHei UI", 9F)
        };
    }

    private static void ApplyGridTheme(DataGridView grid)
    {
        grid.BackgroundColor = SurfaceBackground;
        grid.GridColor = Color.FromArgb(54, 60, 78);
        grid.DefaultCellStyle.BackColor = SurfaceBackground;
        grid.DefaultCellStyle.ForeColor = TextSecondaryColor;
        grid.DefaultCellStyle.SelectionBackColor = Color.FromArgb(48, AccentBlue);
        grid.DefaultCellStyle.SelectionForeColor = TextPrimaryColor;
        grid.DefaultCellStyle.Padding = new Padding(6, 0, 6, 0);
        grid.DefaultCellStyle.Font = new Font("Microsoft YaHei UI", 9F);
        grid.AlternatingRowsDefaultCellStyle.BackColor = Color.FromArgb(24, 26, 36);
        grid.AlternatingRowsDefaultCellStyle.ForeColor = TextSecondaryColor;
        grid.ColumnHeadersDefaultCellStyle.BackColor = HeaderBackground;
        grid.ColumnHeadersDefaultCellStyle.ForeColor = TextPrimaryColor;
        grid.ColumnHeadersDefaultCellStyle.SelectionBackColor = HeaderBackground;
        grid.ColumnHeadersDefaultCellStyle.SelectionForeColor = TextPrimaryColor;
        grid.ColumnHeadersDefaultCellStyle.Font = new Font("Microsoft YaHei UI", 9F, FontStyle.Bold);
        grid.RowTemplate.Height = 34;
    }

    private static SurfacePanel CreateSurfacePanel(Padding padding)
    {
        return new SurfacePanel(16)
        {
            Dock = DockStyle.Fill,
            Padding = padding,
            FillColor = SurfaceBackground,
            BorderColor = SurfaceBorder
        };
    }

    private sealed class SurfacePanel : Panel
    {
        private readonly int _radius;

        public SurfacePanel(int radius)
        {
            _radius = radius;
            SetStyle(
                ControlStyles.UserPaint |
                ControlStyles.AllPaintingInWmPaint |
                ControlStyles.OptimizedDoubleBuffer |
                ControlStyles.ResizeRedraw,
                true);
            DoubleBuffered = true;
            UpdateStyles();
            BackColor = Color.Transparent;
        }

        public Color FillColor { get; init; }

        public Color BorderColor { get; init; }

        protected override void OnPaintBackground(PaintEventArgs e)
        {
            e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
            using var path = CreateRoundedPath(new Rectangle(0, 0, Width - 1, Height - 1), _radius);
            using var fillBrush = new SolidBrush(FillColor);
            using var borderPen = new Pen(BorderColor, 1f);
            e.Graphics.FillPath(fillBrush, path);
            e.Graphics.DrawPath(borderPen, path);
        }
    }

    private static GraphicsPath CreateRoundedPath(Rectangle rect, int radius)
    {
        var path = new GraphicsPath();
        var diameter = radius * 2;
        path.AddArc(rect.X, rect.Y, diameter, diameter, 180, 90);
        path.AddArc(rect.Right - diameter, rect.Y, diameter, diameter, 270, 90);
        path.AddArc(rect.Right - diameter, rect.Bottom - diameter, diameter, diameter, 0, 90);
        path.AddArc(rect.X, rect.Bottom - diameter, diameter, diameter, 90, 90);
        path.CloseFigure();
        return path;
    }

    private enum ImportColumn
    {
        LineName,
        DeviceName,
        InspectionItem,
        Inspector,
        Status,
        MeasuredValue,
        CheckedAt,
        Remark
    }

    private sealed record CsvPreviewState(
        string FileName,
        DataTable PreviewTable,
        IReadOnlyList<InspectionEntryViewModel> Entries,
        IReadOnlyList<string> ValidationErrors,
        IReadOnlyList<string> Warnings,
        int RowCount,
        int ColumnCount,
        int MissingValueCount,
        int DisplayedRowCount,
        int NormalCount,
        int WarningCount,
        int AbnormalCount)
    {
        public bool CanImport => ValidationErrors.Count == 0 && Entries.Count > 0;

        public int ValidEntryCount => Entries.Count;
    }
}
