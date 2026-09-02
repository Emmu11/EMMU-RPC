using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace EmmuRpc
{
    internal sealed class NeonPanel : Panel
    {
        public Color BorderColor { get; set; }

        public NeonPanel()
        {
            BorderColor = Color.FromArgb(0, 221, 255);
            BackColor = Color.FromArgb(14, 21, 37);
            SetStyle(ControlStyles.AllPaintingInWmPaint |
                     ControlStyles.OptimizedDoubleBuffer |
                     ControlStyles.UserPaint, true);
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            using (Pen outer = new Pen(Color.FromArgb(95, BorderColor), 1F))
                e.Graphics.DrawRectangle(outer, 0, 0, Width - 1, Height - 1);
            using (Pen inner = new Pen(Color.FromArgb(35, BorderColor), 1F))
                e.Graphics.DrawRectangle(inner, 2, 2, Width - 5, Height - 5);
        }
    }

    internal sealed class NeonButton : Button
    {
        private bool _hovered;

        public NeonButton()
        {
            FlatStyle = FlatStyle.Flat;
            FlatAppearance.BorderSize = 0;
            Cursor = Cursors.Hand;
            ForeColor = Color.White;
            BackColor = Color.FromArgb(104, 68, 255);
            SetStyle(ControlStyles.AllPaintingInWmPaint |
                     ControlStyles.OptimizedDoubleBuffer |
                     ControlStyles.UserPaint, true);
        }

        protected override void OnMouseEnter(EventArgs e)
        {
            _hovered = true;
            Invalidate();
            base.OnMouseEnter(e);
        }

        protected override void OnMouseLeave(EventArgs e)
        {
            _hovered = false;
            Invalidate();
            base.OnMouseLeave(e);
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
            Rectangle area = new Rectangle(0, 0, Width, Height);
            Color start = Enabled
                ? (_hovered ? Color.FromArgb(128, 82, 255) : Color.FromArgb(99, 65, 245))
                : Color.FromArgb(48, 54, 73);
            Color end = Enabled
                ? (_hovered ? Color.FromArgb(0, 238, 255) : Color.FromArgb(0, 196, 235))
                : Color.FromArgb(62, 68, 86);

            using (LinearGradientBrush brush = new LinearGradientBrush(area, start, end, 0F))
                e.Graphics.FillRectangle(brush, area);
            using (Pen border = new Pen(Color.FromArgb(170, 116, 247, 255), 1F))
                e.Graphics.DrawRectangle(border, 0, 0, Width - 1, Height - 1);

            TextRenderer.DrawText(
                e.Graphics,
                Text,
                Font,
                area,
                Enabled ? ForeColor : Color.FromArgb(145, 151, 170),
                TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.SingleLine);

            if (Focused && ShowFocusCues)
                ControlPaint.DrawFocusRectangle(e.Graphics, new Rectangle(4, 4, Width - 8, Height - 8));
        }
    }

    internal sealed class NeonListBox : ListBox
    {
        public NeonListBox()
        {
            DrawMode = DrawMode.OwnerDrawFixed;
            ItemHeight = 29;
            BorderStyle = BorderStyle.None;
            BackColor = Color.FromArgb(10, 16, 29);
            ForeColor = Color.FromArgb(226, 232, 246);
            IntegralHeight = false;
            HorizontalScrollbar = true;
        }

        protected override void OnDrawItem(DrawItemEventArgs e)
        {
            if (e.Index < 0 || e.Index >= Items.Count)
                return;

            bool selected = (e.State & DrawItemState.Selected) == DrawItemState.Selected;
            Color background = selected ? Color.FromArgb(29, 49, 73) : BackColor;
            using (SolidBrush brush = new SolidBrush(background))
                e.Graphics.FillRectangle(brush, e.Bounds);

            if (selected)
            {
                using (SolidBrush accent = new SolidBrush(Color.FromArgb(0, 221, 255)))
                    e.Graphics.FillRectangle(accent, e.Bounds.Left, e.Bounds.Top, 3, e.Bounds.Height);
            }

            Rectangle textArea = new Rectangle(e.Bounds.Left + 12, e.Bounds.Top, e.Bounds.Width - 18, e.Bounds.Height);
            TextRenderer.DrawText(
                e.Graphics,
                Items[e.Index].ToString(),
                Font,
                textArea,
                selected ? Color.White : ForeColor,
                TextFormatFlags.Left | TextFormatFlags.VerticalCenter | TextFormatFlags.EndEllipsis | TextFormatFlags.SingleLine);

            if ((e.State & DrawItemState.Focus) == DrawItemState.Focus)
                e.DrawFocusRectangle();
        }
    }
}

