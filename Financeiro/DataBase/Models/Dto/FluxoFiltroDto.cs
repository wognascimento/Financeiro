namespace Financeiro.DataBase.Models.Dto;

public sealed class FluxoFiltroDto
{
    public string? ControleNome { get; set; }
    public int? IdEmpresa { get; set; }
    public string? Conta { get; set; }
    public DateTime? DataInicio { get; set; }
    public DateTime? DataFim { get; set; }

    public bool TemFiltro =>
        IdEmpresa.HasValue ||
        !string.IsNullOrWhiteSpace(Conta) ||
        (DataInicio.HasValue && DataFim.HasValue);
}
