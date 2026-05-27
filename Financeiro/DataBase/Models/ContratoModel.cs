namespace Financeiro.DataBase.Models;

public sealed class ContratoModel
{
    public int? ano_original { get; set; }
    public string? sigla_original { get; set; }
    public string? sigla_contrato_original { get; set; }
    public int? ano { get; set; }
    public string? sigla { get; set; }
    public string? sigla_contrato { get; set; }
    public string? revisado_fin { get; set; }
    public string? contrato { get; set; }
    public string? confirmado { get; set; }
    public string? tema { get; set; }
    public string? nome_fantasia { get; set; }
    public DateOnly? data_contrato { get; set; }
    public DateOnly? inicio_montagem { get; set; }
    public DateOnly? final_montagem { get; set; }
    public string? transporte_por_conta { get; set; }
    public string? Moeda { get; set; }
    public int? valor_total_contrato { get; set; }
    public int? valor_contratada { get; set; }
    public int? valor_subcontratada { get; set; }
    public int? valor_venda { get; set; }
    public string? subcontratada { get; set; }
    public string? vendedora { get; set; }
    public string? area_deposito { get; set; }
    public DateOnly? inicio_desmontagem { get; set; }
    public DateOnly? final_desmontagem { get; set; }
    public string? manutencao { get; set; }
    public string? observacoes { get; set; }
    public string? locacao { get; set; }
    public string? venda { get; set; }
    public string? servico { get; set; }
    public string? tipo_servico { get; set; }
    public string? internacional { get; set; }
    public string? corrigir { get; set; }
    public DateOnly? aditivo { get; set; }
    public DateOnly? nemorial { get; set; }
    public DateOnly? conferido { get; set; }
    public DateOnly? remetido { get; set; }
    public DateOnly? assinado { get; set; }
    public string? via_remessa { get; set; }
    public string? apoio { get; set; }
    public string? projeto { get; set; }
}
