using App.Core.Models;
using WinFormsApp.Controllers;
using WinFormsApp.ViewModels;

namespace WinFormsApp.Views;

internal sealed class DeviceMonitorPageControl : UserControl
{
    private static readonly Color PageBackground = Color.FromArgb(10, 10, 15);
    private static readonly Color SurfaceBackground = Color.FromArgb(28, 30, 40);
    private static readonly Color HeaderBackground = Color.FromArgb(22, 24, 33);
    private static readonly Color SurfaceBorder = Color.FromArgb(80, 85, 110);
    private static readonly Color TextPrimaryColor = Color.FromArgb(255, 255, 255);
    private static readonly Color TextSecondaryColor = Color.FromArgb(210, 215, 230);
    private static readonly Color TextMutedColor = Color.FromArgb(160, 170, 190);
    private static readonly Color AccentBlue = Color.FromArgb(88, 130, 255);
    private static readonly Color SuccessColor = Color.FromArgb(39, 174, 96);
    private static readonly Color WarningColor = Color.FromArgb(241, 196, 15);
    private static readonly Color DangerColor = Color.FromArgb(231, 76, 60);

    private readonly InspectionController _inspectionController;
    private readonly Label _generatedAtLabel;
    private Label _deviceCountValueLabel = null!;
    private Label _issueDeviceValueLabel = null!;
    private Label _issueDeviceNoteLabel = null!;
    private Label _pendingCountValueLabel = null!;
    private Label _pendingCountNoteLabel = null!;
    private Label _healthyCountValueLabel = null!;
    private Label _healthyCountNoteLabel = null!;
    private Label _focusDeviceLabel = null!;
    private Label _focusDetailLabel = null!;
    private DataGridView _deviceGrid = null!;
    private DataGridView _attentionGrid = null!;
    private Label _attentionEmptyLabel = null!;

    private IReadOnlyList<DeviceRow> _deviceRows = Array.Empty<DeviceRow>();
    private IReadOnlyList<AttentionRow> _attentionRows = Array.Empty<AttentionRow>();

    public DeviceMonitorPageControl(InspectionController inspectionController)
    {
        _inspectionController = inspectionController;
        Dock = DockStyle.Fill;
        BackColor = PageBackground;
        Font = new Font("Microsoft YaHei UI", 9F);
        Padding = new Padding(30, 20, 30, 20);

        _generatedAtLabel = CreateInfoLabel();
        var refreshButton = CreateRefreshButton();
        refreshButton.Click += (_, _) => RefreshData();

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

        root.Controls.Add(BuildHeader(refreshButton), 0, 0);
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
            .Where(record => !record.IsRevoked)
            .OrderByDescending(record => record.CheckedAtValue)
            .ToList();

        _deviceRows = records
            .GroupBy(record => new { record.LineName, record.DeviceName })
            .Select(group =>
            {
                var latest = group.First();
                var pendingCount = group.Count(record => record.Status != InspectionStatus.Normal && !record.IsClosed);
                var abnormalCount = group.Count(record => record.Status == InspectionStatus.Abnormal && !record.IsClosed);
                var warningCount = group.Count(record => record.Status == InspectionStatus.Warning && !record.IsClosed);

                return new DeviceRow
                {
                    LineName = latest.LineName,
                    DeviceName = latest.DeviceName,
                    LatestStatus = latest.StatusText,
                    LatestCheckedAt = latest.CheckedAtValue.ToString("MM-dd HH:mm"),
                    Inspector = latest.Inspector,
                    PendingCount = pendingCount,
                    AttentionLevel = abnormalCount > 0
                        ? "异常待处理"
                        : warningCount > 0
                            ? "预警待确认"
                            : "状态稳定"
                };
            })
            .OrderByDescending(row => row.PendingCount)
            .ThenBy(row => row.LineName)
            .ThenBy(row => row.DeviceName)
            .ToList();

        _attentionRows = records
            .Where(record => record.Status != InspectionStatus.Normal && !record.IsClosed)
            .Take(8)
            .Select(record => new AttentionRow
            {
                DeviceName = record.DeviceName,
                InspectionItem = record.InspectionItem,
                StatusText = record.StatusText,
                CheckedAt = record.CheckedAtValue.ToString("MM-dd HH:mm"),
                Detail = $"{record.LineName} / {record.Inspector}"
            })
            .ToList();

        var focusRow = _deviceRows.FirstOrDefault(row => row.PendingCount > 0) ?? _deviceRows.FirstOrDefault();
        var pendingDeviceCount = _deviceRows.Count(row => row.PendingCount > 0);
        var healthyDeviceCount = Math.Max(0, _deviceRows.Count - pendingDeviceCount);

        _deviceCountValueLabel.Text = _deviceRows.Count.ToString();
        _pendingCountValueLabel.Text = pendingDeviceCount.ToString();
        _pendingCountNoteLabel.Text = pendingDeviceCount == 0 ? "当前没有待处理设备" : "优先处理有未闭环问题的设备";
        _healthyCountValueLabel.Text = healthyDeviceCount.ToString();
        _healthyCountNoteLabel.Text = healthyDeviceCount == 0 ? "暂时没有稳定设备" : "最近一次巡检结果正常";

        if (focusRow is null)
        {
            _issueDeviceValueLabel.Text = "--";
            _issueDeviceNoteLabel.Text = "暂时没有设备数据";
            _focusDeviceLabel.Text = "暂无重点设备";
            _focusDetailLabel.Text = "等有巡检记录后，这里再显示关注设备。";
        }
        else
        {
            _issueDeviceValueLabel.Text = focusRow.DeviceName;
            _issueDeviceNoteLabel.Text = $"{focusRow.LineName} / {focusRow.AttentionLevel}";
            _focusDeviceLabel.Text = $"{focusRow.LineName} / {focusRow.DeviceName}";
            _focusDetailLabel.Text = focusRow.PendingCount > 0
                ? $"当前有 {focusRow.PendingCount} 条待处理问题，建议先进入报警中心或巡检页。"
                : $"最近巡检时间 {focusRow.LatestCheckedAt}，当前状态稳定。";
        }

        _generatedAtLabel.Text = $"最近刷新 {dashboard.GeneratedAt:yyyy-MM-dd HH:mm:ss}";
        _deviceGrid.DataSource = _deviceRows.ToList();
        _attentionGrid.DataSource = _attentionRows.ToList();
        _attentionGrid.Visible = _attentionRows.Count > 0;
        _attentionEmptyLabel.Visible = _attentionRows.Count == 0;
    }

    public void ApplyTheme()
    {
        ApplyDarkVisualTree(this);
    }

    private Control BuildHeader(Button refreshButton)
    {
        var header = CreateCardPanel();
        header.Padding = new Padding(18, 10, 18, 10);
        header.Margin = new Padding(0, 0, 0, 12);

        var titleLabel = new Label
        {
            AutoSize = true,
            Font = new Font("Microsoft YaHei UI", 20F, FontStyle.Bold),
            ForeColor = TextPrimaryColor,
            Text = "设备监控"
        };
        var subtitleLabel = new Label
        {
            AutoSize = true,
            Font = new Font("Microsoft YaHei UI", 9.5F),
            ForeColor = TextMutedColor,
            Text = "先把设备维度的工作面做出来，首页后面只抽摘要。"
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
        refreshButton.Margin = Padding.Empty;
        actionPanel.Controls.Add(refreshButton);

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

        _deviceCountValueLabel = CreateMetricValueLabel();
        var deviceNoteLabel = CreateMetricNoteLabel("当前有巡检数据的设备数");

        _issueDeviceValueLabel = CreateMetricValueLabel();
        _issueDeviceValueLabel.Font = new Font("Microsoft YaHei UI", 16F, FontStyle.Bold);
        _issueDeviceNoteLabel = CreateMetricNoteLabel();

        _pendingCountValueLabel = CreateMetricValueLabel();
        _pendingCountNoteLabel = CreateMetricNoteLabel();

        _healthyCountValueLabel = CreateMetricValueLabel();
        _healthyCountNoteLabel = CreateMetricNoteLabel();

        layout.Controls.Add(BuildMetricCard("设备总数", _deviceCountValueLabel, deviceNoteLabel, AccentBlue), 0, 0);
        layout.Controls.Add(BuildMetricCard("当前关注设备", _issueDeviceValueLabel, _issueDeviceNoteLabel, WarningColor), 1, 0);
        layout.Controls.Add(BuildMetricCard("待处理设备", _pendingCountValueLabel, _pendingCountNoteLabel, DangerColor), 2, 0);
        layout.Controls.Add(BuildMetricCard("状态稳定", _healthyCountValueLabel, _healthyCountNoteLabel, SuccessColor), 3, 0);
        return layout;
    }

    private Control BuildBodyArea()
    {
        var layout = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            BackColor = Color.Transparent,
            ColumnCount = 2,
            RowCount = 1
        };
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 64F));
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 36F));

        _deviceGrid = CreateGrid();
        _deviceGrid.Columns.Add(CreateTextColumn(nameof(DeviceRow.LineName), "产线", 90));
        _deviceGrid.Columns.Add(CreateTextColumn(nameof(DeviceRow.DeviceName), "设备", 160));
        _deviceGrid.Columns.Add(CreateTextColumn(nameof(DeviceRow.LatestStatus), "最近状态", 90));
        _deviceGrid.Columns.Add(CreateTextColumn(nameof(DeviceRow.AttentionLevel), "关注级别", 120));
        _deviceGrid.Columns.Add(CreateTextColumn(nameof(DeviceRow.PendingCount), "待处理", 70));
        _deviceGrid.Columns.Add(CreateTextColumn(nameof(DeviceRow.LatestCheckedAt), "最近巡检", 150));
        _deviceGrid.Columns.Add(CreateTextColumn(nameof(DeviceRow.Inspector), "巡检人", 90));
        _deviceGrid.CellFormatting += DeviceGridOnCellFormatting;

        var devicePanel = CreateCardPanel();
        devicePanel.Padding = new Padding(20, 18, 20, 20);
        _deviceGrid.Dock = DockStyle.Fill;
        _deviceGrid.Margin = new Padding(0, 60, 0, 0);
        devicePanel.Controls.Add(_deviceGrid);
        devicePanel.Controls.Add(BuildSectionHeader("设备列表", "详细控制和更多参数后面继续往这个页面里加。"));

        var rightLayout = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            BackColor = Color.Transparent,
            ColumnCount = 1,
            RowCount = 2,
            Margin = new Padding(12, 0, 0, 0)
        };
        rightLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
        rightLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 140F));
        rightLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));

        _focusDeviceLabel = new Label
        {
            AutoSize = true,
            Font = new Font("Microsoft YaHei UI", 16F, FontStyle.Bold),
            ForeColor = TextPrimaryColor,
            Text = "--"
        };
        _focusDetailLabel = new Label
        {
            Dock = DockStyle.Fill,
            Font = new Font("Microsoft YaHei UI", 9.5F),
            ForeColor = TextSecondaryColor,
            Text = string.Empty
        };

        var focusPanel = CreateCardPanel();
        focusPanel.Padding = new Padding(20, 18, 20, 18);
        focusPanel.Controls.Add(_focusDetailLabel);
        focusPanel.Controls.Add(_focusDeviceLabel);
        focusPanel.Controls.Add(BuildSectionHeader("重点设备", "先处理最该看的设备，首页以后只显示这条摘要。"));
        _focusDetailLabel.BringToFront();
        _focusDetailLabel.Dock = DockStyle.Bottom;
        _focusDeviceLabel.Location = new Point(20, 60);

        _attentionGrid = CreateGrid();
        _attentionGrid.Columns.Add(CreateTextColumn(nameof(AttentionRow.DeviceName), "设备", 120));
        _attentionGrid.Columns.Add(CreateTextColumn(nameof(AttentionRow.InspectionItem), "问题项", 120));
        _attentionGrid.Columns.Add(CreateTextColumn(nameof(AttentionRow.StatusText), "状态", 70));
        _attentionGrid.Columns.Add(CreateTextColumn(nameof(AttentionRow.CheckedAt), "时间", 120));
        _attentionGrid.Columns.Add(CreateTextColumn(nameof(AttentionRow.Detail), "说明", 140));
        _attentionGrid.CellFormatting += AttentionGridOnCellFormatting;
        _attentionGrid.Columns[_attentionGrid.Columns.Count - 1].Visible = false;

        var attentionPanel = CreateCardPanel();
        attentionPanel.Padding = new Padding(20, 18, 20, 20);
        _attentionEmptyLabel = CreateEmptyStateLabel("暂无未闭环问题");
        _attentionGrid.Dock = DockStyle.Fill;
        _attentionGrid.Margin = new Padding(0, 60, 0, 0);
        attentionPanel.Controls.Add(_attentionGrid);
        attentionPanel.Controls.Add(_attentionEmptyLabel);
        attentionPanel.Controls.Add(BuildSectionHeader("最新关注", "这里只看未闭环问题，真正处理去报警中心和巡检页。"));

        rightLayout.Controls.Add(focusPanel, 0, 0);
        rightLayout.Controls.Add(attentionPanel, 0, 1);

        layout.Controls.Add(devicePanel, 0, 0);
        layout.Controls.Add(rightLayout, 1, 0);
        return layout;
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

    private void DeviceGridOnCellFormatting(object? sender, DataGridViewCellFormattingEventArgs e)
    {
        if (_deviceGrid.Columns[e.ColumnIndex].DataPropertyName == nameof(DeviceRow.AttentionLevel) &&
            e.Value is string text)
        {
            var cellStyle = e.CellStyle;
            if (cellStyle is null)
            {
                return;
            }

            cellStyle.ForeColor = text switch
            {
                "异常待处理" => DangerColor,
                "预警待确认" => WarningColor,
                _ => SuccessColor
            };
        }
    }

    private void AttentionGridOnCellFormatting(object? sender, DataGridViewCellFormattingEventArgs e)
    {
        if (_attentionGrid.Columns[e.ColumnIndex].DataPropertyName == nameof(AttentionRow.StatusText) &&
            e.Value is string text)
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
    }

    private static Control BuildMetricCard(string title, Label valueLabel, Label noteLabel, Color accentColor)
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
        noteLabel.AutoSize = false;
        noteLabel.AutoEllipsis = true;
        noteLabel.Dock = DockStyle.Fill;
        noteLabel.Margin = new Padding(0, 2, 0, 0);
        noteLabel.TextAlign = ContentAlignment.TopLeft;

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
            Text = "刷新监控",
            UseVisualStyleBackColor = false
        };
        button.FlatAppearance.BorderSize = 0;
        return button;
    }

    private static Label CreateInfoLabel()
    {
        return new Label
        {
            AutoSize = true,
            Font = new Font("Microsoft YaHei UI", 8.8F),
            ForeColor = TextMutedColor,
            Margin = new Padding(0)
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

    private static Label CreateMetricNoteLabel(string text = "")
    {
        return new Label
        {
            AutoSize = true,
            Font = new Font("Microsoft YaHei UI", 8.8F),
            ForeColor = TextMutedColor,
            Text = text
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
                case Label label:
                    if (label.ForeColor == default)
                    {
                        label.ForeColor = TextSecondaryColor;
                    }
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

    private sealed class DeviceRow
    {
        public string LineName { get; init; } = string.Empty;

        public string DeviceName { get; init; } = string.Empty;

        public string LatestStatus { get; init; } = string.Empty;

        public string AttentionLevel { get; init; } = string.Empty;

        public int PendingCount { get; init; }

        public string LatestCheckedAt { get; init; } = string.Empty;

        public string Inspector { get; init; } = string.Empty;
    }

    private sealed class AttentionRow
    {
        public string DeviceName { get; init; } = string.Empty;

        public string InspectionItem { get; init; } = string.Empty;

        public string StatusText { get; init; } = string.Empty;

        public string CheckedAt { get; init; } = string.Empty;

        public string Detail { get; init; } = string.Empty;
    }
}
