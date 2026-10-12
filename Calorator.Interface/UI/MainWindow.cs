using Calorator.Interface.Services;

namespace Calorator.Interface.UI;

/// <summary>Main Window contains four pages, in line with the UML composition relation.</summary>
public sealed class MainWindow : Form
{
    private readonly InterfaceController _controller;
    private readonly Panel _content;
    private readonly Dictionary<string, Button> _navigation = new();
    private readonly DiaryPage _diary;
    private readonly ProductSearchPage _search;
    private readonly StatisticsPage _statistics;
    private readonly SettingsPage _settings;

    public MainWindow(InterfaceController controller)
    {
        _controller = controller;
        Text = "Calorator — дневник питания";
        MinimumSize = new Size(1240, 790);
        Size = new Size(1330, 830);
        StartPosition = FormStartPosition.CenterScreen;
        AutoScaleMode = AutoScaleMode.Dpi;
        BackColor = UiTheme.Background;
        Font = UiTheme.Regular;

        var layout = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            Margin = Padding.Empty,
            Padding = Padding.Empty,
            ColumnCount = 2,
            RowCount = 1,
            GrowStyle = TableLayoutPanelGrowStyle.FixedSize,
            BackColor = UiTheme.Background
        };
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 235f));
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100f));
        layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100f));
        Controls.Add(layout);

        var sidebar = new Panel { Dock = DockStyle.Fill, Margin = Padding.Empty, BackColor = UiTheme.Navy };
        layout.Controls.Add(sidebar, 0, 0);
        sidebar.Controls.Add(UiTheme.Label("Calorator", 24, 27, 185, 40, 23, true, Color.White));
        sidebar.Controls.Add(UiTheme.Label("учёт питания", 25, 67, 185, 30, 9, true, Color.FromArgb(160, 182, 200)));

        AddNav(sidebar, "Дневник питания", 147, "diary", OpenDiary);
        AddNav(sidebar, "Поиск продуктов", 203, "search", OpenSearch);
        AddNav(sidebar, "Статистика", 259, "statistics", OpenStatistics);
        AddNav(sidebar, "Настройки", 315, "settings", OpenSettings);

        _content = new Panel
        {
            Dock = DockStyle.Fill,
            Margin = Padding.Empty,
            Padding = Padding.Empty,
            BackColor = UiTheme.Background
        };
        layout.Controls.Add(_content, 1, 0);

        _diary = new DiaryPage(_controller);
        _search = new ProductSearchPage(_controller, () => { OpenDiary(); return Task.CompletedTask; });
        _statistics = new StatisticsPage(_controller);
        _settings = new SettingsPage(_controller);
        Shown += (_, _) => OpenDiary();
    }

    private void AddNav(Panel parent, string caption, int y, string key, Action go)
    {
        var button = new Button
        {
            Left = 14, Top = y, Width = 205, Height = 45,
            Text = "  " + caption,
            TextAlign = ContentAlignment.MiddleLeft,
            Font = new Font("Segoe UI Semibold", 10),
            FlatStyle = FlatStyle.Flat,
            BackColor = UiTheme.Navy,
            ForeColor = Color.FromArgb(211, 221, 232),
            Cursor = Cursors.Hand
        };
        button.FlatAppearance.BorderSize = 0;
        button.Click += (_, _) => go();
        parent.Controls.Add(button);
        _navigation[key] = button;
    }

    private void ShowPage(UserControl page, string key, Func<Task> reload)
    {
        _content.SuspendLayout();
        foreach (Control child in _content.Controls) child.Visible = false;
        page.Dock = DockStyle.Fill;
        page.Margin = Padding.Empty;
        if (!_content.Controls.Contains(page)) _content.Controls.Add(page);
        page.Visible = true;
        page.BringToFront();
        _content.ResumeLayout();
        foreach (var item in _navigation)
        {
            item.Value.BackColor = item.Key == key ? UiTheme.Accent : UiTheme.Navy;
            item.Value.ForeColor = item.Key == key ? Color.White : Color.FromArgb(211, 221, 232);
        }
        _ = SafeReloadAsync(reload);
    }

    private async Task SafeReloadAsync(Func<Task> reload)
    {
        try { await reload(); }
        catch (Exception ex) { UiTheme.Error(this, ex); }
    }

    public void OpenDiary() => ShowPage(_diary, "diary", _diary.RefreshAsync);
    public void OpenSearch() => ShowPage(_search, "search", _search.RefreshAsync);
    public void OpenStatistics() => ShowPage(_statistics, "statistics", _statistics.RefreshAsync);
    public void OpenSettings() => ShowPage(_settings, "settings", _settings.RefreshAsync);
}
