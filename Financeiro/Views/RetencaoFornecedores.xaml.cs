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
/// Navegacao de fornecedores e pagamentos de retencao.
/// </summary>
public partial class RetencaoFornecedores : UserControl
{
    public RetencaoFornecedores()
    {
        InitializeComponent();
        DataContext = new RetencaoFornecedoresViewModel();
    }

    private async void Fornecedores_SelectionChanged(object sender, SelectionChangeEventArgs e)
    {
        if (DataContext is RetencaoFornecedoresViewModel vm)
            await vm.CarregarPagamentosFornecedorSelecionadoAsync();
    }

    private void OnAdicionarPagamentoClick(object sender, RoutedEventArgs e)
    {
        GridPagamentos.BeginInsert();
    }

    private void GridPagamentos_AddingNewDataItem(object sender, GridViewAddingNewEventArgs e)
    {
        if (DataContext is RetencaoFornecedoresViewModel vm)
            e.NewObject = vm.CriarNovoPagamento();
    }

    private void GridPagamentos_CellValidating(object sender, GridViewCellValidatingEventArgs e)
    {
        var erro = RetencaoFornecedoresViewModel.ValidarCampoPagamento(e.Cell.Column.UniqueName, e.NewValue);
        if (!string.IsNullOrWhiteSpace(erro))
        {
            e.IsValid = false;
            e.ErrorMessage = erro;
        }
    }

    private async void GridPagamentos_RowValidating(object sender, GridViewRowValidatingEventArgs e)
    {
        if (e.EditOperationType == GridViewEditOperationType.None || e.Row.Item is not PagamentoRetencaoModel pagamento)
            return;

        if (DataContext is not RetencaoFornecedoresViewModel vm)
            return;

        var erros = vm.ValidarPagamento(pagamento);
        if (erros.Count > 0)
        {
            AplicarErrosLinha(e, erros);
            return;
        }

        if (!await vm.SalvarPagamentoAsync(pagamento))
            AplicarErrosLinha(e, ["Nao foi possivel salvar o pagamento no banco de dados."]);
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

public partial class RetencaoFornecedoresViewModel : ObservableObject
{
    private readonly RetencaoFornecedorRepository _repository = new(DataBaseSettings.Instance.ConnectionString ?? string.Empty);

    [ObservableProperty]
    private ObservableCollection<FornecedorModel> fornecedores = [];

    [ObservableProperty]
    private ObservableCollection<PagamentoRetencaoModel> pagamentos = [];

    [ObservableProperty]
    private FornecedorModel? selectedFornecedor;

    public RetencaoFornecedoresViewModel()
    {
        _ = InicializarAsync();
    }

    private async Task InicializarAsync()
    {
        try
        {
            var dados = await _repository.CarregarFornecedoresAsync();
            Fornecedores = new ObservableCollection<FornecedorModel>(dados);
            SelectedFornecedor = Fornecedores.FirstOrDefault();

            if (SelectedFornecedor is not null)
                await CarregarPagamentosFornecedorSelecionadoAsync();
        }
        catch (Exception ex)
        {
            MessageBox.Show(
                $"Erro ao carregar fornecedores.\n\n{ex.Message}",
                "Erro",
                MessageBoxButton.OK,
                MessageBoxImage.Error);
        }
    }

    public async Task CarregarPagamentosFornecedorSelecionadoAsync()
    {
        try
        {
            if (string.IsNullOrWhiteSpace(SelectedFornecedor?.cnpj_cpf))
            {
                Pagamentos.Clear();
                return;
            }

            var dados = await _repository.CarregarPagamentosAsync(SelectedFornecedor.cnpj_cpf);
            Pagamentos = new ObservableCollection<PagamentoRetencaoModel>(dados);
        }
        catch (Exception ex)
        {
            MessageBox.Show(
                $"Erro ao carregar pagamentos do fornecedor.\n\n{ex.Message}",
                "Erro",
                MessageBoxButton.OK,
                MessageBoxImage.Error);
        }
    }

    public PagamentoRetencaoModel CriarNovoPagamento()
    {
        return new PagamentoRetencaoModel
        {
            id_fornecedor = SelectedFornecedor?.cnpj_cpf,
            data_emissao = DateOnly.FromDateTime(DateTime.Today),
            inss = 0,
            iss = 0,
            irrf = 0,
            csrf = 0,
            valor = 0,
            cadastradopor = DataBaseSettings.Instance.Username,
            cadastradoem = DateTimeOffset.Now
        };
    }

    public async Task<bool> SalvarPagamentoAsync(PagamentoRetencaoModel pagamento)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(pagamento.id_fornecedor))
                pagamento.id_fornecedor = SelectedFornecedor?.cnpj_cpf;

            if (string.IsNullOrWhiteSpace(pagamento.id_fornecedor))
                throw new InvalidOperationException("Selecione um fornecedor antes de salvar o pagamento.");

            if (pagamento.id_retencao.HasValue)
            {
                pagamento.alteradopor = DataBaseSettings.Instance.Username;
                pagamento.alteradoem = DateTimeOffset.Now;
                await _repository.AtualizarPagamentoAsync(pagamento);
            }
            else
            {
                pagamento.cadastradopor = DataBaseSettings.Instance.Username;
                pagamento.cadastradoem = DateTimeOffset.Now;
                pagamento.id_retencao = await _repository.InserirPagamentoAsync(pagamento);
            }

            await CarregarPagamentosFornecedorSelecionadoAsync();
            return true;
        }
        catch (Exception ex)
        {
            MessageBox.Show(
                $"Erro ao salvar pagamento. A operacao foi desfeita no banco.\n\n{ex.Message}",
                "Erro",
                MessageBoxButton.OK,
                MessageBoxImage.Error);

            await CarregarPagamentosFornecedorSelecionadoAsync();
            return false;
        }
    }

    public List<string> ValidarPagamento(PagamentoRetencaoModel pagamento)
    {
        List<string> erros = [];

        if (string.IsNullOrWhiteSpace(pagamento.id_fornecedor ?? SelectedFornecedor?.cnpj_cpf))
            erros.Add("Selecione um fornecedor antes de salvar o pagamento.");

        if (pagamento.valor < 0)
            erros.Add("Valor nao pode ser negativo.");

        if (pagamento.inss < 0 || pagamento.iss < 0 || pagamento.irrf < 0 || pagamento.csrf < 0)
            erros.Add("Valores de retencao nao podem ser negativos.");

        return erros;
    }

    public static string? ValidarCampoPagamento(string campo, object? valor)
    {
        if (campo is "valor" or "inss" or "iss" or "irrf" or "csrf")
        {
            if (valor is null || !float.TryParse(valor.ToString(), out var numero))
                return "Informe um valor numerico valido.";

            if (numero < 0)
                return "Valor nao pode ser negativo.";
        }

        return null;
    }
}
