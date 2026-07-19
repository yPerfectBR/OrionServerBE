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

Um diferencial do OrionServer é suportar execução de cargas do mundo em workers dedicados, com distribuição definida pelo projeto, para melhorar throughput em CPUs modernas.

Objetivos amplos:

- execução de mundo com consciência de multithreading
- padrões async/jobs amigáveis para plugins
- menor contenção em cenários pesados no mesmo mundo
