// ============================================================
// Animations.cs — 非线性缓动与动画控件（改编自 StyleKit 动画思路）
// 入场 / 悬停 / 指针 / 文字 / 加载 / 过渡 / 微交互
// ============================================================

using System.Drawing.Drawing2D;
using System.Drawing.Text;
using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace ipv6;

// ============================================================
// 非线性缓动函数
// ============================================================
internal static class Ease
{
    public static double OutCubic(double t) => 1 - Math.Pow(1 - t, 3);
    public static double InCubic(double t) => t * t * t;
    public static double InOutCubic(double t) => t < 0.5 ? 4 * t * t * t : 1 - Math.Pow(-2 * t + 2, 3) / 2;
    public static double OutQuint(double t) => 1 - Math.Pow(1 - t, 5);

    public static double BackOut(double t)
    {
        const double c1 = 1.70158, c3 = c1 + 1;
        return 1 + c3 * Math.Pow(t - 1, 3) + c1 * Math.Pow(t - 1, 2);
    }

    public static double ElasticOut(double t) =>
        t <= 0 ? 0 : t >= 1 ? 1 : Math.Pow(2, -10 * t) * Math.Sin((t * 10 - 0.75) * (2 * Math.PI / 3)) + 1;

    public static double Spring(double t) =>
        1 - Math.Pow(1 - t, 2) * Math.Cos(t * 9);

    public static double Smooth(double t)
    {
        t = Math.Clamp(t, 0, 1);
        return t * t * (3 - 2 * t);
    }

    // cubic-bezier(x1,y1,x2,y2)（StyleKit 常用一类的贝塞尔缓动）
    public static Func<double, double> Bezier(double x1, double y1, double x2, double y2)
    {
        double Cx(double t) => 3 * (1 - t) * (1 - t) * t * x1 + 3 * (1 - t) * t * t * x2 + t * t * t;
        double Cy(double t) => 3 * (1 - t) * (1 - t) * t * y1 + 3 * (1 - t) * t * t * y2 + t * t * t;
        return p =>
        {
            p = Math.Clamp(p, 0, 1);
            double lo = 0, hi = 1;
            for (int i = 0; i < 18; i++)
            {
                double mid = (lo + hi) / 2;
                if (Cx(mid) < p) lo = mid; else hi = mid;
            }
            return Cy((lo + hi) / 2);
        };
    }

    public static readonly Func<double, double> PageIn = Bezier(0.22, 1, 0.36, 1);   // easeOutQuint 类
    public static readonly Func<double, double> PageOut = Bezier(0.55, 0, 1, 0.45);
    public static readonly Func<double, double> Material = Bezier(0.4, 0, 0.2, 1);
}

// ============================================================
// 帧循环 / 抖动
// ============================================================
internal static class Anim
{
    public static async Task RunAsync(Func<double, double> ease, int durationMs, Action<double> step, CancellationToken ct = default)
    {
        var sw = Stopwatch.StartNew();
        try
        {
            while (true)
            {
                double p = durationMs <= 0 ? 1 : Math.Min(1, sw.Elapsed.TotalMilliseconds / durationMs);
                step(ease(p));
                if (p >= 1) break;
                await Task.Delay(8, ct);
            }
        }
        catch (OperationCanceledException) { }
        catch (ObjectDisposedException) { }
    }

    private sealed class ShakeState { public int Version; }
    private static readonly ConditionalWeakTable<Control, ShakeState> ShakeStates = new();

    public static void Shake(Control c)
    {
        if (c == null || c.IsDisposed) return;
        var state = ShakeStates.GetOrCreateValue(c);
        int version = ++state.Version;
        int origin = c.Left;
        _ = ShakeLoop(c, origin, version, state);
    }

    private static async Task ShakeLoop(Control c, int origin, int version, ShakeState state)
    {
        var sw = Stopwatch.StartNew();
        try
        {
            while (sw.ElapsedMilliseconds < 460)
            {
                if (c.IsDisposed || state.Version != version) break;
                double t = sw.ElapsedMilliseconds / 460.0;
                double damp = Math.Pow(1 - t, 2);
                c.Left = origin + (int)(Math.Sin(t * 44) * 8 * damp);
                await Task.Delay(12);
            }
        }
        finally
        {
            try { if (!c.IsDisposed) c.Left = origin; } catch { }
        }
    }
}

// ============================================================
// 主题与绘图工具
// ============================================================
internal static class UiTheme
{
    public static readonly Color BgDark = Color.FromArgb(13, 17, 23);
    public static readonly Color Card = Color.FromArgb(28, 33, 40);
    public static readonly Color Hover = Color.FromArgb(38, 44, 52);
    public static readonly Color Active = Color.FromArgb(48, 54, 61);
    public static readonly Color TextPrimary = Color.FromArgb(240, 246, 252);
    public static readonly Color TextSecondary = Color.FromArgb(139, 148, 158);
    public static readonly Color TextMuted = Color.FromArgb(125, 135, 146);
    public static readonly Color Blue = Color.FromArgb(88, 166, 255);
    public static readonly Color Green = Color.FromArgb(63, 185, 80);
    public static readonly Color Red = Color.FromArgb(248, 81, 73);
    public static readonly Color Orange = Color.FromArgb(255, 165, 50);
    public static readonly Color Purple = Color.FromArgb(170, 120, 255);
    public static readonly Color Cyan = Color.FromArgb(85, 210, 210);
    public static readonly Color Pink = Color.FromArgb(255, 100, 180);
    public static readonly Color Yellow = Color.FromArgb(230, 210, 80);

    public static readonly Font FontTitle = new("Segoe UI", 18F, FontStyle.Bold);
    public static readonly Font FontSub = new("Segoe UI", 9.5F);
    public static readonly Font FontButton = new("Segoe UI", 10F, FontStyle.Bold);
    public static readonly Font FontBody = new("Segoe UI", 9.5F);
    public static readonly Font FontSmall = new("Segoe UI", 8.5F);
    public static readonly Font FontTiny = new("Segoe UI", 7.5F);
    public static readonly Font FontTinyBold = new("Segoe UI", 7.5F, FontStyle.Bold);
    public static readonly Font FontIcon = new("Segoe UI Emoji", 15F);
    public static readonly Font FontWin = new("Segoe UI", 11F);
    public static readonly Font FontMetric = new("Segoe UI", 26F, FontStyle.Bold);
    public static readonly Font FontMetricTitle = new("Segoe UI", 8.5F);

    public static Color Blend(Color a, Color b, double t)
    {
        t = Math.Clamp(t, 0, 1);
        return Color.FromArgb(
            (int)(a.A + (b.A - a.A) * t),
            (int)(a.R + (b.R - a.R) * t),
            (int)(a.G + (b.G - a.G) * t),
            (int)(a.B + (b.B - a.B) * t));
    }

    public static Color Alpha(Color c, int alpha) => Color.FromArgb(Math.Clamp(alpha, 0, 255), c);

    public static GraphicsPath RoundRect(RectangleF r, float radius)
    {
        var path = new GraphicsPath();
        float d = Math.Max(0, Math.Min(radius * 2, Math.Min(r.Width, r.Height)));
        if (d <= 0) { path.AddRectangle(r); return path; }
        path.AddArc(r.X, r.Y, d, d, 180, 90);
        path.AddArc(r.Right - d, r.Y, d, d, 270, 90);
        path.AddArc(r.Right - d, r.Bottom - d, d, d, 0, 90);
        path.AddArc(r.X, r.Bottom - d, d, d, 90, 90);
        path.CloseFigure();
        return path;
    }
}

// ============================================================
// 动画瓷砖：淡入上滑 / 悬浮抬升 / 悬浮发光 / 聚光灯 / 磁吸 /
//           点击涟漪 / 边框光束 / 焦点描边
// ============================================================
internal sealed class AnimatedTile : Control
{
    public string TileTitle { get; set; } = "";
    public string TileDesc { get; set; } = "";
    public string TileIcon { get; set; } = "";
    public Color Accent { get; set; } = UiTheme.Blue;
    public Action? Activate { get; set; }

    private readonly System.Windows.Forms.Timer _ticker;
    private readonly List<(PointF pos, DateTime start, int seed)> _ripples = new();
    private double _hover, _hoverTarget;
    private double _press, _pressTarget;
    private double _magX, _magY, _magTargetX, _magTargetY;
    private double _alpha = 1, _rise;
    private Point _spot;
    private bool _beam;

    public AnimatedTile()
    {
        SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint |
                 ControlStyles.OptimizedDoubleBuffer | ControlStyles.SupportsTransparentBackColor |
                 ControlStyles.Selectable, true);
        Size = new Size(230, 66);
        Margin = new Padding(0, 0, 10, 10);
        Cursor = Cursors.Hand;
        TabStop = true;
        BackColor = Color.Transparent;
        AccessibleRole = AccessibleRole.PushButton;

        _ticker = new System.Windows.Forms.Timer { Interval = 16 };
        _ticker.Tick += (s, e) => OnTick();
    }

    public void PlayEntrance(int delayMs)
    {
        _alpha = 0;
        _rise = 16;
        _ = EntranceAsync(delayMs);
    }

    private async Task EntranceAsync(int delayMs)
    {
        if (delayMs > 0) await Task.Delay(delayMs);
        if (IsDisposed) return;
        await Anim.RunAsync(Ease.OutCubic, 460, p =>
        {
            if (IsDisposed) return;
            _alpha = Math.Clamp(p, 0, 1);
            _rise = 16 * (1 - p);
            Invalidate();
        });
    }

    protected override void OnHandleCreated(EventArgs e)
    {
        base.OnHandleCreated(e);
        AccessibleName = TileTitle;
    }

    protected override AccessibleObject CreateAccessibilityInstance() => new TileAccessible(this);

    private sealed class TileAccessible : ControlAccessibleObject
    {
        private readonly AnimatedTile _owner;
        public TileAccessible(AnimatedTile owner) : base(owner) => _owner = owner;
        public override string? DefaultAction => "打开";
        public override void DoDefaultAction()
        {
            base.DoDefaultAction();
            _owner.Activate?.Invoke();
        }
    }

    protected override void OnMouseEnter(EventArgs e)
    {
        base.OnMouseEnter(e);
        _hoverTarget = 1;
        _beam = true;
        _ticker.Start();
    }

    protected override void OnMouseLeave(EventArgs e)
    {
        base.OnMouseLeave(e);
        _hoverTarget = 0;
        _pressTarget = 0;
        _magTargetX = _magTargetY = 0;
        _beam = false;
        _ticker.Start();
    }

    protected override void OnMouseMove(MouseEventArgs e)
    {
        base.OnMouseMove(e);
        _spot = e.Location;
        if (_hoverTarget > 0)
        {
            _magTargetX = Math.Clamp((e.X - Width / 2.0) * 0.045, -4, 4);
            _magTargetY = Math.Clamp((e.Y - Height / 2.0) * 0.06, -3, 3);
        }
    }

    protected override void OnMouseDown(MouseEventArgs e)
    {
        base.OnMouseDown(e);
        if (e.Button != MouseButtons.Left) return;
        _pressTarget = 1;
        _ripples.Add((e.Location, DateTime.UtcNow, Random.Shared.Next()));
        _ticker.Start();
        Focus();
    }

    protected override void OnMouseUp(MouseEventArgs e)
    {
        base.OnMouseUp(e);
        _pressTarget = 0;
        _ticker.Start();
    }

    protected override void OnClick(EventArgs e)
    {
        base.OnClick(e);
        Activate?.Invoke();
    }

    protected override void OnKeyDown(KeyEventArgs e)
    {
        base.OnKeyDown(e);
        if (e.KeyCode is Keys.Enter or Keys.Space)
        {
            e.Handled = true;
            _pressTarget = 1;
            _ripples.Add((new PointF(Width / 2f, Height / 2f), DateTime.UtcNow, Random.Shared.Next()));
            _ticker.Start();
            Activate?.Invoke();
        }
    }

    protected override void OnKeyUp(KeyEventArgs e)
    {
        base.OnKeyUp(e);
        _pressTarget = 0;
        _ticker.Start();
    }

    protected override void OnEnter(EventArgs e) { base.OnEnter(e); Invalidate(); }
    protected override void OnLeave(EventArgs e) { base.OnLeave(e); Invalidate(); }

    private void OnTick()
    {
        bool need = false;

        if (Math.Abs(_hoverTarget - _hover) > 0.003)
        {
            _hover += (_hoverTarget - _hover) * 0.20;
            need = true;
        }
        else if (_hover != _hoverTarget) { _hover = _hoverTarget; need = true; }

        if (Math.Abs(_pressTarget - _press) > 0.004)
        {
            _press += (_pressTarget - _press) * 0.30;
            need = true;
        }
        else if (_press != _pressTarget) { _press = _pressTarget; need = true; }

        if (Math.Abs(_magTargetX - _magX) > 0.05 || Math.Abs(_magTargetY - _magY) > 0.05)
        {
            _magX += (_magTargetX - _magX) * 0.16;
            _magY += (_magTargetY - _magY) * 0.16;
            need = true;
        }

        _ripples.RemoveAll(r => (DateTime.UtcNow - r.start).TotalMilliseconds > 620);
        if (_ripples.Count > 0 || _beam) need = true;

        if (need) Invalidate();
        if (!need && _hover <= 0.001 && _pressTarget == 0 && !_beam) _ticker.Stop();
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        var g = e.Graphics;
        g.SmoothingMode = SmoothingMode.AntiAlias;
        g.TextRenderingHint = TextRenderingHint.AntiAliasGridFit;

        int a = (int)(255 * _alpha);
        float lift = (float)(_hover * 2.0);
        float scale = (float)(1 - _press * 0.02 + (_alpha < 1 ? (1 - _alpha) * -0.02 : 0));

        g.TranslateTransform(Width / 2f, Height / 2f);
        g.ScaleTransform(scale, scale);
        g.TranslateTransform(-Width / 2f, -Height / 2f);
        g.TranslateTransform((float)_magX, (float)_magY);

        var rect = new RectangleF(1, 1 - lift + (float)_rise * (float)_alpha, Width - 2, Height - 2);
        using var path = UiTheme.RoundRect(rect, 9);

        using (var bg = new SolidBrush(UiTheme.Alpha(UiTheme.Blend(UiTheme.Card, UiTheme.Hover, _hover), a)))
            g.FillPath(bg, path);

        if (_hover > 0.01)
        {
            var clip = g.Clip;
            g.SetClip(path);
            using var spot = new PathGradientBrush(path)
            {
                CenterPoint = _spot,
                CenterColor = UiTheme.Alpha(Accent, (int)(Math.Min(255, 55 * _hover) * _alpha)),
                SurroundColors = new[] { UiTheme.Alpha(Accent, 0) },
            };
            g.FillPath(spot, path);
            g.Clip = clip;
        }

        using (var bar = new SolidBrush(UiTheme.Alpha(Accent, a)))
            g.FillRectangle(bar, rect.X + 0.5f, rect.Y + 1, 3, rect.Height - 2);

        // 悬浮发光 + 边框光束
        using (var border = new Pen(UiTheme.Alpha(Accent, (int)((30 + 105 * _hover) * _alpha)), Focused ? 2f : 1f))
            g.DrawPath(border, path);

        if (_hover > 0.04)
            DrawBeam(g, rect);

        if (Focused)
        {
            using var fp = new Pen(UiTheme.Alpha(Accent, 190), 1) { DashStyle = DashStyle.Dot };
            var fr = new RectangleF(rect.X + 3, rect.Y + 3, rect.Width - 6, rect.Height - 6);
            using var fp2 = UiTheme.RoundRect(fr, 7);
            g.DrawPath(fp, fp2);
        }

        // 文字（随淡入透明度变化）
        g.DrawString(TileIcon, UiTheme.FontIcon, new SolidBrush(UiTheme.Alpha(UiTheme.TextPrimary, a)),
            new PointF(12, 9), StringFormat.GenericTypographic);
        g.DrawString(TileTitle, UiTheme.FontButton, new SolidBrush(UiTheme.Alpha(UiTheme.TextPrimary, a)),
            new PointF(48, 11), StringFormat.GenericTypographic);
        g.DrawString(TileDesc, UiTheme.FontTiny, new SolidBrush(UiTheme.Alpha(UiTheme.TextSecondary, a)),
            new RectangleF(48, 34, rect.Width - 56, 18), StringFormat.GenericTypographic);

        // 点击涟漪
        foreach (var r in _ripples)
        {
            double t = Math.Clamp((DateTime.UtcNow - r.start).TotalMilliseconds / 620, 0, 1);
            float radius = (float)(8 + 46 * Ease.OutCubic(t));
            int alpha = (int)(90 * Math.Pow(1 - t, 2) * _alpha);
            if (alpha <= 0) continue;
            using var rp = new Pen(UiTheme.Alpha(Accent, alpha), 2);
            g.DrawEllipse(rp, r.pos.X - radius, r.pos.Y - radius, radius * 2, radius * 2);
        }
    }

    private void DrawBeam(Graphics g, RectangleF rect)
    {
        double phase = Stopwatch.GetTimestamp() / (double)Stopwatch.Frequency;
        float eased = (float)(0.5 - 0.5 * Math.Cos((phase * 0.62 % 1) * Math.PI * 2)); // 非线性往复
        float x = rect.Left - 70 + (rect.Width + 140) * eased;
        using var lg = new LinearGradientBrush(
            new RectangleF(x - 60, rect.Top - 1, 120, 3),
            UiTheme.Alpha(Accent, 0), UiTheme.Alpha(Accent, 0), 0f);
        lg.InterpolationColors = new ColorBlend
        {
            Colors = new[] { UiTheme.Alpha(Accent, 0), UiTheme.Alpha(Accent, (int)(230 * _hover * _alpha)), UiTheme.Alpha(Accent, 0) },
            Positions = new[] { 0f, 0.5f, 1f },
        };
        g.FillRectangle(lg, x - 60, rect.Top - 1, 120, 3);
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing) { try { _ticker.Stop(); _ticker.Dispose(); } catch { } }
        base.Dispose(disposing);
    }
}

// ============================================================
// 环境背景：背景渐变流动 + 形状变形（blob）
// ============================================================
internal sealed class AmbientTileHost : FlowLayoutPanel
{
    private readonly System.Windows.Forms.Timer _timer;
    private double _t;

    public AmbientTileHost()
    {
        SetStyle(ControlStyles.OptimizedDoubleBuffer | ControlStyles.AllPaintingInWmPaint, true);
        BackColor = UiTheme.BgDark;
        _timer = new System.Windows.Forms.Timer { Interval = 50 };
        _timer.Tick += (s, e) => { _t += 0.05; Invalidate(); };
    }

    protected override void OnVisibleChanged(EventArgs e)
    {
        base.OnVisibleChanged(e);
        if (Visible) _timer.Start(); else _timer.Stop();
    }

    protected override void OnPaintBackground(PaintEventArgs e)
    {
        var g = e.Graphics;
        var r = ClientRectangle;
        if (r.Width <= 0 || r.Height <= 0) return;

        using (var bg = new LinearGradientBrush(r, UiTheme.BgDark, Color.FromArgb(17, 22, 30), 60f))
            g.FillRectangle(bg, r);

        Blob(g, r, 0.24 + 0.04 * Math.Sin(_t * 0.7), 0.22 + 0.03 * Math.Cos(_t * 0.6), 0.34, UiTheme.Blue, 26);
        Blob(g, r, 0.76 + 0.04 * Math.Cos(_t * 0.55), 0.35 + 0.04 * Math.Sin(_t * 0.8), 0.30, UiTheme.Purple, 22);
        Blob(g, r, 0.55 + 0.05 * Math.Sin(_t * 0.45), 0.85 + 0.03 * Math.Cos(_t * 0.5), 0.32, UiTheme.Cyan, 18);
    }

    private static void Blob(Graphics g, Rectangle r, double cx, double cy, double radiusRatio, Color color, int alpha)
    {
        float baseR = (float)(Math.Min(r.Width, r.Height) * radiusRatio * (1 + 0.10 * Math.Sin(cx * 9 + cy * 7)));
        var rect = new RectangleF(
            (float)(r.Width * cx) - baseR,
            (float)(r.Height * cy) - baseR,
            baseR * 2, baseR * 2);
        if (rect.Width < 4) return;

        using var path = new GraphicsPath();
        path.AddEllipse(rect);
        using var pgb = new PathGradientBrush(path)
        {
            CenterColor = UiTheme.Alpha(color, alpha),
            SurroundColors = new[] { UiTheme.Alpha(color, 0) },
        };
        g.FillPath(pgb, path);
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing) { try { _timer.Stop(); _timer.Dispose(); } catch { } }
        base.Dispose(disposing);
    }
}

// ============================================================
// 按钮：涟漪点击 + 悬浮抬升/发光 + 按压回弹
// ============================================================
internal sealed class FlatButton : Control
{
    public Color NormalBack { get; set; } = UiTheme.Blue;
    public Color HoverBack { get; set; } = Color.FromArgb(255, 120, 180, 255);
    public Color NormalFore { get; set; } = Color.White;
    public Color HoverFore { get; set; } = Color.White;
    public Color? GlowColor { get; set; }
    public float Radius { get; set; } = 6f;

    private readonly System.Windows.Forms.Timer _ticker;
    private readonly List<(PointF pos, DateTime start)> _ripples = new();
    private double _hover, _hoverTarget;
    private double _press, _pressTarget;

    public FlatButton()
    {
        SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint |
                 ControlStyles.OptimizedDoubleBuffer | ControlStyles.SupportsTransparentBackColor |
                 ControlStyles.Selectable, true);
        BackColor = Color.Transparent;
        Cursor = Cursors.Hand;
        TabStop = true;
        Font = UiTheme.FontButton;
        AccessibleRole = AccessibleRole.PushButton;
        _ticker = new System.Windows.Forms.Timer { Interval = 16 };
        _ticker.Tick += (s, e) => OnTick();
    }

    protected override void OnTextChanged(EventArgs e)
    {
        base.OnTextChanged(e);
        AccessibleName = Text;
        Invalidate();
    }

    protected override AccessibleObject CreateAccessibilityInstance() => new ButtonAccessible(this);

    private sealed class ButtonAccessible : ControlAccessibleObject
    {
        private readonly FlatButton _owner;
        public ButtonAccessible(FlatButton owner) : base(owner) => _owner = owner;
        public override string? DefaultAction => "按下";
        public override void DoDefaultAction()
        {
            base.DoDefaultAction();
            if (_owner.Enabled) _owner.OnClick(EventArgs.Empty);
        }
    }

    protected override void OnEnabledChanged(EventArgs e)
    {
        base.OnEnabledChanged(e);
        Invalidate();
    }

    protected override void OnMouseEnter(EventArgs e)
    {
        base.OnMouseEnter(e);
        _hoverTarget = 1;
        _ticker.Start();
    }

    protected override void OnMouseLeave(EventArgs e)
    {
        base.OnMouseLeave(e);
        _hoverTarget = 0;
        _pressTarget = 0;
        _ticker.Start();
    }

    protected override void OnMouseDown(MouseEventArgs e)
    {
        base.OnMouseDown(e);
        if (e.Button != MouseButtons.Left) return;
        _pressTarget = 1;
        _ripples.Add((e.Location, DateTime.UtcNow));
        _ticker.Start();
    }

    protected override void OnMouseUp(MouseEventArgs e)
    {
        base.OnMouseUp(e);
        _pressTarget = 0;
        _ticker.Start();
    }

    protected override void OnKeyDown(KeyEventArgs e)
    {
        base.OnKeyDown(e);
        if ((e.KeyCode is Keys.Enter or Keys.Space) && Enabled)
        {
            e.Handled = true;
            _pressTarget = 1;
            _ripples.Add((new PointF(Width / 2f, Height / 2f), DateTime.UtcNow));
            _ticker.Start();
            OnClick(EventArgs.Empty);
        }
    }

    protected override void OnKeyUp(KeyEventArgs e)
    {
        base.OnKeyUp(e);
        _pressTarget = 0;
        _ticker.Start();
    }

    protected override void OnEnter(EventArgs e) { base.OnEnter(e); Invalidate(); }
    protected override void OnLeave(EventArgs e) { base.OnLeave(e); Invalidate(); }

    private void OnTick()
    {
        bool need = false;
        if (Math.Abs(_hoverTarget - _hover) > 0.003) { _hover += (_hoverTarget - _hover) * 0.22; need = true; }
        else if (_hover != _hoverTarget) { _hover = _hoverTarget; need = true; }
        if (Math.Abs(_pressTarget - _press) > 0.004) { _press += (_pressTarget - _press) * 0.32; need = true; }
        else if (_press != _pressTarget) { _press = _pressTarget; need = true; }

        _ripples.RemoveAll(r => (DateTime.UtcNow - r.start).TotalMilliseconds > 520);
        if (_ripples.Count > 0) need = true;

        if (need) Invalidate();
        if (!need && _hover <= 0.001 && _pressTarget == 0) _ticker.Stop();
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        var g = e.Graphics;
        g.SmoothingMode = SmoothingMode.AntiAlias;
        g.TextRenderingHint = TextRenderingHint.AntiAliasGridFit;

        float scale = (float)(1 - _press * 0.025);
        int lift = (int)Math.Round(_hover * 1.0);

        g.TranslateTransform(Width / 2f, Height / 2f);
        g.ScaleTransform(scale, scale);
        g.TranslateTransform(-Width / 2f, -Height / 2f);

        var rect = new RectangleF(1, 1 - lift, Width - 2, Height - 2);
        using var path = UiTheme.RoundRect(rect, Radius);

        Color bg = UiTheme.Blend(NormalBack, HoverBack, _hover);
        if (bg.A > 0)
        {
            using var brush = new SolidBrush(bg);
            g.FillPath(brush, path);
        }
        if (!Enabled && bg.A > 0)
        {
            using var dim = new SolidBrush(Color.FromArgb(120, UiTheme.BgDark));
            g.FillPath(dim, path);
        }

        if (GlowColor is Color glow && _hover > 0.01)
        {
            using var pen = new Pen(UiTheme.Alpha(glow, (int)(110 * _hover)), 1);
            g.DrawPath(pen, path);
        }
        if (Focused)
        {
            using var pen = new Pen(Color.FromArgb(200, UiTheme.TextPrimary), 1) { DashStyle = DashStyle.Dot };
            g.DrawPath(pen, path);
        }

        Color fore = Enabled ? UiTheme.Blend(NormalFore, HoverFore, _hover) : UiTheme.TextMuted;
        TextRenderer.DrawText(g, Text, Font, new Rectangle(0, -lift, Width, Height), fore,
            TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.NoPrefix |
            TextFormatFlags.EndEllipsis);

        foreach (var r in _ripples)
        {
            double t = Math.Clamp((DateTime.UtcNow - r.start).TotalMilliseconds / 520, 0, 1);
            float radius = (float)(6 + Math.Min(Width, Height) * 0.55 * Ease.OutCubic(t));
            int alpha = (int)(80 * Math.Pow(1 - t, 2));
            if (alpha <= 0) continue;
            using var pen = new Pen(Color.FromArgb(alpha, Color.White), 2);
            g.DrawEllipse(pen, r.pos.X - radius, r.pos.Y - radius, radius * 2, radius * 2);
        }
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing) { try { _ticker.Stop(); _ticker.Dispose(); } catch { } }
        base.Dispose(disposing);
    }
}

// ============================================================
// 页面标题：文字渐变流动 + 文字揭示 + 打字机 + 下划线绘制
// ============================================================
internal sealed class PageHeaderView : Control
{
    public string TitleText { get; }
    public string SubtitleText { get; }
    public Color Accent { get; set; }

    private readonly System.Windows.Forms.Timer _flow;
    private readonly System.Windows.Forms.Timer _anim;
    private readonly Stopwatch _playClock = new();
    private double _phase;
    private double _type;
    private double _rise = 1;
    private double _line;

    public PageHeaderView(string title, string subtitle, Color accent)
    {
        TitleText = title;
        SubtitleText = subtitle;
        Accent = accent;
        Height = 64;
        Dock = DockStyle.Top;
        SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint |
                 ControlStyles.OptimizedDoubleBuffer | ControlStyles.SupportsTransparentBackColor, true);
        BackColor = Color.Transparent;
        AccessibleRole = AccessibleRole.StaticText;
        AccessibleName = title;

        _flow = new System.Windows.Forms.Timer { Interval = 60 };
        _flow.Tick += (s, e) => { _phase += 0.06; if (Visible) Invalidate(); };

        _anim = new System.Windows.Forms.Timer { Interval = 15 };
        _anim.Tick += (s, e) =>
        {
            double t = Math.Clamp(_playClock.Elapsed.TotalMilliseconds / 760.0, 0, 1);
            double e2 = Ease.OutCubic(t);
            _type = e2;
            _rise = Math.Min(1, t * 1.6);
            _line = e2;
            Invalidate();
            if (t >= 1) _anim.Stop();
        };
    }

    public void Play()
    {
        _type = 0;
        _rise = 0;
        _line = 0;
        _playClock.Restart();
        _anim.Start();
        Invalidate();
    }

    protected override void OnVisibleChanged(EventArgs e)
    {
        base.OnVisibleChanged(e);
        if (Visible)
        {
            _flow.Start();
            if (_type <= 0.001) Play();
        }
        else
        {
            _flow.Stop();
            _anim.Stop();
        }
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        var g = e.Graphics;
        g.SmoothingMode = SmoothingMode.AntiAlias;
        g.TextRenderingHint = TextRenderingHint.AntiAliasGridFit;

        double rise = (1 - Ease.OutCubic(Math.Clamp(_rise, 0, 1))) * 7;
        double vis = Math.Clamp(_rise, 0, 1);
        if (vis <= 0.001) return;

        Color Fade(Color c) => UiTheme.Blend(UiTheme.BgDark, c, vis);

        // 文字揭示 + 文字渐变流动（基础色 + 移动高光带，GDI 渲染）
        var titleRect = new Rectangle(0, (int)(1 + rise), Math.Max(10, Width), 30);
        TextRenderer.DrawText(g, TitleText, UiTheme.FontTitle, titleRect, Fade(UiTheme.TextPrimary),
            TextFormatFlags.Left | TextFormatFlags.NoPrefix);

        float sweep = (float)(0.5 - 0.5 * Math.Cos(_phase * 1.15));
        float bandX = -160 + (Width + 320) * sweep;
        var oldClip = g.Clip;
        g.SetClip(new RectangleF(bandX, titleRect.Y - 2, 150, titleRect.Height + 4));
        TextRenderer.DrawText(g, TitleText, UiTheme.FontTitle, titleRect, Fade(Accent),
            TextFormatFlags.Left | TextFormatFlags.NoPrefix);
        g.SetClip(new RectangleF(bandX + 90, titleRect.Y - 2, 70, titleRect.Height + 4));
        TextRenderer.DrawText(g, TitleText, UiTheme.FontTitle, titleRect, Fade(UiTheme.Cyan),
            TextFormatFlags.Left | TextFormatFlags.NoPrefix);
        g.Clip = oldClip;

        // 打字机字幕 + 光标
        int shown = (int)Math.Ceiling(SubtitleText.Length * Math.Clamp(_type, 0, 1));
        string sub = SubtitleText[..Math.Clamp(shown, 0, SubtitleText.Length)];
        var subPoint = new Point(2, (int)(34 + rise));
        TextRenderer.DrawText(g, sub, UiTheme.FontSub, subPoint, Fade(UiTheme.TextSecondary),
            TextFormatFlags.Left | TextFormatFlags.NoPrefix | TextFormatFlags.NoPadding);

        if (_type < 0.999 && ((int)(_phase * 30)) % 2 == 0)
        {
            int tx = 2 + TextRenderer.MeasureText(sub, UiTheme.FontSub).Width;
            using var caret = new SolidBrush(Fade(Accent));
            g.FillRectangle(caret, tx + 1, 37 + (float)rise, 2, 12);
        }

        // 下划线绘制（自左向右）
        float lineW = (float)(Math.Min(Width, 260) * Ease.OutCubic(Math.Clamp(_line, 0, 1)));
        if (lineW > 1)
        {
            using var brush = new SolidBrush(Fade(Accent));
            g.FillRectangle(brush, 2, 57, lineW, 2);
            using var tail = new SolidBrush(Fade(UiTheme.Cyan));
            g.FillRectangle(tail, 2 + lineW * 0.65f, 57, lineW * 0.35f, 2);
        }
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            try { _flow.Stop(); _flow.Dispose(); } catch { }
            try { _anim.Stop(); _anim.Dispose(); } catch { }
        }
        base.Dispose(disposing);
    }
}

// ============================================================
// 连接状态点：脉搏跳动 + 脉冲光环
// ============================================================
internal sealed class StatusDot : Control
{
    private readonly System.Windows.Forms.Timer _timer;
    private readonly Stopwatch _clock = Stopwatch.StartNew();
    private bool? _connected;

    public bool? Connected
    {
        get => _connected;
        set { _connected = value; Invalidate(); }
    }

    public StatusDot()
    {
        SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint |
                 ControlStyles.OptimizedDoubleBuffer | ControlStyles.SupportsTransparentBackColor, true);
        Size = new Size(22, 22);
        BackColor = Color.Transparent;
        AccessibleRole = AccessibleRole.Indicator;
        _timer = new System.Windows.Forms.Timer { Interval = 33 };
        _timer.Tick += (s, e) => Invalidate();
    }

    protected override void OnVisibleChanged(EventArgs e)
    {
        base.OnVisibleChanged(e);
        if (Visible) _timer.Start(); else _timer.Stop();
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        var g = e.Graphics;
        g.SmoothingMode = SmoothingMode.AntiAlias;

        Color color = _connected == true ? UiTheme.Green : _connected == false ? UiTheme.Red : UiTheme.TextMuted;
        float cx = Width / 2f, cy = Height / 2f;
        double t = _clock.Elapsed.TotalSeconds;

        // 脉冲光环（两道，错开相位，非线性淡出）
        for (int i = 0; i < 2; i++)
        {
            double phase = (t * 0.75 + i * 0.5) % 1;
            double e2 = Ease.OutCubic(phase);
            float radius = (float)(4 + 8 * e2);
            int alpha = (int)(110 * Math.Pow(1 - phase, 2));
            if (alpha <= 0) continue;
            using var pen = new Pen(Color.FromArgb(alpha, color), 1.4f);
            g.DrawEllipse(pen, cx - radius, cy - radius, radius * 2, radius * 2);
        }

        double pulse = 0.5 + 0.5 * Math.Sin(t * 2.4);
        float core = (float)(3.6 + 1.0 * Ease.Smooth(pulse));
        using (var glow = new SolidBrush(Color.FromArgb(45, color)))
            g.FillEllipse(glow, cx - core - 3, cy - core - 3, (core + 3) * 2, (core + 3) * 2);
        using (var b = new SolidBrush(color))
            g.FillEllipse(b, cx - core, cy - core, core * 2, core * 2);
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing) { try { _timer.Stop(); _timer.Dispose(); } catch { } }
        base.Dispose(disposing);
    }
}

// ============================================================
// 状态文字：文字解密（乱码快速滚动后逐字解码）
// ============================================================
internal sealed class StatusText : Control
{
    private string _target = "";
    private Color _color = UiTheme.TextSecondary;
    private double _reveal = 1;
    private int _version;
    private const string Noise = "01#%&@$*<>/\\|=+~";

    public StatusText()
    {
        SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint |
                 ControlStyles.OptimizedDoubleBuffer | ControlStyles.SupportsTransparentBackColor, true);
        BackColor = Color.Transparent;
        Font = UiTheme.FontSmall;
        Height = 20;
        AutoSize = false;
        AccessibleRole = AccessibleRole.StaticText;
    }

    public void SetStatus(string text, Color color, bool animate = true)
    {
        _target = text ?? "";
        _color = color;
        AccessibleName = _target;
        Width = TextRenderer.MeasureText(_target, Font).Width + 6;
        if (!animate)
        {
            _reveal = 1;
            Invalidate();
            return;
        }

        int version = ++_version;
        _reveal = 0;
        _ = Anim.RunAsync(Ease.OutCubic, 460, p =>
        {
            if (version != _version || IsDisposed) return;
            _reveal = p;
            Invalidate();
        });
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        var g = e.Graphics;
        g.TextRenderingHint = TextRenderingHint.AntiAliasGridFit;

        var sb = new System.Text.StringBuilder(_target.Length);
        double reveal = Math.Clamp(_reveal, 0, 1);
        int shown = (int)(_target.Length * reveal);
        for (int i = 0; i < _target.Length; i++)
        {
            char ch = _target[i];
            if (i < shown || ch == ' ') sb.Append(ch);
            else if (reveal > 0.02) sb.Append(Noise[Random.Shared.Next(Noise.Length)]);
            else sb.Append(' ');
        }

        using var brush = new SolidBrush(_color);
        g.DrawString(sb.ToString(), Font, brush, new PointF(0, (Height - Font.Height) / 2f), StringFormat.GenericTypographic);
    }
}

// ============================================================
// 加载视图：三点弹跳 + 骨架脉冲 + 微光扫过 + 进度条光泽
// ============================================================
internal sealed class LoadingView : Control
{
    private readonly System.Windows.Forms.Timer _timer;
    private readonly Stopwatch _clock = Stopwatch.StartNew();
    private string _status = "加载中...";

    public LoadingView()
    {
        SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint |
                 ControlStyles.OptimizedDoubleBuffer | ControlStyles.SupportsTransparentBackColor, true);
        BackColor = Color.Transparent;
        Dock = DockStyle.Fill;
        _timer = new System.Windows.Forms.Timer { Interval = 33 };
        _timer.Tick += (s, e) => Invalidate();
    }

    public void SetStatus(string text)
    {
        _status = text ?? "加载中...";
        Invalidate();
    }

    protected override void OnVisibleChanged(EventArgs e)
    {
        base.OnVisibleChanged(e);
        if (Visible) _timer.Start(); else _timer.Stop();
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        var g = e.Graphics;
        g.SmoothingMode = SmoothingMode.AntiAlias;
        g.TextRenderingHint = TextRenderingHint.AntiAliasGridFit;

        double t = _clock.Elapsed.TotalSeconds;
        float cx = Width / 2f, cy = Height / 2f;

        // 三点弹跳
        for (int i = 0; i < 3; i++)
        {
            double phase = (t * 1.7 + i * 0.22) % 1;
            double bounce = phase < 0.5 ? Math.Abs(Math.Sin(phase * Math.PI * 2)) * (1 - phase * 0.6) : 0;
            float y = (float)(cy - 46 - bounce * 16);
            int alpha = (int)(120 + 110 * bounce);
            using var brush = new SolidBrush(UiTheme.Alpha(i == 1 ? UiTheme.Cyan : UiTheme.Blue, alpha));
            g.FillEllipse(brush, cx - 26 + i * 22, y, 10, 10);
        }

        // 骨架屏脉冲
        double pulse = 0.55 + 0.45 * Math.Sin(t * 2.2);
        using (var bar = new SolidBrush(Color.FromArgb((int)(28 + 26 * pulse), UiTheme.TextPrimary)))
        {
            using var p1 = UiTheme.RoundRect(new RectangleF(cx - 110, cy - 12, 220, 8), 4);
            using var p2 = UiTheme.RoundRect(new RectangleF(cx - 70, cy + 2, 140, 8), 4);
            g.FillPath(bar, p1);
            g.FillPath(bar, p2);
        }

        // 状态文字 + 微光扫过（GDI 渲染，避免透明控件上 GDI+ 文本失效）
        var sz = TextRenderer.MeasureText(_status, UiTheme.FontBody);
        float sx = cx - sz.Width / 2f, sy = cy + 30;
        var textRect = new Rectangle((int)sx, (int)sy, sz.Width, sz.Height);
        TextRenderer.DrawText(g, _status, UiTheme.FontBody, textRect, UiTheme.TextSecondary,
            TextFormatFlags.Left | TextFormatFlags.NoPrefix | TextFormatFlags.NoPadding);

        float shimmerX = (float)((0.5 - 0.5 * Math.Cos(t * 1.6)) * (sz.Width + 120)) - 60;
        var shimmerRect = new Rectangle((int)(sx + shimmerX), (int)sy, 60, sz.Height);
        if (shimmerRect.IntersectsWith(textRect))
        {
            g.SetClip(Rectangle.Intersect(shimmerRect, textRect));
            TextRenderer.DrawText(g, _status, UiTheme.FontBody, textRect, UiTheme.TextPrimary,
                TextFormatFlags.Left | TextFormatFlags.NoPrefix | TextFormatFlags.NoPadding);
            g.ResetClip();
        }

        // 进度条：轨道 + 非线性往复的光泽段
        float barW = 260, barH = 4;
        float barX = cx - barW / 2, barY = cy + 54;
        using (var track = new SolidBrush(Color.FromArgb(36, UiTheme.TextPrimary)))
        using (var tp = UiTheme.RoundRect(new RectangleF(barX, barY, barW, barH), 2))
            g.FillPath(track, tp);

        double segPhase = 0.5 - 0.5 * Math.Cos(t * 1.25);
        float segW = 90;
        float segX = barX + (float)((barW - segW) * Ease.InOutCubic(segPhase));
        var segRect = new RectangleF(segX, barY, segW, barH);
        using (var lg = new LinearGradientBrush(segRect, UiTheme.Alpha(UiTheme.Blue, 40), UiTheme.Alpha(UiTheme.Blue, 40), 0f))
        {
            lg.InterpolationColors = new ColorBlend
            {
                Colors = new[]
                {
                    UiTheme.Alpha(UiTheme.Blue, 10), UiTheme.Alpha(UiTheme.Cyan, 200), UiTheme.Alpha(UiTheme.Blue, 10),
                },
                Positions = new[] { 0f, 0.5f, 1f },
            };
            using var sp = UiTheme.RoundRect(segRect, 2);
            g.FillPath(lg, sp);
        }
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing) { try { _timer.Stop(); _timer.Dispose(); } catch { } }
        base.Dispose(disposing);
    }
}

// ============================================================
// 彩纸爆炸（复制成功等点击反馈）
// ============================================================
internal static class ConfettiFx
{
    private sealed class Particle
    {
        public float X, Y, VX, VY, Rot, VRot, Size;
        public Color Color;
        public DateTime Start;
        public int LifeMs;
    }

    private sealed class Burst
    {
        public required Control Host;
        public System.Windows.Forms.Timer Timer = null!;
        public readonly List<Particle> Particles = new();
        public bool Painting;

        public void Attach()
        {
            Timer = new System.Windows.Forms.Timer { Interval = 16 };
            Timer.Tick += (s, e) => Step();
            Host.Paint += OnPaint;
            Host.Disposed += (s, e) => { Timer.Stop(); Timer.Dispose(); };
        }

        public void Step()
        {
            var now = DateTime.UtcNow;
            for (int i = Particles.Count - 1; i >= 0; i--)
            {
                var p = Particles[i];
                double age = (now - p.Start).TotalMilliseconds;
                if (age > p.LifeMs) { Particles.RemoveAt(i); continue; }
                p.VY += 0.35f;
                p.X += p.VX;
                p.Y += p.VY;
                p.VX *= 0.985f;
                p.Rot += p.VRot;
            }
            Host.Invalidate();
            if (Particles.Count == 0) Timer.Stop();
        }

        public void OnPaint(object? sender, PaintEventArgs e)
        {
            if (Particles.Count == 0) return;
            var now = DateTime.UtcNow;
            e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
            foreach (var p in Particles)
            {
                double t = Math.Clamp((now - p.Start).TotalMilliseconds / p.LifeMs, 0, 1);
                int alpha = (int)(255 * Math.Pow(1 - t, 1.4));
                if (alpha <= 0) continue;
                var state = e.Graphics.Save();
                e.Graphics.TranslateTransform(p.X, p.Y);
                e.Graphics.RotateTransform(p.Rot);
                using var brush = new SolidBrush(Color.FromArgb(alpha, p.Color));
                e.Graphics.FillRectangle(brush, -p.Size / 2, -p.Size / 4, p.Size, p.Size / 2);
                e.Graphics.Restore(state);
            }
        }
    }

    private static readonly ConditionalWeakTable<Control, Burst> Bursts = new();

    public static void Play(Control host, Point origin)
    {
        if (host == null || host.IsDisposed) return;
        var burst = Bursts.GetOrCreateValue(host);
        if (!burst.Painting)
        {
            burst.Host = host;
            burst.Attach();
            burst.Painting = true;
        }

        var colors = new[] { UiTheme.Blue, UiTheme.Green, UiTheme.Orange, UiTheme.Purple, UiTheme.Cyan, UiTheme.Pink, UiTheme.Yellow };
        var now = DateTime.UtcNow;
        for (int i = 0; i < 18; i++)
        {
            double angle = -Math.PI / 2 + (Random.Shared.NextDouble() - 0.5) * Math.PI * 1.3;
            double speed = 2.2 + Random.Shared.NextDouble() * 3.4;
            burst.Particles.Add(new Particle
            {
                X = origin.X,
                Y = origin.Y,
                VX = (float)(Math.Cos(angle) * speed),
                VY = (float)(Math.Sin(angle) * speed),
                Rot = Random.Shared.Next(360),
                VRot = (float)((Random.Shared.NextDouble() - 0.5) * 22),
                Size = 4 + (float)Random.Shared.NextDouble() * 5,
                Color = colors[Random.Shared.Next(colors.Length)],
                Start = now,
                LifeMs = 700 + Random.Shared.Next(350),
            });
        }
        burst.Timer.Start();
    }
}

// ============================================================
// Toast（滑入 + 淡入淡出，非线性）
// ============================================================
internal static class ToastFx
{
    private sealed class ToastForm : Form
    {
        private readonly string _message;
        private readonly Color _accent;

        public ToastForm(string message, Color accent)
        {
            _message = message;
            _accent = accent;
            FormBorderStyle = FormBorderStyle.None;
            ShowInTaskbar = false;
            StartPosition = FormStartPosition.Manual;
            Size = new Size(300, 54);
            BackColor = UiTheme.Card;
            Opacity = 0;
            DoubleBuffered = true;
            TopMost = false;
        }

        protected override bool ShowWithoutActivation => true;

        protected override CreateParams CreateParams
        {
            get
            {
                var cp = base.CreateParams;
                cp.ExStyle |= 0x08000000; // WS_EX_NOACTIVATE
                cp.ExStyle |= 0x00000080; // WS_EX_TOOLWINDOW
                return cp;
            }
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            var g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            using (var bg = new SolidBrush(UiTheme.Card))
                g.FillRectangle(bg, ClientRectangle);
            using (var bar = new SolidBrush(_accent))
                g.FillRectangle(bar, 0, 0, 3, Height);
            using (var icon = new SolidBrush(_accent))
                g.FillEllipse(icon, 18, Height / 2f - 10, 20, 20);
            using (var check = new Pen(Color.White, 2f))
            {
                g.DrawLines(check, new[]
                {
                    new PointF(23, Height / 2f),
                    new PointF(27, Height / 2f + 4),
                    new PointF(33, Height / 2f - 4),
                });
            }
            TextRenderer.DrawText(g, _message, UiTheme.FontBody, new Rectangle(48, 0, Width - 60, Height),
                UiTheme.TextPrimary, TextFormatFlags.Left | TextFormatFlags.VerticalCenter |
                TextFormatFlags.NoPrefix | TextFormatFlags.EndEllipsis);
        }
    }

    private static ToastForm? _current;

    public static void Show(Form? owner, string message, Color? accent = null)
    {
        try
        {
            _current?.Close();
            _current = null;

            var toast = new ToastForm(message, accent ?? UiTheme.Green);
            _current = toast;

            Rectangle bounds = owner != null && owner.Visible
                ? owner.Bounds
                : Screen.PrimaryScreen?.WorkingArea ?? new Rectangle(0, 0, 1280, 720);

            var target = new Point(bounds.Right - toast.Width - 24, bounds.Bottom - toast.Height - 24);
            toast.Location = new Point(target.X, target.Y + 18);
            toast.Opacity = 0;

            toast.Shown += async (s, e) =>
            {
                try
                {
                    await Anim.RunAsync(Ease.BackOut, 280, p =>
                    {
                        if (toast.IsDisposed) return;
                        toast.Opacity = Math.Clamp(p, 0, 1);
                        toast.Top = target.Y + (int)((1 - Math.Clamp(p, 0, 1)) * 18);
                    });
                    toast.Opacity = 1;
                    toast.Top = target.Y;
                    await Task.Delay(1700);
                    await Anim.RunAsync(Ease.InCubic, 220, p =>
                    {
                        if (toast.IsDisposed) return;
                        toast.Opacity = 1 - p;
                        toast.Top = target.Y + (int)(10 * p);
                    });
                }
                finally
                {
                    try { toast.Close(); } catch { }
                    if (ReferenceEquals(_current, toast)) _current = null;
                }
            };

            toast.Show(owner);
        }
        catch { }
    }
}

// ============================================================
// 对话框：缩放淡入（弹性入场） + 缩放消失
// ============================================================
internal static class DialogFx
{
    public static DialogResult Show(Form owner, Form dialog)
    {
        bool closing = false;
        dialog.StartPosition = FormStartPosition.CenterParent;
        dialog.Opacity = 0;

        dialog.Shown += async (s, e) =>
        {
            try
            {
                var target = dialog.Bounds;
                var small = Rectangle.Inflate(target, -(int)(target.Width * 0.03), -(int)(target.Height * 0.03));
                dialog.Bounds = small;
                await Anim.RunAsync(Ease.BackOut, 300, p =>
                {
                    if (dialog.IsDisposed) return;
                    double q = Math.Clamp(p, 0, 1);
                    dialog.Opacity = q;
                    int w = (int)(small.Width + (target.Width - small.Width) * p);
                    int h = (int)(small.Height + (target.Height - small.Height) * p);
                    dialog.SetBounds(target.X + (target.Width - w) / 2, target.Y + (target.Height - h) / 2, w, h);
                });
                dialog.Opacity = 1;
                dialog.Bounds = target;
            }
            catch { }
        };

        dialog.FormClosing += (s, e) =>
        {
            if (closing) return;
            e.Cancel = true;
            closing = true;
            _ = CloseAsync();
        };

        async Task CloseAsync()
        {
            try
            {
                var from = dialog.Bounds;
                await Anim.RunAsync(Ease.InCubic, 170, p =>
                {
                    if (dialog.IsDisposed) return;
                    dialog.Opacity = 1 - p;
                    int w = (int)(from.Width * (1 - 0.04 * p));
                    int h = (int)(from.Height * (1 - 0.04 * p));
                    dialog.SetBounds(from.X + (from.Width - w) / 2, from.Y + (from.Height - h) / 2, w, h);
                });
            }
            catch { }
            try { dialog.Close(); } catch { }
        }

        return dialog.ShowDialog(owner);
    }
}

// ============================================================
// 双缓冲面板
// ============================================================
internal sealed class BufferedPanel : Panel
{
    public BufferedPanel()
    {
        SetStyle(ControlStyles.OptimizedDoubleBuffer | ControlStyles.AllPaintingInWmPaint, true);
        DoubleBuffered = true;
    }
}
