#!/bin/bash
# Nacos Docker Compose 停止脚本
# 使用方式: ./stop.sh [standalone|mysql|cluster] [--clean]

set -e

MODE=${1:-standalone}
CLEAN=${2:-}
SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"

cd "$SCRIPT_DIR"

echo "==================================="
echo "  Nacos Docker Compose 停止脚本"
echo "==================================="
echo ""

case "$MODE" in
    cluster)
        echo "停止模式: 集群模式"
        
        if [ "$CLEAN" = "--clean" ]; then
            echo "清理模式: 删除所有数据"
            docker-compose -f docker-compose.cluster.yml down -v
            rm -rf mysql/data/* nacos/logs/* nacos/data/*
        else
            docker-compose -f docker-compose.cluster.yml down
        fi
        ;;
    mysql)
        echo "停止模式: 单机模式 + MySQL"
        
        if [ "$CLEAN" = "--clean" ]; then
            echo "清理模式: 删除所有数据"
            docker-compose -f docker-compose.mysql.yml down -v
            rm -rf mysql/data/* nacos/logs/* nacos/data/*
        else
            docker-compose -f docker-compose.mysql.yml down
        fi
        ;;
    *)
        echo "停止模式: 单机模式"
        
        if [ "$CLEAN" = "--clean" ]; then
            echo "清理模式: 删除所有数据"
            docker-compose down -v
            rm -rf nacos/logs/* nacos/data/*
        else
            docker-compose down
        fi
        ;;
esac

echo ""
echo "服务已停止!"
echo ""
