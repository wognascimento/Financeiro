using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Dapper;
using Financeiro.DataBase;
using Financeiro.DataBase.Models;
using Financeiro.DataBase.Models.Dto;
using Financeiro.DataBase.Repository;
using Npgsql;
using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Controls;

namespace Financeiro.Views;

/// <summary>
/// Interação lógica para FluxoCaixa.xam
/// </summary>
public partial class FluxoCaixa : UserControl
{
    public FluxoCaixa()
    {
        InitializeComponent();
        DataContext = new FluxoCaixaViewModel();
        //Loaded += FluxoCaixa_Loaded;
    }

    private async void FluxoCaixa_Loaded(object sender, RoutedEventArgs e)
    {
        try
        {
            if (DataContext is FluxoCaixaViewModel vm)
                await vm.CarregarEmpresasAsync();
        }
        catch (PostgresException ex)
        {
            MessageBox.Show($"Erro ao carregar empresas: {ex.Message}", "Erro de Banco de Dados", MessageBoxButton.OK, MessageBoxImage.Error);
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Erro inesperado: {ex.Message}", "Erro", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }


    /*
    private async void RadComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        try
        {
            if (DataContext is FluxoCaixaViewModel vm)
            {
                await vm.CarregarContasAsync(vm.SelectedEmpresa?.id_empresa);
                await vm.CarregarMovimentosFuturosAsync(vm.SelectedEmpresa?.id_empresa);
            }
        }
        catch (PostgresException ex)
        {
            MessageBox.Show($"Erro ao carregar empresas: {ex.Message}", "Erro de Banco de Dados", MessageBoxButton.OK, MessageBoxImage.Error);
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Erro inesperado: {ex.Message}", "Erro", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }
    */
}

public partial class FluxoCaixaViewModel : ObservableObject
{
    private readonly DataBaseSettings BaseSettings = DataBaseSettings.Instance;

    private readonly FluxoRepository _repository;
    private CancellationTokenSource _filtroCts;

    public FluxoCaixaViewModel()
    {
        _repository = new FluxoRepository(BaseSettings.ConnectionString);

        _ = InicializarAsync();
    }

    [ObservableProperty]
    private ObservableCollection<EmpresaModel> empresas = [];

    [ObservableProperty]
    private ObservableCollection<ContaModel> contas = [];

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
    private double saldoAnterior;

    [ObservableProperty]
    private double saldo;

    [ObservableProperty]
    private string descricaoFiltro;

    [ObservableProperty]
    private string cnpjFiltro;

    [ObservableProperty]
    private string formaPagtoFiltro;

    [ObservableProperty]
    private string tipoFiltro;

    [ObservableProperty]
    private string okFiltro;

    private async Task InicializarAsync()
    {
        try
        {
            var empresasDb = await _repository.BuscarEmpresasAsync();
            Empresas = new ObservableCollection<EmpresaModel>(empresasDb);

            //var contasDb = await _repository.BuscarContasAsync();
            //Contas = new ObservableCollection<ContaModel>(contasDb);
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
    }

    public async Task CarregarEmpresasAsync()
    {
        using var conn = new NpgsqlConnection(BaseSettings.ConnectionString);

        var sql = @"SELECT
                        id_empresa,
                        nome_empresa
                    FROM
                        financeiro.tbl_empresas
                    ORDER BY
                        nome_empresa;";
        var itens = await conn.QueryAsync<EmpresaModel>(sql);
        Empresas = new ObservableCollection<EmpresaModel>(itens);
    }

    public async Task CarregarContasAsync(long? idEmpresa)
    {
        using var conn = new NpgsqlConnection(BaseSettings.ConnectionString);

        var sql = @"SELECT *
                    FROM 
                        financeiro.tbl_conta
                    WHERE 
                        id_empresa = @idEmpresa
                    ORDER BY 
                        conta;";
        var itens = await conn.QueryAsync<ContaModel>(sql, new { idEmpresa });
        Contas = new ObservableCollection<ContaModel>(itens);
    }

    public async Task CarregarMovimentosFuturosAsync(long? idEmpresa)
    {
        using var conn = new NpgsqlConnection(BaseSettings.ConnectionString);

        var sql = @"SELECT *
                    FROM 
                        financeiro.qry_fluxo
                    WHERE 
                        id_empresa = @idEmpresa
                    ORDER BY 
                        data_pagamento;";
        var itens = await conn.QueryAsync<FluxoDto>(sql, new { idEmpresa });
        //MovimentosFuturos = new ObservableCollection<FluxoDto>(itens);
    }


    partial void OnSelectedEmpresaChanged(EmpresaModel value)
    {
        _ = AtualizarContasPorEmpresaAsync();
        _ = FiltrarComDebounceAsync("empresa");
    }

    partial void OnSelectedContaChanged(ContaModel value)
    {
        _ = FiltrarComDebounceAsync("conta");
    }

    partial void OnDataInicialChanged(DateTime? value)
    {
        _ = FiltrarComDebounceAsync("A_partir_de");
    }

    partial void OnDataFinalChanged(DateTime? value)
    {
        _ = FiltrarComDebounceAsync("ate");
    }

    private async Task AtualizarContasPorEmpresaAsync()
    {
        try
        {
            var idEmpresa = SelectedEmpresa?.id_empresa;

            SelectedConta = null;

            var contasDb = await _repository.BuscarContasAsync(idEmpresa);
            Contas = new ObservableCollection<ContaModel>(contasDb);
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

            var filtro = MontarFiltroAtual(controleNome);

            if (!filtro.TemFiltro)
            {
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
        var usarData = DataInicial.HasValue && DataFinal.HasValue;

        return new FluxoFiltroDto
        {
            ControleNome = controleNome,

            IdEmpresa = SelectedEmpresa == null
            ? null
            : Convert.ToInt32(SelectedEmpresa.id_empresa),

            Conta = string.IsNullOrWhiteSpace(SelectedConta?.conta)
                ? null
                : SelectedConta.conta,

            DataInicio = usarData ? DataInicial.Value.Date : null,
            DataFim = usarData ? DataFinal.Value.Date : null,
        };
    }



}
