import type { ReactNode } from 'react'
import { SectionTitle } from '../components/ui'

function Topic({ title, children }: { title: string; children: ReactNode }) {
  return (
    <div className="list-card px-5 py-4">
      <div className="font-sans text-[14px] font-semibold text-ink-2">{title}</div>
      <div className="mt-1.5 font-sans text-[13px] leading-relaxed text-muted">{children}</div>
    </div>
  )
}

/** Guia de uso estático — explica cada aba e as duas peças mais não-óbvias do app (votação e tema). */
export function Guide() {
  return (
    <div className="flex flex-col gap-9">
      <div className="card px-7 py-[26px]">
        <div className="mb-2 font-serif text-xl font-semibold text-ink">Como funciona o Wolvesville Manager</div>
        <div className="font-sans text-[13.5px] leading-relaxed text-muted">
          O app gerencia o clã pela API oficial do Wolvesville, usando a chave de um bot autorizado
          (aba <span className="font-bold text-lav">Registrar clã</span>). Cada ação aqui — iniciar
          missão, pular espera, publicar anúncio — é feita de verdade dentro do jogo, então trate as
          automações com cuidado: elas gastam ouro e gemas do clã.
        </div>
      </div>

      <div>
        <SectionTitle>Telas do painel</SectionTitle>
        <div className="flex flex-col gap-2.5">
          <Topic title="Visão geral">
            Ouro, gemas e o progresso da missão ativa do clã, tudo num relance.
          </Topic>
          <Topic title="Missões">
            Missões disponíveis (com votos de dentro do jogo), botão de embaralhar e o histórico de
            conclusões.
          </Topic>
          <Topic title="Membros">
            Nível, XP no clã, ganho de XP no período e ações (participação em missão, kick, bloqueio).
            Ordenados por líder → vice-líderes → o resto por XP.
          </Topic>
          <Topic title="Anúncios">Publica um aviso para todo o clã, pelo bot.</Topic>
          <Topic title="Chat">Lê e envia mensagens no chat do clã pelo bot.</Topic>
          <Topic title="Atividade">Livro-razão de ouro/gemas por membro e o log de auditoria do clã.</Topic>
          <Topic title="Jogo">
            Resgate do chapéu exclusivo da API, edição de flair, busca de jogadores, battle pass, loja
            e rotações de papéis.
          </Topic>
        </div>
      </div>

      <div>
        <SectionTitle>Automações</SectionTitle>
        <div className="flex flex-col gap-2.5">
          <Topic title="Como criar">
            Na aba Automações, defina um horário (campos guiados ou expressão cron avançada) e o que
            deve acontecer: iniciar a missão mais votada (por dentro do jogo ou pelo formulário
            público), iniciar uma missão específica, pular o tempo de espera ou resgatar tempo extra.
          </Topic>
          <Topic title="Pular espera com retentativa">
            Ela só age quando o XP do tier já bateu o objetivo. Para tentar de novo se ainda não bateu,
            coloque mais de um horário no cron do mesmo dia (ex.: <span className="font-mono text-lav">0 18,20,22 * * SEG-SEX</span>)
            — a automação garante no máximo 1 pulo por dia, então as tentativas seguintes não arriscam
            pular a espera do tier errado.
          </Topic>
          <Topic title="Cada automação acorda o app sozinha">
            O app fica "dormindo" na maior parte do tempo (hospedagem gratuita); cada automação cria um
            gatilho externo que acorda o app e o banco só no horário certo, então não precisa deixar
            nenhuma aba aberta para elas funcionarem.
          </Topic>
        </div>
      </div>

      <div>
        <SectionTitle>Votação pública</SectionTitle>
        <div className="flex flex-col gap-2.5">
          <Topic title="O que é">
            Na aba <span className="font-bold text-lav">Votação</span>, o app gera um link
            (<span className="font-mono text-lav">/votar/…</span>) que você compartilha com o clã fora
            do app — Discord, WhatsApp, anúncio no jogo. Quem abrir o link vê só o formulário, sem
            precisar de login, e vota uma vez por navegador.
          </Topic>
          <Topic title="Embaralhar também é voto">
            A opção "🔀 Embaralhar missões" concorre como mais uma candidata na mesma urna. Se ela
            vencer a apuração, a automação embaralha as missões em vez de iniciar uma — para quando
            ninguém no clã gosta das opções atuais.
          </Topic>
          <Topic title="Prazo obrigatório">
            Toda votação tem um prazo — nunca fica aberta para sempre. Depois que o prazo passa, o
            formulário continua visível mas para de aceitar votos novos, até você definir um prazo
            novo na aba Votação.
          </Topic>
          <Topic title="Aplicar o resultado">
            Crie uma automação do tipo "Iniciar mais votada do formulário" para, no horário combinado,
            iniciar a missão vencedora (ou embaralhar, se for o caso) e zerar a urna automaticamente.
          </Topic>
        </div>
      </div>

      <div>
        <SectionTitle>Tema Hogwarts</SectionTitle>
        <Topic title="Trocar o visual">
          O ícone de faísca no topo da tela abre o seletor de tema. Escolhendo Hogwarts, dá para
          também escolher uma casa — a cor de destaque do app muda para a cor da casa escolhida. Fica
          salvo no navegador, então cada pessoa pode ter o tema que preferir.
        </Topic>
      </div>
    </div>
  )
}
