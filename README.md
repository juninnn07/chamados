# Chamados

Sistema de chamados (ordens de serviço) — projeto integrador da formação
*Desenvolvimento Web e Multiplataforma com .NET*. Estado: Módulo 7.

## Requisitos

- .NET SDK 10.0.1xx ou superior na linha 10.0 (fixado em `global.json`)
- Git 2.28+
- Visual Studio 2026 (Community serve) com a carga de trabalho **ASP.NET e desenvolvimento Web**

## Executar

No Visual Studio: abra `Chamados.slnx`, escolha o perfil **https** na barra de ferramentas e pressione `F5`.
O navegador abre no painel (`https://localhost:7080/`). Na primeira execução, aceite a confiança no certificado de desenvolvimento.

Pela CLI:

```bash
dotnet build
dotnet run --project src/Chamados.Web --launch-profile https
```

Portas fixas: http 5080, https 7080.

### Banco de dados (Módulo 7)

Os chamados ficam num arquivo SQLite, `src/Chamados.Web/chamados.db` (string de conexão `ConnectionStrings:Chamados`). Em Development, a aplicação aplica as migrations pendentes ao iniciar e, se o banco estiver vazio, grava 12 chamados de exemplo. Para recomeçar do zero, pare a aplicação e apague `chamados.db`, `chamados.db-shm` e `chamados.db-wal`. O arquivo não vai para o Git (`.gitignore`).

Migrations no Visual Studio (**Ferramentas › Gerenciador de Pacotes do NuGet › Console do Gerenciador de Pacotes**, com o projeto padrão `Chamados.Infra` e `Chamados.Web` como projeto de inicialização). Os comandos vêm do pacote `Microsoft.EntityFrameworkCore.Tools`, já referenciado no `Chamados.Web`:

```powershell
Add-Migration NomeDaMudanca
Update-Database
Script-Migration -Idempotent   # SQL revisável, para produção
```

Pela CLI (ferramenta `dotnet-ef`, instalada uma vez com `dotnet tool install --global dotnet-ef --version 10.0.15`):

```bash
dotnet ef migrations add NomeDaMudanca --project src/Chamados.Infra --startup-project src/Chamados.Web
dotnet ef database update --project src/Chamados.Infra --startup-project src/Chamados.Web
```

O projeto já traz duas migrations: `Inicial` (tabelas `Chamados` e `HistoricoChamados`) e `IndicePrioridade` (um índice criado depois, para mostrar o banco evoluindo sem perder dados). Um banco criado só com a primeira recebe a segunda no próximo início.

Em Development, o log mostra cada comando SQL (categoria `Microsoft.EntityFrameworkCore.Database.Command`), com os valores dos parâmetros ocultos.

### Páginas (Razor Pages)

| Rota | Página | O que faz |
| --- | --- | --- |
| `/` | `Pages/Index` | Painel com totais por status (`ObterEstatisticasAsync`) |
| `/Chamados?status=&prioridadeMinima=&texto=&ordem=&pagina=` | `Pages/Chamados/Index` | Lista com filtros, ordenação e paginação (`ListarPaginaAsync`) |
| `/Chamados/Novo` | `Pages/Chamados/Novo` | Formulário de abertura (`AbrirAsync`), validação e Post/Redirect/Get |
| `/Chamados/Detalhes/{id}` | `Pages/Chamados/Detalhes` | Detalhe, histórico e handlers Iniciar, Resolver, Cancelar |
| `/Chamados/Editar/{id}` | `Pages/Chamados/Editar` | Edição com concorrência otimista (`EditarAsync`) |
| `/Erro`, `/Erro/{código}` | `Pages/Erro` | Erros sem detalhes internos; 404 e 400 amigáveis fora de `/api` |

### Páginas Blazor (componentes Razor, Módulos 5 e 6)

| Rota | Componente | O que faz |
| --- | --- | --- |
| `/atendimento` | `Components/Pages/Atendimento.razor` | Fila interativa (InteractiveServer): busca com cancelamento, iniciar, resolver, cancelar, abrir e editar (EditForm) sem recarregar |
| `/modos` | `Components/Pages/Modos.razor` | O mesmo `Contador` renderizado de forma estática e interativa, lado a lado |
| `/indicadores` | `Components/Pages/Indicadores.razor` | Totais lidos pela API com o cliente tipado `ChamadosApiCliente` (Módulo 7), com renderização em fluxo |

Razor Pages e Blazor convivem na mesma aplicação: `MapRazorPages()` e `MapRazorComponents<App>()` no `Program.cs`, o mesmo `site.css` e o mesmo `ChamadoServico`.

Componentes acrescentados no Módulo 6: `Formularios/FormularioChamado` (EditForm com Data Annotations, validação personalizada e erros do servidor), `Layout/ResumoFila` (totais no cabeçalho, alimentados pelo serviço `Estado/EstadoFila`) e o módulo JavaScript `wwwroot/js/preferencias.js` (lembra o responsável no `localStorage`).

### API JSON (movida para `/api` no Módulo 3)

| Rota | Função |
| --- | --- |
| `GET /api/chamados?status=&prioridadeMinima=&texto=&ordem=` | Lista com filtros e ordenação |
| `GET /api/chamados/estatisticas` | Totais e tempo médio de resolução |
| `GET /api/chamados/{id}` | Um chamado com o histórico (`DetalheChamadoResposta`) |
| `POST /api/chamados` | Abre chamado (corpo: `{"titulo": "...", "prioridade": "Alta"}`); corpo inválido → 400 com erros por campo |
| `POST /api/chamados/{id}/atendimento` | Inicia atendimento (corpo: `{"responsavel": "..."}`) |
| `PUT /api/chamados/{id}` | Edita (corpo com `versaoEsperada`; versão desatualizada → 409) |
| `GET /api/status`, `GET /api/ambiente` | Herdadas do Módulo 1 (`/api/ambiente` só em Development) |

Erros de domínio na API voltam como Problem Details (RFC 9457): 400, 404 ou 409. Em Development, o documento OpenAPI fica em `/openapi/v1.json`.

### Configuração

`ConnectionStrings:Chamados` aponta para o arquivo SQLite; `Api:UrlBase` é o endereço que o cliente HTTP da página Indicadores usa: `https://localhost:7080/` no perfil `https`; o perfil `http` troca para `http://localhost:5080/` pela variável `Api__UrlBase` do `launchSettings.json`.
`Simulacao:LatenciaMs` (0 a 10000) atrasa as consultas para observar carregamento e cancelamento. Em Development vale 300.
`Simulacao:FalhaNaListagemACada` (0 a 100) faz uma listagem a cada N falhar de propósito, para testar o tratamento de erro; 0 desliga.
Valor fora da faixa impede a aplicação de iniciar (`ValidateOnStart`). Para testar sem editar arquivo:

```powershell
$env:Simulacao__LatenciaMs = "20000"; dotnet run --project src/Chamados.Web
$env:Simulacao__FalhaNaListagemACada = "3"; dotnet run --project src/Chamados.Web
```

No Visual Studio, o perfil **https-falha** (em `launchSettings.json`) já liga `Simulacao__FalhaNaListagemACada = 3`; pela CLI: `dotnet run --project src/Chamados.Web --launch-profile https-falha`.

### Bibliotecas do lado do cliente

`wwwroot/lib` traz jQuery 3.7.1, jQuery Validation 1.21.0 e jQuery Validation Unobtrusive 4.0.0 (licenças MIT e Apache 2.0, arquivos de licença incluídos), usados só pela validação no cliente. O `libman.json` registra a origem; no Visual Studio, botão direito em `libman.json` › **Restaurar Bibliotecas do Lado do Cliente** baixa de novo.

## Testar

No Visual Studio: **Teste > Gerenciador de Testes > Executar Todos** (`Ctrl+R, A`). Pela CLI: `dotnet test`. São 51 testes: 43 do domínio e 8 de integração com o banco.

## Estrutura

| Projeto | Papel | Depende de |
| --- | --- | --- |
| `src/Chamados.Domain` | Entidades, regras e casos de uso | — |
| `src/Chamados.Infra` | EF Core: `ChamadosDbContext`, mapeamento (Fluent API), migrations e `RepositorioChamadosEf` | Domain |
| `src/Chamados.Web` | Páginas Razor, componentes Blazor, API JSON, composição e pipeline | Domain, Infra |
| `tests/Chamados.Domain.Tests` | Testes xUnit do domínio (repositório em memória) | Domain |
| `tests/Chamados.Infra.Tests` | Testes de integração do repositório com SQLite em memória | Infra |

## Decisões

1. **.NET 10 LTS** como alvo: suporte até 14/11/2028; .NET 8 e 9 encerram em 10/11/2026.
2. **Formato `.slnx`** para a solução, padrão do SDK 10.
3. **Chamados.Web como projeto de inicialização**, declarado no `.slnx` com `DefaultStartup="true"`.
4. **Domain não referencia nenhum outro projeto**: as camadas externas dependem dele, nunca o contrário.
5. **Repositório como interface** (`IChamadoRepositorio`): a implementação em memória será trocada pelo EF Core no Módulo 7.
6. **`TimeProvider` injetado** em vez de `DateTimeOffset.UtcNow` direto: permite testar datas com um relógio controlado.
7. **Sem camada Application separada** por enquanto: os casos de uso (`ChamadoServico`) ficam no Domain.
8. **Razor Pages para os formulários** (Módulo 3): uma página por tela, com o `PageModel` chamando só o `ChamadoServico`, nunca o repositório.
9. **Validação em duas camadas**: Data Annotations no modelo do formulário (mensagem amigável, no servidor) e invariantes no domínio (palavra final). Os limites de tamanho são constantes do `Chamado`, reusadas nos atributos.
10. **Sem validação no cliente por enquanto**: nenhuma biblioteca JavaScript; o servidor valida sempre. Validação no cliente é conforto, não segurança (Módulo 4).
11. **API sob `/api`** com `MapGroup` e um filtro único de exceções de domínio, para não disputar a rota `/Chamados` com as páginas.
12. **Concorrência otimista com número de versão** (Módulo 4): `Chamado.Versao` aumenta a cada alteração; quem edita informa a versão lida. No Módulo 7 o EF Core assume a conferência com um token de concorrência.
13. **Paginação no serviço, não na página**: `ListarPaginaAsync` devolve `Pagina<T>`. Quando o EF Core chegar, `Skip`/`Take` viram SQL sem mudar as páginas.
14. **Partial para o que só exibe, View Component para o que busca dados, Tag Helper para marcação repetida**: `_CamposChamado`, `ResumoStatus` e `<selo>`.
15. **Páginas de status só fora de `/api`**: `UseWhen` cria um ramo do pipeline para que a API continue respondendo JSON.
16. **Blazor no mesmo projeto Web** (Módulo 5): Razor Pages ficam com os formulários de cadastro; a fila de atendimento, que muda o tempo todo, vira um componente interativo no servidor (`InteractiveServer`). Nenhum projeto novo.
17. **Interatividade por página, não global**: só `/atendimento` declara `@rendermode InteractiveServer`. O resto continua estático, sem conexão aberta com o servidor. No Módulo 6, o `ResumoFila` do cabeçalho das páginas Blazor também é interativo, com a própria raiz no circuito.
18. **Componente pede, página executa**: `CartaoChamado` só avisa a página por `EventCallback<PedidoTransicao>`; quem chama o `ChamadoServico` e trata as exceções é `Atendimento.razor`.
19. **`ChamadoInput` também no Blazor** (Módulo 6): o `EditForm` usa o mesmo modelo e as mesmas Data Annotations das Razor Pages. A regra nova, `[TextoSignificativo]`, vale nas duas telas.
20. **Erros do servidor no `EditContext`**: concorrência e regras do domínio entram por um `ValidationMessageStore` e aparecem no `ValidationSummary`. O envio usa `OnSubmit` com `Validate()` manual, para limpar esses erros antes de validar de novo.
21. **Estado compartilhado em serviço Scoped com evento**: `EstadoFila` liga a página de atendimento ao resumo do cabeçalho sem relação pai e filho. Quem se inscreve no evento cancela a inscrição no `Dispose`.
22. **JavaScript só no `OnAfterRenderAsync`, em módulo ES isolado**: nada de JS na pré-renderização; o `localStorage` guarda só preferência de tela, nunca dado sensível.
23. **Fila sem busca duplicada**: `[PersistentState]` leva a lista carregada na pré-renderização para o circuito; cada busca nova cancela a anterior com `CancellationTokenSource`.
24. **EF Core com SQLite num projeto próprio** (Módulo 7): `Chamados.Infra` implementa `IChamadoRepositorio`; o domínio continua sem referência a EF. A única concessão é um construtor privado sem parâmetros no `Chamado`, para o EF materializar objetos lidos do banco.
25. **Mapeamento só por Fluent API**, numa classe `IEntityTypeConfiguration` por entidade; o histórico é entidade *owned* (tabela `HistoricoChamados`), carregada e gravada sempre com o chamado.
26. **Consultas no banco, não em memória**: o repositório recebe o filtro e devolve resumos projetados; `Where`, `ORDER BY`, `LIMIT/OFFSET` e `GROUP BY` viram SQL. O repositório em memória mantém a mesma regra para os testes do domínio.
27. **Um DbContext por operação, criado por `IDbContextFactory`**: no Blazor Server, um contexto por circuito seria usado por dois componentes ao mesmo tempo, o que o EF não permite.
28. **Concorrência no banco**: `Versao` é token de concorrência; o UPDATE leva `WHERE Versao = <lida>` e a falha vira `ConcorrenciaException`.
29. **DateTimeOffset gravado como número** (`DateTimeOffsetToBinaryConverter`): o SQLite não ordena nem compara datas com fuso guardadas como texto. O custo é a precisão: frações abaixo do milissegundo se perdem na gravação.
30. **API com DTOs, validação automática e OpenAPI**: a entidade nunca é serializada; `AddValidation()` confere as Data Annotations do corpo; o documento OpenAPI é publicado só em Development.
31. **Toda mudança de esquema por migration**: nada de `EnsureCreated` nem de editar o banco à mão. Se o modelo muda sem migration, o `MigrateAsync` recusa iniciar (`PendingModelChangesWarning`) e um teste de integração (`HasPendingModelChanges`) acusa antes.
