# Biblioteca API — Trabalho 1 de Cloud Computing (AWS)

CRUD de livros em **.NET 8 + EF Core + PostgreSQL**, hospedado na AWS:

```
Usuário ──HTTP:80──▶ EC2 (Ubuntu · Nginx ▶ API .NET :5000) ──5432──▶ RDS PostgreSQL (privado)
                                                                       └─ backups automáticos + AWS Backup
```

| Requisito do trabalho | Como foi atendido |
|---|---|
| CRUD | API REST `/api/livros` com Swagger |
| VPS (EC2) com a stack | Ubuntu 24.04 + .NET 8 + Nginx + systemd |
| Repositório de código | GitHub; a EC2 faz `git clone`/`git pull` |
| Copiar dados para o servidor e servir | `deploy/deploy.sh` (pull → publish → restart) |
| Banco no RDS | PostgreSQL no RDS, sem acesso público |
| Backup | Backup automático do RDS (7 dias) + snapshot manual + AWS Backup |

## Endpoints

| Método | Rota | Descrição |
|---|---|---|
| GET | `/api/livros?busca=` | Lista (busca opcional por título/autor) |
| GET | `/api/livros/{id}` | Detalhe |
| POST | `/api/livros` | Cria |
| PUT | `/api/livros/{id}` | Atualiza |
| DELETE | `/api/livros/{id}` | Remove |
| GET | `/health` | Status da API e da conexão com o banco |
| GET | `/swagger` | Documentação interativa |

## Rodando localmente

Pré-requisitos: .NET 8 SDK e um PostgreSQL local (ajuste `src/BibliotecaApi/appsettings.Development.json`).

```bash
cd src/BibliotecaApi
dotnet run
# abre http://localhost:5034/swagger
```

A tabela é criada automaticamente na inicialização (migrations do EF Core).

---

# Guia de deploy na AWS

> Use **a mesma região** para tudo (ex.: `us-east-1`). Os nomes abaixo são sugestões.

## Fase 0 — Conta e segurança de custos
1. Acesse o console da AWS.
2. **Billing and Cost Management → Budgets → Create budget** → template *Zero spend* ou *Monthly cost budget* de US$ 5, com o seu e-mail. Assim você é avisado antes de qualquer cobrança.

## Fase 2 — Security Groups (firewall)
**EC2 → Security Groups → Create security group** (VPC padrão):

1. `biblioteca-app-sg` (para a EC2), regras de **entrada**:
   - SSH · porta 22 · origem **My IP**
   - HTTP · porta 80 · origem **Anywhere-IPv4 (0.0.0.0/0)**
2. `biblioteca-db-sg` (para o RDS), regra de **entrada**:
   - PostgreSQL · porta 5432 · origem **o security group `biblioteca-app-sg`** (digite `sg-` e selecione o `biblioteca-app-sg` na lista)

> Assim, só a EC2 consegue falar com o banco. O banco não fica exposto na internet.

## Fase 3 — RDS PostgreSQL
**RDS → Create database**:
- *Standard create* → **PostgreSQL** (versão padrão)
- Template: **Free tier** (ou *Sandbox*/*Dev/Test* com instância micro)
- DB instance identifier: `biblioteca-db`
- Master username: `postgres` · senha **sem `;` nem aspas** (anote!)
- Instance class: `db.t3.micro` ou `db.t4g.micro` · Storage: 20 GB gp3, **desmarque autoscaling**
- Connectivity: **Don't connect to an EC2** · VPC padrão · **Public access: No** · Security group: **`biblioteca-db-sg`** (remova o `default`)
- *Additional configuration*:
  - Initial database name: **`biblioteca`**
  - **Backup: Enable automated backups · retenção 7 dias**
- Create database → espere ficar *Available* (~10 min) → copie o **Endpoint**.

## Fase 4 — EC2
**EC2 → Launch instance**:
- Nome: `biblioteca-app` · AMI: **Ubuntu Server 24.04 LTS** (x86) · Tipo: **t3.micro**
- Key pair: **Create new** → `biblioteca-key` (.pem) → salve em local seguro
- Network: **Select existing security group → `biblioteca-app-sg`**
- Storage: 8–20 GB gp3 → Launch.

(Recomendado) **EC2 → Elastic IPs → Allocate → Associate** com a instância, para o IP não mudar.

No seu computador:
```bash
chmod 400 ~/Downloads/biblioteca-key.pem
ssh -i ~/Downloads/biblioteca-key.pem ubuntu@IP_PUBLICO
```

Na EC2:
```bash
git clone https://github.com/SEU_USUARIO/SEU_REPO.git app
cd app
bash deploy/setup-ec2.sh                 # instala .NET, Nginx, swap, serviço
sudo nano /etc/biblioteca-api.env        # cole endpoint e senha do RDS
bash deploy/deploy.sh                    # compila, sobe a API e testa /health
```

Teste no navegador: `http://IP_PUBLICO/swagger` e `http://IP_PUBLICO/health`.

Comandos úteis na EC2:
```bash
sudo systemctl status biblioteca-api     # status do serviço
sudo journalctl -u biblioteca-api -f     # logs em tempo real
psql "host=ENDPOINT dbname=biblioteca user=postgres" -c "select * from livros;"   # consulta direta no RDS
```

## Fase 5 — Backup
1. **Backup automático do RDS** — já ativado na criação (RDS → banco → aba *Maintenance & backups*).
2. **Snapshot manual** — RDS → banco → *Actions → Take snapshot* → `biblioteca-snapshot-manual`.
3. **AWS Backup** (cobre RDS **e** EC2):
   - AWS Backup → *Backup plans → Create backup plan* → *Start with a template* → **Daily-35day-Retention** → nome `biblioteca-backup-plan`.
   - No plano criado → *Assign resources* → nome `biblioteca-recursos` → IAM role *Default* → *Include specific resource types*: **RDS** (`biblioteca-db`) e **EC2** (`biblioteca-app`).
   - Para demonstrar na hora: *Protected resources → Create on-demand backup*.

## Fase 7 — Depois da apresentação
Apague para não gerar custo: EC2 (terminate), Elastic IP (release), RDS (delete), snapshots e o backup plan / vault.
