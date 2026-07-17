// ============================================================
// Form1.Designer.cs — 平铺式 IPv6 管理工具 UI
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
    static readonly Color TEXT_MUTED = Color.FromArgb(80, 90, 100);
    static readonly Color BORDER_COLOR = Color.FromArgb(48, 54, 61);
    static readonly Color CARD_BORDER = Color.FromArgb(38, 44, 52);

    const int TITLEBAR_HEIGHT = 44;
    const int CORNER_RADIUS = 10;
    const string GITHUB_REPO = "BadeGusi/ipv6";
    const string CURRENT_VERSION = "v2.0";

    [System.Runtime.InteropServices.DllImport("gdi32.dll")]
    static extern nint CreateRoundRectRgn(int x1, int y1, int x2, int y2, int cx, int cy);

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
    internal Button btnMinimize; internal Button btnMaximize; internal Button btnClose; internal Button btnUpdate;
    internal Label connIndicator; internal Label connText; internal Label publicIPLabel;

    internal Label icon; internal Label ver;
    internal Panel contentPanel;
    internal FlowLayoutPanel tileContainer;
    internal Panel[] detailPages;
    internal Panel currentDetail;
    internal int activeDetailIndex = -1;

    internal FlowLayoutPanel dashCardContainer; internal ListBox dashEventLog;
    internal Panel myIPv6Panel; internal FlowLayoutPanel myIPv6Container;

    internal ListView adapterListView; internal Label adapterStatusLabel;
    internal TreeView addressTreeView;
    internal ListView dnsListView;
    internal ComboBox connTargetCombo; internal TextBox connResultBox;
    internal ListView routeListView; internal ListView tunnelListView;
    internal ListView firewallListView; internal ComboBox firewallDirectionCombo;
    internal ListView scanNeighborList;
    internal TextBox reportTextBox; internal Button btnReportCopy; internal Button btnReportSave;
    private Panel loadingOverlay;

    internal Label _cmStatusLbl; internal Label _cmDetailLbl; internal Label _cmCountdownLbl; internal ProgressBar _cmProgress;
    internal NotifyIcon trayIcon; internal ContextMenuStrip trayMenu;

    protected override void Dispose(bool disposing)
    {
        if (disposing && components != null) components.Dispose();
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
        connIndicator = new Label();
        connText = new Label();
        publicIPLabel = new Label();
        btnUpdate = new Button();
        contentPanel = new Panel();
        loadingOverlay = new Panel();
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
        connIndicator.Size = new Size(100, 23);
        connIndicator.TabIndex = 3;
        // 
        // connText
        // 
        connText.Location = new Point(0, 0);
        connText.Name = "connText";
        connText.Size = new Size(100, 23);
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
        btnUpdate.FlatAppearance.BorderSize = 0;
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
        Text = "IPv6 管理工具 v2.0";
        titleBar.ResumeLayout(false);
        ResumeLayout(false);
    }

    private static Button MakeWinBtn(string text, int x)
    {
        Button b = new Button { Text = text, Location = new Point(x, 0), Size = new Size(46, TITLEBAR_HEIGHT), FlatStyle = FlatStyle.Flat, FlatAppearance = { BorderSize = 0 }, BackColor = Color.Transparent, ForeColor = TEXT_SECONDARY, Font = new Font("Segoe UI", 11F), Cursor = Cursors.Hand, Anchor = AnchorStyles.Top | AnchorStyles.Right };
        b.MouseEnter += new EventHandler(WinBtn_MouseEnter);
        b.MouseLeave += new EventHandler(WinBtn_MouseLeave);
        return b;
    }

    // ============================================================
    // 运行时完整 UI 构建
    // ============================================================
    internal void BuildCompleteUI()
    {
        BuildWinButtons();
        ApplyTheme();
        BuildTiles();
        BuildDetailPages();
        BuildLoadingOverlay();
        BuildSystemTray();
        this.contentPanel.Controls.Add(this.loadingOverlay);
        SwitchToTileView();
    }

    private void BuildWinButtons()
    {
        this.btnMinimize = MakeWinBtn("─", 1134);
        this.btnMaximize = MakeWinBtn("□", 1180);
        this.btnClose = MakeWinBtn("✕", 1226);
        this.btnClose.Width = 54;

        this.btnMinimize.Click += new EventHandler(this.BtnMinimize_Click);
        this.btnMaximize.Click += new EventHandler(this.BtnMaximize_Click);
        this.btnClose.Click += new EventHandler(this.BtnClose_Click);
        this.btnClose.MouseEnter += (s, e) => { this.btnClose.BackColor = Color.FromArgb(196, 43, 28); this.btnClose.ForeColor = Color.White; };
        this.btnClose.MouseLeave += (s, e) => { this.btnClose.BackColor = Color.Transparent; this.btnClose.ForeColor = TEXT_SECONDARY; };

        this.titleBar.Controls.Add(this.btnMinimize);
        this.titleBar.Controls.Add(this.btnMaximize);
        this.titleBar.Controls.Add(this.btnClose);
    }

    // ============================================================
    // 应用毛玻璃主题
    // ============================================================
    private void ApplyTheme()
    {
        this.BackColor = Color.FromArgb(13, 17, 23);
        this.ForeColor = TEXT_PRIMARY;

        // ---- 标题栏：毛玻璃 ----
        this.titleBar.BackColor = Color.FromArgb(215, 22, 27, 34);
        this.titleBar.Height = TITLEBAR_HEIGHT;
        this.titleBar.Paint += (s, e) =>
        {
            using Pen p = new Pen(Color.FromArgb(40, 139, 148, 158));
            e.Graphics.DrawLine(p, 0, titleBar.Height - 1, titleBar.Width, titleBar.Height - 1);
        };

        // 标题栏拖拽
        MouseEventHandler dragHandler = (s, e) =>
        {
            if (e.Button == MouseButtons.Left)
            {
                NativeMethods.ReleaseCapture();
                _ = NativeMethods.SendMessage(this.Handle, 0xA1, 2, 0);
            }
        };
        this.titleBar.MouseDown += dragHandler;
        foreach (Control c in this.titleBar.Controls) c.MouseDown += dragHandler;

        this.titleLabel.ForeColor = TEXT_PRIMARY;
        this.titleLabel.Text = "  IPv6 管理工具";
        this.titleLabel.Font = new Font("Segoe UI", 12F, FontStyle.Bold);
        this.titleLabel.Location = new Point(42, 11);
        this.titleLabel.AutoSize = true;

        this.icon.Text = "\U0001f310";
        this.icon.Location = new Point(14, 10);
        this.icon.AutoSize = true;
        this.icon.Font = new Font("Segoe UI", 14F);

        this.ver.Text = "v2.0";
        this.ver.Location = new Point(185, 14);
        this.ver.AutoSize = true;
        this.ver.Font = new Font("Segoe UI", 8.5F);
        this.ver.ForeColor = Color.FromArgb(160, 139, 148, 158);

        this.connIndicator.ForeColor = TEXT_MUTED;
        this.connIndicator.Location = new Point(226, 14);
        this.connIndicator.AutoSize = true;
        this.connIndicator.Font = new Font("Segoe UI", 9F);

        this.connText.ForeColor = Color.FromArgb(160, 139, 148, 158);
        this.connText.Text = "检测中...";
        this.connText.Location = new Point(243, 14);
        this.connText.AutoSize = true;
        this.connText.Font = new Font("Segoe UI", 8.5F);

        this.publicIPLabel.Location = new Point(350, 14);
        this.publicIPLabel.AutoSize = true;
        this.publicIPLabel.Font = new Font("Segoe UI", 8.5F);
        this.publicIPLabel.ForeColor = Color.FromArgb(120, 139, 148, 158);

        // ---- 更新按钮 ----
        this.btnUpdate.Text = "⬇";
        this.btnUpdate.Font = new Font("Segoe UI", 12F);
        this.btnUpdate.ForeColor = Color.FromArgb(180, 139, 148, 158);
        this.btnUpdate.BackColor = Color.Transparent;
        this.btnUpdate.Cursor = Cursors.Hand;
        this.btnUpdate.Size = new Size(32, TITLEBAR_HEIGHT);
        this.btnUpdate.Location = new Point(1098, 0);
        this.btnUpdate.Anchor = AnchorStyles.Top | AnchorStyles.Right;
        this.btnUpdate.FlatAppearance.MouseOverBackColor = Color.FromArgb(50, 88, 166, 255);
        this.btnUpdate.TextAlign = ContentAlignment.MiddleCenter;

        this.contentPanel.BackColor = Color.FromArgb(13, 17, 23);
    }

    // ============================================================
    // 瓷砖仪表盘
    // ============================================================
    private void BuildTiles()
    {
        this.tileContainer = new FlowLayoutPanel
        {
            Dock = DockStyle.Top,
            Height = 260,
            BackColor = Color.Transparent,
            FlowDirection = FlowDirection.LeftToRight,
            WrapContents = true,
            Padding = new Padding(0, 0, 0, 8),
            AutoScroll = false
        };

        TileDef[] tiles = new TileDef[] {
            new TileDef("我的 IPv6", "查看本机所有 IPv6 地址", "\U0001f310", ACCENT_BLUE, 0),
            new TileDef("网络适配器", "管理适配器 IPv6 绑定", "\U0001f50c", ACCENT_GREEN, 1),
            new TileDef("IPv6 地址", "查看地址分配详情", "\U0001f4e1", ACCENT_CYAN, 2),
            new TileDef("DNS 设置", "IPv6 DNS 服务器配置", "\U0001f4cb", ACCENT_PURPLE, 3),
            new TileDef("连接测试", "Ping / Traceroute", "\U0001f4f6", ACCENT_PINK, 4),
            new TileDef("路由表", "IPv6 路由表查看", "\U0001f5fa\ufe0f", ACCENT_YELLOW, 5),
            new TileDef("隧道状态", "Teredo / 6to4 / ISATAP", "\U0001f517", ACCENT_ORANGE, 6),
            new TileDef("防火墙规则", "IPv6 防火墙规则列表", "\U0001f525", ACCENT_RED, 7),
            new TileDef("网络扫描", "邻居发现 / 活动连接", "\U0001f50d", ACCENT_CYAN, 8),
            new TileDef("高级工具", "诊断 / 维护命令", "\u26a1", ACCENT_PURPLE, 9),
            new TileDef("配置报告", "生成完整 IPv6 报告", "\U0001f4c4", ACCENT_GREEN, 10),
        };

        foreach (TileDef t in tiles)
        {
            Color tileColor = t.Color;
            bool hovered = false;

            Panel card = new Panel
            {
                Size = new Size(240, 76),
                BackColor = Color.FromArgb(170, 28, 33, 40),
                Margin = new Padding(0, 0, 10, 10),
                Cursor = Cursors.Hand,
                Tag = t.PageIndex
            };
            card.Paint += (s, e) =>
            {
                if (hovered)
                {
                    // hover 辉光
                    using Pen glow1 = new Pen(Color.FromArgb(120, tileColor), 3);
                    e.Graphics.DrawRectangle(glow1, 0, 0, 239, 75);
                    using Pen glow2 = new Pen(Color.FromArgb(60, tileColor), 6);
                    e.Graphics.DrawRectangle(glow2, 0, 0, 239, 75);
                    // 左边框发光
                    using Pen leftGlow = new Pen(Color.FromArgb(255, tileColor), 3);
                    e.Graphics.DrawLine(leftGlow, 0, 0, 0, 76);
                }
                else
                {
                    using Pen border = new Pen(Color.FromArgb(180, tileColor), 2);
                    e.Graphics.DrawLine(border, 0, 0, 0, 76);
                    using Pen outline = new Pen(Color.FromArgb(40, tileColor));
                    e.Graphics.DrawRectangle(outline, 1, 1, 238, 74);
                }
                using Pen top = new Pen(Color.FromArgb(25, 255, 255, 255));
                e.Graphics.DrawLine(top, 5, 2, 235, 2);
            };
            card.MouseEnter += (s, e) => { hovered = true; card.BackColor = Color.FromArgb(210, 38, 44, 52); card.Invalidate(); };
            card.MouseLeave += (s, e) => { hovered = false; card.BackColor = Color.FromArgb(170, 28, 33, 40); card.Invalidate(); };

            int idx = t.PageIndex;
            card.Click += (s, e) => SwitchToDetail(idx);

            Label iconLbl = new Label { Text = t.Icon, Location = new Point(14, 12), AutoSize = true, Font = new Font("Segoe UI", 18F) };
            Label titleLbl = new Label { Text = t.Title, Location = new Point(52, 14), AutoSize = true, Font = new Font("Segoe UI", 11F, FontStyle.Bold), ForeColor = TEXT_PRIMARY };
            Label descLbl = new Label { Text = t.Desc, Location = new Point(52, 36), AutoSize = true, Font = new Font("Segoe UI", 8.5F), ForeColor = TEXT_SECONDARY };
            Label accentLine = new Label { Text = "", Location = new Point(0, 0), Width = 3, Height = 72, BackColor = t.Color };

            foreach (Control c in new Control[] { iconLbl, titleLbl, descLbl, accentLine })
            {
                c.Click += (s, e) => SwitchToDetail(idx);
                card.Controls.Add(c);
            }

            this.tileContainer.Controls.Add(card);
        }

        this.contentPanel.Controls.Add(this.tileContainer);
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
    // 详情页面（复用原有页面内容）
    // ============================================================
    private void BuildDetailPages()
    {
        this.detailPages = new Panel[11];
        for (int i = 0; i < 11; i++)
        {
            this.detailPages[i] = new Panel { BackColor = BG_DARK, Padding = new Padding(0, 0, 0, 0) };
            this.detailPages[i].Visible = false;
        }

        // 返回按钮（通用，加到每个详情页前）
        for (int i = 0; i < 11; i++)
        {
            Panel headerBar = new Panel { Dock = DockStyle.Top, Height = 44, BackColor = Color.Transparent, Padding = new Padding(0, 5, 0, 0) };
            Button bb = new Button
            {
                Text = "\u2190  返回仪表盘",
                FlatStyle = FlatStyle.Flat,
                FlatAppearance = { BorderSize = 0, MouseOverBackColor = Color.FromArgb(40, 88, 166, 255) },
                Size = new Size(150, 30),
                Location = new Point(0, 7),
                BackColor = Color.Transparent,
                ForeColor = ACCENT_BLUE,
                Font = new Font("Segoe UI", 10F, FontStyle.Bold),
                Cursor = Cursors.Hand,
                TextAlign = ContentAlignment.MiddleLeft
            };
            bb.Click += (s, e) => SwitchToTileView();
            headerBar.Controls.Add(bb);
            this.detailPages[i].Controls.Add(headerBar);

            // 内容面板（由各 Build*Detail 方法填充）
            Panel contentArea = new Panel { Dock = DockStyle.Fill, BackColor = Color.Transparent, Name = "ContentArea" };
            this.detailPages[i].Controls.Add(contentArea);
        }

        // 构建各页内容（复用原有 Build 方法）
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
    }

    private Panel GetContentArea(int pageIndex)
    {
        foreach (Control c in this.detailPages[pageIndex].Controls)
            if (c is Panel p && p.Name == "ContentArea") return p;
        return null;
    }

    private void AddPageHeader(Panel parent, string title, string subtitle)
    {
        Panel h = new Panel { Height = 50, Dock = DockStyle.Top, BackColor = Color.Transparent };
        h.Controls.Add(new Label { Text = title, Location = new Point(0, 4), AutoSize = true, Font = new Font("Segoe UI", 18F, FontStyle.Bold), ForeColor = TEXT_PRIMARY });
        h.Controls.Add(new Label { Text = subtitle, Location = new Point(0, 32), AutoSize = true, Font = new Font("Segoe UI", 9.5F), ForeColor = TEXT_SECONDARY });
        parent.Controls.Add(h);
    }

    // ============================================================
    // 页面构建（内容与之前相同，目标为 ContentArea）
    // ============================================================
    private Button MakeTitleBtn(string text, Color bgColor, EventHandler click)
    {
        Button b = new Button { Text = text, FlatStyle = FlatStyle.Flat, FlatAppearance = { BorderSize = 0 }, BackColor = bgColor, ForeColor = Color.White, Font = new Font("Segoe UI", 10F, FontStyle.Bold), Cursor = Cursors.Hand, TextAlign = ContentAlignment.MiddleCenter };
        b.FlatAppearance.MouseOverBackColor = ControlPaint.Light(bgColor);
        b.FlatAppearance.MouseDownBackColor = ControlPaint.Dark(bgColor);
        b.Click += click;
        return b;
    }

    private ListView MakeLV(string[] cols, int[] widths)
    {
        // BackColor 必须纯色（ListView 不支持透明背景）
        Color solidBg = Color.FromArgb(28, 33, 40);
        Color solidHdr = Color.FromArgb(22, 27, 34);
        Color solidSel = Color.FromArgb(48, 54, 61);

        ListView lv = new ListView { Dock = DockStyle.Fill, View = View.Details, FullRowSelect = true, BorderStyle = BorderStyle.None, BackColor = solidBg, ForeColor = TEXT_PRIMARY, Font = new Font("Segoe UI", 9.5F), HeaderStyle = ColumnHeaderStyle.Nonclickable, OwnerDraw = true };
        lv.DrawColumnHeader += (s, e) => { e.Graphics.FillRectangle(new SolidBrush(solidHdr), e.Bounds); using var p = new Pen(Color.FromArgb(60, 88, 166, 255)); e.Graphics.DrawLine(p, e.Bounds.Left, e.Bounds.Bottom - 1, e.Bounds.Right, e.Bounds.Bottom - 1); TextRenderer.DrawText(e.Graphics, e.Header?.Text ?? "", new Font("Segoe UI", 9F, FontStyle.Bold), new Point(e.Bounds.X + 8, e.Bounds.Y + 6), Color.FromArgb(200, 139, 148, 158), TextFormatFlags.Left); };
        lv.DrawSubItem += (s, e) => { if (e.Item == null) return; e.Graphics.FillRectangle(new SolidBrush(e.Item.Selected ? solidSel : solidBg), e.Bounds); using var p = new Pen(Color.FromArgb(40, 255, 255, 255)); e.Graphics.DrawLine(p, e.Bounds.Left, e.Bounds.Top, e.Bounds.Right, e.Bounds.Top); TextRenderer.DrawText(e.Graphics, e.SubItem?.Text ?? "", lv.Font, new Point(e.Bounds.X + 6, e.Bounds.Y + 3), TEXT_PRIMARY, TextFormatFlags.Left | TextFormatFlags.VerticalCenter); };
        for (int i = 0; i < cols.Length; i++) lv.Columns.Add(cols[i], widths[i]);
        return lv;
    }

    internal void ApplyRoundedCorners()
    {
        nint hrgn = CreateRoundRectRgn(0, 0, this.Width, this.Height, CORNER_RADIUS, CORNER_RADIUS);
        this.Region = Region.FromHrgn(hrgn);
    }

    // ============================================================
    // 仪表盘详情
    // ============================================================
    private void BuildDashboardDetail()
    {
        Panel p = GetContentArea(0);
        AddPageHeader(p, "仪表盘", "IPv6 网络状态概览");

        Panel ab = new Panel { Dock = DockStyle.Top, Height = 44, BackColor = Color.Transparent, Padding = new Padding(0, 6, 0, 0) };
        Button b1 = MakeTitleBtn("\U0001f504 刷新所有", ACCENT_BLUE, (s, e) => ResetDetailCache(0)); b1.Location = new Point(0, 0); b1.Size = new Size(140, 32);
        Button b2 = MakeTitleBtn("\u2705 启用全部", ACCENT_GREEN, (s, e) => IPv6DataService.RunAdmin("Enable-NetAdapterBinding -Name '*' -ComponentID ms_tcpip6")); b2.Location = new Point(150, 0); b2.Size = new Size(140, 32);
        Button b3 = MakeTitleBtn("\u274c 禁用全部", ACCENT_RED, (s, e) => IPv6DataService.RunAdmin("Disable-NetAdapterBinding -Name '*' -ComponentID ms_tcpip6")); b3.Location = new Point(300, 0); b3.Size = new Size(140, 32);
        Button b4 = MakeTitleBtn("\u26a1 开机检测", ACCENT_ORANGE, (s, e) => ToggleAutoStart()); b4.Location = new Point(460, 0); b4.Size = new Size(130, 32);
        ab.Controls.Add(b1); ab.Controls.Add(b2); ab.Controls.Add(b3); ab.Controls.Add(b4);
        p.Controls.Add(ab);

        this.myIPv6Panel = new Panel { Dock = DockStyle.Top, Height = 170, BackColor = Color.Transparent };
        Label mh = new Label { Text = "    我的 IPv6 地址", Dock = DockStyle.Top, Height = 26, Font = new Font("Segoe UI", 10F, FontStyle.Bold), ForeColor = ACCENT_CYAN };
        this.myIPv6Panel.Controls.Add(mh);
        this.myIPv6Container = new FlowLayoutPanel { Dock = DockStyle.Fill, BackColor = Color.Transparent, FlowDirection = FlowDirection.LeftToRight, WrapContents = true, Padding = new Padding(8, 0, 0, 0), AutoScroll = true };
        this.myIPv6Panel.Controls.Add(this.myIPv6Container);
        p.Controls.Add(this.myIPv6Panel);

        Panel mp = new Panel { Dock = DockStyle.Top, Height = 90, BackColor = Color.Transparent };
        this.dashCardContainer = new FlowLayoutPanel { Dock = DockStyle.Fill, BackColor = Color.Transparent, FlowDirection = FlowDirection.LeftToRight, WrapContents = false, Padding = new Padding(0, 6, 0, 0) };
        mp.Controls.Add(this.dashCardContainer);
        p.Controls.Add(mp);

        Label lh = new Label { Text = "    系统日志", Dock = DockStyle.Top, Height = 24, Font = new Font("Segoe UI", 10F, FontStyle.Bold), ForeColor = TEXT_PRIMARY };
        p.Controls.Add(lh);
        this.dashEventLog = new ListBox { Dock = DockStyle.Fill, BackColor = BG_CARD, ForeColor = TEXT_SECONDARY, Font = new Font("Consolas", 9F), BorderStyle = BorderStyle.None, IntegralHeight = false };
        p.Controls.Add(this.dashEventLog);
    }

    private void BuildAdaptersDetail()
    {
        Panel p = GetContentArea(1);
        AddPageHeader(p, "网络适配器", "查看和管理每个适配器的 IPv6 状态");
        Panel ab = new Panel { Dock = DockStyle.Top, Height = 44, BackColor = Color.Transparent, Padding = new Padding(0, 6, 0, 0) };
        Button r = MakeTitleBtn("\U0001f504 刷新", ACCENT_BLUE, (s, e) => ResetDetailCache(1)); r.Location = new Point(0, 0); r.Size = new Size(110, 32);
        Button e_ = MakeTitleBtn("\u2705 启用选中", ACCENT_GREEN, (s, e) => ToggleSelectedAdapter(true)); e_.Location = new Point(120, 0); e_.Size = new Size(130, 32);
        Button d = MakeTitleBtn("\u274c 禁用选中", ACCENT_RED, (s, e) => ToggleSelectedAdapter(false)); d.Location = new Point(260, 0); d.Size = new Size(130, 32);
        this.adapterStatusLabel = new Label { Text = "", Location = new Point(400, 4), AutoSize = true, Font = new Font("Segoe UI", 9F), ForeColor = TEXT_SECONDARY };
        ab.Controls.Add(r); ab.Controls.Add(e_); ab.Controls.Add(d); ab.Controls.Add(this.adapterStatusLabel);
        p.Controls.Add(ab);
        this.adapterListView = MakeLV(new[] { "名称", "IPv6 状态", "接口指标", "状态", "描述" }, new[] { 200, 100, 90, 80, 260 });
        p.Controls.Add(this.adapterListView);
    }

    private void BuildAddressesDetail()
    {
        Panel p = GetContentArea(2);
        AddPageHeader(p, "IPv6 地址", "查看所有网络适配器的 IPv6 地址分配");
        Panel ab = new Panel { Dock = DockStyle.Top, Height = 44, BackColor = Color.Transparent, Padding = new Padding(0, 6, 0, 0) };
        Button r = MakeTitleBtn("\U0001f504 刷新", ACCENT_BLUE, (s, e) => ResetDetailCache(2)); r.Location = new Point(0, 0); r.Size = new Size(110, 32);
        ab.Controls.Add(r);
        p.Controls.Add(ab);
        this.addressTreeView = new TreeView { Dock = DockStyle.Fill, BackColor = BG_CARD, ForeColor = TEXT_PRIMARY, Font = new Font("Consolas", 9.5F), BorderStyle = BorderStyle.None, ShowLines = false, FullRowSelect = true };
        p.Controls.Add(this.addressTreeView);
    }

    private void BuildDnsDetail()
    {
        Panel p = GetContentArea(3);
        AddPageHeader(p, "DNS 设置", "查看 IPv6 DNS 服务器配置");
        Panel ab = new Panel { Dock = DockStyle.Top, Height = 44, BackColor = Color.Transparent, Padding = new Padding(0, 6, 0, 0) };
        ab.Controls.Add(new Label { Text = "适配器:", Location = new Point(0, 8), AutoSize = true, Font = new Font("Segoe UI", 9.5F), ForeColor = TEXT_SECONDARY });
        this.dnsListView = MakeLV(new[] { "适配器名称", "DNS 服务器 (IPv6)", "配置方式" }, new[] { 220, 320, 160 });
        p.Controls.Add(ab);
        p.Controls.Add(this.dnsListView);
    }

    private void BuildConnTestDetail()
    {
        Panel p = GetContentArea(4);
        AddPageHeader(p, "连接测试", "IPv6 连通性诊断工具");
        Panel ab = new Panel { Dock = DockStyle.Top, Height = 44, BackColor = Color.Transparent, Padding = new Padding(0, 6, 0, 0) };
        ab.Controls.Add(new Label { Text = "目标:", Location = new Point(0, 8), AutoSize = true, Font = new Font("Segoe UI", 9.5F), ForeColor = TEXT_SECONDARY });
        this.connTargetCombo = new ComboBox { Location = new Point(40, 4), Width = 280, BackColor = BG_CARD, ForeColor = TEXT_PRIMARY, FlatStyle = FlatStyle.Flat };
        this.connTargetCombo.Items.AddRange(new object[] { "ipv6.google.com", "2001:4860:4860::8888", "2001:4860:4860::8844", "2606:4700:4700::1111", "2620:fe::fe", "localhost", "::1" });
        this.connTargetCombo.SelectedIndex = 0;
        Button pb = MakeTitleBtn("\U0001f4f6 Ping", ACCENT_BLUE, async (s, e) => await RunPingTest()); pb.Location = new Point(340, 0); pb.Size = new Size(100, 32);
        Button tb = MakeTitleBtn("\U0001f5fa\ufe0f Traceroute", ACCENT_PURPLE, async (s, e) => await RunTracert()); tb.Location = new Point(450, 0); tb.Size = new Size(120, 32);
        ab.Controls.Add(this.connTargetCombo); ab.Controls.Add(pb); ab.Controls.Add(tb);
        p.Controls.Add(ab);
        this.connResultBox = new TextBox { Dock = DockStyle.Fill, Multiline = true, ReadOnly = true, ScrollBars = ScrollBars.Both, BackColor = Color.FromArgb(10, 14, 20), ForeColor = Color.FromArgb(180, 200, 220), Font = new Font("Consolas", 9.5F), BorderStyle = BorderStyle.None, WordWrap = false };
        p.Controls.Add(this.connResultBox);
    }

    private void BuildRoutesDetail()
    {
        Panel p = GetContentArea(5);
        AddPageHeader(p, "路由表", "IPv6 路由表查看");
        Panel ab = new Panel { Dock = DockStyle.Top, Height = 44, BackColor = Color.Transparent, Padding = new Padding(0, 6, 0, 0) };
        Button r = MakeTitleBtn("\U0001f504 刷新", ACCENT_BLUE, (s, e) => ResetDetailCache(5)); r.Location = new Point(0, 0); r.Size = new Size(110, 32);
        ab.Controls.Add(r);
        p.Controls.Add(ab);
        this.routeListView = MakeLV(new[] { "前缀", "接口别名", "跃点数", "前缀长度", "类型" }, new[] { 260, 200, 80, 100, 120 });
        p.Controls.Add(this.routeListView);
    }

    private void BuildTunnelsDetail()
    {
        Panel p = GetContentArea(6);
        AddPageHeader(p, "隧道状态", "Teredo / 6to4 / ISATAP 隧道状态检查");
        Panel ab = new Panel { Dock = DockStyle.Top, Height = 44, BackColor = Color.Transparent, Padding = new Padding(0, 6, 0, 0) };
        Button r = MakeTitleBtn("\U0001f504 刷新", ACCENT_BLUE, (s, e) => ResetDetailCache(6)); r.Location = new Point(0, 0); r.Size = new Size(110, 32);
        ab.Controls.Add(r);
        p.Controls.Add(ab);
        this.tunnelListView = MakeLV(new[] { "隧道类型", "接口名称", "状态", "本地地址", "远程地址" }, new[] { 100, 200, 80, 180, 180 });
        p.Controls.Add(this.tunnelListView);
    }

    private void BuildFirewallDetail()
    {
        Panel p = GetContentArea(7);
        AddPageHeader(p, "防火墙规则", "IPv6 相关的 Windows Defender 防火墙规则");
        Panel ab = new Panel { Dock = DockStyle.Top, Height = 44, BackColor = Color.Transparent, Padding = new Padding(0, 6, 0, 0) };
        ab.Controls.Add(new Label { Text = "方向:", Location = new Point(0, 8), AutoSize = true, Font = new Font("Segoe UI", 9.5F), ForeColor = TEXT_SECONDARY });
        this.firewallDirectionCombo = new ComboBox { Location = new Point(42, 4), Width = 120, DropDownStyle = ComboBoxStyle.DropDownList, BackColor = BG_CARD, ForeColor = TEXT_PRIMARY, FlatStyle = FlatStyle.Flat };
        this.firewallDirectionCombo.Items.AddRange(new object[] { "全部", "入站", "出站" }); this.firewallDirectionCombo.SelectedIndex = 0;
        this.firewallDirectionCombo.SelectedIndexChanged += (s, e) => ResetDetailCache(7);
        Button r = MakeTitleBtn("\U0001f504 刷新", ACCENT_BLUE, (s, e) => ResetDetailCache(7)); r.Location = new Point(180, 0); r.Size = new Size(100, 32);
        ab.Controls.Add(this.firewallDirectionCombo); ab.Controls.Add(r);
        p.Controls.Add(ab);
        this.firewallListView = MakeLV(new[] { "规则名称", "方向", "操作", "协议", "本地端口", "远程端口" }, new[] { 220, 70, 70, 80, 100, 100 });
        p.Controls.Add(this.firewallListView);
    }

    private void BuildScanDetail()
    {
        Panel p = GetContentArea(8);
        AddPageHeader(p, "网络扫描", "扫描本地网络中的 IPv6 设备与连接");
        Panel ab = new Panel { Dock = DockStyle.Top, Height = 50, BackColor = Color.Transparent, Padding = new Padding(0, 6, 0, 0) };
        Button b1 = MakeTitleBtn("\U0001f50d 扫描邻居", ACCENT_BLUE, (s, e) => ResetDetailCache(8)); b1.Location = new Point(0, 0); b1.Size = new Size(140, 32);
        Button b2 = MakeTitleBtn("\U0001f4e1 多播 Ping", ACCENT_PURPLE, async (s, e) => await ScanMulticastAsync()); b2.Location = new Point(150, 0); b2.Size = new Size(130, 32);
        Button b3 = MakeTitleBtn("\U0001f517 活动连接", ACCENT_CYAN, async (s, e) => await ScanConnectionsAsync()); b3.Location = new Point(290, 0); b3.Size = new Size(130, 32);
        ab.Controls.Add(b1); ab.Controls.Add(b2); ab.Controls.Add(b3);
        p.Controls.Add(ab);
        p.Controls.Add(new Label { Text = "    发现的 IPv6 邻居设备", Dock = DockStyle.Top, Height = 28, Font = new Font("Segoe UI", 10F, FontStyle.Bold), ForeColor = TEXT_PRIMARY });
        this.scanNeighborList = MakeLV(new[] { "IP 地址", "MAC 地址", "接口", "状态", "发现方式" }, new[] { 280, 180, 200, 70, 100 });
        p.Controls.Add(this.scanNeighborList);
    }

    private void BuildToolsDetail()
    {
        Panel p = GetContentArea(9);
        AddPageHeader(p, "高级工具", "IPv6 堆栈维护、诊断工具");
        Panel tp = new Panel { Dock = DockStyle.Fill, BackColor = Color.Transparent, AutoScroll = true };
        string[][] tools = new string[][] {
            new string[] { "重置 IPv6 堆栈", "重置 IPv6 配置到默认状态（需重启）", "netsh int ipv6 reset" },
            new string[] { "刷新 DNS 缓存", "清除 DNS 解析缓存", "ipconfig /flushdns" },
            new string[] { "IPv6 隐私扩展", "查看随机 IPv6 地址设置", "netsh int ipv6 show privacy" },
            new string[] { "前缀策略表", "查看 IPv6 前缀优先级策略", "netsh int ipv6 show prefixpolicies" },
            new string[] { "全局配置", "查看全局 IPv6 配置参数", "netsh int ipv6 show global" },
            new string[] { "邻居缓存", "查看 IPv6 邻居发现缓存", "netsh int ipv6 show neighbors" },
            new string[] { "目的缓存", "查看 IPv6 目的缓存", "netsh int ipv6 show destinationcache" },
            new string[] { "IPsec 统计", "查看 IPv6 IPsec 统计", "netsh int ipv6 show ipsec" },
            new string[] { "IPv6 统计", "IPv6 TCP/UDP 协议统计", "netstat -s -p IPv6" },
            new string[] { "接口信息", "查看 IPv6 接口详细信息", "netsh int ipv6 show interfaces" },
            new string[] { "DHCPv6 状态", "查看 DHCPv6 配置信息", "netsh int ipv6 show dhcp" },
            new string[] { "路由缓存", "查看 IPv6 路由缓存", "netsh int ipv6 show routecache" },
        };
        foreach (string[] t in tools)
        {
            string name = t[0]; string desc = t[1]; string cmd = t[2];
            Panel card = new Panel { Height = 60, Dock = DockStyle.Top, BackColor = BG_CARD };
            card.Controls.Add(new Label { Text = name, Location = new Point(16, 8), AutoSize = true, Font = new Font("Segoe UI", 10F, FontStyle.Bold), ForeColor = TEXT_PRIMARY });
            card.Controls.Add(new Label { Text = desc, Location = new Point(16, 28), AutoSize = true, Font = new Font("Segoe UI", 8.5F), ForeColor = TEXT_SECONDARY });
            Button eb = MakeTitleBtn("\U0001f4cb 执行", ACCENT_BLUE, async (s, e) => { ShowLoading($"执行: {name}..."); string r = await IPv6DataService.RunPsAsync(cmd, cache: false); HideLoading(); ShowResultDialog(name, r); AddLog($"[{name}] 执行完成"); });
            eb.Anchor = AnchorStyles.Top | AnchorStyles.Right; eb.Location = new Point(0, 14); eb.Size = new Size(100, 32);
            card.Resize += (s, e) => eb.Location = new Point(card.Width - 120, 14);
            card.Controls.Add(eb);
            tp.Controls.Add(card);
        }
        p.Controls.Add(tp);
    }

    private void BuildReportDetail()
    {
        Panel p = GetContentArea(10);
        AddPageHeader(p, "IPv6 配置报告", "一键生成完整的系统 IPv6 配置报告");
        Panel ab = new Panel { Dock = DockStyle.Top, Height = 44, BackColor = Color.Transparent, Padding = new Padding(0, 6, 0, 0) };
        Button b1 = MakeTitleBtn("\U0001f4c4 生成报告", ACCENT_BLUE, async (s, e) => await GenerateReport()); b1.Location = new Point(0, 0); b1.Size = new Size(140, 32);
        this.btnReportCopy = MakeTitleBtn("\U0001f4cb 复制到剪贴板", ACCENT_GREEN, (s, e) => CopyReport()); this.btnReportCopy.Location = new Point(150, 0); this.btnReportCopy.Size = new Size(150, 32);
        this.btnReportSave = MakeTitleBtn("\U0001f4be 保存为文件", ACCENT_ORANGE, (s, e) => SaveReport()); this.btnReportSave.Location = new Point(310, 0); this.btnReportSave.Size = new Size(140, 32);
        ab.Controls.Add(b1); ab.Controls.Add(this.btnReportCopy); ab.Controls.Add(this.btnReportSave);
        p.Controls.Add(ab);
        this.reportTextBox = new TextBox { Dock = DockStyle.Fill, Multiline = true, ReadOnly = true, ScrollBars = ScrollBars.Both, BackColor = Color.FromArgb(10, 14, 20), ForeColor = Color.FromArgb(180, 200, 220), Font = new Font("Consolas", 9F), BorderStyle = BorderStyle.None, WordWrap = false };
        p.Controls.Add(this.reportTextBox);
    }

    // ============================================================
    // 加载遮罩
    // ============================================================
    private void BuildLoadingOverlay()
    {
        Label sp = new Label { Text = "\u23f3", AutoSize = true, Font = new Font("Segoe UI", 32F), ForeColor = ACCENT_BLUE, Location = new Point(0, 0) };
        Label lt = new Label { Text = "加载中...", AutoSize = true, Font = new Font("Segoe UI", 14F), ForeColor = TEXT_SECONDARY, Location = new Point(0, 60) };
        this.loadingOverlay.Controls.Add(sp);
        this.loadingOverlay.Controls.Add(lt);
        this.loadingOverlay.Resize += (s, e) => { sp.Location = new Point((loadingOverlay.Width - sp.Width) / 2, loadingOverlay.Height / 2 - 60); lt.Location = new Point((loadingOverlay.Width - lt.Width) / 2, loadingOverlay.Height / 2 + 10); };
    }

    // ============================================================
    // 系统托盘
    // ============================================================
    private void BuildSystemTray()
    {
        this.trayMenu = new ContextMenuStrip();
        this.trayMenu.Items.Add("显示窗口", null, (s, e) => { this.Show(); this.WindowState = FormWindowState.Normal; this.BringToFront(); });
        this.trayMenu.Items.Add("检查 IPv6 连通性", null, async (s, e) => await CheckConnectivityAsync());
        this.trayMenu.Items.Add(new ToolStripSeparator());
        this.trayMenu.Items.Add("开机自启设置", null, (s, e) => ToggleAutoStart());
        this.trayMenu.Items.Add(new ToolStripSeparator());
        this.trayMenu.Items.Add("退出", null, (s, e) => { trayIcon.Visible = false; Application.Exit(); });
        this.trayIcon = new NotifyIcon { Icon = SystemIcons.Shield, Text = "IPv6 管理工具", ContextMenuStrip = this.trayMenu, Visible = true };
        this.trayIcon.DoubleClick += (s, e) => { this.Show(); this.WindowState = FormWindowState.Normal; this.BringToFront(); };
        this.Resize += (s, e) => { if (this.WindowState == FormWindowState.Minimized) { this.Hide(); trayIcon.ShowBalloonTip(2000, "IPv6 管理工具", "已最小化到系统托盘", ToolTipIcon.Info); } };
    }

    // ============================================================
    // 窗口按钮事件
    // ============================================================
    private static void WinBtn_MouseEnter(object sender, EventArgs e)
    {
        if (sender is Button btn) { btn.BackColor = BG_HOVER; btn.ForeColor = TEXT_PRIMARY; }
    }
    private static void WinBtn_MouseLeave(object sender, EventArgs e)
    {
        if (sender is Button btn) { btn.BackColor = Color.Transparent; btn.ForeColor = TEXT_SECONDARY; }
    }
    private void BtnMinimize_Click(object sender, EventArgs e) { this.WindowState = FormWindowState.Minimized; }
    private void BtnMaximize_Click(object sender, EventArgs e) { ToggleMaximize(); }
    private void BtnClose_Click(object sender, EventArgs e) { Application.Exit(); }

    // ============================================================
    // 检测模式 UI
    // ============================================================
    internal void BuildCheckUI()
    {
        this.Size = new Size(520, 260); this.MinimumSize = this.Size;
        Panel panel = new Panel { Dock = DockStyle.Fill, BackColor = BG_DARK };
        panel.Controls.Add(new Label { Text = "\U0001f310", AutoSize = true, Font = new Font("Segoe UI", 36F), Location = new Point(38, 28), ForeColor = ACCENT_BLUE });
        this._cmStatusLbl = new Label { Text = "IPv6 状态检测中...", AutoSize = true, Font = new Font("Segoe UI", 16F, FontStyle.Bold), Location = new Point(100, 34), ForeColor = TEXT_PRIMARY };
        panel.Controls.Add(this._cmStatusLbl);
        this._cmDetailLbl = new Label { Text = "正在检查...", AutoSize = true, Font = new Font("Segoe UI", 10F), Location = new Point(100, 66), ForeColor = TEXT_SECONDARY };
        panel.Controls.Add(this._cmDetailLbl);
        this._cmCountdownLbl = new Label { Text = "5 秒后自动关闭", AutoSize = true, Font = new Font("Segoe UI", 9.5F), Location = new Point(100, 98), ForeColor = TEXT_MUTED };
        panel.Controls.Add(this._cmCountdownLbl);
        this._cmProgress = new ProgressBar { Location = new Point(38, 150), Width = 440, Height = 6, Style = ProgressBarStyle.Continuous, ForeColor = ACCENT_GREEN, BackColor = BG_CARD, Minimum = 0, Maximum = 50, Value = 50 };
        panel.Controls.Add(this._cmProgress);
        Button sb = new Button { Text = "\u2699 开机自启设置", FlatStyle = FlatStyle.Flat, FlatAppearance = { BorderSize = 1, BorderColor = BORDER_COLOR }, Size = new Size(130, 30), Location = new Point(38, 186), BackColor = BG_CARD, ForeColor = TEXT_SECONDARY, Font = new Font("Segoe UI", 9F), Cursor = Cursors.Hand };
        sb.Click += (s, e) => ToggleAutoStart();
        panel.Controls.Add(sb);
        Button cb = new Button { Text = "✕ 立即关闭", FlatStyle = FlatStyle.Flat, FlatAppearance = { BorderSize = 0 }, Size = new Size(100, 30), Location = new Point(380, 186), BackColor = Color.Transparent, ForeColor = TEXT_MUTED, Font = new Font("Segoe UI", 9F), Cursor = Cursors.Hand };
        cb.MouseEnter += (s, e) => cb.ForeColor = TEXT_PRIMARY; cb.MouseLeave += (s, e) => cb.ForeColor = TEXT_MUTED; cb.Click += (s, e) => Application.Exit();
        panel.Controls.Add(cb);
        this.Controls.Add(panel);
        nint hrgn = CreateRoundRectRgn(0, 0, this.Width, this.Height, 12, 12);
        this.Region = Region.FromHrgn(hrgn);
    }
}
