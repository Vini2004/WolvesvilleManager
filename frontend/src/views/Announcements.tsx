import { useState } from 'react'
import { api, ApiError } from '../api/client'
import { ErrorBox, Loading, SectionTitle } from '../components/ui'
import { useAsync } from '../lib/useAsync'
import { fmtDateTime } from '../lib/format'

const MAX_LEN = 500

export function Announcements({ clanRegId }: { clanRegId: number }) {
  const list = useAsync(() => api.getAnnouncements(clanRegId), [clanRegId])
  const [text, setText] = useState('')
  const [busy, setBusy] = useState(false)
  const [error, setError] = useState<string | null>(null)

  const publish = async () => {
    if (!text.trim()) return
    if (!window.confirm('Publicar este anúncio para todo o clã?')) return
    setBusy(true)
    setError(null)
    try {
      await api.postAnnouncement(clanRegId, text.trim())
      setText('')
      list.reload()
    } catch (e: unknown) {
      setError(e instanceof ApiError ? e.message : 'Erro inesperado.')
    } finally {
      setBusy(false)
    }
  }

  return (
    <div>
      <div className="card mb-7 px-7 py-[26px]">
        <div className="mb-2 font-serif text-xl font-semibold text-ink">Publicar anúncio</div>
        <div className="mb-4 font-sans text-[13px] leading-relaxed text-muted">
          O anúncio aparece para todos os membros dentro do jogo, publicado pelo bot do clã.
        </div>
        <textarea
          value={text}
          onChange={(e) => setText(e.target.value.slice(0, MAX_LEN))}
          placeholder="Escreva o anúncio…"
          rows={4}
          className="input-dark resize-y"
        />
        <div className="mt-2 flex items-center justify-between">
          <div className="font-mono text-[11.5px] text-faint">
            {text.length}/{MAX_LEN}
          </div>
          <button onClick={publish} disabled={busy || !text.trim()} className="btn-primary">
            {busy ? 'Publicando…' : 'Publicar anúncio'}
          </button>
        </div>
        {error && <div className="mt-3 font-sans text-[12.5px] text-danger">{error}</div>}
      </div>

      <SectionTitle>Anúncios publicados</SectionTitle>
      {list.loading ? (
        <Loading />
      ) : list.error ? (
        <ErrorBox message={list.error} onRetry={list.reload} />
      ) : (
        <div className="list-card">
          {list.data!.length === 0 && (
            <div className="px-5 py-6 font-sans text-[13px] text-muted">Nenhum anúncio ainda.</div>
          )}
          {list.data!.map((a, i) => (
            <div
              key={a.id ?? i}
              className="border-b border-[rgba(180,150,220,0.07)] px-5 py-4 last:border-b-0"
            >
              <div className="whitespace-pre-wrap font-sans text-[13.5px] leading-relaxed text-ink-2">
                {a.content}
              </div>
              <div className="mt-1.5 font-sans text-xs text-faint">
                {a.author ? `${a.author} · ` : ''}
                {fmtDateTime(a.timestamp)}
              </div>
            </div>
          ))}
        </div>
      )}
    </div>
  )
}
