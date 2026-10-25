# Quickstart: Encurtador de Links

## Pré-requisitos

- .NET 10 SDK.
- Ferramenta de linha de comando do EF Core para aplicar migrações.
- O arquivo SQLite configurado em caminho persistente e gravável.

## Validar

Na raiz do repositório, depois de gerar a solução e os projetos previstos no plano:

```powershell
dotnet test
```

O comando deve executar os testes xUnit unitários e de integração. Os testes de integração devem usar um banco SQLite isolado e verificar:

1. Criação com código gerado e customizado.
2. Validação de URL HTTP(S), limites de tamanho e caracteres/comprimento do código personalizado.
3. Rejeição de um código personalizado duplicado, incluindo criação concorrente.
4. Redirecionamento e incremento de uma unidade por solicitação.
5. Incrementos concorrentes sem perda de acessos.
6. Persistência do link e da contagem após recriar o host da aplicação com o mesmo arquivo SQLite.
7. Consulta pública de estatísticas e respostas para códigos inexistentes.

## Executar localmente

```powershell
$env:ConnectionStrings__Links = 'Data Source=encurtador.db'
$env:PublicBaseUrl = 'http://localhost:5002'
dotnet ef database update --project src\Encurtador.Infrastructure --startup-project src\Encurtador.Api
dotnet run --project src\Encurtador.Api
```

O arquivo `encurtador.db` deve permanecer entre reinicializações; não configure o caminho como banco em memória. O comando de migração e a API devem usar a mesma `ConnectionStrings__Links`; sem essa variável, ambos usam `encurtador.db` na pasta do projeto da API. Aplique as migrações antes de iniciar o serviço e use um `PublicBaseUrl` configurado, não o cabeçalho Host da solicitação, para formar links compartilháveis. Após iniciar, valide os contratos documentados em [contracts/http-api.md](./contracts/http-api.md), usando o endereço anunciado pela aplicação. O redirecionamento deve responder com `Cache-Control: no-store`, para que acessos subsequentes voltem ao serviço e sejam contados.
