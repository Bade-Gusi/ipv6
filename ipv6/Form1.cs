// ============================================================
// Form1.cs — 业务逻辑（全部数据走 IPv6DataService）
// ============================================================

#nullable disable
#pragma warning disable CS4014 // 故意使用 fire-and-forget 异步调用

using System.Diagnostics;
using System.Net.Http;
using System.Text.Json;
using System.Text;

namespace ipv6;

public partial class Form1 : Form
{
    private readonly bool _checkMode;
    private int _countdownSeconds = 5;
    private readonly bool[] _detailLoaded = new bool[11];
    private string _cachedReport = "";
    private readonly IPv6DataService _data = new();
    private bool _firstTileView = true;

    // ============================================================
    // 构造 & 窗口
    // ============================================================
    public Form1(bool checkMode = false)
    {
        _checkMode = checkMode;
        InitializeComponent();
        if (_checkMode) { BuildCheckUI(); this.Load += OnCheckModeLoad; }
        else { BuildCompleteUI(); this.Load += OnFormLoad; }
    }

    protected override void WndProc(ref Message m)
    {
        if (m.Msg == 0x84)
        {
            base.WndProc(ref m);
            if (m.Result == (nint)1)
            {
                Point pt = PointToClient(Cursor.Position);
                if (pt.Y <= 44 && pt.X < this.Width - 140) { m.Result = (nint)2; return; }
                int e = 8;
                if (pt.X <= e && pt.Y <= e) m.Result = (nint)13;
                else if (pt.X >= Width - e && pt.Y <= e) m.Result = (nint)14;
                else if (pt.X <= e && pt.Y >= Height - e) m.Result = (nint)16;
                else if (pt.X >= Width - e && pt.Y >= Height - e) m.Result = (nint)17;
                else if (pt.X <= e) m.Result = (nint)10;
                else if (pt.X >= Width - e) m.Result = (nint)11;
                else if (pt.Y <= e) m.Result = (nint)12;
                else if (pt.Y >= Height - e) m.Result = (nint)15;
            }
            return;
        }
        base.WndProc(ref m);
    }

    protected override void OnResize(EventArgs e)
    {
        base.OnResize(e);
    }

    private void ToggleMaximize()
    {
        if (WindowState == FormWindowState.Normal)
        {
            WindowState = FormWindowState.Maximized;
            btnMaximize.Text = "\u2750";
        }
        else
        {
            WindowState = FormWindowState.Normal;
            btnMaximize.Text = "\u25a1";
        }
    }
    private void BtnUpdate_Click(object s, EventArgs e) { _ = CheckForUpdatesAsync(); }

    private async Task AnimateTilesInAsync()
    {
        await Task.Delay(30);
        if (tileContainer == null || tileContainer.IsDisposed) return;
        int i = 0;
        foreach (Control c in tileContainer.Controls)
        {
            if (c is Panel card)
            {
                card.Top += 20;
                int targetTop = card.Top - 20;
                int delay = i * 35;
                int startTop = card.Top;
                _ = AnimateSlide(card, startTop, targetTop, delay);
                i++;
            }
        }
    }

    private async Task AnimateSlide(Panel card, int from, int to, int delayMs)
    {
        await Task.Delay(delayMs);
        if (card == null || card.IsDisposed) return;
        try
        {
            for (int i = 0; i < 10; i++)
            {
                float t = 1 - MathF.Pow(1 - i / 10f, 2);
                card.BeginInvoke(() => card.Top = from + (int)((to - from) * t));
                await Task.Delay(12);
            }
            card.BeginInvoke(() => card.Top = to);
        }
        catch { }
    }

    private async void SwitchToDetail(int index)
    {
        tileContainer.Visible = false;
        if (currentDetail != null && currentDetail.Parent == contentPanel)
            contentPanel.Controls.Remove(currentDetail);

        Panel dp = detailPages[index];
        // 铺满内容区（使用 DisplayRectangle 适应 Padding）
        Rectangle area = contentPanel.DisplayRectangle;
        dp.Size = area.Size;
        dp.Visible = true;

        int w = area.Width;
        dp.Location = new Point(area.X + w, area.Y);
        contentPanel.Controls.Add(dp);
        dp.BringToFront();
        currentDetail = dp;
        activeDetailIndex = index;

        // 从右侧滑入（ease-out）
        for (int i = 0; i < 12; i++)
        {
            float t = 1 - MathF.Pow(1 - i / 12f, 3);
            dp.Left = w - (int)(w * t);
            await Task.Delay(10);
        }
        dp.Left = 0;

        if (!_detailLoaded[index])
        {
            _detailLoaded[index] = true;
            LoadDetailData(index);
        }
    }

    private async void SwitchToTileView()
    {
        if (currentDetail != null && currentDetail.Parent == contentPanel)
        {
            // 详情页向右滑出
            int w = currentDetail.Width;
            for (int i = 0; i < 10; i++)
            {
                float t = i / 10f;
                currentDetail.Left = (int)(w * t);
                await Task.Delay(10);
            }
            contentPanel.Controls.Remove(currentDetail);
            currentDetail.Left = 0;
        }
        tileContainer.Visible = true;
        currentDetail = null;
        activeDetailIndex = -1;
        if (!_firstTileView) { _ = AnimateTilesInAsync(); } else { _firstTileView = false; }
    }

    private void ResetDetailCache(int idx)
    {
        _detailLoaded[idx] = false;
        if (idx == activeDetailIndex)
        {
            _detailLoaded[idx] = true;
            LoadDetailData(idx);
        }
    }

    private async void LoadDetailData(int idx)
    {
        if (_data.Bindings.Count == 0) { await _data.RefreshAllAsync(); if (dashEventLog != null) AddLog("✓ 全部数据已刷新"); }

        switch (idx)
        {
            case 0: PopulateDashboard(); break;
            case 1: PopulateAdapters(); break;
            case 2: PopulateAddresses(); break;
            case 3: PopulateDns(); break;
            case 5: PopulateRoutes(); break;
            case 6: _ = PopulateTunnels(); break;
            case 7: _ = PopulateFirewall(); break;
            case 8: _ = PopulateNetworkScanAsync(); break;
        }
    }

    // ============================================================
    // Form Load
    // ============================================================
    private async void OnFormLoad(object sender, EventArgs e)
    {
        ApplyRoundedCorners();

        // 淡入动画
        this.Opacity = 0;
        for (int i = 0; i < 25; i++) { this.Opacity += 0.04; await Task.Delay(8); }
        this.Opacity = 1;

        _ = _data.RefreshAllAsync(s => { if (dashEventLog != null && !dashEventLog.IsDisposed) dashEventLog.BeginInvoke(() => AddLog(s)); });
        _ = CheckConnectivityAsync();
        _ = AnimatePulseAsync();  // 连通性脉冲
    }

    // ============================================================
    // 脉冲动画：连通性指示器呼吸效果
    // ============================================================
    private async Task AnimatePulseAsync()
    {
        bool growing = false;
        float size = 9f;
        while (!this.IsDisposed)
        {
            await Task.Delay(1200);
            if (connIndicator == null || connIndicator.IsDisposed) return;
            if (connIndicator.InvokeRequired) return;

            growing = !growing;
            float target = growing ? 13f : 9f;
            float start = size;
            for (int i = 0; i < 10; i++)
            {
                size = start + (target - start) * (i / 10f);
                try { connIndicator.Font = new Font("Segoe UI", size); } catch { }
                await Task.Delay(25);
            }
        }
    }

    // ============================================================
    // 仪表盘
    // ============================================================
    private void PopulateDashboard()
    {
        int total = _data.Bindings.Count, enabled = _data.Bindings.Count(b => b.Enabled), disabled = total - enabled;
        dashCardContainer.Controls.Clear();
        AddMetric("总适配器", total.ToString(), TEXT_PRIMARY);
        AddMetric("已启用", enabled.ToString(), ACCENT_GREEN);
        AddMetric("已禁用", disabled.ToString(), ACCENT_RED);
        AddMetric("启用率", total > 0 ? $"{enabled * 100 / total}%" : "0%", ACCENT_BLUE);
        AddMetric("IPv6地址", _data.Addresses.Count.ToString(), ACCENT_CYAN);
        AddLog($"✓ 仪表盘: {total}适配器/{_data.Addresses.Count}地址/{_data.Routes.Count}路由");

        // 异步填充我的 IPv6 地址
        _ = PopulateMyIPv6AddressesAsync();
    }

    // ============================================================
    // 我的 IPv6 地址展示（毛玻璃卡片 + 复制 + 入场动画）
    // ============================================================
    private async Task PopulateMyIPv6AddressesAsync()
    {
        try
        {
            if (_data.Bindings.Count == 0) await _data.RefreshAllAsync();
            var myIPs = await _data.GetMyIPv6AddressesAsync();
            var gateways = await _data.GetDefaultGatewayAsync();

            if (myIPv6Container == null || myIPv6Container.IsDisposed) return;
            if (myIPv6Container.InvokeRequired) { myIPv6Container.BeginInvoke(new Action(async () => await PopulateMyIPv6AddressesAsync())); return; }

            myIPv6Container.Controls.Clear();
            int added = 0;

            foreach (var a in myIPs)
            {
                Color typeColor = a.Type switch
                {
                    "全局单播" => ACCENT_BLUE,
                    "唯一本地" => ACCENT_PURPLE,
                    "链路本地" => ACCENT_CYAN,
                    "回环" => TEXT_MUTED,
                    "多播" => ACCENT_ORANGE,
                    "6to4" => ACCENT_GREEN,
                    "站点本地" => ACCENT_YELLOW,
                    _ => ACCENT_GREEN
                };

                var card = new Panel
                {
                    Size = new Size(300, 66),
                    BackColor = Color.FromArgb(180, 28, 33, 40),
                    Margin = new Padding(0, 2, 10, 2),
                    Cursor = Cursors.Hand,
                    Tag = a.IP
                };
                card.Paint += (s, e) =>
                {
                    using Pen c = new Pen(Color.FromArgb(120, typeColor), 1);
                    e.Graphics.DrawRectangle(c, 0, 0, 299, 65);
                    using Pen left = new Pen(typeColor, 2);
                    e.Graphics.DrawLine(left, 0, 0, 0, 66);
                };
                card.MouseEnter += (s, e) => card.BackColor = Color.FromArgb(220, 38, 44, 52);
                card.MouseLeave += (s, e) => card.BackColor = Color.FromArgb(180, 28, 33, 40);
                card.Click += (s, e) =>
                {
                    Clipboard.SetText(a.IP);
                    AddLog($"📋 已复制 IPv6: {a.IP}");
                };

                // 类型标签 + 复制提示
                Panel topBar = new Panel { Height = 20, Dock = DockStyle.Top, BackColor = Color.Transparent };
                topBar.Controls.Add(new Label { Text = a.Type, Location = new Point(10, 2), AutoSize = true, Font = new Font("Segoe UI", 7.5F, FontStyle.Bold), ForeColor = typeColor });
                topBar.Controls.Add(new Label { Text = "\U0001f4cb", Location = new Point(265, 2), AutoSize = true, Font = new Font("Segoe UI", 9F), ForeColor = Color.FromArgb(120, 139, 148, 158) });
                card.Controls.Add(topBar);

                // IP 地址
                card.Controls.Add(new Label { Text = a.IP, Location = new Point(10, 22), AutoSize = true, Font = new Font("Consolas", 10F, FontStyle.Bold), ForeColor = TEXT_PRIMARY });

                // 前缀 + 接口 + 状态
                card.Controls.Add(new Label
                {
                    Text = $"/{a.PrefixLen}  {a.Interface.Truncate(20)}  •  {a.State}",
                    Location = new Point(10, 44),
                    AutoSize = true,
                    Font = new Font("Segoe UI", 7.5F),
                    ForeColor = Color.FromArgb(160, 139, 148, 158)
                });

                myIPv6Container.Controls.Add(card);
                added++;
            }

            // 默认网关卡片
            foreach (var gw in gateways)
            {
                Panel gwCard = new Panel
                {
                    Size = new Size(260, 66),
                    BackColor = Color.FromArgb(170, 28, 33, 40),
                    Margin = new Padding(0, 2, 10, 2),
                    Cursor = Cursors.Hand
                };
                gwCard.Paint += (s, e) =>
                {
                    using Pen c = new Pen(Color.FromArgb(120, ACCENT_ORANGE), 1);
                    e.Graphics.DrawRectangle(c, 0, 0, 259, 65);
                    using Pen left = new Pen(ACCENT_ORANGE, 2);
                    e.Graphics.DrawLine(left, 0, 0, 0, 66);
                };
                gwCard.MouseEnter += (s, e) => gwCard.BackColor = Color.FromArgb(220, 38, 44, 52);
                gwCard.MouseLeave += (s, e) => gwCard.BackColor = Color.FromArgb(170, 28, 33, 40);
                gwCard.Click += (s, e) => { Clipboard.SetText(gw.Gateway); AddLog($"📋 已复制网关: {gw.Gateway}"); };

                gwCard.Controls.Add(new Label { Text = "默认网关", Location = new Point(10, 4), AutoSize = true, Font = new Font("Segoe UI", 7.5F, FontStyle.Bold), ForeColor = ACCENT_ORANGE });
                gwCard.Controls.Add(new Label { Text = gw.Gateway, Location = new Point(10, 22), AutoSize = true, Font = new Font("Consolas", 10F, FontStyle.Bold), ForeColor = TEXT_PRIMARY });
                gwCard.Controls.Add(new Label { Text = gw.Interface.Truncate(24), Location = new Point(10, 44), AutoSize = true, Font = new Font("Segoe UI", 7.5F), ForeColor = Color.FromArgb(160, 139, 148, 158) });

                myIPv6Container.Controls.Add(gwCard);
            }

            // 空状态
            if (added == 0 && myIPv6Container.Controls.Count == 0)
            {
                myIPv6Container.Controls.Add(new Label
                {
                    Text = "  ⚠ 未检测到 IPv6 地址",
                    AutoSize = true,
                    Font = new Font("Segoe UI", 11F),
                    ForeColor = TEXT_MUTED,
                    Margin = new Padding(10, 10, 0, 0)
                });
            }

            // 入场动画：卡片依次淡入滑上
            int idx = 0;
            foreach (Control c in myIPv6Container.Controls)
            {
                if (c is Panel p)
                {
                    int originTop = p.Top;
                    p.Top += 15;
                    int targetTop = originTop;
                    int delay = idx * 40;
                    int cur = idx;
                    _ = AnimateSlide(p, originTop + 15, targetTop, delay);
                    idx++;
                }
            }
        }
        catch (Exception ex)
        {
            AddLog($"❌ 获取 IPv6 地址失败: {ex.Message}");
        }
    }

    // ============================================================
    // IPv6 连通性检测
    // ============================================================
    private async Task CheckConnectivityAsync()
    {
        try
        {
            if (connIndicator == null || connIndicator.IsDisposed) return;

            var status = await _data.CheckConnectivityAsync();

            if (connIndicator.InvokeRequired)
            {
                connIndicator.Invoke(() => _ = CheckConnectivityAsync());
                return;
            }

            connIndicator.ForeColor = status.HasConnectivity ? ACCENT_GREEN : ACCENT_RED;
            connIndicator.Text = status.HasConnectivity ? "●" : "○";
            connText.Text = status.HasConnectivity
                ? $"IPv6 已连接{(status.PublicIP != null ? $" | {status.PublicIP}" : "")}"
                : "IPv6 未连接";
            connText.ForeColor = status.HasConnectivity ? ACCENT_GREEN : ACCENT_RED;

            if (status.PublicIP != null)
            {
                AddLog($"🌐 公网 IPv6: {status.PublicIP}");
            }

            // 更新托盘图标
            if (trayIcon != null)
            {
                trayIcon.Text = status.HasConnectivity
                    ? $"IPv6 已连接 | {status.PublicIP ?? "无公网IP"}"
                    : "IPv6 未连接";
            }
        }
        catch { }
    }

    private void AddMetric(string title, string val, Color c)
    {
        Panel p = new Panel { Size = new Size(170, 88), BackColor = Color.FromArgb(180, 28, 33, 40), Margin = new Padding(0, 8, 16, 0) };
        p.Controls.Add(new Label { Text = title, Location = new Point(14, 8), AutoSize = true, Font = new Font("Segoe UI", 8.5F), ForeColor = Color.FromArgb(180, 139, 148, 158) });
        p.Controls.Add(new Label { Text = val, Location = new Point(12, 30), AutoSize = true, Font = new Font("Segoe UI", 26F, FontStyle.Bold), ForeColor = c });
        p.Paint += (s, e) =>
        {
            using Pen border = new Pen(Color.FromArgb(60, c), 1);
            e.Graphics.DrawRectangle(border, 0, 0, p.Width - 1, p.Height - 1);
            using Pen top = new Pen(Color.FromArgb(30, 255, 255, 255));
            e.Graphics.DrawLine(top, 2, 1, p.Width - 3, 1);
        };
        p.MouseEnter += (s, e) => p.BackColor = Color.FromArgb(210, 38, 44, 52);
        p.MouseLeave += (s, e) => p.BackColor = Color.FromArgb(180, 28, 33, 40);
        dashCardContainer.Controls.Add(p);
    }

    private void AddLog(string msg) { if (dashEventLog == null || dashEventLog.IsDisposed) return; if (dashEventLog.InvokeRequired) { dashEventLog.Invoke(() => AddLog(msg)); return; } dashEventLog.Items.Insert(0, $"[{DateTime.Now:HH:mm:ss}] {msg}"); while (dashEventLog.Items.Count > 200) dashEventLog.Items.RemoveAt(199); }

    // ============================================================
    // 数据填充方法（从 _data 读取，不再执行 PowerShell）
    // ============================================================
    private void PopulateAdapters()
    {
        var rows = _data.Bindings.Select(b => new[] { b.Name, b.Enabled ? "✅ 启用" : "❌ 禁用", b.IfIndex.ToString(), b.Status, b.Desc }).ToArray();
        SetLV(adapterListView, rows);
        adapterStatusLabel.Text = $"共 {rows.Length} 适配器";
        AddLog($"✓ 适配器: {rows.Length} 个");
    }

    private void PopulateAddresses()
    {
        addressTreeView.BeginUpdate(); addressTreeView.Nodes.Clear();
        foreach (var g in _data.Addresses.GroupBy(a => a.Interface))
        {
            var n = new TreeNode($"📡 {g.Key} ({g.Count()} 地址)") { ForeColor = ACCENT_CYAN };
            foreach (var a in g) n.Nodes.Add(new TreeNode($"{a.IP} /{a.PrefixLen}  [{a.State}]") { ForeColor = TEXT_PRIMARY });
            addressTreeView.Nodes.Add(n);
        }
        addressTreeView.ExpandAll(); addressTreeView.EndUpdate();
        AddLog($"✓ IPv6地址: {_data.Addresses.Count} 个");
    }

    private void PopulateDns()
    {
        var rows = _data.DnsServers.Select(d => new[] { d.Interface, string.Join(", ", d.Servers), d.Servers.Length > 0 ? "配置" : "无" }).ToArray();
        SetLV(dnsListView, rows);
        AddLog($"✓ DNS: {rows.Length} 项");
    }

    private void PopulateRoutes()
    {
        var rows = _data.Routes.Select(r => new[] { r.Prefix, r.Interface, r.Metric.ToString(), r.NextHop }).ToArray();
        SetLV(routeListView, rows);
        AddLog($"✓ 路由: {rows.Length} 条");
    }

    private async Task PopulateTunnels()
    {
        string t = await IPv6DataService.RunPsAsync("netsh int teredo show state", cache: false);
        string s = await IPv6DataService.RunPsAsync("netsh int 6to4 show state", cache: false);
        string i = await IPv6DataService.RunPsAsync("netsh int isatap show state", cache: false);
        SetLV(tunnelListView, new[] {
            new[] { "Teredo", "Teredo 隧道", ParseTunnelState(t), "-", "-" },
            new[] { "6to4", "6to4 隧道", ParseTunnelState(s), "-", "-" },
            new[] { "ISATAP", "ISATAP 隧道", ParseTunnelState(i), "-", "-" },
        });
        AddLog("✓ 隧道状态已加载");
    }

    private string ParseTunnelState(string raw) => raw.Contains("online", StringComparison.OrdinalIgnoreCase) || raw.Contains("enabled", StringComparison.OrdinalIgnoreCase) ? "✅ 可用" : "❌ 不可用";

    private async Task PopulateFirewall()
    {
        string dir = firewallDirectionCombo?.SelectedItem?.ToString() ?? "全部";
        string dirFilter = dir switch { "入站" => "|? Direction -eq 'Inbound'", "出站" => "|? Direction -eq 'Outbound'", _ => "" };
        string raw = await IPv6DataService.RunPsAsync($"Get-NetFirewallRule -AddressFamily IPv6{dirFilter}|Select DisplayName,Direction,Action,Protocol,LocalPort,RemotePort|ConvertTo-Json -Compress", cache: false);
        var list = new List<string[]>();
        foreach (var e in ToElements(raw))
        {
            string lp = e.TryGetProperty("LocalPort", out var p) ? PortValue(p) : "";
            string rp = e.TryGetProperty("RemotePort", out var pr) ? PortValue(pr) : "";
            list.Add(new[] { S(e, "DisplayName").Truncate(50), S(e, "Direction"), S(e, "Action"), S(e, "Protocol"), lp, rp });
        }
        SetLV(firewallListView, list.ToArray());
        AddLog($"✓ 防火墙: {list.Count} 条");
    }

    private string PortValue(JsonElement e) => e.ValueKind == JsonValueKind.Array ? string.Join(",", e.EnumerateArray().Select(x => x.GetString() ?? "")) : e.GetString() ?? "";

    private async Task PopulateNetworkScanAsync()
    {
        string raw = await IPv6DataService.RunPsAsync("Get-NetNeighbor -AddressFamily IPv6|Select IPAddress,LinkLayerAddress,InterfaceAlias,State|ConvertTo-Json -Compress", cache: false);
        var rows = _data.Neighbors.Select(n => new[] { n.IP, n.MAC, n.Interface, n.State, "NDP" }).ToArray();
        SetLV(scanNeighborList, rows);
        AddLog($"✓ 网络扫描: {rows.Length} 邻居");
    }

    private async Task ScanMulticastAsync() { ShowLoading("多播 Ping..."); await IPv6DataService.RunPsAsync("ping -6 -n 3 ff02::1", cache: false); HideLoading(); AddLog("📡 多播 Ping 完成"); ResetDetailCache(8); }
    private async Task ScanConnectionsAsync() { ShowLoading("活动连接..."); string r = await _data.NetStatAsync(); HideLoading(); connResultBox.Text = r; AddLog("🔗 连接扫描完成"); }

    // ============================================================
    // 连接测试
    // ============================================================
    private async Task RunPingTest()
    {
        string tgt = connTargetCombo.Text.Trim(); if (string.IsNullOrEmpty(tgt)) return;
        ShowLoading($"Ping {tgt}...");
        connResultBox.Text = $"Ping: {tgt}\n{new string('─', 50)}\n{await _data.PingAsync(tgt)}";
        HideLoading(); AddLog($"📶 Ping {tgt}");
    }

    private async Task RunTracert()
    {
        string tgt = connTargetCombo.Text.Trim(); if (string.IsNullOrEmpty(tgt)) return;
        ShowLoading($"Tracert {tgt}...");
        connResultBox.Text = $"Tracert: {tgt}\n{new string('─', 50)}\n{await _data.TracertAsync(tgt)}";
        HideLoading(); AddLog($"🗺 Tracert {tgt}");
    }

    private async Task ExecToolAsync(string cmd, string name) { ShowLoading(name); string r = await _data.ExecToolAsync(cmd); HideLoading(); ShowResultDialog(name, r); AddLog($"[{name}] 完成"); }

    // ============================================================
    // 适配器操作
    // ============================================================
    private void ToggleSelectedAdapter(bool enable)
    {
        if (adapterListView.SelectedItems.Count == 0) { MessageBox.Show("请先选中适配器"); return; }
        string n = adapterListView.SelectedItems[0].Text;
        IPv6DataService.RunAdmin($"{(enable ? "Enable" : "Disable")}-NetAdapterBinding -Name '{n}' -ComponentID ms_tcpip6");
        AddLog($"⚠ {(enable ? "启用" : "禁用")} {n}");
    }

    // ============================================================
    // 报告
    // ============================================================
    private async Task GenerateReport()
    {
        ShowLoading("生成报告...");
        var sb = new StringBuilder();
        sb.AppendLine("=== IPv6 配置报告 ===");
        sb.AppendLine($"时间: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
        sb.AppendLine(new string('=', 60));
        sb.AppendLine($"\n[适配器] {_data.Bindings.Count}个, 启用{_data.Bindings.Count(b=>b.Enabled)}个");
        foreach (var b in _data.Bindings) sb.AppendLine($"  {b.Name}: {(b.Enabled?"启用":"禁用")} [{b.Desc}]");
        sb.AppendLine($"\n[地址] {_data.Addresses.Count}个");
        foreach (var a in _data.Addresses.Take(50)) sb.AppendLine($"  {a.Interface}: {a.IP}/{a.PrefixLen}");
        sb.AppendLine($"\n[DNS] {_data.DnsServers.Count}项");
        foreach (var d in _data.DnsServers) sb.AppendLine($"  {d.Interface}: {string.Join(",", d.Servers)}");
        sb.AppendLine($"\n[路由] {_data.Routes.Count}条");
        foreach (var r in _data.Routes.Take(30)) sb.AppendLine($"  {r.Prefix} -> {r.NextHop} [{r.Metric}]");
        sb.AppendLine($"\n[全局配置]\n{await _data.ExecToolAsync("netsh int ipv6 show global")}");
        _cachedReport = sb.ToString();
        reportTextBox.Text = _cachedReport;
        HideLoading(); AddLog("✓ 报告生成完成");
    }
    private void CopyReport() { if (string.IsNullOrEmpty(_cachedReport)) { MessageBox.Show("请先生成报告"); return; } Clipboard.SetText(_cachedReport); AddLog("📋 已复制"); }
    private void SaveReport()
    {
        if (string.IsNullOrEmpty(_cachedReport)) { MessageBox.Show("请先生成报告"); return; }
        using var dlg = new SaveFileDialog { FileName = $"IPv6_Report_{DateTime.Now:yyyyMMdd}.txt", Filter = "文本|*.txt" };
        if (dlg.ShowDialog() == DialogResult.OK) { File.WriteAllText(dlg.FileName, _cachedReport, Encoding.UTF8); AddLog($"💾 已保存: {dlg.FileName}"); }
    }

    // ============================================================
    // 高级工具
    // ============================================================
    private void ShowResultDialog(string title, string content)
    {
        var f = new Form { Text = title, Size = new Size(750, 500), StartPosition = FormStartPosition.CenterParent, BackColor = BG_DARK, ForeColor = TEXT_PRIMARY };
        var tb = new TextBox { Multiline = true, ReadOnly = true, ScrollBars = ScrollBars.Both, Dock = DockStyle.Fill, BackColor = Color.FromArgb(10, 14, 20), ForeColor = Color.FromArgb(180, 200, 220), Font = new Font("Consolas", 9.5F), BorderStyle = BorderStyle.None, Text = content };
        var cb = new Button { Text = "关闭", Dock = DockStyle.Bottom, Height = 30, FlatStyle = FlatStyle.Flat, BackColor = BG_CARD, ForeColor = TEXT_PRIMARY };
        cb.Click += (s, e) => f.Close(); f.Controls.Add(tb); f.Controls.Add(cb); f.ShowDialog(this);
    }

    // ============================================================
    // 加载遮罩
    // ============================================================
    void ShowLoading(string text) { if (loadingOverlay.InvokeRequired) { loadingOverlay.Invoke(() => ShowLoading(text)); return; } if (loadingOverlay.Controls.Count > 1 && loadingOverlay.Controls[1] is Label l) l.Text = text; loadingOverlay.BringToFront(); loadingOverlay.Visible = true; }
    void HideLoading() { if (loadingOverlay.InvokeRequired) { loadingOverlay.Invoke(HideLoading); return; } loadingOverlay.Visible = false; }

    // ============================================================
    // 检测模式
    // ============================================================
    private async void OnCheckModeLoad(object sender, EventArgs e)
    {
        _countdownSeconds = 5; _cmProgress.Value = 50;
        try
        {
            await _data.RefreshAllAsync();
            int disabled = _data.Bindings.Count(b => !b.Enabled);
            if (disabled > 0)
            {
                _cmStatusLbl.Text = "⚠ IPv6 未完全启用"; _cmStatusLbl.ForeColor = ACCENT_ORANGE;
                _cmDetailLbl.Text = $"{disabled}/{_data.Bindings.Count} 个未启用，正在开启...";
                await Task.Delay(300);
                IPv6DataService.RunAdmin("Enable-NetAdapterBinding -Name '*' -ComponentID ms_tcpip6");
                await Task.Delay(600);
                _cmStatusLbl.Text = "✅ 已自动启用"; _cmStatusLbl.ForeColor = ACCENT_GREEN;
            }
            else if (_data.Bindings.Count > 0) { _cmStatusLbl.Text = "✅ 全部已启用"; _cmStatusLbl.ForeColor = ACCENT_GREEN; _cmDetailLbl.Text = $"{_data.Bindings.Count} 适配器正常"; }
            else { _cmStatusLbl.Text = "❓ 无法检测"; _cmStatusLbl.ForeColor = ACCENT_RED; }
        }
        catch (Exception ex) { _cmStatusLbl.Text = "❌ 错误"; _cmStatusLbl.ForeColor = ACCENT_RED; _cmDetailLbl.Text = ex.Message; }
        for (int i = _countdownSeconds; i > 0; i--) { _cmCountdownLbl.Text = $"{i} 秒后关闭"; _cmProgress.Value = i * 10; await Task.Delay(1000); }
        Application.Exit();
    }

    private void SetStartupCheck(bool enable)
    {
        try
        {
            using var rk = Microsoft.Win32.Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\Run", true);
            if (rk == null) return;
            if (enable) rk.SetValue("IPv6Manager", $"\"{Application.ExecutablePath}\" --check");
            else if (rk.GetValue("IPv6Manager") != null) rk.DeleteValue("IPv6Manager");
            AddLog(enable ? "✅ 已启用开机自启" : "✅ 已关闭开机自启");
            MessageBox.Show(enable ? "已启用开机检测" : "已关闭开机检测", "设置", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }
        catch (Exception ex) { MessageBox.Show("设置失败: " + ex.Message); }
    }

    private bool IsStartupEnabled()
    {
        try
        {
            using var rk = Microsoft.Win32.Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\Run", false);
            return rk?.GetValue("IPv6Manager") != null;
        }
        catch { return false; }
    }

    private void ToggleAutoStart()
    {
        bool en = IsStartupEnabled();
        if (MessageBox.Show(en ? "当前已启用，是否关闭？" : "是否启用开机自动检测？\n每次开机会自动检查并启用 IPv6", "开机自启", MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
            SetStartupCheck(!en);
    }

    // ============================================================
    // 更新检测
    // ============================================================
    private async Task CheckForUpdatesAsync()
    {
        btnUpdate.Text = "⏳"; btnUpdate.Enabled = false;
        try
        {
            using var h = new HttpClient(); h.DefaultRequestHeaders.Add("User-Agent", "ipv6-tool"); h.Timeout = TimeSpan.FromSeconds(8);
            string json = await h.GetStringAsync($"https://api.github.com/repos/{GITHUB_REPO}/releases/latest");
            using var d = JsonDocument.Parse(json);
            string tag = d.RootElement.TryGetProperty("tag_name", out var t) ? t.GetString() ?? "" : "";
            if (string.IsNullOrEmpty(tag)) { MessageBox.Show("无法获取版本信息"); return; }
            if (tag == CURRENT_VERSION) MessageBox.Show($"已是最新 ({CURRENT_VERSION})");
            else if (MessageBox.Show($"新版本: {tag}\n当前: {CURRENT_VERSION}\n打开下载页？", "更新", MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
                Process.Start(new ProcessStartInfo { FileName = d.RootElement.TryGetProperty("html_url", out var u) ? u.GetString() ?? "" : "", UseShellExecute = true });
        }
        catch { MessageBox.Show("无法连接 GitHub"); }
        finally { btnUpdate.Text = "⬇"; btnUpdate.Enabled = true; }
    }

    // ============================================================
    // ListView 数据设置
    // ============================================================
    private void SetLV(ListView lv, string[][] rows)
    {
        lv.BeginUpdate(); lv.Items.Clear();
        foreach (var r in rows) { var item = new ListViewItem(r[0]); for (int i = 1; i < r.Length; i++) item.SubItems.Add(r[i]); lv.Items.Add(item); }
        lv.EndUpdate();
        if (lv.Items.Count > 0) lv.AutoResizeColumns(ColumnHeaderAutoResizeStyle.HeaderSize);
    }

    // ============================================================
    // JSON 辅助
    // ============================================================
    static List<JsonElement> ToElements(string json)
    {
        var l = new List<JsonElement>();
        try { using var d = JsonDocument.Parse(json.Trim()); if (d.RootElement.ValueKind == JsonValueKind.Array) foreach (var e in d.RootElement.EnumerateArray()) l.Add(e.Clone()); } catch { }
        return l;
    }
    static string S(JsonElement e, string k) => e.TryGetProperty(k, out var p) ? p.GetString() ?? "" : "";
    static int I(JsonElement e, string k) => e.TryGetProperty(k, out var p) && p.TryGetInt32(out int v) ? v : 0;
}

// ============================================================
// String 扩展
// ============================================================
public static class StringExtensions
{
    public static string Truncate(this string v, int max) => v != null && v.Length > max ? v[..max] + "..." : v ?? "";
}
