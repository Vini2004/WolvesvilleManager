import { useState } from 'react'
import { api, ApiError } from '../api/client'
import { ErrorBox, Loading, Particles } from '../components/ui'
import { useAsync } from '../lib/useAsync'

const VOTER_STORAGE = 'wvm.voterId'

/** Identificador anônimo deste navegador — a "cédula" da urna (1 voto por navegador). */
function getVoterId(): string {
  let id = localStorage.getItem(VOTER_STORAGE)
  if (!id) {
    id = crypto.randomUUID()
    localStorage.setItem(VOTER_STORAGE, id)
  }
  return id
}

/**
 * Página pública do link /votar/{token}: só o formulário de votação, sem login e sem
 * nenhuma outra parte do app. Votar de novo troca o voto deste navegador.
 */
export function PublicPoll({ token }: { token: string }) {
  const voterId = getVoterId()
  const poll = useAsync(() => api.getPublicPoll(token, voterId), [token])
  const [busy, setBusy] = useState<string | null>(null)
  const [shuffling, setShuffling] = useState(false)
  const [error, setError] = useState<string | null>(null)

  const vote = async (questId: string) => {
    setBusy(questId)
    setError(null)
    try {
      await api.votePoll(token, questId, voterId)
      poll.reload()
    } catch (e: unknown) {
      setError(e instanceof ApiError ? e.message : 'Erro inesperado.')
    } finally {
      setBusy(null)
    }
  }

  const shuffle = async () => {
    if (
      !window.confirm(
        'Embaralhar traz missões novas para todo o clã (custa ouro do clã) e zera os votos atuais. Continuar?',
      )
    )
      return
    setShuffling(true)
    setError(null)
    try {
      await api.shufflePoll(token)
      poll.reload()
    } catch (e: unknown) {
      setError(e instanceof ApiError ? e.message : 'Erro inesperado.')
    } finally {
      setShuffling(false)
    }
  }

  return (
    <div className="relative flex min-h-screen justify-center overflow-hidden bg-night px-4 py-10">
      <Particles />
      <div className="relative z-1 w-full max-w-2xl">
        {poll.loading && <Loading label="Carregando votação…" />}
        {!poll.loading && (poll.error || !poll.data) && (
          <ErrorBox message={poll.error ?? 'Votação não encontrada.'} onRetry={poll.reload} />
        )}
        {poll.data && (
          <>
            <div className="mb-8 text-center">
              <div className="font-serif text-2xl font-semibold text-ink">{poll.data.clanName}</div>
              <div className="mt-1 font-sans text-[13px] text-muted">
                Vote na próxima missão do clã · seu voto pode ser trocado até a apuração
              </div>
              <button
                onClick={shuffle}
                disabled={shuffling || busy !== null}
                className="btn-ghost mt-4"
              >
                {shuffling ? 'Embaralhando…' : '🔀 Embaralhar missões (custa ouro)'}
              </button>
            </div>

            {error && (
              <div className="mb-4">
                <ErrorBox message={error} />
              </div>
            )}

            {poll.data.quests.length === 0 ? (
              <div className="card p-7 text-center font-sans text-[13.5px] text-muted">
                Não há missões disponíveis para votar agora. Volte mais tarde!
              </div>
            ) : (
              <div className="flex flex-col gap-4">
                {poll.data.quests.map((q) => {
                  const isVoted = poll.data!.votedQuestId === q.questId
                  return (
                    <div
                      key={q.questId}
                      className="card overflow-hidden"
                      style={isVoted ? { boxShadow: 'inset 0 0 0 2px var(--color-violet)' } : undefined}
                    >
                      {q.imageUrl && (
                        <img
                          src={q.imageUrl}
                          alt=""
                          className="block h-36 w-full object-cover"
                          loading="lazy"
                        />
                      )}
                      <div className="flex flex-wrap items-center justify-between gap-3 p-5">
                        <div className="min-w-0">
                          <div className="font-serif text-lg font-semibold text-ink">{q.name}</div>
                          <div className="mt-0.5 font-sans text-[12px] text-faint">
                            paga com {q.gems ? 'gemas' : 'ouro'} ·{' '}
                            <span className="font-mono text-gold">
                              {q.votes} voto{q.votes === 1 ? '' : 's'}
                            </span>
                          </div>
                        </div>
                        <button
                          onClick={() => vote(q.questId)}
                          disabled={busy !== null || shuffling || isVoted}
                          className={isVoted ? 'btn-secondary flex-none' : 'btn-primary flex-none'}
                        >
                          {isVoted ? '✓ Seu voto' : busy === q.questId ? 'Votando…' : 'Votar nesta'}
                        </button>
                      </div>
                    </div>
                  )
                })}
              </div>
            )}

            <div className="mt-8 text-center font-sans text-[11.5px] text-dim">
              Wolvesville Manager · um voto por navegador
            </div>
          </>
        )}
      </div>
    </div>
  )
}
