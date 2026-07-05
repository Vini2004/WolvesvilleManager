import { useState } from 'react'
import { ApiError, clearAccessKey, setAccessKey } from '../api/client'
import { MoonLogo, Particles } from '../components/ui'

/**
 * Tela de entrada: pede a chave de acesso da aplicação (enviada como X-Api-Key).
 * A validação é feita chamando a API — se o backend não exigir chave (dev), entra direto.
 */
export function Login({ onSuccess }: { onSuccess: () => Promise<void> }) {
  const [key, setKey] = useState('')
  const [remember, setRemember] = useState(true)
  const [busy, setBusy] = useState(false)
  const [error, setError] = useState<string | null>(null)

  const submit = async () => {
    setBusy(true)
    setError(null)
    if (key.trim()) setAccessKey(key.trim(), remember)
    else clearAccessKey()
    try {
      await onSuccess()
    } catch (e: unknown) {
      clearAccessKey()
      setError(
        e instanceof ApiError && e.isUnauthorized
          ? 'Chave de acesso inválida.'
          : e instanceof ApiError
            ? e.message
            : 'Erro inesperado.',
      )
    } finally {
      setBusy(false)
    }
  }

  return (
    <div className="relative z-2 flex min-h-screen w-full items-center justify-center bg-night p-10">
      <Particles />
      <div className="relative z-1 w-full max-w-[400px]">
        <div className="mb-8 flex flex-col items-center gap-3.5">
          <MoonLogo size={56} holeBg="#0d0b14" />
          <div className="text-center">
            <div className="font-serif text-[22px] font-semibold text-ink">Wolvesville Manager</div>
            <div className="mt-1 font-sans text-[13px] text-muted">
              Entre para gerenciar as missões do seu clã
            </div>
          </div>
        </div>

        <div className="card p-7">
          <div className="field-label mb-1.5">Chave de acesso</div>
          <input
            type="password"
            value={key}
            onChange={(e) => setKey(e.target.value)}
            onKeyDown={(e) => e.key === 'Enter' && !busy && submit()}
            placeholder="••••••••••"
            className="input-dark"
            autoFocus
          />
          <div className="mt-2 font-sans text-[11.5px] leading-relaxed text-dim">
            Chave configurada no servidor (Security:AppApiKey). Em ambiente local sem chave, deixe em
            branco.
          </div>

          <div className="mt-3.5 flex items-center justify-between">
            <label className="flex cursor-pointer items-center gap-2 font-sans text-[12.5px] text-muted">
              <div
                onClick={() => setRemember(!remember)}
                className="h-4 w-4 rounded border-[1.5px] transition-colors"
                style={{
                  borderColor: remember ? '#8b5cf6' : 'rgba(180,150,220,0.35)',
                  background: remember ? '#8b5cf6' : 'transparent',
                }}
              />
              Lembrar de mim
            </label>
          </div>

          <button
            onClick={submit}
            disabled={busy}
            className="btn-primary mt-[22px] w-full px-[18px] py-[13px] text-sm"
          >
            {busy ? 'Entrando…' : 'Entrar'}
          </button>

          {error && (
            <div className="mt-3.5 text-center font-sans text-[12.5px] text-danger">{error}</div>
          )}
        </div>

        <div className="mt-5 text-center font-sans text-xs text-dim">
          Acesso restrito a administradores de clã autorizados.
        </div>
      </div>
    </div>
  )
}
