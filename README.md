# 🌐 IPv6 管理工具

Windows 平台下的 IPv6 网络配置管理工具，采用深色毛玻璃 UI，提供地址 / 路由 / DNS / 防火墙查看、连通性诊断、一键体检与配置报告导出。

![.NET](https://img.shields.io/badge/.NET-8.0-512BD4)
![Platform](https://img.shields.io/badge/Platform-Windows-0078D4)
![License](https://img.shields.io/badge/License-MIT-green)

---

## ✨ 功能

| 模块 | 功能 |
|------|------|
| **我的 IPv6** | 按类型（全局单播 / 链路本地 / 唯一本地 / 回环 / 多播 / Teredo / 6to4）分类展示本机全部 IPv6 地址，点击卡片复制 |
| **网络适配器** | 查看每个适配器的 IPv6 绑定、速率、MAC、连接状态，可单独启用 / 禁用（带提权结果反馈） |
| **IPv6 地址** | 按适配器分组查看地址、前缀长度、地址状态与地址类型，双击复制 |
| **DNS 设置** | 查看 IPv6 DNS 配置；域名 AAAA 解析；对每个已配置 DNS 服务器做解析延迟测速 |
| **连接测试** | .NET 原生 IPv6 Ping（6 次逐包结果 + 统计）、Traceroute |
| **路由表** | IPv6 路由表查看 |
| **隧道状态** | 通过 `Get-NetTeredoConfiguration` / `Get-Net6to4Configuration` / `Get-NetIsatapConfiguration` 结构化读取，不依赖系统语言 |
| **防火墙规则** | 无需管理员即可列出规则；点击“管理员查询详情”可补全协议 / 端口 / 远程地址 |
| **网络扫描** | NDP 邻居发现、多播 Ping（自动带接口作用域）、IPv6 活动连接 |
| **监听端口** | 列出本机所有监听 IPv6 的 TCP / UDP 端口及所属进程，方便核对对外服务 |
| **一键体检** | 自动检查绑定、全局地址、默认路由、DNS、DAD 状态、连通性、公网地址，给出得分与修复建议 |
| **网络监控** | 每 60 秒检测一次连通性与公网 IPv6 变化，断开 / 变化时托盘气泡提醒 |
| **高级工具** | 重置堆栈、刷新 DNS、隐私扩展、前缀策略等 12 项命令，以及 **IPv6 子网计算器** |
| **配置报告** | 一键生成完整报告，可复制或导出 TXT / JSON / HTML |

## 🚀 快速开始

### 环境要求
- Windows 10 / Windows 11
- [.NET 8.0 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)（运行发布版则只需 .NET 8 桌面运行时）
- 启用 / 禁用 IPv6 绑定、查询防火墙详情、重置堆栈等操作需要**管理员权限**（会弹出 UAC）

### 构建运行

```bash
git clone https://github.com/Bade-Gusi/ipv6.git
cd ipv6
dotnet run --project ipv6
```

或使用 Visual Studio 打开 `ipv6.sln` 直接运行。

### 发布单文件

```bash
dotnet publish ipv6 -c Release -r win-x64 --self-contained false
```

产物为单个 `ipv6.exe`（依赖 .NET 8 桌面运行时）。

### 命令行参数

| 参数 | 说明 |
|------|------|
| `--check` / `-c` | 检测模式：检查 IPv6 绑定，必要时自动启用，随后自动退出（供计划任务调用） |

### 开机自动检测

在仪表盘点击「开机检测」会注册计划任务 `IPv6ManagerCheck`（登录时以最高权限静默运行 `--check`），
不会再在每次登录时弹出 UAC。关闭同理。

## 🎞 动画（改编自 StyleKit，全部使用非线性缓动）

缓动曲线：cubic-bezier(0.22, 1, 0.36, 1)、BackOut、ElasticOut、Spring、OutCubic 等，没有线性动画。

| 分类 | 已实现 |
|------|--------|
| 入场 | 淡入上滑（瓷砖）、缩放淡入（页面）、右侧滑入 / 滑出、弹性入场（对话框）、弹簧缩放（结果窗口） |
| 悬停 | 悬浮抬升、悬浮发光、聚光灯卡片（鼠标位置高光）、边框光束、磁吸悬浮、靠近显现（复制提示滑入） |
| 指针 | 涟漪点击（瓷砖 / 所有按钮）、焦点描边、卡片悬停描边 |
| 文字 | 文字渐变流动（标题高光扫过）、打字机字幕 + 闪烁光标、文字揭示（标题升入）、文字解密（连接状态）、数字滚动（指标 / 体检得分） |
| 加载 | 三点弹跳、骨架屏脉冲、微光扫过、进度条光泽段 |
| 背景 | 背景渐变流动、形状变形（环境光斑） |
| 过渡 | 滑动切换、交叉淡入淡出（页面）、形态过渡（工具栏卡片展开 / 收起） |
| 微交互 | 子元素错开动画（瓷砖 / 指标）、抖动提示（输入校验）、脉搏跳动 + 脉冲光环（连接状态点）、彩纸爆炸（复制地址）、弹性回弹（按钮按压）、Toast 滑入淡出 |

## 🎨 界面特点

- **13 个功能瓷砖**：平铺式导航，支持 Tab 键聚焦与回车打开
- **动画**：见上表，全部非线性缓动；环境光斑背景仅瓷砖页运行，隐藏时自动停表
- **深色主题**：统一设计令牌，对比度满足可读性要求
- **系统托盘**：最小化到托盘后台运行，托盘菜单可启动 / 停止网络监控

## 🔧 技术栈

- **框架**：.NET 8.0 Windows Forms
- **数据源**：PowerShell 结构化 cmdlet（`Get-NetIPAddress`、`Get-NetRoute`、`Get-NetNeighbor`、`Get-NetIPInterface`、`Get-DnsClientServerAddress`、`Get-NetFirewallRule`、`Get-NetTCPConnection` 等）+ `netsh`
- **执行方式**：PowerShell 通过 `-EncodedCommand` 调用，彻底避免引号 / 注入问题；统一异步读取 stdout/stderr，带超时与进程终止
- **提权**：通过 `runas` 执行并以临时文件回传结果，可感知用户取消与命令失败
- **UI**：OwnerDraw 自定义绘制，无第三方 UI 库；绘图资源统一缓存

## 📸 截图

<!-- TODO: 添加截图 -->

## 🛠 常见问题

- **页面数据为空**：以前版本误用了不存在的 `Get-NetAdapterAddress`、`Get-NetFirewallRule -AddressFamily`。v2.1 已改为正确的 cmdlet；现在 PowerShell 的 stderr 会直接写入「系统日志」，不再静默失败。
- **连通性显示未连接**：探测目标为国内 IPv6 DNS（阿里 / 腾讯 / 下一代互联网 / CERNET）并追加公网地址探测；ICMP 被拦截时仍可通过公网地址判定为已连接。
- **防火墙端口列为 `-`**：协议 / 端口过滤器的查询需要管理员权限，请点「管理员查询详情」。
- **双击运行闪退**：查看 `%LOCALAPPDATA%\IPv6Manager\crash.log`。

## 📄 许可证

[MIT](LICENSE)
