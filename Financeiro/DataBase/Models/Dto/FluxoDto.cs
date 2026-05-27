namespace Financeiro.DataBase.Models.Dto;

public class FluxoDto
{
    public int? linha_fluxo { get; set; }
    public string? agencia { get; set; }
    public string? numero_conta { get; set; }
    public string? depto { get; set; }
    public string? classif { get; set; }
    public string? tipo { get; set; }
    public string? descricao { get; set; }
    public string? mes { get; set; }
    public string? forma_pagto { get; set; }
    public string? numero_documento { get; set; }
    public DateOnly? data_vencimento { get; set; }
    public DateOnly? data_pagamento { get; set; }
    public decimal debito { get; set; }
    public decimal credito { get; set; }
    public double valor_previsto { get; set; }
    public string? conta { get; set; }
    public double saldo_acumulado { get; set; }
    public int? id_compras { get; set; }
    public string? sub_classif { get; set; }
    public string? obs { get; set; }
    public string? ok { get; set; }
    public string? cadastradopor { get; set; }
    public DateTime? cadastradoem { get; set; }
    public string? alteradopor { get; set; }
    public DateTime? alteradoem { get; set; }
    public long? id_empresa { get; set; }
    public bool remover { get; set; }
    public string? previsao { get; set; }
    public string? parcela { get; set; }
    public DateOnly? data_emissao { get; set; }
    public string? class3 { get; set; }
    public string? razao_social { get; set; }
    public string? mes_emissao { get; set; }
    public string? detalhe_cartao { get; set; }
    public string? cnpj { get; set; }
}
