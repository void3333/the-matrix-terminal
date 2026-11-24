using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Text;
using System.Windows.Forms;

internal sealed class MatrixRain : Panel
{
    private readonly Timer timer = new Timer();
    private readonly Font codeFont = new Font("Consolas", 11, FontStyle.Regular, GraphicsUnit.Pixel);
    private readonly Brush[] trail = {
        new SolidBrush(Color.FromArgb(5, 24, 12)),
        new SolidBrush(Color.FromArgb(6, 34, 17)),
        new SolidBrush(Color.FromArgb(8, 48, 21)),
        new SolidBrush(Color.FromArgb(14, 67, 31)),
        new SolidBrush(Color.FromArgb(34, 103, 49))
    };
    private readonly char[] glyphs = "0101<>{}=+*.:[]/".ToCharArray();
    private int frame;

    internal MatrixRain()
    {
        SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.UserPaint, true);
        BackColor = Color.FromArgb(3, 9, 5);
        TabStop = false;
        timer.Interval = 110;
        timer.Tick += delegate { frame++; Invalidate(); };
    }

    internal void Start() { timer.Start(); }

    protected override void OnPaint(PaintEventArgs args)
    {
        Graphics g = args.Graphics;
        g.Clear(BackColor);
        g.TextRenderingHint = TextRenderingHint.SingleBitPerPixelGridFit;
        for (int x = 8, stream = 0; x < Width; x += 19, stream++)
        {
            int span = Math.Max(1, Height + 240);
            int leading = (stream * 419 + frame * (5 + stream % 6)) % span;
            for (int step = 0; step < 16; step++)
            {
                int y = leading - step * 13;
                if (y < -13 || y > Height) continue;
                char glyph = glyphs[(stream * 13 + step * 7 + frame / 9) % glyphs.Length];
                g.DrawString(glyph.ToString(), codeFont, trail[Math.Max(0, trail.Length - 1 - step / 3)], x, y);
            }
        }
        using (var line = new Pen(Color.FromArgb(60, 0, 0, 0)))
        {
            for (int y = 0; y < Height; y += 4) g.DrawLine(line, 0, y, Width, y);
        }
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            timer.Dispose();
            codeFont.Dispose();
            foreach (Brush brush in trail) brush.Dispose();
        }
        base.Dispose(disposing);
    }
}

internal sealed class MatrixPanel : Panel
{
    internal Color Edge { get; set; }
    protected override void OnPaint(PaintEventArgs args)
    {
        base.OnPaint(args);
        if (Width <= 1 || Height <= 1) return;
        using (var pen = new Pen(Edge))
            args.Graphics.DrawRectangle(pen, 0, 0, Width - 1, Height - 1);
    }
}

internal sealed class MatrixMeter : Control
{
    private int value;
    internal int Value
    {
        get { return value; }
        set { this.value = Math.Min(100, Math.Max(0, value)); Invalidate(); }
    }

    internal MatrixMeter()
    {
        SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.UserPaint, true);
        TabStop = false;
        AccessibleName = "Progresso de instalação";
    }

    protected override void OnPaint(PaintEventArgs args)
    {
        if (ClientSize.Width < 2 || ClientSize.Height < 2) return;
        Graphics g = args.Graphics;
        g.Clear(Color.FromArgb(19, 41, 25));
        int width = ClientSize.Width * value / 100;
        if (width > 0)
        {
            using (var fill = new LinearGradientBrush(ClientRectangle, Color.FromArgb(16, 124, 58), Color.FromArgb(126, 255, 148), 0f))
                g.FillRectangle(fill, 0, 0, width, ClientSize.Height);
        }
        using (var pen = new Pen(Color.FromArgb(55, 99, 66)))
            g.DrawRectangle(pen, 0, 0, Width - 1, Height - 1);
    }
}
