namespace Financeiro.DataBase.Models;

public sealed class PagamentoRetencaoModel
{
    public int? id_retencao { get; set; }
    public string? empresa { get; set; }
    public string? id_fornecedor { get; set; }
    public string? nf { get; set; }
    public DateOnly? data_emissao { get; set; }
    public float? inss { get; set; }
    public float? iss { get; set; }
    public float? irrf { get; set; }
    public float? csrf { get; set; }
    public int? id_pedido { get; set; }
    public string? ok_fiscal { get; set; }
    public string? sem_retencao { get; set; }
    public string? cadastradopor { get; set; }
    public string? alteradopor { get; set; }
    public float? valor { get; set; }
    public DateTimeOffset? cadastradoem { get; set; }
    public DateTimeOffset? alteradoem { get; set; }
}
