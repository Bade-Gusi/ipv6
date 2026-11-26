// ============================================================
// Form1.Designer.cs — 平铺式 IPv6 管理工具 UI v2.1
// ============================================================

#nullable disable

using System.Windows.Forms;

namespace ipv6;

partial class Form1
{
    // ============================================================
    // 设计令牌
    // ============================================================
    static readonly Color BG_DARK = Color.FromArgb(13, 17, 23);
    static readonly Color BG_CARD = Color.FromArgb(28, 33, 40);
    static readonly Color BG_HOVER = Color.FromArgb(38, 44, 52);
    static readonly Color BG_ACTIVE = Color.FromArgb(48, 54, 61);
    static readonly Color ACCENT_BLUE = Color.FromArgb(88, 166, 255);
    static readonly Color ACCENT_GREEN = Color.FromArgb(63, 185, 80);
    static readonly Color ACCENT_RED = Color.FromArgb(248, 81, 73);
    static readonly Color ACCENT_ORANGE = Color.FromArgb(255, 165, 50);
    static readonly Color ACCENT_PURPLE = Color.FromArgb(170, 120, 255);
    static readonly Color ACCENT_CYAN = Color.FromArgb(85, 210, 210);
    static readonly Color ACCENT_PINK = Color.FromArgb(255, 100, 180);
    static readonly Color ACCENT_YELLOW = Color.FromArgb(230, 210, 80);
    static readonly Color TEXT_PRIMARY = Color.FromArgb(240, 246, 252);
    static readonly Color TEXT_SECONDARY = Color.FromArgb(139, 148, 158);
    static readonly Color TEXT_MUTED = Color.FromArgb(125, 135, 146);
    static readonly Color BORDER_COLOR = Color.FromArgb(48, 54, 61);
    static readonly Color CARD_BORDER = Color.FromArgb(38, 44, 52);
    static readonly Color MONO_BG = Color.FromArgb(10, 14, 20);
    static readonly Color MONO_FG = Color.FromArgb(180, 200, 220);

    const int TITLEBAR_HEIGHT = 44;
    const int CORNER_RADIUS = 10;
    const string GITHUB_REPO = "Bade-Gusi/ipv6";

    // 当前版本取自程序集（csproj 中 <Version>）
    internal static readonly string CURRENT_VERSION =
        System.Reflection.Assembly.GetExecutingAssembly().GetName().Version is { } v
            ? $"v{v.Major}.{v.Minor}.{v.Build}"
            : "v2.1.0";

    // ============================================================
    // 缓存的绘图资源（避免 OwnerDraw 每帧 new 造成 GDI 泄漏）
    // ============================================================
    static readonly SolidBrush BrRowBg = new(BG_CARD);
    static readonly SolidBrush BrRowSelected = new(BG_ACTIVE);
    static readonly SolidBrush BrHeaderBg = new(Color.FromArgb(22, 27, 34));
    static readonly Pen PenHeaderLine = new(Color.FromArgb(60, 88, 166, 255));
    static readonly Pen PenRowLine = new(Color.FromArgb(36, 255, 255, 255));
    static readonly Pen PenTitleLine = new(Color.FromArgb(40, 139, 148, 158));
    static readonly Pen PenCardTop = new(Color.FromArgb(30, 255, 255, 255));
    static readonly Dictionary<Color, Pen> AccentPens = new();

    static readonly Font FTitle = new("Segoe UI", 12F, FontStyle.Bold);
    static readonly Font FPageTitle = new("Segoe UI", 18F, FontStyle.Bold);
    static readonly Font FPageSub = new("Segoe UI", 9.5F);
    static readonly Font FBody = new("Segoe UI", 9.5F);
    static readonly Font FSmall = new("Segoe UI", 8.5F);
    static readonly Font FTiny = new("Segoe UI", 7.5F);
    static readonly Font FTinyBold = new("Segoe UI", 7.5F, FontStyle.Bold);
    static readonly Font FBold = new("Segoe UI", 10F, FontStyle.Bold);
    static readonly Font FIco = new("Segoe UI", 16F);
    static readonly Font FHeaderFont = new("Segoe UI", 9F, FontStyle.Bold);
    static readonly Font FMetricTitle = new("Segoe UI", 8.5F);
    static readonly Font FValue = new("Segoe UI", 26F, FontStyle.Bold);
    static readonly Font FWinBtn = new("Segoe UI", 11F);
    static readonly Font FMono = new("Consolas", 9.5F);
    static readonly Font FMonoSm = new("Consolas", 9F);
    static readonly Font FIconBig = new("Segoe UI", 14F);

    internal static Pen AccentPen(Color c)
    {
        if (!AccentPens.TryGetValue(c, out var p))
        {
            p = new Pen(c, 3);
            AccentPens[c] = p;
        }
        return p;
    }

    [System.Runtime.InteropServices.DllImport("gdi32.dll")]
    static extern nint CreateRoundRectRgn(int x1, int y1, int x2, int y2, int cx, int cy);

    [System.Runtime.InteropServices.DllImport("gdi32.dll")]
    static extern bool DeleteObject(nint hObject);

    internal static class NativeMethods
    {
        [System.Runtime.InteropServices.DllImport("user32.dll")]
        internal static extern int ReleaseCapture();

        [System.Runtime.InteropServices.DllImport("user32.dll")]
        internal static extern nint SendMessage(nint hWnd, int msg, int wParam, int lParam);
    }

    // ============================================================
    // 控件字段
    // ============================================================
    private System.ComponentModel.IContainer components = null;
    internal Panel titleBar; internal Label titleLabel;
    internal FlatButton btnMinimize; internal FlatButton btnMaximize; internal FlatButton btnClose; internal FlatButton btnUpdate;
    internal StatusDot connIndicator; internal StatusText connText; internal Label publicIPLabel;

    internal Label icon; internal Label ver;
    internal Panel contentPanel;
    internal AmbientTileHost tileContainer;
    internal Panel[] detailPages;
    internal Panel currentDetail;
    internal int activeDetailIndex = -1;
    internal PageHeaderView[] pageHeaders = new PageHeaderView[PAGE_COUNT];
    internal LoadingView loadingView;

    static readonly Color[] PageAccents =
    {
        ACCENT_BLUE, ACCENT_GREEN, ACCENT_CYAN, ACCENT_PURPLE, ACCENT_PINK,
        ACCENT_YELLOW, ACCENT_ORANGE, ACCENT_RED, ACCENT_CYAN, ACCENT_PURPLE,
        ACCENT_GREEN, ACCENT_GREEN, ACCENT_BLUE,
    };

    internal FlowLayoutPanel dashCardContainer; internal ListBox dashEventLog;
    internal Panel myIPv6Panel; internal FlowLayoutPanel myIPv6Container;
    internal FlatButton btnMonitor;

    internal ListView adapterListView; internal Label adapterStatusLabel;
    internal TreeView addressTreeView;
    internal ListView dnsListView; internal TextBox dnsDomainBox; internal TextBox dnsResultBox;
    internal ComboBox connTargetCombo; internal TextBox connResultBox;
    internal ListView routeListView; internal ListView tunnelListView;
    internal ListView firewallListView; internal ComboBox firewallDirectionCombo; internal Label firewallHintLabel;
    internal ListView scanNeighborList;
    internal ListView listenerListView; internal Label listenerStatusLabel;
    internal Label healthScoreLabel; internal FlowLayoutPanel healthFlow;
    internal TextBox subnetInputBox;
    internal TextBox reportTextBox; internal FlatButton btnReportCopy; internal FlatButton btnReportSave;
    private Panel loadingOverlay;

    internal Label _cmStatusLbl; internal Label _cmDetailLbl; internal Label _cmCountdownLbl; internal ProgressBar _cmProgress;
    internal NotifyIcon trayIcon; internal ContextMenuStrip trayMenu; internal ToolStripMenuItem trayMonitorItem;
    internal ContextMenuStrip dashLogMenu; internal ToolTip tips;

    internal const int PAGE_COUNT = 13;

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            try { if (trayIcon != null) { trayIcon.Visible = false; trayIcon.Dispose(); } } catch { }
            try { trayMenu?.Dispose(); } catch { }
            try { dashLogMenu?.Dispose(); } catch { }
            try { tips?.Dispose(); } catch { }
            components?.Dispose();
        }
        base.Dispose(disposing);
    }

    // ============================================================
    // InitializeComponent — 设计器兼容骨架
    // ============================================================
    private void InitializeComponent()
    {
        titleBar = new Panel();
        icon = new Label();
        titleLabel = new Label();
        ver = new Label();
        connIndicator = new StatusDot();
        connText = new StatusText();
        publicIPLabel = new Label();
        btnUpdate = new FlatButton();
        contentPanel = new BufferedPanel();
        loadingOverlay = new BufferedPanel();
        titleBar.SuspendLayout();
        SuspendLayout();
        //
        // titleBar
        //
        titleBar.Controls.Add(icon);
        titleBar.Controls.Add(titleLabel);
        titleBar.Controls.Add(ver);
        titleBar.Controls.Add(connIndicator);
        titleBar.Controls.Add(connText);
        titleBar.Controls.Add(publicIPLabel);
        titleBar.Controls.Add(btnUpdate);
        titleBar.Dock = DockStyle.Top;
        titleBar.Location = new Point(0, 0);
        titleBar.Name = "titleBar";
        titleBar.Size = new Size(1280, 100);
        titleBar.TabIndex = 0;
        //
        // icon
        //
        icon.Location = new Point(0, 0);
        icon.Name = "icon";
        icon.Size = new Size(100, 23);
        icon.TabIndex = 0;
        //
        // titleLabel
        //
        titleLabel.Location = new Point(0, 0);
        titleLabel.Name = "titleLabel";
        titleLabel.Size = new Size(100, 23);
        titleLabel.TabIndex = 1;
        //
        // ver
        //
        ver.Location = new Point(0, 0);
        ver.Name = "ver";
        ver.Size = new Size(100, 23);
        ver.TabIndex = 2;
        //
        // connIndicator
        //
        connIndicator.Location = new Point(0, 0);
        connIndicator.Name = "connIndicator";
        connIndicator.Size = new Size(22, 22);
        connIndicator.TabIndex = 3;
        //
        // connText
        //
        connText.Location = new Point(0, 0);
        connText.Name = "connText";
        connText.Size = new Size(100, 20);
        connText.TabIndex = 4;
        //
        // publicIPLabel
        //
        publicIPLabel.Location = new Point(0, 0);
        publicIPLabel.Name = "publicIPLabel";
        publicIPLabel.Size = new Size(100, 23);
        publicIPLabel.TabIndex = 5;
        //
        // btnUpdate
        //
        btnUpdate.Location = new Point(0, 0);
        btnUpdate.Name = "btnUpdate";
        btnUpdate.Size = new Size(75, 23);
        btnUpdate.TabIndex = 6;
        btnUpdate.Click += BtnUpdate_Click;
        //
        // contentPanel
        //
        contentPanel.Dock = DockStyle.Fill;
        contentPanel.Location = new Point(0, 100);
        contentPanel.Name = "contentPanel";
        contentPanel.Padding = new Padding(16, 12, 16, 16);
        contentPanel.Size = new Size(1280, 720);
        contentPanel.TabIndex = 1;
        //
        // loadingOverlay
        //
        loadingOverlay.Dock = DockStyle.Fill;
        loadingOverlay.Location = new Point(0, 0);
        loadingOverlay.Name = "loadingOverlay";
        loadingOverlay.BackColor = BG_DARK;
        loadingOverlay.Size = new Size(200, 100);
        loadingOverlay.TabIndex = 0;
        loadingOverlay.Visible = false;
        //
        // Form1
        //
        ClientSize = new Size(1280, 820);
        Controls.Add(contentPanel);
        Controls.Add(titleBar);
        DoubleBuffered = true;
        Font = new Font("Segoe UI", 9.75F);
        FormBorderStyle = FormBorderStyle.None;
        MinimumSize = new Size(1000, 680);
        Name = "Form1";
        StartPosition = FormStartPosition.CenterScreen;
        Text = "IPv6 管理工具 " + CURRENT_VERSION;
        titleBar.ResumeLayout(false);
        ResumeLayout(false);
    }

    private static FlatButton MakeWinBtn(string text, int x, Color? hoverBack = null, Color? hoverFore = null)
    {
        return new FlatButton
        {
            Text = text,
            Location = new Point(x, 0),
            Size = new Size(46, TITLEBAR_HEIGHT),
            NormalBack = Color.Transparent,
            HoverBack = hoverBack ?? Color.FromArgb(180, BG_HOVER),
            NormalFore = TEXT_SECONDARY,
            HoverFore = hoverFore ?? TEXT_PRIMARY,
            Font = FWinBtn,
            Radius = 0,
            Anchor = AnchorStyles.Top | AnchorStyles.Right,
            TabStop = false,
        };
    }

    // ============================================================
    // 运行时完整 UI 构建
    // ============================================================
    internal void BuildCompleteUI()
    {
        tips = new ToolTip { AutoPopDelay = 6000, InitialDelay = 400, ReshowDelay = 200 };
        BuildWinButtons();
        ApplyTheme();
        BuildTiles();
        BuildDetailPages();
        BuildLoadingOverlay();
        BuildSystemTray();
        BuildLogMenu();
        contentPanel.Controls.Add(loadingOverlay);
        SwitchToTileView();
        try { ActiveControl = null; } catch { }
    }

    private void BuildWinButtons()
    {
        btnMinimize = MakeWinBtn("─", 1134);
        btnMaximize = MakeWinBtn("□", 1180);
        btnClose = MakeWinBtn("✕", 1226, Color.FromArgb(196, 43, 28), Color.White);
        btnClose.Width = 54;

        btnMinimize.Click += BtnMinimize_Click;
        btnMaximize.Click += BtnMaximize_Click;
        btnClose.Click += BtnClose_Click;

        titleBar.Controls.Add(btnMinimize);
        titleBar.Controls.Add(btnMaximize);
        titleBar.Controls.Add(btnClose);
    }

    // ============================================================
    // 应用主题
    // ============================================================
    private void ApplyTheme()
    {
        BackColor = BG_DARK;
        ForeColor = TEXT_PRIMARY;

        titleBar.BackColor = Color.FromArgb(215, 22, 27, 34);
        titleBar.Height = TITLEBAR_HEIGHT;
        titleBar.Paint += (s, e) => e.Graphics.DrawLine(PenTitleLine, 0, titleBar.Height - 1, titleBar.Width, titleBar.Height - 1);

        MouseEventHandler dragHandler = (s, e) =>
        {
            if (e.Button == MouseButtons.Left)
            {
                NativeMethods.ReleaseCapture();
                _ = NativeMethods.SendMessage(Handle, 0xA1, 2, 0);
            }
        };
        titleBar.MouseDown += dragHandler;
        foreach (Control c in new Control[] { icon, titleLabel, ver, connIndicator, connText, publicIPLabel })
            c.MouseDown += dragHandler;
        titleBar.DoubleClick += (s, e) => ToggleMaximize();

        titleLabel.ForeColor = TEXT_PRIMARY;
        titleLabel.Text = "  IPv6 管理工具";
        titleLabel.Font = FTitle;
        titleLabel.Location = new Point(42, 11);
        titleLabel.AutoSize = true;

        icon.Text = "\U0001f310";
        icon.Location = new Point(14, 8);
        icon.AutoSize = true;
        icon.Font = FIconBig;

        ver.Text = CURRENT_VERSION;
        ver.Location = new Point(185, 14);
        ver.AutoSize = true;
        ver.Font = FSmall;
        ver.ForeColor = TEXT_MUTED;

        connIndicator.Connected = null;
        connIndicator.Location = new Point(238, 11);
        tips?.SetToolTip(connIndicator, "IPv6 连接状态");

        connText.Location = new Point(262, 13);
        connText.Font = FSmall;
        connText.SetStatus("检测中...", TEXT_SECONDARY, animate: false);

        publicIPLabel.Location = new Point(400, 14);
        publicIPLabel.AutoSize = true;
        publicIPLabel.Font = FSmall;
        publicIPLabel.ForeColor = TEXT_MUTED;

        btnUpdate.Text = "\u27f3";
        btnUpdate.Font = FWinBtn;
        btnUpdate.NormalBack = Color.Transparent;
        btnUpdate.HoverBack = Color.FromArgb(60, 88, 166, 255);
        btnUpdate.NormalFore = TEXT_MUTED;
        btnUpdate.HoverFore = TEXT_PRIMARY;
        btnUpdate.Radius = 0;
        btnUpdate.Cursor = Cursors.Hand;
        btnUpdate.Size = new Size(32, TITLEBAR_HEIGHT);
        btnUpdate.Location = new Point(1098, 0);
        btnUpdate.Anchor = AnchorStyles.Top | AnchorStyles.Right;
        btnUpdate.TabStop = false;
        tips?.SetToolTip(btnUpdate, "检查更新");

        contentPanel.BackColor = BG_DARK;
    }

    // ============================================================
    // 瓷砖仪表盘
    // ============================================================
    private void BuildTiles()
    {
        tileContainer = new AmbientTileHost
        {
            Dock = DockStyle.Fill,
            FlowDirection = FlowDirection.LeftToRight,
            WrapContents = true,
            Padding = new Padding(0, 0, 0, 8),
            AutoScroll = true,
        };

        TileDef[] tiles =
        {
            new TileDef("我的 IPv6", "查看本机所有 IPv6 地址", "\U0001f310", ACCENT_BLUE, 0),
            new TileDef("网络适配器", "管理适配器 IPv6 绑定", "\U0001f50c", ACCENT_GREEN, 1),
            new TileDef("IPv6 地址", "查看地址分配详情", "\U0001f4e1", ACCENT_CYAN, 2),
            new TileDef("DNS 设置", "IPv6 DNS 配置与解析测试", "\U0001f4cb", ACCENT_PURPLE, 3),
            new TileDef("连接测试", "Ping / Traceroute", "\U0001f4f6", ACCENT_PINK, 4),
            new TileDef("路由表", "IPv6 路由表查看", "\U0001f5fa\ufe0f", ACCENT_YELLOW, 5),
            new TileDef("隧道状态", "Teredo / 6to4 / ISATAP", "\U0001f517", ACCENT_ORANGE, 6),
            new TileDef("防火墙规则", "IPv6 防火墙规则列表", "\U0001f525", ACCENT_RED, 7),
            new TileDef("网络扫描", "邻居发现 / 活动连接", "\U0001f50d", ACCENT_CYAN, 8),
            new TileDef("高级工具", "诊断 / 维护 / 子网计算", "\u26a1", ACCENT_PURPLE, 9),
            new TileDef("配置报告", "生成并导出 IPv6 报告", "\U0001f4c4", ACCENT_GREEN, 10),
            new TileDef("一键体检", "自动诊断并给出修复建议", "\u2705", ACCENT_GREEN, 11),
            new TileDef("监听端口", "本机 IPv6 服务监听状态", "\U0001f5a5\ufe0f", ACCENT_BLUE, 12),
        };

        foreach (TileDef t in tiles)
        {
            Color tileColor = t.Color;
            int pageIdx = t.PageIndex;

            var card = new AnimatedTile
            {
                TileTitle = t.Title,
                TileDesc = t.Desc,
                TileIcon = t.Icon,
                Accent = tileColor,
                Activate = () => SwitchToDetail(pageIdx),
            };

            tips?.SetToolTip(card, t.Desc);
            tileContainer.Controls.Add(card);
        }

        contentPanel.Controls.Add(tileContainer);
    }

    private struct TileDef
    {
        public string Title;
        public string Desc;
        public string Icon;
        public Color Color;
        public int PageIndex;
        public TileDef(string t, string d, string i, Color c, int p) { Title = t; Desc = d; Icon = i; Color = c; PageIndex = p; }
    }

    // ============================================================
    // 详情页面
    // ============================================================
    private void BuildDetailPages()
    {
        detailPages = new Panel[PAGE_COUNT];
        for (int i = 0; i < PAGE_COUNT; i++)
        {
            detailPages[i] = new BufferedPanel { BackColor = BG_DARK, Padding = new Padding(0), Visible = false };

            Panel headerBar = new Panel { Dock = DockStyle.Top, Height = 44, BackColor = Color.Transparent, Padding = new Padding(0, 5, 0, 0) };
            Button bb = new Button
            {
                Text = "\u2190  返回仪表盘",
                FlatStyle = FlatStyle.Flat,
                Size = new Size(150, 30),
                Location = new Point(0, 7),
                BackColor = Color.Transparent,
                ForeColor = ACCENT_BLUE,
                Font = FBold,
                Cursor = Cursors.Hand,
                TextAlign = ContentAlignment.MiddleLeft,
            };
            bb.FlatAppearance.BorderSize = 0;
            bb.FlatAppearance.MouseOverBackColor = Color.FromArgb(40, 88, 166, 255);
            bb.Click += (s, e) => SwitchToTileView();
            headerBar.Controls.Add(bb);

            // 先添加 Fill，再添加 Top：Fill 的布局区域才会排除顶部栏
            detailPages[i].Controls.Add(new Panel { Dock = DockStyle.Fill, BackColor = Color.Transparent, Name = "ContentArea" });
            detailPages[i].Controls.Add(headerBar);
        }

        BuildDashboardDetail();
        BuildAdaptersDetail();
        BuildAddressesDetail();
        BuildDnsDetail();
        BuildConnTestDetail();
        BuildRoutesDetail();
        BuildTunnelsDetail();
        BuildFirewallDetail();
        BuildScanDetail();
        BuildToolsDetail();
        BuildReportDetail();
        BuildHealthDetail();
        BuildListenersDetail();
    }

    private Panel GetContentArea(int pageIndex)
    {
        foreach (Control c in detailPages[pageIndex].Controls)
            if (c is Panel p && p.Name == "ContentArea") return p;
        return null;
    }

    private void AddPageHeader(Panel parent, int pageIndex, string title, string subtitle)
    {
        var header = new PageHeaderView(title, subtitle, PageAccents[pageIndex]);
        parent.Controls.Add(header);
        pageHeaders[pageIndex] = header;
    }

    private FlatButton MakeTitleBtn(string text, Color bgColor, EventHandler click)
    {
        var b = new FlatButton
        {
            Text = text,
            NormalBack = bgColor,
            HoverBack = ControlPaint.Light(bgColor),
            NormalFore = Color.White,
            HoverFore = Color.White,
            GlowColor = ControlPaint.Light(bgColor),
            Font = FBold,
            Radius = 6f,
        };
        b.Click += click;
        return b;
    }

    private ListView MakeLV(string[] cols, int[] widths)
    {
        ListView lv = new ListView
        {
            Dock = DockStyle.Fill,
            View = View.Details,
            FullRowSelect = true,
            BorderStyle = BorderStyle.None,
            BackColor = BG_CARD,
            ForeColor = TEXT_PRIMARY,
            Font = FBody,
            HeaderStyle = ColumnHeaderStyle.Nonclickable,
            OwnerDraw = true,
            HideSelection = false,
        };

        lv.DrawColumnHeader += (s, e) =>
        {
            e.Graphics.FillRectangle(BrHeaderBg, e.Bounds);
            e.Graphics.DrawLine(PenHeaderLine, e.Bounds.Left, e.Bounds.Bottom - 1, e.Bounds.Right, e.Bounds.Bottom - 1);
            TextRenderer.DrawText(e.Graphics, e.Header?.Text ?? "", FHeaderFont,
                new Point(e.Bounds.X + 8, e.Bounds.Y + 6), TEXT_SECONDARY, TextFormatFlags.Left);
        };
        lv.DrawSubItem += (s, e) =>
        {
            if (e.Item == null) return;
            e.Graphics.FillRectangle(e.Item.Selected ? BrRowSelected : BrRowBg, e.Bounds);
            e.Graphics.DrawLine(PenRowLine, e.Bounds.Left, e.Bounds.Top, e.Bounds.Right, e.Bounds.Top);
            TextRenderer.DrawText(e.Graphics, e.SubItem?.Text ?? "", FBody,
                new Rectangle(e.Bounds.X + 6, e.Bounds.Y, e.Bounds.Width - 8, e.Bounds.Height),
                TEXT_PRIMARY, TextFormatFlags.Left | TextFormatFlags.VerticalCenter | TextFormatFlags.EndEllipsis);
        };

        for (int i = 0; i < cols.Length; i++) lv.Columns.Add(cols[i], widths[i]);

        // 让最后一列吸收剩余宽度：既避免表头右侧留白，也不会多出横向滚动条
        int lastMin = widths[widths.Length - 1];
        bool fitting = false;
        void FitLastColumn()
        {
            if (fitting || lv.Columns.Count == 0) return;
            fitting = true;
            try
            {
                int used = 0;
                for (int i = 0; i < lv.Columns.Count - 1; i++) used += lv.Columns[i].Width;
                int width = lv.ClientSize.Width - used - 2;
                lv.Columns[lv.Columns.Count - 1].Width = Math.Max(lastMin, width);
            }
            finally { fitting = false; }
        }
        lv.ColumnWidthChanged += (s, e) => FitLastColumn();
        lv.ClientSizeChanged += (s, e) => FitLastColumn();
        lv.Tag = (Action)FitLastColumn;

        return lv;
    }

    internal void ApplyRoundedCorners()
    {
        try
        {
            if (WindowState == FormWindowState.Maximized)
            {
                var old = Region;
                Region = null;
                old?.Dispose();
                return;
            }
            nint hrgn = CreateRoundRectRgn(0, 0, Width + 1, Height + 1, CORNER_RADIUS, CORNER_RADIUS);
            if (hrgn == 0) return;
            var region = Region.FromHrgn(hrgn);
            DeleteObject(hrgn);
            var previous = Region;
            Region = region;
            previous?.Dispose();
        }
        catch { }
    }

    // ============================================================
    // 仪表盘详情
    // ============================================================
    private void BuildDashboardDetail()
    {
        Panel p = GetContentArea(0);

        Panel ab = new Panel { Dock = DockStyle.Top, Height = 44, BackColor = Color.Transparent, Padding = new Padding(0, 6, 0, 0) };
        FlatButton b1 = MakeTitleBtn("\U0001f504 刷新所有", ACCENT_BLUE, (s, e) => _ = RefreshAndReloadAsync(0)); b1.SetBounds(0, 0, 130, 32);
        FlatButton b2 = MakeTitleBtn("\u2705 启用全部", ACCENT_GREEN, (s, e) => _ = EnableAllAsync()); b2.SetBounds(140, 0, 130, 32);
        FlatButton b3 = MakeTitleBtn("\u274c 禁用全部", ACCENT_RED, (s, e) => _ = DisableAllAsync()); b3.SetBounds(280, 0, 130, 32);
        FlatButton b4 = MakeTitleBtn("\u26a1 开机检测", ACCENT_ORANGE, (s, e) => ToggleAutoStart()); b4.SetBounds(420, 0, 130, 32);
        btnMonitor = MakeTitleBtn("\U0001f4c8 开始监控", ACCENT_PURPLE, (s, e) => ToggleMonitor()); btnMonitor.SetBounds(560, 0, 130, 32);
        ab.Controls.Add(b1); ab.Controls.Add(b2); ab.Controls.Add(b3); ab.Controls.Add(b4); ab.Controls.Add(btnMonitor);

        myIPv6Panel = new Panel { Dock = DockStyle.Top, Height = 176, BackColor = Color.Transparent };
        Label mh = new Label { Text = "    我的 IPv6 地址（点击卡片复制）", Dock = DockStyle.Top, Height = 26, Font = FBold, ForeColor = ACCENT_CYAN };
        myIPv6Container = new FlowLayoutPanel { Dock = DockStyle.Fill, BackColor = Color.Transparent, FlowDirection = FlowDirection.LeftToRight, WrapContents = true, Padding = new Padding(8, 0, 0, 0), AutoScroll = true };
        // Fill 先添加，Title 后添加：否则 Fill 会铺满整个面板并压到标题下方
        myIPv6Panel.Controls.Add(myIPv6Container);
        myIPv6Panel.Controls.Add(mh);

        Panel mp = new Panel { Dock = DockStyle.Top, Height = 90, BackColor = Color.Transparent };
        dashCardContainer = new FlowLayoutPanel { Dock = DockStyle.Fill, BackColor = Color.Transparent, FlowDirection = FlowDirection.LeftToRight, WrapContents = false, Padding = new Padding(0, 6, 0, 0) };
        mp.Controls.Add(dashCardContainer);

        Label lh = new Label { Text = "    系统日志（右键可复制 / 导出）", Dock = DockStyle.Top, Height = 24, Font = FBold, ForeColor = TEXT_PRIMARY };
        dashEventLog = new ListBox { Dock = DockStyle.Fill, BackColor = BG_CARD, ForeColor = TEXT_SECONDARY, Font = FMonoSm, BorderStyle = BorderStyle.None, IntegralHeight = false };

        // 先添加 Fill，再按“视觉顺序的逆序”添加 Dock=Top 控件
        p.Controls.Add(dashEventLog);
        p.Controls.Add(lh);
        p.Controls.Add(mp);
        p.Controls.Add(myIPv6Panel);
        p.Controls.Add(ab);
        AddPageHeader(p, 0, "仪表盘", "IPv6 网络状态概览");
    }

    // ============================================================
    // 网络适配器
    // ============================================================
    private void BuildAdaptersDetail()
    {
        Panel p = GetContentArea(1);
        Panel ab = new Panel { Dock = DockStyle.Top, Height = 44, BackColor = Color.Transparent, Padding = new Padding(0, 6, 0, 0) };
        FlatButton r = MakeTitleBtn("\U0001f504 刷新", ACCENT_BLUE, (s, e) => _ = RefreshAndReloadAsync(1)); r.SetBounds(0, 0, 110, 32);
        FlatButton e_ = MakeTitleBtn("\u2705 启用选中", ACCENT_GREEN, (s, e) => _ = ToggleSelectedAdapterAsync(true)); e_.SetBounds(120, 0, 130, 32);
        FlatButton d = MakeTitleBtn("\u274c 禁用选中", ACCENT_RED, (s, e) => _ = ToggleSelectedAdapterAsync(false)); d.SetBounds(260, 0, 130, 32);
        adapterStatusLabel = new Label { Text = "", Location = new Point(404, 8), AutoSize = true, Font = FSmall, ForeColor = TEXT_SECONDARY };
        ab.Controls.Add(r); ab.Controls.Add(e_); ab.Controls.Add(d); ab.Controls.Add(adapterStatusLabel);
        adapterListView = MakeLV(new[] { "名称", "IPv6 状态", "接口", "连接", "速率", "描述" }, new[] { 190, 90, 60, 60, 90, 300 });
        tips?.SetToolTip(adapterListView, "选中一行后可启用/禁用其 IPv6 绑定");
        p.Controls.Add(adapterListView);
        p.Controls.Add(ab);
        AddPageHeader(p, 1, "网络适配器", "查看和管理每个适配器的 IPv6 状态");
    }

    // ============================================================
    // IPv6 地址
    // ============================================================
    private void BuildAddressesDetail()
    {
        Panel p = GetContentArea(2);
        Panel ab = new Panel { Dock = DockStyle.Top, Height = 44, BackColor = Color.Transparent, Padding = new Padding(0, 6, 0, 0) };
        FlatButton r = MakeTitleBtn("\U0001f504 刷新", ACCENT_BLUE, (s, e) => _ = RefreshAndReloadAsync(2)); r.SetBounds(0, 0, 110, 32);
        ab.Controls.Add(r);
        addressTreeView = new TreeView
        {
            Dock = DockStyle.Fill,
            BackColor = BG_CARD,
            ForeColor = TEXT_PRIMARY,
            Font = FMono,
            BorderStyle = BorderStyle.None,
            ShowLines = false,
            FullRowSelect = true,
            ItemHeight = 24,
        };
        addressTreeView.DoubleClick += (s, e) => { if (addressTreeView.SelectedNode?.Tag is string ip) TryCopy(ip); };
        tips?.SetToolTip(addressTreeView, "双击地址节点复制");
        p.Controls.Add(addressTreeView);
        p.Controls.Add(ab);
        AddPageHeader(p, 2, "IPv6 地址", "查看所有网络适配器的 IPv6 地址分配");
    }

    // ============================================================
    // DNS 设置（含解析 / 测速）
    // ============================================================
    private void BuildDnsDetail()
    {
        Panel p = GetContentArea(3);

        Panel ab = new Panel { Dock = DockStyle.Top, Height = 44, BackColor = Color.Transparent, Padding = new Padding(0, 6, 0, 0) };
        ab.Controls.Add(new Label { Text = "域名:", Location = new Point(0, 9), AutoSize = true, Font = FBody, ForeColor = TEXT_SECONDARY });
        dnsDomainBox = new TextBox { Location = new Point(44, 4), Width = 240, BackColor = BG_CARD, ForeColor = TEXT_PRIMARY, Font = FBody, BorderStyle = BorderStyle.FixedSingle, Text = "www.qq.com" };
        FlatButton rb = MakeTitleBtn("\U0001f50d 解析 AAAA", ACCENT_BLUE, (s, e) => _ = ResolveDnsAsync()); rb.SetBounds(294, 0, 130, 32);
        FlatButton tb = MakeTitleBtn("\u23f1 DNS 测速", ACCENT_PURPLE, (s, e) => _ = TestDnsAsync()); tb.SetBounds(434, 0, 120, 32);
        ab.Controls.Add(dnsDomainBox);
        ab.Controls.Add(rb);
        ab.Controls.Add(tb);
        tips?.SetToolTip(dnsDomainBox, "输入域名后点击右侧按钮");

        dnsResultBox = new TextBox
        {
            Dock = DockStyle.Bottom,
            Height = 170,
            Multiline = true,
            ReadOnly = true,
            ScrollBars = ScrollBars.Both,
            BackColor = MONO_BG,
            ForeColor = MONO_FG,
            Font = FMonoSm,
            BorderStyle = BorderStyle.None,
            WordWrap = false,
            Text = "解析/测速结果将显示在这里",
        };
        dnsListView = MakeLV(new[] { "适配器名称", "DNS 服务器 (IPv6)", "配置方式" }, new[] { 240, 380, 140 });

        p.Controls.Add(dnsListView);
        p.Controls.Add(dnsResultBox);
        p.Controls.Add(ab);
        AddPageHeader(p, 3, "DNS 设置", "查看 IPv6 DNS 服务器，并进行 AAAA 解析与测速");
    }


    // ============================================================
    // 连接测试
    // ============================================================
    private void BuildConnTestDetail()
    {
        Panel p = GetContentArea(4);
        Panel ab = new Panel { Dock = DockStyle.Top, Height = 44, BackColor = Color.Transparent, Padding = new Padding(0, 6, 0, 0) };
        ab.Controls.Add(new Label { Text = "目标:", Location = new Point(0, 9), AutoSize = true, Font = FBody, ForeColor = TEXT_SECONDARY });
        connTargetCombo = new ComboBox { Location = new Point(40, 4), Width = 300, BackColor = BG_CARD, ForeColor = TEXT_PRIMARY, FlatStyle = FlatStyle.Flat, Font = FBody };
        connTargetCombo.Items.AddRange(new object[] { "ipv6.google.com", "2400:3200::1", "2402:4e00::", "2001:4860:4860::8888", "2606:4700:4700::1111", "localhost", "::1" });
        connTargetCombo.SelectedIndex = 0;
        FlatButton pb = MakeTitleBtn("\U0001f4f6 Ping", ACCENT_BLUE, (s, e) => _ = RunPingTestAsync()); pb.SetBounds(352, 0, 100, 32);
        FlatButton tb = MakeTitleBtn("\U0001f5fa\ufe0f Traceroute", ACCENT_PURPLE, (s, e) => _ = RunTracertAsync()); tb.SetBounds(462, 0, 130, 32);
        ab.Controls.Add(connTargetCombo); ab.Controls.Add(pb); ab.Controls.Add(tb);
        connResultBox = new TextBox
        {
            Dock = DockStyle.Fill,
            Multiline = true,
            ReadOnly = true,
            ScrollBars = ScrollBars.Both,
            BackColor = MONO_BG,
            ForeColor = MONO_FG,
            Font = FMono,
            BorderStyle = BorderStyle.None,
            WordWrap = false,
        };
        p.Controls.Add(connResultBox);
        p.Controls.Add(ab);
        AddPageHeader(p, 4, "连接测试", "IPv6 连通性诊断工具");
    }

    // ============================================================
    // 路由表
    // ============================================================
    private void BuildRoutesDetail()
    {
        Panel p = GetContentArea(5);
        Panel ab = new Panel { Dock = DockStyle.Top, Height = 44, BackColor = Color.Transparent, Padding = new Padding(0, 6, 0, 0) };
        FlatButton r = MakeTitleBtn("\U0001f504 刷新", ACCENT_BLUE, (s, e) => _ = RefreshAndReloadAsync(5)); r.SetBounds(0, 0, 110, 32);
        ab.Controls.Add(r);
        routeListView = MakeLV(new[] { "目标前缀", "接口别名", "跃点数", "下一跳" }, new[] { 280, 220, 80, 340 });
        p.Controls.Add(routeListView);
        p.Controls.Add(ab);
        AddPageHeader(p, 5, "路由表", "IPv6 路由表查看");
    }

    // ============================================================
    // 隧道状态
    // ============================================================
    private void BuildTunnelsDetail()
    {
        Panel p = GetContentArea(6);
        Panel ab = new Panel { Dock = DockStyle.Top, Height = 44, BackColor = Color.Transparent, Padding = new Padding(0, 6, 0, 0) };
        FlatButton r = MakeTitleBtn("\U0001f504 刷新", ACCENT_BLUE, (s, e) => _ = RefreshAndReloadAsync(6)); r.SetBounds(0, 0, 110, 32);
        ab.Controls.Add(r);
        tunnelListView = MakeLV(new[] { "隧道类型", "配置状态", "运行状态", "服务器 / 中继" }, new[] { 120, 120, 120, 400 });
        p.Controls.Add(tunnelListView);
        p.Controls.Add(ab);
        AddPageHeader(p, 6, "隧道状态", "Teredo / 6to4 / ISATAP 隧道配置（结构化读取，不依赖系统语言）");
    }

    // ============================================================
    // 防火墙规则
    // ============================================================
    private void BuildFirewallDetail()
    {
        Panel p = GetContentArea(7);
        Panel ab = new Panel { Dock = DockStyle.Top, Height = 44, BackColor = Color.Transparent, Padding = new Padding(0, 6, 0, 0) };
        ab.Controls.Add(new Label { Text = "方向:", Location = new Point(0, 9), AutoSize = true, Font = FBody, ForeColor = TEXT_SECONDARY });
        firewallDirectionCombo = new ComboBox { Location = new Point(42, 4), Width = 110, DropDownStyle = ComboBoxStyle.DropDownList, BackColor = BG_CARD, ForeColor = TEXT_PRIMARY, FlatStyle = FlatStyle.Flat, Font = FBody };
        firewallDirectionCombo.Items.AddRange(new object[] { "全部", "入站", "出站" });
        firewallDirectionCombo.SelectedIndex = 0;
        firewallDirectionCombo.SelectedIndexChanged += (s, e) => ApplyFirewallFilter();
        FlatButton r = MakeTitleBtn("\U0001f504 刷新", ACCENT_BLUE, (s, e) => _ = RefreshAndReloadAsync(7)); r.SetBounds(164, 0, 100, 32);
        FlatButton d = MakeTitleBtn("\U0001f510 管理员查询详情", ACCENT_ORANGE, (s, e) => _ = LoadFirewallDetailsAsync()); d.SetBounds(274, 0, 170, 32);
        firewallHintLabel = new Label { Text = "协议/端口/地址需管理员权限", Location = new Point(456, 9), AutoSize = true, Font = FSmall, ForeColor = TEXT_MUTED };
        ab.Controls.Add(firewallDirectionCombo); ab.Controls.Add(r); ab.Controls.Add(d); ab.Controls.Add(firewallHintLabel);
        firewallListView = MakeLV(new[] { "规则名称", "状态", "方向", "操作", "协议", "本地端口", "远程端口", "远程地址" }, new[] { 240, 60, 70, 60, 60, 90, 90, 220 });
        p.Controls.Add(firewallListView);
        p.Controls.Add(ab);
        AddPageHeader(p, 7, "防火墙规则", "IPv6 相关的 Windows Defender 防火墙规则");
    }

    // ============================================================
    // 网络扫描
    // ============================================================
    private void BuildScanDetail()
    {
        Panel p = GetContentArea(8);
        Panel ab = new Panel { Dock = DockStyle.Top, Height = 50, BackColor = Color.Transparent, Padding = new Padding(0, 6, 0, 0) };
        FlatButton b1 = MakeTitleBtn("\U0001f50d 扫描邻居", ACCENT_BLUE, (s, e) => _ = RefreshAndReloadAsync(8)); b1.SetBounds(0, 0, 140, 32);
        FlatButton b2 = MakeTitleBtn("\U0001f4e1 多播 Ping", ACCENT_PURPLE, (s, e) => _ = ScanMulticastAsync()); b2.SetBounds(150, 0, 130, 32);
        FlatButton b3 = MakeTitleBtn("\U0001f517 活动连接", ACCENT_CYAN, (s, e) => _ = ScanConnectionsAsync()); b3.SetBounds(290, 0, 130, 32);
        ab.Controls.Add(b1); ab.Controls.Add(b2); ab.Controls.Add(b3);
        var listTitle = new Label { Text = "    发现的 IPv6 邻居设备（NDP 缓存）", Dock = DockStyle.Top, Height = 28, Font = FBold, ForeColor = TEXT_PRIMARY };
        scanNeighborList = MakeLV(new[] { "IP 地址", "MAC 地址", "接口", "状态" }, new[] { 330, 180, 220, 100 });

        p.Controls.Add(scanNeighborList);
        p.Controls.Add(listTitle);
        p.Controls.Add(ab);
        AddPageHeader(p, 8, "网络扫描", "扫描本地网络中的 IPv6 设备与活动连接");
    }

    // ============================================================
    // 监听端口（新）
    // ============================================================
    private void BuildListenersDetail()
    {
        Panel p = GetContentArea(12);
        Panel ab = new Panel { Dock = DockStyle.Top, Height = 50, BackColor = Color.Transparent, Padding = new Padding(0, 6, 0, 0) };
        FlatButton r = MakeTitleBtn("\U0001f504 刷新", ACCENT_BLUE, (s, e) => _ = PopulateListenersAsync()); r.SetBounds(0, 0, 110, 32);
        listenerStatusLabel = new Label { Text = "", Location = new Point(124, 9), AutoSize = true, Font = FSmall, ForeColor = TEXT_SECONDARY };
        ab.Controls.Add(r);
        ab.Controls.Add(listenerStatusLabel);
        ab.Controls.Add(new Label { Text = "（双击复制地址）", Location = new Point(480, 9), AutoSize = true, Font = FSmall, ForeColor = TEXT_MUTED });
        listenerListView = MakeLV(new[] { "协议", "本地地址", "端口", "进程", "PID" }, new[] { 70, 320, 90, 220, 80 });
        listenerListView.DoubleClick += (s, e) =>
        {
            if (listenerListView.SelectedItems.Count > 0)
            {
                string addr = listenerListView.SelectedItems[0].SubItems[1].Text;
                int port = int.TryParse(listenerListView.SelectedItems[0].SubItems[2].Text, out int v) ? v : 0;
                TryCopy(port > 0 ? $"[{addr}]:{port}" : addr);
            }
        };
        p.Controls.Add(listenerListView);
        p.Controls.Add(ab);
        AddPageHeader(p, 12, "监听端口", "本机所有监听 IPv6 的 TCP / UDP 端口（对外提供服务时核对这里）");
    }

    // ============================================================
    // 一键体检（新）
    // ============================================================
    private void BuildHealthDetail()
    {
        Panel p = GetContentArea(11);
        Panel ab = new Panel { Dock = DockStyle.Top, Height = 50, BackColor = Color.Transparent, Padding = new Padding(0, 6, 0, 0) };
        FlatButton r = MakeTitleBtn("\U0001f3e5 重新体检", ACCENT_GREEN, (s, e) => _ = RunHealthCheckAsync()); r.SetBounds(0, 0, 130, 32);
        FlatButton c = MakeTitleBtn("\U0001f4cb 复制结果", ACCENT_BLUE, (s, e) => CopyHealthReport()); c.SetBounds(140, 0, 130, 32);
        healthScoreLabel = new Label { Text = "尚未体检", Location = new Point(290, 8), AutoSize = true, Font = FBold, ForeColor = TEXT_SECONDARY };
        ab.Controls.Add(r); ab.Controls.Add(c); ab.Controls.Add(healthScoreLabel);
        healthFlow = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            BackColor = Color.Transparent,
            FlowDirection = FlowDirection.LeftToRight,
            WrapContents = true,
            AutoScroll = true,
            Padding = new Padding(0, 4, 0, 0),
        };
        p.Controls.Add(healthFlow);
        p.Controls.Add(ab);
        AddPageHeader(p, 11, "一键体检", "自动检查 IPv6 配置链路，给出结论与修复建议");
    }

    // ============================================================
    // 高级工具
    // ============================================================
    private void BuildToolsDetail()
    {
        Panel p = GetContentArea(9);

        Panel tp = new Panel { Dock = DockStyle.Fill, BackColor = Color.Transparent, AutoScroll = true };
        p.Controls.Add(tp);
        AddPageHeader(p, 9, "高级工具", "IPv6 堆栈维护、诊断工具与子网计算");

        string[][] tools =
        {
            new[] { "重置 IPv6 堆栈", "重置 IPv6 配置到默认状态（需重启）", "netsh int ipv6 reset" },
            new[] { "刷新 DNS 缓存", "清除 DNS 解析缓存", "ipconfig /flushdns" },
            new[] { "IPv6 隐私扩展", "查看随机 IPv6 地址设置", "netsh int ipv6 show privacy" },
            new[] { "前缀策略表", "查看 IPv6 前缀优先级策略", "netsh int ipv6 show prefixpolicies" },
            new[] { "全局配置", "查看全局 IPv6 配置参数", "netsh int ipv6 show global" },
            new[] { "邻居缓存", "查看 IPv6 邻居发现缓存", "netsh int ipv6 show neighbors" },
            new[] { "目的缓存", "查看 IPv6 目的缓存", "netsh int ipv6 show destinationcache" },
            new[] { "IPsec 统计", "查看 IPv6 IPsec 统计", "netsh int ipv6 show ipsec" },
            new[] { "IPv6 统计", "IPv6 TCP/UDP 协议统计", "netstat -s -p IPv6" },
            new[] { "接口信息", "查看 IPv6 接口详细信息", "netsh int ipv6 show interfaces" },
            new[] { "DHCPv6 状态", "查看 DHCPv6 配置信息", "netsh int ipv6 show dhcp" },
            new[] { "路由缓存", "查看 IPv6 路由缓存", "netsh int ipv6 show routecache" },
        };

        // 倒序添加，保证数组第一项显示在最上方
        for (int i = tools.Length - 1; i >= 0; i--)
        {
            string name = tools[i][0], desc = tools[i][1], cmd = tools[i][2];
            bool needConfirm = cmd.Contains("reset", StringComparison.OrdinalIgnoreCase);

            Panel card = new Panel { Height = 60, Dock = DockStyle.Top, BackColor = BG_CARD, Margin = new Padding(0, 0, 0, 4) };
            var nameLbl = new Label { Text = name, Location = new Point(16, 8), AutoSize = true, Font = FBold, ForeColor = TEXT_PRIMARY, Cursor = Cursors.Hand };
            var descLbl = new Label { Text = desc, Location = new Point(16, 30), AutoSize = true, Font = FSmall, ForeColor = TEXT_SECONDARY, Cursor = Cursors.Hand };
            var cmdLbl = new Label { Text = cmd, Location = new Point(16, 58), AutoSize = true, Font = FMonoSm, ForeColor = ACCENT_CYAN, Visible = false };
            var chevron = new Label { Text = "▸", AutoSize = true, Font = FBold, ForeColor = TEXT_MUTED, Location = new Point(0, 8), Cursor = Cursors.Hand };
            card.Controls.Add(nameLbl);
            card.Controls.Add(descLbl);
            card.Controls.Add(cmdLbl);
            card.Controls.Add(chevron);

            FlatButton eb = MakeTitleBtn("\U0001f4cb 执行", ACCENT_BLUE, (s, e) => _ = ExecToolAsync(cmd, name, needConfirm));
            eb.Size = new Size(100, 32);
            card.Controls.Add(eb);
            tips?.SetToolTip(eb, cmd);

            int toggleVersion = 0;
            bool expanded = false;
            async void Toggle()
            {
                int version = ++toggleVersion;
                bool willExpand = !expanded;
                expanded = willExpand;
                if (willExpand) cmdLbl.Visible = true;
                chevron.Text = willExpand ? "▾" : "▸";
                int from = card.Height;
                int to = willExpand ? 88 : 60;
                await Anim.RunAsync(Ease.OutCubic, 210, p =>
                {
                    if (version != toggleVersion || card.IsDisposed) return;
                    card.Height = (int)(from + (to - from) * p);
                });
                if (version != toggleVersion || card.IsDisposed) return;
                card.Height = to;
                if (!expanded) cmdLbl.Visible = false;
            }

            bool hovered = false;
            card.Paint += (s, e) =>
            {
                if (!hovered) return;
                using var pen = new Pen(Color.FromArgb(90, ACCENT_BLUE), 1);
                e.Graphics.DrawRectangle(pen, 0, 0, card.Width - 1, card.Height - 1);
                using var bar = new SolidBrush(ACCENT_BLUE);
                e.Graphics.FillRectangle(bar, 0, 0, 2, card.Height);
            };
            card.MouseEnter += (s, e) => { hovered = true; card.BackColor = BG_HOVER; card.Invalidate(); };
            card.MouseLeave += (s, e) => { hovered = false; card.BackColor = BG_CARD; card.Invalidate(); };

            card.Click += (s, e) => Toggle();
            nameLbl.Click += (s, e) => Toggle();
            descLbl.Click += (s, e) => Toggle();
            chevron.Click += (s, e) => Toggle();

            card.Resize += (s, e) =>
            {
                eb.Location = new Point(Math.Max(0, card.Width - 120), 14);
                chevron.Location = new Point(Math.Max(0, card.Width - 150), 8);
            };

            tips?.SetToolTip(card, "点击展开命令：" + cmd);
            tp.Controls.Add(card);
        }

        // 子网计算器（置顶）
        Panel subnetCard = new Panel { Height = 76, Dock = DockStyle.Top, BackColor = BG_CARD };
        subnetCard.Controls.Add(new Label { Text = "IPv6 子网计算器", Location = new Point(16, 10), AutoSize = true, Font = FBold, ForeColor = TEXT_PRIMARY });
        subnetCard.Controls.Add(new Label { Text = "例如 2400:3200::/32，计算网络前缀 / 范围 / 地址总数 / 反向解析域", Location = new Point(16, 34), AutoSize = true, Font = FSmall, ForeColor = TEXT_SECONDARY });
        subnetInputBox = new TextBox { Location = new Point(16, 50), Width = 260, BackColor = MONO_BG, ForeColor = MONO_FG, Font = FMonoSm, BorderStyle = BorderStyle.FixedSingle, Text = "2400:3200::/32" };
        FlatButton sc = MakeTitleBtn("\U0001f9ee 计算", ACCENT_GREEN, (s, e) => RunSubnetCalc());
        sc.Size = new Size(100, 24);
        sc.Location = new Point(284, 49);
        subnetCard.Controls.Add(subnetInputBox);
        subnetCard.Controls.Add(sc);
        tp.Controls.Add(subnetCard);
    }

    // ============================================================
    // 配置报告
    // ============================================================
    private void BuildReportDetail()
    {
        Panel p = GetContentArea(10);
        Panel ab = new Panel { Dock = DockStyle.Top, Height = 44, BackColor = Color.Transparent, Padding = new Padding(0, 6, 0, 0) };
        FlatButton b1 = MakeTitleBtn("\U0001f4c4 生成报告", ACCENT_BLUE, (s, e) => _ = GenerateReportAsync()); b1.SetBounds(0, 0, 140, 32);
        btnReportCopy = MakeTitleBtn("\U0001f4cb 复制到剪贴板", ACCENT_GREEN, (s, e) => CopyReport()); btnReportCopy.SetBounds(150, 0, 150, 32);
        btnReportSave = MakeTitleBtn("\U0001f4be 导出文件", ACCENT_ORANGE, (s, e) => SaveReport()); btnReportSave.SetBounds(310, 0, 140, 32);
        ab.Controls.Add(b1); ab.Controls.Add(btnReportCopy); ab.Controls.Add(btnReportSave);
        reportTextBox = new TextBox
        {
            Dock = DockStyle.Fill,
            Multiline = true,
            ReadOnly = true,
            ScrollBars = ScrollBars.Both,
            BackColor = MONO_BG,
            ForeColor = MONO_FG,
            Font = FMonoSm,
            BorderStyle = BorderStyle.None,
            WordWrap = false,
        };
        p.Controls.Add(reportTextBox);
        p.Controls.Add(ab);
        AddPageHeader(p, 10, "IPv6 配置报告", "一键生成完整配置报告，可导出 TXT / JSON / HTML");
    }

    // ============================================================
    // 加载遮罩
    // ============================================================
    private void BuildLoadingOverlay()
    {
        loadingView = new LoadingView { Name = "LoadingView" };
        loadingOverlay.Controls.Add(loadingView);
    }

    // ============================================================
    // 日志右键菜单
    // ============================================================
    private void BuildLogMenu()
    {
        dashLogMenu = new ContextMenuStrip();
        dashLogMenu.Items.Add("复制全部日志", null, (s, e) => CopyLog());
        dashLogMenu.Items.Add("导出日志…", null, (s, e) => SaveLog());
        dashLogMenu.Items.Add(new ToolStripSeparator());
        dashLogMenu.Items.Add("清空日志", null, (s, e) => { if (dashEventLog != null) dashEventLog.Items.Clear(); });
        if (dashEventLog != null) dashEventLog.ContextMenuStrip = dashLogMenu;
    }

    // ============================================================
    // 系统托盘
    // ============================================================
    private void BuildSystemTray()
    {
        trayMenu = new ContextMenuStrip();
        trayMenu.Items.Add("显示窗口", null, (s, e) => ShowFromTray());
        trayMenu.Items.Add("检查 IPv6 连通性", null, async (s, e) => await CheckConnectivityAsync(quiet: false));
        trayMonitorItem = new ToolStripMenuItem("开始网络监控", null, (s, e) => ToggleMonitor());
        trayMenu.Items.Add(trayMonitorItem);
        trayMenu.Items.Add(new ToolStripSeparator());
        trayMenu.Items.Add("开机自启设置", null, (s, e) => ToggleAutoStart());
        trayMenu.Items.Add("关于", null, (s, e) => ShowAbout());
        trayMenu.Items.Add(new ToolStripSeparator());
        trayMenu.Items.Add("退出", null, (s, e) => { if (trayIcon != null) trayIcon.Visible = false; Application.Exit(); });

        trayIcon = new NotifyIcon
        {
            Icon = SystemIcons.Shield,
            Text = "IPv6 管理工具",
            ContextMenuStrip = trayMenu,
            Visible = true,
        };
        trayIcon.DoubleClick += (s, e) => ShowFromTray();

        Resize += (s, e) =>
        {
            if (WindowState == FormWindowState.Minimized)
            {
                Hide();
                trayIcon?.ShowBalloonTip(2000, "IPv6 管理工具", "已最小化到系统托盘", ToolTipIcon.Info);
            }
        };
    }

    // ============================================================
    // 窗口按钮事件
    // ============================================================
    private void BtnMinimize_Click(object sender, EventArgs e) => WindowState = FormWindowState.Minimized;
    private void BtnMaximize_Click(object sender, EventArgs e) => ToggleMaximize();
    private void BtnClose_Click(object sender, EventArgs e) => Application.Exit();

    // ============================================================
    // 检测模式 UI
    // ============================================================
    internal void BuildCheckUI()
    {
        Size = new Size(540, 280); MinimumSize = Size;
        Panel panel = new Panel { Dock = DockStyle.Fill, BackColor = BG_DARK };
        panel.Controls.Add(new Label { Text = "\U0001f310", AutoSize = true, Font = new Font("Segoe UI", 36F), Location = new Point(38, 28), ForeColor = ACCENT_BLUE });
        _cmStatusLbl = new Label { Text = "IPv6 状态检测中...", AutoSize = true, Font = new Font("Segoe UI", 16F, FontStyle.Bold), Location = new Point(100, 34), ForeColor = TEXT_PRIMARY };
        panel.Controls.Add(_cmStatusLbl);
        _cmDetailLbl = new Label { Text = "正在检查...", AutoSize = true, MaximumSize = new Size(430, 44), Font = FBold, Location = new Point(100, 72), ForeColor = TEXT_SECONDARY };
        panel.Controls.Add(_cmDetailLbl);
        _cmCountdownLbl = new Label { Text = "", AutoSize = true, Font = FSmall, Location = new Point(100, 100), ForeColor = TEXT_MUTED };
        panel.Controls.Add(_cmCountdownLbl);
        _cmProgress = new ProgressBar { Location = new Point(38, 150), Width = 464, Height = 6, Style = ProgressBarStyle.Continuous, Minimum = 0, Maximum = 100, Value = 0 };
        panel.Controls.Add(_cmProgress);
        Button sb = new Button
        {
            Text = "\u2699 开机自启设置",
            FlatStyle = FlatStyle.Flat,
            Size = new Size(140, 30),
            Location = new Point(38, 190),
            BackColor = BG_CARD,
            ForeColor = TEXT_SECONDARY,
            Font = FSmall,
            Cursor = Cursors.Hand,
        };
        sb.FlatAppearance.BorderSize = 1;
        sb.FlatAppearance.BorderColor = BORDER_COLOR;
        sb.Click += (s, e) => ToggleAutoStart();
        panel.Controls.Add(sb);
        Button cb = new Button
        {
            Text = "✕ 立即关闭",
            FlatStyle = FlatStyle.Flat,
            Size = new Size(110, 30),
            Location = new Point(392, 190),
            BackColor = Color.Transparent,
            ForeColor = TEXT_MUTED,
            Font = FSmall,
            Cursor = Cursors.Hand,
        };
        cb.FlatAppearance.BorderSize = 0;
        cb.MouseEnter += (s, e) => cb.ForeColor = TEXT_PRIMARY;
        cb.MouseLeave += (s, e) => cb.ForeColor = TEXT_MUTED;
        cb.Click += (s, e) => Application.Exit();
        panel.Controls.Add(cb);
        Controls.Add(panel);
        ApplyRoundedCorners();
    }
}
