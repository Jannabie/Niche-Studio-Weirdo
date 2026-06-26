using System.Drawing;
using System.Windows.Forms;

namespace BGITranslator
{
    public static class Clr
    {
        public static readonly Color BgDark  = Color.FromArgb(13,  13,  14);
        public static readonly Color BgPanel = Color.FromArgb(20,  20,  23);
        public static readonly Color BgInput = Color.FromArgb(24,  24,  27);
        public static readonly Color BgAlt   = Color.FromArgb(17,  17,  19);
        public static readonly Color Gold    = Color.FromArgb(201, 169, 110);
        public static readonly Color GoldDim = Color.FromArgb(115, 88,  48);
        public static readonly Color TxMain  = Color.FromArgb(230, 224, 212);
        public static readonly Color TxDim   = Color.FromArgb(148, 142, 130);
        public static readonly Color TxMuted = Color.FromArgb(72,  70,  66);
        public static readonly Color Bdr     = Color.FromArgb(35,  35,  39);
        public static readonly Color Bdr2    = Color.FromArgb(50,  50,  55);
        public static readonly Color Green   = Color.FromArgb(88,  168, 118);
        public static readonly Color Blue    = Color.FromArgb(100, 150, 185);
        public static readonly Color SelBg   = Color.FromArgb(48,  42,  22);
        public static readonly Color HitBg   = Color.FromArgb(58,  52,  28);
        public static readonly Color CurBg   = Color.FromArgb(82,  72,  32);
    }

    public class DarkColorTable : ProfessionalColorTable
    {
        public override Color MenuBorder                    { get { return Clr.Bdr2; } }
        public override Color MenuItemBorder                { get { return Clr.Bdr2; } }
        public override Color MenuItemSelected              { get { return Clr.SelBg; } }
        public override Color MenuItemSelectedGradientBegin { get { return Clr.SelBg; } }
        public override Color MenuItemSelectedGradientEnd   { get { return Clr.SelBg; } }
        public override Color MenuItemPressedGradientBegin  { get { return Clr.SelBg; } }
        public override Color MenuItemPressedGradientEnd    { get { return Clr.SelBg; } }
        public override Color MenuStripGradientBegin        { get { return Clr.BgPanel; } }
        public override Color MenuStripGradientEnd          { get { return Clr.BgPanel; } }
        public override Color ToolStripDropDownBackground   { get { return Clr.BgPanel; } }
        public override Color ImageMarginGradientBegin      { get { return Clr.BgPanel; } }
        public override Color ImageMarginGradientMiddle     { get { return Clr.BgPanel; } }
        public override Color ImageMarginGradientEnd        { get { return Clr.BgPanel; } }
        public override Color SeparatorDark                 { get { return Clr.Bdr2; } }
        public override Color SeparatorLight                { get { return Clr.Bdr2; } }
    }

    public class DarkRenderer : ToolStripProfessionalRenderer
    {
        public DarkRenderer() : base(new DarkColorTable()) { }

        protected override void OnRenderMenuItemBackground(ToolStripItemRenderEventArgs e)
        {
            Color bg = e.Item.Selected ? Clr.SelBg : Clr.BgPanel;
            using (SolidBrush br = new SolidBrush(bg))
                e.Graphics.FillRectangle(br, 0, 0, e.Item.Width, e.Item.Height);
        }
        protected override void OnRenderItemText(ToolStripItemTextRenderEventArgs e)
        {
            e.TextColor = e.Item.Selected ? Clr.Gold : Clr.TxDim;
            base.OnRenderItemText(e);
        }
        protected override void OnRenderToolStripBackground(ToolStripRenderEventArgs e)
        {
            using (SolidBrush br = new SolidBrush(Clr.BgPanel))
                e.Graphics.FillRectangle(br, e.AffectedBounds);
        }
        protected override void OnRenderToolStripBorder(ToolStripRenderEventArgs e)
        {
            using (Pen p = new Pen(Clr.Bdr2))
                e.Graphics.DrawLine(p, 0, e.AffectedBounds.Height - 1, e.AffectedBounds.Width, e.AffectedBounds.Height - 1);
        }
        protected override void OnRenderButtonBackground(ToolStripItemRenderEventArgs e)
        {
            if (e.Item.Selected || e.Item.Pressed)
            {
                Rectangle r = new Rectangle(1, 1, e.Item.Width - 2, e.Item.Height - 2);
                using (SolidBrush br = new SolidBrush(Clr.SelBg))
                    e.Graphics.FillRectangle(br, r);
                using (Pen p = new Pen(Clr.Bdr2))
                    e.Graphics.DrawRectangle(p, r);
            }
        }
    }
}
