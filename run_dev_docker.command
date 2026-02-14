#!/bin/bash

# Переходим в директорию где лежит скрипт (и docker-compose.yml)
cd "$(dirname "$0")"

# Устанавливаем заголовок окна терминала
echo -ne "\033]0;Bookify Launcher\007"

echo "=============================================="
echo "     STARTING BOOKIFY ENVIRONMENT"
echo "=============================================="

# 1. Сначала останавливаем всё старое, чтобы не было конфликтов
echo ""
echo "[1/2] Cleaning up old containers..."
docker-compose down --remove-orphans

# 2. Собираем и запускаем
# --build: пересобирает образ (важно для C# кода)
# --force-recreate: пересоздает контейнеры с нуля
# -d: Detached mode (в фоне, чтобы консоль не висела)
echo ""
echo "[2/2] Building and Starting services..."
docker-compose up --build --force-recreate -d

echo ""
echo "=============================================="
echo "     DONE! STATUS CHECK:"
echo "=============================================="
echo ""

# Показываем статус запущенных контейнеров
docker-compose ps

echo ""
read -p "Press Enter to continue..."
