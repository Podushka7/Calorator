using Calorator.Interface.Models;
using Calorator.Interface.Services;

namespace Calorator.Interface.UI;

public sealed class DiaryPage : UserControl
{
    private readonly InterfaceController _controller;
    private readonly DateTimePicker _date;
    private readonly DataGridView _grid;
    private readonly Label _total;
    private readonly Label _empty;

    public DiaryPage(InterfaceController controller)
    {
        _controller = controller;
        BackColor = UiTheme.Background;
        Dock = DockStyle.Fill;

        var page = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            Padding = new Padding(28, 24, 28, 24),
            Margin = Padding.Empty,
            ColumnCount = 1,
            RowCount = 3,
            BackColor = UiTheme.Background
        };
        page.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        page.RowStyles.Add(new RowStyle(SizeType.Absolute, 86));
        page.RowStyles.Add(new RowStyle(SizeType.Absolute, 78));
        page.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        Controls.Add(page);

        var heading = new Panel { Dock = DockStyle.Fill };
        heading.Controls.Add(UiTheme.Label("Дневник питания", 0, 0, 720, 44, 22, true));
        heading.Controls.Add(UiTheme.Label("Записи о продуктах и приёмах пищи за выбранный день", 2, 46, 890, 28, 10, false, UiTheme.Muted));
        page.Controls.Add(heading, 0, 0);

        var tools = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            AutoScroll = true,
            WrapContents = false,
            FlowDirection = FlowDirection.LeftToRight,
            Padding = new Padding(0, 8, 0, 8),
            Margin = Padding.Empty
        };
        page.Controls.Add(tools, 0, 1);
        var dateLabel = UiTheme.Label("Дата", 0, 0, 62, 40, 10, true);
        dateLabel.TextAlign = ContentAlignment.MiddleLeft;
        dateLabel.Margin = new Padding(0, 0, 4, 0);
        tools.Controls.Add(dateLabel);

        _date = UiTheme.DatePicker(0, 0, 215);
        _date.Margin = new Padding(0, 2, 18, 0);
        _date.ValueChanged += async (_, _) => await LoadEntries(_date.Value);
        tools.Controls.Add(_date);

        var add = UiTheme.Button("+ Добавить продукт", 0, 0, 205);
        add.Margin = new Padding(0, 0, 12, 0);
        add.Click += async (_, _) => await AddProduct();
        tools.Controls.Add(add);

        var remove = UiTheme.Button("Удалить запись", 0, 0, 175, primary: false);
        remove.Margin = Padding.Empty;
        remove.Click += async (_, _) => await RemoveEntry();
        tools.Controls.Add(remove);

        var card = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            BackColor = Color.White,
            BorderStyle = BorderStyle.FixedSingle,
            Padding = new Padding(16),
            Margin = Padding.Empty,
            ColumnCount = 1,
            RowCount = 2
        };
        card.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        card.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        card.RowStyles.Add(new RowStyle(SizeType.Absolute, 46));
        page.Controls.Add(card, 0, 2);

        var gridHost = new Panel { Dock = DockStyle.Fill, Margin = Padding.Empty, BackColor = Color.White };
        card.Controls.Add(gridHost, 0, 0);
        _grid = UiTheme.Grid(0, 0, 0, 0);
        _grid.Dock = DockStyle.Fill;
        _grid.Columns.Add("meal", "Приём пищи");
        _grid.Columns.Add("name", "Продукт");
        _grid.Columns.Add("grams", "Порция, г");
        _grid.Columns.Add("calories", "Ккал");
        _grid.Columns.Add("protein", "Белки, г");
        _grid.Columns.Add("fat", "Жиры, г");
        _grid.Columns.Add("carbs", "Углеводы, г");
        _grid.Columns["name"]!.FillWeight = 160;
        _grid.Columns["name"]!.MinimumWidth = 150;
        _grid.Columns["meal"]!.MinimumWidth = 105;
        foreach (var key in new[] { "grams", "calories", "protein", "fat", "carbs" })
            _grid.Columns[key]!.MinimumWidth = 85;
        gridHost.Controls.Add(_grid);

        _empty = UiTheme.Label("Записей пока нет — добавьте первый продукт.", 0, 0, 600, 40, 11, false, UiTheme.Muted);
        _empty.BackColor = Color.White;
        _empty.TextAlign = ContentAlignment.MiddleCenter;
        _empty.Visible = false;
        gridHost.Controls.Add(_empty);
        gridHost.Resize += (_, _) =>
        {
            _empty.Width = Math.Max(120, Math.Min(600, gridHost.ClientSize.Width - 20));
            _empty.Left = (gridHost.ClientSize.Width - _empty.Width) / 2;
            _empty.Top = Math.Max(50, (gridHost.ClientSize.Height - _empty.Height) / 2);
            if (_empty.Visible) _empty.BringToFront();
        };

        _total = UiTheme.Label("", 0, 0, 700, 35, 10, true);
        _total.Dock = DockStyle.Fill;
        _total.Margin = new Padding(2, 12, 2, 0);
        _total.TextAlign = ContentAlignment.MiddleLeft;
        card.Controls.Add(_total, 0, 1);
    }

    public Task RefreshAsync() => LoadEntries(_date.Value);

    public async Task LoadEntries(DateTime date)
    {
        try
        {
            var entries = await _controller.GetDiaryEntries(date);
            _grid.Rows.Clear();
            foreach (var entry in entries)
            {
                int row = _grid.Rows.Add(entry.MealType.ToRussian(), entry.Product.Name, entry.Grams.ToString("F0"),
                    entry.Calories.ToString("F1"), entry.Protein.ToString("F1"), entry.Fat.ToString("F1"), entry.Carbohydrates.ToString("F1"));
                _grid.Rows[row].Tag = entry;
            }
            _empty.Visible = entries.Count == 0;
            if (_empty.Visible) _empty.BringToFront();
            _total.Text = $"Итого за день: {entries.Sum(x => x.Calories):F1} ккал  ·  Б: {entries.Sum(x => x.Protein):F1} г  ·  Ж: {entries.Sum(x => x.Fat):F1} г  ·  У: {entries.Sum(x => x.Carbohydrates):F1} г";
            _grid.ClearSelection();
        }
        catch (Exception ex) { UiTheme.Error(this, ex); }
    }

    public async Task AddProduct()
    {
        using var dialog = new ProductPickerDialog(_controller);
        if (dialog.ShowDialog(FindForm()) != DialogResult.OK || dialog.SelectedProduct is null) return;
        try
        {
            await _controller.AddToDiary(dialog.SelectedProduct, dialog.SelectedGrams, dialog.SelectedMealType, _date.Value);
            await LoadEntries(_date.Value);
        }
        catch (Exception ex) { UiTheme.Error(this, ex); }
    }

    public async Task RemoveEntry()
    {
        if (_grid.SelectedRows.Count == 0 || _grid.SelectedRows[0].Tag is not DiaryEntry item)
        {
            MessageBox.Show(FindForm(), "Сначала выберите запись в таблице.", "Calorator", MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }
        if (MessageBox.Show(FindForm(), $"Удалить запись «{item.Product.Name}»?", "Подтверждение", MessageBoxButtons.YesNo, MessageBoxIcon.Question) != DialogResult.Yes) return;
        try
        {
            await _controller.RemoveEntry(item.Id);
            await LoadEntries(_date.Value);
        }
        catch (Exception ex) { UiTheme.Error(this, ex); }
    }
}
