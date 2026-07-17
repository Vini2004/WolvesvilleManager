import type {
  AvailableQuest,
  BattlePassSeason,
  BlocklistEntry,
  ChatMessage,
  ClanAnnouncement,
  ClanLogEntry,
  GameAnnouncement,
  PlayerProfile,
  RoleRotationEntry,
  ShopOffer,
  ClanInfo,
  ClanMember,
  CreateScheduledTaskRequest,
  LedgerEntry,
  PollAdmin,
  PollDuration,
  PollInfo,
  PollWindow,
  QuestHistoryEntry,
  QuestsOverview,
  RegisteredClan,
  ScheduledTask,
  TaskExecutionLog,
  XpReport,
} from './types'

const BASE_URL: string = import.meta.env.VITE_API_URL ?? 'http://localhost:5074'

const ACCESS_KEY_STORAGE = 'wvm.accessKey'

export function getAccessKey(): string | null {
  return sessionStorage.getItem(ACCESS_KEY_STORAGE) ?? localStorage.getItem(ACCESS_KEY_STORAGE)
}

export function setAccessKey(key: string, remember: boolean) {
  clearAccessKey()
  ;(remember ? localStorage : sessionStorage).setItem(ACCESS_KEY_STORAGE, key)
}

export function clearAccessKey() {
  localStorage.removeItem(ACCESS_KEY_STORAGE)
  sessionStorage.removeItem(ACCESS_KEY_STORAGE)
}

/** Erro da API com a mensagem do ProblemDetails do backend. */
export class ApiError extends Error {
  status: number
  constructor(status: number, message: string) {
    super(message)
    this.status = status
  }
  get isUnauthorized() {
    return this.status === 401
  }
}

async function request<T>(path: string, init?: RequestInit): Promise<T> {
  const headers: Record<string, string> = {
    ...(init?.body ? { 'Content-Type': 'application/json' } : {}),
    ...((init?.headers as Record<string, string>) ?? {}),
  }
  const key = getAccessKey()
  if (key) headers['X-Api-Key'] = key

  let res: Response
  try {
    res = await fetch(`${BASE_URL}${path}`, { ...init, headers })
  } catch {
    throw new ApiError(0, 'Não foi possível conectar à API. Verifique se o backend está no ar.')
  }

  if (!res.ok) {
    let message = `Erro ${res.status}`
    try {
      const body = await res.json()
      message = body.detail ?? body.message ?? body.title ?? message
    } catch {
      /* corpo não é JSON */
    }
    throw new ApiError(res.status, message)
  }

  if (res.status === 204) return undefined as T
  return (await res.json()) as T
}

export const api = {
  // ---------- Clãs ----------
  listClans: () => request<RegisteredClan[]>('/api/clans'),
  getAuthorizedClans: (apiKey: string) =>
    request<ClanInfo[]>('/api/clans/authorized', {
      method: 'POST',
      body: JSON.stringify({ apiKey }),
    }),
  registerClan: (apiKey: string, clanId: string) =>
    request<RegisteredClan>('/api/clans', {
      method: 'POST',
      body: JSON.stringify({ apiKey, clanId }),
    }),
  deleteClan: (id: number) => request<void>(`/api/clans/${id}`, { method: 'DELETE' }),

  // ---------- Missões ----------
  getQuestsOverview: (clanId: number) => request<QuestsOverview>(`/api/clans/${clanId}/quests`),
  getQuestHistory: (clanId: number) =>
    request<QuestHistoryEntry[]>(`/api/clans/${clanId}/quests/history`),
  claimQuest: (clanId: number, questId: string) =>
    request<void>(`/api/clans/${clanId}/quests/claim`, {
      method: 'POST',
      body: JSON.stringify({ questId }),
    }),
  shuffleQuests: (clanId: number) =>
    request<void>(`/api/clans/${clanId}/quests/shuffle`, { method: 'POST' }),
  skipWaitingTime: (clanId: number) =>
    request<void>(`/api/clans/${clanId}/quests/skip-waiting-time`, { method: 'POST' }),
  claimExtraTime: (clanId: number) =>
    request<void>(`/api/clans/${clanId}/quests/claim-extra-time`, { method: 'POST' }),
  cancelQuest: (clanId: number) =>
    request<void>(`/api/clans/${clanId}/quests/cancel`, { method: 'POST' }),

  // ---------- Membros ----------
  listMembers: (clanId: number) => request<ClanMember[]>(`/api/clans/${clanId}/members`),
  getXpReport: (clanId: number, days: number) =>
    request<XpReport>(`/api/clans/${clanId}/members/xp-report?days=${days}`),
  setFlair: (clanId: number, playerId: string, flair: string) =>
    request<void>(`/api/clans/${clanId}/members/${playerId}/flair`, {
      method: 'PUT',
      body: JSON.stringify({ flair }),
    }),
  getBlocklist: (clanId: number) =>
    request<BlocklistEntry[]>(`/api/clans/${clanId}/members/blocklist`),
  setParticipation: (clanId: number, playerId: string, participate: boolean) =>
    request<void>(`/api/clans/${clanId}/members/${playerId}/participation`, {
      method: 'PUT',
      body: JSON.stringify({ participate }),
    }),
  setAllParticipation: (clanId: number, participate: boolean) =>
    request<void>(`/api/clans/${clanId}/members/participation`, {
      method: 'PUT',
      body: JSON.stringify({ participate }),
    }),
  kickMember: (clanId: number, playerId: string, reason: string | null) =>
    request<void>(`/api/clans/${clanId}/members/${playerId}/kick`, {
      method: 'POST',
      body: JSON.stringify({ reason }),
    }),
  blockMember: (clanId: number, playerId: string) =>
    request<void>(`/api/clans/${clanId}/members/${playerId}/block`, { method: 'POST' }),
  unblockMember: (clanId: number, playerId: string) =>
    request<void>(`/api/clans/${clanId}/members/${playerId}/unblock`, { method: 'POST' }),

  // ---------- Anúncios ----------
  getAnnouncements: (clanId: number) =>
    request<ClanAnnouncement[]>(`/api/clans/${clanId}/announcements`),
  postAnnouncement: (clanId: number, message: string) =>
    request<void>(`/api/clans/${clanId}/announcements`, {
      method: 'POST',
      body: JSON.stringify({ message }),
    }),

  // ---------- Chat, ledger e logs ----------
  getChat: (clanId: number) => request<ChatMessage[]>(`/api/clans/${clanId}/chat`),
  sendChat: (clanId: number, message: string) =>
    request<void>(`/api/clans/${clanId}/chat`, {
      method: 'POST',
      body: JSON.stringify({ message }),
    }),
  getLedger: (clanId: number) => request<LedgerEntry[]>(`/api/clans/${clanId}/ledger`),
  getLogs: (clanId: number) => request<ClanLogEntry[]>(`/api/clans/${clanId}/logs`),

  // ---------- Jogo ----------
  redeemApiHat: (clanId: number) =>
    request<void>(`/api/clans/${clanId}/game/redeem-api-hat`, { method: 'POST' }),
  searchPlayer: (clanId: number, username: string) =>
    request<PlayerProfile>(
      `/api/clans/${clanId}/game/players/search?username=${encodeURIComponent(username)}`,
    ),
  getRoleRotations: (clanId: number) =>
    request<RoleRotationEntry[]>(`/api/clans/${clanId}/game/role-rotations`),
  getBattlePass: (clanId: number) =>
    request<BattlePassSeason>(`/api/clans/${clanId}/game/battle-pass`),
  getShopOffers: (clanId: number) => request<ShopOffer[]>(`/api/clans/${clanId}/game/shop-offers`),
  getGameAnnouncements: (clanId: number) =>
    request<GameAnnouncement[]>(`/api/clans/${clanId}/game/announcements`),

  // ---------- Automações ----------
  listScheduledTasks: (clanId: number) =>
    request<ScheduledTask[]>(`/api/clans/${clanId}/scheduled-tasks`),
  createScheduledTask: (clanId: number, body: CreateScheduledTaskRequest) =>
    request<ScheduledTask>(`/api/clans/${clanId}/scheduled-tasks`, {
      method: 'POST',
      body: JSON.stringify(body),
    }),
  updateScheduledTask: (taskId: number, body: CreateScheduledTaskRequest) =>
    request<ScheduledTask>(`/api/scheduled-tasks/${taskId}`, {
      method: 'PUT',
      body: JSON.stringify(body),
    }),
  deleteScheduledTask: (taskId: number) =>
    request<void>(`/api/scheduled-tasks/${taskId}`, { method: 'DELETE' }),
  getTaskLogs: (taskId: number, take = 50) =>
    request<TaskExecutionLog[]>(`/api/scheduled-tasks/${taskId}/logs?take=${take}`),

  // ---------- Votação (formulário público) ----------
  getPollAdmin: (clanId: number) => request<PollAdmin>(`/api/clans/${clanId}/poll`),
  resetPoll: (clanId: number) =>
    request<void>(`/api/clans/${clanId}/poll/reset`, { method: 'POST' }),
  setPollExpiration: (clanId: number, duration: PollDuration) =>
    request<{ expiresAtUtc: string }>(`/api/clans/${clanId}/poll/expiration`, {
      method: 'POST',
      body: JSON.stringify({ duration }),
    }),
  setPollWindows: (
    clanId: number,
    windows: { startDay: string; startTime: string; endDay: string; endTime: string }[],
    timeZoneId: string,
  ) =>
    request<PollWindow[]>(`/api/clans/${clanId}/poll/windows`, {
      method: 'POST',
      body: JSON.stringify({ windows, timeZoneId }),
    }),
  clearPollWindows: (clanId: number) =>
    request<void>(`/api/clans/${clanId}/poll/windows`, { method: 'DELETE' }),
  // Rotas públicas: o backend não exige X-Api-Key em /api/poll/* (o token do link é a credencial).
  getPublicPoll: (token: string, nickname: string) =>
    request<PollInfo>(`/api/poll/${token}?nickname=${encodeURIComponent(nickname)}`),
  votePoll: (token: string, questId: string, nickname: string) =>
    request<void>(`/api/poll/${token}/vote`, {
      method: 'POST',
      body: JSON.stringify({ questId, nickname }),
    }),
}

export type { AvailableQuest }
