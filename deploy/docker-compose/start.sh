#!/bin/bash
# Nacos Docker Compose 启动脚本
# Nacos 版本: 3.1.1
# 使用方式: ./start.sh [standalone|mysql|cluster]

set -e

MODE=${1:-standalone}
SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"

cd "$SCRIPT_DIR"

echo "==================================="
echo "  Nacos Docker Compose 部署脚本"
echo "  版本: 3.1.1"
echo "==================================="
echo ""

# 检查 Docker 是否运行
if ! docker info > /dev/null 2>&1; then
    echo "错误: Docker 未运行，请先启动 Docker"
    exit 1
fi

# 创建必要的目录
mkdir -p mysql/data mysql/init nacos/logs nacos/data

# 复制环境变量文件（如果不存在）
if [ ! -f .env ] && [ -f .env.example ]; then
    echo "复制 .env.example 到 .env"
    cp .env.example .env
fi

case "$MODE" in
    cluster)
        echo "启动模式: 集群模式 (3 节点 + MySQL)"
        echo ""
        
        # 创建集群模式的日志和数据目录
        mkdir -p nacos/logs/nacos1 nacos/logs/nacos2 nacos/logs/nacos3
        mkdir -p nacos/data/nacos1 nacos/data/nacos2 nacos/data/nacos3
        
        docker-compose -f docker-compose.cluster.yml up -d
        
        echo ""
        echo "集群模式启动完成!"
        echo ""
        echo "访问地址:"
        echo "  - Nginx 负载均衡: http://localhost"
        echo "  - Nacos 节点 1:   http://localhost:8080"
        echo "  - Nacos 节点 2:   http://localhost:8081"
        echo "  - Nacos 节点 3:   http://localhost:8082"
        ;;
    mysql)
        echo "启动模式: 单机模式 + MySQL"
        echo ""
        
        docker-compose -f docker-compose.mysql.yml up -d
        
        echo ""
        echo "单机模式 (MySQL) 启动完成!"
        echo ""
        echo "访问地址:"
        echo "  - Nacos 控制台: http://localhost:8080"
        echo "  - HTTP API:     http://localhost:8848"
        ;;
    *)
        echo "启动模式: 单机模式 (内嵌存储)"
        echo ""
        
        docker-compose up -d
        
        echo ""
        echo "单机模式启动完成!"
        echo ""
        echo "访问地址:"
        echo "  - Nacos 控制台: http://localhost:8080"
        echo "  - HTTP API:     http://localhost:8848"
        ;;
esac

echo ""
echo "首次访问需要初始化 nacos 用户密码"
echo ""
echo "常用命令:"
echo "  查看日志: docker-compose logs -f nacos"
echo "  停止服务: ./stop.sh $MODE"
echo ""
