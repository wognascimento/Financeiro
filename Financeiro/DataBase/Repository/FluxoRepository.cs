using Dapper;
using Financeiro.DataBase.Models;
using Financeiro.DataBase.Models.Dto;
using Npgsql;
using System.Text;

namespace Financeiro.DataBase.Repository;

public sealed class FluxoRepository(string connectionString)
{
    private readonly string _connectionString = connectionString;

    public async Task<IReadOnlyList<EmpresaModel>> BuscarEmpresasAsync()
    {
        await using var conn = new NpgsqlConnection(_connectionString);

        const string sql = @"
            SELECT id_empresa, nome_empresa
            FROM financeiro.tbl_empresas
            ORDER BY nome_empresa;
        ";

        var dados = await conn.QueryAsync<EmpresaModel>(sql);
        return dados.AsList();
    }

    public async Task<IReadOnlyList<ContaModel>> BuscarContasAsync(long? idEmpresa = null)
    {
        await using var conn = new NpgsqlConnection(_connectionString);

        var sql = new StringBuilder();
        var param = new DynamicParameters();

        sql.AppendLine(@"
             SELECT *
             FROM 
                financeiro.tbl_conta
             WHERE conta IS NOT NULL
        ");

        if (idEmpresa.HasValue)
        {
            sql.AppendLine("AND id_empresa = @IdEmpresa");
            param.Add("@IdEmpresa", idEmpresa.Value);
        }

        sql.AppendLine("ORDER BY conta;");

        var dados = await conn.QueryAsync<ContaModel>(sql.ToString(), param);
        return dados.AsList();
    }

    public async Task<SaldoFluxoDto> MontarSaldoAsync(FluxoFiltroDto filtro)
    {
        if (!filtro.DataInicio.HasValue || !filtro.DataFim.HasValue)
            return new SaldoFluxoDto();

        await using var conn = new NpgsqlConnection(_connectionString);

        const string sql = @"
        SELECT financeiro.fnc_monta_saldo(
            @ControleNome::text,
            @Conta::text,
            @IdEmpresa::integer,
            @DataInicio::date,
            @DataFim::date
        );
    ";

        var retorno = await conn.QueryFirstOrDefaultAsync<double[]>(sql, new
        {
            ControleNome = filtro.ControleNome,
            Conta = string.IsNullOrWhiteSpace(filtro.Conta) ? null : filtro.Conta,
            IdEmpresa = filtro.IdEmpresa.HasValue ? Convert.ToInt32(filtro.IdEmpresa.Value) : (int?)null,
            DataInicio = filtro.DataInicio.Value.Date,
            DataFim = filtro.DataFim.Value.Date
        });

        return new SaldoFluxoDto
        {
            SaldoAnterior = retorno?[0] ?? 0,
            SaldoAtual = retorno?[1] ?? 0
        };
    }

    public async Task<IReadOnlyList<FluxoDto>> CarregarFluxoAsync(FluxoFiltroDto filtro)
    {
        await using var conn = new NpgsqlConnection(_connectionString);

        var sql = new StringBuilder();
        var where = new List<string>();
        var param = new DynamicParameters();

        sql.AppendLine("SELECT *");
        sql.AppendLine("FROM financeiro.qry_fluxo");

        if (filtro.IdEmpresa.HasValue)
        {
            where.Add("id_empresa = @IdEmpresa");
            param.Add("@IdEmpresa", filtro.IdEmpresa.Value);
        }

        if (!string.IsNullOrWhiteSpace(filtro.Conta))
        {
            where.Add("conta = @Conta");
            param.Add("@Conta", filtro.Conta.Trim());
        }

        if (filtro.DataInicio.HasValue && filtro.DataFim.HasValue)
        {
            where.Add("data_pagamento BETWEEN @DataInicio AND @DataFim");
            param.Add("@DataInicio", filtro.DataInicio.Value.Date);
            param.Add("@DataFim", filtro.DataFim.Value.Date);
        }

        if (where.Count > 0)
            sql.AppendLine("WHERE " + string.Join(" AND ", where));

        sql.AppendLine("ORDER BY data_pagamento, linha_fluxo;");

        var dados = await conn.QueryAsync<FluxoDto>(sql.ToString(), param);
        return dados.AsList();
    }

    public async Task<(SaldoFluxoDto Saldo, IReadOnlyList<FluxoDto> Fluxos)> AtualizarTelaAsync(FluxoFiltroDto filtro)
    {
        var saldo = await MontarSaldoAsync(filtro);
        var fluxos = await CarregarFluxoAsync(filtro);

        return (saldo, fluxos);
    }
}
