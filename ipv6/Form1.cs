// ============================================================
// Form1.cs — 业务逻辑 v2.1
// ============================================================

#nullable disable
#pragma warning disable CS4014 // 故意使用 fire-and-forget 异步调用

using System.Diagnostics;
using System.Net;
using System.Net.Http;
using System.Net.NetworkInformation;
using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;

namespace ipv6;

public partial class Form1 : Form
{
    private const string StartupTaskName = "IPv6ManagerCheck";

    private readonly bool _checkMode;
    private readonly IPv6DataService _data = new();
    private readonly bool[] _detailLoaded = new bool[PAGE_COUNT];
    private readonly Dictionary<Color, Pen> _metricPens = new();

    private bool _dataLoading;
    private int _viewVersion;
    private string _cachedReport = "";
    private List<IPv6DataService.HealthItem> _health = new();
    private List<IPv6DataService.FirewallDetail> _fwDetails = new();
    private bool _fwDetailLoaded;

    private IPv6DataService.ConnectivityStatus _lastConn;
    private bool _monitoring;
    private bool _monitorBusy;
    private System.Windows.Forms.Timer _monitorTimer;
    private string _lastPublicIP;
    private bool _lastConnectedKnown;
    private bool _lastConnected;

    private int _metricSeq;

    // ============================================================
    // 构造 & 窗口
    // ============================================================
    public Form1(bool checkMode = false)
    {
        _checkMode = checkMode;
        InitializeComponent();

        IPv6DataService.ErrorOccurred += OnDataError;
        FormClosed += (s, e) =>
        {
            IPv6DataService.ErrorOccurred -= OnDataError;
            try { _monitorTimer?.Stop(); _monitorTimer?.Dispose(); } catch { }
            try { if (trayIcon != null) trayIcon.Visible = false; } catch { }
        };
        FormClosing += (s, e) => { try { trayIcon.Visible = false; } catch { } };

        if (_checkMode) { BuildCheckUI(); Load += OnCheckModeLoad; }
        else { BuildCompleteUI(); Load += OnFormLoad; }
    }

    private void OnDataError(string message) => AddLog("⚠ " + message);

    protected override void WndProc(ref Message m)
    {
        if (m.Msg == 0x84) // WM_NCHITTEST
        {
            base.WndProc(ref m);
            if (m.Result == (nint)1)
            {
                Point pt = PointToClient(Cursor.Position);
                if (pt.Y <= TITLEBAR_HEIGHT && pt.X < Width - 140) { m.Result = (nint)2; return; }

                int edge = 8;
                if (pt.X <= edge && pt.Y <= edge) m.Result = (nint)13;
                else if (pt.X >= Width - edge && pt.Y <= edge) m.Result = (nint)14;
                else if (pt.X <= edge && pt.Y >= Height - edge) m.Result = (nint)16;
                else if (pt.X >= Width - edge && pt.Y >= Height - edge) m.Result = (nint)17;
                else if (pt.X <= edge) m.Result = (nint)10;
                else if (pt.X >= Width - edge) m.Result = (nint)11;
                else if (pt.Y <= edge) m.Result = (nint)12;
                else if (pt.Y >= Height - edge) m.Result = (nint)15;
            }
            return;
        }
        base.WndProc(ref m);
    }

    protected override void OnResize(EventArgs e)
    {
        base.OnResize(e);
        if (!_checkMode) ApplyRoundedCorners();
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
        ApplyRoundedCorners();
    }

    private void BtnUpdate_Click(object s, EventArgs e) => _ = CheckForUpdatesAsync();

    // ============================================================
    // UI 线程辅助
    // ============================================================
    private void RunOnUi(Action action)
    {
        if (IsDisposed) return;
        try
        {
            if (InvokeRequired) BeginInvoke(action);
            else action();
        }
        catch (ObjectDisposedException) { }
        catch (InvalidOperationException) { }
    }

    private static void ClearControls(Control parent)
    {
        if (parent == null) return;
        while (parent.Controls.Count > 0)
        {
            Control c = parent.Controls[0];
            parent.Controls.RemoveAt(0);
            c.Dispose();
        }
    }

    private static string Trunc(string text, int max) =>
        string.IsNullOrEmpty(text) ? "" : text.Length <= max ? text : text[..max] + "…";

    private static string Brief(string output)
    {
        if (string.IsNullOrWhiteSpace(output)) return "未知错误";
        string t = output.Trim();
        if (t.StartsWith("OK|", StringComparison.Ordinal)) t = t[3..].Trim();
        else if (t.StartsWith("ERR|", StringComparison.Ordinal)) t = t[4..].Trim();
        string[] lines = t.Replace("\r", "").Split('\n');
        string line = lines.FirstOrDefault(x => !string.IsNullOrWhiteSpace(x))?.Trim() ?? t;
        return line.Length > 300 ? line[..300] : line;
    }

    private void TryCopy(string text)
    {
        bool ok = TryCopyText(text);
        AddLog(ok ? $"📋 已复制: {text}" : "⚠ 复制失败（剪贴板被占用）");
        if (ok) ToastFx.Show(this, "已复制：" + Trunc(text, 28));
    }

    private void CopyWithFx(string text, Control host, Point origin)
    {
        bool ok = TryCopyText(text);
        if (!ok)
        {
            AddLog("⚠ 复制失败（剪贴板被占用）");
            return;
        }
        AddLog($"📋 已复制: {text}");
        ConfettiFx.Play(host, origin);
        ToastFx.Show(this, "已复制：" + Trunc(text, 26));
    }

    private static bool TryCopyText(string text)
    {
        for (int i = 0; i < 3; i++)
        {
            try { Clipboard.SetText(text); return true; }
            catch { System.Threading.Thread.Sleep(60); }
        }
        return false;
    }

    // ============================================================
    // 页面切换动画（cubic-bezier 非线性）
    // ============================================================
    private void PlayTilesEntrance()
    {
        if (tileContainer == null || tileContainer.IsDisposed) return;
        int i = 0;
        foreach (Control c in tileContainer.Controls)
        {
            if (c is AnimatedTile tile)
            {
                tile.PlayEntrance(i * 38); // 子元素错开动画
                i++;
            }
        }
    }

    private async void SwitchToDetail(int index)
    {
        if (detailPages == null || index < 0 || index >= detailPages.Length) return;
        int version = ++_viewVersion;

        tileContainer.Visible = false;
        if (currentDetail != null && currentDetail.Parent == contentPanel)
        {
            contentPanel.Controls.Remove(currentDetail);
            currentDetail.Visible = false;
        }

        Panel dp = detailPages[index];
        Rectangle area = contentPanel.DisplayRectangle;
        dp.Bounds = new Rectangle(area.Right, area.Y, area.Width, area.Height);
        dp.Visible = true;
        contentPanel.Controls.Add(dp);
        dp.BringToFront();
        currentDetail = dp;
        activeDetailIndex = index;

        // 右侧滑入 + cubic-bezier(0.22, 1, 0.36, 1)
        const int steps = 18;
        for (int i = 1; i <= steps; i++)
        {
            if (version != _viewVersion) return;
            double t = Ease.PageIn(i / (double)steps);
            dp.Left = area.Right - (int)(area.Width * t);
            if (i % 2 == 0) await Task.Delay(10);
        }
        dp.Left = area.X;

        pageHeaders[index]?.Play(); // 文字渐变流动 + 揭示 + 打字机 + 下划线

        if (!_detailLoaded[index])
        {
            _detailLoaded[index] = true;
            _ = LoadDetailDataAsync(index);
        }
    }

    private async void SwitchToTileView()
    {
        int version = ++_viewVersion;
        if (currentDetail != null && currentDetail.Parent == contentPanel)
        {
            Panel dp = currentDetail;
            Rectangle area = contentPanel.DisplayRectangle;
            const int steps = 14;
            for (int i = 1; i <= steps; i++)
            {
                if (version != _viewVersion) return;
                double t = Ease.PageOut(i / (double)steps);
                dp.Left = area.X + (int)(area.Width * t);
                if (i % 2 == 0) await Task.Delay(10);
            }
            contentPanel.Controls.Remove(dp);
            dp.Visible = false;
            dp.Bounds = area;
        }
        tileContainer.Visible = true;
        currentDetail = null;
        activeDetailIndex = -1;
        PlayTilesEntrance();
    }

    private async Task LoadDetailDataAsync(int idx)
    {
        switch (idx)
        {
            case 0: await EnsureDataAsync(); PopulateDashboard(); break;
            case 1: await EnsureDataAsync(); PopulateAdapters(); break;
            case 2: await EnsureDataAsync(); PopulateAddresses(); break;
            case 3: await EnsureDataAsync(); PopulateDns(); break;
            case 5: await EnsureDataAsync(); PopulateRoutes(); break;
            case 6: await PopulateTunnelsAsync(); break;
            case 7: await EnsureDataAsync(); ApplyFirewallFilter(); break;
            case 8: await EnsureDataAsync(); PopulateScan(); break;
            case 11: await RunHealthCheckAsync(); break;
            case 12: await PopulateListenersAsync(); break;
        }
    }

    private async Task EnsureDataAsync(bool force = false)
    {
        if (_data.IsLoaded && !force) return;
        if (_dataLoading)
        {
            for (int i = 0; i < 120 && _dataLoading; i++) await Task.Delay(100);
            return;
        }

        _dataLoading = true;
        try
        {
            await _data.RefreshAllAsync(s => RunOnUi(() => AddLog(s)));
            RunOnUi(() => AddLog($"✓ 数据已刷新：{_data.Bindings.Count} 适配器 / {_data.Addresses.Count} 地址 / {_data.Routes.Count} 路由"));
        }
        finally
        {
            _dataLoading = false;
        }
    }

    private async Task RefreshAndReloadAsync(int idx)
    {
        await EnsureDataAsync(force: true);
        if (idx == 7) _fwDetailLoaded = false;
        await LoadDetailDataAsync(idx);
    }

    // ============================================================
    // Form Load
    // ============================================================
    private async void OnFormLoad(object sender, EventArgs e)
    {
        ApplyRoundedCorners();

        // 窗口淡入（非线性）
        Opacity = 0;
        await Anim.RunAsync(Ease.OutCubic, 340, p => { if (!IsDisposed) Opacity = p; });
        Opacity = 1;

        PlayTilesEntrance();
        _ = EnsureDataAsync();
        _ = CheckConnectivityAsync();
    }

    // ============================================================
    // 仪表盘
    // ============================================================
    private void PopulateDashboard()
    {
        if (dashCardContainer == null) return;
        int total = _data.Bindings.Count;
        int enabled = _data.Bindings.Count(b => b.Enabled);
        int disabled = total - enabled;

        ClearControls(dashCardContainer);
        _metricSeq = 0;
        AddMetric("总适配器", total.ToString(), TEXT_PRIMARY);
        AddMetric("已启用", enabled.ToString(), ACCENT_GREEN);
        AddMetric("已禁用", disabled.ToString(), ACCENT_RED);
        AddMetric("启用率", total > 0 ? $"{enabled * 100 / total}%" : "0%", ACCENT_BLUE);
        AddMetric("IPv6 地址", _data.Addresses.Count.ToString(), ACCENT_CYAN);

        PopulateMyIPv6Cards();
        AddLog($"✓ 仪表盘：{total} 适配器 / {_data.Addresses.Count} 地址 / {_data.Routes.Count} 路由");
    }

    private void AddMetric(string title, string value, Color color)
    {
        Panel p = new Panel { Size = new Size(170, 88), BackColor = Color.FromArgb(180, 28, 33, 40), Margin = new Padding(0, 8, 16, 0) };
        p.Controls.Add(new Label { Text = title, Location = new Point(14, 8), AutoSize = true, Font = FMetricTitle, ForeColor = TEXT_SECONDARY });
        var valueLabel = new Label { Text = value, Location = new Point(12, 28), AutoSize = true, Font = FValue, ForeColor = color };
        p.Controls.Add(valueLabel);

        bool hovered = false;
        p.Paint += (s, e) =>
        {
            if (!_metricPens.TryGetValue(color, out var border))
                _metricPens[color] = border = new Pen(Color.FromArgb(60, color), 1);
            e.Graphics.DrawRectangle(border, 0, 0, p.Width - 1, p.Height - 1);
            e.Graphics.DrawLine(PenCardTop, 2, 1, p.Width - 3, 1);
            if (hovered)
            {
                using var glow = new Pen(Color.FromArgb(110, color), 1);
                e.Graphics.DrawRectangle(glow, 1, 1, p.Width - 3, p.Height - 3);
            }
        };
        p.MouseEnter += (s, e) =>
        {
            hovered = true;
            p.BackColor = Color.FromArgb(210, 38, 44, 52);
            p.Invalidate();
        };
        p.MouseLeave += (s, e) =>
        {
            hovered = false;
            p.BackColor = Color.FromArgb(180, 28, 33, 40);
            p.Invalidate();
        };
        dashCardContainer.Controls.Add(p);

        // 数字滚动（非线性）
        bool percent = value.EndsWith("%", StringComparison.Ordinal);
        if (int.TryParse(value.TrimEnd('%'), out int target))
        {
            valueLabel.Text = percent ? "0%" : "0";
            _ = AnimateMetricAsync(valueLabel, target, percent, _metricSeq++ * 55);
        }
    }

    private static async Task AnimateMetricAsync(Label label, int target, bool percent, int delayMs)
    {
        if (delayMs > 0) await Task.Delay(delayMs);
        await Anim.RunAsync(Ease.OutCubic, 820, p =>
        {
            if (label.IsDisposed) return;
            int v = (int)Math.Round(target * Math.Clamp(p, 0, 1));
            label.Text = percent ? v + "%" : v.ToString();
        });
        try { if (!label.IsDisposed) label.Text = percent ? target + "%" : target.ToString(); } catch { }
    }

    private void PopulateMyIPv6Cards()
    {
        if (myIPv6Container == null) return;
        ClearControls(myIPv6Container);

        foreach (var a in _data.MyAddresses.Where(x => x.Type != "多播"))
        {
            Color color = TypeColor(a.Type);
            myIPv6Container.Controls.Add(MakeCopyCard(
                a.Type, a.IP, $"/{a.PrefixLen}  {Trunc(a.Interface, 18)}  {MapAddressState(a.State)}", color));
        }

        foreach (var gw in _data.DefaultGateways.Take(3))
        {
            myIPv6Container.Controls.Add(MakeCopyCard(
                "默认网关", gw.Gateway, Trunc(gw.Interface, 24), ACCENT_ORANGE));
        }

        if (myIPv6Container.Controls.Count == 0)
        {
            myIPv6Container.Controls.Add(new Label
            {
                Text = "  ⚠ 未检测到 IPv6 地址（请确认适配器已启用 IPv6）",
                AutoSize = true,
                Font = FSmall,
                ForeColor = TEXT_MUTED,
                Margin = new Padding(10, 10, 0, 0),
            });
        }

        try { myIPv6Container.AutoScrollPosition = new Point(0, 0); } catch { }
    }

    private Panel MakeCopyCard(string tag, string value, string extra, Color color)
    {
        Panel card = new Panel
        {
            Size = new Size(296, 60),
            BackColor = BG_CARD,
            Margin = new Padding(0, 2, 10, 2),
            Cursor = Cursors.Hand,
        };

        bool hovered = false;
        card.Paint += (s, e) =>
        {
            e.Graphics.DrawLine(AccentPen(color), 0, 0, 0, 60);
            if (hovered)
            {
                using var border = new Pen(Color.FromArgb(90, color), 1);
                e.Graphics.DrawRectangle(border, 1, 1, card.Width - 3, card.Height - 3);
            }
        };

        void Copy() => CopyWithFx(value, card, new Point(card.Width / 2, 30));

        Label l1 = new Label { Text = tag, Location = new Point(12, 4), AutoSize = true, Font = FTinyBold, ForeColor = color, BackColor = Color.Transparent, Cursor = Cursors.Hand };
        Label l2 = new Label
        {
            Text = value,
            Location = new Point(12, 19),
            AutoSize = false,
            Size = new Size(272, 18),
            Font = FMonoSm,
            ForeColor = TEXT_PRIMARY,
            BackColor = Color.Transparent,
            Cursor = Cursors.Hand,
            AutoEllipsis = true,
        };
        Label l3 = new Label { Text = extra, Location = new Point(12, 40), AutoSize = true, Font = FTiny, ForeColor = TEXT_SECONDARY, BackColor = Color.Transparent, Cursor = Cursors.Hand };

        // 靠近显现：悬停时才滑入“复制”提示
        var hint = new Label
        {
            Text = "复制 →",
            AutoSize = true,
            Font = FTinyBold,
            ForeColor = color,
            BackColor = Color.Transparent,
            Cursor = Cursors.Hand,
            Location = new Point(card.Width, 5),
            Visible = false,
        };
        int hintVersion = 0;
        async void SlideHint(bool show)
        {
            int version = ++hintVersion;
            int width = TextRenderer.MeasureText(hint.Text, hint.Font).Width;
            if (show) hint.Visible = true;
            int targetX = show ? card.Width - width - 10 : card.Width;
            int fromX = hint.Left;
            await Anim.RunAsync(Ease.OutCubic, 190, p =>
            {
                if (version != hintVersion || hint.IsDisposed || card.IsDisposed) return;
                hint.Left = (int)(fromX + (targetX - fromX) * p);
            });
            if (!show && version == hintVersion && !hint.IsDisposed) hint.Visible = false;
        }

        card.MouseEnter += (s, e) =>
        {
            hovered = true;
            card.BackColor = BG_HOVER;
            card.Invalidate();
            SlideHint(true);
        };
        card.MouseLeave += (s, e) =>
        {
            hovered = false;
            card.BackColor = BG_CARD;
            card.Invalidate();
            SlideHint(false);
        };
        card.Click += (s, e) => Copy();

        foreach (Label l in new[] { l1, l2, l3, hint })
        {
            l.Click += (s, e) => Copy();
            card.Controls.Add(l);
        }
        tips?.SetToolTip(card, "点击复制 " + value);
        return card;
    }

    private static string MapAddressState(string state) => state switch
    {
        "Preferred" => "首选",
        "Deprecated" => "已弃用",
        "Tentative" => "临时(DAD)",
        "Duplicate" => "重复",
        "Invalid" => "无效",
        _ => state,
    };

    private static string MapOrigin(string origin) => origin switch
    {
        "Dhcp" => "DHCPv6",
        "RouterAdvertisement" => "路由通告",
        "Manual" => "手动",
        "WellKnown" => "系统默认",
        "LinkLayerAddress" => "基于 MAC",
        "Random" => "随机",
        "Other" => "其他",
        _ => origin,
    };

    private static Color TypeColor(string type) => type switch
    {
        "全局单播" => ACCENT_BLUE,
        "唯一本地" => ACCENT_PURPLE,
        "链路本地" => ACCENT_CYAN,
        "回环" => TEXT_MUTED,
        "多播" => ACCENT_ORANGE,
        "Teredo" or "6to4" => ACCENT_ORANGE,
        _ => ACCENT_GREEN,
    };

    private void AddLog(string msg)
    {
        if (dashEventLog == null || dashEventLog.IsDisposed) return;
        if (dashEventLog.InvokeRequired)
        {
            try { dashEventLog.BeginInvoke(new Action(() => AddLog(msg))); } catch { }
            return;
        }
        dashEventLog.Items.Insert(0, $"[{DateTime.Now:HH:mm:ss}] {msg}");
        while (dashEventLog.Items.Count > 300)
            dashEventLog.Items.RemoveAt(dashEventLog.Items.Count - 1);
    }

    // ============================================================
    // 连通性 / 监控
    // ============================================================
    private async Task CheckConnectivityAsync(bool quiet = false)
    {
        try
        {
            var status = await _data.CheckConnectivityAsync();
            bool changed = _lastConnectedKnown && _lastConnected != status.HasConnectivity;
            _lastConn = status;
            if (!_lastConnectedKnown) { _lastConnectedKnown = true; _lastConnected = status.HasConnectivity; }
            else _lastConnected = status.HasConnectivity;
            if (string.IsNullOrEmpty(_lastPublicIP) && !string.IsNullOrEmpty(status.PublicIP)) _lastPublicIP = status.PublicIP;

            RunOnUi(() =>
            {
                UpdateConnUI(status, changed);
                if (changed) ToastFx.Show(this, status.HasConnectivity ? "IPv6 已恢复连接" : "IPv6 连接已断开");
            });
            if (!quiet)
            {
                AddLog(status.HasConnectivity
                    ? $"🌐 IPv6 已连接{(status.PublicIP != null ? $" | {status.PublicIP}" : "")}{(status.Target != null ? $" | {status.RttMs} ms" : "")}"
                    : "❌ IPv6 未连接");
                if (!string.IsNullOrEmpty(status.Detail)) AddLog("   " + status.Detail);
            }
        }
        catch (Exception ex)
        {
            AddLog("❌ 连通性检测失败: " + ex.Message);
        }
    }

    private void UpdateConnUI(IPv6DataService.ConnectivityStatus status, bool animateChange = false)
    {
        if (connIndicator == null || connIndicator.IsDisposed) return;
        connIndicator.Connected = status.HasConnectivity;
        Color color = status.HasConnectivity ? ACCENT_GREEN : ACCENT_RED;
        connText.SetStatus(status.HasConnectivity ? "IPv6 已连接" : "IPv6 未连接", color, animate: animateChange);
        if (animateChange && !status.HasConnectivity) Anim.Shake(connText);
        tips?.SetToolTip(connIndicator, status.Detail ?? "IPv6 连接状态");
        publicIPLabel.Text = status.PublicIP ?? (status.Target != null ? $"{status.Target} · {status.RttMs}ms" : "");
        publicIPLabel.ForeColor = TEXT_MUTED;

        if (trayIcon != null)
        {
            string tip = status.HasConnectivity ? $"IPv6 已连接 | {status.PublicIP ?? "无公网地址"}" : "IPv6 未连接";
            if (tip.Length > 63) tip = tip[..63];
            try { trayIcon.Text = tip; } catch { }
        }
    }

    private void ToggleMonitor()
    {
        if (_monitoring) StopMonitor();
        else StartMonitor();
    }

    private void StartMonitor()
    {
        _monitorTimer ??= new System.Windows.Forms.Timer { Interval = 60000 };
        _monitorTimer.Tick -= MonitorTick;
        _monitorTimer.Tick += MonitorTick;
        _monitorTimer.Start();
        _monitoring = true;
        UpdateMonitorUi();
        AddLog("📈 网络监控已开启（每 60 秒检查一次）");
    }

    private void StopMonitor()
    {
        try { _monitorTimer?.Stop(); } catch { }
        _monitoring = false;
        UpdateMonitorUi();
        if (dashEventLog != null && !dashEventLog.IsDisposed) AddLog("⏹ 网络监控已停止");
    }

    private void UpdateMonitorUi()
    {
        RunOnUi(() =>
        {
            if (btnMonitor != null && !btnMonitor.IsDisposed)
                btnMonitor.Text = _monitoring ? "\u23f9 停止监控" : "\U0001f4c8 开始监控";
            if (trayMonitorItem != null)
                trayMonitorItem.Text = _monitoring ? "停止网络监控" : "开始网络监控";
        });
    }

    private async void MonitorTick(object sender, EventArgs e)
    {
        if (_monitorBusy) return;
        _monitorBusy = true;
        try
        {
            var status = await _data.CheckConnectivityAsync();
            bool connChanged = _lastConnectedKnown && _lastConnected != status.HasConnectivity;
            bool ipChanged = !string.IsNullOrEmpty(_lastPublicIP) && !string.IsNullOrEmpty(status.PublicIP) && _lastPublicIP != status.PublicIP;

            _lastConnectedKnown = true;
            _lastConnected = status.HasConnectivity;
            if (!string.IsNullOrEmpty(status.PublicIP)) _lastPublicIP = status.PublicIP;
            _lastConn = status;

            RunOnUi(() => UpdateConnUI(status, connChanged));

            if (connChanged || ipChanged)
            {
                string msg = connChanged
                    ? (status.HasConnectivity ? "IPv6 已恢复连接" : "IPv6 连接已断开")
                    : $"公网 IPv6 地址已变化：{status.PublicIP}";
                AddLog("📣 " + msg);
                RunOnUi(() =>
                {
                    try { trayIcon?.ShowBalloonTip(4000, "IPv6 状态变化", msg, ToolTipIcon.Info); } catch { }
                    if (connChanged) ToastFx.Show(this, msg);
                });
            }
        }
        catch { }
        finally
        {
            _monitorBusy = false;
        }
    }

    // ============================================================
    // 适配器
    // ============================================================
    private void PopulateAdapters()
    {
        if (adapterListView == null) return;
        var rows = _data.Bindings.Select(b => new[]
        {
            b.Name,
            b.Enabled ? "启用" : "禁用",
            b.IfIndex.ToString(),
            MapAdapterStatus(b.Status),
            b.Speed,
            b.Desc,
        }).ToArray();
        SetLV(adapterListView, rows);
        if (adapterStatusLabel != null)
            adapterStatusLabel.Text = $"共 {rows.Length} 个适配器，已启用 {rows.Count(r => r[1] == "启用")} 个";
        AddLog($"✓ 适配器：{rows.Length} 个");
    }

    private static string MapAdapterStatus(string status) => status switch
    {
        "Up" => "已连接",
        "Disconnected" => "已断开",
        "Disabled" => "已禁用",
        "Not Present" => "不存在",
        _ => status,
    };

    private async Task ToggleSelectedAdapterAsync(bool enable)
    {
        if (adapterListView == null || adapterListView.SelectedItems.Count == 0)
        {
            MessageBox.Show("请先在列表中选择一个适配器", "提示", MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }

        string name = adapterListView.SelectedItems[0].Text;
        var (ok, output) = await IPv6DataService.RunAdminAsync(
            $"{(enable ? "Enable" : "Disable")}-NetAdapterBinding -Name {IPv6DataService.PsQuote(name)} -ComponentID ms_tcpip6");

        bool success = ok && output.StartsWith("OK|", StringComparison.Ordinal);
        AddLog(success ? $"✓ 已{(enable ? "启用" : "禁用")} {name} 的 IPv6" : $"❌ 操作失败: {Brief(output)}");
        if (!success) MessageBox.Show(Brief(output), "操作失败", MessageBoxButtons.OK, MessageBoxIcon.Warning);
        await RefreshAndReloadAsync(1);
    }

    private async Task EnableAllAsync()
    {
        var (ok, output) = await IPv6DataService.RunAdminAsync("Enable-NetAdapterBinding -Name '*' -ComponentID ms_tcpip6");
        bool success = ok && output.StartsWith("OK|", StringComparison.Ordinal);
        AddLog(success ? "✓ 已启用全部适配器的 IPv6" : $"❌ 启用失败: {Brief(output)}");
        if (!success) MessageBox.Show(Brief(output), "操作失败", MessageBoxButtons.OK, MessageBoxIcon.Warning);
        await RefreshAndReloadAsync(activeDetailIndex >= 0 ? activeDetailIndex : 0);
    }

    private async Task DisableAllAsync()
    {
        if (MessageBox.Show("确定要禁用所有适配器的 IPv6 吗？\n可能导致部分网络或远程连接异常。", "确认",
                MessageBoxButtons.YesNo, MessageBoxIcon.Warning) != DialogResult.Yes) return;

        var (ok, output) = await IPv6DataService.RunAdminAsync("Disable-NetAdapterBinding -Name '*' -ComponentID ms_tcpip6");
        bool success = ok && output.StartsWith("OK|", StringComparison.Ordinal);
        AddLog(success ? "⚠ 已禁用全部适配器的 IPv6" : $"❌ 禁用失败: {Brief(output)}");
        if (!success) MessageBox.Show(Brief(output), "操作失败", MessageBoxButtons.OK, MessageBoxIcon.Warning);
        await RefreshAndReloadAsync(activeDetailIndex >= 0 ? activeDetailIndex : 0);
    }

    // ============================================================
    // 地址
    // ============================================================
    private void PopulateAddresses()
    {
        if (addressTreeView == null) return;
        addressTreeView.BeginUpdate();
        addressTreeView.Nodes.Clear();
        foreach (var group in _data.Addresses.GroupBy(a => a.Interface))
        {
            var node = new TreeNode($"📡 {group.Key}（{group.Count()} 个地址）") { ForeColor = ACCENT_CYAN, Tag = null };
            foreach (var a in group)
            {
                node.Nodes.Add(new TreeNode($"{a.IP} /{a.PrefixLen}   [{a.Type} · {MapAddressState(a.State)} · {MapOrigin(a.Origin)}]")
                {
                    ForeColor = TypeColor(a.Type),
                    Tag = a.IP,
                });
            }
            addressTreeView.Nodes.Add(node);
        }
        addressTreeView.ExpandAll();
        addressTreeView.EndUpdate();
        AddLog($"✓ IPv6 地址：{_data.Addresses.Count} 个");
    }

    // ============================================================
    // DNS
    // ============================================================
    private void PopulateDns()
    {
        if (dnsListView == null) return;
        var rows = _data.DnsServers.Select(d => new[]
        {
            d.Interface,
            d.Servers.Length > 0 ? string.Join(", ", d.Servers) : "（无）",
            d.Servers.Length > 0 ? "已配置" : "无",
        }).ToArray();
        SetLV(dnsListView, rows);
        AddLog($"✓ DNS：{rows.Length} 条适配器记录");
    }

    private async Task ResolveDnsAsync()
    {
        string host = dnsDomainBox?.Text.Trim() ?? "";
        if (!IPv6DataService.IsValidHostName(host) && !IPAddress.TryParse(host, out _))
        {
            MessageBox.Show("请输入合法的域名或地址", "提示", MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }
        ShowLoading($"解析 {host} ...");
        try
        {
            var addrs = await IPv6DataService.ResolveHostAsync(host);
            var v6 = addrs.Where(a => a.Contains(':')).ToList();
            var v4 = addrs.Where(a => !a.Contains(':')).ToList();

            var sb = new StringBuilder();
            sb.AppendLine($"域名: {host}");
            sb.AppendLine(new string('─', 50));
            sb.AppendLine($"AAAA（IPv6）: {(v6.Count == 0 ? "无记录" : string.Join(", ", v6))}");
            sb.AppendLine($"A（IPv4）   : {(v4.Count == 0 ? "无记录" : string.Join(", ", v4))}");
            dnsResultBox.Text = sb.ToString();

            AddLog(v6.Count > 0 ? $"✓ {host} 解析到 {v6.Count} 个 IPv6 地址" : $"⚠ {host} 没有 AAAA 记录");
        }
        finally { HideLoading(); }
    }

    private async Task TestDnsAsync()
    {
        string host = dnsDomainBox?.Text.Trim() ?? "";
        if (!IPv6DataService.IsValidHostName(host))
        {
            MessageBox.Show("DNS 测速需要一个域名", "提示", MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }

        var servers = _data.DnsServers
            .SelectMany(d => d.Servers)
            .Where(s => IPAddress.TryParse(s ?? "", out var ip) && ip.AddressFamily == System.Net.Sockets.AddressFamily.InterNetworkV6)
            .Distinct()
            .ToList();

        bool fallback = false;
        if (servers.Count == 0)
        {
            servers = new List<string> { "2400:3200::1", "2402:4e00::" };
            fallback = true;
        }

        ShowLoading("DNS 测速...");
        try
        {
            var results = await IPv6DataService.TestDnsServersAsync(host, servers);
            var sb = new StringBuilder();
            sb.AppendLine($"DNS 测速 · 域名 {host}（AAAA）");
            sb.AppendLine(new string('─', 60));
            if (fallback) sb.AppendLine("（本机未配置 IPv6 DNS，使用公共 DNS 测试）");
            if (results.Count == 0) sb.AppendLine("没有可用结果（命令执行失败或超时）");
            foreach (var r in results)
            {
                sb.AppendLine(r.Ok
                    ? $"{r.Server,-24}  ✓ {r.Ms,6} ms"
                    : $"{r.Server,-24}  ✗ {r.Ms,6} ms  {Trunc(r.Error, 60)}");
            }
            dnsResultBox.Text = sb.ToString();
            AddLog($"⏱ DNS 测速完成：{results.Count(r => r.Ok)}/{results.Count} 个服务器可用");
        }
        finally { HideLoading(); }
    }

    // ============================================================
    // 路由 / 隧道
    // ============================================================
    private void PopulateRoutes()
    {
        if (routeListView == null) return;
        var rows = _data.Routes.Select(r => new[] { r.Prefix, r.Interface, r.Metric.ToString(), r.NextHop }).ToArray();
        SetLV(routeListView, rows);
        AddLog($"✓ 路由：{rows.Length} 条");
    }

    private async Task PopulateTunnelsAsync()
    {
        if (tunnelListView == null) return;
        var list = await _data.GetTunnelsAsync();
        var rows = list.Select(t => new[]
        {
            t.Kind,
            MapTunnelConfig(t.Kind, t.Type, t.State),
            MapTunnelState(t.State),
            t.Detail,
        }).ToArray();
        SetLV(tunnelListView, rows);
        AddLog(list.Count > 0 ? $"✓ 隧道状态：{list.Count} 项" : "⚠ 未能读取隧道配置");
    }

    private static string MapTunnelConfig(string kind, string type, string state)
    {
        string value = kind == "Teredo" ? type : state;
        return value switch
        {
            "Default" => "系统默认",
            "Disabled" => "已禁用",
            "Enabled" => "已启用",
            "Client" => "客户端",
            "EnterpriseClient" => "企业客户端",
            "Server" => "服务器",
            "" => "-",
            _ => value,
        };
    }

    private static string MapTunnelState(string state) => state switch
    {
        "online" => "在线",
        "offline" => "离线",
        "dormant" => "休眠",
        "disabled" => "已禁用",
        "enabled" => "已启用",
        "Default" => "系统默认",
        "Disabled" => "已禁用",
        "Enabled" => "已启用",
        "-" => "-",
        _ => state,
    };

    // ============================================================
    // 防火墙
    // ============================================================
    private void ApplyFirewallFilter()
    {
        if (firewallListView == null) return;

        string dir = firewallDirectionCombo?.SelectedItem?.ToString() ?? "全部";
        IEnumerable<string[]> rows;

        if (_fwDetailLoaded && _fwDetails.Count > 0)
        {
            rows = _fwDetails.Select(d => new[]
            {
                d.Name,
                d.Enabled ? "启用" : "禁用",
                MapDirection(d.Dir),
                MapAction(d.Action),
                d.Proto,
                d.LocalPort,
                d.RemotePort,
                d.RemoteAddress,
            });
        }
        else
        {
            rows = _data.FirewallRules.Select(d => new[]
            {
                d.Name,
                d.Enabled ? "启用" : "禁用",
                MapDirection(d.Dir),
                MapAction(d.Action),
                "-", "-", "-", "-",
            });
        }

        if (dir == "入站") rows = rows.Where(r => r[2] == "入站");
        else if (dir == "出站") rows = rows.Where(r => r[2] == "出站");

        var arr = rows.OrderByDescending(r => r[1] == "启用").ThenBy(r => r[0]).ToArray();
        SetLV(firewallListView, arr);

        if (firewallHintLabel != null)
            firewallHintLabel.Text = _fwDetailLoaded
                ? $"完整信息：{arr.Length} 条"
                : "协议/端口/地址需管理员权限";
        AddLog($"✓ 防火墙规则：{arr.Length} 条");
    }

    private async Task LoadFirewallDetailsAsync()
    {
        ShowLoading("以管理员身份查询防火墙详情...");
        try
        {
            var (ok, rules, message) = await IPv6DataService.QueryFirewallDetailsAsync();
            if (ok && rules.Count > 0)
            {
                _fwDetails = rules;
                _fwDetailLoaded = true;
                ApplyFirewallFilter();
                AddLog("✓ " + message);
            }
            else
            {
                AddLog("⚠ 防火墙详情加载失败：" + Brief(message));
                MessageBox.Show(Brief(message), "查询失败", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }
        finally { HideLoading(); }
    }

    private static string MapDirection(string dir) => dir switch
    {
        "Inbound" => "入站",
        "Outbound" => "出站",
        _ => dir,
    };

    private static string MapAction(string action) => action switch
    {
        "Allow" => "允许",
        "Block" => "阻止",
        _ => action,
    };

    // ============================================================
    // 网络扫描
    // ============================================================
    private void PopulateScan()
    {
        if (scanNeighborList == null) return;
        var rows = _data.Neighbors
            .Where(n => !string.IsNullOrEmpty(n.IP))
            .Select(n => new[] { n.IP, n.MAC, n.Interface, MapNeighborState(n.State) })
            .ToArray();
        SetLV(scanNeighborList, rows);
        AddLog($"✓ 邻居缓存：{rows.Length} 个 IPv6 邻居");
    }

    private static string MapNeighborState(string state) => state switch
    {
        "Reachable" => "可达",
        "Stale" => "陈旧",
        "Delay" => "延迟",
        "Probe" => "探测中",
        "Unreachable" => "不可达",
        "Incomplete" => "不完整",
        "Permanent" => "永久",
        _ => state,
    };

    private async Task ScanMulticastAsync()
    {
        var iface = _data.Interfaces.FirstOrDefault(i => i.State == 1 && i.Index > 1);
        string target = iface != null ? $"ff02::1%{iface.Index}" : "ff02::1";

        ShowLoading("多播 Ping ...");
        try
        {
            string output = await IPv6DataService.RunPsAsync($"ping -6 -n 3 {target}", 25000);
            ShowResultDialog("多播 Ping（ff02::1）",
                output + (iface != null ? $"\n接口: {iface.Alias}（#{iface.Index}）" : "\n未找到已连接的接口，未指定作用域"));
            AddLog("📡 多播 Ping 完成");
        }
        finally { HideLoading(); }
    }

    private async Task ScanConnectionsAsync()
    {
        ShowLoading("读取 IPv6 活动连接...");
        try
        {
            string output = await IPv6DataService.RunPsAsync("netstat -p IPv6 -n", 20000);
            ShowResultDialog("IPv6 活动连接", output);
            AddLog("🔗 活动连接读取完成");
        }
        finally { HideLoading(); }
    }

    // ============================================================
    // 监听端口
    // ============================================================
    private async Task PopulateListenersAsync()
    {
        if (listenerListView == null) return;
        ShowLoading("读取 IPv6 监听端口...");
        try
        {
            var list = await _data.GetListenersAsync();
            var rows = list.Select(l => new[] { l.Proto, l.Address, l.Port.ToString(), l.Process, l.Pid.ToString() }).ToArray();
            SetLV(listenerListView, rows);
            if (listenerStatusLabel != null)
                listenerStatusLabel.Text = $"共 {rows.Length} 个 IPv6 监听（TCP {list.Count(l => l.Proto == "TCP")} / UDP {list.Count(l => l.Proto == "UDP")}）";
            AddLog($"✓ 监听端口：{rows.Length} 个");
        }
        finally { HideLoading(); }
    }

    // ============================================================
    // 一键体检
    // ============================================================
    private async Task RunHealthCheckAsync()
    {
        if (healthFlow == null) return;
        ShowLoading("正在体检...");
        try
        {
            await EnsureDataAsync();
            var conn = await _data.CheckConnectivityAsync();
            _lastConn = conn;

            var items = new List<IPv6DataService.HealthItem>();

            int total = _data.Bindings.Count;
            int disabled = _data.Bindings.Count(b => !b.Enabled);
            items.Add(new("适配器 IPv6 绑定",
                total == 0 ? "warn" : disabled == 0 ? "pass" : "fail",
                total == 0 ? "未发现网络适配器" : disabled == 0 ? $"{total} 个适配器全部启用 IPv6" : $"{disabled}/{total} 个适配器未启用 IPv6，可在「网络适配器」页启用"));

            var globals = _data.Addresses.Where(a => a.Type == "全局单播").ToList();
            items.Add(new("全局 IPv6 地址",
                globals.Count > 0 ? "pass" : "fail",
                globals.Count > 0 ? $"存在 {globals.Count} 个全局单播地址" : "没有全局单播地址，可能未获取到 IPv6 前缀"));

            bool hasDefault = _data.Routes.Any(r => r.Prefix == "::/0");
            items.Add(new("默认 IPv6 路由",
                hasDefault ? "pass" : "fail",
                hasDefault ? string.Join("；", _data.DefaultGateways.Select(g => $"{g.Interface} → {g.Gateway}").Take(3)) : "缺少 ::/0 默认路由"));

            int dnsCount = _data.DnsServers.Sum(d => d.Servers.Length);
            items.Add(new("IPv6 DNS 服务器",
                dnsCount > 0 ? "pass" : "warn",
                dnsCount > 0 ? $"{dnsCount} 个 IPv6 DNS 地址" : "未配置 IPv6 DNS，AAAA 解析可能依赖 IPv4 DNS"));

            int tentative = _data.Addresses.Count(a => a.State.Contains("Tentative", StringComparison.OrdinalIgnoreCase));
            items.Add(new("地址状态",
                tentative == 0 ? "pass" : "warn",
                tentative == 0 ? "没有地址处于 DAD 临时状态" : $"{tentative} 个地址仍在 DAD 临时状态"));

            items.Add(new("IPv6 连通性",
                conn.HasConnectivity ? "pass" : "fail",
                conn.Detail ?? ""));

            items.Add(new("公网 IPv6 地址",
                conn.PublicIP != null ? "pass" : "warn",
                conn.PublicIP ?? "未能获取公网 IPv6 地址（可能无 IPv6 出口或被拦截）"));

            _health = items;
            RenderHealth(items);
            AddLog($"🩺 体检完成：{items.Count(i => i.Status == "pass")}/{items.Count} 项通过");
            ToastFx.Show(this, "体检完成：" + items.Count(i => i.Status == "pass") + "/" + items.Count + " 项通过", ACCENT_GREEN);
        }
        catch (Exception ex)
        {
            AddLog("❌ 体检失败: " + ex.Message);
        }
        finally { HideLoading(); }
    }

    private void RenderHealth(List<IPv6DataService.HealthItem> items)
    {
        ClearControls(healthFlow);
        double score = items.Count == 0 ? 0 : items.Sum(i => i.Status == "pass" ? 1.0 : i.Status == "warn" ? 0.5 : 0) / items.Count * 100;
        Color scoreColor = score >= 80 ? ACCENT_GREEN : score >= 50 ? ACCENT_ORANGE : ACCENT_RED;
        if (healthScoreLabel != null)
        {
            healthScoreLabel.ForeColor = scoreColor;
            int passes = items.Count(i => i.Status == "pass");
            string stamp = DateTime.Now.ToString("HH:mm:ss");
            _ = Anim.RunAsync(Ease.OutCubic, 760, p =>
            {
                if (healthScoreLabel.IsDisposed) return;
                double v = score * Math.Clamp(p, 0, 1);
                healthScoreLabel.Text = $"得分 {v:0} · 通过 {passes}/{items.Count} · {stamp}";
            });
        }

        foreach (var item in items)
        {
            Color color = item.Status switch
            {
                "pass" => ACCENT_GREEN,
                "warn" => ACCENT_ORANGE,
                "fail" => ACCENT_RED,
                _ => ACCENT_BLUE,
            };
            string icon = item.Status switch { "pass" => "✅", "warn" => "⚠", "fail" => "❌", _ => "ℹ" };

            Panel card = new Panel
            {
                Size = new Size(590, 68),
                BackColor = BG_CARD,
                Margin = new Padding(0, 0, 12, 12),
            };
            card.Paint += (s, e) => e.Graphics.DrawLine(AccentPen(color), 0, 0, 0, 68);
            card.Controls.Add(new Label { Text = $"{icon}  {item.Title}", Location = new Point(14, 8), AutoSize = true, Font = FBold, ForeColor = TEXT_PRIMARY });
            card.Controls.Add(new Label
            {
                Text = item.Detail,
                Location = new Point(38, 32),
                AutoSize = true,
                MaximumSize = new Size(536, 30),
                Font = FSmall,
                ForeColor = TEXT_SECONDARY,
            });
            healthFlow.Controls.Add(card);
        }
    }

    private void CopyHealthReport()
    {
        if (_health.Count == 0) { MessageBox.Show("请先执行体检", "提示"); return; }
        var sb = new StringBuilder();
        sb.AppendLine($"IPv6 体检结果 · {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
        foreach (var item in _health)
            sb.AppendLine($"[{(item.Status == "pass" ? "通过" : item.Status == "warn" ? "警告" : "失败")}] {item.Title}：{item.Detail}");
        TryCopy(sb.ToString());
    }

    // ============================================================
    // 连接测试（.NET Ping，输出不受系统语言影响）
    // ============================================================
    private async Task RunPingTestAsync()
    {
        string target = connTargetCombo?.Text.Trim() ?? "";
        if (!TryValidateTarget(target, out string error))
        {
            Anim.Shake(connTargetCombo);
            ToastFx.Show(this, error, ACCENT_RED);
            return;
        }

        ShowLoading($"Ping {target} ...");
        var sb = new StringBuilder();
        sb.AppendLine($"Ping {target}（IPv6，最多 6 次）");
        sb.AppendLine(new string('─', 52));
        if (connResultBox != null) connResultBox.Text = sb.ToString();

        int ok = 0;
        long min = long.MaxValue, max = 0, sum = 0;
        try
        {
            for (int i = 1; i <= 6; i++)
            {
                try
                {
                    using var ping = new Ping();
                    var reply = await ping.SendPingAsync(target, 2000);
                    if (reply.Status == IPStatus.Success)
                    {
                        ok++;
                        sum += reply.RoundtripTime;
                        min = Math.Min(min, reply.RoundtripTime);
                        max = Math.Max(max, reply.RoundtripTime);
                        sb.AppendLine($"第 {i} 次: 来自 {reply.Address} 回复  时间={reply.RoundtripTime} ms");
                    }
                    else
                    {
                        sb.AppendLine($"第 {i} 次: {DescribePingStatus(reply.Status)}");
                    }
                }
                catch (Exception ex)
                {
                    sb.AppendLine($"第 {i} 次: 错误 {ex.Message}");
                    break;
                }

                if (connResultBox != null) connResultBox.Text = sb.ToString();
                await Task.Delay(250);
            }

            if (ok > 0) sb.AppendLine($"\n统计: 成功 {ok}/6，最小 {min} ms，最大 {max} ms，平均 {sum / ok} ms");
            else sb.AppendLine("\n全部失败：目标可能没有 IPv6 地址、未启用 IPv6，或被防火墙/安全软件拦截。");

            if (connResultBox != null) connResultBox.Text = sb.ToString();
            AddLog(ok > 0 ? $"📶 Ping {target}：成功 {ok}/6" : $"⚠ Ping {target}：全部失败");
        }
        finally { HideLoading(); }
    }

    private static string DescribePingStatus(IPStatus status) => status switch
    {
        IPStatus.TimedOut => "请求超时",
        IPStatus.DestinationHostUnreachable => "目标主机不可达",
        IPStatus.DestinationNetworkUnreachable => "目标网络不可达",
        IPStatus.DestinationPortUnreachable => "目标端口不可达",
        IPStatus.BadDestination => "目标地址无效",
        IPStatus.SourceQuench => "源抑制",
        _ => "失败（" + status + "）",
    };

    private async Task RunTracertAsync()
    {
        string target = connTargetCombo?.Text.Trim() ?? "";
        if (!TryValidateTarget(target, out string error))
        {
            Anim.Shake(connTargetCombo);
            ToastFx.Show(this, error, ACCENT_RED);
            return;
        }

        ShowLoading($"Traceroute {target} ...");
        try
        {
            string output = await IPv6DataService.RunPsAsync($"tracert -6 -d -h 20 -w 1000 {IPv6DataService.PsQuote(target)}", 120000);
            if (connResultBox != null)
                connResultBox.Text = $"Traceroute: {target}\n{new string('─', 52)}\n{output}";
            AddLog($"🗺 Traceroute {target} 完成");
        }
        finally { HideLoading(); }
    }

    private static bool TryValidateTarget(string input, out string error)
    {
        error = "";
        string t = (input ?? "").Trim();
        if (t.Length == 0) { error = "请输入目标地址或域名"; return false; }
        if (t.Length > 100) { error = "目标过长"; return false; }
        foreach (char ch in t)
        {
            if (!(char.IsLetterOrDigit(ch) || ch == '.' || ch == ':' || ch == '-' || ch == '%' || ch == '_'))
            {
                error = "目标只能包含字母、数字和 . : - % _";
                return false;
            }
        }
        return true;
    }

    // ============================================================
    // 高级工具
    // ============================================================
    private async Task ExecToolAsync(string cmd, string name, bool needConfirm)
    {
        if (needConfirm && MessageBox.Show($"确定执行「{name}」？\n\n{cmd}\n\n此操作会修改系统网络配置。", "确认",
                MessageBoxButtons.YesNo, MessageBoxIcon.Warning) != DialogResult.Yes) return;

        bool elevated = cmd.StartsWith("netsh int ipv6 reset", StringComparison.OrdinalIgnoreCase)
                     || cmd.StartsWith("ipconfig /flushdns", StringComparison.OrdinalIgnoreCase);

        ShowLoading($"{name} ...");
        try
        {
            string output;
            if (elevated)
            {
                var (ok, adminOutput) = await IPv6DataService.RunAdminAsync(cmd, 120000);
                output = ok && adminOutput.StartsWith("OK|", StringComparison.Ordinal)
                    ? adminOutput[3..]
                    : "❌ " + adminOutput;
            }
            else
            {
                output = await IPv6DataService.RunPsAsync(cmd, 60000);
            }

            ShowResultDialog(name, output);
            AddLog($"[{name}] 执行完成");
        }
        finally { HideLoading(); }
    }

    private void RunSubnetCalc()
    {
        string input = subnetInputBox?.Text.Trim() ?? "";
        string result = IPv6DataService.ComputeSubnet(input);
        if (result.StartsWith("格式", StringComparison.Ordinal) || result.StartsWith("不是", StringComparison.Ordinal) ||
            result.StartsWith("前缀", StringComparison.Ordinal))
        {
            Anim.Shake(subnetInputBox);
            ToastFx.Show(this, result, ACCENT_ORANGE);
            return;
        }
        ShowResultDialog("IPv6 子网计算", $"{input}\n{new string('─', 52)}\n{result}");
    }

    // ============================================================
    // 报告
    // ============================================================
    private async Task GenerateReportAsync()
    {
        ShowLoading("生成报告...");
        try
        {
            await EnsureDataAsync();
            _cachedReport = BuildReportText();
            if (reportTextBox != null) reportTextBox.Text = _cachedReport;
            AddLog("✓ 报告生成完成");
        }
        catch (Exception ex)
        {
            AddLog("❌ 报告生成失败: " + ex.Message);
        }
        finally { HideLoading(); }
    }

    private string BuildReportText()
    {
        var sb = new StringBuilder();
        sb.AppendLine("=== IPv6 配置报告 ===");
        sb.AppendLine($"时间: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
        sb.AppendLine($"主机: {Environment.MachineName}    版本: {CURRENT_VERSION}");
        sb.AppendLine(new string('=', 60));

        sb.AppendLine($"\n[适配器] {_data.Bindings.Count} 个，启用 {_data.Bindings.Count(b => b.Enabled)} 个");
        foreach (var b in _data.Bindings)
            sb.AppendLine($"  {b.Name}: {(b.Enabled ? "启用" : "禁用")}  {b.Speed}  [{b.Desc}]");

        sb.AppendLine($"\n[IPv6 地址] {_data.Addresses.Count} 个");
        foreach (var a in _data.Addresses.Take(100))
            sb.AppendLine($"  {a.Interface}: {a.IP}/{a.PrefixLen}  {a.Type}  {MapAddressState(a.State)}  来源: {MapOrigin(a.Origin)}");

        sb.AppendLine($"\n[DNS] {_data.DnsServers.Count} 条");
        foreach (var d in _data.DnsServers)
            sb.AppendLine($"  {d.Interface}: {string.Join(", ", d.Servers)}");

        sb.AppendLine($"\n[默认网关]");
        foreach (var g in _data.DefaultGateways)
            sb.AppendLine($"  {g.Interface} → {g.Gateway}");

        sb.AppendLine($"\n[路由] {_data.Routes.Count} 条");
        foreach (var r in _data.Routes.Take(50))
            sb.AppendLine($"  {r.Prefix} → {r.NextHop}  跃点 {r.Metric}  [{r.Interface}]");

        sb.AppendLine($"\n[接口] {_data.Interfaces.Count} 个");
        foreach (var i in _data.Interfaces)
            sb.AppendLine($"  #{i.Index} {i.Alias}: {i.StateText}  MTU {i.Mtu}");

        if (_health.Count > 0)
        {
            sb.AppendLine($"\n[体检结果]");
            foreach (var h in _health)
                sb.AppendLine($"  [{h.Status}] {h.Title}: {h.Detail}");
        }

        if (_lastConn != null)
        {
            sb.AppendLine($"\n[连通性] {( _lastConn.HasConnectivity ? "已连接" : "未连接")}");
            sb.AppendLine($"  公网地址: {_lastConn.PublicIP ?? "无"}");
            sb.AppendLine($"  详情: {_lastConn.Detail}");
        }

        return sb.ToString();
    }

    private string BuildReportJson()
    {
        var payload = new
        {
            Generated = DateTime.Now,
            Machine = Environment.MachineName,
            Version = CURRENT_VERSION,
            Connectivity = _lastConn,
            Health = _health,
            Bindings = _data.Bindings,
            Addresses = _data.Addresses,
            Dns = _data.DnsServers,
            Routes = _data.Routes,
            Interfaces = _data.Interfaces,
            Neighbors = _data.Neighbors,
            Firewall = _data.FirewallRules,
        };
        return JsonSerializer.Serialize(payload, new JsonSerializerOptions
        {
            WriteIndented = true,
            Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
        });
    }

    private string BuildReportHtml()
    {
        static string E(string s) => WebUtility.HtmlEncode(s ?? "");
        var sb = new StringBuilder();
        sb.AppendLine("<!DOCTYPE html><html lang=\"zh-CN\"><head><meta charset=\"utf-8\">");
        sb.AppendLine("<title>IPv6 配置报告</title>");
        sb.AppendLine("<style>body{background:#0d1117;color:#f0f6fc;font-family:Segoe UI,sans-serif;margin:32px}" +
                      "h1{color:#58a6ff}h2{color:#55d2d2;margin-top:28px;border-bottom:1px solid #30363d;padding-bottom:6px}" +
                      "table{border-collapse:collapse;width:100%;margin-top:8px}td,th{border:1px solid #30363d;padding:6px 10px;font-size:13px;text-align:left}" +
                      "th{background:#161b22}tr:nth-child(even){background:#11161d}.mono{font-family:Consolas,monospace}</style></head><body>");
        sb.AppendLine($"<h1>IPv6 配置报告</h1><p>主机 <b>{E(Environment.MachineName)}</b> · 版本 {E(CURRENT_VERSION)} · 生成时间 {DateTime.Now:yyyy-MM-dd HH:mm:ss}</p>");

        void Table(string title, string[] header, IEnumerable<string[]> rows)
        {
            sb.AppendLine($"<h2>{E(title)}</h2><table><tr>");
            foreach (var h in header) sb.AppendLine($"<th>{E(h)}</th>");
            sb.AppendLine("</tr>");
            foreach (var row in rows)
            {
                sb.AppendLine("<tr>");
                foreach (var cell in row) sb.AppendLine($"<td class=\"mono\">{E(cell)}</td>");
                sb.AppendLine("</tr>");
            }
            sb.AppendLine("</table>");
        }

        Table("适配器", new[] { "名称", "IPv6", "状态", "速率", "描述" },
            _data.Bindings.Select(b => new[] { b.Name, b.Enabled ? "启用" : "禁用", b.Status, b.Speed, b.Desc }));
        Table("IPv6 地址", new[] { "接口", "地址", "前缀", "类型", "状态", "来源" },
            _data.Addresses.Select(a => new[] { a.Interface, a.IP, "/" + a.PrefixLen, a.Type, MapAddressState(a.State), MapOrigin(a.Origin) }));
        Table("DNS", new[] { "接口", "服务器" },
            _data.DnsServers.Select(d => new[] { d.Interface, string.Join(", ", d.Servers) }));
        Table("路由", new[] { "前缀", "接口", "跃点", "下一跳" },
            _data.Routes.Select(r => new[] { r.Prefix, r.Interface, r.Metric.ToString(), r.NextHop }));
        Table("接口", new[] { "索引", "别名", "状态", "MTU" },
            _data.Interfaces.Select(i => new[] { i.Index.ToString(), i.Alias, i.StateText, i.Mtu.ToString() }));

        if (_health.Count > 0)
            Table("体检结果", new[] { "状态", "项目", "说明" },
                _health.Select(h => new[] { h.Status, h.Title, h.Detail }));

        sb.AppendLine("</body></html>");
        return sb.ToString();
    }

    private void CopyReport()
    {
        if (string.IsNullOrEmpty(_cachedReport)) { MessageBox.Show("请先生成报告", "提示"); return; }
        TryCopy(_cachedReport);
    }

    private void SaveReport()
    {
        if (string.IsNullOrEmpty(_cachedReport)) { MessageBox.Show("请先生成报告", "提示"); return; }

        using var dlg = new SaveFileDialog
        {
            FileName = $"IPv6_Report_{DateTime.Now:yyyyMMdd}",
            Filter = "文本文件|*.txt|JSON 文件|*.json|HTML 文件|*.html",
            DefaultExt = "txt",
        };
        if (dlg.ShowDialog(this) != DialogResult.OK) return;

        try
        {
            string ext = Path.GetExtension(dlg.FileName).ToLowerInvariant();
            string content = ext switch
            {
                ".json" => BuildReportJson(),
                ".html" or ".htm" => BuildReportHtml(),
                _ => _cachedReport,
            };
            File.WriteAllText(dlg.FileName, content, new UTF8Encoding(true));
            AddLog($"💾 已保存: {dlg.FileName}");
        }
        catch (Exception ex)
        {
            MessageBox.Show("保存失败: " + ex.Message, "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private void CopyLog()
    {
        if (dashEventLog == null) return;
        var lines = dashEventLog.Items.Cast<object>().Select(o => o?.ToString() ?? "").ToArray();
        if (lines.Length == 0) { MessageBox.Show("日志为空", "提示"); return; }
        TryCopy(string.Join(Environment.NewLine, lines));
    }

    private void SaveLog()
    {
        if (dashEventLog == null) return;
        var lines = dashEventLog.Items.Cast<object>().Select(o => o?.ToString() ?? "").ToArray();
        if (lines.Length == 0) { MessageBox.Show("日志为空", "提示"); return; }

        using var dlg = new SaveFileDialog
        {
            FileName = $"IPv6_Log_{DateTime.Now:yyyyMMdd_HHmmss}.txt",
            Filter = "文本文件|*.txt",
        };
        if (dlg.ShowDialog(this) != DialogResult.OK) return;
        try
        {
            File.WriteAllText(dlg.FileName, string.Join(Environment.NewLine, lines), new UTF8Encoding(true));
            AddLog($"💾 日志已保存: {dlg.FileName}");
        }
        catch (Exception ex)
        {
            MessageBox.Show("保存失败: " + ex.Message, "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    // ============================================================
    // 对话框 / 遮罩 / ListView
    // ============================================================
    private void ShowResultDialog(string title, string content)
    {
        using var f = new Form
        {
            Text = title,
            Size = new Size(820, 540),
            MinimumSize = new Size(600, 360),
            StartPosition = FormStartPosition.CenterParent,
            BackColor = BG_DARK,
            ForeColor = TEXT_PRIMARY,
            MinimizeBox = false,
            ShowIcon = false,
        };
        var tb = new TextBox
        {
            Multiline = true,
            ReadOnly = true,
            ScrollBars = ScrollBars.Both,
            Dock = DockStyle.Fill,
            BackColor = MONO_BG,
            ForeColor = MONO_FG,
            Font = FMono,
            BorderStyle = BorderStyle.None,
            WordWrap = false,
            Text = content ?? "",
        };
        var cb = new FlatButton
        {
            Text = "关闭",
            Dock = DockStyle.Bottom,
            Height = 32,
            NormalBack = BG_CARD,
            HoverBack = BG_HOVER,
            NormalFore = TEXT_PRIMARY,
            HoverFore = TEXT_PRIMARY,
            Radius = 0,
        };
        cb.Click += (s, e) => f.Close();
        f.Controls.Add(tb);
        f.Controls.Add(cb);
        f.Shown += (s, e) => { tb.SelectionLength = 0; cb.Focus(); };
        DialogFx.Show(this, f);
    }

    private void ShowLoading(string text)
    {
        if (loadingOverlay == null || loadingOverlay.IsDisposed) return;
        if (loadingOverlay.InvokeRequired)
        {
            try { loadingOverlay.BeginInvoke(new Action(() => ShowLoading(text))); } catch { }
            return;
        }
        loadingView?.SetStatus(text);
        loadingOverlay.BringToFront();
        loadingOverlay.Visible = true;
    }

    private void HideLoading()
    {
        if (loadingOverlay == null || loadingOverlay.IsDisposed) return;
        if (loadingOverlay.InvokeRequired)
        {
            try { loadingOverlay.BeginInvoke(new Action(HideLoading)); } catch { }
            return;
        }
        loadingOverlay.Visible = false;
    }

    private void SetLV(ListView lv, string[][] rows)
    {
        if (lv == null || lv.IsDisposed) return;
        lv.BeginUpdate();
        try
        {
            lv.Items.Clear();
            foreach (var r in rows)
            {
                if (r == null || r.Length == 0) continue;
                var item = new ListViewItem(r[0] ?? "");
                for (int i = 1; i < r.Length && i < lv.Columns.Count; i++) item.SubItems.Add(r[i] ?? "");
                lv.Items.Add(item);
            }
        }
        finally
        {
            lv.EndUpdate();
        }

        if (lv.Items.Count > 0 && lv.Columns.Count > 0)
        {
            lv.AutoResizeColumns(ColumnHeaderAutoResizeStyle.ColumnContent);
            for (int i = 0; i < lv.Columns.Count - 1; i++)
            {
                if (lv.Columns[i].Width < 90) lv.Columns[i].Width = 90;
                if (lv.Columns[i].Width > 420) lv.Columns[i].Width = 420;
            }
            if (lv.Tag is Action fit) fit();
            try { lv.TopItem = lv.Items[0]; } catch { }
        }
    }

    // ============================================================
    // 托盘 / 关于
    // ============================================================
    private void ShowFromTray()
    {
        Show();
        WindowState = FormWindowState.Normal;
        Activate();
        BringToFront();
    }

    private void ShowAbout()
    {
        using var f = new Form
        {
            Text = "关于",
            Size = new Size(560, 430),
            BackColor = BG_DARK,
            ForeColor = TEXT_PRIMARY,
            FormBorderStyle = FormBorderStyle.FixedDialog,
            StartPosition = FormStartPosition.CenterParent,
            MaximizeBox = false,
            MinimizeBox = false,
            ShowIcon = false,
        };

        var header = new PageHeaderView("IPv6 管理工具", CURRENT_VERSION + " · MIT License", ACCENT_BLUE)
        {
            Dock = DockStyle.Top,
            Height = 84,
        };
        var body = new Label
        {
            Dock = DockStyle.Fill,
            Padding = new Padding(18, 8, 18, 8),
            Font = FSmall,
            ForeColor = TEXT_SECONDARY,
            Text =
                "地址 / 路由 / DNS / 邻居 / 防火墙 查看\r\n" +
                "一键体检 · 网络监控 · 监听端口\r\n" +
                "DNS 解析测速 · IPv6 子网计算器\r\n" +
                "报告导出 TXT / JSON / HTML\r\n\r\n" +
                "动画改编自 StyleKit（60 项动效中的可落地子集）：\r\n" +
                "淡入上滑 · 弹性入场 · 悬浮抬升/发光 · 聚光灯\r\n" +
                "涟漪点击 · 边框光束 · 文字渐变/打字机/解密\r\n" +
                "三点加载 · 微光扫过 · 数字滚动 · 彩纸 · 折叠\r\n\r\n" +
                "仓库: https://github.com/" + GITHUB_REPO,
        };
        var close = new FlatButton
        {
            Text = "关闭",
            Dock = DockStyle.Bottom,
            Height = 38,
            NormalBack = ACCENT_BLUE,
            HoverBack = ControlPaint.Light(ACCENT_BLUE),
            NormalFore = Color.White,
            HoverFore = Color.White,
            Radius = 0,
        };
        close.Click += (s, e) => f.Close();

        f.Controls.Add(body);
        f.Controls.Add(close);
        f.Controls.Add(header);
        DialogFx.Show(this, f);
    }

    // ============================================================
    // 检测模式（--check）
    // ============================================================
    private async void OnCheckModeLoad(object sender, EventArgs e)
    {
        bool changed = false;
        _cmProgress.Value = 10;
        try
        {
            await _data.RefreshAllAsync();
            _cmProgress.Value = 40;

            int total = _data.Bindings.Count;
            int disabled = _data.Bindings.Count(b => !b.Enabled);

            if (total == 0)
            {
                _cmStatusLbl.Text = "❓ 无法检测";
                _cmStatusLbl.ForeColor = ACCENT_ORANGE;
                _cmDetailLbl.Text = "未发现网络适配器";
            }
            else if (disabled == 0)
            {
                _cmStatusLbl.Text = "✅ 全部已启用";
                _cmStatusLbl.ForeColor = ACCENT_GREEN;
                _cmDetailLbl.Text = $"{total} 个适配器 IPv6 正常";
            }
            else
            {
                _cmStatusLbl.Text = "⚠ IPv6 未完全启用";
                _cmStatusLbl.ForeColor = ACCENT_ORANGE;
                _cmDetailLbl.Text = $"{disabled}/{total} 个未启用，正在自动开启...";
                await Task.Delay(400);

                var (ok, output) = await IPv6DataService.RunAdminAsync("Enable-NetAdapterBinding -Name '*' -ComponentID ms_tcpip6");
                changed = true;
                _cmProgress.Value = 80;

                bool success = ok && output.StartsWith("OK|", StringComparison.Ordinal);
                _cmStatusLbl.Text = success ? "✅ 已自动启用" : "⚠ 自动启用失败";
                _cmStatusLbl.ForeColor = success ? ACCENT_GREEN : ACCENT_ORANGE;
                _cmDetailLbl.Text = success ? "IPv6 绑定已全部启用" : Brief(output);
                await Task.Delay(600);
                await _data.RefreshAllAsync();
            }
        }
        catch (Exception ex)
        {
            _cmStatusLbl.Text = "❌ 错误";
            _cmStatusLbl.ForeColor = ACCENT_RED;
            _cmDetailLbl.Text = ex.Message;
        }

        _cmProgress.Value = 100;
        int wait = changed ? 4 : 1;
        for (int i = wait; i > 0; i--)
        {
            _cmCountdownLbl.Text = $"{i} 秒后关闭";
            await Task.Delay(1000);
        }
        Application.Exit();
    }

    // ============================================================
    // 开机自启（计划任务，避免每次登录弹 UAC）
    // ============================================================
    private async Task<bool> IsStartupEnabledAsync()
    {
        string output = await IPv6DataService.RunPsAsync(
            $"if (Get-ScheduledTask -TaskName '{StartupTaskName}' -ErrorAction SilentlyContinue) {{ Write-Output 'YES' }}", 15000);
        return output.Contains("YES", StringComparison.Ordinal);
    }

    private async Task SetStartupCheckAsync(bool enable)
    {
        string exe = Application.ExecutablePath.Replace("'", "''");
        string cmd = enable
            ? "$a=New-ScheduledTaskAction -Execute '" + exe + "' -Argument '--check'; " +
              "$t=New-ScheduledTaskTrigger -AtLogOn; " +
              "Register-ScheduledTask -TaskName '" + StartupTaskName + "' -Action $a -Trigger $t -RunLevel Highest -Force | Out-Null; " +
              "Write-Output '已创建计划任务'"
            : "Unregister-ScheduledTask -TaskName '" + StartupTaskName + "' -Confirm:$false; Write-Output '已删除计划任务'";

        var (ok, output) = await IPv6DataService.RunAdminAsync(cmd, 60000);
        bool success = ok && output.StartsWith("OK|", StringComparison.Ordinal);

        if (success)
        {
            AddLog(enable ? "✅ 已启用开机静默检测（计划任务）" : "✅ 已关闭开机检测");
            MessageBox.Show(enable
                    ? "已启用开机检测。\n登录后将以管理员权限静默检查并启用 IPv6，不再弹出 UAC。"
                    : "已关闭开机检测。",
                "设置", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }
        else
        {
            AddLog("❌ 开机自启设置失败: " + Brief(output));
            MessageBox.Show("设置失败: " + Brief(output), "设置", MessageBoxButtons.OK, MessageBoxIcon.Warning);
        }
    }

    private async void ToggleAutoStart()
    {
        bool enabled = await IsStartupEnabledAsync();
        var answer = MessageBox.Show(
            enabled ? "当前已启用开机检测，是否关闭？" : "是否启用开机自动检测？\n将通过计划任务在登录时静默检查并启用 IPv6。",
            "开机自启", MessageBoxButtons.YesNo, MessageBoxIcon.Question);
        if (answer == DialogResult.Yes) await SetStartupCheckAsync(!enabled);
    }

    // ============================================================
    // 更新检测
    // ============================================================
    private async Task CheckForUpdatesAsync()
    {
        btnUpdate.Text = "…";
        btnUpdate.Enabled = false;
        try
        {
            using var http = new HttpClient { Timeout = TimeSpan.FromSeconds(10) };
            http.DefaultRequestHeaders.Add("User-Agent", "ipv6-tool");
            string json = await http.GetStringAsync($"https://api.github.com/repos/{GITHUB_REPO}/releases/latest");
            using var doc = JsonDocument.Parse(json);

            string tag = doc.RootElement.TryGetProperty("tag_name", out var t) ? t.GetString() ?? "" : "";
            string url = doc.RootElement.TryGetProperty("html_url", out var u) ? u.GetString() ?? "" : "";
            if (tag.Length == 0)
            {
                MessageBox.Show("无法获取版本信息", "更新", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            if (!IsNewerVersion(tag))
            {
                MessageBox.Show($"已是最新版本（{CURRENT_VERSION}）", "更新", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            if (MessageBox.Show($"发现新版本 {tag}\n当前版本 {CURRENT_VERSION}\n\n打开下载页？", "更新",
                    MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes && url.Length > 0)
            {
                Process.Start(new ProcessStartInfo { FileName = url, UseShellExecute = true });
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show("检查更新失败：\n" + ex.Message, "更新", MessageBoxButtons.OK, MessageBoxIcon.Warning);
        }
        finally
        {
            btnUpdate.Text = "⬇";
            btnUpdate.Enabled = true;
        }
    }

    private static bool IsNewerVersion(string tag)
    {
        if (!Version.TryParse(tag.TrimStart('v', 'V'), out var remote)) return false;
        if (!Version.TryParse(CURRENT_VERSION.TrimStart('v', 'V'), out var local)) return true;
        return remote > local;
    }
}
