#!/usr/bin/env bash
# Executar na EC2 sempre que quiser publicar a versão mais recente do GitHub:
#   bash deploy/deploy.sh
set -euo pipefail

REPO_DIR="$(cd "$(dirname "$0")/.." && pwd)"
cd "$REPO_DIR"

echo ">> Baixando a versão mais recente do repositório"
git pull

echo ">> Compilando (Release)"
dotnet publish src/BibliotecaApi/BibliotecaApi.csproj -c Release -o /var/www/biblioteca

echo ">> Reiniciando o serviço"
sudo systemctl restart biblioteca-api
sleep 5
sudo systemctl --no-pager status biblioteca-api | head -n 5

echo ">> Health check"
curl -s http://localhost/health; echo
