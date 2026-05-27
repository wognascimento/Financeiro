namespace Financeiro.DataBase.Models;

public sealed class RetencaoFaturamentoModel
{
    public int? conta { get; set; }
    public int? natal { get; set; }
    public string? codigo_cliente { get; set; }
    public string? nf { get; set; }
    public DateOnly? data_emissao { get; set; }
    public double? valor_contabil { get; set; }
    public DateOnly? data_baixa { get; set; }
    public double? iss_retido { get; set; }
    public double? irpj_retido { get; set; }
    public double? pis_retido { get; set; }
    public double? cofins_retida { get; set; }
    public double? csll_retida { get; set; }
    public string? servico { get; set; }
    public int? id_retencao_faturamento { get; set; }
    public double? inss_retido { get; set; }
}
