# Video SaaS Studio

Sistema local para gerar vídeos automáticos em formato Reels/Shorts usando .NET 8, React, Ollama, ComfyUI, Piper e FFmpeg em Docker.

## O que este projeto entrega

- Backend .NET 8 Web API em Clean Architecture.
- MediatR para comandos e queries.
- Pipeline assíncrono com estados persistidos.
- Multi-tenant com `TenantId` e `UserId` em headers.
- Entidades/tabelas: `Tenants`, `Users`, `VideoJobs`, `Billing`.
- Controle inicial de monetização por plano gratuito e limite mensal.
- React UI com formulário completo, status, preview de script e lista de vídeos.
- Integração com containers já existentes:
  - Ollama em `http://localhost:11434`
  - ComfyUI em `http://localhost:8188`
- Piper com modelo local `pt_BR-cadu-medium.onnx`.
- FFmpeg via Docker para gerar `video.mp4`, `reel.mp4` e `narration.wav`.

## Estrutura

```text
/src
  /Api
  /Application
  /Domain
  /Infrastructure
  /Workers
/frontend
  /app
/docker
  /media-pipeline
/scripts
  generate_video.ps1
/tests
```

## Pre-requisitos

- Docker Desktop rodando.
- Containers existentes já ativos:
  - Ollama: porta `11434`
  - ComfyUI: porta `8188`
- Modelo Piper em:

```text
models/piper/pt_BR-cadu-medium.onnx
```

Se o Piper exigir arquivo `.json` do modelo, coloque-o na mesma pasta.

## Configuração Docker

Copie `.env.example` para `.env` e ajuste os caminhos absolutos:

```text
VIDEO_MEDIA_HOST_PATH=C:/Users/CAIO/OneDrive/Documentos/New project 2/media
PIPER_MODELS_HOST_PATH=C:/Users/CAIO/OneDrive/Documentos/New project 2/models/piper
```

Esses caminhos são importantes porque a API chama `docker run` para Piper/FFmpeg; o Docker precisa montar o diretório real do host.

## Subir local com Docker Compose

```powershell
docker compose up --build
```

URLs:

- UI: `http://localhost:5173`
- API/Swagger: `http://localhost:8080/swagger`
- Health: `http://localhost:8080/health`

O compose não cria Ollama nem ComfyUI.

## Rodar sem Compose

Backend:

```powershell
dotnet run --project src/Api/Api.csproj
```

Frontend:

```powershell
cd frontend/app
npm install
npm run dev
```

## API

Headers multi-tenant demo:

```text
X-Tenant-Id: 11111111-1111-1111-1111-111111111111
X-User-Id: 22222222-2222-2222-2222-222222222222
```

Criar vídeo:

```http
POST /videos/generate
Content-Type: application/json

{
  "request": {
    "theme": "Como usar IA local para criar vídeos virais",
    "style": "educativo",
    "duration": "curto",
    "tone": "viral",
    "voice": "pt_BR-cadu-medium",
    "sceneCount": 4,
    "imageType": "cinematic",
    "format": "reels_9_16"
  }
}
```

Listar:

```http
GET /videos
GET /videos/{id}
GET /tenants
```

Script PowerShell:

```powershell
.\scripts\generate_video.ps1 -Theme "5 automacoes com IA local"
```

## Pipeline

1. API recebe request e cria `VideoJob`.
2. Worker processa o job em background.
3. Ollama gera JSON estruturado:

```json
{
  "cenas": [
    {
      "texto": "narração",
      "prompt_imagem": "prompt visual"
    }
  ]
}
```

4. ComfyUI gera uma imagem por cena.
5. Piper gera um `.wav` por cena.
6. FFmpeg concatena cenas, áudio e gera:
   - `video.mp4`
   - `reel.mp4`
   - `narration.wav`

## Monetização

O tenant demo usa plano `free`, com limite mensal em `TenantSettings.MonthlyVideoLimit`. A tabela `Billing` registra:

- vídeos gerados no mês
- duração total gerada no mês
- plano atual

A estrutura está pronta para Stripe/Mercado Pago ou outro provedor.

## Escala

O worker usa uma fila em memória por padrão. Para produção, substitua `IJobQueue` por Kafka usando o tópico:

```text
video.generate
```

Como o pipeline depende de armazenamento compartilhado de mídia, em escala horizontal use volume compartilhado, S3 compatível ou storage dedicado por tenant.

## Testes

```powershell
dotnet test
```

Os testes de pipeline completo dependem de Ollama, ComfyUI, Docker, modelo Piper e imagens FFmpeg disponíveis.
