import { useState } from 'react'
import { api, ApiError } from '../api/client'
import type { CreateScheduledTaskRequest, ScheduledTask, ScheduledTaskType, TaskExecutionLog } from '../api/types'
import { ErrorBox, Loading, Modal, SectionTitle, Toggle } from '../components/ui'
import { useAsync } from '../lib/useAsync'
import { fmtDateTime, humanCron, OUTCOME_META, questName, TASK_TYPE_LABELS } from '../lib/format'
import { buildCron, parseCron, WEEKDAY_OPTIONS, DEFAULT_SCHEDULE, type FriendlySchedule } from '../lib/cron'

type Mode = 'simple' | 'advanced'

interface FormState {
  type: ScheduledTaskType
  mode: Mode
  schedule: FriendlySchedule
  advancedCron: string
  minVotes: number
  targetQuestId: string
  targetQuestName: string
  targetQuestPromoImageUrl: string
  enabled: boolean
  autoRetryOnXpNotReached: boolean
}

const DEFAULT_FORM: FormState = {
  type: 'ClaimMostVotedQuest',
  mode: 'simple',
  schedule: { ...DEFAULT_SCHEDULE },
  advancedCron: buildCron(DEFAULT_SCHEDULE),
  minVotes: 1,
  targetQuestId: '',
  targetQuestName: '',
  targetQuestPromoImageUrl: '',
  enabled: true,
  autoRetryOnXpNotReached: true,
}

function formFromTask(t: ScheduledTask): FormState {
  const parsed = parseCron(t.cronExpression)
  return {
    type: t.type,
    mode: parsed ? 'simple' : 'advanced',
    schedule: parsed ?? { ...DEFAULT_SCHEDULE },
    advancedCron: t.cronExpression,
    minVotes: t.minVotes,
    targetQuestId: t.targetQuestId ?? '',
    targetQuestName: t.targetQuestName ?? '',
    targetQuestPromoImageUrl: t.targetQuestPromoImageUrl ?? '',
    enabled: t.enabled,
    autoRetryOnXpNotReached: t.autoRetryOnXpNotReached,
  }
}

function toRequest(f: FormState): CreateScheduledTaskRequest {
  const cronExpression = f.mode === 'simple' ? buildCron(f.schedule) : f.advancedCron
  const specific = f.type === 'ClaimSpecificQuest'
  return {
    type: f.type,
    cronExpression,
    timeZoneId: 'America/Sao_Paulo',
    minVotes: f.minVotes,
    targetQuestId: specific ? f.targetQuestId || null : null,
    targetQuestName: specific ? f.targetQuestName || null : null,
    targetQuestPromoImageUrl: specific ? f.targetQuestPromoImageUrl || null : null,
    enabled: f.enabled,
    autoRetryOnXpNotReached: f.autoRetryOnXpNotReached,
  }
}

export function Automations({ clanRegId }: { clanRegId: number }) {
  const tasks = useAsync(() => api.listScheduledTasks(clanRegId), [clanRegId])
  // Missões disponíveis do clã (as mesmas da aba "Missões") para o seletor de missão específica.
  const available = useAsync(() => api.getQuestsOverview(clanRegId), [clanRegId])
  const logs = useAsync(async () => {
    const list = await api.listScheduledTasks(clanRegId)
    const byTask = await Promise.all(
      list.map(async (t) => (await api.getTaskLogs(t.id, 10)).map((l) => ({ ...l, task: t }))),
    )
    return byTask
      .flat()
      .sort((a, b) => b.ranAtUtc.localeCompare(a.ranAtUtc))
      .slice(0, 20)
  }, [clanRegId])

  const [busy, setBusy] = useState(false)
  const [actionError, setActionError] = useState<string | null>(null)
  const [modal, setModal] = useState<{ editing: ScheduledTask | null; form: FormState } | null>(null)
  const [formError, setFormError] = useState<string | null>(null)

  const reloadAll = () => {
    tasks.reload()
    logs.reload()
  }

  const run = async (fn: () => Promise<unknown>) => {
    setBusy(true)
    setActionError(null)
    try {
      await fn()
      reloadAll()
      return true
    } catch (e: unknown) {
      setActionError(e instanceof ApiError ? e.message : 'Erro inesperado.')
      return false
    } finally {
      setBusy(false)
    }
  }

  const submitModal = async () => {
    if (!modal) return
    if (modal.form.type === 'ClaimSpecificQuest' && !modal.form.targetQuestId) {
      setFormError('Escolha a missão específica que a automação deve iniciar.')
      return
    }
    setBusy(true)
    setFormError(null)
    try {
      if (modal.editing) await api.updateScheduledTask(modal.editing.id, toRequest(modal.form))
      else await api.createScheduledTask(clanRegId, toRequest(modal.form))
      setModal(null)
      reloadAll()
    } catch (e: unknown) {
      setFormError(e instanceof ApiError ? e.message : 'Erro inesperado.')
    } finally {
      setBusy(false)
    }
  }

  if (tasks.loading) return <Loading />
  if (tasks.error) return <ErrorBox message={tasks.error} onRetry={tasks.reload} />

  return (
    <div>
      <div className="mb-3.5 flex items-center justify-between">
        <div className="font-sans text-sm font-bold text-lav">Tarefas agendadas</div>
        <button
          onClick={() => {
            setFormError(null)
            setModal({ editing: null, form: { ...DEFAULT_FORM } })
          }}
          className="btn-primary"
        >
          + Nova automação
        </button>
      </div>

      {actionError && (
        <div className="mb-4">
          <ErrorBox message={actionError} />
        </div>
      )}

      <div className="mb-8 flex flex-col gap-3">
        {tasks.data!.length === 0 && (
          <div className="list-card px-5 py-6 font-sans text-[13px] text-muted">
            Nenhuma automação configurada. Crie uma para o agendador trabalhar por você — por exemplo,
            iniciar a missão mais votada toda sexta às 20h.
          </div>
        )}
        {tasks.data!.map((t) => (
          <div key={t.id} className="list-card flex flex-wrap items-center justify-between gap-x-4 gap-y-3 px-[22px] py-[18px]">
            <div className="flex min-w-0 flex-1 basis-[220px] items-center gap-4">
              <Toggle
                on={t.enabled}
                disabled={busy}
                onClick={() =>
                  run(() =>
                    api.updateScheduledTask(t.id, {
                      type: t.type,
                      cronExpression: t.cronExpression,
                      timeZoneId: t.timeZoneId,
                      minVotes: t.minVotes,
                      targetQuestId: t.targetQuestId,
                      targetQuestName: t.targetQuestName,
                      targetQuestPromoImageUrl: t.targetQuestPromoImageUrl,
                      enabled: !t.enabled,
                      autoRetryOnXpNotReached: t.autoRetryOnXpNotReached,
                    }),
                  )
                }
              />
              <div className="min-w-0">
                <div className="font-sans text-[14.5px] font-semibold text-ink-2">
                  {TASK_TYPE_LABELS[t.type] ?? t.type}
                </div>
                <div className="mt-[3px] font-sans text-[12.5px] text-muted">
                  {humanCron(t.cronExpression, t.timeZoneId)}
                  {t.type === 'ClaimMostVotedQuest' || t.type === 'ClaimMostVotedFormQuest'
                    ? ` · mín. ${t.minVotes} votos`
                    : ''}
                  {t.type === 'ClaimSpecificQuest' ? ` · ${t.targetQuestName || 'missão fixada'}` : ''}
                  {t.type === 'SkipQuestWaitingTime' && !t.autoRetryOnXpNotReached
                    ? ' · retentativa automática desligada'
                    : ''}
                </div>
              </div>
            </div>
            <div className="flex flex-none items-center gap-4 sm:gap-[22px]">
              <div className="text-right">
                <div className="font-mono text-[11.5px] text-faint">próxima</div>
                <div className="font-mono text-[12.5px] text-lav">
                  {t.enabled ? fmtDateTime(t.nextRunAtUtc) : 'pausada'}
                </div>
              </div>
              <button
                disabled={busy}
                onClick={() => {
                  setFormError(null)
                  setModal({ editing: t, form: formFromTask(t) })
                }}
                className="btn-ghost"
              >
                Editar
              </button>
              <button
                disabled={busy}
                onClick={() => {
                  if (window.confirm('Excluir esta automação e seu histórico?'))
                    run(() => api.deleteScheduledTask(t.id))
                }}
                className="btn-danger-ghost"
              >
                Excluir
              </button>
            </div>
          </div>
        ))}
      </div>

      <SectionTitle>Histórico de execuções</SectionTitle>
      {logs.loading ? (
        <Loading />
      ) : logs.error ? (
        <ErrorBox message={logs.error} onRetry={logs.reload} />
      ) : (
        <div className="list-card">
          {logs.data!.length === 0 && (
            <div className="px-5 py-5 font-sans text-[13px] text-muted">Nenhuma execução registrada ainda.</div>
          )}
          {logs.data!.map((l: TaskExecutionLog & { task: ScheduledTask }) => {
            const meta = OUTCOME_META[l.outcome]
            return (
              <div
                key={l.id}
                className="flex items-center justify-between border-b border-[rgba(180,150,220,0.07)] px-5 py-4 last:border-b-0"
              >
                <div className="flex items-center gap-3">
                  <div className="h-2 w-2 flex-none rounded-full" style={{ background: meta.color }} />
                  <div>
                    <div className="font-sans text-[13.5px] text-ink-2">
                      {TASK_TYPE_LABELS[l.task.type] ?? l.task.type}
                    </div>
                    <div className="mt-0.5 font-sans text-xs text-faint">{l.message}</div>
                  </div>
                </div>
                <div className="flex flex-none items-center gap-3.5">
                  <div
                    className="rounded-[20px] px-2.5 py-[3px] font-sans text-[10.5px] font-bold"
                    style={{ color: meta.color, background: `${meta.color}22` }}
                  >
                    {meta.label}
                  </div>
                  <div className="w-[110px] text-right font-mono text-xs text-faint">
                    {fmtDateTime(l.ranAtUtc)}
                  </div>
                </div>
              </div>
            )
          })}
        </div>
      )}

      {modal && (
        <Modal
          title={modal.editing ? 'Editar automação' : 'Nova automação'}
          onClose={() => setModal(null)}
        >
          <div className="field-label mb-1.5">Ação</div>
          <select
            value={modal.form.type}
            onChange={(e) =>
              setModal({ ...modal, form: { ...modal.form, type: e.target.value as ScheduledTaskType } })
            }
            className="input-dark cursor-pointer"
          >
            {(Object.keys(TASK_TYPE_LABELS) as ScheduledTaskType[]).map((t) => (
              <option key={t} value={t}>
                {TASK_TYPE_LABELS[t]}
              </option>
            ))}
          </select>

          <div className="field-label mb-1.5 mt-[18px] flex items-center justify-between">
            <span>Quando (horário de São Paulo)</span>
            <button
              onClick={() =>
                setModal({
                  ...modal,
                  form: {
                    ...modal.form,
                    mode: modal.form.mode === 'simple' ? 'advanced' : 'simple',
                    advancedCron: modal.form.mode === 'simple' ? buildCron(modal.form.schedule) : modal.form.advancedCron,
                  },
                })
              }
              className="cursor-pointer border-none bg-transparent font-sans text-[11px] font-semibold text-lav hover:text-ink"
            >
              {modal.form.mode === 'simple' ? 'Usar expressão cron' : 'Usar campos guiados'}
            </button>
          </div>

          {modal.form.mode === 'simple' ? (
            <>
              <div className="mb-2.5 flex gap-1.5">
                {(
                  [
                    ['daily', 'Todo dia'],
                    ['weekly', 'Dias da semana'],
                    ['monthly', 'Dia do mês'],
                  ] as [FriendlySchedule['frequency'], string][]
                ).map(([freq, label]) => (
                  <button
                    key={freq}
                    onClick={() =>
                      setModal({
                        ...modal,
                        form: { ...modal.form, schedule: { ...modal.form.schedule, frequency: freq } },
                      })
                    }
                    className={`flex-1 cursor-pointer rounded-lg border px-2 py-1.5 font-sans text-[12px] font-semibold ${
                      modal.form.schedule.frequency === freq
                        ? 'border-violet/50 bg-violet/15 text-ink'
                        : 'border-[rgba(180,150,220,0.22)] bg-transparent text-muted hover:text-lav'
                    }`}
                  >
                    {label}
                  </button>
                ))}
              </div>

              {modal.form.schedule.frequency === 'weekly' && (
                <div className="mb-2.5 flex flex-wrap gap-1.5">
                  {WEEKDAY_OPTIONS.map((d) => {
                    const on = modal.form.schedule.days.includes(d.value)
                    return (
                      <button
                        key={d.value}
                        onClick={() =>
                          setModal({
                            ...modal,
                            form: {
                              ...modal.form,
                              schedule: {
                                ...modal.form.schedule,
                                days: on
                                  ? modal.form.schedule.days.filter((x) => x !== d.value)
                                  : [...modal.form.schedule.days, d.value],
                              },
                            },
                          })
                        }
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
              )}

              {modal.form.schedule.frequency === 'monthly' && (
                <input
                  type="number"
                  min={1}
                  max={31}
                  value={modal.form.schedule.dayOfMonth}
                  onChange={(e) =>
                    setModal({
                      ...modal,
                      form: {
                        ...modal.form,
                        schedule: {
                          ...modal.form.schedule,
                          dayOfMonth: Math.min(31, Math.max(1, Number(e.target.value) || 1)),
                        },
                      },
                    })
                  }
                  className="input-dark mb-2.5"
                />
              )}

              <input
                type="time"
                value={modal.form.schedule.time}
                onChange={(e) =>
                  setModal({ ...modal, form: { ...modal.form, schedule: { ...modal.form.schedule, time: e.target.value } } })
                }
                className="input-dark"
              />
              <div className="mt-1.5 font-sans text-[11.5px] text-dim">
                {humanCron(buildCron(modal.form.schedule), 'America/Sao_Paulo')}
              </div>
            </>
          ) : (
            <>
              <input
                value={modal.form.advancedCron}
                onChange={(e) => setModal({ ...modal, form: { ...modal.form, advancedCron: e.target.value } })}
                placeholder="0 20 * * FRI"
                className="input-dark"
              />
              <div className="mt-1.5 font-sans text-[11.5px] text-dim">
                Expressão cron de 5 campos: minuto, hora, dia do mês, mês, dia da semana.
              </div>
            </>
          )}

          {modal.form.type === 'ClaimSpecificQuest' && (
            <>
              <div className="field-label mb-1.5 mt-[18px]">Missão a iniciar</div>
              {available.loading ? (
                <div className="input-dark text-muted">Carregando missões…</div>
              ) : available.error ? (
                <ErrorBox message={available.error} onRetry={available.reload} />
              ) : (
                (() => {
                  const list = (available.data?.available ?? []).map((a) => a.quest)
                  // Se a missão fixada não estiver mais entre as disponíveis, mantém a opção salva visível.
                  const missingSaved =
                    modal.form.targetQuestId && !list.some((q) => q.id === modal.form.targetQuestId)
                  return (
                    <select
                      value={modal.form.targetQuestId}
                      onChange={(e) => {
                        const q = list.find((x) => x.id === e.target.value)
                        setModal({
                          ...modal,
                          form: {
                            ...modal.form,
                            targetQuestId: e.target.value,
                            targetQuestName: q ? questName(q) : modal.form.targetQuestName,
                            targetQuestPromoImageUrl: q?.promoImageUrl ?? modal.form.targetQuestPromoImageUrl,
                          },
                        })
                      }}
                      className="input-dark cursor-pointer"
                    >
                      <option value="">Selecione uma missão…</option>
                      {missingSaved && (
                        <option value={modal.form.targetQuestId}>
                          {modal.form.targetQuestName || 'Missão fixada'} (fora das disponíveis agora)
                        </option>
                      )}
                      {list.map((q) => (
                        <option key={q.id} value={q.id}>
                          {questName(q)} · {q.purchasableWithGems ? 'gemas' : 'ouro'}
                        </option>
                      ))}
                    </select>
                  )
                })()
              )}
              <div className="mt-1.5 font-sans text-[11.5px] text-dim">
                Escolha entre as missões disponíveis do clã (as mesmas da aba “Missões”). No horário
                agendado ela é iniciada ignorando votos, desde que ainda esteja disponível — senão, a
                execução é registrada e pulada.
              </div>
            </>
          )}

          {(modal.form.type === 'ClaimMostVotedQuest' || modal.form.type === 'ClaimMostVotedFormQuest') && (
            <>
              <div className="field-label mb-1.5 mt-[18px]">Mínimo de votos (quórum)</div>
              <input
                type="number"
                min={0}
                value={modal.form.minVotes}
                onChange={(e) =>
                  setModal({
                    ...modal,
                    form: { ...modal.form, minVotes: Math.max(0, Number(e.target.value) || 0) },
                  })
                }
                className="input-dark"
              />
              <div className="mt-1.5 font-sans text-[11.5px] text-dim">
                {modal.form.type === 'ClaimMostVotedFormQuest'
                  ? 'Conta os votos do formulário público (aba Votação), incluindo "🔀 Embaralhar missões". A ação só é feita se a mais votada tiver pelo menos este número de votos.'
                  : 'A missão só é iniciada se a mais votada tiver pelo menos este número de votos.'}
              </div>
            </>
          )}

          {modal.form.type === 'SkipQuestWaitingTime' && (
            <>
              <div className="field-label mb-1.5 mt-[18px]">Retentativa automática</div>
              <div className="flex items-center gap-3">
                <Toggle
                  on={modal.form.autoRetryOnXpNotReached}
                  onClick={() =>
                    setModal({
                      ...modal,
                      form: { ...modal.form, autoRetryOnXpNotReached: !modal.form.autoRetryOnXpNotReached },
                    })
                  }
                />
                <span className="font-sans text-[13px] text-muted">
                  {modal.form.autoRetryOnXpNotReached ? 'Ativada' : 'Desativada'}
                </span>
              </div>
              <div className="mt-1.5 font-sans text-[11.5px] text-dim">
                {modal.form.autoRetryOnXpNotReached
                  ? 'Se o XP do tier ainda não bateu no horário configurado, tenta de novo sozinha a cada 30 min, até 10 vezes.'
                  : 'Se o XP do tier ainda não bateu no horário configurado, não tenta de novo sozinha — só no próximo horário normal desta automação.'}
              </div>
            </>
          )}

          <div className="mt-[18px] flex items-center gap-3">
            <Toggle
              on={modal.form.enabled}
              onClick={() => setModal({ ...modal, form: { ...modal.form, enabled: !modal.form.enabled } })}
            />
            <span className="font-sans text-[13px] text-muted">
              {modal.form.enabled ? 'Ativada' : 'Pausada'}
            </span>
          </div>

          {formError && <div className="mt-4 font-sans text-[12.5px] text-danger">{formError}</div>}

          <div className="mt-6 flex justify-end gap-2.5">
            <button onClick={() => setModal(null)} className="btn-ghost">
              Cancelar
            </button>
            <button onClick={submitModal} disabled={busy} className="btn-primary">
              {busy ? 'Salvando…' : modal.editing ? 'Salvar' : 'Criar automação'}
            </button>
          </div>
        </Modal>
      )}
    </div>
  )
}
