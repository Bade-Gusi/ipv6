// ============================================================
// IPv6DataService — 统一数据获取引擎
// ============================================================

using System.Collections.Concurrent;
using System.Diagnostics;
using System.Text;
using System.Text.Json;

namespace ipv6;

public class IPv6DataService
{
    private static readonly ConcurrentDictionary<string, (string result, DateTime time)> _cache = new();
    static readonly TimeSpan CacheTTL = TimeSpan.FromSeconds(30);

    public List<BindingInfo> Bindings { get; private set; } = new();
    public List<AddrInfo> Addresses { get; private set; } = new();
    public List<DnsInfo> DnsServers { get; private set; } = new();
    public List<RouteInfo> Routes { get; private set; } = new();
    public List<NeighborInfo> Neighbors { get; private set; } = new();
    public List<FwRuleInfo> FirewallRules { get; private set; } = new();
    public DateTime RefreshTime { get; private set; }

    public record BindingInfo(string Name, bool Enabled, string Desc, string Status, int IfIndex);
    private record AdapterDetail(string Desc, string Status, int IfIndex);
    public record AddrInfo(string Interface, string IP, int PrefixLen, string State, string Origin);
    public record DnsInfo(string Interface, string[] Servers);
    public record RouteInfo(string Prefix, string Interface, int Metric, string NextHop);
    public record NeighborInfo(string IP, string MAC, string Interface, string State);
    public record FwRuleInfo(string Name, string Dir, string Action, string Proto, string LPort, string RPort);
    public record MyIPv6Info(string IP, int PrefixLen, string Interface, string Type, string State, string Origin);
    public record GatewayInfo(string Interface, string Gateway, string Prefix);
    public record ConnectivityStatus(bool HasConnectivity, string? PublicIP, DateTime CheckTime, string? PingResult);

    // ============================================================
    // 低级: 单次 PowerShell（带缓存 + 合并批量）
    // ============================================================
    private const string PS_UTF8 = "[Console]::OutputEncoding=[Text.Encoding]::UTF8;";

    public static async Task<string> RunPsAsync(string cmd, bool cache = true, int timeout = 10000)
    {
        if (cache && _cache.TryGetValue(cmd, out var c) && DateTime.Now - c.time < CacheTTL)
            return c.result;

        var result = await Task.Run(() =>
        {
            try
            {
                var psi = new ProcessStartInfo
                {
                    FileName = "powershell.exe",
                    Arguments = $"-NoProfile -NoLogo -ExecutionPolicy Bypass -Command \"{PS_UTF8}{cmd.Replace("\"", "\\\"")}\"",
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    CreateNoWindow = true,
                    StandardOutputEncoding = Encoding.UTF8,
                    StandardErrorEncoding = Encoding.UTF8,
                };
                using var p = Process.Start(psi) ?? throw new Exception("PS启动失败");
                string o = p.StandardOutput.ReadToEnd();
                p.WaitForExit(timeout);
                return o;
            }
            catch (Exception ex) { return "ERR|" + ex.Message; }
        });

        if (cache && !result.StartsWith("ERR|"))
            _cache[cmd] = (result, DateTime.Now);
        return result;
    }

    public static void RunAdmin(string cmd)
    {
        try
        {
            Process.Start(new ProcessStartInfo
            {
                FileName = "powershell.exe",
                Arguments = $"-NoProfile -ExecutionPolicy Bypass -Command \"{cmd}\"",
                UseShellExecute = true,
                Verb = "runas",
            });
        }
        catch { }
    }

    // ============================================================
    // 核心: 并行批量刷新
    // ============================================================
    public async Task RefreshAllAsync(Action<string>? status = null)
    {
        // 批次1: 适配器 + 绑定（同一个 PS 进程）
        status?.Invoke("获取适配器...");
        var t1 = RunPsAsync(
            "$a=Get-NetAdapter|Select Name,InterfaceDescription,Status,InterfaceIndex|ConvertTo-Json -Compress;" +
            "$b=Get-NetAdapterBinding -ComponentID ms_tcpip6|Select Name,Enabled|ConvertTo-Json -Compress;" +
            "Write-Host '---MARKER---';$a;Write-Host '---MARKER---';$b", cache: false);

        // 批次2: 地址（独立并行）
        status?.Invoke("获取 IPv6 地址...");
        var t2 = RunPsAsync(
            "Get-NetAdapterAddress -AddressFamily IPv6|Select InterfaceAlias,IPAddress,PrefixLength,AddressState,PrefixOrigin|ConvertTo-Json -Compress",
            cache: false);

        // 批次3: DNS + 路由 + 邻居（合并到一个 PS 进程）
        status?.Invoke("获取 DNS/路由/邻居...");
        var t3 = RunPsAsync(
            "$d=Get-DnsClientServerAddress -AddressFamily IPv6|Select InterfaceAlias,ServerAddresses|ConvertTo-Json -Compress;" +
            "Write-Host '---A---';$d;Write-Host '---B---';" +
            "$r=Get-NetRoute -AddressFamily IPv6|Select DestinationPrefix,InterfaceAlias,RouteMetric,NextHop|ConvertTo-Json -Compress;" +
            "Write-Host '---B---';$r;Write-Host '---C---';" +
            "$n=Get-NetNeighbor -AddressFamily IPv6|Select IPAddress,LinkLayerAddress,InterfaceAlias,State|ConvertTo-Json -Compress;" +
            "Write-Host '---C---';$n",
            cache: false);

        // 批次4: 防火墙（并行）
        var t4 = RunPsAsync(
            "Get-NetFirewallRule -AddressFamily IPv6|Select DisplayName,Direction,Action,Protocol,LocalPort,RemotePort|ConvertTo-Json -Compress",
            cache: false);

        await Task.WhenAll(t1, t2, t3, t4);

        // 解析
        ParseAdapters(t1.Result);
        Addresses = DeserializeList<AddrInfo>(t2.Result, e => new AddrInfo(S(e, "InterfaceAlias"), S(e, "IPAddress"), I(e, "PrefixLength"), S(e, "AddressState"), S(e, "PrefixOrigin")));
        ParseDnsRouteNeighbor(t3.Result);
        FirewallRules = DeserializeList<FwRuleInfo>(t4.Result, e => new FwRuleInfo(S(e, "DisplayName"), S(e, "Direction"), S(e, "Action"), S(e, "Protocol"), S(e, "LocalPort"), S(e, "RemotePort")));

        RefreshTime = DateTime.Now;
    }

    // ============================================================
    // 连通性测试（不缓存）
    // ============================================================
    public async Task<string> PingAsync(string t) => await RunPsAsync($"ping -6 -n 6 {t}", cache: false);
    public async Task<string> TracertAsync(string t) => await RunPsAsync($"tracert -6 -h 20 {t}", cache: false);
    public async Task<string> NetStatAsync() => await RunPsAsync("netstat -p IPv6 -n", cache: false, timeout: 15000);
    public async Task<string> ExecToolAsync(string cmd) => await RunPsAsync(cmd, cache: false, timeout: 15000);

    // ============================================================
    // 解析
    // ============================================================
    private void ParseAdapters(string raw)
    {
        var parts = raw.Split("---MARKER---");
        string aJson = parts.Length > 1 ? parts[1] : "[]";
        string bJson = parts.Length > 2 ? parts[2] : "[]";

        var ad = new Dictionary<string, AdapterDetail>(StringComparer.OrdinalIgnoreCase);
        foreach (var e in ToElements(aJson))
        {
            string n = S(e, "Name");
            if (!string.IsNullOrEmpty(n)) ad[n] = new AdapterDetail(S(e, "InterfaceDescription"), S(e, "Status"), I(e, "InterfaceIndex"));
        }

        Bindings.Clear();
        foreach (var e in ToElements(bJson))
        {
            string n = S(e, "Name");
            bool en = e.TryGetProperty("Enabled", out var ep) && ep.ValueKind == JsonValueKind.True;
            var d = ad.GetValueOrDefault(n, new AdapterDetail("-", "-", 0));
            Bindings.Add(new BindingInfo(n, en, d.Desc, d.Status, d.IfIndex));
        }
    }

    private void ParseDnsRouteNeighbor(string raw)
    {
        var parts = raw.Split(new[] { "---A---", "---B---", "---C---" }, StringSplitOptions.None);
        string dJson = parts.Length > 1 ? parts[1] : "[]";
        string rJson = parts.Length > 3 ? parts[3] : "[]";
        string nJson = parts.Length > 5 ? parts[5] : "[]";

        DnsServers = DeserializeList<DnsInfo>(dJson, e =>
        {
            var srv = new List<string>();
            if (e.TryGetProperty("ServerAddresses", out var sa) && sa.ValueKind == JsonValueKind.Array)
                foreach (var a in sa.EnumerateArray()) srv.Add(a.GetString() ?? "");
            return new DnsInfo(S(e, "InterfaceAlias"), srv.ToArray());
        });

        Routes = DeserializeList<RouteInfo>(rJson, e => new RouteInfo(S(e, "DestinationPrefix"), S(e, "InterfaceAlias"), I(e, "RouteMetric"), S(e, "NextHop")));

        Neighbors = DeserializeList<NeighborInfo>(nJson, e => new NeighborInfo(S(e, "IPAddress"), S(e, "LinkLayerAddress"), S(e, "InterfaceAlias"), S(e, "State")));
    }

    // ============================================================
    // 新功能: 我的 IPv6 地址（分类展示）
    // ============================================================
    public async Task<List<MyIPv6Info>> GetMyIPv6AddressesAsync()
    {
        string raw = await RunPsAsync(
            "Get-NetAdapterAddress -AddressFamily IPv6 | " +
            "Select InterfaceAlias,IPAddress,PrefixLength,AddressState,SuffixOrigin | ConvertTo-Json -Compress",
            cache: false);
        return DeserializeList<MyIPv6Info>(raw, e =>
        {
            string ip = S(e, "IPAddress");
            return new MyIPv6Info(ip, I(e, "PrefixLength"), S(e, "InterfaceAlias"),
                ClassifyIPv6(ip), S(e, "AddressState"), S(e, "SuffixOrigin"));
        });
    }

    public static string ClassifyIPv6(string ip)
    {
        if (string.IsNullOrEmpty(ip)) return "未知";
        if (ip == "::1") return "回环";
        if (ip.StartsWith("fe80", StringComparison.OrdinalIgnoreCase)) return "链路本地";
        if (ip.StartsWith("fc", StringComparison.OrdinalIgnoreCase) ||
            ip.StartsWith("fd", StringComparison.OrdinalIgnoreCase)) return "唯一本地";
        if (ip.StartsWith("ff", StringComparison.OrdinalIgnoreCase)) return "多播";
        if (ip.StartsWith("2002:", StringComparison.OrdinalIgnoreCase)) return "6to4";
        if (ip.StartsWith("2001:", StringComparison.OrdinalIgnoreCase)) return "全局单播";
        if (ip.StartsWith("fe", StringComparison.OrdinalIgnoreCase)) return "站点本地";
        return "全局单播";
    }

    public async Task<ConnectivityStatus> CheckConnectivityAsync()
    {
        string ping = await RunPsAsync("ping -6 -n 2 2001:4860:4860::8888", cache: false, timeout: 8000);
        bool ok = ping.Contains("TTL=") || ping.Contains("回复") || ping.Contains("time=");
        string pub = "";
        try { using var h = new HttpClient { Timeout = TimeSpan.FromSeconds(5) };
            pub = (await h.GetStringAsync("https://api6.ipify.org")).Trim(); ok = true; } catch { }
        return new ConnectivityStatus(ok, string.IsNullOrEmpty(pub) ? null : pub, DateTime.Now, ping);
    }

    public async Task<List<GatewayInfo>> GetDefaultGatewayAsync()
    {
        string raw = await RunPsAsync(
            "Get-NetRoute -AddressFamily IPv6 -DestinationPrefix '::/0' | " +
            "Select InterfaceAlias,NextHop,DestinationPrefix | ConvertTo-Json -Compress", cache: false);
        return DeserializeList<GatewayInfo>(raw, e => new GatewayInfo(
            S(e, "InterfaceAlias"), S(e, "NextHop"), S(e, "DestinationPrefix")));
    }

    // ---- JSON 辅助 ----
    static string S(JsonElement e, string k) => e.TryGetProperty(k, out var p) ? p.GetString() ?? "" : "";
    static int I(JsonElement e, string k) => e.TryGetProperty(k, out var p) && p.TryGetInt32(out int v) ? v : 0;

    static List<JsonElement> ToElements(string json)
    {
        var list = new List<JsonElement>();
        try
        {
            using var doc = JsonDocument.Parse(json.Trim());
            if (doc.RootElement.ValueKind == JsonValueKind.Array)
                foreach (var e in doc.RootElement.EnumerateArray()) list.Add(e.Clone());
        }
        catch { }
        return list;
    }

    static List<T> DeserializeList<T>(string json, Func<JsonElement, T> map)
    {
        var list = new List<T>();
        foreach (var e in ToElements(json))
            try { list.Add(map(e)); } catch { }
        return list;
    }
}
