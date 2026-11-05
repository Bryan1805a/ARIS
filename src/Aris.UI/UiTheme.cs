using System.Drawing;
using System.Windows.Forms;

namespace Aris.UI;

/// <summary>
/// Small, dependency-free theme applied by the shell and screen controls. It keeps
/// WinForms while giving the application a consistent modern palette, typography,
/// flat buttons and readable data grids.
/// </summary>
internal static class UiTheme {
    public static readonly Color Sidebar = Color.FromArgb(15, 23, 42);
    public static readonly Color SidebarHover = Color.FromArgb(30, 41, 59);
    public static readonly Color SidebarActive = Color.FromArgb(37, 99, 235);
    public static readonly Color Accent = Color.FromArgb(37, 99, 235);
    public static readonly Color AccentHover = Color.FromArgb(29, 78, 216);
    public static readonly Color AccentSoft = Color.FromArgb(219, 234, 254);
    public static readonly Color Background = Color.FromArgb(241, 245, 249);
    public static readonly Color Card = Color.White;
    public static readonly Color Text = Color.FromArgb(15, 23, 42);
    public static readonly Color Muted = Color.FromArgb(100, 116, 139);
    public static readonly Color Border = Color.FromArgb(203, 213, 225);
    public static readonly Color GridLine = Color.FromArgb(226, 232, 240);
    public static readonly Color Danger = Color.FromArgb(220, 38, 38);
    public static readonly Color DangerHover = Color.FromArgb(185, 28, 28);

    public static readonly Font BaseFont = new("Segoe UI", 9.5F);
    public static readonly Font SemiboldFont = new("Segoe UI Semibold", 9.5F);
    public static readonly Font TitleFont = new("Segoe UI Semibold", 20F);
    public static readonly Font SectionFont = new("Segoe UI Semibold", 12F);
    public static readonly Font SubtitleFont = new("Segoe UI", 10F);
    public static readonly Font SmallFont = new("Segoe UI", 8.75F);

    public static void ApplyShell(Form form) {
        form.Font = BaseFont;
        form.BackColor = Background;
        form.ForeColor = Text;
    }

    public static void StyleSidebarButton(Button button) {
        button.FlatStyle = FlatStyle.Flat;
        button.FlatAppearance.BorderSize = 0;
        button.BackColor = Sidebar;
        button.ForeColor = Color.FromArgb(226, 232, 240);
        button.Font = SemiboldFont;
        button.TextAlign = ContentAlignment.MiddleLeft;
        button.Padding = new Padding(18, 0, 8, 0);
        button.Cursor = Cursors.Hand;
        button.UseVisualStyleBackColor = false;

        button.MouseEnter += (_, _) => {
            if (button.Enabled && !IsActiveNav(button)) {
                button.BackColor = SidebarHover;
                button.ForeColor = Color.White;
            }
        };

        button.MouseLeave += (_, _) => {
            if (!IsActiveNav(button)) {
                button.BackColor = Sidebar;
                button.ForeColor = Color.FromArgb(226, 232, 240);
            }
        };
    }

    public static void SetActiveNav(Button active, IEnumerable<Button> navigationButtons) {
        foreach (var button in navigationButtons) {
            button.Tag = null;
            button.BackColor = Sidebar;
            button.ForeColor = Color.FromArgb(226, 232, 240);
        }

        active.Tag = "active";
        active.BackColor = SidebarActive;
        active.ForeColor = Color.White;
    }

    public static void StyleStatusStrip(StatusStrip strip) {
        strip.BackColor = Sidebar;
        strip.ForeColor = Color.White;
        strip.Font = SemiboldFont;
        strip.SizingGrip = false;
    }

    public static void StyleMenuStrip(MenuStrip menu) {
        menu.BackColor = Card;
        menu.ForeColor = Text;
        menu.Font = SemiboldFont;
        menu.Padding = new Padding(8, 2, 0, 2);
        menu.ShowItemToolTips = true;
        menu.Renderer = new ToolStripProfessionalRenderer(new ArisColorTable()) {
            RoundedEdges = false
        };
    }

    public static void StyleToolStrip(ToolStrip strip) {
        strip.BackColor = Color.FromArgb(248, 250, 252);
        strip.ForeColor = Text;
        strip.Font = BaseFont;
        strip.GripStyle = ToolStripGripStyle.Hidden;
        strip.Padding = new Padding(10, 5, 10, 5);
        strip.Renderer = new ToolStripProfessionalRenderer(new ArisColorTable()) {
            RoundedEdges = false
        };
    }

    public static ToolStripButton StyleToolStripButton(ToolStripButton button) {
        button.DisplayStyle = ToolStripItemDisplayStyle.Text;
        button.Font = SemiboldFont;
        button.ForeColor = Text;
        button.AutoSize = false;
        button.Height = 30;
        button.Width = TextRenderer.MeasureText(button.Text, SemiboldFont).Width + 26;
        return button;
    }

    /// <summary>Flat palette for the menu bar and the quick-access toolbar.</summary>
    private sealed class ArisColorTable : ProfessionalColorTable {
        public override Color MenuItemSelected => AccentSoft;
        public override Color MenuItemSelectedGradientBegin => AccentSoft;
        public override Color MenuItemSelectedGradientEnd => AccentSoft;
        public override Color MenuItemPressedGradientBegin => AccentSoft;
        public override Color MenuItemPressedGradientEnd => AccentSoft;
        public override Color MenuItemBorder => AccentSoft;
        public override Color MenuBorder => Border;
        public override Color ToolStripDropDownBackground => Card;
        public override Color ImageMarginGradientBegin => Card;
        public override Color ImageMarginGradientMiddle => Card;
        public override Color ImageMarginGradientEnd => Card;
        public override Color ToolStripGradientBegin => Color.FromArgb(248, 250, 252);
        public override Color ToolStripGradientMiddle => Color.FromArgb(248, 250, 252);
        public override Color ToolStripGradientEnd => Color.FromArgb(248, 250, 252);
        public override Color SeparatorDark => Border;
        public override Color SeparatorLight => Card;
    }

    /// <summary>Styles a screen control and every child it owns.</summary>
    public static void ApplyScreen(Control root) {
        root.BackColor = Background;
        root.ForeColor = Text;
        root.Font = BaseFont;
        StyleTree(root);
    }

    public static void StyleTitle(Label label) {
        label.Font = TitleFont;
        label.ForeColor = Text;
    }

    public static void StyleSubtitle(Label label) {
        label.Font = SubtitleFont;
        label.ForeColor = Muted;
    }

    public static void StyleMuted(Label label) {
        label.Font = SmallFont;
        label.ForeColor = Muted;
    }

    public static void StyleInput(TextBox textBox) {
        textBox.Font = BaseFont;
        textBox.BackColor = Card;
        textBox.ForeColor = Text;
        textBox.BorderStyle = BorderStyle.FixedSingle;
    }

    public static void StyleInput(ComboBox comboBox) {
        comboBox.Font = BaseFont;
        comboBox.BackColor = Card;
        comboBox.ForeColor = Text;
        comboBox.FlatStyle = FlatStyle.Flat;
    }

    public static void StylePrimaryButton(Button button) {
        StyleButton(button, Accent, Color.White, AccentHover, 0);
    }

    public static void StyleSecondaryButton(Button button) {
        StyleButton(button, Card, Text, AccentSoft, 1);
        button.FlatAppearance.BorderColor = Border;
    }

    public static void StyleDangerButton(Button button) {
        StyleButton(button, Danger, Color.White, DangerHover, 0);
    }

    public static void StyleGrid(DataGridView grid) {
        grid.BackgroundColor = Card;
        grid.BorderStyle = BorderStyle.None;
        grid.CellBorderStyle = DataGridViewCellBorderStyle.SingleHorizontal;
        grid.GridColor = GridLine;
        grid.EnableHeadersVisualStyles = false;
        grid.ColumnHeadersDefaultCellStyle.BackColor = Color.FromArgb(248, 250, 252);
        grid.ColumnHeadersDefaultCellStyle.ForeColor = Muted;
        grid.ColumnHeadersDefaultCellStyle.Font = SemiboldFont;
        grid.ColumnHeadersDefaultCellStyle.SelectionBackColor = Color.FromArgb(248, 250, 252);
        grid.ColumnHeadersDefaultCellStyle.SelectionForeColor = Muted;
        grid.ColumnHeadersHeight = 38;
        grid.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.DisableResizing;
        grid.DefaultCellStyle.BackColor = Card;
        grid.DefaultCellStyle.ForeColor = Text;
        grid.DefaultCellStyle.Font = BaseFont;
        grid.DefaultCellStyle.SelectionBackColor = AccentSoft;
        grid.DefaultCellStyle.SelectionForeColor = Text;
        grid.DefaultCellStyle.Padding = new Padding(6, 0, 6, 0);
        grid.AlternatingRowsDefaultCellStyle.BackColor = Color.FromArgb(248, 250, 252);
        grid.RowTemplate.Height = 34;
    }

    private static bool IsActiveNav(Button button) => button.Tag as string == "active";

    private static void StyleTree(Control parent) {
        foreach (Control child in parent.Controls) {
            switch (child) {
                case Label label:
                    StyleLabel(label);
                    break;
                case Button button:
                    StyleButton(button);
                    break;
                case DataGridView grid:
                    StyleGrid(grid);
                    break;
                case TextBox textBox:
                    StyleInput(textBox);
                    break;
                case ComboBox comboBox:
                    StyleInput(comboBox);
                    break;
                case CheckedListBox checkedList:
                    checkedList.Font = BaseFont;
                    checkedList.BackColor = Card;
                    checkedList.ForeColor = Text;
                    checkedList.BorderStyle = BorderStyle.FixedSingle;
                    break;
                case DateTimePicker picker:
                    picker.Font = BaseFont;
                    break;
                case GroupBox groupBox:
                    groupBox.Font = SemiboldFont;
                    groupBox.ForeColor = Text;
                    break;
            }

            if (child.HasChildren) {
                StyleTree(child);
            }
        }
    }

    private static void StyleLabel(Label label) {
        if (label.Font.Size >= 14F
            || label.Name.Contains("Title", StringComparison.OrdinalIgnoreCase)) {
            StyleTitle(label);
            return;
        }

        if (label.Font.Bold && label.Font.Size >= 11.5F) {
            label.Font = SectionFont;
            label.ForeColor = Text;
            return;
        }

        if (label.ForeColor == Color.Gray || label.ForeColor == Color.DimGray) {
            StyleMuted(label);
            return;
        }

        label.Font = BaseFont;
        if (label.ForeColor == Color.Black) {
            label.ForeColor = Text;
        }
    }

    private static void StyleButton(Button button) {
        if (IsColor(button.BackColor, Color.Firebrick)
            || IsColor(button.BackColor, Color.Red)
            || IsColor(button.BackColor, Danger)) {
            StyleDangerButton(button);
            return;
        }

        if (IsColor(button.BackColor, Color.DarkSlateGray)
            || IsColor(button.BackColor, Color.SteelBlue)
            || IsColor(button.BackColor, Accent)) {
            StylePrimaryButton(button);
            return;
        }

        StyleSecondaryButton(button);
    }

    private static void StyleButton(
        Button button,
        Color normal,
        Color foreground,
        Color hover,
        int borderSize) {
        button.FlatStyle = FlatStyle.Flat;
        button.FlatAppearance.BorderSize = borderSize;
        button.FlatAppearance.MouseOverBackColor = hover;
        button.BackColor = normal;
        button.ForeColor = foreground;
        button.Font = SemiboldFont;
        button.Cursor = Cursors.Hand;
        button.UseVisualStyleBackColor = false;
        button.Padding = new Padding(10, 0, 10, 0);

        var normalBack = normal;
        var normalFore = foreground;

        button.MouseEnter += (_, _) => {
            if (button.Enabled) {
                button.BackColor = hover;
                if (normal == Card) {
                    button.ForeColor = Accent;
                }
            }
        };

        button.MouseLeave += (_, _) => {
            button.BackColor = normalBack;
            button.ForeColor = normalFore;
        };
    }

    private static bool IsColor(Color left, Color right)
        => left.R == right.R && left.G == right.G && left.B == right.B;
}
