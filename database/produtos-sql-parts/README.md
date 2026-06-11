# produtos.sql fracionado para sync VendApps

Origem: D:\produtos.sql
Gerado em: 2026-06-11 20:06:25
Total de partes: 10

Cada arquivo `produtos_part_XXX.sql` e um mini dump completo: contem o `CREATE TABLE produtos` e somente um bloco de dados.
Isso permite enviar/selecionar qualquer parte no wizard de sincronismo, porque o backend consegue descobrir tabela e colunas a partir do proprio arquivo.

## Uso no wizard

1. Modo: `Arquivo .sql (dump)`.
2. Banco: `MySQL`.
3. Arquivo: envie uma parte por vez, em ordem.
4. Tabela: `produtos`.
5. Unidade dos precos: `reais`.

## Mapeamento recomendado

| Coluna do dump | Campo do sistema |
|---|---|
| `CONTADOR` | `ExternalId` |
| `CODPRODUTO` | `InternalCode` |
| `CODEAN` | `Barcode` |
| `NOMEPRODUTO` | `Name` |
| `NOMEGENERICO` | `Description` |
| `GRUPO` | `CategoryName` |
| `FABRICANTE` | `BrandName` |
| `UNIDADE` | `Unit` |
| `PRECOVENDA` | `PriceCents` |
| `PRECOCUSTO` | `CostCents` |
| `QTDATUAL` | `StockQty` |
| `NCM` | `Ncm` |
| `FOTOGRAFIA` | `ImageUrl` |
| `ATUALIZA` | `UpdatedAt` |

Observacao: nao mapeie `DESATIVADO` para `IsActive` nesse fluxo atual. No dump, `DESATIVADO = 1` significa produto desativado, mas o parser do backend interpreta `1` como booleano verdadeiro. Deixe `IsActive` sem mapeamento para importar os produtos como ativos, ou ajuste o backend para inverter essa coluna.

## Arquivos

| Ordem | Arquivo | Registros aproximados | Tamanho |
|---:|---|---:|---:|
| 1 | `produtos_part_001.sql` | 971 | 1.052.506 bytes |
| 2 | `produtos_part_002.sql` | 1005 | 1.053.178 bytes |
| 3 | `produtos_part_003.sql` | 1010 | 1.052.320 bytes |
| 4 | `produtos_part_004.sql` | 991 | 1.052.780 bytes |
| 5 | `produtos_part_005.sql` | 995 | 1.052.529 bytes |
| 6 | `produtos_part_006.sql` | 985 | 1.052.451 bytes |
| 7 | `produtos_part_007.sql` | 986 | 1.052.229 bytes |
| 8 | `produtos_part_008.sql` | 967 | 1.053.116 bytes |
| 9 | `produtos_part_009.sql` | 957 | 1.053.246 bytes |
| 10 | `produtos_part_010.sql` | 447 | 505.718 bytes |
