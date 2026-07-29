import { api } from '../api/client'
import { ErrorBox, Loading, Pager, SectionTitle } from '../components/ui'
import { useAsync } from '../lib/useAsync'
import { usePaged } from '../lib/paged'
import { fmtDateTime, fmtNumber } from '../lib/format'

// Rótulos das ações mais comuns; cai no valor cru se não reconhecer.
const LEDGER_TYPES: Record<string, string> = {
  DONATE: 'Doação',
  CLAIM_QUEST: 'Missão iniciada',
  SHUFFLE_QUESTS: 'Missões embaralhadas',
  SKIP_QUEST_WAITING_TIME: 'Espera pulada',
  CLAIM_QUEST_EXTRA_TIME: 'Tempo extra',
}

const LOG_ACTIONS: Record<string, string> = {
  JOIN: 'entrou no clã',
  PLAYER_JOINED: 'entrou no clã',
  LEAVE: 'saiu do clã',
  PLAYER_LEFT: 'saiu do clã',
  KICK: 'expulsou',
  BLOCK: 'bloqueou',
  UNBLOCK: 'desbloqueou',
  PROMOTE: 'promoveu',
  DEMOTE: 'rebaixou',
  ACCEPT_INVITE: 'aceitou o convite',
  DECLINE_INVITE: 'recusou o convite',
  JOIN_REQUEST_SENT_BY_EXTERNAL_PLAYER: 'pediu para entrar',
  JOIN_REQUEST_SENT_BY_CLAN: 'convidou para o clã',
  JOIN_REQUEST_ACCEPTED: 'pedido de entrada aceito',
}

export function Activity({ clanRegId }: { clanRegId: number }) {
  const ledger = useAsync(() => api.getLedger(clanRegId), [clanRegId])
  const logs = useAsync(() => api.getLogs(clanRegId), [clanRegId])
  const ledgerPage = usePaged(ledger.data ?? [], 5)
  const logsPage = usePaged(logs.data ?? [], 5)

  return (
    <div>
      <SectionTitle>Livro-razão — ouro e gemas</SectionTitle>
      {ledger.loading ? (
        <Loading />
      ) : ledger.error ? (
        <ErrorBox message={ledger.error} onRetry={ledger.reload} />
      ) : (
        <div className="mb-9">
        <div className="list-card">
          {ledger.data!.length === 0 && (
            <div className="px-5 py-5 font-sans text-[13px] text-muted">Nenhuma movimentação registrada.</div>
          )}
          {ledgerPage.slice.map((l, i) => (
            <div
              key={l.id ?? i}
              className="flex items-center justify-between border-b border-[rgba(180,150,220,0.07)] px-5 py-3.5 last:border-b-0"
            >
              <div>
                <div className="font-sans text-[13.5px] text-ink-2">
                  <span className="font-semibold">{l.playerUsername ?? 'Desconhecido'}</span>
                  <span className="text-muted"> · {LEDGER_TYPES[l.type ?? ''] ?? l.type ?? '—'}</span>
                </div>
                <div className="mt-0.5 font-sans text-xs text-faint">{fmtDateTime(l.creationTime)}</div>
              </div>
              <div className="flex flex-none items-center gap-4 font-mono text-[12.5px]">
                {l.gold != null && l.gold !== 0 && (
                  <span className="text-gold">
                    {l.gold > 0 ? '+' : ''}
                    {fmtNumber(l.gold)} ouro
                  </span>
                )}
                {l.gems != null && l.gems !== 0 && (
                  <span className="text-violet">
                    {l.gems > 0 ? '+' : ''}
                    {fmtNumber(l.gems)} gemas
                  </span>
                )}
              </div>
            </div>
          ))}
        </div>
        <Pager page={ledgerPage.page} pages={ledgerPage.pages} onChange={ledgerPage.setPage} />
        </div>
      )}

      <SectionTitle>Log de auditoria</SectionTitle>
      {logs.loading ? (
        <Loading />
      ) : logs.error ? (
        <ErrorBox message={logs.error} onRetry={logs.reload} />
      ) : (
        <div>
        <div className="list-card">
          {logs.data!.length === 0 && (
            <div className="px-5 py-5 font-sans text-[13px] text-muted">Nenhum registro de auditoria.</div>
          )}
          {logsPage.slice.map((l, i) => (
            <div
              key={i}
              className="flex items-center justify-between border-b border-[rgba(180,150,220,0.07)] px-5 py-3.5 last:border-b-0"
            >
              <div className="font-sans text-[13.5px] text-ink-2">
                <span className="font-semibold">{l.playerUsername ?? 'Alguém'}</span>{' '}
                <span className="text-muted">{LOG_ACTIONS[l.action ?? ''] ?? l.action ?? '—'}</span>
                {l.targetPlayerUsername && <span className="font-semibold"> {l.targetPlayerUsername}</span>}
              </div>
              <div className="flex-none font-mono text-[11px] text-faint">{fmtDateTime(l.creationTime)}</div>
            </div>
          ))}
        </div>
        <Pager page={logsPage.page} pages={logsPage.pages} onChange={logsPage.setPage} />
        </div>
      )}
    </div>
  )
}
