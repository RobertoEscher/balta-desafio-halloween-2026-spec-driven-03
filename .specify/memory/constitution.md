<!--
Relatório de Impacto da Sincronização
Alteração de versão: 1.0.0 -> 1.0.1 (tradução editorial para português do Brasil)
Princípios modificados:
- I. Separation of API and Application Responsibilities -> I. Separação entre API e Aplicação
- II. Provider-Independent Persistence -> II. Persistência Independente de Provedor
- III. Business-Rule Tests and Validation Responses -> III. Testes de Regras de Negócio e Respostas de Validação
- IV. Justified Dependencies -> IV. Dependências Justificadas
- V. Plans and Requirement-Traceable Commits -> V. Planos e Commits Vinculados a Requisitos
Seções adicionadas: Nenhuma
Seções removidas: Nenhuma
TODO pendente: Confirmar a data original de ratificação.
-->

# Constituição da API Encurtador

## Princípios Fundamentais

### I. Separação entre API e Aplicação
A API utiliza .NET 10, C# e Minimal APIs. Os endpoints DEVEM chamar serviços de aplicação
e NÃO DEVEM acessar o banco de dados diretamente. As regras de negócio DEVEM residir nos
serviços de aplicação ou em componentes de domínio, nunca nos endpoints ou handlers HTTP.
Essa separação mantém as responsabilidades de transporte independentes do comportamento
da aplicação.

### II. Persistência Independente de Provedor
A persistência DEVE utilizar EF Core, e o provedor de banco de dados DEVE ser substituível
sem alterar as regras de negócio ou o comportamento dos endpoints. Os detalhes específicos
de persistência DEVEM permanecer atrás dos limites de persistência da aplicação.

### III. Testes de Regras de Negócio e Respostas de Validação
Toda regra de negócio DEVE ter testes unitários implementados com xUnit. Os erros de
validação retornados pela API DEVEM usar HTTP 400 e o formato ProblemDetails. Esses
requisitos tornam o comportamento de negócio verificável e oferecem aos clientes um
contrato de erro consistente.

### IV. Dependências Justificadas
Nenhuma dependência nova pode ser introduzida sem que sua justificativa esteja documentada
no plano de implementação. O plano DEVE identificar a dependência e explicar por que os
recursos existentes no projeto são insuficientes.

### V. Planos e Commits Vinculados a Requisitos
Os planos de implementação DEVEM estar em conformidade com esta constituição. Se um plano
entrar em conflito com um princípio, o plano DEVE ser alterado antes do início da
implementação. Cada commit DEVE seguir o formato `tipo(escopo): descrição` e incluir o
identificador do requisito que implementa, por exemplo:
`feat(api): criar redirecionamento (REQ-001)`.

## Restrições Técnicas

A aplicação utiliza .NET 10, C# e Minimal APIs. O acesso a dados utiliza EF Core com
provedor substituível. Erros de validação da API usam HTTP 400 com ProblemDetails.

## Fluxo de Desenvolvimento

Antes da implementação, o plano DEVE ser revisado para verificar sua conformidade com esta
constituição, incluindo as justificativas de dependências. Durante a revisão, DEVEM ser
verificados os limites entre endpoints e serviços, a localização das regras de negócio,
a independência do provedor, a cobertura xUnit de cada regra de negócio e o contrato de
erros de validação. Quando houver conflito, o plano DEVE ser ajustado em vez de flexibilizar
um requisito constitucional.

## Governança

Esta constituição é o conjunto oficial de princípios do projeto. As alterações DEVEM
atualizar este documento e seus metadados de versão. O versionamento segue o versionamento
semântico: MAJOR para remoções ou redefinições incompatíveis de princípios, MINOR para
novos princípios ou expansão material de requisitos e PATCH para esclarecimentos ou
alterações de redação sem impacto semântico. As revisões DEVEM verificar se os planos e a
implementação estão em conformidade com a constituição vigente; qualquer conflito DEVE ser
resolvido atualizando o plano antes da implementação.

**Versão**: 1.0.1 | **Ratificada**: TODO(RATIFICATION_DATE): confirmar a data original de adoção | **Última alteração**: 2026-10-17
