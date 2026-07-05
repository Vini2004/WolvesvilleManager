import { useState } from 'react'
import { api, ApiError } from '../api/client'
import type { CreateScheduledTaskRequest, ScheduledTask, ScheduledTaskType, TaskExecutionLog } from '../api/types'
import { ErrorBox, Loading, Modal, SectionTitle, Toggle } from '../components/ui'
import { useAsync } from '../lib/useAsync'
import { fmtDateTime, humanCron, OUTCOME_META, TASK_TYPE_LABELS } from '../lib/format'

const CRON_PRESETS: { label: string; value: string }[] = [
  { label: 'Toda sexta às 20:00', value: '0 20 * * FRI' },
  { label: 'Todos os dias às 09:00', value: '0 9 * * *' },
  { label: 'Toda segunda à meia-noite', value: '0 0 * * MON' },
  { label: 'A cada 30 minutos', value: '*/30 * * * *' },
]

interface FormState {
  type: ScheduledTaskType
  cronExpression: string
  minVotes: number
  enabled: boolean
}

const DEFAULT_FORM: FormState = {
  type: 'ClaimMostVotedQuest',
  cronExpression: '0 20 * * FRI',
  minVotes: 1,
  enabled: true,
}

function toRequest(f: FormState): CreateScheduledTaskRequest {
  return { ...f, timeZoneId: 'America/Sao_Paulo' }
}

export function Automations({ clanRegId }: { clanRegId: number }) {
  const tasks = useAsync(() => api.listScheduledTasks(clanRegId), [clanRegId])
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
          <div key={t.id} className="list-card flex items-center justify-between px-[22px] py-[18px]">
            <div className="flex min-w-0 flex-1 items-center gap-4">
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
                      enabled: !t.enabled,
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
                  {t.type === 'ClaimMostVotedQuest' ? ` · mín. ${t.minVotes} votos` : ''}
                </div>
              </div>
            </div>
            <div className="flex flex-none items-center gap-[22px]">
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
                  setModal({
                    editing: t,
                    form: {
                      type: t.type,
                      cronExpression: t.cronExpression,
                      minVotes: t.minVotes,
                      enabled: t.enabled,
                    },
                  })
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

          <div className="field-label mb-1.5 mt-[18px]">Quando (horário de São Paulo)</div>
          <div className="mb-2 flex flex-wrap gap-1.5">
            {CRON_PRESETS.map((p) => (
              <button
                key={p.value}
                onClick={() => setModal({ ...modal, form: { ...modal.form, cronExpression: p.value } })}
                className={`cursor-pointer rounded-[20px] border px-3 py-1 font-sans text-[11.5px] font-semibold ${
                  modal.form.cronExpression === p.value
                    ? 'border-violet/50 bg-violet/15 text-ink'
                    : 'border-[rgba(180,150,220,0.22)] bg-transparent text-muted hover:text-lav'
                }`}
              >
                {p.label}
              </button>
            ))}
          </div>
          <input
            value={modal.form.cronExpression}
            onChange={(e) => setModal({ ...modal, form: { ...modal.form, cronExpression: e.target.value } })}
            placeholder="0 20 * * FRI"
            className="input-dark"
          />
          <div className="mt-1.5 font-sans text-[11.5px] text-dim">
            Expressão cron de 5 campos: minuto, hora, dia do mês, mês, dia da semana.
          </div>

          {modal.form.type === 'ClaimMostVotedQuest' && (
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
                A missão só é iniciada se a mais votada tiver pelo menos este número de votos.
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
