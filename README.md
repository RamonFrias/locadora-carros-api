# Locadora de Carros — Web API (.NET 10)

API REST para uma locadora de carros com **usuários** (cliente, funcionário, gerente), **carros** (placa antiga e Mercosul), **lojas** e **locações** (retirada e devolução em lojas/cidades diferentes, pagamento, caução, cancelamento com reembolso).

Construída com **DDD**, **Clean Architecture** e autenticação **Bearer Token (JWT)**.

## Como rodar

Pré-requisitos: .NET SDK 10 e MySQL 8.0+.

O banco padrão é o MySQL local (`localhost:3306`, banco `locadora_carros`, usuário `root`). A senha fica fora do código, nos user-secrets do .NET. Configure uma vez:

```bash
dotnet user-secrets set "ConnectionStrings:Locadora" "Server=localhost;Port=3306;Database=locadora_carros;Uid=root;Pwd=<sua senha>;Persist Security Info=true;" --project src/LocadoraCarros.Api
```

Depois, rode:

```bash
dotnet run --project src/LocadoraCarros.Api --launch-profile http
```

- Documentação interativa (Scalar): http://localhost:5194/scalar/v1
- OpenAPI: http://localhost:5194/openapi/v1.json

Na primeira execução em `Development`, o banco `locadora_carros` e as tabelas são criados automaticamente e populados com 3 lojas, 8 carros e um usuário de cada perfil:

| Perfil | Login (e-mail) | Login (CPF) |
|---|---|---|
| Gerente | gerente@locadora.dev | 123.456.789-09 |
| Funcionário | funcionario@locadora.dev | 987.654.321-00 |
| Cliente | cliente@locadora.dev | 529.982.247-25 |

A senha dos três está em `Seed:SenhaPadrao` no `appsettings.Development.json`.

### Autenticação

1. `POST /api/auth/login` com `{ "login": "<e-mail ou CPF>", "senha": "..." }`
2. Envie o token em todas as chamadas: `Authorization: Bearer <accessToken>`

Na UI do Scalar, use o botão de autenticação e cole o token.

### Testes

```bash
dotnet test
```

## Arquitetura

```
src/
  LocadoraCarros.Domain          Entidades, value objects, regras de negócio e contratos de repositório
  LocadoraCarros.Application     Casos de uso (services), DTOs, abstrações (token, hash, relógio, usuário atual)
  LocadoraCarros.Infrastructure  EF Core (MySQL, SQLite ou SQL Server), repositórios, JWT, PBKDF2, seed
  LocadoraCarros.Api             Controllers, políticas de acesso, tratamento de erros, OpenAPI
tests/
  LocadoraCarros.Domain.Tests    Testes das regras de domínio (xUnit)
```

As dependências apontam sempre para dentro: `Api → Infrastructure → Application → Domain`. O domínio não conhece EF, ASP.NET nem JWT.

### DDD

- **Agregados**: `Usuario` (`Cliente`, `Funcionario`, `Gerente`), `Carro`, `Loja`, `Locacao`. Agregados se referenciam apenas por Id.
- **Value objects** com validação: `Cpf`, `Cnpj` (inclusive o alfanumérico de 2026), `Placa` (antiga e Mercosul, com equivalência entre as duas), `Email`, `Telefone`, `Endereco`, `Cnh`.
- **Regras no domínio**: o agregado `Locacao` calcula valores, controla pagamento, reembolso e caução, e muda o status do carro na retirada/devolução. Os services de aplicação só orquestram.
- **Linguagem ubíqua** em português, igual à do negócio.

### Padrões da API (design system)

Para que todos os endpoints se comportem igual:

- **Rotas** em português, no plural e com substantivos: `/api/locacoes/{id}/cancelamento`, `/api/carros/{id}/status`.
- **Erros** sempre em Problem Details (RFC 9457):

  | Status | Quando |
  |---|---|
  | 400 | Dados inválidos (validação de entrada) |
  | 401 | Sem token, token inválido ou login incorreto |
  | 403 | Perfil sem permissão |
  | 404 | Recurso inexistente (ou locação de outro cliente) |
  | 409 | Duplicidade ou período ocupado |
  | 422 | Regra de negócio violada |
  | 429 | Excesso de tentativas de login |

- **Listas paginadas** no formato `{ itens, pagina, tamanhoPagina, totalItens, totalPaginas }`, com `?pagina=1&tamanhoPagina=20`.
- **Enums** como texto (`"Disponivel"`, `"CartaoCredito"`).
- **Datas** no horário local das lojas (Brasília), sem fuso: `2026-11-10T09:00:00`.
- **Exclusão lógica**: clientes, funcionários, carros e lojas são desativados, não apagados, para preservar o histórico.

## Permissões

| Ação | Cliente | Funcionário | Gerente |
|---|:-:|:-:|:-:|
| Cadastrar-se (`/api/auth/registro`) | ✔ (público) | | |
| Alterar o próprio cadastro (exceto CPF) | ✔ | ✘ | ✘ |
| Alterar a própria senha | ✔ | ✔ | ✔ |
| Reservar, alterar e cancelar as **próprias** locações | ✔ | | |
| Locações de qualquer cliente (criar, alterar, cancelar) | | ✔ | ✔ |
| Registrar retirada, devolução e quitação | | ✔ | ✔ |
| Clientes: CRUD completo (inclusive CPF e senha) | | ✔ | ✔ |
| Carros: ver os disponíveis de uma loja no período | ✔ | ✔ | ✔ |
| Carros: listar a frota e consultar por id | | ✔ | ✔ |
| Carros: cadastrar, alterar, status, transferir, excluir | | ✔ | ✔ |
| Funcionários: cadastrar, alterar, excluir | | | ✔ |
| Lojas: cadastrar, alterar, desativar | | | ✔ |

## Endpoints

| Método | Rota | Acesso |
|---|---|---|
| POST | `/api/auth/login` · `/api/auth/registro` | Público |
| GET/PUT | `/api/conta` · PUT `/api/conta/senha` | Logado |
| GET/POST/PUT/DELETE | `/api/clientes` · PUT `{id}/senha` · POST `{id}/reativacao` | Equipe |
| GET/POST/PUT/DELETE | `/api/funcionarios` · PUT `{id}/senha` · POST `{id}/reativacao` | Gerente |
| GET | `/api/carros/disponiveis?lojaId&inicio&fim` (loja obrigatória; sem placa, RENAVAM, chassi) | Logado |
| GET | `/api/carros` · `/api/carros/{id}` | Equipe |
| POST/PUT/DELETE | `/api/carros` · PATCH `{id}/status` · POST `{id}/transferencia` | Equipe |
| GET | `/api/lojas` · `/api/lojas/{id}` | Público |
| POST/PUT/DELETE | `/api/lojas` · POST `{id}/reativacao` | Gerente |
| GET | `/api/lojas/{id}/carros` · `/locacoes?movimento=Retirada\|Devolucao` · `/painel` | Equipe |
| GET | `/api/lojas/{id}/funcionarios` | Gerente |
| POST | `/api/locacoes/cotacao` · `/api/locacoes` | Logado |
| GET/PUT | `/api/locacoes` · `/api/locacoes/{id}` | Logado (cliente vê só as suas) |
| POST | `/api/locacoes/{id}/cancelamento` | Logado |
| POST | `/api/locacoes/{id}/retirada` · `/devolucao` · `/quitacao` | Equipe |

## Regras de negócio

**Locação**
- Uma diária a cada 24h, com 1h de tolerância e mínimo de 1 diária; o máximo é de 90 diárias.
- O condutor precisa ter 21 anos ou mais e CNH válida até a devolução, em categoria que permita dirigir carro.
- O carro precisa estar na loja de retirada e sem outra reserva no período.
- A retirada e a devolução previstas precisam cair no horário de funcionamento das lojas.
- **Devolução em outra loja**: taxa de R$ 60 na mesma cidade e de R$ 350 em outra cidade. A loja de destino precisa aceitar devoluções de outras lojas.
- **Proteção** (por diária): Básica R$ 29,90 · Completa R$ 54,90 · Premium R$ 79,90.
- **Adicionais** (por diária): condutor adicional, cadeirinha, assento de elevação, GPS, internet móvel e tag de pedágio.
- **Caução**: de R$ 700 a R$ 5.000 conforme a categoria do carro. É bloqueada na retirada e, na devolução, cobre os encargos.

**Pagamento**
- Cartão ou Pix são cobrados na reserva. Se a reserva for alterada, a diferença é cobrada ou estornada automaticamente.
- Em dinheiro, o valor é cobrado no balcão, na retirada.

**Cancelamento**
- Só é possível antes da retirada.
- Com **24h ou mais** de antecedência, o reembolso é integral. Com menos, o valor fica retido; só a equipe pode conceder reembolso fora do prazo (`concederReembolso: true`).

**Devolução**
- Atraso além de 1h cobra diárias extras (diária + proteção + adicionais).
- Cada oitavo de tanque faltante custa R$ 45.
- Danos são lançados pela equipe.
- Os encargos são descontados da caução; o que exceder vira saldo devedor, quitado em `/quitacao`.
- O carro passa para a loja de devolução com status `EmLimpeza`.

**Carros**
- Placa antiga e Mercosul são equivalentes: `ABC-1234` e `ABC1C34` não podem coexistir.
- O status `Alugado` é automático. Os demais (`EmManutencao`, `EmLimpeza`, `EmTransferencia`, `Indisponivel`, `Inativo`) são definidos pela equipe.

## Configuração para produção

- **Chave JWT**: nunca use a do `appsettings.Development.json`. Defina `Jwt__ChaveSecreta` (32+ caracteres) por variável de ambiente ou user-secrets.
- **Banco**: MySQL é o padrão (`Database:Provider=MySql`, provider oficial `MySql.EntityFrameworkCore`). Para outro banco: `Database__Provider=SqlServer` ou `Sqlite` e `ConnectionStrings__Locadora=...`. Em produção, passe a connection string por variável de ambiente e use um usuário próprio da aplicação, não o `root`.
- **Migrations**: o projeto usa `EnsureCreated` para facilitar o início. Em produção, troque por migrations do EF Core (`dotnet ef migrations add Inicial`).
- **Seed**: mantenha `Seed:Habilitado=false` fora do ambiente de desenvolvimento.

## Limitações conhecidas / próximos passos

- Não há refresh token: ao expirar (60 min), é preciso fazer login de novo.
- O pagamento é simulado. Integrar um gateway real exige webhooks e idempotência.
- A checagem de conflito de reserva não usa lock: duas reservas simultâneas do mesmo carro podem passar. Use transação serializável ou uma coluna de concorrência.
- Não é possível promover um funcionário a gerente (crie um novo cadastro com perfil `Gerente`).
- Faltam testes de integração da API (`WebApplicationFactory`).
