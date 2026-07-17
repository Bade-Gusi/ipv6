# 🌐 IPv6 管理工具

Windows 平台下的 IPv6 网络配置管理工具，采用毛玻璃 UI 设计，提供全面的 IPv6 诊断与管理功能。

![.NET](https://img.shields.io/badge/.NET-8.0-512BD4)
![Platform](https://img.shields.io/badge/Platform-Windows-0078D4)
![License](https://img.shields.io/badge/License-MIT-green)

---

## ✨ 功能

| 模块 | 功能 |
|------|------|
| **我的 IPv6** | 展示本机所有 IPv6 地址（全局单播/链路本地/唯一本地/回环/多播），自动分类着色 |
| **网络适配器** | 查看和管理每个适配器的 IPv6 绑定状态，一键启用/禁用 |
| **IPv6 地址** | 按适配器分组查看所有 IPv6 地址详情 |
| **DNS 设置** | 查看各适配器的 IPv6 DNS 服务器配置 |
| **连接测试** | Ping / Traceroute 连通性诊断 |
| **路由表** | IPv6 路由表查看 |
| **隧道状态** | Teredo / 6to4 / ISATAP 隧道状态 |
| **防火墙规则** | IPv6 相关的 Windows Defender 防火墙规则 |
| **网络扫描** | NDP 邻居发现、多播 Ping、活动连接扫描 |
| **高级工具** | 重置堆栈、刷新 DNS、隐私扩展、前缀策略等 12 项诊断命令 |
| **配置报告** | 一键生成完整的系统 IPv6 配置报告并导出 |

## 🚀 快速开始

### 环境要求
- Windows 10 / Windows 11
- [.NET 8.0 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)
- 部分功能需要**管理员权限**

### 构建运行

```bash
git clone https://github.com/Bade-Gusi/ipv6.git
cd ipv6
dotnet run --project ipv6
```

或使用 Visual Studio 打开 `ipv6.sln` 直接运行。

### 命令行参数

| 参数 | 说明 |
|------|------|
| `--check` / `-c` | 检测模式：开机自动检查并启用 IPv6，检测后自动退出 |

## 🎨 界面特点

- **毛玻璃设计**：半透明磨砂质感，环境光晕背景
- **平铺式导航**：11 个功能瓷砖，一目了然
- **流畅动画**：页面滑入/滑出、瓷砖入场、连通性脉冲
- **深色主题**：护眼暗色配色方案
- **系统托盘**：最小化到托盘，后台运行

## 📸 截图

<!-- TODO: 添加截图 -->

## 🔧 技术栈

- **框架**：.NET 8.0 Windows Forms
- **数据源**：PowerShell cmdlet（`Get-NetAdapter`、`Get-NetAdapterAddress`、`netsh` 等）
- **UI**：OwnerDraw 自定义绘制，无第三方 UI 库

## 📄 许可证

MIT
