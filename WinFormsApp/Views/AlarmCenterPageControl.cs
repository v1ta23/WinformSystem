using App.Core.Models;
using WinFormsApp.Controllers;
using WinFormsApp.ViewModels;

namespace WinFormsApp.Views;

internal sealed class AlarmCenterPageControl : UserControl
{
    private static readonly Color PageBackground = Color.FromArgb(10, 10, 15);
    private static readonly Color SurfaceBackground = Color.FromArgb(28, 30, 40);
    private static readonly Color HeaderBackground = Color.FromArgb(22, 24, 33);
    private static readonly Color SurfaceBorder = Color.FromArgb(80, 85, 110);
    private static readonly Color TextPrimaryColor = Color.FromArgb(255, 255, 255);
    private static readonly Color TextSecondaryColor = Color.FromArgb(210, 215, 230);
    private static readonly Color TextMutedColor = Color.FromArgb(160, 170, 190);
    private static readonly Color AccentBlue = Color.FromArgb(88, 130, 255);
    private static readonly Color WarningColor = Color.FromArgb(241, 196, 15);
    private static readonly Color DangerColor = Color.FromArgb(231, 76, 60);
    private static readonly Color SuccessColor = Color.FromArgb(39, 174, 96);

    private readonly string _account;
    private readonly InspectionController _inspectionController;
    private readonly Label _generatedAtLabel;
    private Label _pendingValueLabel = null!;
    private Label _abnormalValueLabel = null!;
    private Label _warningValueLabel = null!;
    private Label _closedValueLabel = null!;
    private DataGridView _pendingGrid = null!;
    private DataGridView _historyGrid = null!;
    private Label _historyEmptyLabel = null!;

    private IReadOnlyList<AlarmRow> _pendingRows = Array.Empty<AlarmRow>();
    private IReadOnlyList<AlarmRow> _historyRows = Array.Empty<AlarmRow>();

    public event EventHandler? DataChanged;

    public AlarmCenterPageControl(string account, InspectionController inspectionController)
    {
        _account = account;
        _inspectionController = inspectionController;
        Dock = DockStyle.Fill;
        BackColor = PageBackground;
        Font = new Font("Microsoft YaHei UI", 9F);
        Padding = new Padding(30, 20, 30, 20);

        _generatedAtLabel = CreateInfoLabel();
        var refreshButton = CreateRefreshButton();
        refreshButton.Click += (_, _) => RefreshData();
        var closeButton = CreateCloseButton();
        closeButton.Click += (_, _) => CloseSelectedAlarm();

        var root = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            BackColor = Color.Transparent,
            ColumnCount = 1,
            RowCount = 3
        };
        root.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 112F));
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 132F));
        root.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));

        root.Controls.Add(BuildHeader(refreshButton, closeButton), 0, 0);
        root.Controls.Add(BuildSummaryArea(), 0, 1);
        root.Controls.Add(BuildBodyArea(), 0, 2);

        Controls.Add(root);
        ApplyTheme();
        Load += (_, _) => RefreshData();
    }

    public void RefreshData()
    {
        var dashboard = _inspectionController.Load(new InspectionFilterViewModel());
        var records = dashboard.Records
            .Where(record => !record.IsRevoked && record.Status != InspectionStatus.Normal)
            .OrderByDescending(record => record.CheckedAtValue)
            .ToList();

        _pendingRows = records
            .Where(record => !record.IsClosed)
            .Select(record => ToRow(record, "待处理"))
            .ToList();

        _historyRows = records
            .Where(record => record.IsClosed)
            .Take(10)
            .Select(record => ToRow(record, "已闭环"))
            .ToList();

        _pendingValueLabel.Text = _pendingRows.Count.ToString();
        _abnormalValueLabel.Text = _pendingRows.Count(row => row.StatusText == "异常").ToString();
        _warningValueLabel.Text = _pendingRows.Count(row => row.StatusText == "预警").ToString();
        _closedValueLabel.Text = _historyRows.Count.ToString();
        _generatedAtLabel.Text = $"最近刷新 {dashboard.GeneratedAt:yyyy-MM-dd HH:mm:ss}";

        _pendingGrid.DataSource = _pendingRows.ToList();
        _historyGrid.DataSource = _historyRows.ToList();
        _historyGrid.Visible = _historyRows.Count > 0;
        _historyEmptyLabel.Visible = _historyRows.Count == 0;
    }

    public void ApplyTheme()
    {
        ApplyDarkVisualTree(this);
    }

    private Control BuildHeader(Button refreshButton, Button closeButton)
    {
        var header = CreateCardPanel();
        header.Padding = new Padding(18, 10, 18, 10);
        header.Margin = new Padding(0, 0, 0, 12);

        var titleLabel = new Label
        {
            AutoSize = true,
            Font = new Font("Microsoft YaHei UI", 20F, FontStyle.Bold),
            ForeColor = TextPrimaryColor,
            Text = "报警中心"
        };
        var subtitleLabel = new Label
        {
            AutoSize = true,
            Font = new Font("Microsoft YaHei UI", 9.5F),
            ForeColor = TextMutedColor,
            Text = "这里看完整告警列表和处理状态，首页后面只挑重点摘要。"
        };

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
        titleLabel.Dock = DockStyle.Top;
        titleLabel.Margin = new Padding(0, 0, 0, 2);
        titleLabel.Font = new Font("Microsoft YaHei UI", 16F, FontStyle.Bold);
        subtitleLabel.Dock = DockStyle.Top;
        subtitleLabel.Margin = new Padding(0, 0, 0, 2);
        subtitleLabel.Font = new Font("Microsoft YaHei UI", 9F);
        subtitleLabel.ForeColor = TextSecondaryColor;
        _generatedAtLabel.Dock = DockStyle.Top;
        _generatedAtLabel.Margin = Padding.Empty;
        titlePanel.Controls.Add(titleLabel, 0, 0);
        titlePanel.Controls.Add(subtitleLabel, 0, 1);
        titlePanel.Controls.Add(_generatedAtLabel, 0, 2);

        var actionPanel = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            AutoSize = true,
            AutoSizeMode = AutoSizeMode.GrowAndShrink,
            FlowDirection = FlowDirection.RightToLeft,
            WrapContents = false,
            BackColor = Color.Transparent,
            Margin = Padding.Empty
        };
        closeButton.Margin = new Padding(10, 0, 0, 0);
        refreshButton.Margin = Padding.Empty;
        actionPanel.Controls.Add(refreshButton);
        actionPanel.Controls.Add(closeButton);

        var layout = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            BackColor = Color.Transparent,
            ColumnCount = 2,
            RowCount = 1,
            Margin = Padding.Empty,
            Padding = Padding.Empty
        };
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
        layout.Controls.Add(titlePanel, 0, 0);
        layout.Controls.Add(actionPanel, 1, 0);

        header.Controls.Add(layout);
        return header;
    }

    private Control BuildSummaryArea()
    {
        var layout = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            BackColor = Color.Transparent,
            ColumnCount = 4,
            RowCount = 1,
            Margin = new Padding(0, 0, 0, 12)
        };
        layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));

        for (var index = 0; index < 4; index++)
        {
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 25F));
        }

        _pendingValueLabel = CreateMetricValueLabel();
        _abnormalValueLabel = CreateMetricValueLabel();
        _warningValueLabel = CreateMetricValueLabel();
        _closedValueLabel = CreateMetricValueLabel();

        layout.Controls.Add(BuildMetricCard("待处理总数", _pendingValueLabel, "未闭环的预警和异常", DangerColor), 0, 0);
        layout.Controls.Add(BuildMetricCard("异常", _abnormalValueLabel, "优先级最高，建议先处理", DangerColor), 1, 0);
        layout.Controls.Add(BuildMetricCard("预警", _warningValueLabel, "可安排巡检复核", WarningColor), 2, 0);
        layout.Controls.Add(BuildMetricCard("最近闭环", _closedValueLabel, "方便回看最近处理结果", SuccessColor), 3, 0);
        return layout;
    }

    private Control BuildBodyArea()
    {
        var layout = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            BackColor = Color.Transparent,
            ColumnCount = 1,
            RowCount = 2
        };
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
        layout.RowStyles.Add(new RowStyle(SizeType.Percent, 58F));
        layout.RowStyles.Add(new RowStyle(SizeType.Percent, 42F));

        _pendingGrid = CreateGrid();
        _pendingGrid.Columns.Add(CreateTextColumn(nameof(AlarmRow.CheckedAt), "时间", 140));
        _pendingGrid.Columns.Add(CreateTextColumn(nameof(AlarmRow.LineName), "产线", 90));
        _pendingGrid.Columns.Add(CreateTextColumn(nameof(AlarmRow.DeviceName), "设备", 120));
        _pendingGrid.Columns.Add(CreateTextColumn(nameof(AlarmRow.InspectionItem), "问题项", 140));
        _pendingGrid.Columns.Add(CreateTextColumn(nameof(AlarmRow.StatusText), "状态", 70));
        _pendingGrid.Columns.Add(CreateTextColumn(nameof(AlarmRow.ProcessingState), "处理状态", 100));
        _pendingGrid.Columns.Add(CreateTextColumn(nameof(AlarmRow.Inspector), "巡检人", 90));
        _pendingGrid.Columns.Add(CreateTextColumn(nameof(AlarmRow.Remark), "备注", 220));
        _pendingGrid.CellFormatting += GridOnCellFormatting;
        _pendingGrid.CellDoubleClick += (_, _) => CloseSelectedAlarm();

        var pendingPanel = CreateCardPanel();
        pendingPanel.Padding = new Padding(20, 18, 20, 20);
        pendingPanel.Controls.Add(_pendingGrid);
        pendingPanel.Controls.Add(BuildSectionHeader("待处理告警", "这里看完整清单，确认、闭环动作仍通过巡检流程完成。"));

        _historyGrid = CreateGrid();
        _historyGrid.Columns.Add(CreateTextColumn(nameof(AlarmRow.CheckedAt), "时间", 140));
        _historyGrid.Columns.Add(CreateTextColumn(nameof(AlarmRow.LineName), "产线", 90));
        _historyGrid.Columns.Add(CreateTextColumn(nameof(AlarmRow.DeviceName), "设备", 120));
        _historyGrid.Columns.Add(CreateTextColumn(nameof(AlarmRow.InspectionItem), "问题项", 140));
        _historyGrid.Columns.Add(CreateTextColumn(nameof(AlarmRow.StatusText), "状态", 70));
        _historyGrid.Columns.Add(CreateTextColumn(nameof(AlarmRow.ProcessingState), "处理状态", 100));
        _historyGrid.Columns.Add(CreateTextColumn(nameof(AlarmRow.Remark), "处理说明", 320));
        _historyGrid.CellFormatting += GridOnCellFormatting;

        var historyPanel = CreateCardPanel();
        historyPanel.Padding = new Padding(20, 18, 20, 20);
        historyPanel.Margin = new Padding(0, 12, 0, 0);
        _historyEmptyLabel = CreateEmptyStateLabel("暂无闭环记录");
        historyPanel.Controls.Add(_historyGrid);
        historyPanel.Controls.Add(_historyEmptyLabel);
        historyPanel.Controls.Add(BuildSectionHeader("最近闭环", "只保留最近处理完的记录，方便回看。"));

        layout.Controls.Add(pendingPanel, 0, 0);
        layout.Controls.Add(historyPanel, 0, 1);
        return layout;
    }

    private void GridOnCellFormatting(object? sender, DataGridViewCellFormattingEventArgs e)
    {
        if (sender is not DataGridView grid || e.Value is not string text)
        {
            return;
        }

        var propertyName = grid.Columns[e.ColumnIndex].DataPropertyName;
        if (propertyName == nameof(AlarmRow.StatusText))
        {
            var cellStyle = e.CellStyle;
            if (cellStyle is null)
            {
                return;
            }

            cellStyle.ForeColor = text switch
            {
                "异常" => DangerColor,
                "预警" => WarningColor,
                _ => TextSecondaryColor
            };
        }

        if (propertyName == nameof(AlarmRow.ProcessingState))
        {
            var cellStyle = e.CellStyle;
            if (cellStyle is null)
            {
                return;
            }

            cellStyle.ForeColor = text switch
            {
                "待处理" => DangerColor,
                "已闭环" => SuccessColor,
                _ => TextSecondaryColor
            };
        }
    }

    private AlarmRow? GetSelectedPendingRow()
    {
        return _pendingGrid.CurrentRow?.DataBoundItem as AlarmRow;
    }

    private void CloseSelectedAlarm()
    {
        var row = GetSelectedPendingRow();
        if (row is null)
        {
            MessageBox.Show(this, "请先选中要闭环的告警。", "告警闭环", MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }

        var closureRemark = ShowActionInputDialog(
            "告警闭环",
            $"请填写 {row.DeviceName} / {row.InspectionItem} 的处理说明。",
            "提交闭环");
        if (closureRemark is null)
        {
            return;
        }

        try
        {
            CloseAlarm(row.RecordId, closureRemark);
        }
        catch (Exception ex)
        {
            MessageBox.Show(this, ex.Message, "告警闭环失败", MessageBoxButtons.OK, MessageBoxIcon.Warning);
        }
    }

    private void CloseAlarm(Guid recordId, string closureRemark)
    {
        _inspectionController.Close(recordId, _account, closureRemark);
        RefreshData();
        DataChanged?.Invoke(this, EventArgs.Empty);
    }

    private string? ShowActionInputDialog(string title, string description, string confirmText)
    {
        using var window = new Form
        {
            Text = title,
            StartPosition = FormStartPosition.CenterParent,
            Size = new Size(520, 320),
            MinimumSize = new Size(480, 300),
            FormBorderStyle = FormBorderStyle.FixedDialog,
            MaximizeBox = false,
            MinimizeBox = false,
            BackColor = PageBackground,
            Font = Font,
            ShowIcon = false,
            ShowInTaskbar = false
        };

        var shell = new BufferedPanel
        {
            Dock = DockStyle.Fill,
            BackColor = PageBackground,
            Padding = new Padding(18)
        };

        var card = CreateCardPanel();
        card.Dock = DockStyle.Fill;
        card.Padding = new Padding(18);

        var descriptionLabel = new Label
        {
            AutoSize = true,
            Dock = DockStyle.Top,
            ForeColor = TextSecondaryColor,
            Text = description
        };

        var inputBox = new TextBox
        {
            Dock = DockStyle.Fill,
            Multiline = true,
            BackColor = Color.FromArgb(18, 22, 30),
            ForeColor = TextPrimaryColor,
            BorderStyle = BorderStyle.FixedSingle
        };

        var buttonPanel = new FlowLayoutPanel
        {
            Dock = DockStyle.Bottom,
            AutoSize = true,
            FlowDirection = FlowDirection.LeftToRight,
            WrapContents = false,
            Padding = new Padding(0, 12, 0, 0)
        };

        var confirmButton = CreateRefreshButton();
        confirmButton.Text = confirmText;
        confirmButton.Click += (_, _) =>
        {
            if (string.IsNullOrWhiteSpace(inputBox.Text))
            {
                MessageBox.Show(window, "请先填写处理说明。", title, MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            window.DialogResult = DialogResult.OK;
            window.Close();
        };

        var cancelButton = CreateCloseButton();
        cancelButton.Text = "取消";
        cancelButton.Click += (_, _) =>
        {
            window.DialogResult = DialogResult.Cancel;
            window.Close();
        };

        buttonPanel.Controls.Add(confirmButton);
        buttonPanel.Controls.Add(cancelButton);

        card.Controls.Add(inputBox);
        card.Controls.Add(buttonPanel);
        card.Controls.Add(descriptionLabel);
        shell.Controls.Add(card);
        window.Controls.Add(shell);
        ApplyDarkVisualTree(window);

        return window.ShowDialog(this) == DialogResult.OK
            ? inputBox.Text.Trim()
            : null;
    }

    private static AlarmRow ToRow(InspectionRecordViewModel record, string processingState)
    {
        return new AlarmRow
        {
            RecordId = record.Id,
            CheckedAt = record.CheckedAtValue.ToString("MM-dd HH:mm"),
            LineName = record.LineName,
            DeviceName = record.DeviceName,
            InspectionItem = record.InspectionItem,
            StatusText = record.StatusText,
            ProcessingState = processingState,
            Inspector = record.Inspector,
            Remark = string.IsNullOrWhiteSpace(record.ActionRemark) ? record.Remark : record.ActionRemark
        };
    }

    private static Control BuildMetricCard(string title, Label valueLabel, string note, Color accentColor)
    {
        var card = CreateCardPanel();
        card.Margin = new Padding(0, 0, 12, 0);
        card.Padding = new Padding(18, 14, 18, 14);

        var shell = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            BackColor = Color.Transparent,
            ColumnCount = 2,
            RowCount = 1,
            Margin = Padding.Empty,
            Padding = Padding.Empty
        };
        shell.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 4F));
        shell.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
        shell.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));

        var accent = new Panel
        {
            Dock = DockStyle.Fill,
            Margin = Padding.Empty,
            BackColor = accentColor
        };

        var layout = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            BackColor = Color.Transparent,
            ColumnCount = 2,
            RowCount = 2,
            Margin = new Padding(14, 0, 0, 0),
            Padding = new Padding(0, 6, 0, 0)
        };
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));

        var titleLabel = new Label
        {
            AutoSize = true,
            Dock = DockStyle.Top,
            Font = new Font("Microsoft YaHei UI", 9F),
            ForeColor = TextMutedColor,
            Margin = new Padding(0, 2, 0, 6),
            Text = title
        };
        valueLabel.Dock = DockStyle.Right;
        valueLabel.Margin = new Padding(16, 0, 0, 0);
        valueLabel.TextAlign = ContentAlignment.MiddleRight;
        var noteLabel = new Label
        {
            AutoSize = false,
            AutoEllipsis = true,
            Dock = DockStyle.Fill,
            Font = new Font("Microsoft YaHei UI", 8.8F),
            ForeColor = TextMutedColor,
            Margin = new Padding(0, 2, 0, 0),
            Text = note,
            TextAlign = ContentAlignment.TopLeft
        };

        layout.Controls.Add(titleLabel, 0, 0);
        layout.Controls.Add(valueLabel, 1, 0);
        layout.Controls.Add(noteLabel, 0, 1);
        layout.SetColumnSpan(noteLabel, 2);

        shell.Controls.Add(accent, 0, 0);
        shell.Controls.Add(layout, 1, 0);
        card.Controls.Add(shell);
        return card;
    }

    private static Control BuildSectionHeader(string title, string subtitle)
    {
        var host = new Panel
        {
            Dock = DockStyle.Top,
            Height = 48,
            BackColor = Color.Transparent
        };

        var titleLabel = new Label
        {
            AutoSize = true,
            Font = new Font("Microsoft YaHei UI", 12.5F, FontStyle.Bold),
            ForeColor = TextPrimaryColor,
            Text = title,
            Location = new Point(0, 0)
        };
        var subtitleLabel = new Label
        {
            AutoSize = true,
            Font = new Font("Microsoft YaHei UI", 8.8F),
            ForeColor = TextMutedColor,
            Text = subtitle,
            Location = new Point(0, 24)
        };

        host.Controls.Add(titleLabel);
        host.Controls.Add(subtitleLabel);
        return host;
    }

    private static BufferedPanel CreateCardPanel()
    {
        return new BufferedPanel
        {
            Dock = DockStyle.Fill,
            BackColor = SurfaceBackground,
            Margin = new Padding(0)
        };
    }

    private static DataGridView CreateGrid()
    {
        var grid = new DataGridView
        {
            Dock = DockStyle.Fill,
            AllowUserToAddRows = false,
            AllowUserToDeleteRows = false,
            AllowUserToResizeRows = false,
            AutoGenerateColumns = false,
            AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
            BackgroundColor = SurfaceBackground,
            BorderStyle = BorderStyle.None,
            EnableHeadersVisualStyles = false,
            MultiSelect = false,
            ReadOnly = true,
            RowHeadersVisible = false,
            ScrollBars = ScrollBars.Vertical,
            SelectionMode = DataGridViewSelectionMode.FullRowSelect
        };

        grid.ColumnHeadersDefaultCellStyle.BackColor = HeaderBackground;
        grid.ColumnHeadersDefaultCellStyle.ForeColor = TextPrimaryColor;
        grid.ColumnHeadersDefaultCellStyle.Font = new Font("Microsoft YaHei UI", 9F, FontStyle.Bold);
        grid.DefaultCellStyle.BackColor = SurfaceBackground;
        grid.DefaultCellStyle.ForeColor = TextSecondaryColor;
        grid.DefaultCellStyle.SelectionBackColor = Color.FromArgb(45, 56, 78);
        grid.DefaultCellStyle.SelectionForeColor = TextPrimaryColor;
        grid.GridColor = SurfaceBorder;
        return grid;
    }

    private static DataGridViewTextBoxColumn CreateTextColumn(
        string dataPropertyName,
        string headerText,
        float fillWeight,
        int minimumWidth = 68)
    {
        return new DataGridViewTextBoxColumn
        {
            DataPropertyName = dataPropertyName,
            HeaderText = headerText,
            AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill,
            FillWeight = fillWeight,
            MinimumWidth = minimumWidth
        };
    }

    private static Button CreateRefreshButton()
    {
        var button = new Button
        {
            AutoSize = true,
            BackColor = AccentBlue,
            FlatStyle = FlatStyle.Flat,
            Font = new Font("Microsoft YaHei UI", 9F, FontStyle.Bold),
            ForeColor = TextPrimaryColor,
            Margin = new Padding(0, 0, 0, 6),
            Padding = new Padding(14, 6, 14, 6),
            Text = "刷新告警",
            UseVisualStyleBackColor = false
        };
        button.FlatAppearance.BorderSize = 0;
        return button;
    }

    private static Button CreateCloseButton()
    {
        var button = new Button
        {
            AutoSize = true,
            BackColor = Color.FromArgb(56, 63, 78),
            FlatStyle = FlatStyle.Flat,
            Font = new Font("Microsoft YaHei UI", 9F, FontStyle.Bold),
            ForeColor = TextPrimaryColor,
            Margin = new Padding(0, 0, 0, 6),
            Padding = new Padding(14, 6, 14, 6),
            Text = "闭环选中告警",
            UseVisualStyleBackColor = false
        };
        button.FlatAppearance.BorderColor = Color.FromArgb(86, 98, 118);
        button.FlatAppearance.BorderSize = 1;
        return button;
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

    private static Label CreateMetricValueLabel()
    {
        return new Label
        {
            AutoSize = true,
            Font = new Font("Microsoft YaHei UI", 18F, FontStyle.Bold),
            ForeColor = TextPrimaryColor
        };
    }

    private static Label CreateEmptyStateLabel(string text)
    {
        return new Label
        {
            Dock = DockStyle.Fill,
            Font = new Font("Microsoft YaHei UI", 9.5F),
            ForeColor = TextMutedColor,
            Text = text,
            TextAlign = ContentAlignment.MiddleCenter,
            Visible = false
        };
    }

    private static void ApplyDarkVisualTree(Control root)
    {
        foreach (Control control in root.Controls)
        {
            switch (control)
            {
                case TableLayoutPanel table:
                    table.BackColor = Color.Transparent;
                    break;
                case FlowLayoutPanel flow:
                    flow.BackColor = Color.Transparent;
                    break;
                case Panel panel:
                    panel.BackColor = panel.BackColor == Color.Transparent ? Color.Transparent : SurfaceBackground;
                    break;
                case Button button:
                    button.BackColor = AccentBlue;
                    button.ForeColor = TextPrimaryColor;
                    break;
                case DataGridView grid:
                    grid.BackgroundColor = SurfaceBackground;
                    grid.GridColor = SurfaceBorder;
                    break;
            }

            ApplyDarkVisualTree(control);
        }
    }

    private sealed class AlarmRow
    {
        public Guid RecordId { get; init; }

        public string CheckedAt { get; init; } = string.Empty;

        public string LineName { get; init; } = string.Empty;

        public string DeviceName { get; init; } = string.Empty;

        public string InspectionItem { get; init; } = string.Empty;

        public string StatusText { get; init; } = string.Empty;

        public string ProcessingState { get; init; } = string.Empty;

        public string Inspector { get; init; } = string.Empty;

        public string Remark { get; init; } = string.Empty;
    }
}
