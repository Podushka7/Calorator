using Calorator.Interface.Models;
using Calorator.Interface.Services;

namespace Calorator.Interface.UI;

/// <summary>Selects a product and portion without direct database access.</summary>
public sealed class ProductPickerDialog : Form
{
    private readonly InterfaceController _controller;
    private readonly TextBox _query;
    private readonly DataGridView _grid;
    private readonly NumericUpDown _grams;
    private readonly ComboBox _meal;

    public ProductInfo? SelectedProduct { get; private set; }
    public double SelectedGrams => (double)_grams.Value;
    public MealType SelectedMealType => (MealType)_meal.SelectedIndex;

    public ProductPickerDialog(InterfaceController controller)
    {
        _controller = controller;
        Text = "Добавить продукт в дневник";
        StartPosition = FormStartPosition.CenterParent;
        Size = new Size(760, 620);
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        MinimizeBox = false;
        BackColor = UiTheme.Background;
        Font = UiTheme.Regular;
        Controls.Add(UiTheme.Label("Выберите продукт", 24, 20, 600, 40, 18, true));
        _query = UiTheme.Input(25, 82, 475, "Название или штрихкод");
        _query.Anchor = AnchorStyles.Left | AnchorStyles.Right | AnchorStyles.Top;
        Controls.Add(_query);
        var find = UiTheme.Button("Поиск", 516, 78, 190);
        find.Anchor = AnchorStyles.Right | AnchorStyles.Top;
        find.Click += async (_, _) => await FindAsync();
        _query.KeyDown += async (_, e) => { if (e.KeyCode == Keys.Enter) { e.SuppressKeyPress = true; await FindAsync(); } };
        Controls.Add(find);
        _grid = UiTheme.Grid(25, 135, 682, 295);
        _grid.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
        _grid.Columns.Add("name", "Продукт");
        _grid.Columns.Add("cal", "Ккал / 100 г");
        _grid.Columns.Add("protein", "Б, г");
        _grid.Columns.Add("fat", "Ж, г");
        _grid.Columns.Add("carbs", "У, г");
        _grid.Columns["name"]!.FillWeight = 185;
        _grid.CellDoubleClick += (_, _) => Accept();
        Controls.Add(_grid);
        Controls.Add(UiTheme.Label("Масса порции, г", 25, 452, 200, 30, 10, true));
        _grams = new NumericUpDown { Left = 205, Top = 449, Width = 105, Minimum = 1, Maximum = 3000, Value = 100, DecimalPlaces = 0, Font = UiTheme.Regular, Anchor = AnchorStyles.Left | AnchorStyles.Bottom };
        Controls.Add(_grams);
        Controls.Add(UiTheme.Label("Приём пищи", 337, 452, 135, 30, 10, true));
        _meal = new ComboBox { Left = 467, Top = 449, Width = 242, DropDownStyle = ComboBoxStyle.DropDownList, Font = UiTheme.Regular, Anchor = AnchorStyles.Bottom | AnchorStyles.Right };
        _meal.Items.AddRange(["Завтрак", "Обед", "Ужин", "Перекус"]);
        _meal.SelectedIndex = 0;
        Controls.Add(_meal);
        var accept = UiTheme.Button("Добавить", 534, 510, 175);
        accept.Anchor = AnchorStyles.Bottom | AnchorStyles.Right;
        accept.Click += (_, _) => Accept();
        Controls.Add(accept);
        var cancel = UiTheme.Button("Отмена", 347, 510, 175, primary: false);
        cancel.Anchor = AnchorStyles.Bottom | AnchorStyles.Right;
        cancel.DialogResult = DialogResult.Cancel;
        Controls.Add(cancel);
        AcceptButton = accept;
        CancelButton = cancel;
        Shown += async (_, _) => await FindAsync();
    }

    private async Task FindAsync()
    {
        try
        {
            var results = await _controller.SearchProducts(_query.Text);
            _grid.Rows.Clear();
            foreach (var product in results)
            {
                int row = _grid.Rows.Add(product.Name, product.CaloriesPer100g.ToString("F0"), product.ProteinPer100g.ToString("F1"),
                    product.FatPer100g.ToString("F1"), product.CarbohydratesPer100g.ToString("F1"));
                _grid.Rows[row].Tag = product;
            }
            _grid.ClearSelection();
        }
        catch (Exception ex) { UiTheme.Error(this, ex); }
    }

    private void Accept()
    {
        if (_grid.SelectedRows.Count == 0 || _grid.SelectedRows[0].Tag is not ProductInfo product)
        {
            MessageBox.Show(this, "Выберите продукт из списка.", "Calorator", MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }
        SelectedProduct = product;
        DialogResult = DialogResult.OK;
        Close();
    }
}
