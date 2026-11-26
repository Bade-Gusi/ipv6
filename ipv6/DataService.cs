// ============================================================
// IPv6DataService — 统一数据获取引擎 v2.1
// ============================================================

using System.Collections.Concurrent;
using System.Diagnostics;
using System.Net;
using System.Net.Http;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Numerics;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace ipv6;

public class IPv6DataService
{
    // ============================================================
    // 数据模型
    // ============================================================
    public record BindingInfo(string Name, bool Enabled, string Desc, string Status, int IfIndex, string Speed, string Mac);
    public record AddrInfo(string Interface, string IP, int PrefixLen, string State, string Origin, string Type, bool SkipAsSource, int IfIndex);
    public record DnsInfo(string Interface, string[] Servers);
    public record RouteInfo(string Prefix, string Interface, int Metric, string NextHop);
    public record NeighborInfo(string IP, string MAC, string Interface, string State);
    public record FwRuleInfo(string Name, bool Enabled, string Dir, string Action, string Proto, string LocalPort, string RemotePort, string RemoteAddress);
    public record InterfaceInfo(string Alias, int Index, int State, int Mtu)
    {
        public string StateText => State switch { 1 => "已连接", 2 => "已断开", 3 => "正在测试", 4 => "未知", 5 => "已休眠", 6 => "不存在", _ => "已降层" };
    }
    public record MyIPv6Info(string IP, int PrefixLen, string Interface, string Type, string State, string Origin);
    public record GatewayInfo(string Interface, string Gateway, string Prefix);
    public record ConnectivityStatus(bool HasConnectivity, string? PublicIP, DateTime CheckTime, string? Detail, string? Target, long RttMs);
    public record ListenerInfo(string Proto, string Address, int Port, int Pid, string Process);
    public record TunnelInfo(string Kind, string Type, string State, string Detail);
    public record DnsTestResult(string Server, bool Ok, long Ms, string Error);
    public record HealthItem(string Title, string Status, string Detail);
    public record FirewallDetail(string Name, bool Enabled, string Dir, string Action, string Proto, string LocalPort, string RemotePort, string RemoteAddress);

    // ============================================================
    // 公开状态
    // ============================================================
    public List<BindingInfo> Bindings { get; private set; } = new();
    public List<AddrInfo> Addresses { get; private set; } = new();
    public List<DnsInfo> DnsServers { get; private set; } = new();
    public List<RouteInfo> Routes { get; private set; } = new();
    public List<NeighborInfo> Neighbors { get; private set; } = new();
    public List<FwRuleInfo> FirewallRules { get; private set; } = new();
    public List<InterfaceInfo> Interfaces { get; private set; } = new();
    public bool IsLoaded { get; private set; }
    public DateTime RefreshTime { get; private set; }

    public IEnumerable<MyIPv6Info> MyAddresses =>
        Addresses.Select(a => new MyIPv6Info(a.IP, a.PrefixLen, a.Interface, a.Type, a.State, a.Origin));

    public IEnumerable<GatewayInfo> DefaultGateways =>
        Routes.Where(r => r.Prefix == "::/0").Select(r => new GatewayInfo(r.Interface, r.NextHop, r.Prefix));

    // 数据层错误（stderr / 超时 / 提权失败），由 UI 订阅后写入日志
    public static event Action<string>? ErrorOccurred;

    private static readonly ConcurrentDictionary<string, DateTime> RecentErrors = new();

    private static void ReportError(string message)
    {
        if (string.IsNullOrWhiteSpace(message)) return;
        string key = message.Length > 160 ? message[..160] : message;
        var now = DateTime.UtcNow;
        if (RecentErrors.TryGetValue(key, out var prev) && now - prev < TimeSpan.FromSeconds(15)) return;
        RecentErrors[key] = now;
        try { ErrorOccurred?.Invoke(message.Trim()); } catch { }
    }

    // ============================================================
    // 进程执行底层
    // ============================================================
    private record ProcResult(string Output, string Error, int ExitCode, bool TimedOut);

    private static async Task<ProcResult> RunProcessAsync(string fileName, IEnumerable<string> args, Encoding encoding, int timeoutMs)
    {
        var psi = new ProcessStartInfo(fileName)
        {
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            CreateNoWindow = true,
            StandardOutputEncoding = encoding,
            StandardErrorEncoding = encoding,
        };
        foreach (var a in args) psi.ArgumentList.Add(a);

        try
        {
            using var p = new Process { StartInfo = psi };
            if (!p.Start()) return new("", "进程启动失败", -1, false);

            var outTask = p.StandardOutput.ReadToEndAsync();
            var errTask = p.StandardError.ReadToEndAsync();

            bool timedOut = false;
            using (var cts = new CancellationTokenSource(timeoutMs))
            {
                try { await p.WaitForExitAsync(cts.Token).ConfigureAwait(false); }
                catch (OperationCanceledException)
                {
                    timedOut = true;
                    try { p.Kill(entireProcessTree: true); } catch { }
                }
            }

            string output = await outTask.ConfigureAwait(false);
            string error = await errTask.ConfigureAwait(false);
            return timedOut ? new(output, error, -1, true) : new(output, error, p.ExitCode, false);
        }
        catch (Exception ex)
        {
            return new("", ex.Message, -1, false);
        }
    }

    private static string FirstLine(string text)
    {
        if (string.IsNullOrWhiteSpace(text)) return "未知错误";
        string t = text.Trim();
        int nl = t.IndexOfAny(new[] { '\r', '\n' });
        return nl > 0 ? t[..nl] : t;
    }

    /// <summary>执行 PowerShell 命令（-EncodedCommand，天然免疫引号/注入问题）。失败时通过 ErrorOccurred 上报。</summary>
    public static async Task<string> RunPsAsync(string cmd, int timeoutMs = 30000)
    {
        string full = "[Console]::OutputEncoding=[Text.Encoding]::UTF8;$ProgressPreference='SilentlyContinue';" + cmd;
        string encoded = Convert.ToBase64String(Encoding.Unicode.GetBytes(full));
        var r = await RunProcessAsync("powershell.exe",
            new[] { "-NoProfile", "-NonInteractive", "-NoLogo", "-ExecutionPolicy", "Bypass", "-EncodedCommand", encoded },
            Encoding.UTF8, timeoutMs).ConfigureAwait(false);

        if (r.TimedOut)
        {
            ReportError($"PowerShell 执行超时（{timeoutMs / 1000} 秒）");
            return "ERR|timeout";
        }
        if (!string.IsNullOrWhiteSpace(r.Error)) ReportError(r.Error);
        if (r.Output.Trim().Length == 0 && r.ExitCode != 0) return "ERR|" + FirstLine(r.Error);
        return r.Output;
    }

    /// <summary>以管理员身份执行命令，返回是否成功及输出（通过临时文件回传结果，可感知失败/取消）。</summary>
    public static async Task<(bool Ok, string Output)> RunAdminAsync(string cmd, int timeoutMs = 180000)
    {
        string tmp = Path.Combine(Path.GetTempPath(), "ipv6mgr_" + Guid.NewGuid().ToString("N") + ".txt");
        string script =
            "$ErrorActionPreference='Stop'\r\n" +
            "$r = ''\r\n" +
            "try {\r\n" +
            "  $o = (& {\r\n" + cmd + "\r\n} 2>&1 | Out-String)\r\n" +
            "  $r = 'OK|' + $o\r\n" +
            "} catch {\r\n" +
            "  $r = 'ERR|' + $_.Exception.Message\r\n" +
            "}\r\n" +
            "Set-Content -LiteralPath " + PsQuote(tmp) + " -Value $r -Encoding UTF8\r\n";

        string encoded = Convert.ToBase64String(Encoding.Unicode.GetBytes(script));
        var psi = new ProcessStartInfo("powershell.exe")
        {
            UseShellExecute = true,
            Verb = "runas",
            WindowStyle = ProcessWindowStyle.Hidden,
            Arguments = "-NoProfile -NonInteractive -ExecutionPolicy Bypass -EncodedCommand " + encoded,
        };

        Process? proc = null;
        try { proc = Process.Start(psi); }
        catch (System.ComponentModel.Win32Exception ex) when (ex.NativeErrorCode == 1223)
        {
            return (false, "已取消管理员授权");
        }
        catch (Exception ex) { return (false, "无法启动提权进程: " + ex.Message); }

        try
        {
            var sw = Stopwatch.StartNew();
            while (sw.ElapsedMilliseconds < timeoutMs)
            {
                if (File.Exists(tmp)) { await Task.Delay(120).ConfigureAwait(false); break; }
                bool exited = false;
                try { exited = proc is null || proc.HasExited; } catch { exited = true; }
                if (exited)
                {
                    await Task.Delay(300).ConfigureAwait(false);
                    if (!File.Exists(tmp)) return (false, "提权进程已退出但未返回结果（可能被安全软件拦截）");
                    break;
                }
                await Task.Delay(200).ConfigureAwait(false);
            }

            if (!File.Exists(tmp)) return (false, "管理员操作超时");

            string text = (await File.ReadAllTextAsync(tmp).ConfigureAwait(false)).Trim();
            if (text.StartsWith("OK|", StringComparison.Ordinal)) return (true, text);
            if (text.StartsWith("ERR|", StringComparison.Ordinal)) return (false, text[4..].Trim());
            return (false, FirstLine(text));
        }
        finally
        {
            try { File.Delete(tmp); } catch { }
        }
    }

    public static string PsQuote(string? value) => "'" + (value ?? "").Replace("'", "''") + "'";

    // ============================================================
    // 批量刷新（单一并发闸门）
    // ============================================================
    private readonly SemaphoreSlim _gate = new(1, 1);

    private const string BatchAdapters = @"
$a=@(Get-NetAdapter | Select-Object Name,InterfaceDescription,Status,InterfaceIndex,LinkSpeed,MacAddress)
$b=@(Get-NetAdapterBinding -ComponentID ms_tcpip6 | Select-Object Name,Enabled,DisplayName)
Write-Output '@@ADAPTER@@'
$a | ConvertTo-Json -Compress
Write-Output '@@BIND@@'
$b | ConvertTo-Json -Compress
";

    private const string BatchNetwork = @"
$addr=@(Get-NetIPAddress -AddressFamily IPv6 | Select-Object InterfaceAlias,IPAddress,PrefixLength,@{n='AddressState';e={[string]$_.AddressState}},@{n='PrefixOrigin';e={[string]$_.PrefixOrigin}},@{n='SuffixOrigin';e={[string]$_.SuffixOrigin}},SkipAsSource,InterfaceIndex)
$dns=@(Get-DnsClientServerAddress -AddressFamily IPv6 | Select-Object InterfaceAlias,ServerAddresses)
$route=@(Get-NetRoute -AddressFamily IPv6 | Select-Object DestinationPrefix,InterfaceAlias,RouteMetric,NextHop)
$neigh=@(Get-NetNeighbor -AddressFamily IPv6 | Select-Object IPAddress,LinkLayerAddress,InterfaceAlias,@{n='State';e={[string]$_.State}})
$if=@(Get-NetIPInterface -AddressFamily IPv6 | Select-Object InterfaceAlias,InterfaceIndex,ConnectionState,NlMtu)
Write-Output '@@ADDR@@'
$addr | ConvertTo-Json -Compress
Write-Output '@@DNS@@'
$dns | ConvertTo-Json -Compress -Depth 4
Write-Output '@@ROUTE@@'
$route | ConvertTo-Json -Compress
Write-Output '@@NEIGH@@'
$neigh | ConvertTo-Json -Compress
Write-Output '@@IF@@'
$if | ConvertTo-Json -Compress
";

    private const string BatchFirewall = @"
$rules=@(Get-NetFirewallRule | Select-Object @{n='Name';e={$_.DisplayName}},@{n='Enabled';e={[string]$_.Enabled}},@{n='Dir';e={[string]$_.Direction}},@{n='Action';e={[string]$_.Action}})
Write-Output '@@FW@@'
$rules | ConvertTo-Json -Compress
";

    public async Task RefreshAllAsync(Action<string>? status = null)
    {
        await _gate.WaitAsync().ConfigureAwait(false);
        try
        {
            status?.Invoke("获取适配器...");
            var t1 = RunPsAsync(BatchAdapters, 30000);
            status?.Invoke("获取地址/路由/邻居...");
            var t2 = RunPsAsync(BatchNetwork, 45000);
            status?.Invoke("获取防火墙规则...");
            var t3 = RunPsAsync(BatchFirewall, 60000);

            await Task.WhenAll(t1, t2, t3).ConfigureAwait(false);

            ParseAdapters(t1.Result);
            ParseNetwork(t2.Result);

            var fwSec = SplitSections(t3.Result);
            FirewallRules = DeserializeList(SectionOf(fwSec, "FW"), e => new FwRuleInfo(
                S(e, "Name"), IsTrue(S(e, "Enabled")), S(e, "Dir"), S(e, "Action"), "", "", "", ""));

            IsLoaded = true;
            RefreshTime = DateTime.Now;
        }
        finally
        {
            _gate.Release();
        }
    }

    // ============================================================
    // 解析
    // ============================================================
    private void ParseAdapters(string raw)
    {
        var sec = SplitSections(raw);

        var adapters = new Dictionary<string, (string Desc, string Status, int IfIndex, string Speed, string Mac)>(StringComparer.OrdinalIgnoreCase);
        foreach (var e in ToElements(SectionOf(sec, "ADAPTER")))
        {
            string n = S(e, "Name");
            if (!string.IsNullOrEmpty(n))
                adapters[n] = (S(e, "InterfaceDescription"), S(e, "Status"), I(e, "InterfaceIndex"), S(e, "LinkSpeed"), S(e, "MacAddress"));
        }

        var list = new List<BindingInfo>();
        foreach (var e in ToElements(SectionOf(sec, "BIND")))
        {
            string n = S(e, "Name");
            bool enabled = e.TryGetProperty("Enabled", out var ep) && ep.ValueKind == JsonValueKind.True;
            var a = adapters.TryGetValue(n, out var v)
                ? v
                : (Desc: "-", Status: "-", IfIndex: 0, Speed: "-", Mac: "-");
            list.Add(new BindingInfo(n, enabled, a.Desc, a.Status, a.IfIndex, a.Speed, a.Mac));
        }
        Bindings = list;
    }

    private void ParseNetwork(string raw)
    {
        var sec = SplitSections(raw);

        Addresses = DeserializeList(SectionOf(sec, "ADDR"), e =>
        {
            string ip = S(e, "IPAddress");
            return new AddrInfo(S(e, "InterfaceAlias"), ip, I(e, "PrefixLength"), S(e, "AddressState"),
                S(e, "PrefixOrigin"), ClassifyIPv6(ip), B(e, "SkipAsSource"), I(e, "InterfaceIndex"));
        });

        DnsServers = DeserializeList(SectionOf(sec, "DNS"), e =>
        {
            var servers = new List<string>();
            if (e.TryGetProperty("ServerAddresses", out var sa) && sa.ValueKind == JsonValueKind.Array)
                foreach (var a in sa.EnumerateArray()) servers.Add(a.GetString() ?? "");
            return new DnsInfo(S(e, "InterfaceAlias"), servers.ToArray());
        });

        Routes = DeserializeList(SectionOf(sec, "ROUTE"), e =>
            new RouteInfo(S(e, "DestinationPrefix"), S(e, "InterfaceAlias"), I(e, "RouteMetric"), S(e, "NextHop")));

        Neighbors = DeserializeList(SectionOf(sec, "NEIGH"), e =>
            new NeighborInfo(S(e, "IPAddress"), S(e, "LinkLayerAddress"), S(e, "InterfaceAlias"), S(e, "State")));

        Interfaces = DeserializeList(SectionOf(sec, "IF"), e =>
            new InterfaceInfo(S(e, "InterfaceAlias"), I(e, "InterfaceIndex"), I(e, "ConnectionState"), I(e, "NlMtu")));
    }

    // ============================================================
    // 地址分类
    // ============================================================
    public static string ClassifyIPv6(string ip)
    {
        if (string.IsNullOrWhiteSpace(ip)) return "未知";
        string s = ip.Trim().Trim('[', ']');
        int pct = s.IndexOf('%');
        if (pct >= 0) s = s[..pct];

        if (s == "::1") return "回环";
        if (s == "::") return "未指定";
        if (s.StartsWith("::ffff:", StringComparison.OrdinalIgnoreCase)) return "IPv4 映射";

        string low = s.ToLowerInvariant();
        if (low.StartsWith("fe80:")) return "链路本地";
        if (low.StartsWith("fec0:")) return "站点本地";
        if (low.StartsWith("fc") || low.StartsWith("fd")) return "唯一本地";
        if (low.StartsWith("ff")) return "多播";
        if (low.StartsWith("2002:")) return "6to4";
        if (low.StartsWith("2001:0:") || low.StartsWith("2001:0000:")) return "Teredo";
        if (low.StartsWith("2001:db8")) return "文档地址";
        return "全局单播";
    }

    // ============================================================
    // 连通性（多目标 + 公网地址双通道）
    // ============================================================
    private static readonly string[] PingTargets =
    {
        "2400:3200::1",   // AliDNS
        "2402:4e00::",    // 腾讯 DNS
        "240c::6666",     // 下一代互联网 DNS
        "2001:da8::666",  // CERNET
    };

    private static readonly string[] PublicIpEndpoints =
    {
        "https://ipv6.ip.sb",
        "https://ipv6.lookup.test-ipv6.com/ip/",
        "https://api6.ipify.org",
        "https://6.ipw.cn",
    };

    private static readonly Regex JsonIpRx = new("\"ip\"\\s*:\\s*\"([^\"]+)\"", RegexOptions.Compiled);

    private static readonly HttpClient Http = new() { Timeout = TimeSpan.FromSeconds(8) };

    public async Task<ConnectivityStatus> CheckConnectivityAsync(bool fetchPublicIp = true)
    {
        var results = await Task.WhenAll(PingTargets.Select(async target =>
        {
            try
            {
                using var ping = new Ping();
                var reply = await ping.SendPingAsync(IPAddress.Parse(target), 2500).ConfigureAwait(false);
                return (target, ok: reply.Status == IPStatus.Success, rtt: (long)reply.RoundtripTime);
            }
            catch { return (target, ok: false, rtt: 0L); }
        })).ConfigureAwait(false);

        string? bestTarget = null;
        long bestRtt = 0;
        foreach (var r in results.Where(r => r.ok).OrderBy(r => r.rtt))
        {
            bestTarget = r.target;
            bestRtt = r.rtt;
            break;
        }

        string? publicIp = fetchPublicIp ? await FetchPublicIPv6Async().ConfigureAwait(false) : null;
        bool ok = bestTarget != null || publicIp != null;

        string detail = bestTarget != null
            ? $"Ping {bestTarget} 成功，{bestRtt} ms"
            : publicIp != null
                ? "公网地址可达（ICMP 被拦截）"
                : "所有 IPv6 探测目标均无响应";

        return new ConnectivityStatus(ok, publicIp, DateTime.Now, detail, bestTarget, bestRtt);
    }

    public static async Task<string?> FetchPublicIPv6Async()
    {
        foreach (string url in PublicIpEndpoints)
        {
            try
            {
                using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(6));
                string text = (await Http.GetStringAsync(url, cts.Token).ConfigureAwait(false)).Trim();
                string? address = ExtractIPv6(text);
                if (address != null) return address;
            }
            catch { }
        }
        return null;
    }

    private static string? ExtractIPv6(string text)
    {
        if (string.IsNullOrWhiteSpace(text)) return null;
        text = text.Trim();

        // 1) 纯地址
        if (IPAddress.TryParse(text, out var direct) && direct.AddressFamily == AddressFamily.InterNetworkV6 && !direct.IsIPv6LinkLocal)
            return text;

        // 2) JSON / JSONP: "ip":"2408:..."
        var match = JsonIpRx.Match(text);
        if (match.Success && IPAddress.TryParse(match.Groups[1].Value, out var fromJson) &&
            fromJson.AddressFamily == AddressFamily.InterNetworkV6)
            return match.Groups[1].Value;

        // 3) 页面中包含的裸地址
        foreach (string token in text.Split(new[] { '\r', '\n', ' ', '\t', '"', '>', '<', ',' }, StringSplitOptions.RemoveEmptyEntries))
        {
            if (token.Length < 3 || !token.Contains(':')) continue;
            if (IPAddress.TryParse(token, out var ip) && ip.AddressFamily == AddressFamily.InterNetworkV6 && !ip.IsIPv6LinkLocal)
                return token;
        }
        return null;
    }

    // ============================================================
    // 隧道配置（结构化对象，不依赖界面语言）
    // ============================================================
    private const string TunnelScript = @"
$t=Get-NetTeredoConfiguration
$six=Get-Net6to4Configuration
$i=Get-NetIsatapConfiguration
$ts=(netsh int teredo show state | Out-String)
$state='-'
if ($ts -match '(?m)^\s*(?:State|状态)\s*[:：]\s*(\S+)') { $state=$Matches[1] }
Write-Output '@@TUN@@'
@([pscustomobject]@{Kind='Teredo';Type=[string]$t.Type;State=$state;Detail=$t.ServerName},
  [pscustomobject]@{Kind='6to4';Type='';State=[string]$six.State;Detail=$six.RelayName},
  [pscustomobject]@{Kind='ISATAP';Type='';State=[string]$i.State;Detail=$i.Router}) | ConvertTo-Json -Compress
";

    public async Task<List<TunnelInfo>> GetTunnelsAsync() =>
        DeserializeList(SectionOf(SplitSections(await RunPsAsync(TunnelScript, 20000).ConfigureAwait(false)), "TUN"),
            e => new TunnelInfo(S(e, "Kind"), S(e, "Type"), S(e, "State"), S(e, "Detail")));

    // ============================================================
    // 监听端口（TCP/UDP，IPv6）
    // ============================================================
    private const string ListenerScript = @"
$tcp=@(Get-NetTCPConnection -State Listen | Where-Object { $_.LocalAddress -like '*:*' } | Select-Object LocalAddress,LocalPort,OwningProcess)
$udp=@(Get-NetUDPEndpoint | Where-Object { $_.LocalAddress -like '*:*' } | Select-Object LocalAddress,LocalPort,OwningProcess)
Write-Output '@@TCP@@'
$tcp | ConvertTo-Json -Compress
Write-Output '@@UDP@@'
$udp | ConvertTo-Json -Compress
";

    public async Task<List<ListenerInfo>> GetListenersAsync()
    {
        var sec = SplitSections(await RunPsAsync(ListenerScript, 30000).ConfigureAwait(false));
        var list = new List<ListenerInfo>();
        var names = new Dictionary<int, string>();

        string ProcessName(int pid)
        {
            if (pid <= 0) return "-";
            if (names.TryGetValue(pid, out var cached)) return cached;
            string name;
            try { name = Process.GetProcessById(pid).ProcessName; }
            catch { name = "-"; }
            names[pid] = name;
            return name;
        }

        foreach (var e in ToElements(SectionOf(sec, "TCP")))
        {
            int pid = I(e, "OwningProcess");
            list.Add(new ListenerInfo("TCP", S(e, "LocalAddress"), I(e, "LocalPort"), pid, ProcessName(pid)));
        }
        foreach (var e in ToElements(SectionOf(sec, "UDP")))
        {
            int pid = I(e, "OwningProcess");
            list.Add(new ListenerInfo("UDP", S(e, "LocalAddress"), I(e, "LocalPort"), pid, ProcessName(pid)));
        }

        return list.OrderBy(l => l.Proto).ThenBy(l => l.Port).ToList();
    }

    // ============================================================
    // 防火墙（基础列表无需管理员；端口/地址详情需提权查询）
    // ============================================================
    private const string FirewallDetailScript = @"
$p=@{}; foreach($x in (Get-NetFirewallPortFilter)) { $p[$x.InstanceID]=$x }
$a=@{}; foreach($x in (Get-NetFirewallAddressFilter)) { $a[$x.InstanceID]=$x }
$out=@(foreach($r in (Get-NetFirewallRule)) {
  $pf=$p[$r.InstanceID]
  if ($null -eq $pf) { continue }
  $af=$a[$r.InstanceID]
  $ra=''; $la=''
  if ($null -ne $af) { $ra=($af.RemoteAddress -join ','); $la=($af.LocalAddress -join ',') }
  if ($ra -match ':' -or $la -match ':' -or $ra -eq 'Any' -or $la -eq 'Any') {
    [pscustomobject]@{Name=$r.DisplayName;Enabled=[string]$r.Enabled;Dir=[string]$r.Direction;Action=[string]$r.Action;Proto=$pf.Protocol;LP=($pf.LocalPort -join ',');RP=($pf.RemotePort -join ',');RA=$ra}
  }
})
Write-Output '@@FW@@'
$out | ConvertTo-Json -Compress
";

    public static async Task<(bool Ok, List<FirewallDetail> Rules, string Message)> QueryFirewallDetailsAsync()
    {
        var (ok, output) = await RunAdminAsync(FirewallDetailScript, 240000).ConfigureAwait(false);
        if (!ok) return (false, new(), output);

        string body = output.StartsWith("OK|", StringComparison.Ordinal) ? output[3..] : output;
        var sec = SplitSections(body);
        var rules = DeserializeList(SectionOf(sec, "FW"), e => new FirewallDetail(
            S(e, "Name"), IsTrue(S(e, "Enabled")), S(e, "Dir"), S(e, "Action"),
            S(e, "Proto"), S(e, "LP"), S(e, "RP"), S(e, "RA")));

        return (rules.Count > 0, rules, rules.Count > 0 ? $"已加载 {rules.Count} 条完整规则" : "未返回规则数据");
    }

    // ============================================================
    // DNS 诊断
    // ============================================================
    private static readonly Regex HostRx = new(@"^[A-Za-z0-9]([A-Za-z0-9\-\.]{0,251})$", RegexOptions.Compiled);

    public static bool IsValidHostName(string? host) => !string.IsNullOrWhiteSpace(host) && HostRx.IsMatch(host);

    public static async Task<List<string>> ResolveHostAsync(string host)
    {
        var result = new List<string>();
        if (!IsValidHostName(host) && !IPAddress.TryParse(host, out _)) return result;
        try
        {
            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(8));
            var addrs = await Dns.GetHostAddressesAsync(host, cts.Token).ConfigureAwait(false);
            result.AddRange(addrs.Select(a => a.ToString()).Distinct());
        }
        catch { }
        return result;
    }

    public static async Task<List<DnsTestResult>> TestDnsServersAsync(string domain, IEnumerable<string> servers)
    {
        var results = new List<DnsTestResult>();
        if (!IsValidHostName(domain)) return results;

        var ips = servers.Distinct().Where(s => IPAddress.TryParse(s ?? "", out _)).Take(8).ToList();
        if (ips.Count == 0) return results;

        string list = string.Join(",", ips.Select(PsQuote));
        string cmd =
            "$servers=@(" + list + ")\r\n" +
            "$results=@(foreach($s in $servers){\r\n" +
            "  $sw=[Diagnostics.Stopwatch]::StartNew()\r\n" +
            "  try { Resolve-DnsName -Name " + PsQuote(domain) + " -Type AAAA -DnsOnly -QuickTimeout -ErrorAction Stop | Out-Null; $ok=$true; $err='' }\r\n" +
            "  catch { $ok=$false; $err=$_.Exception.Message }\r\n" +
            "  [pscustomobject]@{Server=$s;Ok=$ok;Ms=$sw.ElapsedMilliseconds;Error=$err}\r\n" +
            "})\r\n" +
            "Write-Output '@@DNS@@'\r\n" +
            "$results | ConvertTo-Json -Compress\r\n";

        var sec = SplitSections(await RunPsAsync(cmd, 40000).ConfigureAwait(false));
        return DeserializeList(SectionOf(sec, "DNS"), e =>
            new DnsTestResult(S(e, "Server"), B(e, "Ok"), I(e, "Ms"), S(e, "Error")));
    }

    // ============================================================
    // IPv6 子网计算器
    // ============================================================
    public static string ComputeSubnet(string input)
    {
        string text = (input ?? "").Trim();
        var parts = text.Split('/');
        if (parts.Length != 2) return "格式：地址/前缀长度，例如 2400:3200::/32";
        if (!IPAddress.TryParse(parts[0].Trim(), out var ip) || ip.AddressFamily != AddressFamily.InterNetworkV6)
            return "不是合法的 IPv6 地址";
        if (!int.TryParse(parts[1].Trim(), out int prefix) || prefix < 0 || prefix > 128)
            return "前缀长度必须在 0-128 之间";

        byte[] bytes = ip.GetAddressBytes();
        int full = prefix / 8, rem = prefix % 8;
        var net = new byte[16];
        var last = new byte[16];

        for (int i = 0; i < 16; i++)
        {
            if (i < full) { net[i] = bytes[i]; last[i] = bytes[i]; }
            else if (i == full && rem > 0) { net[i] = (byte)(bytes[i] & (0xFF << (8 - rem))); last[i] = (byte)(net[i] | (0xFF >> rem)); }
            else { net[i] = 0; last[i] = 0xFF; }
        }

        var netIp = new IPAddress(net);
        var lastIp = new IPAddress(last);
        BigInteger total = BigInteger.One << (128 - prefix);

        string hex = Convert.ToHexString(net).ToLowerInvariant();
        int nibbles = (prefix + 3) / 4;
        string reverse = string.Join(".", hex[..nibbles].Reverse()) + ".ip6.arpa";

        var sb = new StringBuilder();
        sb.AppendLine($"输入地址    : {ip}");
        sb.AppendLine($"网络前缀    : {netIp}/{prefix}");
        sb.AppendLine($"首地址      : {netIp}");
        sb.AppendLine($"末地址      : {lastIp}");
        sb.AppendLine($"地址总数    : {total:N0}");
        sb.AppendLine($"地址类型    : {ClassifyIPv6(netIp.ToString())}");
        sb.AppendLine($"反向解析域  : {reverse}");
        return sb.ToString().TrimEnd();
    }

    // ============================================================
    // JSON / 文本辅助
    // ============================================================
    private static readonly Regex SectionRx = new(@"(?m)^@@(?<n>[A-Z]+)@@\s*$", RegexOptions.Compiled);

    private static Dictionary<string, string> SplitSections(string raw)
    {
        var dict = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        if (string.IsNullOrWhiteSpace(raw)) return dict;

        var matches = SectionRx.Matches(raw);
        for (int i = 0; i < matches.Count; i++)
        {
            int start = matches[i].Index + matches[i].Length;
            int end = i + 1 < matches.Count ? matches[i + 1].Index : raw.Length;
            dict[matches[i].Groups["n"].Value] = raw[start..end].Trim();
        }
        return dict;
    }

    private static string SectionOf(Dictionary<string, string> sections, string name) =>
        sections.TryGetValue(name, out var text) ? text : "[]";

    private static List<JsonElement> ToElements(string json)
    {
        var list = new List<JsonElement>();
        if (string.IsNullOrWhiteSpace(json) || json.StartsWith("ERR|", StringComparison.Ordinal)) return list;
        try
        {
            using var doc = JsonDocument.Parse(json.Trim());
            if (doc.RootElement.ValueKind == JsonValueKind.Array)
                foreach (var e in doc.RootElement.EnumerateArray()) list.Add(e.Clone());
            else if (doc.RootElement.ValueKind == JsonValueKind.Object)
                list.Add(doc.RootElement.Clone());
        }
        catch { }
        return list;
    }

    private static List<T> DeserializeList<T>(string json, Func<JsonElement, T> map)
    {
        var list = new List<T>();
        foreach (var e in ToElements(json))
            try { list.Add(map(e)); } catch { }
        return list;
    }

    private static string S(JsonElement e, string key)
    {
        if (!e.TryGetProperty(key, out var p)) return "";
        return p.ValueKind switch
        {
            JsonValueKind.String => p.GetString() ?? "",
            JsonValueKind.Number => p.GetRawText(),
            JsonValueKind.True => "True",
            JsonValueKind.False => "False",
            JsonValueKind.Null or JsonValueKind.Undefined => "",
            _ => p.GetRawText(),
        };
    }
    private static int I(JsonElement e, string key) => e.TryGetProperty(key, out var p) && p.TryGetInt32(out int v) ? v : 0;

    private static bool B(JsonElement e, string key) =>
        e.TryGetProperty(key, out var p) && (p.ValueKind == JsonValueKind.True ||
        (p.ValueKind == JsonValueKind.String && string.Equals(p.GetString(), "True", StringComparison.OrdinalIgnoreCase)));

    private static bool IsTrue(string value) => string.Equals(value, "True", StringComparison.OrdinalIgnoreCase) || value == "1";
}
