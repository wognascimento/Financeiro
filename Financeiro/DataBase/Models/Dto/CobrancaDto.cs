namespace Financeiro.DataBase.Models.Dto;

public sealed class CobrancaDto
{
    public string? nf_obs { get; set; }
    public double? nf_percent { get; set; }
    public string? nf_tipo { get; set; }
    public int? nf_cod { get; set; }
    public string? depto { get; set; }
    public string? classif { get; set; }
    public string? sub_classif { get; set; }
    public string? tipo { get; set; }
    public string? descricao { get; set; }
    public string? mes { get; set; }
    public string? razao_social { get; set; }
    public string? cnpj { get; set; }
    public string? forma_pagto { get; set; }
    public string? numero_documento { get; set; }
    public DateOnly? data_vencimento { get; set; }
    public DateOnly? data_pagamento { get; set; }
    public decimal debito { get; set; }
    public decimal credito { get; set; }
    public double valor_previsto { get; set; }
    public string? conta { get; set; }
    public string? obs { get; set; }
    public string? ok { get; set; }
    public string? cadastradopor { get; set; }
    public DateTimeOffset? cadastradoem { get; set; }
    public string? alteradopor { get; set; }
    public DateTimeOffset? alteradoem { get; set; }
    public string? parcela { get; set; }
    public DateOnly? data_emissao { get; set; }
    public string? class3 { get; set; }
    public int? linha_fluxo { get; set; }
    public string? previsao { get; set; }
    public double? base_calculo { get; set; }
}
