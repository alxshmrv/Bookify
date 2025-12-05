@echo off
:: Устанавливаем заголовок окна консоли
title Bookify Launcher

echo ==============================================
echo      STARTING BOOKIFY ENVIRONMENT
echo ==============================================

:: 1. Сначала останавливаем всё старое, чтобы не было конфликтов
echo.
echo [1/2] Cleaning up old containers...
docker-compose down --remove-orphans

:: 2. Собираем и запускаем
:: --build: пересобирает образ (важно для C# кода)
:: --force-recreate: пересоздает контейнеры с нуля
:: -d: Detached mode (в фоне, чтобы консоль не висела)
echo.
echo [2/2] Building and Starting services...
docker-compose up --build --force-recreate -d

echo.
echo ==============================================
echo      DONE! STATUS CHECK:
echo ==============================================
echo.

:: Показываем статус запущенных контейнеров
docker-compose ps

echo.
echo App is available at: http://localhost:5001
echo Database is at:      localhost:5432
echo.
pause