# Nacos Docker Compose 部署指南

基于 Nacos 官方文档，提供 Docker Compose 一键部署方案。

- **Nacos 版本**: 3.1.1 (最新稳定版)
- **官方文档**: https://nacos.io/docs/latest/quickstart/quick-start/

## 部署模式

| 文件 | 模式 | 存储 | 适用场景 |
|------|------|------|----------|
| `docker-compose.yml` | 单机 | 内嵌 (Derby) | 快速体验、开发测试 |
| `docker-compose.mysql.yml` | 单机 | MySQL | 开发测试、数据持久化 |
| `docker-compose.cluster.yml` | 集群 (3节点) | MySQL | 生产环境 |

## 快速开始

### 1. 单机模式 (最简单)

```bash
cd deploy/docker-compose
docker-compose up -d
```

### 2. 单机模式 + MySQL

```bash
docker-compose -f docker-compose.mysql.yml up -d
```

### 3. 集群模式

```bash
docker-compose -f docker-compose.cluster.yml up -d
```

### 4. 使用脚本启动

```bash
# Linux/Mac
./start.sh                    # 单机模式
./start.sh mysql              # 单机 + MySQL
./start.sh cluster            # 集群模式

# Windows
start.bat                     # 单机模式
start.bat mysql               # 单机 + MySQL
start.bat cluster             # 集群模式
```

## 访问地址

| 服务 | 地址 |
|------|------|
| Nacos 控制台 | http://localhost:8080 |
| HTTP API | http://localhost:8848 |
| gRPC 端口 | localhost:9848 |

> 首次访问需要初始化 `nacos` 用户密码 (Nacos 3.0+ 安全特性)

## 配置说明

### 环境变量

复制 `.env.example` 为 `.env` 并根据需要修改：

```bash
cp .env.example .env
```

| 变量 | 说明 | 默认值 |
|------|------|--------|
| `MYSQL_PASSWORD` | MySQL 密码 | nacos |
| `JVM_XMS` | JVM 初始堆 | 512m |
| `JVM_XMX` | JVM 最大堆 | 512m |
| `NACOS_AUTH_TOKEN` | 鉴权密钥 (Base64) | 默认密钥 |

### 端口说明

| 端口 | 说明 |
|------|------|
| 8080 | 控制台 (Nacos 3.0+) |
| 8848 | HTTP API |
| 9848 | gRPC 客户端 |
| 9849 | gRPC 服务端 |

## 客户端配置

```json
{
  "RedNb:Nacos": {
    "ServerAddresses": ["http://localhost:8848"],
    "UserName": "nacos",
    "Password": "your_password",
    "UseGrpc": true
  }
}
```

## 常用命令

```bash
# 查看日志
docker-compose logs -f nacos

# 停止服务
docker-compose down

# 停止并清理数据
docker-compose down -v
```

## 生产建议

- 使用集群模式 (至少 3 节点)
- 修改默认鉴权密钥
- 推荐配置: 2C4G 60G
- 配置外部 MySQL 集群
