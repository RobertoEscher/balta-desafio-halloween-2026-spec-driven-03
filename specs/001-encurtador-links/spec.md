# Especificação da Funcionalidade: Encurtador de Links

**Feature Branch**: `[001-encurtador-links]`

**Criado em**: 2026-10-17

**Status**: Draft

**Entrada**: Descrição do usuário: "Um encurtador de links. O criador informa uma URL longa e recebe um link curto para compartilhar. Quem clica no link curto é redirecionado ao destino, e cada acesso é contado. O criador consegue consultar quantos acessos o link dele recebeu. O código pode ser gerado automaticamente com 7 caracteres alfanuméricos ou escolhido pelo criador, com 4 a 20 caracteres. O código é único em todo o sistema; se um código escolhido já existir, a criação é recusada. A URL de destino precisa ser http ou https e ter no máximo 2048 caracteres. O contador de acessos nunca diminui; os links continuam válidos depois que a aplicação reinicia. Fora de escopo: cadastro e login de usuários, domínio personalizado, relatório geográfico, edição do destino de um link já criado."

## Clarificações

### Sessão 2026-10-17

- Q: Quais caracteres o criador pode usar em um código personalizado? → A: Apenas letras ASCII maiúsculas e minúsculas e dígitos (0–9).
- Q: Como o serviço deve reduzir criações abusivas de links se não haverá login? → A: Não aplicar controle específico no escopo inicial.

## Cenários de Usuário e Testes

### História de Usuário 1 - Criar um link curto (Prioridade: P1)

Uma pessoa informa uma URL de destino válida e recebe um link curto que pode compartilhar.

**Por que esta prioridade**: Criar um link curto é a capacidade central do serviço e permite que os demais fluxos entreguem valor.

**Teste independente**: Enviar uma URL válida sem código escolhido e verificar a geração automática de um código alfanumérico de 7 caracteres; repetir informando um código personalizado válido e verificar que ele é usado no link retornado.

**Cenários de aceitação**:

1. **Dado** que uma pessoa informa uma URL HTTP ou HTTPS válida com até 2048 caracteres sem escolher um código, **quando** solicita a criação, **então** recebe um link curto com código gerado automaticamente de exatamente 7 caracteres alfanuméricos.
2. **Dado** que uma pessoa informa uma URL válida e escolhe um código que atende às regras de comprimento e caracteres, **quando** solicita a criação, **então** recebe um link curto com o código escolhido.
3. **Dado** que o código escolhido já está associado a outro link, **quando** a pessoa solicita a criação, **então** o sistema recusa a criação e não altera o link existente.
4. **Dado** que a pessoa escolhe um código com menos de 4 ou mais de 20 caracteres, ou com caracteres que não sejam letras ASCII ou dígitos, **quando** solicita a criação, **então** o sistema rejeita a solicitação e não cria um link.
5. **Dado** que o código gerado automaticamente já está associado a outro link, **quando** o serviço tenta criar o link, **então** gera outro código e não substitui nem associa o código duplicado.
6. **Dado** que a URL informada não usa HTTP ou HTTPS, **quando** a pessoa solicita a criação, **então** o serviço rejeita a solicitação e não cria um link.
7. **Dado** que a URL informada tem mais de 2048 caracteres, **quando** a pessoa solicita a criação, **então** o serviço rejeita a solicitação e não cria um link.
8. **Dado** que um link curto foi criado com sucesso, **quando** a aplicação reinicia, **então** o link continua disponível para redirecionamento.

### História de Usuário 2 - Abrir um link curto (Prioridade: P1)

Uma pessoa que recebe um link curto abre-o e é encaminhada ao destino; o acesso é contabilizado.

**Por que esta prioridade**: O redirecionamento é o principal valor entregue a quem compartilha e a quem recebe o link.

**Teste independente**: Abrir um link curto existente e verificar que a resposta encaminha ao destino e que a contagem aumenta em exatamente um.

**Cenários de aceitação**:

1. **Dado** que o código corresponde a um link existente, **quando** uma pessoa acessa o link curto, **então** é redirecionada à URL de destino e a contagem de acessos aumenta em um.
2. **Dado** que um link existente já recebeu acessos, **quando** a aplicação reinicia e o link é acessado novamente, **então** o redirecionamento continua funcionando e a contagem anterior é preservada e incrementada.
3. **Dado** que o código não corresponde a nenhum link, **quando** alguém acessa o link curto, **então** o serviço informa que o link não foi encontrado e não altera a contagem de qualquer link.

### História de Usuário 3 - Consultar acessos (Prioridade: P2)

A pessoa que criou um link consulta a quantidade de acessos recebidos por ele.

**Por que esta prioridade**: Consultar a contagem permite que a pessoa avalie o alcance do link após poder criá-lo e compartilhá-lo.

**Teste independente**: Consultar a contagem de um código existente antes e depois de acessos e verificar que o valor corresponde ao total acumulado.

**Cenários de aceitação**:

1. **Dado** que um link curto existe e ainda não recebeu acessos, **quando** a pessoa consulta sua contagem, **então** recebe o valor zero.
2. **Dado** que um link curto recebeu acessos, **quando** a pessoa consulta sua contagem, **então** recebe o total acumulado de acessos.
3. **Dado** que o código consultado não existe, **quando** a pessoa solicita sua contagem, **então** o serviço informa que o link não foi encontrado.

### Casos de borda

- Uma URL HTTP ou HTTPS com exatamente 2048 caracteres é válida; uma URL com 2049 caracteres é inválida.
- Esquemas diferentes de HTTP e HTTPS, inclusive URLs sem esquema, são rejeitados.
- Se uma colisão de código ocorrer, nenhum link existente é sobrescrito e a criação prossegue com outro código.
- Se um código escolhido já existir, a criação é recusada em vez de gerar um código substituto.
- O código personalizado deve ter entre 4 e 20 caracteres, inclusive, e usar somente letras ASCII maiúsculas ou minúsculas e dígitos.
- Um código desconhecido não provoca redirecionamento nem alteração de contagem.
- Cada solicitação que resulta em um redirecionamento bem-sucedido conta como um acesso; solicitações distintas contam separadamente.
- A contagem existente permanece igual ou aumenta após cada acesso e após reinicializações; nunca é reiniciada ou reduzida.
- Uma falha ao armazenar um novo link não deve resultar na entrega de um link curto que não possa ser recuperado.

## Requisitos

### Requisitos Funcionais

- **FR-001**: O sistema DEVE permitir criar um link curto a partir de uma URL de destino.
- **FR-002**: O sistema DEVE gerar automaticamente para cada link um código de exatamente 7 caracteres, usando apenas letras ASCII maiúsculas, letras ASCII minúsculas e dígitos de 0 a 9.
- **FR-003**: O sistema DEVE garantir que cada código esteja associado a no máximo um link em todo o sistema, inclusive quando houver criações simultâneas.
- **FR-004**: O sistema DEVE aceitar como destino somente URLs com esquema HTTP ou HTTPS e comprimento máximo de 2048 caracteres, inclusive.
- **FR-005**: O sistema DEVE rejeitar destinos que não atendam aos requisitos de esquema e comprimento, sem criar um link.
- **FR-006**: Após criar um link, o sistema DEVE retornar à pessoa criadora seu link curto completo para compartilhamento.
- **FR-007**: Ao receber uma solicitação para um código existente, o sistema DEVE redirecionar a pessoa à URL de destino correspondente.
- **FR-008**: O sistema DEVE aumentar a contagem do link em exatamente um para cada solicitação que resulte em redirecionamento bem-sucedido.
- **FR-009**: O sistema DEVE permitir consultar a contagem de acessos de um link existente, usando seu código curto como identificador.
- **FR-010**: O sistema DEVE informar que o link não foi encontrado ao receber um código inexistente para redirecionamento ou consulta de acessos.
- **FR-011**: O sistema DEVE preservar os links e suas contagens de acesso após a reinicialização da aplicação.
- **FR-012**: A contagem de acessos de um link NUNCA DEVE diminuir nem ser reiniciada durante a vida útil do link.
- **FR-013**: O sistema DEVE permitir que a pessoa criadora escolha um código personalizado com comprimento de 4 a 20 caracteres, inclusive.
- **FR-014**: O sistema DEVE recusar a criação de um link se o código personalizado escolhido já estiver associado a outro link, sem alterar o link existente.
- **FR-015**: O sistema DEVE aceitar em códigos personalizados somente letras ASCII maiúsculas ou minúsculas e dígitos de 0 a 9.

### Entidades Principais

- **Link curto**: Associação durável entre um código único — gerado automaticamente com 7 caracteres alfanuméricos ou escolhido pela pessoa criadora com 4 a 20 caracteres —, uma URL de destino HTTP ou HTTPS e a contagem acumulada de acessos.
- **Acesso**: Solicitação que resulta em um redirecionamento bem-sucedido para o destino de um link curto; cada solicitação bem-sucedida representa uma unidade na contagem.

## Critérios de Sucesso

### Resultados Mensuráveis

- **SC-001**: Em testes de aceitação sem código personalizado informado, 100% das URLs HTTP ou HTTPS válidas com até 2048 caracteres resultam em links curtos compartilháveis com códigos gerados automaticamente de exatamente 7 caracteres alfanuméricos.
- **SC-002**: Em testes de aceitação, 100% das URLs com esquema não permitido ou mais de 2048 caracteres são rejeitadas sem criação de link.
- **SC-003**: Em testes de aceitação, 100% dos acessos a códigos existentes redirecionam ao destino correto e incrementam a contagem em exatamente um.
- **SC-004**: Em testes de aceitação, nenhum link nem contagem previamente confirmados se perde após reiniciar a aplicação.
- **SC-005**: Em testes de aceitação, todos os códigos existentes são únicos, inclusive quando são simuladas colisões durante a criação.
- **SC-006**: Em teste de usabilidade, pelo menos 90% das pessoas participantes conseguem criar um link curto e consultar sua contagem sem assistência em até 2 minutos.
- **SC-007**: Em testes de aceitação, 100% dos códigos personalizados válidos e disponíveis são atribuídos exatamente como escolhidos, e 100% dos códigos personalizados já existentes são recusados sem alterar links anteriores.

## Pressupostos

- A pessoa criadora pode consultar a contagem usando o código curto que recebeu; como não há contas ou login, qualquer pessoa que conheça esse código também pode consultar a contagem.
- O serviço disponibiliza uma URL-base própria para formar o link curto completo; domínio personalizado não faz parte desta funcionalidade.
- A contagem inicial de um link recém-criado é zero.
- A contagem considera cada solicitação que conclui um redirecionamento, e não tenta identificar ou deduplicar pessoas ou dispositivos.
- Comparações de unicidade de códigos são exatas, incluindo diferenças entre letras maiúsculas e minúsculas.

## Fora de Escopo

- Cadastro e login de usuários.
- Domínios personalizados.
- Relatórios geográficos ou outros dados analíticos além da contagem total de acessos.
- Edição da URL de destino de um link já criado.
- Limites de frequência, CAPTCHA ou outros controles específicos contra criações abusivas.
