import { useState } from 'react'
import { api, ApiError } from '../api/client'
import type { ClanInfo, RegisteredClan } from '../api/types'
import { SectionTitle } from '../components/ui'
import { fmtDate } from '../lib/format'

type Step = 'key' | 'pick' | 'done'

export function RegisterClan({
  clans,
  onChanged,
}: {
  clans: RegisteredClan[]
  onChanged: () => Promise<RegisteredClan[]>
}) {
  const [step, setStep] = useState<Step>('key')
  const [apiKey, setApiKey] = useState('')
  const [authorized, setAuthorized] = useState<ClanInfo[]>([])
  const [busy, setBusy] = useState(false)
  const [error, setError] = useState<string | null>(null)

  const fetchAuthorized = async () => {
    if (!apiKey.trim()) {
      setError('Cole a chave de API do bot.')
      return
    }
    setBusy(true)
    setError(null)
    try {
      const list = await api.getAuthorizedClans(apiKey.trim())
      if (list.length === 0) {
        setError('Esta chave não é clan bot autorizada de nenhum clã. Autorize-a nas configurações do clã dentro do jogo.')
      } else {
        setAuthorized(list)
        setStep('pick')
      }
    } catch (e: unknown) {
      setError(e instanceof ApiError ? e.message : 'Erro inesperado.')
    } finally {
      setBusy(false)
    }
  }

  const register = async (clanId: string) => {
    setBusy(true)
    setError(null)
    try {
      await api.registerClan(apiKey.trim(), clanId)
      await onChanged()
      setStep('done')
      setApiKey('')
    } catch (e: unknown) {
      setError(e instanceof ApiError ? e.message : 'Erro inesperado.')
    } finally {
      setBusy(false)
    }
  }

  const remove = async (id: number, name: string) => {
    if (!window.confirm(`Remover o clã "${name}"? As automações dele serão excluídas.`)) return
    setBusy(true)
    setError(null)
    try {
      await api.deleteClan(id)
      await onChanged()
    } catch (e: unknown) {
      setError(e instanceof ApiError ? e.message : 'Erro inesperado.')
    } finally {
      setBusy(false)
    }
  }

  const reset = () => {
    setStep('key')
    setApiKey('')
    setAuthorized([])
    setError(null)
  }

  return (
    <div className="grid grid-cols-[1.1fr_0.9fr] items-start gap-6">
      <div className="card px-7 py-[26px]">
        {step === 'key' && (
          <>
            <div className="mb-2 font-sans text-[11.5px] font-bold uppercase tracking-[0.6px] text-violet">
              Passo 1 de 2
            </div>
            <div className="mb-2 font-serif text-xl font-semibold text-ink">Conectar com uma chave de API</div>
            <div className="mb-5 font-sans text-[13px] leading-relaxed text-muted">
              Cole a chave de clan bot do Wolvesville. Vamos listar os clãs para os quais ela é autorizada —
              a chave só é salva no passo seguinte, e sempre criptografada.
            </div>
            <input
              type="password"
              value={apiKey}
              onChange={(e) => setApiKey(e.target.value)}
              onKeyDown={(e) => e.key === 'Enter' && !busy && fetchAuthorized()}
              placeholder="Chave de API do bot"
              className="input-dark"
            />
            <button onClick={fetchAuthorized} disabled={busy} className="btn-primary mt-4">
              {busy ? 'Buscando…' : 'Buscar clãs autorizados'}
            </button>
          </>
        )}

        {step === 'pick' && (
          <>
            <div className="mb-2 font-sans text-[11.5px] font-bold uppercase tracking-[0.6px] text-violet">
              Passo 2 de 2
            </div>
            <div className="mb-2 font-serif text-xl font-semibold text-ink">Escolha o clã para gerenciar</div>
            <div className="mb-[18px] font-sans text-[13px] leading-relaxed text-muted">
              Esta chave é clan bot autorizada para os clãs abaixo.
            </div>
            <div className="mb-5 flex flex-col gap-2.5">
              {authorized.map((c, i) => (
                <div
                  key={c.id}
                  onClick={() => !busy && register(c.id)}
                  className="flex cursor-pointer items-center gap-3.5 rounded-xl border border-[rgba(180,150,220,0.16)] px-4 py-3.5 transition-colors hover:bg-white/5"
                >
                  <div
                    className="h-[34px] w-[34px] flex-none rounded-lg"
                    style={{ background: c.iconColor ?? (i % 2 ? '#8b5cf6' : '#d9a441') }}
                  />
                  <div className="flex-1">
                    <div className="font-sans text-sm font-semibold text-ink-2">{c.name}</div>
                    <div className="mt-0.5 font-mono text-[11.5px] text-faint">
                      {c.tag ? `#${c.tag} · ` : ''}
                      {c.memberCount} membros
                    </div>
                  </div>
                </div>
              ))}
            </div>
            <div className="flex gap-2.5">
              <button onClick={reset} disabled={busy} className="btn-ghost">
                Voltar
              </button>
            </div>
          </>
        )}

        {step === 'done' && (
          <>
            <div className="mb-2 font-sans text-[11.5px] font-bold uppercase tracking-[0.6px] text-ok">
              Concluído
            </div>
            <div className="mb-2 font-serif text-xl font-semibold text-ink">Clã registrado com sucesso</div>
            <div className="mb-5 font-sans text-[13px] leading-relaxed text-muted">
              Agora você pode gerenciar missões, membros e automações deste clã pelo painel.
            </div>
            <button onClick={reset} className="btn-secondary">
              Registrar outro clã
            </button>
          </>
        )}

        {error && <div className="mt-4 font-sans text-[12.5px] text-danger">{error}</div>}
      </div>

      <div>
        <SectionTitle>Clãs já registrados</SectionTitle>
        <div className="list-card">
          {clans.length === 0 && (
            <div className="px-5 py-5 font-sans text-[13px] text-muted">Nenhum clã registrado ainda.</div>
          )}
          {clans.map((r, i) => (
            <div
              key={r.id}
              className="flex items-center justify-between border-b border-[rgba(180,150,220,0.07)] px-5 py-4 last:border-b-0"
            >
              <div className="flex items-center gap-3">
                <div
                  className="h-[30px] w-[30px] rounded-[7px]"
                  style={{ background: i % 2 ? '#8b5cf6' : '#d9a441' }}
                />
                <div>
                  <div className="font-sans text-sm font-semibold text-ink-2">{r.clanName}</div>
                  <div className="mt-0.5 font-mono text-[11px] text-faint">desde {fmtDate(r.createdAtUtc)}</div>
                </div>
              </div>
              <button onClick={() => remove(r.id, r.clanName)} disabled={busy} className="btn-danger-ghost">
                Remover
              </button>
            </div>
          ))}
        </div>
      </div>
    </div>
  )
}
