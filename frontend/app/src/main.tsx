import React, { useEffect, useMemo, useState } from 'react';
import { createRoot } from 'react-dom/client';
import { Clapperboard, Download, ExternalLink, Film, Loader2, Play, RefreshCw, Trash2, Wand2 } from 'lucide-react';
import './styles.css';

const API_BASE = import.meta.env.VITE_API_BASE_URL ?? 'http://localhost:8080';
const TENANT_ID = '11111111-1111-1111-1111-111111111111';
const USER_ID = '22222222-2222-2222-2222-222222222222';

type VideoStatus = 'Queued' | 'Processing' | 'GeneratingScript' | 'GeneratingImages' | 'GeneratingAudio' | 'RenderingVideo' | 'Completed' | 'Failed' | number;

type Scene = {
  index: number;
  text: string;
  imagePrompt: string;
  imagePath?: string;
  audioPath?: string;
  estimatedSeconds: number;
};

type VideoJob = {
  id: string;
  theme: string;
  style: string;
  duration: string;
  tone: string;
  voice: string;
  sceneCount: number;
  imageType: string;
  imageProvider?: string;
  imageModel?: string;
  format: string;
  status: VideoStatus;
  scenes: Scene[];
  scriptJson?: string;
  videoPath?: string;
  reelPath?: string;
  audioPath?: string;
  error?: string;
  createdAt: string;
};

type FormState = {
  theme: string;
  style: string;
  duration: string;
  tone: string;
  voice: string;
  sceneCount: number;
  imageType: string;
  imageProvider: string;
  imageModel: string;
  format: string;
};

const defaults: FormState = {
  theme: 'Como usar IA local para criar vídeos virais',
  style: 'educativo',
  duration: 'curto',
  tone: 'viral',
  voice: 'pt_BR-cadu-medium',
  sceneCount: 4,
  imageType: 'cinematic',
  imageProvider: 'comfyui',
  imageModel: 'local',
  format: 'reels_9_16'
};

const formatLabels: Record<string, string> = {
  reels_9_16: 'Reels 9:16',
  youtube_shorts_9_16: 'YouTube Shorts 9:16',
  youtube_16_9: 'YouTube 16:9'
};

const imageModels = [
  { provider: 'comfyui', model: 'local', label: 'ComfyUI local', detail: 'modelo local atual' },
  { provider: 'fal', model: 'fal-flux-schnell', label: 'fal.ai FLUX schnell', detail: 'rápido e leve' },
  { provider: 'fal', model: 'fal-flux-dev', label: 'fal.ai FLUX dev', detail: 'qualidade maior' },
  { provider: 'replicate', model: 'replicate-flux-schnell', label: 'Replicate FLUX schnell', detail: 'API simples' },
  { provider: 'replicate', model: 'replicate-flux-dev', label: 'Replicate FLUX dev', detail: 'boa qualidade' },
  { provider: 'together', model: 'together-flux-schnell', label: 'Together FLUX schnell', detail: 'API rápida' },
  { provider: 'huggingface', model: 'hf-flux-dev', label: 'Hugging Face FLUX dev', detail: 'Inference Providers' }
];

function statusLabel(status: VideoStatus) {
  const map: Record<number, string> = {
    0: 'processando',
    1: 'processando',
    2: 'gerando roteiro',
    3: 'gerando imagens',
    4: 'gerando áudio',
    5: 'renderizando',
    6: 'finalizado',
    7: 'falhou'
  };
  return typeof status === 'number' ? map[status] ?? 'processando' : status;
}

function isCompleted(status: VideoStatus) {
  return status === 'Completed' || status === 6;
}

function artifactUrl(video: VideoJob, kind: 'final' | 'base' | 'audio') {
  return `${API_BASE}/videos/${video.id}/artifacts/${kind}`;
}

async function api<T>(path: string, init?: RequestInit): Promise<T> {
  const response = await fetch(`${API_BASE}${path}`, {
    ...init,
    headers: {
      'Content-Type': 'application/json',
      'X-Tenant-Id': TENANT_ID,
      'X-User-Id': USER_ID,
      ...(init?.headers ?? {})
    }
  });

  if (!response.ok) {
    throw new Error(await response.text());
  }

  return response.json();
}

function App() {
  const [form, setForm] = useState<FormState>(defaults);
  const [videos, setVideos] = useState<VideoJob[]>([]);
  const [selectedId, setSelectedId] = useState<string | null>(null);
  const [busy, setBusy] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [apiOnline, setApiOnline] = useState<boolean | null>(null);

  const selected = useMemo(() => videos.find((video) => video.id === selectedId) ?? videos[0], [videos, selectedId]);

  async function loadVideos() {
    try {
      const data = await api<VideoJob[]>('/videos');
      setVideos(data);
      setApiOnline(true);
      setError(null);
      if (!selectedId && data.length > 0) {
        setSelectedId(data[0].id);
      }
    } catch (err) {
      setApiOnline(false);
      setError(err instanceof TypeError ? `API indisponivel em ${API_BASE}` : err instanceof Error ? err.message : 'Falha ao carregar videos');
    }
  }

  useEffect(() => {
    loadVideos();
    const timer = window.setInterval(() => {
      loadVideos();
    }, 3000);
    return () => window.clearInterval(timer);
  }, []);

  async function submit(event: React.FormEvent) {
    event.preventDefault();
    setBusy(true);
    setError(null);
    try {
      const job = await api<VideoJob>('/videos/generate', {
        method: 'POST',
        body: JSON.stringify({ request: form })
      });
      setSelectedId(job.id);
      await loadVideos();
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Falha ao gerar vídeo');
    } finally {
      setBusy(false);
    }
  }
  async function deleteVideo(id: string, event?: React.MouseEvent) {
    if (event) event.stopPropagation();
    if (!confirm('Remover este vídeo e apagar os arquivos gerados?')) return;
    
    try {
      await api(`/videos/${id}`, { method: 'DELETE' });
      const updated = videos.filter(v => v.id !== id);
      setVideos(updated);
      if (selectedId === id) {
        setSelectedId(updated.length > 0 ? updated[0].id : null);
      }
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Falha ao deletar vídeo');
    }
  }

  return (
    <main className="shell">
      <section className="topbar">
        <div>
          <span className="eyebrow"><Clapperboard size={16} /> SaaS multi-tenant</span>
          <h1>Video SaaS Studio</h1>
        </div>
        <div className="topActions">
          <span className={apiOnline === false ? 'apiBadge offline' : 'apiBadge'}>
            API {apiOnline === false ? 'offline' : 'online'}
          </span>
          <button className="iconButton" title="Atualizar" onClick={() => loadVideos()}>
            <RefreshCw size={18} />
          </button>
        </div>
      </section>

      <section className="workspace">
        <form className="panel formPanel" onSubmit={submit}>
          <div className="panelTitle">
            <Wand2 size={18} />
            <h2>Gerar vídeo</h2>
          </div>

          <label>
            Tema
            <textarea value={form.theme} onChange={(event) => setForm({ ...form, theme: event.target.value })} rows={3} />
          </label>

          <div className="grid2">
            <label>
              Estilo
              <select value={form.style} onChange={(event) => setForm({ ...form, style: event.target.value })}>
                <option value="educativo">educativo</option>
                <option value="dark">dark</option>
                <option value="motivacional">motivacional</option>
              </select>
            </label>
            <label>
              Tom
              <select value={form.tone} onChange={(event) => setForm({ ...form, tone: event.target.value })}>
                <option value="viral">viral</option>
                <option value="engraçado">engraçado</option>
                <option value="sério">sério</option>
              </select>
            </label>
          </div>

          <div className="grid2">
            <label>
              Duração
              <select value={form.duration} onChange={(event) => setForm({ ...form, duration: event.target.value })}>
                <option value="curto">curto</option>
                <option value="médio">médio</option>
              </select>
            </label>
            <label>
              Imagem
              <select value={form.imageType} onChange={(event) => setForm({ ...form, imageType: event.target.value })}>
                <option value="realista">realista</option>
                <option value="cartoon">cartoon</option>
                <option value="cinematic">cinematic</option>
              </select>
            </label>
          </div>

          <div className="grid2">
            <label>
              Voz
              <select value={form.voice} onChange={(event) => setForm({ ...form, voice: event.target.value })}>
                <option value="pt_BR-cadu-medium">Cadu (Médio)</option>
                <option value="pt_BR-edresson-low">Edresson (Baixo)</option>
                <option value="pt_BR-faber-medium">Faber (Médio)</option>
                <option value="pt_BR-jeff-medium">Jeff (Médio)</option>
              </select>
            </label>
            <label>
              Cenas
              <input type="number" min={1} max={8} value={form.sceneCount} onChange={(event) => setForm({ ...form, sceneCount: Number(event.target.value) })} />
            </label>
          </div>

          <label>
            Gerador de imagem
            <select
              value={form.imageModel}
              onChange={(event) => {
                const selected = imageModels.find((item) => item.model === event.target.value) ?? imageModels[0];
                setForm({ ...form, imageProvider: selected.provider, imageModel: selected.model });
              }}
            >
              {imageModels.map((item) => (
                <option key={item.model} value={item.model}>
                  {item.label} - {item.detail}
                </option>
              ))}
            </select>
          </label>

          <label>
            Formato
            <select value={form.format} onChange={(event) => setForm({ ...form, format: event.target.value })}>
              <option value="reels_9_16">Reels 9:16</option>
              <option value="youtube_shorts_9_16">YouTube Shorts 9:16</option>
              <option value="youtube_16_9">YouTube 16:9</option>
            </select>
          </label>

          <button className="primary" disabled={busy || !form.theme.trim()}>
            {busy ? <Loader2 className="spin" size={18} /> : <Play size={18} />}
            GERAR VIDEO
          </button>
          {error && <p className="error">{error}</p>}
        </form>

        <section className="panel previewPanel">
          <div className="panelTitle">
            <Film size={18} />
            <h2>Preview do script</h2>
          </div>
          {selected ? (
            <>
              <div className="statusRow">
                <strong>{selected.theme}</strong>
                <span className={`status ${statusLabel(selected.status)}`}>{formatLabels[selected.format] ?? selected.format} · {statusLabel(selected.status)}</span>
              </div>
              <div className="metaPills">
                <span>{selected.imageProvider ?? 'comfyui'}</span>
                <span>{selected.imageModel ?? 'local'}</span>
              </div>
              <div className="scriptList">
                {(selected.scenes ?? []).length === 0 ? (
                  <p className="muted">O roteiro aparece aqui assim que o Ollama finalizar a primeira etapa.</p>
                ) : (
                  selected.scenes.map((scene) => (
                    <article key={scene.index} className="scene">
                      <b>Cena {scene.index}</b>
                      <p>{scene.text}</p>
                      <small>{scene.imagePrompt}</small>
                    </article>
                  ))
                )}
              </div>
              {selected.error && <p className="error">{selected.error}</p>}
              <div className="artifactGrid">
                <div className="artifactHeader">
                  <strong>Arquivos gerados</strong>
                  <small>{isCompleted(selected.status) ? 'Prontos para abrir ou baixar' : 'Aparecem quando o render finalizar'}</small>
                </div>
                {isCompleted(selected.status) ? (
                  <>
                    <div className="artifactActions">
                      <a className="artifactButton primaryArtifact" href={artifactUrl(selected, 'final')} target="_blank" rel="noreferrer">
                        <ExternalLink size={16} />
                        Abrir final
                      </a>
                      <a className="artifactButton" href={artifactUrl(selected, 'final')} download>
                        <Download size={16} />
                        Baixar MP4
                      </a>
                    </div>
                    <div className="artifactLinks">
                      <a href={artifactUrl(selected, 'base')} target="_blank" rel="noreferrer">MP4 base</a>
                      <a href={artifactUrl(selected, 'audio')} target="_blank" rel="noreferrer">Narração WAV</a>
                    </div>
                  </>
                ) : (
                  <p className="muted">O vídeo ainda está em processamento.</p>
                )}
                <details>
                  <summary>Caminhos técnicos</summary>
                  <span>Final: {selected.reelPath ?? '-'}</span>
                  <span>Base: {selected.videoPath ?? '-'}</span>
                  <span>WAV: {selected.audioPath ?? '-'}</span>
                </details>
              </div>
            </>
          ) : (
            <p className="muted">Nenhum vídeo gerado ainda.</p>
          )}
        </section>

        <section className="panel listPanel">
          <div className="panelTitle">
            <Clapperboard size={18} />
            <h2>Vídeos gerados</h2>
          </div>
          <div className="videoList">
            {videos.map((video) => (
              <div key={video.id} className={video.id === selected?.id ? 'videoItem active' : 'videoItem'} onClick={() => setSelectedId(video.id)} style={{ display: 'flex', justifyContent: 'space-between', cursor: 'pointer' }}>
                <div style={{ flex: 1 }}>
                  <span style={{ display: 'block' }}>{video.theme}</span>
                  <small>{statusLabel(video.status)} · {new Date(video.createdAt).toLocaleString('pt-BR')}</small>
                </div>
                <button type="button" className="iconButton" title="Deletar" onClick={(e) => deleteVideo(video.id, e)} style={{ alignSelf: 'center', opacity: 0.7, padding: '4px' }}>
                  <Trash2 size={16} />
                </button>
              </div>
            ))}
          </div>
        </section>
      </section>
    </main>
  );
}

createRoot(document.getElementById('root')!).render(<App />);
