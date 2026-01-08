# RedNb.Nacos 测试指南

本文档说明如何运行 RedNb.Nacos 项目的测试套件。

## 测试类型

项目包含两种类型的测试：

### 1. 单元测试
- **不需要外部依赖**
- 使用 Mock 对象模拟依赖
- 快速执行
- 自动在 CI/CD 中运行

### 2. 集成测试
- **需要实际运行的 Nacos 服务器**
- 测试与真实 Nacos 服务的集成
- 标记为 `[Trait("Category", "Integration")]`
- 需要手动配置才能运行

## 运行测试

### 运行所有单元测试（推荐用于日常开发）

```bash
dotnet test --filter "Category!=Integration"
```

### 运行所有测试（包括集成测试）

**前提条件：**
1. 确保 Nacos 服务器正在运行（默认 `http://localhost:8848`）
2. 配置测试设置（见下方）

```bash
dotnet test
```

### 只运行集成测试

```bash
dotnet test --filter "Category=Integration"
```

## 配置集成测试

### 步骤 1: 启动 Nacos 服务器

使用 Docker 快速启动 Nacos（推荐）：

```bash
docker run --name nacos-standalone -e MODE=standalone -p 8848:8848 -p 9848:9848 -d nacos/nacos-server:v2.3.0
```

### 步骤 2: 配置测试设置

1. 复制模板文件：
   ```bash
   cd tests/RedNb.Nacos.Tests
   cp testsettings.template.json testsettings.json
   ```

2. 编辑 `testsettings.json`（如果需要修改默认配置）：
   ```json
   {
     "Nacos": {
       "ServerAddress": "http://localhost:8848",
       "GrpcPort": 9848,
       "Namespace": "public",
       "Username": "nacos",
       "Password": "nacos"
     }
   }
   ```

**注意：** `testsettings.json` 已添加到 `.gitignore`，不会提交到版本控制。

### 步骤 3: 运行集成测试

```bash
dotnet test --filter "Category=Integration"
```

## 测试覆盖率

查看测试覆盖率报告：

```bash
dotnet test --collect:"XPlat Code Coverage"
```

## CI/CD 集成

在持续集成环境中，建议只运行单元测试以保持快速反馈：

```yaml
# GitHub Actions 示例
- name: Run Unit Tests
  run: dotnet test --filter "Category!=Integration" --no-build --verbosity normal
```

集成测试可以配置为：
- 夜间构建
- 手动触发
- 在专门的测试环境中运行

## 测试文件说明

### 集成测试文件

- **`ConfigIntegrationTests.cs`** - 配置中心集成测试
  - 配置发布、获取、更新、删除
  - 配置监听和热更新
  - 多种配置格式支持（JSON、YAML、Properties）
  - 中文和特殊字符处理

- **`GrpcIntegrationTests.cs`** - gRPC 通信集成测试
  - gRPC 协议测试
  - 服务注册发现

### 单元测试文件

项目中的其他测试文件为单元测试，不需要外部依赖即可运行。

## 故障排查

### 集成测试失败：连接被拒绝

**错误信息：**
```
System.Net.Sockets.SocketException: 由于目标计算机积极拒绝，无法连接。
```

**解决方法：**
1. 确认 Nacos 服务器正在运行
2. 检查 `testsettings.json` 中的服务器地址
3. 确认防火墙未阻止端口 8848 和 9848

### 测试超时

集成测试可能需要较长时间（特别是配置监听测试），这是正常的。典型运行时间：
- 单元测试：< 1 秒
- 集成测试：1-5 分钟

## 最佳实践

1. **日常开发：** 只运行单元测试以获得快速反馈
2. **功能完成前：** 运行集成测试验证与 Nacos 的集成
3. **提交代码前：** 确保所有单元测试通过
4. **发布前：** 运行完整测试套件（包括集成测试）

## 测试数据清理

集成测试会自动清理测试数据：
- 测试使用前缀 `integration-test-` 的 DataId
- 测试结束后自动删除创建的配置
- 如有残留，可手动在 Nacos 控制台删除

## 编写新测试

### 单元测试
- 放在适当的测试类中
- 使用 Mock 对象模拟依赖
- 确保测试独立且可重复

### 集成测试
- 添加 `[Trait("Category", "Integration")]` 特性
- 使用唯一的 DataId（建议加时间戳或 GUID）
- 在测试清理中删除创建的资源
- 添加适当的延迟以等待 Nacos 数据同步

```csharp
[Trait("Category", "Integration")]
public async Task MyNewIntegrationTest()
{
    // 测试代码
}
```
