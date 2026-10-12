using Calorator.Interface.Models;

namespace Calorator.Interface.UI;

public sealed class PortionDialog : Form
{
    private readonly NumericUpDown _grams;
    private readonly ComboBox _meal;
    private readonly DateTimePicker _date;

    public double Grams => (double)_grams.Value;
    public MealType MealType => (MealType)_meal.SelectedIndex;
    public DateTime Date => _date.Value.Date;

    public PortionDialog(ProductInfo product)
    {
        Text = "Добавление в дневник";
        StartPosition = FormStartPosition.CenterParent;
        Size = new Size(470, 340);
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        MinimizeBox = false;
        BackColor = UiTheme.Background;
        Controls.Add(UiTheme.Label(product.Name, 25, 19, 405, 38, 17, true));
        Controls.Add(UiTheme.Label("Укажите данные записи", 26, 57, 380, 27, 10, false, UiTheme.Muted));
        Controls.Add(UiTheme.Label("Масса, г", 25, 104, 145, 27, 10, true));
        _grams = new NumericUpDown { Left = 184, Top = 101, Width = 240, Minimum = 1, Maximum = 3000, Value = 100, Font = UiTheme.Regular };
        Controls.Add(_grams);
        Controls.Add(UiTheme.Label("Приём пищи", 25, 148, 150, 27, 10, true));
        _meal = new ComboBox { Left = 184, Top = 145, Width = 240, DropDownStyle = ComboBoxStyle.DropDownList, Font = UiTheme.Regular };
        _meal.Items.AddRange(["Завтрак", "Обед", "Ужин", "Перекус"]);
        _meal.SelectedIndex = 0;
        Controls.Add(_meal);
        Controls.Add(UiTheme.Label("Дата", 25, 192, 150, 27, 10, true));
        _date = UiTheme.DatePicker(184, 190, 240);
        Controls.Add(_date);
        var ok = UiTheme.Button("Сохранить", 248, 244, 176);
        ok.DialogResult = DialogResult.OK;
        Controls.Add(ok);
        var cancel = UiTheme.Button("Отмена", 25, 244, 176, primary: false);
        cancel.DialogResult = DialogResult.Cancel;
        Controls.Add(cancel);
        AcceptButton = ok;
        CancelButton = cancel;
    }
}
