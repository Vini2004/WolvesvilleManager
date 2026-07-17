import { useState } from 'react'
import { api, ApiError } from '../api/client'
import { SHUFFLE_OPTION_ID } from '../api/types'
import { ErrorBox, Loading, Particles } from '../components/ui'
import { useAsync } from '../lib/useAsync'
import { fmtDateTime, timeLeft } from '../lib/format'

const NICKNAME_STORAGE = 'wvm.pollNickname'

/**
 * Página pública do link /votar/{token}: só o formulário de votação, sem login e sem
 * nenhuma outra parte do app. O voto é identificado pelo nick digitado — não por
 * navegador, então trocar de navegador ou usar aba anônima não abre voto extra; votar
 * de novo com o mesmo nick troca a escolha.
 */
export function PublicPoll({ token }: { token: string }) {
  const [nickname, setNickname] = useState(() => localStorage.getItem(NICKNAME_STORAGE) ?? '')
  const poll = useAsync(() => api.getPublicPoll(token, nickname), [token])
  const [busy, setBusy] = useState<string | null>(null)
  const [error, setError] = useState<string | null>(null)

  const trimmedNickname = nickname.trim()

  const checkNickname = () => {
    localStorage.setItem(NICKNAME_STORAGE, nickname)
    poll.reload()
  }

  const vote = async (questId: string) => {
    if (!trimmedNickname) {
      setError('Digite seu nick do Wolvesville para votar.')
      return
    }
    setBusy(questId)
    setError(null)
    try {
      localStorage.setItem(NICKNAME_STORAGE, nickname)
      await api.votePoll(token, questId, trimmedNickname)
      poll.reload()
    } catch (e: unknown) {
      setError(e instanceof ApiError ? e.message : 'Erro inesperado.')
    } finally {
      setBusy(null)
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
              {poll.data.isClosed ? (
                <div className="mt-1 font-sans text-[13px] font-bold text-danger">
                  Votação encerrada
                  {timeLeft(poll.data.nextBoundaryUtc) ? (
                    <> · reabre em {fmtDateTime(poll.data.nextBoundaryUtc)} ({timeLeft(poll.data.nextBoundaryUtc)})</>
                  ) : (
                    ' — aguarde o clã abrir um novo prazo.'
                  )}
                </div>
              ) : (
                <div className="mt-1 font-sans text-[13px] text-muted">
                  Vote na próxima missão do clã · seu voto pode ser trocado até a apuração
                  {timeLeft(poll.data.nextBoundaryUtc) && (
                    <>
                      {' '}
                      · encerra em {fmtDateTime(poll.data.nextBoundaryUtc)} ({timeLeft(poll.data.nextBoundaryUtc)})
                    </>
                  )}
                </div>
              )}
            </div>

            <div className="card mb-6 px-5 py-4">
              <div className="field-label mb-1.5">Seu nick no Wolvesville</div>
              <input
                value={nickname}
                onChange={(e) => setNickname(e.target.value.slice(0, 32))}
                onBlur={checkNickname}
                placeholder="Ex.: SeuNick"
                className="input-dark"
              />
              <div className="mt-1.5 font-sans text-[11.5px] text-dim">
                Usado só para impedir voto duplicado — um voto por nick, não por pessoa/dispositivo.
              </div>
            </div>

            {error && (
              <div className="mb-4">
                <ErrorBox message={error} />
              </div>
            )}

            <div className="flex flex-col gap-4">
              {poll.data.quests.map((q) => {
                  const isVoted = poll.data!.votedQuestId === q.questId
                  const isShuffle = q.questId === SHUFFLE_OPTION_ID
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
                          <div className="font-serif text-lg font-semibold text-ink">
                            {isShuffle ? '🔀 ' : ''}
                            {q.name}
                          </div>
                          <div className="mt-0.5 font-sans text-[12px] text-faint">
                            {isShuffle
                              ? 'se vencer, embaralha as opções (custa ouro do clã) e reabre a votação'
                              : `paga com ${q.gems ? 'gemas' : 'ouro'}`}{' '}
                            ·{' '}
                            <span className="font-mono text-gold">
                              {q.votes} voto{q.votes === 1 ? '' : 's'}
                            </span>
                          </div>
                        </div>
                        <button
                          onClick={() => vote(q.questId)}
                          disabled={busy !== null || isVoted || poll.data!.isClosed || !trimmedNickname}
                          className={isVoted ? 'btn-secondary flex-none' : 'btn-primary flex-none'}
                        >
                          {isVoted ? '✓ Seu voto' : busy === q.questId ? 'Votando…' : 'Votar nesta'}
                        </button>
                      </div>
                    </div>
                  )
              })}
            </div>
          </>
        )}
      </div>
    </div>
  )
}
