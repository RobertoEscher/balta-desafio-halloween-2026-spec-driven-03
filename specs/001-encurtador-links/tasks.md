---
description: "Tarefas de implementação para o encurtador de links"
---

# Tarefas: Encurtador de Links

**Entrada**: Documentos de design em `/specs/001-encurtador-links/`

**Pré-requisitos**: `plan.md`, `spec.md`, `research.md`, `data-model.md` e `contracts/`

**Testes**: Tarefas xUnit incluídas porque a constituição exige testes para as regras de negócio.

**Organização**: Tarefas agrupadas por história de usuário para permitir implementação e validação incremental.

## Formato: `[ID] [P?] [Story?] Descrição`

- **[P]**: Pode ser executada em paralelo, em arquivos separados e sem dependências pendentes.
- **[Story]**: História atendida (`US1`, `US2`, `US3`).
- Cada tarefa indica o caminho exato do arquivo de código, teste ou configuração afetado.

## Fase 1: Setup

**Objetivo**: Criar a solução .NET e os projetos previstos no plano.

- [X] T001 Criar a solução `Encurtador.sln` e as pastas de projetos `src/Encurtador.Api`, `src/Encurtador.Application`, `src/Encurtador.Infrastructure`, `tests/Encurtador.UnitTests` e `tests/Encurtador.IntegrationTests`.
- [X] T002 [P] Criar o projeto ASP.NET Core .NET 10 e o arquivo `src/Encurtador.Api/Encurtador.Api.csproj`.
- [X] T003 [P] Criar o projeto de biblioteca .NET 10 e o arquivo `src/Encurtador.Application/Encurtador.Application.csproj`.
- [X] T004 [P] Criar o projeto de biblioteca .NET 10 e o arquivo `src/Encurtador.Infrastructure/Encurtador.Infrastructure.csproj`.
- [X] T005 [P] Criar o projeto xUnit e o arquivo `tests/Encurtador.UnitTests/Encurtador.UnitTests.csproj`.
- [X] T006 [P] Criar o projeto xUnit e o arquivo `tests/Encurtador.IntegrationTests/Encurtador.IntegrationTests.csproj`.
- [X] T007 Adicionar os cinco projetos a `Encurtador.sln` e configurar referências API→Application, API→Infrastructure, Infrastructure→Application, UnitTests→Application e IntegrationTests→Api/Infrastructure nos respectivos arquivos `.csproj`.

## Fase 2: Fundacional

**Objetivo**: Criar a persistência durável e os limites de camadas que todas as histórias usarão.

**Bloqueio**: Esta fase deve estar concluída antes das histórias de usuário.

- [X] T008 Implementar a entidade persistente `Link` com `Code`, `DestinationUrl` e `ClickCount` no arquivo `src/Encurtador.Application/Links/Link.cs`.
- [X] T009 Definir contratos de persistência agnósticos do provedor para criar links, consultar código, obter contagem e incrementar acessos no arquivo `src/Encurtador.Application/Links/ILinkRepository.cs`.
- [X] T010 Criar o `LinkDbContext`, o mapeamento da entidade `Link` e o índice único case-sensitive para `Code` no arquivo `src/Encurtador.Infrastructure/Persistence/LinkDbContext.cs`.
- [X] T011 Implementar configuração SQLite por connection string durável e registro de `LinkDbContext`/repositório em `src/Encurtador.Infrastructure/DependencyInjection.cs`.
- [X] T012 Implementar `LinkRepository` com EF Core e classificar somente violações do índice único nomeado de `Code` como colisão no arquivo `src/Encurtador.Infrastructure/Persistence/LinkRepository.cs`.
- [X] T013 Criar a migração inicial com tabela `Links`, chave primária `Id`, índice único case-sensitive em `Code` e `ClickCount` inicializado em zero nos arquivos gerados em `src/Encurtador.Infrastructure/Persistence/Migrations/`.
- [X] T014 Configurar ProblemDetails para erros de validação HTTP 400 e registro de serviços/camadas no arquivo `src/Encurtador.Api/Program.cs`.
- [X] T015 Configurar host de teste e banco SQLite temporário isolado em `tests/Encurtador.IntegrationTests/Links/LinkApiFactory.cs`.

**Checkpoint**: Solução compila, schema SQLite é aplicável e projetos de teste conseguem iniciar a API contra armazenamento isolado.

## Fase 3: História de Usuário 1 - Criar um link curto (Prioridade: P1)

**Objetivo**: Criar e persistir um link com código automático ou escolhido, validando entrada e colisões.

**Teste independente**: Submeter URL válida sem código e confirmar código gerado de 7 caracteres; submeter outra URL com código personalizado válido e confirmar o código escolhido. Rejeitar URLs fora das regras, códigos personalizados inválidos e colisões sem alterar dados existentes.

### Testes da História de Usuário 1

- [X] T016 [P] [US1] Testar validação de URL absoluta HTTP/HTTPS, esquema permitido e limites inclusivos de 2048 caracteres em `tests/Encurtador.UnitTests/Links/LinkServiceTests.cs`.
- [X] T017 [P] [US1] Testar geração com o alfabeto `23456789ABCDEFGHJKMNPQRSTVWXYZ` e tamanho exatamente 7 em `tests/Encurtador.UnitTests/Links/LinkCodeGeneratorTests.cs`.
- [X] T018 [P] [US1] Testar contrato HTTP de criação para código gerado, código personalizado case-sensitive, validação 400 ProblemDetails e conflito 409 em `tests/Encurtador.IntegrationTests/Links/CreateLinkEndpointTests.cs`.
- [X] T019 [P] [US1] Testar colisão no índice único SQLite por inserções independentes: código duplicado não substitui registro existente em `tests/Encurtador.IntegrationTests/Links/LinkUniquenessTests.cs`.

### Implementação da História de Usuário 1

- [X] T020 [P] [US1] Implementar geração criptograficamente segura de 7 caracteres usando exclusivamente `23456789ABCDEFGHJKMNPQRSTVWXYZ` em `src/Encurtador.Application/Links/LinkCodeGenerator.cs`.
- [X] T021 [US1] Implementar em `src/Encurtador.Application/Links/LinkService.cs` a validação de destino HTTP/HTTPS com até 2048 caracteres e código personalizado ASCII alfanumérico de 4–20 caracteres, inclusive.
- [X] T022 [US1] Implementar criação em `LinkService` com checagem antecipada, tentativa de até cinco códigos gerados, tradução de colisão personalizada para conflito e persistência antes de retornar sucesso em `src/Encurtador.Application/Links/LinkService.cs`.
- [X] T023 [US1] Implementar o endpoint `POST /api/links`, os modelos de requisição/resposta e a composição do link com `PublicBaseUrl` configurado em `src/Encurtador.Api/Endpoints/LinksEndpoints.cs`.
- [X] T024 [US1] Mapear resultados de criação para `201 Created`, entrada inválida para `400 ProblemDetails`, código personalizado existente para `409 ProblemDetails` e esgotamento de cinco tentativas geradas para `503 ProblemDetails` em `src/Encurtador.Api/Endpoints/LinksEndpoints.cs`.

**Checkpoint**: Criação independente testável pela API; links e códigos sobrevivem ao reinício do host usando o mesmo arquivo SQLite.

## Fase 4: História de Usuário 2 - Abrir um link curto (Prioridade: P1)

**Objetivo**: Redirecionar códigos existentes, contar cada redirecionamento e responder 404 para código desconhecido.

**Teste independente**: Preparar um registro no banco SQLite, abrir seu código e validar destino, resposta 302 e incremento exato; repetir solicitações concorrentes e confirmar que nenhum incremento se perde.

### Testes da História de Usuário 2

- [X] T025 [P] [US2] Testar incremento atômico por update no repositório e ausência de incremento para código inexistente em `tests/Encurtador.IntegrationTests/Links/AtomicClickCountTests.cs`.
- [X] T026 [P] [US2] Testar endpoint de redirecionamento para código existente/inexistente, status 302, Location, Cache-Control no-store e contagem atualizada em `tests/Encurtador.IntegrationTests/Links/RedirectEndpointTests.cs`.
- [X] T027 [P] [US2] Testar múltiplos redirecionamentos concorrentes e reinício do host com preservação do contador em `tests/Encurtador.IntegrationTests/Links/RedirectPersistenceTests.cs`.

### Implementação da História de Usuário 2

- [X] T028 [US2] Implementar incremento de `ClickCount` com uma operação EF Core `ExecuteUpdate` equivalente a `ClickCount = ClickCount + 1`, sem leitura/regravação do contador, no arquivo `src/Encurtador.Infrastructure/Persistence/LinkRepository.cs`.
- [X] T029 [US2] Implementar em `LinkService` a resolução do destino e o resultado não encontrado sem carregar nem salvar uma contagem obsoleta em `src/Encurtador.Application/Links/LinkService.cs`.
- [X] T030 [US2] Mapear `GET /{code}` com redirecionamento 302 não permanente, `Cache-Control: no-store` e 404 quando o código não existir em `src/Encurtador.Api/Endpoints/LinksEndpoints.cs`.

**Checkpoint**: Redirecionamentos contam uma unidade por solicitação bem-sucedida, não perdem incrementos simultâneos e não alteram a contagem de códigos desconhecidos.

## Fase 5: História de Usuário 3 - Consultar acessos (Prioridade: P2)

**Objetivo**: Expor a contagem atual por código sem autenticação ou alteração do contador.

**Teste independente**: Consultar link preparado no banco com zero e com acessos acumulados; verificar valor exato, ausência de incremento pela consulta e 404 para código inexistente.

### Testes da História de Usuário 3

- [X] T031 [P] [US3] Testar consulta de contagem zero, valor acumulado, código inexistente e integração do incremento no serviço em `tests/Encurtador.UnitTests/Links/LinkRedirectServiceTests.cs`.
- [X] T032 [P] [US3] Testar contrato HTTP público `GET /api/links/{code}/stats`, payload de contagem e resposta 404 em `tests/Encurtador.IntegrationTests/Links/LinkStatsEndpointTests.cs`.

### Implementação da História de Usuário 3

- [X] T033 [US3] Implementar consulta somente leitura da contagem atual no método de estatísticas de `src/Encurtador.Application/Links/LinkService.cs`.
- [X] T034 [US3] Mapear `GET /api/links/{code}/stats` sem chave de gerenciamento, retornar `code` e `clickCount`, e responder 404 para código desconhecido em `src/Encurtador.Api/Endpoints/LinksEndpoints.cs`.

**Checkpoint**: Contagem pública reflete acessos previamente persistidos e consultar estatísticas não incrementa nem reduz o valor.

## Fase 6: Polish e preocupações transversais

**Objetivo**: Validar o fluxo completo, a persistência operacional e a documentação do serviço.

- [X] T035 Atualizar configuração de exemplo com connection string SQLite durável e `PublicBaseUrl` obrigatório em `src/Encurtador.Api/appsettings.json`.
- [X] T036 [P] Documentar rotas, exemplos de requisição/resposta e erros ProblemDetails em `README.md`.
- [X] T037 Executar `dotnet test Encurtador.sln` e resolver falhas nos projetos de produção/teste da solução `Encurtador.sln`.
- [X] T038 Validar os cenários de ponta a ponta do guia `specs/001-encurtador-links/quickstart.md`, incluindo aplicação de migrações antes da inicialização.

## Dependências e Ordem de Execução

### Dependências entre fases

- **Setup (Fase 1)**: sem dependências; T002–T006 podem ser paralelas após T001.
- **Fundacional (Fase 2)**: depende do Setup; bloqueia todas as histórias. T008–T010 estabelecem entidade e contrato. T011–T015 dependem dos projetos e contratos anteriores; concluir as migrações antes dos testes de integração.
- **US1 (Fase 3)**: depende do Fundacional; não depende de outras histórias.
- **US2 (Fase 4)**: depende do Fundacional. Os testes podem usar registros preparados no SQLite, sem exigir o endpoint de criação da US1.
- **US3 (Fase 5)**: depende do Fundacional. Os testes podem usar registros preparados no SQLite, sem exigir os endpoints das outras histórias.
- **Polish (Fase 6)**: T035–T036 requerem a configuração/API estabilizada; T037–T038 dependem das histórias que se deseja validar.

### Dependências entre histórias

- **US1 (P1)**: pode começar após a Fase 2; nenhuma dependência de história.
- **US2 (P1)**: pode começar após a Fase 2; usa fixture com dados preparados para teste independente.
- **US3 (P2)**: pode começar após a Fase 2; usa fixture com dados preparados para teste independente.
- Para uma demonstração completa do fluxo da pessoa usuária, criar primeiro o link via US1 e então exercitar US2/US3; essa sequência de demonstração não é dependência de implementação.

### Oportunidades de paralelismo

- Após T001, criação dos arquivos `.csproj` T002–T006 pode ocorrer em paralelo; T007 aguarda todos.
- Após contratos/modelos da Fase 2, testes unitários e de integração da US1 T016–T019 podem ser preparados em paralelo, embora testes de integração só executem após a migração.
- Na US2, T025–T027 são arquivos de teste independentes; na implementação, T028 precede o uso do incremento no serviço/endpoint.
- Na US3, T031 e T032 podem ser implementadas em paralelo; T033 precede T034.
- Depois da Fase 2, US1 e US2 podem ser desenvolvidas em paralelo em arquivos de endpoint/teste distintos; US3 pode paralelizar sua cobertura com essas histórias.

## Exemplo de Paralelismo: História de Usuário 1

Depois que a solução, os contratos necessários e a migração inicial estiverem disponíveis, tarefas abaixo trabalham em arquivos distintos:

```text
T016: regras e validação de LinkServiceTests.cs
T017: geração de código em LinkCodeGeneratorTests.cs
T018: contrato de criação em CreateLinkEndpointTests.cs
T019: colisões concorrentes em LinkUniquenessTests.cs
```

## Estratégia de Implementação

### MVP primeiro (US1)

1. Concluir Setup e Fundacional para garantir solução executável e persistência durável.
2. Concluir US1 para criar códigos gerados/personalizados com validação e unicidade garantida no banco.
3. Parar e validar a história de criação independentemente, incluindo persistência após reiniciar a aplicação.

### Entrega incremental

1. Setup + Fundacional: base executável e banco migrável.
2. US1: criar links e receber links curtos.
3. US2: adicionar redirecionamentos com incrementos atômicos e duráveis.
4. US3: disponibilizar a consulta de estatísticas.
5. Polish: documentação e validação integral pelo quickstart.

## Notas

- `[P]` indica arquivos diferentes e ausência de dependência sobre tarefa incompleta.
- Todos os requisitos de negócio devem ter testes xUnit; executar os testes antes de concluir cada checkpoint.
- A especificação permite códigos personalizados ASCII alfanuméricos; o alfabeto sem caracteres visualmente ambíguos aplica-se apenas à geração automática.
- O banco é SQLite em arquivo durável; migrações são aplicadas antes de servir tráfego.
- Não implementar chave de gerenciamento, login, limites de frequência nem CAPTCHA nesta entrega.
