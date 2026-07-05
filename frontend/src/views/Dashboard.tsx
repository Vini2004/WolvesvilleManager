import { useState } from 'react'
import { api, ApiError } from '../api/client'
import { Avatar, ErrorBox, Loading } from '../components/ui'
import { useAsync } from '../lib/useAsync'
import { fmtNumber, questName, timeLeft } from '../lib/format'

export function Dashboard({ clanRegId }: { clanRegId: number }) {
  const overview = useAsync(() => api.getQuestsOverview(clanRegId), [clanRegId])
  const members = useAsync(() => api.listMembers(clanRegId), [clanRegId])
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
  const active = data.active
  const pct = active && active.xpPerReward > 0 ? Math.min(1, active.xp / active.xpPerReward) : 0
  const remaining = active ? timeLeft(active.tierEndTime) : null
  const inWaiting = active?.tierStartTime ? new Date(active.tierStartTime) > new Date() : false

  return (
    <div>
      <div className="mb-6 grid grid-cols-3 gap-[18px]">
        <div className="card px-6 py-[22px]">
          <div className="mb-2.5 font-sans text-xs uppercase tracking-[0.6px] text-muted">Ouro do clã</div>
          <div className="font-mono text-[34px] font-semibold text-gold">{fmtNumber(data.gold)}</div>
          <div className="mt-1.5 font-sans text-xs text-faint">Usado para comprar e iniciar missões</div>
        </div>
        <div className="card px-6 py-[22px]">
          <div className="mb-2.5 font-sans text-xs uppercase tracking-[0.6px] text-muted">Gemas do clã</div>
          <div className="font-mono text-[34px] font-semibold text-violet">{fmtNumber(data.gems)}</div>
          <div className="mt-1.5 font-sans text-xs text-faint">Pula esperas e resgata tempo extra</div>
        </div>
        <div className="card px-6 py-[22px]">
          <div className="mb-2.5 font-sans text-xs uppercase tracking-[0.6px] text-muted">Membros</div>
          <div className="font-mono text-[34px] font-semibold text-ink">
            {members.loading ? '…' : members.data ? members.data.length : '—'}
          </div>
          <div className="mt-1.5 font-sans text-xs text-faint">
            {members.data
              ? `${members.data.filter((m) => m.participateInClanQuests !== false).length} participando de missões`
              : 'Lista de membros indisponível'}
          </div>
        </div>
      </div>

      {actionError && (
        <div className="mb-4">
          <ErrorBox message={actionError} />
        </div>
      )}

      {active ? (
        <div className="card px-7 py-[26px]">
          <div className="flex flex-wrap items-start justify-between gap-4">
            <div>
              <div className="mb-1.5 font-sans text-[11.5px] font-bold uppercase tracking-[0.6px] text-gold">
                Missão ativa · Tier {active.tier + 1}
              </div>
              <div className="font-serif text-2xl font-semibold capitalize text-ink">
                {questName(active.quest)}
              </div>
            </div>
            <div className="rounded-[20px] border border-violet/30 bg-violet/10 px-3.5 py-1.5 font-sans text-[13px] text-lav">
              {inWaiting
                ? 'Aguardando início'
                : remaining
                  ? `Termina em ${remaining}`
                  : active.tierFinished
                    ? 'Tier concluído'
                    : 'Em andamento'}
            </div>
          </div>

          <div className="mt-[22px]">
            <div className="mb-2 flex justify-between font-mono text-[12.5px] text-muted">
              <span>
                {fmtNumber(active.xp)} / {fmtNumber(active.xpPerReward)} XP
              </span>
              <span>{Math.round(pct * 100)}%</span>
            </div>
            <div className="h-2.5 overflow-hidden rounded-md bg-white/5">
              <div
                className="h-full rounded-md transition-[width] duration-1000"
                style={{ width: `${pct * 100}%`, background: 'linear-gradient(90deg,#8b5cf6,#d9a441)' }}
              />
            </div>
          </div>

          <div className="mt-6 flex flex-wrap items-center justify-between gap-3.5">
            <div className="flex items-center">
              {active.participants.slice(0, 8).map((p, i) => (
                <div key={p.playerId ?? i} style={{ marginLeft: i === 0 ? 0 : -8 }}>
                  <Avatar name={p.username ?? '?'} index={i} size={30} />
                </div>
              ))}
              <div className="ml-2.5 font-sans text-[12.5px] text-faint">
                {active.participants.length} participantes
              </div>
            </div>
            <div className="flex gap-2.5">
              <button
                disabled={busy !== null || !inWaiting}
                onClick={() =>
                  run('skip', 'Pular o tempo de espera? Isso gasta GEMAS do clã.', () =>
                    api.skipWaitingTime(clanRegId),
                  )
                }
                className="btn-secondary"
                title={inWaiting ? undefined : 'A missão já saiu da fase de espera'}
              >
                {busy === 'skip' ? '…' : 'Pular espera'}
              </button>
              <button
                disabled={busy !== null || active.claimedTime}
                onClick={() =>
                  run('extra', 'Resgatar tempo extra? Isso gasta OURO do clã.', () =>
                    api.claimExtraTime(clanRegId),
                  )
                }
                className="btn-secondary"
                title={active.claimedTime ? 'Tempo extra já resgatado' : undefined}
              >
                {busy === 'extra' ? '…' : 'Resgatar tempo extra'}
              </button>
              <button
                disabled={busy !== null}
                onClick={() =>
                  run('cancel', 'Cancelar a missão ativa? O progresso será perdido.', () =>
                    api.cancelQuest(clanRegId),
                  )
                }
                className="btn-danger"
              >
                {busy === 'cancel' ? '…' : 'Cancelar'}
              </button>
            </div>
          </div>
        </div>
      ) : (
        <div className="card px-7 py-[26px]">
          <div className="font-serif text-xl font-semibold text-ink">Nenhuma missão ativa</div>
          <div className="mt-2 font-sans text-[13px] leading-relaxed text-muted">
            O clã não está em missão no momento. Vá em <span className="font-bold text-lav">Missões</span>{' '}
            para ver as disponíveis e os votos dos membros, ou configure uma automação para iniciar a mais
            votada no horário combinado.
          </div>
        </div>
      )}
    </div>
  )
}
