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
/// Tela de contratos comerciais.
/// </summary>
public partial class Contrato : UserControl
{
    public Contrato()
    {
        InitializeComponent();
        DataContext = new ContratoViewModel();
    }

    private void OnAdicionarContratoClick(object sender, RoutedEventArgs e)
    {
        GridContrato.BeginInsert();
    }

    private void GridContrato_AddingNewDataItem(object sender, GridViewAddingNewEventArgs e)
    {
        e.NewObject = new ContratoModel
        {
            ano = DateTime.Today.Year
        };
    }

    private void GridContrato_CellValidating(object sender, GridViewCellValidatingEventArgs e)
    {
        var erro = ContratoViewModel.ValidarCampoContrato(e.Cell.Column.UniqueName, e.NewValue);
        if (!string.IsNullOrWhiteSpace(erro))
        {
            e.IsValid = false;
            e.ErrorMessage = erro;
        }
    }

    private async void GridContrato_RowValidating(object sender, GridViewRowValidatingEventArgs e)
    {
        if (e.EditOperationType == GridViewEditOperationType.None || e.Row.Item is not ContratoModel contrato)
            return;

        if (DataContext is not ContratoViewModel vm)
            return;

        var erros = vm.ValidarContrato(contrato);
        if (erros.Count > 0)
        {
            AplicarErrosLinha(e, erros);
            return;
        }

        if (!await vm.SalvarContratoAsync(contrato))
            AplicarErrosLinha(e, ["Nao foi possivel salvar o contrato no banco de dados."]);
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

public sealed class ContratoViewModel
{
    private readonly ContratoRepository _repository = new(DataBaseSettings.Instance.ConnectionString ?? string.Empty);

    public ObservableCollection<ContratoModel> Contratos { get; } = [];
    public ObservableCollection<ClienteModel> Clientes { get; } = [];
    public ObservableCollection<TemaModel> Temas { get; } = [];

    public ContratoViewModel()
    {
        _ = CarregarContratosAsync();
    }

    private async Task CarregarContratosAsync()
    {
        try
        {
            Contratos.Clear();
            Clientes.Clear();
            Temas.Clear();

            var clientes = await _repository.BuscarClientesAsync();
            foreach (var item in clientes)
                Clientes.Add(item);

            var temas = await _repository.BuscarTemasAsync();
            foreach (var item in temas)
                Temas.Add(item);

            var dados = await _repository.CarregarContratosAsync();
            foreach (var item in dados)
                Contratos.Add(item);
        }
        catch (Exception ex)
        {
            MessageBox.Show(
                $"Erro ao carregar contratos.\n\n{ex.Message}",
                "Erro",
                MessageBoxButton.OK,
                MessageBoxImage.Error);
        }
    }

    public async Task<bool> SalvarContratoAsync(ContratoModel contrato)
    {
        try
        {
            PreencherNomeFantasia(contrato);

            if (!contrato.ano_original.HasValue ||
                string.IsNullOrWhiteSpace(contrato.sigla_original) ||
                string.IsNullOrWhiteSpace(contrato.sigla_contrato_original))
            {
                await _repository.InserirContratoAsync(contrato);
            }
            else
            {
                await _repository.AtualizarContratoAsync(contrato);
            }

            await CarregarContratosAsync();
            return true;
        }
        catch (Exception ex)
        {
            MessageBox.Show(
                $"Erro ao salvar contrato. A operacao foi desfeita no banco.\n\n{ex.Message}",
                "Erro",
                MessageBoxButton.OK,
                MessageBoxImage.Error);

            await CarregarContratosAsync();
            return false;
        }
    }

    public List<string> ValidarContrato(ContratoModel contrato)
    {
        List<string> erros = [];

        if (contrato.ano <= 0)
            erros.Add("Ano e obrigatorio.");

        if (string.IsNullOrWhiteSpace(contrato.sigla))
            erros.Add("Sigla e obrigatoria.");
        else if (!Clientes.Any(c => string.Equals(c.sigla, contrato.sigla, StringComparison.OrdinalIgnoreCase)))
            erros.Add("Sigla informada nao existe na lista de clientes.");

        if (string.IsNullOrWhiteSpace(contrato.sigla_contrato))
            erros.Add("Sigla contrato e obrigatoria.");

        if (!string.IsNullOrWhiteSpace(contrato.tema) &&
            !Temas.Any(t => string.Equals(t.temas, contrato.tema, StringComparison.OrdinalIgnoreCase)))
            erros.Add("Tema informado nao existe na lista de temas.");

        return erros;
    }

    public static string? ValidarCampoContrato(string campo, object? valor)
    {
        var texto = valor?.ToString();

        return campo switch
        {
            "ano" when valor is null || string.IsNullOrWhiteSpace(texto) => "Ano e obrigatorio.",
            "ano" when !int.TryParse(texto, out var ano) || ano <= 0 => "Ano precisa ser um numero valido.",
            "sigla" when string.IsNullOrWhiteSpace(texto) => "Sigla e obrigatoria.",
            "sigla_contrato" when string.IsNullOrWhiteSpace(texto) => "Sigla contrato e obrigatoria.",
            _ => null
        };
    }

    public void PreencherNomeFantasia(ContratoModel contrato)
    {
        var cliente = Clientes.FirstOrDefault(c =>
            string.Equals(c.sigla, contrato.sigla, StringComparison.OrdinalIgnoreCase));

        if (cliente is not null)
            contrato.nome_fantasia = cliente.nome_fantasia;
    }
}
