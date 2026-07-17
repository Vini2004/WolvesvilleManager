// Tipos espelhando os DTOs do backend (camelCase via System.Text.Json).

export interface RegisteredClan {
  id: number
  clanId: string
  clanName: string
  clanTag: string | null
  createdAtUtc: string
}

export interface ClanInfo {
  id: string
  name: string
  tag: string | null
  description: string | null
  xp: number
  memberCount: number
  minLevel: number
  gold: number | null
  gems: number | null
  iconColor: string | null
}

export interface QuestReward {
  type: string | null
  amount: number
}

export interface ClanQuest {
  id: string
  promoImageUrl: string | null
  purchasableWithGems: boolean
  rewards: QuestReward[]
}

export interface QuestParticipant {
  playerId: string | null
  username: string | null
  xp: number
}

export interface ActiveQuest {
  quest: ClanQuest
  tier: number
  xp: number
  tierStartTime: string | null
  tierEndTime: string | null
  tierFinished: boolean
  xpPerReward: number
  claimedTime: boolean
  participants: QuestParticipant[]
}

export interface AvailableQuest {
  quest: ClanQuest
  votes: number
}

export interface QuestsOverview {
  active: ActiveQuest | null
  available: AvailableQuest[]
  gold: number | null
  gems: number | null
}

export interface QuestHistoryEntry {
  quest: ClanQuest
  participants: QuestParticipant[]
  xp: number
  tier: number
  tierEndTime: string | null
}

export interface ClanMember {
  playerId: string
  username: string
  level: number
  xp: number
  status: string | null
  playerStatus: string | null
  isCoLeader: boolean
  isLeader: boolean
  lastOnline: string | null
  participateInClanQuests: boolean | null
}

export interface BlocklistEntry {
  playerId: string | null
  playerUsername: string | null
  creationTime: string | null
}

export type ScheduledTaskType =
  | 'ClaimMostVotedQuest'
  | 'ClaimMostVotedFormQuest'
  | 'ClaimSpecificQuest'
  | 'SkipQuestWaitingTime'
  | 'ClaimQuestExtraTime'

export type TaskExecutionOutcome = 'Success' | 'Skipped' | 'Failed'

// Mesmo valor de WolvesvilleManager.Domain.Entities.QuestPollVote.ShuffleOptionId: não é uma
// missão de verdade, é mais uma cédula na urna — se vencer, a automação embaralha em vez de
// reivindicar uma missão.
export const SHUFFLE_OPTION_ID = '__shuffle__'

/** Missão candidata no formulário público de votação (ou o card de embaralhar). */
export interface PollQuest {
  questId: string
  name: string
  imageUrl: string | null
  gems: boolean
  votes: number
}

/** Página pública: candidatas + o voto deste navegador + o prazo. */
export interface PollInfo {
  clanName: string
  clanTag: string | null
  quests: PollQuest[]
  votedQuestId: string | null
  expiresAtUtc: string | null
  isClosed: boolean
}

/** Uma rodada de votação já decidida (a automação já iniciou a missão ou embaralhou). */
export interface PollHistoryEntry {
  questName: string
  votes: number
  wasShuffle: boolean
  decidedAtUtc: string
}

/** Um voto individual da urna atual — só a aba admin vê. */
export interface PollVoter {
  nickname: string
  questId: string
  questName: string
  wasShuffle: boolean
}

/** Aba admin: link, apuração, votantes, prazo e histórico de rodadas anteriores. */
export interface PollAdmin {
  token: string
  quests: PollQuest[]
  totalVotes: number
  expiresAtUtc: string | null
  isClosed: boolean
  history: PollHistoryEntry[]
  closeCronExpression: string | null
  closeTimeZoneId: string | null
  voters: PollVoter[]
}

/** Durações de prazo oferecidas na aba admin — mesmos valores do enum PollDuration do backend. */
export type PollDuration = 'SixHours' | 'TwelveHours' | 'OneDay' | 'ThreeDays' | 'SevenDays'

export const POLL_DURATION_LABELS: Record<PollDuration, string> = {
  SixHours: '6 horas',
  TwelveHours: '12 horas',
  OneDay: '1 dia',
  ThreeDays: '3 dias',
  SevenDays: '7 dias',
}

export interface ScheduledTask {
  id: number
  clanRegistrationId: number
  type: ScheduledTaskType
  cronExpression: string
  timeZoneId: string
  enabled: boolean
  minVotes: number
  targetQuestId: string | null
  targetQuestName: string | null
  targetQuestPromoImageUrl: string | null
  nextRunAtUtc: string | null
  lastRunAtUtc: string | null
  createdAtUtc: string
}

export interface CreateScheduledTaskRequest {
  type: ScheduledTaskType
  cronExpression: string
  timeZoneId: string
  minVotes: number
  targetQuestId?: string | null
  targetQuestName?: string | null
  targetQuestPromoImageUrl?: string | null
  enabled: boolean
}

export interface TaskExecutionLog {
  id: number
  ranAtUtc: string
  outcome: TaskExecutionOutcome
  message: string | null
}

export interface XpReportEntry {
  playerId: string
  username: string
  currentXp: number
  baselineXp: number | null
  gainedXp: number | null
}

export interface XpReport {
  sinceUtc: string | null
  entries: XpReportEntry[]
}

export interface ClanAnnouncement {
  id: string | null
  content: string | null
  author: string | null
  timestamp: string | null
}

export interface ChatMessage {
  playerId: string | null
  playerBotId: string | null
  playerBotOwnerUsername: string | null
  msg: string | null
  isSystem: boolean
  date: string | null
}

export interface LedgerEntry {
  id: string | null
  playerId: string | null
  playerUsername: string | null
  gold: number | null
  gems: number | null
  type: string | null
  creationTime: string | null
}

export interface ClanLogEntry {
  action: string | null
  playerUsername: string | null
  targetPlayerUsername: string | null
  creationTime: string | null
}

// Campos numéricos com -1 = escondidos pela privacidade do jogador.
export interface PlayerProfile {
  id: string
  username: string
  personalMessage: string | null
  level: number | null
  status: string | null
  clanId: string | null
  lastOnline: string | null
  receivedRosesCount: number | null
  profileIconColor: string | null
  gameStats: {
    totalWinCount: number | null
    totalLoseCount: number | null
    totalTieCount: number | null
    totalPlayTimeInMinutes: number | null
  } | null
}

export interface RoleRotationEntry {
  gameMode: string | null
  // Cru da API: [{ roleRotation: { roles: [[{ probability, role }]] }, probability }]
  roleRotations: { roleRotation: { roles: { probability: number; role: string }[][] } }[] | null
}

export interface BattlePassSeason {
  number: number | null
  startTime: string | null
  durationInDays: number | null
  xpPerReward: number | null
}

export interface ShopOffer {
  type: string | null
  costInGems: number | null
  promoImageUrl: string | null
  expireDate: string | null
}

export interface GameAnnouncement {
  content: string | null
  timestamp: string | null
  author: { username: string | null } | null
  attachments: { filename: string | null; url: string | null }[] | null
}
