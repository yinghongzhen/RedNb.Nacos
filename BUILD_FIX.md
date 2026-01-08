# ?? 构建问题快速修复

如果您遇到以下错误：

```
Failed to open argument file : error : C:\Users\风骑士之怒\AppData\Local\Temp\tmpXXXXXXXX.rsp
```

## ?? 快速解决（3 种方法）

### 方法 1: 使用构建脚本（最简单）

```powershell
# PowerShell
.\build.ps1

# 或 CMD
build.cmd
```

### 方法 2: 一键设置环境变量

**管理员权限**运行以下命令：

```powershell
# PowerShell（推荐）
.\setup-env.ps1

# 或 CMD
setup-env.cmd
```

然后**重启 Visual Studio**。

### 方法 3: 手动设置

1. 右键"此电脑" → 属性 → 高级系统设置 → 环境变量
2. 设置 `TEMP` 和 `TMP` 为 `D:\temp`
3. 重启 Visual Studio

## ?? 详细说明

查看 [BUILD_FIX_README.md](BUILD_FIX_README.md) 了解详细信息和技术原因。

## ? 验证

运行以下命令确认构建成功：

```powershell
.\build.ps1
```

如果看到 "生成 成功"，问题已解决！
