# Financeiro

Aplicacao WPF para consultas e acompanhamento do fluxo financeiro.

## Estrutura esperada

Esta solucao depende de tres repositorios no mesmo diretorio pai:

- `Financeiro`
- `BibliotecasSIG`
- `Update`

Exemplo:

```text
workspace/
  Financeiro/
  BibliotecasSIG/
  Update/
```

## Configuracao

As configuracoes principais ficam em `Financeiro/App.config`.

- `DatabaseHost`: host do PostgreSQL.
- `Database`: banco/ano. Se estiver vazio, usa o ano atual.
- `Username`: usuario inicial. Se estiver vazio, usa o usuario do Windows.
- `Password`: senha padrao compartilhada do usuario direto no banco.
- `SystemPath`: pasta de instalacao usada para atualizar o usuario no arquivo de configuracao.
- `UpdateInfoUrl`: endpoint do manifesto de atualizacao.

Tambem e possivel sobrescrever banco por variaveis de ambiente:

- `FINANCEIRO_DB_HOST`
- `FINANCEIRO_DB_NAME`
- `FINANCEIRO_DB_USER`
- `FINANCEIRO_DB_PASSWORD`

## Telerik

O projeto usa `Telerik.UI.for.Wpf.80.Xaml`. Para restaurar os pacotes, configure credenciais locais para o feed oficial da Telerik:

```powershell
dotnet nuget update source Telerik --username "<usuario-telerik>" --password "<senha-ou-token>" --store-password-in-clear-text
```

O arquivo `NuGet.config` inclui apenas a URL da fonte, sem credenciais.

## Build

```powershell
dotnet restore Financeiro.sln
dotnet build Financeiro.sln
```
