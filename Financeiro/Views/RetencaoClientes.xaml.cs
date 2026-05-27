using CommunityToolkit.Mvvm.ComponentModel;
using Financeiro.DataBase;
using Financeiro.DataBase.Models;
using Financeiro.DataBase.Repository;
using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Controls;
using Telerik.Windows.Controls;
using Telerik.Windows.Controls.GridView;

namespace Financeiro.Views;

/// <summary>
/// Navegacao de clientes e retencoes de faturamento.
/// </summary>
public partial class RetencaoClientes : UserControl
{
    public RetencaoClientes()
    {
        InitializeComponent();
        DataContext = new RetencaoClientesViewModel();
    }

    private async void Clientes_SelectionChanged(object sender, SelectionChangeEventArgs e)
    {
        if (DataContext is RetencaoClientesViewModel vm)
            await vm.CarregarRetencoesClienteSelecionadoAsync();
    }

    private void OnAdicionarRetencaoClick(object sender, RoutedEventArgs e)
    {
        GridRetencoes.BeginInsert();
    }

    private void GridRetencoes_AddingNewDataItem(object sender, GridViewAddingNewEventArgs e)
    {
        if (DataContext is RetencaoClientesViewModel vm)
            e.NewObject = vm.CriarNovaRetencao();
    }

    private void GridRetencoes_CellValidating(object sender, GridViewCellValidatingEventArgs e)
    {
        var erro = RetencaoClientesViewModel.ValidarCampoRetencao(e.Cell.Column.UniqueName, e.NewValue);
        if (!string.IsNullOrWhiteSpace(erro))
        {
            e.IsValid = false;
            e.ErrorMessage = erro;
        }
    }

    private async void GridRetencoes_RowValidating(object sender, GridViewRowValidatingEventArgs e)
    {
        if (e.EditOperationType == GridViewEditOperationType.None || e.Row.Item is not RetencaoFaturamentoModel retencao)
            return;

        if (DataContext is not RetencaoClientesViewModel vm)
            return;

        var erros = vm.ValidarRetencao(retencao);
        if (erros.Count > 0)
        {
            AplicarErrosLinha(e, erros);
            return;
        }

        if (!await vm.SalvarRetencaoAsync(retencao))
            AplicarErrosLinha(e, ["Nao foi possivel salvar a retencao no banco de dados."]);
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

public partial class RetencaoClientesViewModel : ObservableObject
{
    private readonly RetencaoClienteRepository _repository = new(DataBaseSettings.Instance.ConnectionString ?? string.Empty);

    [ObservableProperty]
    private ObservableCollection<ClienteModel> clientes = [];

    [ObservableProperty]
    private ObservableCollection<RetencaoFaturamentoModel> retencoes = [];

    [ObservableProperty]
    private ClienteModel? selectedCliente;

    public RetencaoClientesViewModel()
    {
        _ = InicializarAsync();
    }

    private async Task InicializarAsync()
    {
        try
        {
            var dados = await _repository.CarregarClientesAsync();
            Clientes = new ObservableCollection<ClienteModel>(dados);
            SelectedCliente = Clientes.FirstOrDefault();

            if (SelectedCliente is not null)
                await CarregarRetencoesClienteSelecionadoAsync();
        }
        catch (Exception ex)
        {
            MessageBox.Show(
                $"Erro ao carregar clientes.\n\n{ex.Message}",
                "Erro",
                MessageBoxButton.OK,
                MessageBoxImage.Error);
        }
    }

    public async Task CarregarRetencoesClienteSelecionadoAsync()
    {
        try
        {
            if (string.IsNullOrWhiteSpace(SelectedCliente?.cnpj))
            {
                Retencoes.Clear();
                return;
            }

            var dados = await _repository.CarregarRetencoesAsync(SelectedCliente.cnpj);
            Retencoes = new ObservableCollection<RetencaoFaturamentoModel>(dados);
        }
        catch (Exception ex)
        {
            MessageBox.Show(
                $"Erro ao carregar retencoes do cliente.\n\n{ex.Message}",
                "Erro",
                MessageBoxButton.OK,
                MessageBoxImage.Error);
        }
    }

    public RetencaoFaturamentoModel CriarNovaRetencao()
    {
        return new RetencaoFaturamentoModel
        {
            codigo_cliente = SelectedCliente?.cnpj,
            data_emissao = DateOnly.FromDateTime(DateTime.Today),
            valor_contabil = 0,
            iss_retido = 0,
            irpj_retido = 0,
            pis_retido = 0,
            cofins_retida = 0,
            csll_retida = 0,
            inss_retido = 0
        };
    }

    public async Task<bool> SalvarRetencaoAsync(RetencaoFaturamentoModel retencao)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(retencao.codigo_cliente))
                retencao.codigo_cliente = SelectedCliente?.cnpj;

            if (string.IsNullOrWhiteSpace(retencao.codigo_cliente))
                throw new InvalidOperationException("Selecione um cliente antes de salvar a retencao.");

            if (retencao.id_retencao_faturamento.HasValue)
            {
                await _repository.AtualizarRetencaoAsync(retencao);
            }
            else
            {
                retencao.id_retencao_faturamento = await _repository.InserirRetencaoAsync(retencao);
            }

            await CarregarRetencoesClienteSelecionadoAsync();
            return true;
        }
        catch (Exception ex)
        {
            MessageBox.Show(
                $"Erro ao salvar retencao. A operacao foi desfeita no banco.\n\n{ex.Message}",
                "Erro",
                MessageBoxButton.OK,
                MessageBoxImage.Error);

            await CarregarRetencoesClienteSelecionadoAsync();
            return false;
        }
    }

    public List<string> ValidarRetencao(RetencaoFaturamentoModel retencao)
    {
        List<string> erros = [];

        if (string.IsNullOrWhiteSpace(retencao.codigo_cliente ?? SelectedCliente?.cnpj))
            erros.Add("Selecione um cliente antes de salvar a retencao.");

        if (retencao.valor_contabil < 0)
            erros.Add("Valor contabil nao pode ser negativo.");

        if (retencao.iss_retido < 0 || retencao.irpj_retido < 0 || retencao.pis_retido < 0 ||
            retencao.cofins_retida < 0 || retencao.csll_retida < 0 || retencao.inss_retido < 0)
            erros.Add("Valores de retencao nao podem ser negativos.");

        return erros;
    }

    public static string? ValidarCampoRetencao(string campo, object? valor)
    {
        if (campo is "valor_contabil" or "iss_retido" or "irpj_retido" or "pis_retido" or "cofins_retida" or "csll_retida" or "inss_retido")
        {
            if (valor is null || !double.TryParse(valor.ToString(), out var numero))
                return "Informe um valor numerico valido.";

            if (numero < 0)
                return "Valor nao pode ser negativo.";
        }

        return null;
    }
}
