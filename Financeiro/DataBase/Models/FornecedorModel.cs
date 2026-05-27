namespace Financeiro.DataBase.Models;

public sealed class FornecedorModel
{
    public int? idfornecedor { get; set; }
    public string? nomefantasia { get; set; }
    public string? razao_social { get; set; }
    public string? pessoa_f { get; set; }
    public string? pessoa_j { get; set; }
    public string? cnpj_cpf { get; set; }
    public string? insc_municipal { get; set; }
    public string? insc_estadual { get; set; }
    public string? rg { get; set; }
    public string? enderaco { get; set; }
    public string? bairro { get; set; }
    public string? cidade { get; set; }
    public string? estado { get; set; }
    public string? cep { get; set; }
    public string? numero { get; set; }
    public string? complemento { get; set; }
    public string? site { get; set; }
    public string? email { get; set; }
    public string? fone1 { get; set; }
    public string? fone2 { get; set; }
    public string? fone3 { get; set; }
    public string? fax { get; set; }
    public string? obs { get; set; }
    public string? cadastro_por { get; set; }
    public DateTimeOffset? cadastro_em { get; set; }
    public string? alterado_por { get; set; }
    public int? id_totvs { get; set; }
    public int? id_sage { get; set; }
    public string? simples { get; set; }
    public DateTimeOffset? alterado_em { get; set; }
    public string? apelido { get; set; }
}
