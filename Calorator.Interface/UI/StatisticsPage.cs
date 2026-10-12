using Calorator.Interface.Models;
using Calorator.Interface.Services;

namespace Calorator.Interface.UI;

public sealed class StatisticsPage : UserControl
{
    private readonly InterfaceController _controller;
    private readonly DateTimePicker _date;
    private readonly Label[] _metrics = new Label[4];
    private readonly Label _footnote;
    private readonly Label _count;

    public StatisticsPage(InterfaceController controller)
    {
        _controller = controller;
        Dock = DockStyle.Fill;
        BackColor = UiTheme.Background;
        var layout = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            Padding = new Padding(28, 24, 28, 24),
            ColumnCount = 1,
            RowCount = 4,
            BackColor = UiTheme.Background
        };
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 86));
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 74));
        layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 100));
        Controls.Add(layout);

        var heading = new Panel { Dock = DockStyle.Fill };
        heading.Controls.Add(UiTheme.Label("Статистика питания", 0, 0, 750, 45, 22, true));
        heading.Controls.Add(UiTheme.Label("Сводка по записям дневника за выбранную дату", 2, 46, 830, 27, 10, false, UiTheme.Muted));
        layout.Controls.Add(heading, 0, 0);

        var tools = new FlowLayoutPanel { Dock = DockStyle.Fill, FlowDirection = FlowDirection.LeftToRight, WrapContents = false, AutoScroll = true, Padding = new Padding(0, 8, 0, 8) };
        var label = UiTheme.Label("Дата", 0, 0, 62, 38, 10, true);
        label.TextAlign = ContentAlignment.MiddleLeft;
        label.Margin = new Padding(0, 0, 4, 0);
        tools.Controls.Add(label);
        _date = UiTheme.DatePicker(0, 0, 215);
        _date.Margin = new Padding(0, 2, 18, 0);
        _date.ValueChanged += async (_, _) => await ShowDailySummary();
        tools.Controls.Add(_date);
        var refresh = UiTheme.Button("Обновить", 0, 0, 145);
        refresh.Click += async (_, _) => await ShowDailySummary();
        tools.Controls.Add(refresh);
        layout.Controls.Add(tools, 0, 1);

        var cards = new TableLayoutPanel
        {
            Dock = DockStyle.Fill, ColumnCount = 2, RowCount = 2,
            Margin = Padding.Empty, Padding = Padding.Empty
        };
        cards.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));
        cards.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));
        cards.RowStyles.Add(new RowStyle(SizeType.Percent, 50));
        cards.RowStyles.Add(new RowStyle(SizeType.Percent, 50));
        layout.Controls.Add(cards, 0, 2);

        string[] titles = ["Калорийность", "Белки", "Жиры", "Углеводы"];
        string[] units = ["ккал", "г", "г", "г"];
        for (int i = 0; i < 4; i++)
        {
            var card = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = Color.White,
                BorderStyle = BorderStyle.FixedSingle,
                Margin = new Padding(i % 2 == 0 ? 0 : 8, 0, i % 2 == 0 ? 8 : 0, 16)
            };
            card.Controls.Add(UiTheme.Label(titles[i], 20, 18, 365, 30, 11, true, UiTheme.Muted));
            var value = UiTheme.Label($"— {units[i]}", 20, 65, 380, 63, 26, true, UiTheme.Navy);
            value.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            card.Controls.Add(value);
            _metrics[i] = value;
            cards.Controls.Add(card, i % 2, i / 2);
        }

        var footer = new Panel { Dock = DockStyle.Fill, Margin = Padding.Empty };
        _count = UiTheme.Label("", 2, 9, 850, 32, 11, true);
        _count.Anchor = AnchorStyles.Left | AnchorStyles.Right | AnchorStyles.Top;
        footer.Controls.Add(_count);
        _footnote = UiTheme.Label("", 2, 45, 850, 50, 10, false, UiTheme.Muted);
        _footnote.Anchor = AnchorStyles.Left | AnchorStyles.Right | AnchorStyles.Top;
        _footnote.AutoEllipsis = false;
        footer.Controls.Add(_footnote);
        layout.Controls.Add(footer, 0, 3);
    }

    public Task RefreshAsync() => ShowDailySummary();

    public async Task ShowDailySummary()
    {
        try
        {
            NutritionSummary summary = await _controller.GetStatistics(_date.Value);
            _metrics[0].Text = $"{summary.TotalCalories:F1} ккал";
            _metrics[1].Text = $"{summary.TotalProtein:F1} г";
            _metrics[2].Text = $"{summary.TotalFat:F1} г";
            _metrics[3].Text = $"{summary.TotalCarbohydrates:F1} г";
            _count.Text = $"Записей о продуктах: {summary.EntriesCount}";
            await ShowGoalProgress();
        }
        catch (Exception ex) { UiTheme.Error(this, ex); }
    }

    public Task ShowNutrition() => ShowDailySummary();

    public async Task ShowGoalProgress()
    {
        var settings = await _controller.LoadSettings();
        _footnote.Text = settings.DailyCalorieGoal is double goal
            ? $"Указанная в настройках личная норма: {goal:F0} ккал. Это учётное значение, а не рекомендация по питанию."
            : "Личная норма в настройках не задана. Статистика отражает только записанные продукты.";
    }
}
