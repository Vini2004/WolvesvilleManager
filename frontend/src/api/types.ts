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
  | 'SkipQuestWaitingTime'
  | 'ClaimQuestExtraTime'

export type TaskExecutionOutcome = 'Success' | 'Skipped' | 'Failed'

export interface ScheduledTask {
  id: number
  clanRegistrationId: number
  type: ScheduledTaskType
  cronExpression: string
  timeZoneId: string
  enabled: boolean
  minVotes: number
  nextRunAtUtc: string | null
  lastRunAtUtc: string | null
  createdAtUtc: string
}

export interface CreateScheduledTaskRequest {
  type: ScheduledTaskType
  cronExpression: string
  timeZoneId: string
  minVotes: number
  enabled: boolean
}

export interface TaskExecutionLog {
  id: number
  ranAtUtc: string
  outcome: TaskExecutionOutcome
  message: string | null
}
