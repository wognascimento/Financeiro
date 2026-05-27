using CommunityToolkit.Mvvm.ComponentModel;
using Financeiro.DataBase;
using Financeiro.DataBase.Models;
using Financeiro.DataBase.Models.Dto;
using Financeiro.DataBase.Repository;
using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Controls;
using Telerik.Windows.Controls;
using Telerik.Windows.Controls.GridView;

namespace Financeiro.Views;

/// <summary>
/// Interacao logica para FluxoCaixa.xaml
/// </summary>
public partial class FluxoCaixa : UserControl
{
    public FluxoCaixa()
    {
        InitializeComponent();
        DataContext = new FluxoCaixaViewModel();
    }

    private void OnAdicionarFluxoClick(object sender, RoutedEventArgs e)
    {
        GridFluxo.BeginInsert();
    }

    private void GridFluxo_AddingNewDataItem(object sender, GridViewAddingNewEventArgs e)
    {
        if (DataContext is FluxoCaixaViewModel vm)
            e.NewObject = vm.CriarNovoFluxo();
    }

    private void GridFluxo_CellValidating(object sender, GridViewCellValidatingEventArgs e)
    {
        if (DataContext is not FluxoCaixaViewModel vm)
            return;

        var erro = vm.ValidarCampoFluxo(e.Cell.Column.UniqueName, e.NewValue);
        if (!string.IsNullOrWhiteSpace(erro))
        {
            e.IsValid = false;
            e.ErrorMessage = erro;
        }
    }

    private async void GridFluxo_RowValidating(object sender, GridViewRowValidatingEventArgs e)
    {
        if (e.EditOperationType == GridViewEditOperationType.None || e.Row.Item is not FluxoDto fluxo)
            return;

        if (DataContext is not FluxoCaixaViewModel vm)
            return;

        var erros = vm.ValidarFluxo(fluxo);
        if (erros.Count > 0)
        {
            AplicarErrosLinha(e, erros);
            return;
        }

        if (!await vm.SalvarFluxoAsync(fluxo))
            AplicarErrosLinha(e, ["Nao foi possivel salvar o fluxo no banco de dados."]);
    }

    private static void AplicarErrosLinha(GridViewRowValidatingEventArgs e, IReadOnlyList<string> erros)
    {
        e.IsValid = false;
        foreach (var erro in erros)
        {
            e.ValidationResults.Add(new GridViewCellValidationResult
            {
                ErrorMessage = erro,
                PropertyName = string.Empty
            });
        }
    }
}

public partial class FluxoCaixaViewModel : ObservableObject
{
    private readonly FluxoRepository _repository;
    private readonly bool _carregarInicialSemFiltro;
    private CancellationTokenSource? _filtroCts;
    private bool _inicializando = true;

    public FluxoCaixaViewModel(bool carregarInicialSemFiltro = false)
    {
        _carregarInicialSemFiltro = carregarInicialSemFiltro;
        _repository = new FluxoRepository(DataBaseSettings.Instance.ConnectionString ?? string.Empty);
        _ = InicializarAsync();
    }

    [ObservableProperty]
    private ObservableCollection<EmpresaModel> empresas = [];

    [ObservableProperty]
    private ObservableCollection<ContaModel> contas = [];

    [ObservableProperty]
    private ObservableCollection<string> contasGrid = [];

    [ObservableProperty]
    private ObservableCollection<string> departamentosGrid = [];

    [ObservableProperty]
    private ObservableCollection<string> tiposGrid = [];

    [ObservableProperty]
    private ObservableCollection<FluxoDto> fluxos = [];

    [ObservableProperty]
    private EmpresaModel? selectedEmpresa;

    [ObservableProperty]
    private ContaModel? selectedConta;

    [ObservableProperty]
    private DateTime? dataInicial;

    [ObservableProperty]
    private DateTime? dataFinal;

    [ObservableProperty]
    private decimal saldoAnterior;

    [ObservableProperty]
    private decimal saldo;

    [ObservableProperty]
    private FluxoDto? selectedMovimentoFuturo;

    [ObservableProperty]
    private string? descricaoFiltro;

    [ObservableProperty]
    private string? cnpjFiltro;

    [ObservableProperty]
    private string? formaPagtoFiltro;

    [ObservableProperty]
    private string? tipoFiltro;

    [ObservableProperty]
    private string? okFiltro;

    private async Task InicializarAsync()
    {
        try
        {
            if (!_carregarInicialSemFiltro)
            {
                DataInicial = DateTime.Today;
                DataFinal = DataInicial.Value.AddDays(30);
            }

            await CarregarEmpresasAsync();
            await CarregarContasAsync(null);
            await CarregarContasGridAsync(null);
            await CarregarDepartamentosAsync();
            await CarregarTiposAsync();

            if (_carregarInicialSemFiltro)
                await CarregarTodosLancamentosAsync();
            else
                await AtualizarFluxoAtualAsync("inicializacao");
        }
        catch (Exception ex)
        {
            MessageBox.Show(
                $"Erro ao inicializar tela.\n\n{ex.Message}",
                "Erro",
                MessageBoxButton.OK,
                MessageBoxImage.Error
            );
        }
        finally
        {
            _inicializando = false;
        }
    }

    public async Task CarregarEmpresasAsync()
    {
        var itens = await _repository.BuscarEmpresasAsync();
        Empresas = new ObservableCollection<EmpresaModel>(itens);
    }

    public async Task CarregarContasAsync(long? idEmpresa)
    {
        var itens = await _repository.BuscarContasAsync(idEmpresa);
        Contas = new ObservableCollection<ContaModel>(itens);
    }

    public async Task CarregarContasGridAsync(long? idEmpresa)
    {
        var itens = await _repository.BuscarContasFluxoAsync(idEmpresa);
        ContasGrid = new ObservableCollection<string>(itens);
    }

    public async Task CarregarDepartamentosAsync()
    {
        var itens = await _repository.BuscarDepartamentosAsync();
        DepartamentosGrid = new ObservableCollection<string>(itens);
    }

    public async Task CarregarTiposAsync()
    {
        var itens = await _repository.BuscarTiposAsync();
        TiposGrid = new ObservableCollection<string>(itens);
    }

    public async Task CarregarTodosLancamentosAsync()
    {
        var itens = await _repository.CarregarFluxoAsync(new FluxoFiltroDto
        {
            ControleNome = "todos_lancamentos"
        });

        SaldoAnterior = 0;
        Saldo = 0;
        Fluxos = new ObservableCollection<FluxoDto>(itens);
    }

    public FluxoDto CriarNovoFluxo()
    {
        return new FluxoDto
        {
            conta = SelectedConta?.conta,
            data_emissao = DateOnly.FromDateTime(DateTime.Today),
            data_vencimento = DataInicial.HasValue ? DateOnly.FromDateTime(DataInicial.Value) : null,
            data_pagamento = DateOnly.FromDateTime(DateTime.Today),
            debito = 0,
            credito = 0,
            valor_previsto = 0,
            saldo_acumulado = 0,
            cadastradopor = DataBaseSettings.Instance.Username,
            cadastradoem = DateTime.Now
        };
    }

    public async Task<bool> SalvarFluxoAsync(FluxoDto fluxo)
    {
        try
        {
            var erros = ValidarFluxo(fluxo);
            if (erros.Count > 0)
            {
                MessageBox.Show(string.Join(Environment.NewLine, erros), "Validacao", MessageBoxButton.OK, MessageBoxImage.Warning);
                return false;
            }

            if (!CpfCnpjValido(fluxo.cnpj))
            {
                MessageBox.Show("CPF/CNPJ invalido. Informe 11 digitos para CPF ou 14 digitos para CNPJ valido.", "Validacao", MessageBoxButton.OK, MessageBoxImage.Warning);
                return false;
            }

            var model = MapearFluxo(fluxo);

            if (fluxo.linha_fluxo.HasValue)
            {
                model.alteradopor = DataBaseSettings.Instance.Username;
                model.alteradoem = DateTimeOffset.Now;
                await _repository.AtualizarFluxoAsync(model);
            }
            else
            {
                model.cadastradopor = DataBaseSettings.Instance.Username;
                model.cadastradoem = DateTimeOffset.Now;
                fluxo.linha_fluxo = await _repository.InserirFluxoAsync(model);
            }

            await AtualizarFluxoAtualAsync("grid");
            return true;
        }
        catch (Exception ex)
        {
            MessageBox.Show(
                $"Erro ao salvar fluxo. A operacao foi desfeita no banco.\n\n{ex.Message}",
                "Erro",
                MessageBoxButton.OK,
                MessageBoxImage.Error
            );

            await AtualizarFluxoAtualAsync("erro_grid");
            return false;
        }
    }

    public List<string> ValidarFluxo(FluxoDto fluxo)
    {
        List<string> erros = [];

        if (string.IsNullOrWhiteSpace(fluxo.conta))
        {
            erros.Add("Conta e obrigatoria.");
        }
        else if (!ContasGrid.Contains(fluxo.conta))
        {
            erros.Add("Conta informada nao existe na lista de contas disponiveis.");
        }

        if (!string.IsNullOrWhiteSpace(fluxo.depto) && !DepartamentosGrid.Contains(fluxo.depto))
            erros.Add("Depto informado nao existe na lista de departamentos disponiveis.");

        if (!string.IsNullOrWhiteSpace(fluxo.tipo) && !TiposGrid.Contains(fluxo.tipo))
            erros.Add("F/V informado nao existe na lista de tipos disponiveis.");

        if (string.IsNullOrWhiteSpace(fluxo.descricao))
            erros.Add("Descricao e obrigatoria.");

        if (!fluxo.data_pagamento.HasValue)
            erros.Add("Data de pagamento e obrigatoria.");

        if (string.IsNullOrWhiteSpace(fluxo.cnpj))
            erros.Add("CPF/CNPJ e obrigatorio.");
        else if (!CpfCnpjValido(fluxo.cnpj))
            erros.Add("CPF/CNPJ invalido. Informe 11 digitos para CPF ou 14 digitos para CNPJ valido.");

        if (double.IsNaN(fluxo.valor_previsto) || double.IsInfinity(fluxo.valor_previsto))
            erros.Add("Valor previsto precisa ser um numero valido.");

        return erros;
    }

    public string? ValidarCampoFluxo(string campo, object? valor)
    {
        var texto = valor?.ToString();

        return campo switch
        {
            "conta" when string.IsNullOrWhiteSpace(texto) => null,
            "conta" when !ContasGrid.Contains(texto) => "Conta informada nao existe na lista de contas disponiveis.",
            "depto" when !string.IsNullOrWhiteSpace(texto) && !DepartamentosGrid.Contains(texto) => "Depto informado nao existe na lista de departamentos disponiveis.",
            "tipo" when !string.IsNullOrWhiteSpace(texto) && !TiposGrid.Contains(texto) => "F/V informado nao existe na lista de tipos disponiveis.",
            "descricao" when string.IsNullOrWhiteSpace(texto) => "Descricao e obrigatoria.",
            "data_pagamento" when valor is null => "Data de pagamento e obrigatoria.",
            "cnpj" when string.IsNullOrWhiteSpace(texto) => "CPF/CNPJ e obrigatorio.",
            "cnpj" when !CpfCnpjValido(texto) => "CPF/CNPJ invalido.",
            _ => null
        };
    }

    partial void OnSelectedEmpresaChanged(EmpresaModel? value)
    {
        if (_inicializando)
            return;

        _ = AtualizarContasPorEmpresaAsync();
        _ = FiltrarComDebounceAsync("empresa");
    }

    partial void OnSelectedContaChanged(ContaModel? value)
    {
        if (_inicializando)
            return;

        _ = FiltrarComDebounceAsync("conta");
    }

    partial void OnDataInicialChanged(DateTime? value)
    {
        if (_inicializando)
            return;

        if (value.HasValue)
            DataFinal = value.Value.Date.AddDays(30);

        _ = FiltrarComDebounceAsync("A_partir_de");
    }

    partial void OnDataFinalChanged(DateTime? value)
    {
        if (_inicializando)
            return;

        _ = FiltrarComDebounceAsync("ate");
    }

    private async Task AtualizarContasPorEmpresaAsync()
    {
        try
        {
            SelectedConta = null;
            await CarregarContasAsync(SelectedEmpresa?.id_empresa);
            await CarregarContasGridAsync(SelectedEmpresa?.id_empresa);
        }
        catch (Exception ex)
        {
            MessageBox.Show(
                $"Erro ao carregar contas.\n\n{ex.Message}",
                "Erro",
                MessageBoxButton.OK,
                MessageBoxImage.Error
            );
        }
    }

    private async Task FiltrarComDebounceAsync(string controleNome)
    {
        _filtroCts?.Cancel();
        _filtroCts = new CancellationTokenSource();

        var token = _filtroCts.Token;

        try
        {
            await Task.Delay(250, token);

            await AtualizarFluxoAtualAsync(controleNome);
        }
        catch (TaskCanceledException)
        {
        }
        catch (Exception ex)
        {
            MessageBox.Show(
                $"Erro ao filtrar fluxo.\n\n{ex.Message}",
                "Erro",
                MessageBoxButton.OK,
                MessageBoxImage.Error
            );
        }
    }

    private FluxoFiltroDto MontarFiltroAtual(string controleNome)
    {
        DateTime? dataInicio = DataInicial?.Date;
        DateTime? dataFim = DataFinal?.Date;
        var usarData = dataInicio.HasValue && dataFim.HasValue;

        return new FluxoFiltroDto
        {
            ControleNome = controleNome,
            IdEmpresa = SelectedEmpresa?.id_empresa is null
                ? null
                : Convert.ToInt32(SelectedEmpresa.id_empresa),
            Conta = string.IsNullOrWhiteSpace(SelectedConta?.conta)
                ? null
                : SelectedConta.conta,
            DataInicio = usarData ? dataInicio : null,
            DataFim = usarData ? dataFim : null,
        };
    }

    private async Task AtualizarFluxoAtualAsync(string controleNome)
    {
        var filtro = MontarFiltroAtual(controleNome);

        if (!filtro.TemFiltro)
        {
            if (_carregarInicialSemFiltro)
            {
                await CarregarTodosLancamentosAsync();
                return;
            }

            Fluxos.Clear();
            SaldoAnterior = 0;
            Saldo = 0;
            return;
        }

        var resultado = await _repository.AtualizarTelaAsync(filtro);

        SaldoAnterior = resultado.Saldo.SaldoAnterior;
        Saldo = resultado.Saldo.SaldoAtual;
        Fluxos = new ObservableCollection<FluxoDto>(resultado.Fluxos);
    }

    private static FluxoModel MapearFluxo(FluxoDto fluxo)
    {
        return new FluxoModel
        {
            linha_fluxo = fluxo.linha_fluxo,
            depto = fluxo.depto,
            classif = fluxo.classif,
            tipo = fluxo.tipo,
            descricao = fluxo.descricao,
            mes = fluxo.mes,
            forma_pagto = fluxo.forma_pagto,
            numero_documento = fluxo.numero_documento,
            data_vencimento = fluxo.data_vencimento,
            data_pagamento = fluxo.data_pagamento,
            debito = fluxo.debito,
            credito = fluxo.credito,
            valor_previsto = fluxo.valor_previsto,
            conta = fluxo.conta,
            saldo_acumulado = fluxo.saldo_acumulado,
            id_compras = fluxo.id_compras,
            sub_classif = fluxo.sub_classif,
            obs = fluxo.obs,
            ok = fluxo.ok,
            alteradopor = fluxo.alteradopor,
            alteradoem = fluxo.alteradoem.HasValue ? new DateTimeOffset(fluxo.alteradoem.Value) : null,
            remover = fluxo.remover,
            previsao = fluxo.previsao,
            parcela = fluxo.parcela,
            data_emissao = fluxo.data_emissao,
            class3 = fluxo.class3,
            razao_social = fluxo.razao_social,
            mes_emissao = fluxo.mes_emissao,
            detalhe_cartao = fluxo.detalhe_cartao,
            cnpj = fluxo.cnpj,
            aprovado = false
        };
    }

    private static bool CpfCnpjValido(string? valor)
    {
        if (string.IsNullOrWhiteSpace(valor))
            return true;

        var digitos = new string(valor.Where(char.IsDigit).ToArray());

        return digitos.Length switch
        {
            11 => CpfValido(digitos),
            14 => CnpjValido(digitos),
            _ => false
        };
    }

    private static bool CpfValido(string cpf)
    {
        if (cpf.Distinct().Count() == 1)
            return false;

        var primeiroDigito = CalcularDigito(cpf[..9], 10);
        var segundoDigito = CalcularDigito(cpf[..9] + primeiroDigito, 11);

        return cpf[9] == primeiroDigito && cpf[10] == segundoDigito;
    }

    private static bool CnpjValido(string cnpj)
    {
        if (cnpj.Distinct().Count() == 1)
            return false;

        var primeiroDigito = CalcularDigito(cnpj[..12], [5, 4, 3, 2, 9, 8, 7, 6, 5, 4, 3, 2]);
        var segundoDigito = CalcularDigito(cnpj[..12] + primeiroDigito, [6, 5, 4, 3, 2, 9, 8, 7, 6, 5, 4, 3, 2]);

        return cnpj[12] == primeiroDigito && cnpj[13] == segundoDigito;
    }

    private static char CalcularDigito(string valor, int pesoInicial)
    {
        var soma = 0;

        for (var i = 0; i < valor.Length; i++)
            soma += (valor[i] - '0') * (pesoInicial - i);

        var resto = soma % 11;
        return resto < 2 ? '0' : (char)('0' + 11 - resto);
    }

    private static char CalcularDigito(string valor, int[] pesos)
    {
        var soma = 0;

        for (var i = 0; i < valor.Length; i++)
            soma += (valor[i] - '0') * pesos[i];

        var resto = soma % 11;
        return resto < 2 ? '0' : (char)('0' + 11 - resto);
    }
}
