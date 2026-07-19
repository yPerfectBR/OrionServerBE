# Filosofia e Arquitetura

## DIY por Design

No OrionServer, DIY significa que o servidor é uma fundação, não um pacote completo de mecânicas prontas. Você adiciona apenas o que precisa.

## Fronteira do Core

O core é esperado para cobrir:

- ciclo de vida de jogador e sessão
- fluxo de renderização/visualização de chunks
- comportamento essencial de rede/runtime

Todo o restante (mobs, comandos completos vanilla, mecânicas completas) é opcional por decisão de projeto.

## Direção de Multithreading

Um diferencial do OrionServer é executar cargas do mundo em workers dedicados, com distribuição definida pelo projeto, para melhorar throughput em CPUs modernas.

Modelo atual (area threading):

- o mundo é particionado em **threading areas** (`AreaShard`);
- cada área anexada é simulada em um **AreaWorker**;
- jogadores/entidades migram de worker ao cruzar bordas **sem** teleport forçado no cliente;
- streaming de chunks do jogador permanece no **SessionWorker**.

Documentação do fluxo: [area-threading.md](area-threading.md).

Objetivos amplos:

- execução de mundo com consciência de multithreading
- padrões async/jobs amigáveis para plugins
- menor contenção em cenários pesados no mesmo mundo
