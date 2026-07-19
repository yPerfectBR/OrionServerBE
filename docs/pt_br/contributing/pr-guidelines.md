# Diretrizes de PR

Pull requests são bem-vindos, incluindo código assistido por IA, mas precisam ser fáceis de revisar.

## Branch e Origem

- Abra PRs a partir de fork da branch `development`.
- A branch `main` é reservada para releases oficiais.

## Regras de Escopo

- Mantenha o PR focado em um único assunto.
- Se as mudanças afetarem partes sem relação, divida em PRs separados.

## Qualidade dos Commits

- Prefira commits pequenos e objetivos.
- Use nomes de commit claros e orientados à intenção.
- Divida features grandes em incrementos lógicos.

## Requisitos por Tipo de Mudança

- **Correção de bug**: explique reprodução e como a correção resolve o problema.
- **Mudança estrutural/profunda**: detalhe o motivo e o impacto no projeto.
- **Mudança de performance**: traga benchmark antes/depois.
- **Mudança de comportamento**: inclua testes quando possível.

Mesmo com bom código, PRs podem ser recusados se escopo, organização ou legibilidade de revisão não forem atendidos.
