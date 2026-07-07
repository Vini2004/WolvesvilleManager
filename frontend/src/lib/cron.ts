export type Frequency = 'daily' | 'weekly' | 'monthly'

export const WEEKDAY_OPTIONS: { value: string; label: string }[] = [
  { value: 'SUN', label: 'Dom' },
  { value: 'MON', label: 'Seg' },
  { value: 'TUE', label: 'Ter' },
  { value: 'WED', label: 'Qua' },
  { value: 'THU', label: 'Qui' },
  { value: 'FRI', label: 'Sex' },
  { value: 'SAT', label: 'Sáb' },
]

const WEEKDAY_ORDER = WEEKDAY_OPTIONS.map((d) => d.value)

export interface FriendlySchedule {
  frequency: Frequency
  days: string[] // dias da semana (para 'weekly'), ex.: ['MON', 'WED']
  dayOfMonth: number // para 'monthly'
  time: string // 'HH:mm'
}

export const DEFAULT_SCHEDULE: FriendlySchedule = {
  frequency: 'weekly',
  days: ['MON'],
  dayOfMonth: 1,
  time: '14:00',
}

/** Monta uma expressão cron de 5 campos a partir de um agendamento amigável. */
export function buildCron(s: FriendlySchedule): string {
  const [hh, mm] = s.time.split(':').map((n) => Number(n) || 0)
  if (s.frequency === 'daily') return `${mm} ${hh} * * *`
  if (s.frequency === 'monthly') return `${mm} ${hh} ${s.dayOfMonth} * *`
  const days = s.days.length > 0 ? s.days.slice().sort((a, b) => WEEKDAY_ORDER.indexOf(a) - WEEKDAY_ORDER.indexOf(b)) : ['MON']
  return `${mm} ${hh} * * ${days.join(',')}`
}

/** Tenta reconhecer uma expressão cron simples (min/hora fixos, sem passo/intervalo) como agendamento amigável. */
export function parseCron(cron: string): FriendlySchedule | null {
  const parts = cron.trim().split(/\s+/)
  if (parts.length !== 5) return null
  const [min, hour, dom, month, dow] = parts
  if (!/^\d+$/.test(min) || !/^\d+$/.test(hour) || month !== '*') return null
  const time = `${hour.padStart(2, '0')}:${min.padStart(2, '0')}`

  if (dom === '*' && dow === '*') return { frequency: 'daily', days: [], dayOfMonth: 1, time }

  if (dom === '*' && /^[A-Z]{3}(,[A-Z]{3})*$/.test(dow)) {
    const days = dow.split(',')
    if (days.every((d) => WEEKDAY_ORDER.includes(d))) return { frequency: 'weekly', days, dayOfMonth: 1, time }
  }

  if (dow === '*' && /^\d+$/.test(dom)) {
    const n = Number(dom)
    if (n >= 1 && n <= 31) return { frequency: 'monthly', days: [], dayOfMonth: n, time }
  }

  return null
}
