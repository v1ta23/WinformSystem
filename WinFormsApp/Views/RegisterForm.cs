using WinFormsApp.Controllers;

namespace WinFormsApp.Views;

internal sealed class RegisterForm : Form
{
    private readonly RegisterController _controller;
    private readonly TextBox _accountTextBox;
    private readonly TextBox _passwordTextBox;
    private readonly TextBox _confirmTextBox;
    private readonly CheckBox _showPasswordCheckBox;

    public RegisterForm(RegisterController controller)
    {
        _controller = controller;

        Text = "\u6ce8\u518c";
        StartPosition = FormStartPosition.CenterParent;
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        MinimizeBox = false;
        ClientSize = new Size(420, 340);
        Font = new Font("Microsoft YaHei UI", 9F);
        BackColor = Color.White;

        var titleLabel = new Label
        {
            Text = "\u521b\u5efa\u8d26\u53f7",
            Font = new Font("Microsoft YaHei UI", 16F, FontStyle.Bold),
            Dock = DockStyle.Top,
            Height = 52,
            TextAlign = ContentAlignment.MiddleCenter
        };

        var panel = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            Padding = new Padding(32, 12, 32, 24),
            ColumnCount = 1,
            RowCount = 9
        };
        for (var i = 0; i < 8; i++)
        {
            panel.RowStyles.Add(new RowStyle(SizeType.Absolute, i % 2 == 0 ? 36 : 42));
        }
        panel.RowStyles.Add(new RowStyle(SizeType.Percent, 100));

        _accountTextBox = new TextBox { Dock = DockStyle.Fill, PlaceholderText = "4 \u5230 9 \u4f4d\u8d26\u53f7" };
        _passwordTextBox = new TextBox { Dock = DockStyle.Fill, UseSystemPasswordChar = true, PlaceholderText = "6 \u5230 9 \u4f4d\u5bc6\u7801" };
        _confirmTextBox = new TextBox { Dock = DockStyle.Fill, UseSystemPasswordChar = true, PlaceholderText = "\u518d\u6b21\u8f93\u5165\u5bc6\u7801" };
        _showPasswordCheckBox = new CheckBox { Text = "\u663e\u793a\u5bc6\u7801", AutoSize = true };
        _showPasswordCheckBox.CheckedChanged += (_, _) =>
        {
            var usePasswordChar = !_showPasswordCheckBox.Checked;
            _passwordTextBox.UseSystemPasswordChar = usePasswordChar;
            _confirmTextBox.UseSystemPasswordChar = usePasswordChar;
        };

        var buttonPanel = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            FlowDirection = FlowDirection.RightToLeft
        };

        var submitButton = new Button
        {
            Text = "\u6ce8\u518c",
            AutoSize = true
        };
        submitButton.Click += OnSubmitClicked;

        var cancelButton = new Button { Text = "\u53d6\u6d88", AutoSize = true };
        cancelButton.Click += (_, _) => Close();

        buttonPanel.Controls.Add(submitButton);
        buttonPanel.Controls.Add(cancelButton);

        panel.Controls.Add(CreateFieldLabel("\u8d26\u53f7"), 0, 0);
        panel.Controls.Add(_accountTextBox, 0, 1);
        panel.Controls.Add(CreateFieldLabel("\u5bc6\u7801"), 0, 2);
        panel.Controls.Add(_passwordTextBox, 0, 3);
        panel.Controls.Add(CreateFieldLabel("\u786e\u8ba4\u5bc6\u7801"), 0, 4);
        panel.Controls.Add(_confirmTextBox, 0, 5);
        panel.Controls.Add(_showPasswordCheckBox, 0, 6);
        panel.Controls.Add(buttonPanel, 0, 7);

        Controls.Add(panel);
        Controls.Add(titleLabel);
    }

    private static Label CreateFieldLabel(string text)
    {
        return new Label
        {
            Text = text,
            Dock = DockStyle.Fill,
            TextAlign = ContentAlignment.BottomLeft
        };
    }

    private void OnSubmitClicked(object? sender, EventArgs e)
    {
        try
        {
            var result = _controller.Register(_accountTextBox.Text, _passwordTextBox.Text, _confirmTextBox.Text);
            if (!result.Success)
            {
                MessageBox.Show(this, result.Message, "\u6ce8\u518c\u5931\u8d25", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            MessageBox.Show(this, result.Message, "\u6ce8\u518c\u6210\u529f", MessageBoxButtons.OK, MessageBoxIcon.Information);
            DialogResult = DialogResult.OK;
            Close();
        }
        catch (Exception ex)
        {
            MessageBox.Show(this, $"\u6ce8\u518c\u8fc7\u7a0b\u4e2d\u53d1\u751f\u9519\u8bef\uff1a{ex.Message}", "\u9519\u8bef", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }
}
