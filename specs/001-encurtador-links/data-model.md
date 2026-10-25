# Modelo de Dados: Encurtador de Links

## Entidade: Link

Representa a associação persistente entre um código curto e seu destino.

| Campo | Tipo conceitual | Regras |
|---|---|---|
| `Code` | Texto | Chave identificadora única. Código gerado: exatamente 7 caracteres do alfabeto `23456789ABCDEFGHJKMNPQRSTVWXYZ`. Código personalizado: 4–20 caracteres ASCII alfanuméricos. Comparação sensível a maiúsculas/minúsculas. |
| `DestinationUrl` | Texto | URL absoluta HTTP ou HTTPS; comprimento máximo de 2048 caracteres. |
| `ClickCount` | Inteiro não negativo de 64 bits | Inicializado em zero; incrementado exatamente uma vez por solicitação de redirecionamento bem-sucedida; não pode diminuir. |

## Relações e Restrições

- Cada link tem um único código; um código corresponde a no máximo um link.
- A unicidade é imposta por índice único no armazenamento além da verificação antecipada em `LinkService`.
- Códigos personalizados existentes causam conflito; nunca são substituídos por um código gerado.
- Uma colisão em código gerado leva a uma nova tentativa com outro código.
- O destino e a contagem persistem em armazenamento durável para sobreviver a reinicializações.
- Estatísticas são consultáveis publicamente pelo código; não há campo de chave de gerenciamento nem autenticação de proprietário.
- O incremento é uma operação atômica no banco, nunca uma sequência de leitura e regravação do contador pela aplicação.

## Estados e Ciclo de Vida

Um link é criado com `ClickCount = 0`, pode receber zero ou mais acessos, e permanece disponível sem expiração definida. A edição do destino e a remoção de links não fazem parte do escopo.
