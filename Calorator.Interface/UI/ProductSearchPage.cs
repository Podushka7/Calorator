using Calorator.Interface.Models;
using Calorator.Interface.Services;

namespace Calorator.Interface.UI;

public sealed class ProductSearchPage : UserControl
{
    private readonly InterfaceController _controller;
    private readonly Func<Task> _goDiary;
    private readonly TextBox _query;
    private readonly TextBox _barcode;
    private readonly DataGridView _grid;
    private readonly Label _help;

    public ProductSearchPage(InterfaceController controller, Func<Task> goDiary)
    {
        _controller = controller;
        _goDiary = goDiary;
        Dock = DockStyle.Fill;
        BackColor = UiTheme.Background;

        var page = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            Padding = new Padding(28, 24, 28, 24),
            Margin = Padding.Empty,
            ColumnCount = 1,
            RowCount = 4,
            BackColor = UiTheme.Background
        };
        page.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        page.RowStyles.Add(new RowStyle(SizeType.Absolute, 90));
        page.RowStyles.Add(new RowStyle(SizeType.Absolute, 118));
        page.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        page.RowStyles.Add(new RowStyle(SizeType.Absolute, 68));
        Controls.Add(page);

        var heading = new Panel { Dock = DockStyle.Fill };
        heading.Controls.Add(UiTheme.Label("Поиск продуктов", 0, 0, 650, 45, 22, true));
        heading.Controls.Add(UiTheme.Label("Найдите продукт по названию или штрихкоду. Распознавание фото подключается отдельно.", 2, 46, 940, 27, 10, false, UiTheme.Muted));
        page.Controls.Add(heading, 0, 0);

        var filters = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 2, RowCount = 1, Margin = Padding.Empty };
        filters.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));
        filters.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));
        filters.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        page.Controls.Add(filters, 0, 1);

        var nameGroup = new Panel { Dock = DockStyle.Fill, Margin = new Padding(0, 0, 12, 0) };
        nameGroup.Controls.Add(UiTheme.Label("Название продукта", 0, 5, 230, 26, 10, true));
        var nameRow = new TableLayoutPanel { Dock = DockStyle.Bottom, Height = 62, ColumnCount = 2, RowCount = 1, Padding = new Padding(0, 4, 0, 12) };
        nameRow.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        nameRow.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 130));
        nameGroup.Controls.Add(nameRow);
        _query = UiTheme.Input(0, 0, 300, "Например, яблоко");
        _query.Dock = DockStyle.Fill;
        _query.Margin = new Padding(0, 3, 10, 0);
        nameRow.Controls.Add(_query, 0, 0);
        var find = UiTheme.Button("Найти", 0, 0, 125);
        find.Dock = DockStyle.Fill;
        find.Margin = new Padding(0, 0, 0, 0);
        find.Click += async (_, _) => await SearchAsync();
        nameRow.Controls.Add(find, 1, 0);
        _query.KeyDown += async (_, e) => { if (e.KeyCode == Keys.Enter) { e.SuppressKeyPress = true; await SearchAsync(); } };
        filters.Controls.Add(nameGroup, 0, 0);

        var barcodeGroup = new Panel { Dock = DockStyle.Fill, Margin = new Padding(12, 0, 0, 0) };
        barcodeGroup.Controls.Add(UiTheme.Label("Штрихкод", 0, 5, 200, 26, 10, true));
        var barcodeRow = new TableLayoutPanel { Dock = DockStyle.Bottom, Height = 62, ColumnCount = 2, RowCount = 1, Padding = new Padding(0, 4, 0, 12) };
        barcodeRow.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        barcodeRow.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 152));
        barcodeGroup.Controls.Add(barcodeRow);
        _barcode = UiTheme.Input(0, 0, 265, "Цифры на упаковке");
        _barcode.Dock = DockStyle.Fill;
        _barcode.Margin = new Padding(0, 3, 10, 0);
        barcodeRow.Controls.Add(_barcode, 0, 0);
        var barcodeButton = UiTheme.Button("Распознать", 0, 0, 148, primary: false);
        barcodeButton.Dock = DockStyle.Fill;
        barcodeButton.Margin = Padding.Empty;
        barcodeButton.Click += async (_, _) => await SearchByBarcode(_barcode.Text);
        barcodeRow.Controls.Add(barcodeButton, 1, 0);
        filters.Controls.Add(barcodeGroup, 1, 0);

        var card = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            BackColor = Color.White,
            BorderStyle = BorderStyle.FixedSingle,
            Padding = new Padding(14),
            Margin = Padding.Empty,
            ColumnCount = 1,
            RowCount = 2
        };
        card.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        card.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        card.RowStyles.Add(new RowStyle(SizeType.Absolute, 44));
        page.Controls.Add(card, 0, 2);
        _grid = UiTheme.Grid(0, 0, 0, 0);
        _grid.Dock = DockStyle.Fill;
        _grid.Columns.Add("name", "Продукт");
        _grid.Columns.Add("barcode", "Штрихкод");
        _grid.Columns.Add("calories", "Ккал / 100 г");
        _grid.Columns.Add("protein", "Белки, г");
        _grid.Columns.Add("fat", "Жиры, г");
        _grid.Columns.Add("carbs", "Углеводы, г");
        _grid.Columns["name"]!.FillWeight = 180;
        _grid.Columns["name"]!.MinimumWidth = 180;
        _grid.Columns["barcode"]!.MinimumWidth = 125;
        foreach (var key in new[] { "calories", "protein", "fat", "carbs" })
            _grid.Columns[key]!.MinimumWidth = 90;
        _grid.CellDoubleClick += async (_, _) => await AddSelectedAsync();
        _grid.SelectionChanged += (_, _) =>
        {
            if (_grid.SelectedRows.Count > 0 && _grid.SelectedRows[0].Tag is ProductInfo product)
                ShowNutrition(product);
        };
        card.Controls.Add(_grid, 0, 0);
        _help = UiTheme.Label("Выберите продукт в таблице и нажмите «Добавить в дневник».", 0, 0, 850, 27, 10, false, UiTheme.Muted);
        _help.Dock = DockStyle.Fill;
        _help.Margin = new Padding(2, 10, 2, 0);
        _help.TextAlign = ContentAlignment.MiddleLeft;
        card.Controls.Add(_help, 0, 1);

        var actions = new FlowLayoutPanel { Dock = DockStyle.Fill, Padding = new Padding(0, 14, 0, 0), WrapContents = false, AutoScroll = true };
        var add = UiTheme.Button("+ Добавить в дневник", 0, 0, 250);
        add.Margin = new Padding(0, 0, 16, 0);
        add.Click += async (_, _) => await AddSelectedAsync();
        actions.Controls.Add(add);
        var photo = UiTheme.Button("Выбрать фото (ИИ)", 0, 0, 225, primary: false);
        photo.Margin = Padding.Empty;
        photo.Click += async (_, _) => await SelectPhoto();
        actions.Controls.Add(photo);
        page.Controls.Add(actions, 0, 3);
    }

    public Task RefreshAsync() => SearchAsync();

    private async Task SearchAsync()
    {
        try
        {
            var products = await _controller.SearchProducts(_query.Text);
            ShowProducts(products);
            _help.Text = products.Count == 0 ? "Ничего не найдено." : $"Найдено продуктов: {products.Count}. Выберите строку для добавления в дневник.";
        }
        catch (Exception ex) { UiTheme.Error(this, ex); }
    }

    public async Task SearchByBarcode(string code)
    {
        if (string.IsNullOrWhiteSpace(code))
        {
            MessageBox.Show(FindForm(), "Введите штрихкод продукта.", "Calorator", MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }
        try
        {
            var product = await _controller.FindByBarcode(code);
            ShowProducts(product is null ? [] : [product]);
            _help.Text = product is null ? "Штрихкод не найден в каталоге." : "Продукт найден по штрихкоду.";
        }
        catch (Exception ex) { UiTheme.Error(this, ex); }
    }

    public async Task SelectPhoto()
    {
        using var dialog = new OpenFileDialog
        {
            Title = "Выберите фотографию продукта",
            Filter = "Изображения|*.png;*.jpg;*.jpeg;*.bmp|Все файлы|*.*"
        };
        if (dialog.ShowDialog(FindForm()) != DialogResult.OK) return;
        try
        {
            var product = await _controller.RecognizeByPhoto(dialog.FileName);
            if (product is not null)
            {
                ShowProducts([product]);
                _help.Text = "Результат распознавания получен.";
            }
            else
            {
                MessageBox.Show(FindForm(), "Продукт на фотографии не распознан.", "Calorator", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
        }
        catch (NotSupportedException ex)
        {
            MessageBox.Show(FindForm(), ex.Message, "ИИ-модуль пока не подключён", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }
        catch (Exception ex) { UiTheme.Error(this, ex); }
    }

    public void ShowNutrition(ProductInfo product)
    {
        _help.Text = $"{product.Name}: {product.CaloriesPer100g:F0} ккал · Б {product.ProteinPer100g:F1} г · Ж {product.FatPer100g:F1} г · У {product.CarbohydratesPer100g:F1} г на 100 г";
    }

    private void ShowProducts(IEnumerable<ProductInfo> products)
    {
        _grid.Rows.Clear();
        foreach (var product in products)
        {
            var row = _grid.Rows.Add(product.Name, product.Barcode, product.CaloriesPer100g.ToString("F0"),
                product.ProteinPer100g.ToString("F1"), product.FatPer100g.ToString("F1"), product.CarbohydratesPer100g.ToString("F1"));
            _grid.Rows[row].Tag = product;
        }
        _grid.ClearSelection();
    }

    private async Task AddSelectedAsync()
    {
        if (_grid.SelectedRows.Count == 0 || _grid.SelectedRows[0].Tag is not ProductInfo product)
        {
            MessageBox.Show(FindForm(), "Сначала выберите продукт в таблице.", "Calorator", MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }
        using var dialog = new PortionDialog(product);
        if (dialog.ShowDialog(FindForm()) != DialogResult.OK) return;
        try
        {
            await _controller.AddToDiary(product, dialog.Grams, dialog.MealType, dialog.Date);
            MessageBox.Show(FindForm(), "Продукт добавлен в дневник.", "Calorator", MessageBoxButtons.OK, MessageBoxIcon.Information);
            await _goDiary();
        }
        catch (Exception ex) { UiTheme.Error(this, ex); }
    }
}
