import { useState } from 'react'
import { api, ApiError } from '../api/client'
import { ErrorBox, Loading, SectionTitle } from '../components/ui'
import { useAsync } from '../lib/useAsync'
import { fmtDate, fmtNumber, questName, rewardsSummary } from '../lib/format'

export function Quests({ clanRegId }: { clanRegId: number }) {
  const overview = useAsync(() => api.getQuestsOverview(clanRegId), [clanRegId])
  const history = useAsync(() => api.getQuestHistory(clanRegId), [clanRegId])
  const [busy, setBusy] = useState<string | null>(null)
  const [actionError, setActionError] = useState<string | null>(null)

  const run = async (name: string, confirmMsg: string, fn: () => Promise<void>) => {
    if (!window.confirm(confirmMsg)) return
    setBusy(name)
    setActionError(null)
    try {
      await fn()
      overview.reload()
    } catch (e: unknown) {
      setActionError(e instanceof ApiError ? e.message : 'Erro inesperado.')
    } finally {
      setBusy(null)
    }
  }

  if (overview.loading) return <Loading />
  if (overview.error) return <ErrorBox message={overview.error} onRetry={overview.reload} />

  const data = overview.data!
  const maxVotes = Math.max(1, ...data.available.map((q) => q.votes))
  const hasActive = data.active != null

  return (
    <div>
      <div className="mb-3.5 flex items-center justify-between font-sans text-sm font-bold text-lav">
        <span>Missões disponíveis</span>
        <button
          disabled={busy !== null}
          onClick={() =>
            run('shuffle', 'Embaralhar as missões disponíveis? Isso gasta OURO do clã.', () =>
              api.shuffleQuests(clanRegId),
            )
          }
          className="btn-secondary"
        >
          {busy === 'shuffle' ? '…' : 'Embaralhar (custa ouro)'}
        </button>
      </div>

      {actionError && (
        <div className="mb-4">
          <ErrorBox message={actionError} />
        </div>
      )}

      {data.available.length === 0 ? (
        <div className="list-card mb-9 px-5 py-6 font-sans text-[13px] text-muted">
          Nenhuma missão disponível no momento.
        </div>
      ) : (
        <div className="mb-9 grid grid-cols-1 gap-4 sm:grid-cols-2 xl:grid-cols-3">
          {data.available.map((aq) => {
            const q = aq.quest
            const gems = q.purchasableWithGems
            return (
              <div key={q.id} className="list-card flex flex-col overflow-hidden">
                {q.promoImageUrl ? (
                  // Altura natural: a proporção original da arte, sem cortes.
                  <img src={q.promoImageUrl} alt="" className="w-full" />
                ) : (
                  <div
                    className="flex h-48 items-center justify-center"
                    style={{
                      background:
                        'repeating-linear-gradient(135deg,rgba(255,255,255,0.05),rgba(255,255,255,0.05) 10px,rgba(255,255,255,0.02) 10px,rgba(255,255,255,0.02) 20px)',
                    }}
                  >
                    <span className="font-mono text-[11px] tracking-[0.4px] text-white/50">sem imagem</span>
                  </div>
                )}
                <div className="flex flex-1 flex-col px-[18px] py-4">
                  <div className="flex items-center justify-between gap-2.5">
                    <div className="font-serif text-[16.5px] font-semibold capitalize text-ink">
                      {questName(q)}
                    </div>
                    <div
                      className="whitespace-nowrap rounded-[20px] border px-2.5 py-[3px] font-sans text-[10.5px] font-bold tracking-[0.3px]"
                      style={{
                        color: gems ? '#8b5cf6' : '#d9a441',
                        background: gems ? 'rgba(139,92,246,0.14)' : 'rgba(217,164,65,0.14)',
                        borderColor: gems ? 'rgba(139,92,246,0.3)' : 'rgba(217,164,65,0.3)',
                      }}
                    >
                      {gems ? 'Gemas' : 'Ouro'}
                    </div>
                  </div>
                  <div className="mt-1.5 font-sans text-[12.5px] text-muted">{rewardsSummary(q.rewards)}</div>

                  <div className="mb-4 mt-3.5">
                    <div className="mb-1.5 flex justify-between font-mono text-[11.5px] text-faint">
                      <span>
                        {aq.votes} {aq.votes === 1 ? 'voto' : 'votos'}
                      </span>
                    </div>
                    <div className="h-1.5 overflow-hidden rounded bg-white/5">
                      <div
                        className="h-full rounded bg-violet transition-[width] duration-1000"
                        style={{ width: `${(aq.votes / maxVotes) * 100}%` }}
                      />
                    </div>
                  </div>

                  <button
                    disabled={busy !== null || hasActive}
                    title={hasActive ? 'Já existe uma missão ativa' : undefined}
                    onClick={() =>
                      run(
                        `claim-${q.id}`,
                        `Iniciar "${questName(q)}"? Isso gasta ${gems ? 'GEMAS' : 'OURO'} do clã.`,
                        () => api.claimQuest(clanRegId, q.id),
                      )
                    }
                    className="btn-primary mt-auto w-full"
                  >
                    {busy === `claim-${q.id}` ? '…' : 'Iniciar missão'}
                  </button>
                </div>
              </div>
            )
          })}
        </div>
      )}

      <SectionTitle>Histórico</SectionTitle>
      {history.loading ? (
        <Loading />
      ) : history.error ? (
        <ErrorBox message={history.error} onRetry={history.reload} />
      ) : (
        <div className="list-card">
          {history.data!.length === 0 && (
            <div className="px-5 py-6 font-sans text-[13px] text-muted">Nenhuma missão concluída ainda.</div>
          )}
          {history.data!.map((h, i) => (
            <div
              key={`${h.quest.id}-${i}`}
              className="flex items-center justify-between border-b border-[rgba(180,150,220,0.07)] px-5 py-4 last:border-b-0"
            >
              <div>
                <div className="font-sans text-sm font-semibold capitalize text-ink-2">{questName(h.quest)}</div>
                <div className="mt-0.5 font-sans text-xs text-faint">Concluída em {fmtDate(h.tierEndTime)}</div>
              </div>
              <div className="flex items-center gap-[22px]">
                <div className="font-mono text-[12.5px] text-muted">Tier {h.tier}</div>
                <div className="font-mono text-[12.5px] text-gold">{fmtNumber(h.xp)} XP</div>
              </div>
            </div>
          ))}
        </div>
      )}
    </div>
  )
}
