using Financeiro.DataBase.Models.Dto;
using System.Windows;
using System.Windows.Controls;
using Telerik.Windows.Controls;
using Telerik.Windows.Controls.GridView;

namespace Financeiro.Views;

/// <summary>
/// Tela de todos os lancamentos financeiros sem filtros iniciais.
/// </summary>
public partial class TodosLancamentos : UserControl
{
    public TodosLancamentos()
    {
        InitializeComponent();
        DataContext = new FluxoCaixaViewModel(carregarInicialSemFiltro: true);
    }

    private void OnAdicionarLancamentoClick(object sender, RoutedEventArgs e)
    {
        GridLancamentos.BeginInsert();
    }

    private void GridLancamentos_AddingNewDataItem(object sender, GridViewAddingNewEventArgs e)
    {
        if (DataContext is FluxoCaixaViewModel vm)
            e.NewObject = vm.CriarNovoFluxo();
    }

    private void GridLancamentos_CellValidating(object sender, GridViewCellValidatingEventArgs e)
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

    private async void GridLancamentos_RowValidating(object sender, GridViewRowValidatingEventArgs e)
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
            AplicarErrosLinha(e, ["Nao foi possivel salvar o lancamento no banco de dados."]);
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
