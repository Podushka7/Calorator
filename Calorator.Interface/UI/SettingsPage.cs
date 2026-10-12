using Calorator.Interface.Models;
using Calorator.Interface.Services;

namespace Calorator.Interface.UI;

public sealed class SettingsPage : UserControl
{
    private readonly InterfaceController _controller;
    private readonly TextBox _displayName;
    private readonly CheckBox _hasGoal;
    private readonly NumericUpDown _calorieGoal;
    private readonly Label _saved;

    public SettingsPage(InterfaceController controller)
    {
        _controller = controller;
        Dock = DockStyle.Fill;
        BackColor = UiTheme.Background;

        var layout = new TableLayoutPanel
        {
            Dock = DockStyle.Fill, Padding = new Padding(28, 24, 28, 24),
            ColumnCount = 1, RowCount = 3, BackColor = UiTheme.Background
        };
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 90));
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 425));
        layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        Controls.Add(layout);

        var heading = new Panel { Dock = DockStyle.Fill };
        heading.Controls.Add(UiTheme.Label("Настройки", 0, 0, 700, 45, 22, true));
        heading.Controls.Add(UiTheme.Label("Параметры пользователя", 2, 46, 920, 28, 10, false, UiTheme.Muted));
        layout.Controls.Add(heading, 0, 0);

        var card = new Panel
        {
            Dock = DockStyle.Fill, BorderStyle = BorderStyle.FixedSingle,
            BackColor = Color.White, Margin = Padding.Empty
        };
        layout.Controls.Add(card, 0, 1);
        card.Controls.Add(UiTheme.Label("Профиль", 25, 25, 390, 38, 17, true));
        card.Controls.Add(UiTheme.Label("Отображаемое имя", 25, 88, 260, 29, 10, true));
        _displayName = UiTheme.Input(25, 119, 410, "Как к вам обращаться");
        card.Controls.Add(_displayName);

        card.Controls.Add(UiTheme.Label("Личная норма калорий (необязательно)", 25, 179, 600, 28, 10, true));
        _hasGoal = new CheckBox { Left = 25, Top = 215, Width = 250, Height = 32, Text = "Указать свою норму", Font = UiTheme.Regular, BackColor = Color.White };
        card.Controls.Add(_hasGoal);
        _calorieGoal = new NumericUpDown { Left = 294, Top = 215, Width = 148, Minimum = 1, Maximum = 20000, Value = 1, Font = UiTheme.Regular, Enabled = false };
        _hasGoal.CheckedChanged += (_, _) => _calorieGoal.Enabled = _hasGoal.Checked;
        card.Controls.Add(_calorieGoal);
        card.Controls.Add(UiTheme.Label("Приложение не рассчитывает и не рекомендует нормы автоматически.", 25, 270, 760, 27, 9, false, UiTheme.Muted));

        var save = UiTheme.Button("Сохранить настройки", 25, 331, 225);
        save.Click += async (_, _) => await SaveAsync();
        card.Controls.Add(save);
        _saved = UiTheme.Label("", 275, 338, 430, 30, 10, false, UiTheme.Accent);
        card.Controls.Add(_saved);
    }

    public Task RefreshAsync() => LoadSettings();

    public async Task LoadSettings()
    {
        try
        {
            var settings = await _controller.LoadSettings();
            _displayName.Text = settings.DisplayName;
            _hasGoal.Checked = settings.DailyCalorieGoal.HasValue;
            _calorieGoal.Value = settings.DailyCalorieGoal.HasValue
                ? Math.Clamp((decimal)settings.DailyCalorieGoal.Value, _calorieGoal.Minimum, _calorieGoal.Maximum)
                : 1;
            _saved.Text = "";
        }
        catch (Exception ex) { UiTheme.Error(this, ex); }
    }

    public async Task SaveCalorieGoal(double? calories)
    {
        await _controller.SaveSettings(new AppSettings
        {
            DisplayName = _displayName.Text.Trim(),
            DailyCalorieGoal = calories
        });
    }

    private async Task SaveAsync()
    {
        try
        {
            await SaveCalorieGoal(_hasGoal.Checked ? (double)_calorieGoal.Value : null);
            _saved.Text = "Настройки сохранены до закрытия приложения";
        }
        catch (Exception ex) { UiTheme.Error(this, ex); }
    }
}
