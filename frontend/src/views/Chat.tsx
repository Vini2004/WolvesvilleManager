import { useEffect, useState } from 'react'
import { api, ApiError } from '../api/client'
import { ErrorBox, Loading, Toggle } from '../components/ui'
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

  // ---------- Boas-vindas automáticas ----------
  const welcome = useAsync(() => api.getWelcomeSettings(clanRegId), [clanRegId])
  const [welcomeEnabled, setWelcomeEnabled] = useState(false)
  const [welcomeTemplate, setWelcomeTemplate] = useState('')
  const [welcomeBusy, setWelcomeBusy] = useState(false)
  const [welcomeSaved, setWelcomeSaved] = useState(false)
  const [welcomeError, setWelcomeError] = useState<string | null>(null)

  useEffect(() => {
    if (welcome.data) {
      setWelcomeEnabled(welcome.data.enabled)
      setWelcomeTemplate(welcome.data.template)
    }
  }, [welcome.data])

  const saveWelcome = async () => {
    setWelcomeBusy(true)
    setWelcomeError(null)
    try {
      await api.setWelcomeSettings(clanRegId, welcomeEnabled, welcomeTemplate)
      setWelcomeSaved(true)
      setTimeout(() => setWelcomeSaved(false), 2000)
      welcome.reload()
    } catch (e: unknown) {
      setWelcomeError(e instanceof ApiError ? e.message : 'Erro inesperado.')
    } finally {
      setWelcomeBusy(false)
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

      <div className="card mb-7 px-7 py-[22px]">
        <div className="mb-2 font-serif text-lg font-semibold text-ink">Boas-vindas automáticas</div>
        <div className="mb-4 font-sans text-[13px] leading-relaxed text-muted">
          Quando ligado, manda esta mensagem aqui no chat sozinho sempre que alguém novo entra no
          clã. Use <span className="font-mono text-lav">{'{mention}'}</span> no lugar em que a
          pessoa deve ser marcada — vira "@" + o nick de quem entrou.
        </div>
        {welcome.loading ? (
          <Loading />
        ) : welcome.error ? (
          <ErrorBox message={welcome.error} onRetry={welcome.reload} />
        ) : (
          <>
            <div className="mb-3 flex items-center gap-3">
              <Toggle on={welcomeEnabled} onClick={() => setWelcomeEnabled(!welcomeEnabled)} />
              <span className="font-sans text-[13px] text-muted">
                {welcomeEnabled ? 'Ativado' : 'Desativado'}
              </span>
            </div>
            <textarea
              value={welcomeTemplate}
              onChange={(e) => setWelcomeTemplate(e.target.value.slice(0, 500))}
              rows={4}
              className="input-dark w-full resize-y"
            />
            <div className="mt-3 flex items-center gap-2.5">
              <button onClick={saveWelcome} disabled={welcomeBusy} className="btn-secondary flex-none">
                {welcomeBusy ? '…' : welcomeSaved ? 'Salvo!' : 'Salvar'}
              </button>
            </div>
            {welcomeError && <div className="mt-3 font-sans text-[12.5px] text-danger">{welcomeError}</div>}
          </>
        )}
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
