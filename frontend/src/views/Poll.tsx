import { useState } from 'react'
import { api, ApiError } from '../api/client'
import { ErrorBox, Loading, SectionTitle } from '../components/ui'
import { useAsync } from '../lib/useAsync'

/** Aba admin: link compartilhável do formulário público + apuração em tempo real. */
export function Poll({ clanRegId }: { clanRegId: number }) {
  const poll = useAsync(() => api.getPollAdmin(clanRegId), [clanRegId])
  const [copied, setCopied] = useState(false)
  const [busy, setBusy] = useState(false)
  const [error, setError] = useState<string | null>(null)

  if (poll.loading) return <Loading />
  if (poll.error || !poll.data) return <ErrorBox message={poll.error ?? 'Erro.'} onRetry={poll.reload} />

  const link = `${window.location.origin}/votar/${poll.data.token}`
  const totalShown = poll.data.quests.reduce((sum, q) => sum + q.votes, 0)
  const max = Math.max(1, ...poll.data.quests.map((q) => q.votes))

  const copy = async () => {
    await navigator.clipboard.writeText(link)
    setCopied(true)
    setTimeout(() => setCopied(false), 2000)
  }

  const reset = async () => {
    if (!window.confirm('Zerar todos os votos do formulário? Isso não pode ser desfeito.')) return
    setBusy(true)
    setError(null)
    try {
      await api.resetPoll(clanRegId)
      poll.reload()
    } catch (e: unknown) {
      setError(e instanceof ApiError ? e.message : 'Erro inesperado.')
    } finally {
      setBusy(false)
    }
  }

  return (
    <div>
      <div className="card mb-7 px-7 py-[26px]">
        <div className="mb-2 font-serif text-xl font-semibold text-ink">Link da votação</div>
        <div className="mb-4 font-sans text-[13px] leading-relaxed text-muted">
          Compartilhe este link com o clã (Discord, WhatsApp, anúncio no jogo…). Quem abrir vê só o
          formulário com as missões disponíveis e vota uma vez por navegador — sem login. Crie uma
          automação do tipo <span className="font-bold text-lav">Iniciar mais votada do formulário</span>{' '}
          para iniciar a vencedora no horário combinado; a urna é zerada quando a missão inicia.
        </div>
        <div className="flex flex-wrap items-center gap-2.5">
          <input readOnly value={link} onFocus={(e) => e.target.select()} className="input-dark flex-1 basis-64" />
          <button onClick={copy} className="btn-primary flex-none">
            {copied ? 'Copiado!' : 'Copiar link'}
          </button>
        </div>
      </div>

      <div className="mb-3.5 flex items-center justify-between">
        <SectionTitle>
          Apuração · {totalShown} voto{totalShown === 1 ? '' : 's'}
        </SectionTitle>
        <button onClick={reset} disabled={busy || poll.data.totalVotes === 0} className="btn-danger-ghost">
          {busy ? 'Zerando…' : 'Zerar votos'}
        </button>
      </div>
      {error && (
        <div className="mb-4">
          <ErrorBox message={error} />
        </div>
      )}
      {poll.data.quests.length === 0 ? (
        <div className="list-card px-5 py-4 font-sans text-[13px] text-muted">
          Não há missões disponíveis no momento — o formulário mostra a mesma lista da aba Missões.
        </div>
      ) : (
        <div className="flex flex-col gap-3">
          {poll.data.quests.map((q) => (
            <div key={q.questId} className="list-card px-5 py-4">
              <div className="flex items-center justify-between gap-4">
                <div className="min-w-0 font-sans text-[14px] font-semibold text-ink-2">
                  {q.name}
                  <span className="ml-2 font-sans text-[11.5px] font-normal text-faint">
                    {q.gems ? 'gemas' : 'ouro'}
                  </span>
                </div>
                <div className="flex-none font-mono text-[13px] text-gold">
                  {q.votes} voto{q.votes === 1 ? '' : 's'}
                </div>
              </div>
              <div className="mt-2.5 h-2 overflow-hidden rounded bg-white/5">
                <div
                  className="h-full rounded bg-gradient-to-r from-violet to-gold transition-[width]"
                  style={{ width: `${(q.votes / max) * 100}%` }}
                />
              </div>
            </div>
          ))}
        </div>
      )}
    </div>
  )
}
