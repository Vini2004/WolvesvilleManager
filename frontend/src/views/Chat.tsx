import { useState } from 'react'
import { api, ApiError } from '../api/client'
import { ErrorBox, Loading } from '../components/ui'
import { useAsync } from '../lib/useAsync'
import { fmtDateTime } from '../lib/format'

const MAX_LEN = 500

export function Chat({ clanRegId }: { clanRegId: number }) {
  const chat = useAsync(async () => {
    const [messages, members] = await Promise.all([
      api.getChat(clanRegId),
      api.listMembers(clanRegId),
    ])
    // O chat só traz o playerId — o nome vem da lista de membros.
    const names = new Map(members.map((m) => [m.playerId, m.username]))
    return messages
      .slice()
      .sort((a, b) => (b.date ?? '').localeCompare(a.date ?? ''))
      .map((m) => ({
        ...m,
        author: m.isSystem
          ? 'Sistema'
          : (m.playerId && names.get(m.playerId)) ?? m.playerBotOwnerUsername ?? 'Desconhecido',
      }))
  }, [clanRegId])

  const [text, setText] = useState('')
  const [busy, setBusy] = useState(false)
  const [error, setError] = useState<string | null>(null)

  const send = async () => {
    if (!text.trim()) return
    setBusy(true)
    setError(null)
    try {
      await api.sendChat(clanRegId, text.trim())
      setText('')
      chat.reload()
    } catch (e: unknown) {
      setError(e instanceof ApiError ? e.message : 'Erro inesperado.')
    } finally {
      setBusy(false)
    }
  }

  return (
    <div>
      <div className="card mb-7 px-7 py-[22px]">
        <div className="flex gap-2.5">
          <input
            value={text}
            onChange={(e) => setText(e.target.value.slice(0, MAX_LEN))}
            onKeyDown={(e) => e.key === 'Enter' && !busy && send()}
            placeholder="Enviar mensagem no chat do clã (aparece pelo bot)…"
            className="input-dark flex-1"
          />
          <button onClick={send} disabled={busy || !text.trim()} className="btn-primary flex-none">
            {busy ? '…' : 'Enviar'}
          </button>
        </div>
        {error && <div className="mt-3 font-sans text-[12.5px] text-danger">{error}</div>}
      </div>

      {chat.loading ? (
        <Loading />
      ) : chat.error ? (
        <ErrorBox message={chat.error} onRetry={chat.reload} />
      ) : (
        <div className="list-card">
          {chat.data!.length === 0 && (
            <div className="px-5 py-6 font-sans text-[13px] text-muted">Nenhuma mensagem no chat.</div>
          )}
          {chat.data!.map((m, i) => (
            <div key={i} className="border-b border-[rgba(180,150,220,0.07)] px-5 py-3.5 last:border-b-0">
              <div className="flex items-center justify-between gap-3">
                <span
                  className={`font-sans text-[12.5px] font-semibold ${m.isSystem ? 'text-faint' : 'text-lav'}`}
                >
                  {m.author}
                </span>
                <span className="flex-none font-mono text-[11px] text-faint">{fmtDateTime(m.date)}</span>
              </div>
              <div
                className={`mt-1 whitespace-pre-wrap font-sans text-[13.5px] leading-relaxed ${
                  m.isSystem ? 'italic text-muted' : 'text-ink-2'
                }`}
              >
                {m.msg}
              </div>
            </div>
          ))}
        </div>
      )}
    </div>
  )
}
