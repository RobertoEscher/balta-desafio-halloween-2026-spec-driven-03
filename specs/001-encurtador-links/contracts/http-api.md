# Contrato HTTP: Encurtador de Links

**Base local sugerida**: `http://localhost:5002`

## Criar link

`POST /api/links`

Request JSON:

```json
{
  "destinationUrl": "https://example.com/a/long/path",
  "customCode": "MyLink42"
}
```

`customCode` é opcional. Se omitido ou nulo, o serviço gera um código de 7 caracteres. Quando informado, deve conter 4–20 caracteres ASCII alfanuméricos e é usado exatamente como enviado, incluindo capitalização.

**201 Created**

```json
{
  "code": "MyLink42",
  "shortUrl": "http://localhost:5002/MyLink42"
}
```

**Erros**

- `400 Bad Request` com ProblemDetails se o JSON, destino, esquema, comprimento ou código for inválido.
- `409 Conflict` com ProblemDetails se o código personalizado já estiver em uso.
- `503 Service Unavailable` com ProblemDetails se colisões repetidas impedirem a reserva de um código gerado em cinco candidatos.

## Redirecionar

`GET /{code}`

- Para código existente, responde `302 Found` com `Location` apontando para o destino e `Cache-Control: no-store`.
- O contador é incrementado atomicamente antes de retornar o redirecionamento.
- Para código desconhecido, responde `404 Not Found` sem alterar contagens.

## Consultar acessos

`GET /api/links/{code}/stats`

**200 OK**

```json
{
  "code": "MyLink42",
  "clickCount": 12
}
```

- A consulta é pública para qualquer pessoa que conheça o código; não há chave de gerenciamento.
- Código desconhecido resulta em `404 Not Found`.
- Consultar estatísticas não incrementa a contagem.

## Formato de erro

Erros de validação usam HTTP 400 e o formato ProblemDetails. Conflito de código personalizado usa HTTP 409 e ProblemDetails. Recursos inexistentes usam HTTP 404.
