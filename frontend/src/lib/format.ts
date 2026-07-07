import type { ClanQuest, QuestReward, ScheduledTaskType, TaskExecutionOutcome } from '../api/types'

export function initials(name: string): string {
  return name
    .split(/[\s_-]+/)
    .filter(Boolean)
    .map((w) => w[0])
    .join('')
    .slice(0, 2)
    .toUpperCase()
}

// ponytail: a API não tem nome oficial nem tradução de missões — dicionário curado,
// adicione entradas conforme novas missões aparecerem no clã.
const QUEST_TITLES_PT: Record<string, string> = {
  fallenangel: 'Anjo Caído',
  untilbreathes: 'Último Suspiro',
  redless: 'Sem Vermelho',
  settler: 'Colonizadores',
  memories: 'Memórias',
  fairycold: 'Fada do Gelo',
  killercircus: 'Circo Assassino',
  chilli: 'Pimenta',
  coolkid: 'Descolado',
  puppyball: 'Filhote Brincalhão',
}

/** Nome legível derivado do arquivo da imagem promocional (a API não envia nome), traduzido quando conhecido. */
export function questName(quest: ClanQuest): string {
  if (!quest.promoImageUrl) return 'Missão'
  const file = quest.promoImageUrl.split('/').pop() ?? ''
  const dot = file.lastIndexOf('.')
  const base = dot > 0 ? file.slice(0, dot) : file
  const key = base.toLowerCase().replace(/[-_\d]/g, '')
  return QUEST_TITLES_PT[key] ?? base.replace(/[-_]/g, ' ')
}

export function fmtNumber(n: number | null | undefined): string {
  return n == null ? '—' : n.toLocaleString('pt-BR')
}

/** Datas do nosso backend vêm em UTC mas sem o sufixo "Z" — sem ele o navegador trata como hora local. */
function parseUtc(iso: string): Date {
  return new Date(/Z$|[+-]\d\d:?\d\d$/.test(iso) ? iso : `${iso}Z`)
}

export function fmtDate(iso: string | null | undefined): string {
  if (!iso) return '—'
  const d = parseUtc(iso)
  return isNaN(d.getTime())
    ? '—'
    : d.toLocaleDateString('pt-BR', { day: 'numeric', month: 'short', year: 'numeric' })
}

export function fmtDateTime(iso: string | null | undefined): string {
  if (!iso) return '—'
  const d = parseUtc(iso)
  return isNaN(d.getTime())
    ? '—'
    : d.toLocaleString('pt-BR', { day: '2-digit', month: 'short', hour: '2-digit', minute: '2-digit' })
}

/** Tempo restante até `iso` (ex.: "4h 12min"); null se já passou ou inválido. */
export function timeLeft(iso: string | null | undefined): string | null {
  if (!iso) return null
  const target = parseUtc(iso).getTime()
  if (isNaN(target)) return null
  const ms = target - Date.now()
  if (ms <= 0) return null
  const totalMin = Math.floor(ms / 60000)
  const days = Math.floor(totalMin / 1440)
  const hours = Math.floor((totalMin % 1440) / 60)
  const min = totalMin % 60
  if (days > 0) return `${days}d ${hours}h`
  if (hours > 0) return `${hours}h ${min}min`
  return `${min}min`
}

export function rewardsSummary(rewards: QuestReward[]): string {
  const labels: Record<string, string> = {
    GOLD: 'Ouro',
    GEMS: 'Gemas',
    CLAN_XP: 'XP',
    XP: 'XP',
    AVATAR_ITEM: 'Item de avatar',
    LOOT_BOX: 'Caixa',
    CLAN_ICON: 'Ícone de clã',
  }
  if (!rewards.length) return 'Sem recompensas listadas'
  return rewards
    .map((r) => {
      const label = labels[r.type ?? ''] ?? r.type ?? '?'
      return r.amount > 0 ? `${fmtNumber(r.amount)} ${label}` : label
    })
    .join(' · ')
}

export const TASK_TYPE_LABELS: Record<ScheduledTaskType, string> = {
  ClaimMostVotedQuest: 'Iniciar missão mais votada',
  SkipQuestWaitingTime: 'Pular tempo de espera',
  ClaimQuestExtraTime: 'Resgatar tempo extra',
}

export const OUTCOME_META: Record<TaskExecutionOutcome, { label: string; color: string }> = {
  Success: { label: 'Sucesso', color: '#7fd99a' },
  Skipped: { label: 'Ignorado', color: '#e8c98a' },
  Failed: { label: 'Falhou', color: '#d9695f' },
}

const WEEKDAYS: Record<string, string> = {
  '0': 'domingo', '7': 'domingo', SUN: 'domingo',
  '1': 'segunda', MON: 'segunda',
  '2': 'terça', TUE: 'terça',
  '3': 'quarta', WED: 'quarta',
  '4': 'quinta', THU: 'quinta',
  '5': 'sexta', FRI: 'sexta',
  '6': 'sábado', SAT: 'sábado',
}

/** Descrição amigável para os padrões de cron mais comuns; cai no cru se não reconhecer. */
export function humanCron(cron: string, timeZoneId: string): string {
  const tz = timeZoneId.split('/').pop()?.replace(/_/g, ' ') ?? timeZoneId
  const parts = cron.trim().split(/\s+/)
  if (parts.length === 5) {
    const [min, hour, dom, , dow] = parts
    const time =
      /^\d+$/.test(min) && /^\d+$/.test(hour)
        ? `${hour.padStart(2, '0')}:${min.padStart(2, '0')}`
        : null
    if (time && dom === '*' && dow === '*') return `Todos os dias às ${time} · ${tz}`
    if (time && dom === '*' && dow in WEEKDAYS) return `Toda ${WEEKDAYS[dow]} às ${time} · ${tz}`
    if (time && /^\d+$/.test(dom) && dow === '*') return `Todo dia ${dom} do mês às ${time} · ${tz}`
    if (min.startsWith('*/') && hour === '*') return `A cada ${min.slice(2)} min · ${tz}`
  }
  return `cron: ${cron} · ${tz}`
}
