import { useEffect, useState } from 'react'
import { api, ApiError } from '../api/client'
import type { WelcomeCheckResult, WelcomeCheckStatus } from '../api/types'
import { ErrorBox, Loading, Toggle } from '../components/ui'
import { useAsync } from '../lib/useAsync'
import { fmtDateTime } from '../lib/format'

const MAX_LEN = 500

// Mesmas cores do histórico de execuções das automações (OUTCOME_META), para o painel de
// diagnóstico ler igual ao resto do app.
const WELCOME_STATUS_META: Record<WelcomeCheckStatus, { label: string; color: string }> = {
  Sent: { label: 'Enviada', color: '#7fd99a' },
  Held: { label: 'Aguardando horário', color: '#e8c98a' },
  Skipped: { label: 'Ignorada', color: '#9b93a8' },
  Failed: { label: 'Falhou', color: '#d9695f' },
}

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
  const [welcomeTime1, setWelcomeTime1] = useState('')
  const [welcomeTime2, setWelcomeTime2] = useState('')
  const [welcomeBusy, setWelcomeBusy] = useState(false)
  const [welcomeSaved, setWelcomeSaved] = useState(false)
  const [welcomeError, setWelcomeError] = useState<string | null>(null)

  useEffect(() => {
    if (welcome.data) {
      setWelcomeEnabled(welcome.data.enabled)
      setWelcomeTemplate(welcome.data.template)
      setWelcomeTime1(welcome.data.sendTime1 ?? '')
      setWelcomeTime2(welcome.data.sendTime2 ?? '')
    }
  }, [welcome.data])

  const saveWelcome = async () => {
    setWelcomeBusy(true)
    setWelcomeError(null)
    try {
      await api.setWelcomeSettings(
        clanRegId,
        welcomeEnabled,
        welcomeTemplate,
        welcomeTime1 || null,
        welcomeTime2 || null,
      )
      setWelcomeSaved(true)
      setTimeout(() => setWelcomeSaved(false), 2000)
      welcome.reload()
    } catch (e: unknown) {
      setWelcomeError(e instanceof ApiError ? e.message : 'Erro inesperado.')
    } finally {
      setWelcomeBusy(false)
    }
  }

  const [checking, setChecking] = useState(false)
  const [checkResult, setCheckResult] = useState<WelcomeCheckResult | null>(null)

  const runWelcomeCheck = async () => {
    setChecking(true)
    setWelcomeError(null)
    setCheckResult(null)
    try {
      setCheckResult(await api.runWelcomeCheck(clanRegId))
      welcome.reload()
      chat.reload()
    } catch (e: unknown) {
      setWelcomeError(e instanceof ApiError ? e.message : 'Erro inesperado.')
    } finally {
      setChecking(false)
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

            <div className="mt-4 font-sans text-[12.5px] font-semibold text-ink-2">
              Horários de envio (opcional)
            </div>
            <div className="mb-2 font-sans text-[11.5px] leading-relaxed text-dim">
              Se definir horários, as boas-vindas ficam represadas e liberadas a partir desses
              horários (fuso de Brasília): quem entra depois de um horário é saudado no próximo —
              ex.: com 09:00 e 19:00, quem entra às 10:00 é saudado a partir das 19:00; quem entra
              às 20:00, a partir das 09:00 do dia seguinte. Não é exclusivo desses instantes: o site
              recebe um "toque" leve neles para acordar e enviar, mas qualquer vez que ele já esteja
              acordado (por outra automação) depois do horário de liberação a mensagem também sai.
              Deixe em branco para saudar assim que o site acordar depois da entrada.
            </div>
            <div className="flex flex-wrap items-center gap-2.5">
              <input
                type="time"
                value={welcomeTime1}
                onChange={(e) => setWelcomeTime1(e.target.value)}
                className="input-dark w-auto"
              />
              <input
                type="time"
                value={welcomeTime2}
                onChange={(e) => setWelcomeTime2(e.target.value)}
                className="input-dark w-auto"
              />
              {(welcomeTime1 || welcomeTime2) && (
                <button
                  onClick={() => {
                    setWelcomeTime1('')
                    setWelcomeTime2('')
                  }}
                  className="btn-ghost flex-none"
                >
                  Limpar horários
                </button>
              )}
            </div>

            <div className="mt-3 flex flex-wrap items-center gap-2.5">
              <button onClick={saveWelcome} disabled={welcomeBusy} className="btn-secondary flex-none">
                {welcomeBusy ? '…' : welcomeSaved ? 'Salvo!' : 'Salvar'}
              </button>
              <button onClick={runWelcomeCheck} disabled={checking} className="btn-ghost flex-none">
                {checking ? 'Verificando…' : 'Verificar entradas agora'}
              </button>
            </div>

            <div className="mt-2.5 font-sans text-[11.5px] text-dim">
              {welcome.data?.lastCheckAtUtc ? (
                <>
                  Última verificação: {fmtDateTime(welcome.data.lastCheckAtUtc)}
                  {welcome.data.lastCheckResult ? ` — ${welcome.data.lastCheckResult}` : ''}
                </>
              ) : (
                'Ainda não houve nenhuma verificação.'
              )}
              {(welcomeTime1 || welcomeTime2) && !welcome.data?.pingConfigured && (
                <span className="text-gold">
                  {' '}
                  · Atenção: o gatilho que acorda o site nesses horários não está criado — clique em
                  "Verificar entradas agora" para tentar criá-lo.
                </span>
              )}
            </div>

            {checkResult && (
              <div className="mt-3 rounded-lg border border-[rgba(180,150,220,0.22)] px-3.5 py-3">
                <div className="font-sans text-[12.5px] font-semibold text-ink-2">{checkResult.summary}</div>
                {checkResult.error && (
                  <div className="mt-1 font-sans text-[12px] text-danger">{checkResult.error}</div>
                )}
                {checkResult.entries.length > 0 && (
                  <div className="mt-2 flex flex-col gap-1.5">
                    {checkResult.entries.map((e, i) => (
                      <div key={i} className="font-sans text-[12px] leading-relaxed">
                        <span
                          className="font-semibold"
                          style={{ color: WELCOME_STATUS_META[e.status].color }}
                        >
                          {WELCOME_STATUS_META[e.status].label}
                        </span>
                        <span className="text-ink-2"> {e.username ?? '(sem nick)'}</span>
                        <span className="text-faint"> · {fmtDateTime(e.joinedAtUtc)}</span>
                        <div className="text-dim">{e.detail}</div>
                      </div>
                    ))}
                  </div>
                )}
              </div>
            )}
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
