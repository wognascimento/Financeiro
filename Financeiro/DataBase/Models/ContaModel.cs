namespace Financeiro.DataBase.Models;

public class ContaModel
{
    public long? id_conta { get; set; }
    public long? id_empresa { get; set; }
    public string? conta { get; set; }
    public string? agencia { get; set; }
    public string? numero_conta { get; set; }
    public string? obs { get; set; }
    public decimal limite { get; set; }
    public DateTime? vencimento_limite { get; set; }
    public string? conta_debito { get; set; }
    public string? cod_banco_matic { get; set; }
    public string? pix { get; set; }
}
