using System.Drawing;
using System.Windows.Forms;

namespace OmniRouteLauncher
{
    public static class UITheme
    {
        // Color Tokens
        public static readonly Color Ink = Color.FromArgb(28, 35, 52);
        public static readonly Color Muted = Color.FromArgb(105, 115, 135);
        public static readonly Color Brand = Color.FromArgb(82, 73, 218);
        public static readonly Color Border = Color.FromArgb(225, 229, 238);
        public static readonly Color Bg = Color.FromArgb(247, 248, 252);
        public static readonly Color StatusCheckingBg = Color.FromArgb(238, 240, 246);
        public static readonly Color GreenBg = Color.FromArgb(232, 248, 239);
        public static readonly Color GreenText = Color.FromArgb(28, 125, 70);
        public static readonly Color GreenBorder = Color.FromArgb(198, 232, 210);
        public static readonly Color GreenHover = Color.FromArgb(220, 243, 229);
        public static readonly Color RedBg = Color.FromArgb(253, 238, 238);
        public static readonly Color RedText = Color.FromArgb(181, 55, 55);
        public static readonly Color RedBorder = Color.FromArgb(244, 211, 211);
        public static readonly Color RedHover = Color.FromArgb(255, 237, 237);
        public static readonly Color WarningBg = Color.FromArgb(255, 247, 226);
        public static readonly Color WarningText = Color.FromArgb(155, 99, 15);

        // Convenient Aliases
        public static readonly Color SuccessBg = GreenBg;
        public static readonly Color ErrorBg = RedBg;

        public static Button CreateButton(string text, Point location, Size size, Color background, Color foreground, Color border, bool primary)
        {
            Button btn = new Button();
            btn.Text = text;
            btn.Location = location;
            btn.Size = size;
            btn.BackColor = background;
            btn.ForeColor = foreground;
            btn.FlatStyle = FlatStyle.Flat;
            btn.Cursor = Cursors.Hand;
            btn.Font = new Font("Segoe UI", 9F, FontStyle.Bold);
            btn.UseVisualStyleBackColor = false;
            btn.FlatAppearance.BorderSize = 1;
            btn.FlatAppearance.BorderColor = border;

            if (primary)
            {
                btn.FlatAppearance.MouseOverBackColor = Color.FromArgb(70, 62, 195);
                btn.FlatAppearance.MouseDownBackColor = Color.FromArgb(58, 50, 170);
            }
            else if (background == RedBg)
            {
                btn.FlatAppearance.MouseOverBackColor = RedHover;
                btn.FlatAppearance.MouseDownBackColor = RedBg;
            }
            else if (background == GreenBg)
            {
                btn.FlatAppearance.MouseOverBackColor = GreenHover;
                btn.FlatAppearance.MouseDownBackColor = GreenBg;
            }
            else
            {
                btn.FlatAppearance.MouseOverBackColor = Color.FromArgb(240, 243, 248);
                btn.FlatAppearance.MouseDownBackColor = Color.FromArgb(230, 234, 242);
            }

            return btn;
        }
    }
}
