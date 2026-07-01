using ClosedXML.Excel;
using Dapper;
using Financeiro.DataBase;
using Microsoft.Win32;
using Npgsql;
using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Controls;

namespace Financeiro.Views;

public enum TipoRetencaoExcel
{
    Csrf,
    Inss,
    Irrf,
    Iss
}

public partial class ConsultaRetencaoExcel : UserControl
{
    private readonly ConsultaRetencaoExcelViewModel _viewModel;

    public ConsultaRetencaoExcel(TipoRetencaoExcel tipo)
    {
        InitializeComponent();
        _viewModel = new ConsultaRetencaoExcelViewModel(tipo);
        DataContext = _viewModel;
    }

    private async void OnLoaded(object sender, RoutedEventArgs e)
    {
        await _viewModel.CarregarAsync();
    }

    private void OnExportarExcelClick(object sender, RoutedEventArgs e)
    {
        _viewModel.ExportarParaXlsx();
    }
}

public sealed class ConsultaRetencaoExcelViewModel
{
    private readonly TipoRetencaoExcel _tipo;

    public ObservableCollection<ConsultaRetencaoExcelModel> Retencoes { get; } = [];

    public ConsultaRetencaoExcelViewModel(TipoRetencaoExcel tipo)
    {
        _tipo = tipo;
    }

    public async Task CarregarAsync()
    {
        try
        {
            Retencoes.Clear();

            await using var conn = new NpgsqlConnection(DataBaseSettings.Instance.ConnectionString);
            var dados = await conn.QueryAsync<ConsultaRetencaoExcelModel>(CriarSql());

            foreach (var item in dados)
                Retencoes.Add(item);
        }
        catch (Exception ex)
        {
            MessageBox.Show(
                $"Erro ao carregar retenções.\n\n{ex.Message}",
                "Erro",
                MessageBoxButton.OK,
                MessageBoxImage.Error);
        }
    }

    public void ExportarParaXlsx()
    {
        try
        {
            SaveFileDialog dialog = new()
            {
                Filter = "Excel Workbook (*.xlsx)|*.xlsx",
                FileName = $"{NomeArquivo}-{DateTime.Now:yyyyMMdd-HHmm}.xlsx",
                AddExtension = true,
                DefaultExt = ".xlsx"
            };

            if (dialog.ShowDialog() != true)
                return;

            using var workbook = new XLWorkbook();
            var worksheet = workbook.Worksheets.Add("Retencoes");
            PreencherPlanilha(worksheet);
            workbook.SaveAs(dialog.FileName);

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

    private string CriarSql()
    {
        var colunaRetencao = _tipo switch
        {
            TipoRetencaoExcel.Csrf => "csrf",
            TipoRetencaoExcel.Inss => "inss",
            TipoRetencaoExcel.Irrf => "irrf",
            TipoRetencaoExcel.Iss => "iss",
            _ => throw new InvalidOperationException("Tipo de retenção inválido.")
        };

        return $"""
            SELECT
                pagamento.empresa,
                fornecedor.cnpj_cpf,
                fornecedor.razao_social,
                pagamento.nf,
                pagamento.data_emissao,
                pagamento.valor,
                pagamento.{colunaRetencao} AS retencao,
                pagamento.cadastradoem
            FROM compras.fornecedores fornecedor
            INNER JOIN financeiro.tbl_retencoes_pagamentos pagamento
                ON fornecedor.cnpj_cpf = pagamento.id_fornecedor
            WHERE COALESCE(pagamento.{colunaRetencao}, 0) > 0
            ORDER BY pagamento.data_emissao, fornecedor.razao_social, pagamento.nf;
            """;
    }

    private string NomeArquivo => _tipo switch
    {
        TipoRetencaoExcel.Csrf => "retencao-csrf",
        TipoRetencaoExcel.Inss => "retencao-inss",
        TipoRetencaoExcel.Irrf => "retencao-irrf",
        TipoRetencaoExcel.Iss => "retencao-iss",
        _ => "retencao"
    };

    private void PreencherPlanilha(IXLWorksheet worksheet)
    {
        for (var coluna = 0; coluna < Cabecalhos.Length; coluna++)
        {
            var cell = worksheet.Cell(1, coluna + 1);
            cell.Value = Cabecalhos[coluna];
            cell.Style.Font.Bold = true;
            cell.Style.Fill.BackgroundColor = XLColor.FromHtml("#D9EAF7");
            cell.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
        }

        var linha = 2;

        foreach (var item in Retencoes)
        {
            worksheet.Cell(linha, 1).Value = item.empresa;
            worksheet.Cell(linha, 2).Value = item.cnpj_cpf;
            worksheet.Cell(linha, 3).Value = item.razao_social;
            worksheet.Cell(linha, 4).Value = item.nf;

            if (item.data_emissao.HasValue)
            {
                worksheet.Cell(linha, 5).Value = item.data_emissao.Value.ToDateTime(TimeOnly.MinValue);
                worksheet.Cell(linha, 5).Style.DateFormat.Format = "dd/MM/yyyy";
            }

            worksheet.Cell(linha, 6).Value = item.valor;
            worksheet.Cell(linha, 6).Style.NumberFormat.Format = "#,##0.00";
            worksheet.Cell(linha, 7).Value = item.retencao;
            worksheet.Cell(linha, 7).Style.NumberFormat.Format = "#,##0.00";

            if (item.cadastradoem.HasValue)
            {
                worksheet.Cell(linha, 8).Value = item.cadastradoem.Value;
                worksheet.Cell(linha, 8).Style.DateFormat.Format = "dd/MM/yyyy HH:mm:ss";
            }

            linha++;
        }

        var range = worksheet.Range(1, 1, Math.Max(linha - 1, 1), Cabecalhos.Length);
        range.CreateTable("TabelaRetencoes");
        worksheet.Columns().AdjustToContents();
        worksheet.SheetView.FreezeRows(1);
    }

    private static string[] Cabecalhos => 
    [
        "empresa",
        "cnpj_cpf",
        "razao_social",
        "nf",
        "data_emissao",
        "valor",
        "retencao",
        "cadastradoem"
    ];
}

public sealed class ConsultaRetencaoExcelModel
{
    public string? empresa { get; set; }
    public string? cnpj_cpf { get; set; }
    public string? razao_social { get; set; }
    public string? nf { get; set; }
    public DateOnly? data_emissao { get; set; }
    public double? valor { get; set; }
    public double? retencao { get; set; }
    public DateTime? cadastradoem { get; set; }
}
