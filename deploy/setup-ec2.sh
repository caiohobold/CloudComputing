#!/usr/bin/env bash
# Executar UMA VEZ na EC2 (Ubuntu 24.04), logo após clonar o repositório:
#   bash deploy/setup-ec2.sh
set -euo pipefail

REPO_DIR="$(cd "$(dirname "$0")/.." && pwd)"

echo ">> Criando 2 GB de swap (a t3.micro só tem 1 GB de RAM e o build do .NET precisa de mais)"
if ! swapon --show | grep -q /swapfile; then
  sudo fallocate -l 2G /swapfile
  sudo chmod 600 /swapfile
  sudo mkswap /swapfile
  sudo swapon /swapfile
  echo '/swapfile none swap sw 0 0' | sudo tee -a /etc/fstab
fi

echo ">> Instalando .NET 8 SDK, Nginx e cliente PostgreSQL"
sudo apt-get update
sudo apt-get install -y dotnet-sdk-8.0 nginx postgresql-client

echo ">> Criando pasta da aplicação"
sudo mkdir -p /var/www/biblioteca
sudo chown ubuntu:ubuntu /var/www/biblioteca

echo ">> Criando arquivo de variáveis de ambiente (se ainda não existir)"
if [ ! -f /etc/biblioteca-api.env ]; then
  sudo cp "$REPO_DIR/deploy/biblioteca-api.env.example" /etc/biblioteca-api.env
  sudo chmod 600 /etc/biblioteca-api.env
fi

echo ">> Registrando o serviço systemd"
sudo cp "$REPO_DIR/deploy/biblioteca-api.service" /etc/systemd/system/biblioteca-api.service
sudo systemctl daemon-reload
sudo systemctl enable biblioteca-api

echo ">> Configurando o Nginx como reverse proxy"
sudo cp "$REPO_DIR/deploy/nginx-biblioteca.conf" /etc/nginx/sites-available/biblioteca
sudo ln -sf /etc/nginx/sites-available/biblioteca /etc/nginx/sites-enabled/biblioteca
sudo rm -f /etc/nginx/sites-enabled/default
sudo nginx -t
sudo systemctl reload nginx

echo
echo "Setup concluído!"
echo "PRÓXIMO PASSO: edite a connection string do RDS com:"
echo "  sudo nano /etc/biblioteca-api.env"
echo "e depois rode:  bash deploy/deploy.sh"
