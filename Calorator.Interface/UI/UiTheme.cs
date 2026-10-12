using System.Drawing;

namespace Calorator.Interface.UI;

internal static class UiTheme
{
    public static readonly Color Navy = Color.FromArgb(21, 35, 55);
    public static readonly Color Accent = Color.FromArgb(0, 117, 122);
    public static readonly Color AccentHover = Color.FromArgb(0, 96, 100);
    public static readonly Color Background = Color.FromArgb(246, 248, 251);
    public static readonly Color Muted = Color.FromArgb(106, 119, 135);
    public static readonly Color Border = Color.FromArgb(222, 228, 236);
    public static readonly Font Regular = new("Segoe UI", 10);
    public static readonly Font Heading = new("Segoe UI Semibold", 21);

    public static Label Label(string text, int x, int y, int width, int height, int fontSize = 10, bool bold = false, Color? color = null) =>
        new()
        {
            Text = text,
            Left = x, Top = y, Width = width, Height = height,
            Font = new Font(bold ? "Segoe UI Semibold" : "Segoe UI", fontSize),
            ForeColor = color ?? Navy,
            AutoEllipsis = true
        };

    public static Button Button(string text, int x, int y, int width = 160, int height = 40, bool primary = true)
    {
        var button = new Button
        {
            Text = text,
            Left = x, Top = y, Width = width, Height = height,
            Font = new Font("Segoe UI Semibold", 10),
            FlatStyle = FlatStyle.Flat,
            BackColor = primary ? Accent : Color.White,
            ForeColor = primary ? Color.White : Navy,
            Cursor = Cursors.Hand,
            UseVisualStyleBackColor = false
        };
        button.FlatAppearance.BorderSize = primary ? 0 : 1;
        button.FlatAppearance.BorderColor = Border;
        return button;
    }

    public static TextBox Input(int x, int y, int width, string placeholder = "") => new()
    {
        Left = x, Top = y, Width = width, Height = 31,
        Font = Regular,
        BorderStyle = BorderStyle.FixedSingle,
        PlaceholderText = placeholder
    };

    public static DateTimePicker DatePicker(int x, int y, int width = 190) => new()
    {
        Left = x, Top = y, Width = Math.Max(width, 215), Height = 34,
        Font = Regular,
        Format = DateTimePickerFormat.Custom,
        CustomFormat = "dd.MM.yyyy",
        RightToLeft = RightToLeft.No,
        Value = DateTime.Today
    };

    public static Panel Card(int x, int y, int width, int height) => new()
    {
        Left = x, Top = y, Width = width, Height = height,
        BackColor = Color.White,
        BorderStyle = BorderStyle.FixedSingle
    };

    public static DataGridView Grid(int x, int y, int width, int height)
    {
        var grid = new DataGridView
        {
            Left = x, Top = y, Width = width, Height = height,
            AllowUserToAddRows = false,
            AllowUserToDeleteRows = false,
            AllowUserToResizeRows = false,
            RowHeadersVisible = false,
            ReadOnly = true,
            MultiSelect = false,
            SelectionMode = DataGridViewSelectionMode.FullRowSelect,
            AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
            ScrollBars = ScrollBars.Both,
            BackgroundColor = Color.White,
            BorderStyle = BorderStyle.None,
            CellBorderStyle = DataGridViewCellBorderStyle.SingleHorizontal,
            ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.DisableResizing,
            ColumnHeadersHeight = 42,
            RowTemplate = { Height = 38 },
            GridColor = Border,
            EnableHeadersVisualStyles = false,
            Font = Regular
        };
        grid.ColumnHeadersDefaultCellStyle.BackColor = Color.FromArgb(238, 243, 247);
        grid.ColumnHeadersDefaultCellStyle.ForeColor = Navy;
        grid.ColumnHeadersDefaultCellStyle.Font = new Font("Segoe UI Semibold", 10);
        grid.DefaultCellStyle.SelectionBackColor = Color.FromArgb(221, 243, 242);
        grid.DefaultCellStyle.SelectionForeColor = Navy;
        grid.DefaultCellStyle.BackColor = Color.White;
        grid.DefaultCellStyle.ForeColor = Navy;
        return grid;
    }

    public static void Error(IWin32Window owner, Exception ex)
    {
        MessageBox.Show(owner, ex.Message, "Calorator — ошибка", MessageBoxButtons.OK, MessageBoxIcon.Warning);
    }
}
