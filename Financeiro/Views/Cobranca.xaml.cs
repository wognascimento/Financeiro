using Financeiro.DataBase;
using Financeiro.DataBase.Models.Dto;
using Financeiro.DataBase.Repository;
using Microsoft.Win32;
using System.Collections.ObjectModel;
using System.Globalization;
using System.IO;
using System.Text;
using System.Windows;
using System.Windows.Controls;

namespace Financeiro.Views;

/// <summary>
/// Tela de cobranca em modo somente leitura.
/// </summary>
public partial class Cobranca : UserControl
{
    public Cobranca()
    {
        InitializeComponent();
        DataContext = new CobrancaViewModel();
    }

    private void OnExportarExcelClick(object sender, RoutedEventArgs e)
    {
        if (DataContext is not CobrancaViewModel vm)
            return;

        vm.ExportarParaCsv();
    }
}

public sealed class CobrancaViewModel
{
    private readonly FluxoRepository _repository = new(DataBaseSettings.Instance.ConnectionString ?? string.Empty);

    public ObservableCollection<CobrancaDto> Cobrancas { get; } = [];

    public CobrancaViewModel()
    {
        _ = InicializarAsync();
    }

    private async Task InicializarAsync()
    {
        try
        {
            var dados = await _repository.CarregarCobrancasAsync();
            foreach (var item in dados)
                Cobrancas.Add(item);
        }
        catch (Exception ex)
        {
            MessageBox.Show(
                $"Erro ao carregar cobrancas.\n\n{ex.Message}",
                "Erro",
                MessageBoxButton.OK,
                MessageBoxImage.Error);
        }
    }

    public void ExportarParaCsv()
    {
        try
        {
            SaveFileDialog dialog = new()
            {
                Filter = "Excel CSV (*.csv)|*.csv",
                FileName = $"cobranca-{DateTime.Now:yyyyMMdd-HHmm}.csv",
                AddExtension = true,
                DefaultExt = ".csv"
            };

            if (dialog.ShowDialog() != true)
                return;

            File.WriteAllText(dialog.FileName, GerarCsv(), Encoding.UTF8);

            MessageBox.Show(
                "Arquivo exportado com sucesso.",
                "Exportar Excel",
                MessageBoxButton.OK,
                MessageBoxImage.Information);
        }
        catch (Exception ex)
        {
            MessageBox.Show(
                $"Erro ao exportar para Excel.\n\n{ex.Message}",
                "Erro",
                MessageBoxButton.OK,
                MessageBoxImage.Error);
        }
    }

    private string GerarCsv()
    {
        StringBuilder csv = new();
        csv.AppendLine(string.Join(';', Cabecalhos));

        foreach (var item in Cobrancas)
        {
            string[] valores =
            [
                Formatar(item.conta),
                Formatar(item.depto),
                Formatar(item.classif),
                Formatar(item.sub_classif),
                Formatar(item.class3),
                Formatar(item.ok),
                Formatar(item.razao_social),
                Formatar(item.cnpj),
                Formatar(item.tipo),
                Formatar(item.descricao),
                Formatar(item.previsao),
                Formatar(item.parcela),
                Formatar(item.forma_pagto),
                Formatar(item.numero_documento),
                Formatar(item.data_emissao),
                Formatar(item.data_vencimento),
                Formatar(item.data_pagamento),
                Formatar(item.valor_previsto),
                Formatar(item.credito),
                Formatar(item.debito),
                Formatar(item.obs),
                Formatar(item.cadastradopor),
                Formatar(item.cadastradoem),
                Formatar(item.alteradopor),
                Formatar(item.alteradoem),
                Formatar(item.base_calculo),
                Formatar(item.linha_fluxo)
            ];

            csv.AppendLine(string.Join(';', valores.Select(Escapar)));
        }

        return csv.ToString();
    }

    private static string[] Cabecalhos =>
    [
        "conta", "depto", "classif", "sub_classif", "class3", "ok", "razao_social",
        "cnpj", "tipo", "descricao", "previsao", "parcela", "forma_pagto",
        "numero_documento", "data_emissao", "data_vencimento", "data_pagamento",
        "valor_previsto", "credito", "debito", "obs", "cadastrador", "cadastradoem",
        "alteradopor", "alteradoem", "base_calculo", "linha_fluxo"
    ];

    private static string Formatar(object? valor)
    {
        return valor switch
        {
            null => string.Empty,
            DateOnly data => data.ToString("dd/MM/yyyy", CultureInfo.CurrentCulture),
            DateTime dataHora => dataHora.ToString("dd/MM/yyyy HH:mm:ss", CultureInfo.CurrentCulture),
            DateTimeOffset dataHora => dataHora.ToString("dd/MM/yyyy HH:mm:ss", CultureInfo.CurrentCulture),
            IFormattable formatavel => formatavel.ToString(null, CultureInfo.CurrentCulture),
            _ => valor.ToString() ?? string.Empty
        };
    }

    private static string Escapar(string? valor)
    {
        if (string.IsNullOrEmpty(valor))
            return string.Empty;

        var precisaEscapar = valor.Contains(';') || valor.Contains('"') || valor.Contains('\n') || valor.Contains('\r');
        var tratado = valor.Replace("\"", "\"\"");

        return precisaEscapar ? $"\"{tratado}\"" : tratado;
    }
}
