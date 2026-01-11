# Contributing to RedNb.Nacos

首先，感谢你考虑为 RedNb.Nacos 做出贡献！ 🎉

以下是一套指导方针，帮助你参与到项目中来。

## 📋 目录

- [行为准则](#行为准则)
- [如何贡献](#如何贡献)
- [开发环境设置](#开发环境设置)
- [代码规范](#代码规范)
- [提交规范](#提交规范)
- [Pull Request 流程](#pull-request-流程)
- [问题报告](#问题报告)

## 行为准则

本项目采用 [Contributor Covenant](https://www.contributor-covenant.org/) 行为准则。参与本项目即表示你同意遵守其条款。

## 如何贡献

### 报告 Bug

如果你发现了 Bug，请通过 [GitHub Issues](https://github.com/redNb/RedNb.Nacos/issues/new?template=bug_report.md) 提交报告。

报告时请包含：
- 清晰的标题和描述
- 重现步骤
- 期望行为与实际行为
- 环境信息（.NET 版本、Nacos 版本、操作系统）
- 如果可能，提供最小可重现代码

### 功能建议

欢迎通过 [GitHub Issues](https://github.com/redNb/RedNb.Nacos/issues/new?template=feature_request.md) 提交功能建议。

### 代码贡献

1. Fork 本仓库
2. 创建功能分支 (`git checkout -b feature/amazing-feature`)
3. 编写代码和测试
4. 提交更改 (`git commit -m 'feat: add amazing feature'`)
5. 推送到分支 (`git push origin feature/amazing-feature`)
6. 创建 Pull Request

## 开发环境设置

### 前置条件

- [.NET 10.0 SDK](https://dotnet.microsoft.com/download) 或更高版本
- [Visual Studio 2022](https://visualstudio.microsoft.com/) 或 [VS Code](https://code.visualstudio.com/)
- [Docker](https://www.docker.com/)（用于运行 Nacos 服务器）

### 克隆仓库

```bash
git clone https://github.com/redNb/RedNb.Nacos.git
cd RedNb.Nacos
```

### 启动 Nacos 服务器

```bash
docker run -d --name nacos \
  -e MODE=standalone \
  -p 8848:8848 \
  -p 9848:9848 \
  nacos/nacos-server:v3.1.1
```

### 构建项目

```bash
dotnet build
```

### 运行测试

```bash
dotnet test
```

### 运行示例

```bash
cd samples/RedNb.Nacos.Sample.Console
dotnet run
```

## 代码规范

### C# 编码规范

- 遵循 [Microsoft C# 编码约定](https://docs.microsoft.com/en-us/dotnet/csharp/fundamentals/coding-style/coding-conventions)
- 使用 4 个空格缩进
- 每行代码不超过 120 个字符
- 所有公共 API 必须有 XML 文档注释

### 命名约定

```csharp
// 类和接口：PascalCase
public class NacosConfigService { }
public interface IConfigService { }

// 方法：PascalCase
public async Task<string> GetConfigAsync() { }

// 参数和局部变量：camelCase
public void Method(string configName, int timeout) { }

// 私有字段：_camelCase
private readonly ILogger _logger;

// 常量：PascalCase
public const string DefaultGroup = "DEFAULT_GROUP";
```

### 异步编程

- 所有 I/O 操作使用异步方法
- 异步方法以 `Async` 后缀命名
- 始终使用 `CancellationToken`

```csharp
public async Task<string?> GetConfigAsync(
    string dataId, 
    string group, 
    long timeoutMs, 
    CancellationToken cancellationToken = default)
{
    // ...
}
```

### 文档注释

```csharp
/// <summary>
/// Gets configuration content from Nacos server.
/// </summary>
/// <param name="dataId">The data ID of the configuration.</param>
/// <param name="group">The group name.</param>
/// <param name="timeoutMs">Timeout in milliseconds.</param>
/// <param name="cancellationToken">Cancellation token.</param>
/// <returns>Configuration content, or null if not found.</returns>
/// <exception cref="NacosException">Thrown when server returns an error.</exception>
public async Task<string?> GetConfigAsync(
    string dataId, 
    string group, 
    long timeoutMs, 
    CancellationToken cancellationToken = default);
```

## 提交规范

我们使用 [Conventional Commits](https://www.conventionalcommits.org/) 规范。

### 提交格式

```
<type>(<scope>): <description>

[optional body]

[optional footer(s)]
```

### 类型

| 类型 | 描述 |
|------|------|
| `feat` | 新功能 |
| `fix` | Bug 修复 |
| `docs` | 文档更新 |
| `style` | 代码格式（不影响功能） |
| `refactor` | 代码重构 |
| `perf` | 性能优化 |
| `test` | 测试相关 |
| `chore` | 构建/工具变更 |

### 示例

```bash
feat(config): add fuzzy watch support

Add FuzzyWatchAsync method to IConfigService for pattern-based
configuration listening.

Closes #123
```

```bash
fix(naming): fix heartbeat interval calculation

The heartbeat interval was incorrectly calculated when server
returns custom interval. This fix ensures the client respects
the server-specified interval.

Fixes #456
```

## Pull Request 流程

### 创建 PR 前

1. 确保代码通过所有测试
2. 确保代码符合规范（运行 `dotnet format`）
3. 更新相关文档
4. 如果是新功能，添加相应测试

### PR 标题

使用 Conventional Commits 格式：

```
feat(config): add config encryption filter support
fix(naming): resolve connection timeout issue
docs: update installation guide
```

### PR 描述

使用以下模板：

```markdown
## 描述
简要描述此 PR 的更改内容。

## 更改类型
- [ ] Bug 修复
- [ ] 新功能
- [ ] 破坏性变更
- [ ] 文档更新

## 如何测试
描述如何测试这些更改。

## 检查清单
- [ ] 我已阅读贡献指南
- [ ] 代码通过所有测试
- [ ] 我已添加必要的文档
- [ ] 我已添加相应的测试

## 相关 Issue
Closes #xxx
```

### 代码审查

- 所有 PR 需要至少一位维护者审查
- 请及时回复审查意见
- 如有必要，请更新代码并推送新的提交

## 问题报告

### Bug 报告模板

```markdown
**描述**
简要描述 Bug。

**重现步骤**
1. ...
2. ...
3. ...

**期望行为**
描述你期望发生什么。

**实际行为**
描述实际发生了什么。

**环境**
- OS: [e.g., Windows 11]
- .NET: [e.g., 10.0]
- Nacos: [e.g., 3.1.1]
- SDK 版本: [e.g., 1.0.0]

**附加信息**
任何其他有助于理解问题的信息。
```

### 功能请求模板

```markdown
**功能描述**
简要描述你希望的功能。

**使用场景**
描述这个功能将如何使用。

**替代方案**
你考虑过的任何替代方案或功能。

**附加信息**
任何其他相关信息。
```

## 🏷️ 问题标签

| 标签 | 描述 |
|------|------|
| `bug` | Bug 报告 |
| `enhancement` | 功能增强 |
| `documentation` | 文档相关 |
| `good first issue` | 适合新手 |
| `help wanted` | 需要帮助 |
| `question` | 问题咨询 |
| `wontfix` | 不会修复 |

---

再次感谢你的贡献！ ❤️
