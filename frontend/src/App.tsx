import { useCallback, useEffect, useState } from 'react'
import { api, ApiError, clearAccessKey } from './api/client'
import type { RegisteredClan } from './api/types'
import { Brand, ErrorBoundary, MoonLogo, Particles, ThemeMenu } from './components/ui'
import { useTheme } from './lib/theme'
import { Login } from './views/Login'
import { Dashboard } from './views/Dashboard'
import { Quests } from './views/Quests'
import { Members } from './views/Members'
import { Automations } from './views/Automations'
import { Announcements } from './views/Announcements'
import { Chat } from './views/Chat'
import { Activity } from './views/Activity'
import { Game } from './views/Game'
import { RegisterClan } from './views/RegisterClan'

type View =
  | 'dashboard'
  | 'quests'
  | 'members'
  | 'announcements'
  | 'chat'
  | 'activity'
  | 'game'
  | 'automations'
  | 'register'

const TITLES: Record<View, [string, string]> = {
  dashboard: ['Visão geral', 'Ouro, gemas e a missão ativa do clã'],
  quests: ['Missões', 'Disponíveis, votos e histórico de conclusões'],
  members: ['Membros', 'Participação em missões, kick e bloqueios'],
  announcements: ['Anúncios', 'Publique avisos para todo o clã'],
  chat: ['Chat', 'Converse com o clã pelo bot'],
  activity: ['Atividade', 'Livro-razão e log de auditoria do clã'],
  game: ['Jogo', 'Battle pass, loja, rotações e novidades oficiais'],
  automations: ['Automações', 'Tarefas agendadas via expressão cron'],
  register: ['Registrar clã', 'Conecte uma chave de API de clan bot'],
}

const NAV_ICONS: Record<View, React.ReactNode> = {
  dashboard: (
    <svg width="18" height="18" viewBox="0 0 24 24" fill="none">
      <rect x="3" y="3" width="8" height="8" rx="1.5" stroke="currentColor" strokeWidth="1.8" />
      <rect x="13" y="3" width="8" height="8" rx="1.5" stroke="currentColor" strokeWidth="1.8" />
      <rect x="3" y="13" width="8" height="8" rx="1.5" stroke="currentColor" strokeWidth="1.8" />
      <rect x="13" y="13" width="8" height="8" rx="1.5" stroke="currentColor" strokeWidth="1.8" />
    </svg>
  ),
  quests: (
    <svg width="18" height="18" viewBox="0 0 24 24" fill="none">
      <polygon points="12,2 22,12 12,22 2,12" stroke="currentColor" strokeWidth="1.8" strokeLinejoin="round" />
    </svg>
  ),
  members: (
    <svg width="18" height="18" viewBox="0 0 24 24" fill="none">
      <circle cx="9" cy="9" r="4" stroke="currentColor" strokeWidth="1.8" />
      <circle cx="16" cy="11" r="3.2" stroke="currentColor" strokeWidth="1.8" />
      <path d="M2.5 20c0-3.6 2.9-6 6.5-6s6.5 2.4 6.5 6" stroke="currentColor" strokeWidth="1.8" strokeLinecap="round" />
      <path d="M15.5 14.3c2.6 0.5 4.5 2.5 4.5 5.7" stroke="currentColor" strokeWidth="1.8" strokeLinecap="round" />
    </svg>
  ),
  announcements: (
    <svg width="18" height="18" viewBox="0 0 24 24" fill="none">
      <path d="M3 10v4l11 5V5L3 10z" stroke="currentColor" strokeWidth="1.8" strokeLinejoin="round" />
      <path d="M17 9a4 4 0 0 1 0 6" stroke="currentColor" strokeWidth="1.8" strokeLinecap="round" />
    </svg>
  ),
  chat: (
    <svg width="18" height="18" viewBox="0 0 24 24" fill="none">
      <path d="M21 12a8 8 0 0 1-8 8H4l2.3-2.9A8 8 0 1 1 21 12z" stroke="currentColor" strokeWidth="1.8" strokeLinejoin="round" />
    </svg>
  ),
  activity: (
    <svg width="18" height="18" viewBox="0 0 24 24" fill="none">
      <path d="M3 12h4l2.5-7 4 14 2.5-7h5" stroke="currentColor" strokeWidth="1.8" strokeLinecap="round" strokeLinejoin="round" />
    </svg>
  ),
  game: (
    <svg width="18" height="18" viewBox="0 0 24 24" fill="none">
      <rect x="2" y="7" width="20" height="11" rx="4" stroke="currentColor" strokeWidth="1.8" />
      <path d="M7 10.5v4M5 12.5h4" stroke="currentColor" strokeWidth="1.8" strokeLinecap="round" />
      <circle cx="16" cy="11" r="1" fill="currentColor" />
      <circle cx="18.5" cy="13.5" r="1" fill="currentColor" />
    </svg>
  ),
  automations: (
    <svg width="18" height="18" viewBox="0 0 24 24" fill="none">
      <circle cx="12" cy="12" r="9" stroke="currentColor" strokeWidth="1.8" />
      <path d="M12 7v5l3.5 2" stroke="currentColor" strokeWidth="1.8" strokeLinecap="round" />
    </svg>
  ),
  register: (
    <svg width="18" height="18" viewBox="0 0 24 24" fill="none">
      <circle cx="7.5" cy="7.5" r="4.5" stroke="currentColor" strokeWidth="1.8" />
      <path d="M11 11l9 9M16.2 15.8l2.6-2.6M18.8 18.4l2.2-2.2" stroke="currentColor" strokeWidth="1.8" strokeLinecap="round" />
    </svg>
  ),
}

const CLAN_STORAGE = 'wvm.clanRegistrationId'

export default function App() {
  // 'checking' = validando a chave guardada; 'login' = pedir chave; 'app' = autenticado
  const [phase, setPhase] = useState<'checking' | 'login' | 'app'>('checking')
  const [clans, setClans] = useState<RegisteredClan[]>([])
  const [selectedId, setSelectedId] = useState<number | null>(() => {
    const raw = localStorage.getItem(CLAN_STORAGE)
    return raw ? Number(raw) : null
  })
  const [view, setView] = useState<View>('dashboard')
  const [mobileNavOpen, setMobileNavOpen] = useState(false)
  const { isHogwarts } = useTheme()

  const loadClans = useCallback(async () => {
    const list = await api.listClans()
    setClans(list)
    setSelectedId((current) => {
      const valid = current != null && list.some((c) => c.id === current)
      const next = valid ? current : (list[0]?.id ?? null)
      if (next != null) localStorage.setItem(CLAN_STORAGE, String(next))
      else localStorage.removeItem(CLAN_STORAGE)
      return next
    })
    return list
  }, [])

  useEffect(() => {
    loadClans()
      .then((list) => {
        setPhase('app')
        if (list.length === 0) setView('register')
      })
      .catch((e: unknown) => {
        // 401 (chave exigida) ou API fora do ar: a tela de login mostra o erro ao tentar
        void (e instanceof ApiError)
        setPhase('login')
      })
  }, [loadClans])

  const handleLogin = async () => {
    const list = await loadClans()
    setPhase('app')
    setView(list.length === 0 ? 'register' : 'dashboard')
  }

  const logout = () => {
    clearAccessKey()
    setClans([])
    setPhase('login')
  }

  const selectClan = (id: number) => {
    setSelectedId(id)
    localStorage.setItem(CLAN_STORAGE, String(id))
  }

  if (phase === 'checking') {
    return (
      <div className="flex min-h-screen items-center justify-center bg-night">
        <MoonLogo size={56} holeBg="#0d0b14" />
      </div>
    )
  }

  if (phase === 'login') return <Login onSuccess={handleLogin} />

  const selectedClan = clans.find((c) => c.id === selectedId) ?? null
  const [title, subtitle] = TITLES[view]
  const needsClan = view !== 'register'

  return (
    <div className="relative flex min-h-screen overflow-hidden bg-night">
      <Particles />

      {/* SIDEBAR — oculto no mobile, vira um drawer sobreposto quando aberto */}
      {mobileNavOpen && (
        <div
          className="fixed inset-0 z-40 bg-black/60 md:hidden"
          onClick={() => setMobileNavOpen(false)}
        />
      )}
      <div
        className={`fixed inset-y-0 left-0 z-50 flex w-[264px] flex-none -translate-x-full flex-col border-r border-[rgba(180,150,220,0.1)] bg-gradient-to-b from-side-1 to-side-2 px-5 py-7 transition-transform md:relative md:z-1 md:translate-x-0 ${
          mobileNavOpen ? 'translate-x-0' : ''
        }`}
      >
        <div className="mb-5 flex items-center gap-3 border-b border-[rgba(180,150,220,0.1)] px-1 pb-7">
          <Brand size={36} />
          <div className="min-w-0">
            <div className="truncate font-serif text-base font-semibold leading-tight tracking-[0.2px] text-ink">
              {selectedClan?.clanName ?? 'Wolvesville'}
            </div>
            <div className="mt-px font-sans text-[11px] tracking-[0.4px] text-muted">Wolvesville Manager</div>
          </div>
        </div>

        {clans.length > 1 && (
          <select
            value={selectedId ?? ''}
            onChange={(e) => selectClan(Number(e.target.value))}
            className="mb-4 w-full cursor-pointer rounded-lg border border-[rgba(180,150,220,0.18)] bg-side-2 px-3 py-2 font-sans text-[12.5px] font-semibold text-lav outline-none"
          >
            {clans.map((c) => (
              <option key={c.id} value={c.id}>
                {c.clanName}
              </option>
            ))}
          </select>
        )}

        <div className="flex flex-col gap-1">
          {(Object.keys(TITLES) as View[]).map((v) => {
            const active = v === view
            return (
              <div
                key={v}
                onClick={() => {
                  setView(v)
                  setMobileNavOpen(false)
                }}
                className={`flex cursor-pointer items-center gap-3 rounded-[10px] px-3.5 py-2.5 font-sans text-[13.5px] font-semibold transition-colors ${
                  active ? 'bg-violet/15 text-ink' : 'text-muted hover:text-lav'
                }`}
                style={active ? { boxShadow: 'inset 2px 0 0 var(--color-violet)' } : undefined}
              >
                {NAV_ICONS[v]}
                <span>{TITLES[v][0]}</span>
              </div>
            )
          })}
        </div>

        <div className="mt-auto border-t border-[rgba(180,150,220,0.1)] pt-5">
          <div className="mb-3.5 font-sans text-[11.5px] leading-normal text-faint">
            Conectado à API do Wolvesville via clan bot. Chaves são criptografadas em repouso.
          </div>
          <div onClick={logout} className="cursor-pointer font-sans text-[12.5px] font-semibold text-lav hover:text-ink">
            Sair
          </div>
        </div>
      </div>

      {/* MAIN */}
      <div className="relative z-1 flex h-screen min-w-0 flex-1 flex-col overflow-hidden">
        <div className="flex flex-none items-center justify-between gap-3 border-b border-[rgba(180,150,220,0.1)] px-4 py-4 md:px-10 md:py-[22px]">
          <div className="flex min-w-0 items-center gap-3">
            <button
              onClick={() => setMobileNavOpen(true)}
              className="flex-none cursor-pointer rounded-lg border border-[rgba(180,150,220,0.22)] p-2 text-lav md:hidden"
              aria-label="Abrir menu"
            >
              <svg width="20" height="20" viewBox="0 0 24 24" fill="none">
                <path d="M4 6h16M4 12h16M4 18h16" stroke="currentColor" strokeWidth="1.8" strokeLinecap="round" />
              </svg>
            </button>
            <div className="min-w-0">
              <div className="truncate font-serif text-lg font-semibold text-ink md:text-[22px]">{title}</div>
              <div className="mt-0.5 hidden font-sans text-[13px] text-muted sm:block">{subtitle}</div>
            </div>
          </div>
          <div className="flex flex-none items-center gap-2.5">
            <ThemeMenu />
            {selectedClan && (
              <div className="rounded-[20px] border border-[rgba(217,164,65,0.35)] bg-gold/10 px-3 py-1.5 font-sans text-[11.5px] font-bold tracking-[0.4px] text-warn">
                {selectedClan.clanTag ? `#${selectedClan.clanTag} · ` : ''}
                {selectedClan.clanName.toUpperCase()}
              </div>
            )}
          </div>
        </div>

        <div className="flex-1 overflow-y-auto px-4 pb-16 pt-6 md:px-10 md:pt-9">
          <div key={view + (isHogwarts ? 'h' : 'd')} style={{ animation: isHogwarts ? 'inkReveal 0.6s ease' : 'fadeSlideIn 0.4s ease' }}>
            {needsClan && selectedClan == null ? (
              <div className="card p-7 font-sans text-[13.5px] text-muted">
                Nenhum clã registrado ainda.{' '}
                <span className="cursor-pointer font-bold text-gold" onClick={() => setView('register')}>
                  Registre um clã
                </span>{' '}
                para começar.
              </div>
            ) : (
              <ErrorBoundary>
                {view === 'dashboard' && selectedClan && <Dashboard clanRegId={selectedClan.id} />}
                {view === 'quests' && selectedClan && <Quests clanRegId={selectedClan.id} />}
                {view === 'members' && selectedClan && <Members clanRegId={selectedClan.id} />}
                {view === 'announcements' && selectedClan && <Announcements clanRegId={selectedClan.id} />}
                {view === 'chat' && selectedClan && <Chat clanRegId={selectedClan.id} />}
                {view === 'activity' && selectedClan && <Activity clanRegId={selectedClan.id} />}
                {view === 'game' && selectedClan && <Game clanRegId={selectedClan.id} />}
                {view === 'automations' && selectedClan && <Automations clanRegId={selectedClan.id} />}
                {view === 'register' && <RegisterClan clans={clans} onChanged={loadClans} />}
              </ErrorBoundary>
            )}
          </div>
        </div>
      </div>
    </div>
  )
}
