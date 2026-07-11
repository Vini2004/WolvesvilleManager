import { Component, useEffect, useMemo, useRef, useState, type ErrorInfo, type ReactNode } from 'react'
import { HOUSES, useTheme, type HouseId } from '../lib/theme'

/** Fundo decorativo. No tema padrão, partículas roxas; no Hogwarts, o Grande Salão. */
export function Particles() {
  const { isHogwarts } = useTheme()
  return isHogwarts ? <GreatHall /> : <PurpleDust />
}

/** Partículas roxas subindo (tema padrão). */
function PurpleDust() {
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

/** Cenário Hogwarts: velas flutuantes, faíscas de varinha e a silhueta do castelo ao fundo. */
function GreatHall() {
  const candles = useMemo(
    () =>
      Array.from({ length: 7 }, (_, i) => ({
        left: 8 + ((i * 13.5) % 84),
        top: 10 + ((i * 26) % 52),
        floatDur: 11 + (i % 5) * 1.4, // 11–16.6s: bem devagar
        flickerDur: 2.6 + (i % 3) * 0.35,
        delay: i * 0.9,
        scale: 0.9 + ((i * 3) % 4) / 10,
      })),
    [],
  )
  const sparks = useMemo(
    () =>
      Array.from({ length: 22 }, (_, i) => ({
        left: (i * 4.6) % 100,
        sx: (i % 2 ? 1 : -1) * (18 + i * 3),
        dur: 5 + (i % 5),
        delay: i * 0.35,
      })),
    [],
  )
  return (
    <div className="pointer-events-none fixed inset-0 z-0 overflow-hidden">
      <Castle />
      {/* teto encantado: brilho quente descendo do topo */}
      <div
        className="absolute inset-x-0 top-0 h-1/2"
        style={{ background: 'radial-gradient(120% 90% at 50% -20%, rgba(212,165,63,0.10), transparent 60%)' }}
      />
      {candles.map((c, i) => (
        <div
          key={`c${i}`}
          className="absolute flex flex-col items-center"
          style={{
            left: `${c.left}%`,
            top: `${c.top}%`,
            transform: `scale(${c.scale})`,
            animation: `candleFloat ${c.floatDur}s ease-in-out ${c.delay}s infinite`,
          }}
        >
          {/* chama: gota alaranjada com núcleo claro e brilho ao redor */}
          <div className="relative" style={{ width: 11, height: 17, marginBottom: -2 }}>
            <div
              className="absolute inset-0"
              style={{
                borderRadius: '50% 50% 50% 50% / 62% 62% 38% 38%',
                background: 'radial-gradient(circle at 50% 68%, #fff3c4, #ffbf45 42%, #f2812d 72%, transparent 86%)',
                boxShadow: '0 0 18px 7px rgba(255,168,70,0.5)',
                transformOrigin: '50% 90%',
                animation: `candleFlicker ${c.flickerDur}s ease-in-out infinite`,
              }}
            />
            <div
              className="absolute"
              style={{
                left: '50%',
                bottom: 3,
                width: 3.5,
                height: 8,
                marginLeft: -1.75,
                borderRadius: '50% 50% 50% 50% / 60% 60% 40% 40%',
                background: '#fff7dd',
              }}
            />
          </div>
          {/* pavio */}
          <div style={{ width: 1.5, height: 3, background: '#241708' }} />
          {/* corpo de cera claro */}
          <div
            style={{
              width: 5,
              height: 22,
              borderRadius: '2px 2px 1px 1px',
              background: 'linear-gradient(#f0e3c4, #d9c59b 55%, #b6996a)',
              boxShadow: 'inset -1.5px 0 1px rgba(0,0,0,0.18), 0 1px 2px rgba(0,0,0,0.3)',
            }}
          />
        </div>
      ))}
      {sparks.map((s, i) => (
        <div
          key={`s${i}`}
          className="absolute rounded-full"
          style={
            {
              left: `${s.left}%`,
              bottom: -10,
              width: 3,
              height: 3,
              background: 'var(--color-gold)',
              boxShadow: '0 0 6px var(--color-gold)',
              '--sx': `${s.sx}px`,
              animation: `sparkleDrift ${s.dur}s ease-out ${s.delay}s infinite`,
            } as React.CSSProperties
          }
        />
      ))}
    </div>
  )
}

/** Silhueta estilizada do castelo de Hogwarts, com janelas acesas, fixada na base da tela. */
function Castle() {
  return (
    <svg
      viewBox="0 0 1200 300"
      preserveAspectRatio="xMidYMax slice"
      className="absolute inset-x-0 bottom-0 h-[46%] w-full opacity-[0.55]"
      style={{ filter: 'drop-shadow(0 0 30px rgba(0,0,0,0.6))' }}
    >
      <defs>
        <linearGradient id="castle" x1="0" y1="0" x2="0" y2="1">
          <stop offset="0" stopColor="#0f0a05" />
          <stop offset="1" stopColor="#1c1206" />
        </linearGradient>
      </defs>
      <path
        fill="url(#castle)"
        d="M0 300 V210 h60 v-40 h30 v40 h50 v-70 l25-30 25 30 v70 h60 v-120 l30-34 30 34 v120 h70 v-60 h26 v-40 h28 v40 h26 v60 h80 v-96 l34-40 34 40 v96 h70 v-150 l40-46 40 46 v150 h74 v-70 h28 v-44 h30 v44 h28 v70 h70 v-110 l32-38 32 38 v110 h64 v-46 h26 v-34 h28 v34 h26 v46 h70 v-64 l24-28 24 28 v64 h60 V300 Z"
      />
      {/* janelas acesas */}
      {[
        [175, 175], [175, 200], [305, 150], [305, 185], [305, 220],
        [590, 130], [590, 165], [590, 200], [590, 235], [608, 148], [572, 148],
        [872, 165], [872, 200], [1010, 170], [1010, 205],
      ].map(([x, y], i) => (
        <rect
          key={i}
          x={x - 3}
          y={y}
          width="6"
          height="10"
          rx="1.5"
          fill="#f2b64a"
          opacity={0.75}
          style={{ animation: `candleFlicker ${2.4 + (i % 3) * 0.5}s ease-in-out ${i * 0.2}s infinite` }}
        />
      ))}
    </svg>
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

/** Emblemas heráldicos por casa (artefatos canônicos: espada, serpente, águia, taça). */
const HOUSE_EMBLEM: Record<HouseId, ReactNode> = {
  gryffindor: (
    <g stroke="#f3e6c8" strokeWidth="1.8" strokeLinecap="round" strokeLinejoin="round" fill="none">
      <path d="M20 9 V26" />
      <path d="M15 24 H25" />
      <path d="M20 26 V30" />
      <circle cx="20" cy="31.5" r="1.5" fill="#f3e6c8" stroke="none" />
    </g>
  ),
  slytherin: (
    <g stroke="#f3e6c8" strokeWidth="1.8" strokeLinecap="round" strokeLinejoin="round" fill="none">
      <path d="M24 12 C16 12 16 18.5 21 20.5 C26.5 22.5 24.5 29 16.5 29" />
      <circle cx="24.5" cy="11.5" r="1.4" fill="#f3e6c8" stroke="none" />
    </g>
  ),
  ravenclaw: (
    <g stroke="#f3e6c8" strokeWidth="1.8" strokeLinecap="round" strokeLinejoin="round" fill="none">
      <path d="M10 16 Q20 23 20 15 Q20 23 30 16" />
      <path d="M20 21 V28" />
      <path d="M20 28 L23 30" />
    </g>
  ),
  hufflepuff: (
    <g stroke="#f3e6c8" strokeWidth="1.8" strokeLinecap="round" strokeLinejoin="round" fill="none">
      <path d="M14.5 13 H25.5 L24 21 H16 Z" />
      <path d="M20 21 V27" />
      <path d="M15.5 28 H24.5" />
    </g>
  ),
}

/** Brasão/selo da casa: escudo heráldico com brilho pulsante. Substitui o logo no tema Hogwarts. */
export function HouseCrest({ size = 36 }: { size?: number }) {
  const { house, houseColor } = useTheme()
  return (
    <div className="relative flex-none" style={{ width: size, height: size }}>
      <div
        className="absolute inset-0 rounded-full"
        style={{ background: houseColor, filter: 'blur(11px)', animation: 'sealGlow 3.5s ease-in-out infinite' }}
      />
      <svg viewBox="0 0 40 44" width={size} height={size} className="relative">
        <defs>
          <linearGradient id={`shield-${house}`} x1="0" y1="0" x2="1" y2="1">
            <stop offset="0" stopColor={houseColor} />
            <stop offset="1" stopColor="#241708" />
          </linearGradient>
        </defs>
        <path
          d="M20 2 L36 7 V22 C36 33 29 40 20 42 C11 40 4 33 4 22 V7 Z"
          fill={`url(#shield-${house})`}
          stroke="#d4a53f"
          strokeWidth="1.6"
        />
        <path d="M20 2 L36 7 V22 C36 27 33.5 31 30 34 L20 6 Z" fill="rgba(255,255,255,0.07)" />
        {HOUSE_EMBLEM[house]}
      </svg>
    </div>
  )
}

/** A marca do app: lua no tema padrão, brasão da casa no tema Hogwarts. */
export function Brand({ size = 36 }: { size?: number }) {
  const { isHogwarts } = useTheme()
  return isHogwarts ? <HouseCrest size={size} /> : <MoonLogo size={size} />
}

/** Botão de faísca no header que abre o seletor de tema (Padrão/Hogwarts) e de casa. */
export function ThemeMenu() {
  const { theme, house, isHogwarts, setTheme, setHouse } = useTheme()
  const [open, setOpen] = useState(false)
  const ref = useRef<HTMLDivElement>(null)

  useEffect(() => {
    if (!open) return
    const onDown = (e: MouseEvent) => {
      if (ref.current && !ref.current.contains(e.target as Node)) setOpen(false)
    }
    document.addEventListener('mousedown', onDown)
    return () => document.removeEventListener('mousedown', onDown)
  }, [open])

  const seg = (active: boolean) =>
    `flex-1 rounded-lg px-2.5 py-2 font-sans text-[12.5px] font-semibold transition-colors ${
      active
        ? 'border border-[rgba(201,164,79,0.3)] bg-white/5 text-ink'
        : 'border border-[rgba(180,150,220,0.22)] bg-transparent text-lav hover:bg-white/5'
    }`

  return (
    <div className="relative flex-none" ref={ref}>
      <button
        onClick={() => setOpen((o) => !o)}
        aria-label="Tema"
        className="flex cursor-pointer items-center rounded-lg border border-[rgba(180,150,220,0.22)] bg-transparent p-2 text-lav transition-colors hover:text-ink"
      >
        <svg width="18" height="18" viewBox="0 0 24 24" fill="none">
          <path d="M12 2l1.8 5.2L19 9l-5.2 1.8L12 16l-1.8-5.2L5 9l5.2-1.8L12 2z" fill="currentColor" />
          <circle cx="19" cy="18" r="1.6" fill="currentColor" />
          <circle cx="5" cy="19" r="1.1" fill="currentColor" />
        </svg>
      </button>
      {open && (
        <div
          className="card absolute right-0 top-[calc(100%+8px)] z-50 w-60 p-4"
          style={{ animation: 'fadeSlideIn 0.18s ease' }}
        >
          <div className="field-label mb-2.5">Tema</div>
          <div className="mb-4 flex gap-2">
            <button className={seg(theme === 'default')} onClick={() => setTheme('default')}>
              Padrão
            </button>
            <button className={seg(isHogwarts)} onClick={() => setTheme('hogwarts')}>
              Hogwarts
            </button>
          </div>
          {isHogwarts && (
            <>
              <div className="field-label mb-2.5">Casa</div>
              <div className="grid grid-cols-2 gap-2">
                {HOUSES.map((h) => {
                  const active = house === h.id
                  return (
                    <button
                      key={h.id}
                      onClick={() => setHouse(h.id)}
                      className="flex cursor-pointer items-center gap-2 rounded-lg px-2.5 py-2 font-sans text-[12px] font-semibold transition-colors"
                      style={{
                        border: `1px solid ${active ? h.color : 'rgba(201,164,79,0.22)'}`,
                        background: active ? `${h.color}22` : 'transparent',
                        color: active ? '#f3e6c8' : 'var(--color-lav)',
                      }}
                    >
                      <span
                        className="h-2.5 w-2.5 flex-none rounded-full"
                        style={{ background: h.color }}
                      />
                      {h.name}
                    </button>
                  )
                })}
              </div>
            </>
          )}
        </div>
      )}
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
