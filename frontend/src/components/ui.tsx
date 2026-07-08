import { Component, useMemo, type ErrorInfo, type ReactNode } from 'react'

/** Partículas roxas subindo, do design (fundo decorativo). */
export function Particles() {
  const seed = useMemo(
    () =>
      Array.from({ length: 14 }, () => ({
        left: Math.random() * 100,
        delay: Math.random() * 9,
        dur: 7 + Math.random() * 6,
        size: 2 + Math.random() * 3,
      })),
    [],
  )
  return (
    <div className="pointer-events-none fixed inset-0 z-0 overflow-hidden">
      {seed.map((p, i) => (
        <div
          key={i}
          className="absolute rounded-full bg-violet"
          style={{
            left: `${p.left}%`,
            bottom: -20,
            width: p.size,
            height: p.size,
            boxShadow: '0 0 6px #8b5cf6',
            animation: `floatUp ${p.dur}s ease-in ${p.delay}s infinite`,
          }}
        />
      ))}
    </div>
  )
}

/** Lua crescente com brilho pulsante, marca do app. */
export function MoonLogo({ size = 36, holeBg = '#150f22' }: { size?: number; holeBg?: string }) {
  const hole = size * 0.83
  return (
    <div className="relative flex-none" style={{ width: size, height: size }}>
      <div
        className="absolute inset-0 rounded-full bg-violet"
        style={{ filter: 'blur(12px)', opacity: 0.6, animation: 'glowPulse 3.5s ease-in-out infinite' }}
      />
      <div
        className="relative rounded-full"
        style={{
          width: size,
          height: size,
          background: 'radial-gradient(circle at 35% 35%, #f4ecff, #8b5cf6 65%)',
          boxShadow: '0 0 0 1px rgba(255,255,255,0.08) inset',
        }}
      />
      <div
        className="absolute rounded-full"
        style={{ top: size * 0.055, left: size * 0.34, width: hole, height: hole, background: holeBg }}
      />
    </div>
  )
}

export function Toggle({
  on,
  onClick,
  disabled,
}: {
  on: boolean
  onClick: () => void
  disabled?: boolean
}) {
  return (
    <div
      onClick={disabled ? undefined : onClick}
      className={`box-border flex h-6 w-11 items-center rounded-[14px] p-[3px] transition-colors ${
        disabled ? 'cursor-wait opacity-60' : 'cursor-pointer'
      } ${on ? 'bg-violet' : 'bg-white/10'}`}
    >
      <div
        className="h-[18px] w-[18px] rounded-full bg-white shadow transition-transform"
        style={{ transform: `translateX(${on ? 20 : 0}px)` }}
      />
    </div>
  )
}

export function Modal({
  title,
  onClose,
  children,
}: {
  title: string
  onClose: () => void
  children: ReactNode
}) {
  return (
    <div className="fixed inset-0 z-50 flex items-center justify-center bg-black/60 p-6" onClick={onClose}>
      <div
        className="card w-full max-w-md p-7"
        style={{ animation: 'fadeSlideIn 0.25s ease' }}
        onClick={(e) => e.stopPropagation()}
      >
        <div className="mb-5 flex items-center justify-between">
          <div className="font-serif text-lg font-semibold text-ink">{title}</div>
          <button onClick={onClose} className="cursor-pointer border-none bg-transparent text-lg text-faint hover:text-ink">
            ✕
          </button>
        </div>
        {children}
      </div>
    </div>
  )
}

export function Loading({ label = 'Carregando…' }: { label?: string }) {
  return (
    <div className="flex items-center gap-3 py-10 font-sans text-[13px] text-muted">
      <div className="h-4 w-4 animate-spin rounded-full border-2 border-violet border-t-transparent" />
      {label}
    </div>
  )
}

export function ErrorBox({ message, onRetry }: { message: string; onRetry?: () => void }) {
  return (
    <div className="list-card flex items-center justify-between gap-4 border-[rgba(217,105,95,0.35)] px-5 py-4">
      <div className="font-sans text-[13px] text-danger">{message}</div>
      {onRetry && (
        <button onClick={onRetry} className="btn-ghost flex-none">
          Tentar de novo
        </button>
      )}
    </div>
  )
}

/**
 * Isola falhas de render: sem isto, uma exceção em qualquer view apaga o app inteiro (tela preta).
 * Com o boundary, a tela quebrada mostra a mensagem do erro e as outras continuam funcionando.
 */
export class ErrorBoundary extends Component<{ children: ReactNode }, { error: Error | null }> {
  state: { error: Error | null } = { error: null }

  static getDerivedStateFromError(error: Error) {
    return { error }
  }

  componentDidCatch(error: Error, info: ErrorInfo) {
    console.error('Erro de render na view:', error, info)
  }

  render() {
    if (this.state.error) {
      return (
        <ErrorBox
          message={`Algo quebrou nesta tela: ${this.state.error.message}`}
          onRetry={() => this.setState({ error: null })}
        />
      )
    }
    return this.props.children
  }
}

/** Navegação de páginas para listas paginadas; some quando há uma página só. */
export function Pager({
  page,
  pages,
  onChange,
}: {
  page: number
  pages: number
  onChange: (p: number) => void
}) {
  if (pages <= 1) return null
  return (
    <div className="flex items-center justify-center gap-3 pt-4 font-sans text-[12.5px] text-muted">
      <button onClick={() => onChange(page - 1)} disabled={page === 0} className="btn-ghost disabled:opacity-40">
        Anterior
      </button>
      <span>
        Página {page + 1} de {pages}
      </span>
      <button
        onClick={() => onChange(page + 1)}
        disabled={page >= pages - 1}
        className="btn-ghost disabled:opacity-40"
      >
        Próxima
      </button>
    </div>
  )
}

export function SectionTitle({ children }: { children: ReactNode }) {
  return <div className="mb-3.5 font-sans text-sm font-bold text-lav">{children}</div>
}

export function Avatar({ name, index, size = 34 }: { name: string; index: number; size?: number }) {
  const bg = index % 2 ? '#8b5cf6' : '#d9a441'
  const init = name
    .split(/[\s_-]+/)
    .filter(Boolean)
    .map((w) => w[0])
    .join('')
    .slice(0, 2)
    .toUpperCase()
  return (
    <div
      title={name}
      className="flex flex-none items-center justify-center rounded-full font-sans font-bold text-[#1a1220]"
      style={{ width: size, height: size, background: bg, fontSize: size * 0.35 }}
    >
      {init}
    </div>
  )
}
