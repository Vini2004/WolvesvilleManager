import { useState } from 'react'
import { api, ApiError } from '../api/client'
import { Avatar, ErrorBox, Loading, SectionTitle, Toggle } from '../components/ui'
import { useAsync } from '../lib/useAsync'
import { fmtDate, fmtNumber } from '../lib/format'

function XpReportSection({ clanRegId }: { clanRegId: number }) {
  const [days, setDays] = useState(7)
  const report = useAsync(() => api.getXpReport(clanRegId, days), [clanRegId, days])

  return (
    <div className="mt-7">
      <div className="mb-3.5 flex items-center justify-between">
        <SectionTitle>Relatório de XP</SectionTitle>
        <div className="flex gap-1.5">
          {(
            [
              [7, 'Semanal'],
              [30, 'Mensal'],
            ] as [number, string][]
          ).map(([d, label]) => (
            <button
              key={d}
              onClick={() => setDays(d)}
              className={`cursor-pointer rounded-[20px] border px-3 py-1 font-sans text-[11.5px] font-semibold ${
                days === d
                  ? 'border-violet/50 bg-violet/15 text-ink'
                  : 'border-[rgba(180,150,220,0.22)] bg-transparent text-muted hover:text-lav'
              }`}
            >
              {label}
            </button>
          ))}
        </div>
      </div>
      {report.loading ? (
        <Loading />
      ) : report.error ? (
        <ErrorBox message={report.error} onRetry={report.reload} />
      ) : report.data!.sinceUtc == null ? (
        <div className="list-card px-5 py-5 font-sans text-[13px] text-muted">
          Ainda não há histórico suficiente — o sistema tira uma foto diária do XP e o relatório
          passa a valer a partir de amanhã.
        </div>
      ) : (
        <div className="list-card">
          <div className="border-b border-[rgba(180,150,220,0.1)] px-5 py-3 font-sans text-[11.5px] text-faint">
            Ganho de XP de {fmtDate(report.data!.sinceUtc)} até {fmtDate(new Date().toISOString())}
          </div>
          {report.data!.entries.map((e) => (
            <div
              key={e.playerId}
              className="flex items-center justify-between border-b border-[rgba(180,150,220,0.07)] px-5 py-3 last:border-b-0"
            >
              <div className="font-sans text-[13.5px] text-ink-2">{e.username}</div>
              <div className="flex items-center gap-5">
                <div className="font-mono text-xs text-faint">total {fmtNumber(e.currentXp)}</div>
                <div className="w-[90px] text-right font-mono text-[13px] font-semibold text-gold">
                  {e.gainedXp == null ? 'novo' : `+${fmtNumber(e.gainedXp)}`}
                </div>
              </div>
            </div>
          ))}
        </div>
      )}
    </div>
  )
}

const STATUS_META: Record<string, { label: string; color: string }> = {
  ONLINE: { label: 'Online', color: '#7fd99a' },
  PLAY: { label: 'Em partida', color: '#7fd99a' },
  DEFAULT: { label: 'Ausente', color: '#e8c98a' },
  OFFLINE: { label: 'Offline', color: '#77678f' },
}

function statusOf(raw: string | null): { label: string; color: string } {
  if (!raw) return { label: '—', color: '#77678f' }
  return STATUS_META[raw.toUpperCase()] ?? { label: raw, color: '#9182ad' }
}

export function Members({ clanRegId }: { clanRegId: number }) {
  const members = useAsync(() => api.listMembers(clanRegId), [clanRegId])
  const blocklist = useAsync(() => api.getBlocklist(clanRegId), [clanRegId])
  const [busy, setBusy] = useState<string | null>(null)
  const [actionError, setActionError] = useState<string | null>(null)

  const run = async (name: string, fn: () => Promise<void>, reloadBlocklist = false) => {
    setBusy(name)
    setActionError(null)
    try {
      await fn()
      members.reload()
      if (reloadBlocklist) blocklist.reload()
    } catch (e: unknown) {
      setActionError(e instanceof ApiError ? e.message : 'Erro inesperado.')
    } finally {
      setBusy(null)
    }
  }

  if (members.loading) return <Loading />
  if (members.error) return <ErrorBox message={members.error} onRetry={members.reload} />

  const list = members.data!

  return (
    <div>
      {actionError && (
        <div className="mb-4">
          <ErrorBox message={actionError} />
        </div>
      )}

      <div className="mb-3.5 flex items-center justify-between">
        <div className="font-sans text-sm font-bold text-lav">
          {list.length} {list.length === 1 ? 'membro' : 'membros'}
        </div>
        <div className="flex gap-2">
          <button
            disabled={busy !== null}
            onClick={() => {
              if (window.confirm('Ativar a participação em missões de TODOS os membros?'))
                run('all-on', () => api.setAllParticipation(clanRegId, true))
            }}
            className="btn-ghost"
          >
            Todos participam
          </button>
          <button
            disabled={busy !== null}
            onClick={() => {
              if (window.confirm('Desativar a participação em missões de TODOS os membros?'))
                run('all-off', () => api.setAllParticipation(clanRegId, false))
            }}
            className="btn-ghost"
          >
            Ninguém participa
          </button>
        </div>
      </div>

      <div className="list-card overflow-x-auto">
        <div className="box-border flex min-w-[870px] items-center border-b border-[rgba(180,150,220,0.1)] px-5 py-3 font-sans text-[11px] font-bold uppercase tracking-[0.5px] text-faint">
          <div className="min-w-[220px] flex-1">Jogador</div>
          <div className="w-[110px] flex-none">XP no clã</div>
          <div className="w-20 flex-none">Nível</div>
          <div className="w-[150px] flex-none">Participação</div>
          <div className="w-[230px] flex-none text-right">Ações</div>
        </div>
        {list.map((m, i) => {
          const st = statusOf(m.playerStatus ?? m.status)
          return (
            <div
              key={m.playerId}
              className="box-border flex min-w-[870px] items-center border-b border-[rgba(180,150,220,0.07)] px-5 py-4 last:border-b-0"
            >
              <div className="flex min-w-[220px] flex-1 items-center gap-3">
                <Avatar name={m.username} index={i} />
                <div className="min-w-0">
                  <div className="flex items-center gap-2">
                    <span className="font-sans text-sm font-semibold text-ink-2">{m.username}</span>
                    {m.isLeader && (
                      <span className="rounded-[20px] border border-[rgba(217,164,65,0.5)] bg-gold/20 px-2 py-0.5 font-sans text-[10px] font-bold tracking-[0.3px] text-gold">
                        Líder
                      </span>
                    )}
                    {m.isCoLeader && (
                      <span className="rounded-[20px] border border-[rgba(217,164,65,0.3)] bg-gold/10 px-2 py-0.5 font-sans text-[10px] font-bold tracking-[0.3px] text-gold">
                        Vice-líder
                      </span>
                    )}
                  </div>
                  <div className="mt-0.5 font-sans text-[11.5px]" style={{ color: st.color }}>
                    {st.label}
                  </div>
                </div>
              </div>
              <div className="w-[110px] flex-none font-mono text-[13px] text-gold">{fmtNumber(m.xp)}</div>
              <div className="w-20 flex-none font-mono text-[13px] text-lav">{m.level}</div>
              <div className="w-[150px] flex-none">
                <Toggle
                  on={m.participateInClanQuests ?? false}
                  disabled={busy !== null}
                  onClick={() =>
                    run(`part-${m.playerId}`, () =>
                      api.setParticipation(clanRegId, m.playerId, !(m.participateInClanQuests ?? false)),
                    )
                  }
                />
              </div>
              <div className="flex w-[230px] flex-none justify-end gap-2">
                <button
                  disabled={busy !== null}
                  onClick={() => {
                    const flair = window.prompt(`Flair de ${m.username} (ex.: Squid):`)
                    if (flair && flair.trim())
                      run(`flair-${m.playerId}`, () => api.setFlair(clanRegId, m.playerId, flair.trim()))
                  }}
                  className="btn-ghost"
                >
                  Flair
                </button>
                <button
                  disabled={busy !== null}
                  onClick={() => {
                    const reason = window.prompt(`Expulsar ${m.username}? Motivo (opcional):`)
                    if (reason !== null)
                      run(`kick-${m.playerId}`, () => api.kickMember(clanRegId, m.playerId, reason || null))
                  }}
                  className="btn-ghost"
                >
                  Kick
                </button>
                <button
                  disabled={busy !== null}
                  onClick={() => {
                    if (window.confirm(`Bloquear ${m.username}? Ele será expulso e não poderá voltar.`))
                      run(`block-${m.playerId}`, () => api.blockMember(clanRegId, m.playerId), true)
                  }}
                  className="btn-danger-ghost"
                >
                  Block
                </button>
              </div>
            </div>
          )
        })}
      </div>

      <XpReportSection clanRegId={clanRegId} />

      <div className="mt-7">
        <SectionTitle>Bloqueados</SectionTitle>
        {blocklist.loading ? (
          <Loading />
        ) : blocklist.error ? (
          <ErrorBox message={blocklist.error} onRetry={blocklist.reload} />
        ) : (
          <div className="list-card">
            {blocklist.data!.length === 0 && (
              <div className="px-5 py-5 font-sans text-[13px] text-muted">Ninguém bloqueado.</div>
            )}
            {blocklist.data!.map((b) => (
              <div
                key={b.playerId}
                className="flex items-center justify-between border-b border-[rgba(180,150,220,0.07)] px-5 py-4 last:border-b-0"
              >
                <div className="font-sans text-sm text-ink-2">{b.playerUsername ?? b.playerId}</div>
                <div className="flex items-center gap-3.5">
                  <div className="font-sans text-xs text-faint">desde {fmtDate(b.creationTime)}</div>
                  <button
                    disabled={busy !== null || !b.playerId}
                    onClick={() =>
                      run(`unblock-${b.playerId}`, () => api.unblockMember(clanRegId, b.playerId!), true)
                    }
                    className="btn-ghost"
                  >
                    Desbloquear
                  </button>
                </div>
              </div>
            ))}
          </div>
        )}
      </div>
    </div>
  )
}
