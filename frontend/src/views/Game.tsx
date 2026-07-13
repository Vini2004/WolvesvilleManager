import { useState } from 'react'
import { api, ApiError } from '../api/client'
import type { PlayerProfile } from '../api/types'
import { ErrorBox, Loading, SectionTitle } from '../components/ui'
import { useAsync } from '../lib/useAsync'
import { fmtDate, fmtDateTime, fmtNumber } from '../lib/format'

const GAME_MODE_LABELS: Record<string, string> = {
  quick: 'Partida rápida',
  sandbox: 'Sandbox',
  advanced: 'Avançado',
  'crazy-fun': 'Diversão maluca',
  'ranked-league-silver': 'Ranqueada · Prata',
  'ranked-league-gold': 'Ranqueada · Ouro',
}

const SHOP_TYPE_LABELS: Record<string, string> = {
  AVATAR_ITEMS_SET: 'Conjunto de avatar',
  BASE_ROLE_CARD: 'Carta de papel',
  GEMS: 'Gemas',
  GOLD: 'Ouro',
}

/** -1 = escondido pelas configurações de privacidade do jogador. */
const priv = (n: number | null | undefined) => (n == null || n < 0 ? '—' : fmtNumber(n))

/** Remove imagens de emoji em markdown (![x](url)) do texto dos anúncios do jogo. */
const cleanContent = (s: string) => s.replace(/!\[[^\]]*\]\([^)]*\)/g, '').trim()

function PlayerSearch({ clanRegId }: { clanRegId: number }) {
  const [username, setUsername] = useState('')
  const [player, setPlayer] = useState<PlayerProfile | null>(null)
  const [busy, setBusy] = useState(false)
  const [error, setError] = useState<string | null>(null)

  const search = async () => {
    if (username.trim().length < 3) {
      setError('Digite pelo menos 3 caracteres.')
      return
    }
    setBusy(true)
    setError(null)
    setPlayer(null)
    try {
      setPlayer(await api.searchPlayer(clanRegId, username.trim()))
    } catch (e: unknown) {
      setError(
        e instanceof ApiError && e.status === 404
          ? 'Jogador não encontrado — o nome precisa ser exato.'
          : e instanceof ApiError
            ? e.message
            : 'Erro inesperado.',
      )
    } finally {
      setBusy(false)
    }
  }

  const stats = player?.gameStats
  return (
    <div className="card mb-8 px-7 py-[22px]">
      <div className="mb-1 font-serif text-lg font-semibold text-ink">Buscar jogador</div>
      <div className="mb-4 font-sans text-[12.5px] text-muted">
        Consulte o perfil público de qualquer jogador pelo nome exato — útil para avaliar recrutas.
      </div>
      <div className="flex gap-2.5">
        <input
          value={username}
          onChange={(e) => setUsername(e.target.value)}
          onKeyDown={(e) => e.key === 'Enter' && !busy && search()}
          placeholder="Nome exato do jogador"
          className="input-dark flex-1"
        />
        <button onClick={search} disabled={busy} className="btn-primary flex-none">
          {busy ? '…' : 'Buscar'}
        </button>
      </div>
      {error && <div className="mt-3 font-sans text-[12.5px] text-danger">{error}</div>}

      {player && (
        <div className="mt-5 rounded-xl border border-[rgba(180,150,220,0.16)] px-5 py-4">
          <div className="flex flex-wrap items-center justify-between gap-2">
            <div className="flex items-center gap-3">
              <div
                className="h-9 w-9 flex-none rounded-full"
                style={{ background: player.profileIconColor ?? '#8b5cf6' }}
              />
              <div>
                <div className="font-sans text-[15px] font-semibold text-ink">{player.username}</div>
                <div className="font-sans text-xs text-faint">
                  Nível {player.level == null || player.level < 0 ? '—' : player.level} · visto{' '}
                  {player.lastOnline ? fmtDate(player.lastOnline) : '—'}
                </div>
              </div>
            </div>
            <div className="font-mono text-xs text-muted">
              {player.clanId ? 'Tem clã' : 'Sem clã'} · {priv(player.receivedRosesCount)} rosas
            </div>
          </div>
          {player.personalMessage && (
            <div className="mt-3 whitespace-pre-wrap font-sans text-[12.5px] italic leading-relaxed text-muted">
              {player.personalMessage.trim()}
            </div>
          )}
          <div className="mt-3 flex flex-wrap gap-x-6 gap-y-1 font-mono text-[12px] text-lav">
            <span>Vitórias: {priv(stats?.totalWinCount)}</span>
            <span>Derrotas: {priv(stats?.totalLoseCount)}</span>
            <span>Empates: {priv(stats?.totalTieCount)}</span>
            <span>
              Tempo de jogo:{' '}
              {stats?.totalPlayTimeInMinutes && stats.totalPlayTimeInMinutes > 0
                ? `${fmtNumber(Math.round(stats.totalPlayTimeInMinutes / 60))}h`
                : '—'}
            </span>
          </div>
        </div>
      )}
    </div>
  )
}

export function Game({ clanRegId }: { clanRegId: number }) {
  const battlePass = useAsync(() => api.getBattlePass(clanRegId), [clanRegId])
  const offers = useAsync(() => api.getShopOffers(clanRegId), [clanRegId])
  const rotations = useAsync(() => api.getRoleRotations(clanRegId), [clanRegId])
  const news = useAsync(() => api.getGameAnnouncements(clanRegId), [clanRegId])

  const [hatBusy, setHatBusy] = useState(false)
  const [hatMsg, setHatMsg] = useState<{ ok: boolean; text: string } | null>(null)

  const redeemHat = async () => {
    if (!window.confirm('Resgatar o chapéu exclusivo da API para a conta dona da chave?')) return
    setHatBusy(true)
    setHatMsg(null)
    try {
      await api.redeemApiHat(clanRegId)
      setHatMsg({ ok: true, text: 'Chapéu resgatado! Confira o inventário de avatar no jogo.' })
    } catch (e: unknown) {
      setHatMsg({ ok: false, text: e instanceof ApiError ? e.message : 'Erro inesperado.' })
    } finally {
      setHatBusy(false)
    }
  }

  return (
    <div>
      <div className="card mb-8 flex flex-wrap items-center justify-between gap-4 px-7 py-[22px]">
        <div>
          <div className="mb-1 font-serif text-lg font-semibold text-ink">Chapéu exclusivo da API 🎩</div>
          <div className="font-sans text-[12.5px] leading-relaxed text-muted">
            Presente dos desenvolvedores para quem usa a API: um chapéu de avatar exclusivo,
            resgatado para a conta dona da chave registrada. Só pode ser resgatado uma vez.
          </div>
          {hatMsg && (
            <div className={`mt-2 font-sans text-[12.5px] ${hatMsg.ok ? 'text-ok' : 'text-danger'}`}>
              {hatMsg.text}
            </div>
          )}
        </div>
        <button onClick={redeemHat} disabled={hatBusy} className="btn-primary flex-none">
          {hatBusy ? 'Resgatando…' : 'Resgatar chapéu'}
        </button>
      </div>

      <PlayerSearch clanRegId={clanRegId} />

      <SectionTitle>Battle pass</SectionTitle>
      {battlePass.loading ? (
        <Loading />
      ) : battlePass.error ? (
        <ErrorBox message={battlePass.error} onRetry={battlePass.reload} />
      ) : (
        <div className="mb-8 grid grid-cols-2 gap-[18px] sm:grid-cols-4">
          {(
            [
              ['Temporada', `#${battlePass.data!.number ?? '—'}`],
              ['Início', fmtDate(battlePass.data!.startTime)],
              ['Duração', battlePass.data!.durationInDays ? `${battlePass.data!.durationInDays} dias` : '—'],
              ['XP por recompensa', fmtNumber(battlePass.data!.xpPerReward)],
            ] as [string, string][]
          ).map(([label, value]) => (
            <div key={label} className="card px-5 py-4">
              <div className="mb-1.5 font-sans text-[11px] uppercase tracking-[0.6px] text-muted">{label}</div>
              <div className="font-mono text-xl font-semibold text-gold">{value}</div>
            </div>
          ))}
        </div>
      )}

      <SectionTitle>Ofertas da loja</SectionTitle>
      {offers.loading ? (
        <Loading />
      ) : offers.error ? (
        <ErrorBox message={offers.error} onRetry={offers.reload} />
      ) : (
        <div className="mb-8 grid grid-cols-1 gap-4 sm:grid-cols-2 xl:grid-cols-3">
          {offers.data!.length === 0 && (
            <div className="list-card px-5 py-5 font-sans text-[13px] text-muted">Nenhuma oferta ativa.</div>
          )}
          {offers.data!.map((o, i) => (
            <div key={i} className="list-card overflow-hidden">
              {o.promoImageUrl && <img src={o.promoImageUrl} alt="" className="w-full" />}
              <div className="flex items-center justify-between px-[18px] py-3.5">
                <div>
                  <div className="font-sans text-[13.5px] font-semibold text-ink-2">
                    {SHOP_TYPE_LABELS[o.type ?? ''] ?? (o.type ?? 'Oferta').replace(/_/g, ' ').toLowerCase()}
                  </div>
                  <div className="mt-0.5 font-sans text-[11.5px] text-faint">
                    expira {fmtDateTime(o.expireDate)}
                  </div>
                </div>
                {o.costInGems != null && (
                  <div className="flex-none font-mono text-[14px] font-semibold text-violet">
                    {fmtNumber(o.costInGems)} 💎
                  </div>
                )}
              </div>
            </div>
          ))}
        </div>
      )}

      <SectionTitle>Rotações de papéis</SectionTitle>
      {rotations.loading ? (
        <Loading />
      ) : rotations.error ? (
        <ErrorBox message={rotations.error} onRetry={rotations.reload} />
      ) : (
        <div className="mb-8 flex flex-col gap-3">
          {rotations.data!.map((r, i) => {
            // Papéis da primeira rotação do modo; cada slot pode ter várias opções com probabilidade.
            // Formato profundo da API — blindado: qualquer nível ausente vira lista vazia.
            const slots = r.roleRotations?.[0]?.roleRotation?.roles ?? []
            const roles = [
              ...new Set(
                slots
                  .flat()
                  .map((x) => x?.role)
                  .filter((role): role is string => !!role)
                  .map((role) => role.replace(/-/g, ' ')),
              ),
            ]
            return (
              <div key={r.gameMode ?? i} className="list-card px-5 py-4">
                <div className="mb-2 font-sans text-[13px] font-semibold text-ink-2">
                  {GAME_MODE_LABELS[r.gameMode ?? ''] ?? r.gameMode}
                </div>
                <div className="flex flex-wrap gap-1.5">
                  {roles.length === 0 && <span className="font-sans text-xs text-faint">sem rotação fixa</span>}
                  {roles.map((role) => (
                    <span
                      key={role}
                      className="rounded-[20px] border border-[rgba(139,92,246,0.3)] bg-violet/10 px-2.5 py-0.5 font-sans text-[11px] capitalize text-lav"
                    >
                      {role}
                    </span>
                  ))}
                </div>
              </div>
            )
          })}
        </div>
      )}

      <SectionTitle>Novidades do jogo</SectionTitle>
      {news.loading ? (
        <Loading />
      ) : news.error ? (
        <ErrorBox message={news.error} onRetry={news.reload} />
      ) : (
        <div className="flex flex-col gap-3">
          {news.data!.slice(0, 10).map((a, i) => {
            const text = cleanContent(a.content ?? '')
            const image = a.attachments?.find((att) => att.url)?.url
            return (
              <div key={i} className="list-card overflow-hidden">
                {image && <img src={image} alt="" className="max-h-72 w-full object-cover" />}
                <div className="px-5 py-4">
                  {text && (
                    <div className="whitespace-pre-wrap font-sans text-[13px] leading-relaxed text-ink-2">
                      {text}
                    </div>
                  )}
                  <div className="mt-2 font-sans text-xs text-faint">
                    {a.author?.username ? `${a.author.username} · ` : ''}
                    {fmtDateTime(a.timestamp)}
                  </div>
                </div>
              </div>
            )
          })}
        </div>
      )}
    </div>
  )
}
