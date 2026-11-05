<div align="center">

# Paper Plane Glow Cursor

一个轻盈的 Windows 纸飞机发光光标，点击时会平滑放大并回弹。

[简体中文](README.md) | [English](README.en.md)

[![License: MIT](https://img.shields.io/badge/License-MIT-yellow.svg)](LICENSE)
[![Platform](https://img.shields.io/badge/platform-Windows%2010%20%7C%2011-0078D4.svg)](#系统要求)
[![Latest Release](https://img.shields.io/github/v/release/MiaomiaoYuHao/PaperPlaneGlowCursor?display_name=tag&sort=semver)](https://github.com/MiaomiaoYuHao/PaperPlaneGlowCursor/releases/latest)
[![Build](https://github.com/MiaomiaoYuHao/PaperPlaneGlowCursor/actions/workflows/build.yml/badge.svg)](https://github.com/MiaomiaoYuHao/PaperPlaneGlowCursor/actions/workflows/build.yml)

![Paper Plane Glow Cursor](docs/preview.png)

</div>

## 预览

![点击动画](docs/click-preview.gif)

- 普通光标：40 px
- 点击动画：`40 → 42 → 44 → 46 → 48 → 48 → 46 → 44 → 42 → 40 px`
- 动画时长约 200 ms，跟随鼠标位置
- 支持高 DPI，不需要管理员权限

## 特性

- 透明置顶覆盖层，不阻塞鼠标点击
- 点击时平滑放大并回弹，动画结束后自动恢复
- 使用 DWM 合成同步，避免点击开头的闪烁
- 托盘菜单可随时启用/禁用动画或退出
- 安装时自动备份原来的光标配置
- 一键安装与卸载，不修改系统文件
- 完全开源，无网络请求、无遥测、无后台服务

## 快速安装

### 推荐方式：下载 Release

1. 打开 [最新 Release](https://github.com/MiaomiaoYuHao/PaperPlaneGlowCursor/releases/latest)。
2. 下载 `PaperPlaneGlowCursor-v1.0.0-win.zip`。
3. 将 ZIP **完整解压**到一个长期保留的文件夹，例如：
   `C:\Users\<你的用户名>\Apps\PaperPlaneGlowCursor`
4. 双击 `Install.cmd`。
5. 安装完成后，托盘会出现纸飞机图标。左键点击即可看到动画。

> 不要直接在压缩包预览窗口中运行文件。安装后不要移动或删除该文件夹；如果移动了文件夹，请在新位置重新运行 `Install.cmd`。

### 命令行安装

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File .\Install-Current.ps1
```

安装脚本只写入当前用户范围内的以下位置：

- `HKCU\Control Panel\Cursors`
- `HKCU\Software\Microsoft\Windows\CurrentVersion\Run`

不会请求管理员权限。

### Windows SmartScreen 提示

本项目生成的 `PaperPlaneGlowClick.exe` 没有商业代码签名证书，Windows 可能显示“未知发布者”。仓库包含完整 C# 源码和构建脚本，你可以先检查源码，再自行执行 `build.ps1` 构建。

## 使用

- **左键点击**：播放纸飞机放大回弹动画
- **托盘图标右键**：
  - `Click Grow: Enabled / Disabled`：启用或禁用点击动画
  - `Exit`：退出助手并恢复普通光标
- 动画仅针对普通箭头光标；在文本输入框、链接手型等光标上不会触发

## 更新

1. 右键托盘图标，选择 `Exit`。
2. 下载新版本并解压到原目录，覆盖旧文件。
3. 重新运行 `Install.cmd`。

请保留 `previous-cursor-config.json`，卸载时需要使用它恢复安装前的光标配置。

## 卸载

双击 `Uninstall.cmd`，或运行：

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File .\Restore-Previous.ps1
```

卸载脚本会：

- 停止点击动画助手
- 删除开机启动项
- 恢复安装前备份的光标路径和大小

如果你只想暂时关闭动画，不想恢复原光标，可运行：

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File .\Uninstall-ClickEffect.ps1
```

## 系统要求

- Windows 10 或 Windows 11
- 64 位 Windows（助手为 AnyCPU，可在 64 位系统运行）
- .NET Framework 4.8（Windows 10/11 默认包含）

目前不支持 macOS、Linux、Windows 7/8。

## 从源码构建

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File .\build.ps1
```

构建使用 Windows 自带的 .NET Framework C# 编译器，不需要 Visual Studio，也不需要联网还原依赖。

生成文件：

```text
dist\PaperPlaneGlowClick.exe
```

## 工作原理

助手是一个轻量的 WinForms 托盘程序：

1. 常驻一个透明的置顶分层窗口，并预先完成桌面合成。
2. 检测到左键按下且当前是普通箭头光标时开始动画。
3. 将系统光标临时替换为透明光标，同时由覆盖窗口绘制 `40–48 px` 的动画帧。
4. 每 20 ms 更新一帧，并让覆盖窗口跟随鼠标位置。
5. 动画结束后恢复系统光标，再将覆盖窗口移出屏幕。
6. 在切换系统光标和覆盖窗口时调用 `DwmFlush`，减少合成时序造成的闪烁。

## 项目结构

```text
.
├─ Install.cmd                  # 双击安装
├─ Uninstall.cmd                # 双击卸载
├─ Install-Current.ps1          # 安装逻辑
├─ Restore-Previous.ps1         # 卸载并恢复原光标
├─ Uninstall-ClickEffect.ps1    # 仅停用点击动画
├─ build.ps1                    # 从源码构建 exe
├─ PaperPlaneGlowClick.exe      # 已编译的 Windows 助手
├─ PaperPlaneGlow.cur           # 普通光标
├─ PaperPlaneGlow-Blank.cur     # 动画期间使用的透明光标
├─ PaperPlaneGlow-Frame*.png    # 动画帧
├─ PaperPlaneGlow.ico           # 托盘图标
├─ source/
│  ├─ PaperPlaneGlowClick.cs    # C# 源码
│  └─ PaperPlaneGlow.svg        # 矢量源文件
├─ docs/                        # README 预览图
├─ CHANGELOG.md
└─ LICENSE
```

## 安全与隐私

- 不联网、不上传数据、不包含遥测。
- 不开机驻留服务，只通过当前用户的 `Run` 注册表项启动托盘助手。
- 不修改其他用户的设置，不需要管理员权限。
- 所有安装、卸载和构建逻辑均为可审查的 PowerShell/C# 源码。

## 已知限制

- 仅支持 Windows 和普通箭头光标。
- 只响应鼠标左键。
- 安装路径与当前文件夹绑定，移动文件夹后需要重新安装。
- 如果之后切换了 Windows 光标主题，需要重新运行安装脚本。
- 未使用商业代码签名证书，因此可能出现 SmartScreen 提示。

## 常见问题

### 点击后没有动画？

1. 确认托盘中有纸飞机图标。
2. 右键托盘图标，确认 `Click Grow` 处于 `Enabled` 状态。
3. 确认当前显示的是普通箭头光标。
4. 重新运行 `Install.cmd`。
5. 查看同目录下的 `PaperPlaneGlowClick.log`。

### 光标消失了？

右键托盘图标并选择 `Exit`，然后重新运行 `Install.cmd`。如果仍然异常，运行 `Uninstall.cmd` 恢复原光标配置。

### 可以自定义颜色、大小或动画时长吗？

可以。配色和路径位于 `source/PaperPlaneGlow.svg`，动画帧位于根目录的 `PaperPlaneGlow-Frame*.png`，帧序列位于 `source/PaperPlaneGlowClick.cs` 的 `sequence` 数组。

## 贡献

欢迎提交 Issue 或 Pull Request。修改 C# 源码后请运行：

```powershell
.\build.ps1
```

并在 Windows 10/11 上实际测试安装、点击动画和卸载流程。

## License

[MIT](LICENSE) © 2026 MiaomiaoYuHao