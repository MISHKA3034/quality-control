#!/usr/bin/env bash
# Однократная подготовка окружения Codespaces.
set -euo pipefail

# Кириллический шрифт для PDF-отчётов (Segoe UI в Linux нет).
sudo apt-get update -y && sudo apt-get install -y --no-install-recommends fonts-dejavu-core

# Случайный ключ подписи JWT — в репозитории ключа нет намеренно.
dotnet user-secrets set "Jwt:Key" "$(openssl rand -base64 48)" --project src/QualityControl.API

dotnet restore QualityControl.slnx
(cd client && npm ci)
