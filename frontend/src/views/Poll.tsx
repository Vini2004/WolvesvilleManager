import { useEffect, useState } from 'react'
import { api, ApiError } from '../api/client'
import { POLL_DURATION_LABELS, SHUFFLE_OPTION_ID, type PollDuration } from '../api/types'
import { ErrorBox, Loading, SectionTitle } from '../components/ui'
import { useAsync } from '../lib/useAsync'
import { fmtDateTime, timeLeft } from '../lib/format'
import { buildCron, parseCron, WEEKDAY_OPTIONS } from '../lib/cron'

const POLL_TIMEZONE = 'America/Sao_Paulo'

/** Aba admin: link compartilhável do formulário público + apuração em tempo real. */
export function Poll({ clanRegId }: { clanRegId: number }) {
  const poll = useAsync(() => api.getPollAdmin(clanRegId), [clanRegId])
  const [copied, setCopied] = useState(false)
  const [busy, setBusy] = useState(false)
  const [duration, setDuration] = useState<PollDuration>('OneDay')
  const [mode, setMode] = useState<'manual' | 'recurring'>('manual')
  const [recurDays, setRecurDays] = useState<string[]>([])
  const [recurTime, setRecurTime] = useState('11:00')
  const [error, setError] = useState<string | null>(null)

  // Sincroniza a aba/campos com o que está SALVO de verdade sempre que o cron recorrente muda
  // (carregou a página, ou uma ação acabou de gravar) — sem isso, abrir a aba "Se repete" mostrava
  // valores padrão em vez do que já estava configurado, e um clique em "Salvar" sobrescrevia um
  // prazo fixo que o admin tinha acabado de definir.
  const closeCron = poll.data?.closeCronExpression ?? null
  useEffect(() => {
    if (closeCron) {
      const parsed = parseCron(closeCron)
      if (parsed && parsed.frequency === 'weekly') {
        setMode('recurring')
        setRecurDays(parsed.days)
        setRecurTime(parsed.time)
        return
      }
    }
    setMode('manual')
    setRecurDays([])
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [closeCron])

  if (poll.loading) return <Loading />
  if (poll.error || !poll.data) return <ErrorBox message={poll.error ?? 'Erro.'} onRetry={poll.reload} />

  const link = `${window.location.origin}/votar/${poll.data.token}`
  const totalShown = poll.data.quests.reduce((sum, q) => sum + q.votes, 0)
  const max = Math.max(1, ...poll.data.quests.map((q) => q.votes))
  const remaining = timeLeft(poll.data.expiresAtUtc)

  const activeRecurring = poll.data.closeCronExpression ? parseCron(poll.data.closeCronExpression) : null
  const activeRecurringLabel =
    activeRecurring && activeRecurring.frequency === 'weekly'
      ? `Fecha toda ${activeRecurring.days.map((d) => WEEKDAY_OPTIONS.find((w) => w.value === d)?.label).join(', ')} às ${activeRecurring.time}`
      : null

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

  const extend = async () => {
    if (
      poll.data!.closeCronExpression &&
      !window.confirm('Isso desliga o prazo recorrente atual e passa a usar um prazo fixo. Continuar?')
    )
      return
    setBusy(true)
    setError(null)
    try {
      await api.setPollExpiration(clanRegId, duration)
      poll.reload()
    } catch (e: unknown) {
      setError(e instanceof ApiError ? e.message : 'Erro inesperado.')
    } finally {
      setBusy(false)
    }
  }

  const saveRecurring = async () => {
    if (recurDays.length === 0) {
      setError('Escolha pelo menos um dia da semana.')
      return
    }
    setBusy(true)
    setError(null)
    try {
      const cron = buildCron({ frequency: 'weekly', days: recurDays, dayOfMonth: 1, time: recurTime })
      await api.setPollRecurringClose(clanRegId, cron, POLL_TIMEZONE)
      poll.reload()
    } catch (e: unknown) {
      setError(e instanceof ApiError ? e.message : 'Erro inesperado.')
    } finally {
      setBusy(false)
    }
  }

  const clearRecurring = async () => {
    setBusy(true)
    setError(null)
    try {
      await api.clearPollRecurringClose(clanRegId)
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
          formulário com as missões disponíveis (mais a opção "🔀 Embaralhar missões", caso ninguém
          goste das atuais) e vota uma vez por navegador — sem login. Crie uma automação do tipo{' '}
          <span className="font-bold text-lav">Iniciar mais votada do formulário</span> para aplicar o
          resultado no horário combinado: inicia a missão vencedora, ou embaralha se "Embaralhar" vencer.
          O resultado fica gravado no histórico abaixo antes da urna zerar para a próxima rodada.
        </div>
        <div className="flex flex-wrap items-center gap-2.5">
          <input readOnly value={link} onFocus={(e) => e.target.select()} className="input-dark flex-1 basis-64" />
          <button onClick={copy} className="btn-primary flex-none">
            {copied ? 'Copiado!' : 'Copiar link'}
          </button>
        </div>
      </div>

      <div className="card mb-7 px-7 py-[26px]">
        <div className="mb-4 font-serif text-lg font-semibold text-ink">Prazo da votação</div>

        <div
          className={`mb-5 rounded-xl border-2 px-5 py-4 ${
            poll.data.isClosed
              ? 'border-danger/50 bg-danger/10'
              : 'border-violet/50 bg-violet/10'
          }`}
        >
          <div className="font-sans text-[11px] font-bold uppercase tracking-[0.08em] text-faint">
            Prazo definido
          </div>
          {poll.data.isClosed ? (
            <div className="mt-1 font-sans text-[16px] font-bold text-danger">
              🔒 Votação encerrada — ninguém consegue votar até o prazo ser renovado.
            </div>
          ) : (
            <div className="mt-1 font-sans text-[16px] font-bold text-ink">
              Encerra em {fmtDateTime(poll.data.expiresAtUtc)}
              {remaining && <span className="font-mono text-gold"> · faltam {remaining}</span>}
            </div>
          )}
          {activeRecurringLabel && (
            <div className="mt-1.5 font-sans text-[12.5px] text-lav">
              🔁 {activeRecurringLabel} · reabre sozinha a cada rodada decidida
            </div>
          )}
        </div>

        <div className="mb-4 flex gap-2">
          <button
            onClick={() => setMode('manual')}
            className={`flex-1 rounded-lg px-3 py-2 font-sans text-[12.5px] font-semibold ${
              mode === 'manual' ? 'border border-[rgba(139,92,246,0.4)] bg-violet/15 text-ink' : 'border border-[rgba(180,150,220,0.22)] text-muted hover:text-lav'
            }`}
          >
            Prazo fixo
          </button>
          <button
            onClick={() => setMode('recurring')}
            className={`flex-1 rounded-lg px-3 py-2 font-sans text-[12.5px] font-semibold ${
              mode === 'recurring' ? 'border border-[rgba(139,92,246,0.4)] bg-violet/15 text-ink' : 'border border-[rgba(180,150,220,0.22)] text-muted hover:text-lav'
            }`}
          >
            Se repete toda semana
          </button>
        </div>

        {mode === 'manual' ? (
          <div className="flex flex-wrap items-center gap-2.5">
            <select
              value={duration}
              onChange={(e) => setDuration(e.target.value as PollDuration)}
              className="input-dark w-auto cursor-pointer"
            >
              {(Object.keys(POLL_DURATION_LABELS) as PollDuration[]).map((d) => (
                <option key={d} value={d}>
                  {POLL_DURATION_LABELS[d]}
                </option>
              ))}
            </select>
            <button onClick={extend} disabled={busy} className="btn-secondary flex-none">
              {busy ? '…' : poll.data.isClosed ? 'Reabrir votação' : 'Adiar prazo'}
            </button>
          </div>
        ) : (
          <div>
            <div className="mb-2.5 flex flex-wrap gap-1.5">
              {WEEKDAY_OPTIONS.map((d) => {
                const on = recurDays.includes(d.value)
                return (
                  <button
                    key={d.value}
                    onClick={() => setRecurDays(on ? recurDays.filter((x) => x !== d.value) : [...recurDays, d.value])}
                    className={`h-9 w-9 cursor-pointer rounded-full border font-sans text-[11.5px] font-bold ${
                      on
                        ? 'border-violet/50 bg-violet/15 text-ink'
                        : 'border-[rgba(180,150,220,0.22)] bg-transparent text-muted hover:text-lav'
                    }`}
                  >
                    {d.label}
                  </button>
                )
              })}
            </div>
            <div className="flex flex-wrap items-center gap-2.5">
              <input
                type="time"
                value={recurTime}
                onChange={(e) => setRecurTime(e.target.value)}
                className="input-dark w-auto"
              />
              <button onClick={saveRecurring} disabled={busy} className="btn-secondary flex-none">
                {busy ? '…' : 'Salvar recorrência'}
              </button>
              {poll.data.closeCronExpression && (
                <button onClick={clearRecurring} disabled={busy} className="btn-ghost flex-none">
                  Voltar para prazo fixo
                </button>
              )}
            </div>
            <div className="mt-2 font-sans text-[11.5px] text-dim">
              Toda vez que a automação decidir a rodada (missão iniciada ou embaralhado), o prazo pula
              sozinho para a próxima ocorrência — não precisa mexer de novo.
            </div>
          </div>
        )}
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
      <div className="flex flex-col gap-3">
        {poll.data.quests.map((q) => {
          const isShuffle = q.questId === SHUFFLE_OPTION_ID
          return (
            <div key={q.questId} className="list-card px-5 py-4">
              <div className="flex items-center justify-between gap-4">
                <div className="min-w-0 font-sans text-[14px] font-semibold text-ink-2">
                  {isShuffle ? '🔀 ' : ''}
                  {q.name}
                  <span className="ml-2 font-sans text-[11.5px] font-normal text-faint">
                    {isShuffle ? 'reabre a votação' : q.gems ? 'gemas' : 'ouro'}
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
          )
        })}
      </div>

      <div className="mb-3.5 mt-9">
        <SectionTitle>Histórico de rodadas</SectionTitle>
      </div>
      {poll.data.history.length === 0 ? (
        <div className="list-card px-5 py-4 font-sans text-[13px] text-muted">
          Ainda vazio — só grava quando uma automação do tipo "Iniciar mais votada do formulário"
          decide de fato uma rodada (inicia uma missão ou embaralha).
        </div>
      ) : (
        <>
          <div className="flex flex-col gap-2">
            {poll.data.history.map((h, i) => (
              <div key={i} className="list-card flex items-center justify-between gap-4 px-5 py-3.5">
                <div className="min-w-0 font-sans text-[13.5px] font-semibold text-ink-2">
                  {h.wasShuffle ? '🔀 ' : ''}
                  {h.questName}
                </div>
                <div className="flex flex-none items-center gap-4">
                  <div className="font-mono text-[12.5px] text-gold">
                    {h.votes} voto{h.votes === 1 ? '' : 's'}
                  </div>
                  <div className="font-mono text-[12px] text-faint">{fmtDateTime(h.decidedAtUtc)}</div>
                </div>
              </div>
            ))}
          </div>
        </>
      )}
    </div>
  )
}
