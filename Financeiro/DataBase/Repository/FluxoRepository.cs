using Dapper;
using Financeiro.DataBase.Models;
using Financeiro.DataBase.Models.Dto;
using Npgsql;
using System.Text;

namespace Financeiro.DataBase.Repository;

public sealed class FluxoRepository(string connectionString)
{
    private readonly string _connectionString = string.IsNullOrWhiteSpace(connectionString)
        ? throw new ArgumentException("Connection string nao informada.", nameof(connectionString))
        : connectionString;

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
             SELECT
                id_conta,
                id_empresa,
                conta,
                agencia,
                numero_conta,
                obs,
                limite,
                vencimento_limite,
                conta_debito,
                cod_banco_matic,
                pix
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

    public async Task<IReadOnlyList<string>> BuscarContasFluxoAsync(long? idEmpresa = null)
    {
        await using var conn = new NpgsqlConnection(_connectionString);

        var sql = new StringBuilder();
        var param = new DynamicParameters();

        sql.AppendLine("SELECT conta FROM financeiro.tbl_conta WHERE conta IS NOT NULL");

        if (idEmpresa.HasValue)
        {
            sql.AppendLine("AND id_empresa = @IdEmpresa");
            param.Add("@IdEmpresa", idEmpresa.Value);
        }

        sql.AppendLine("ORDER BY conta;");

        var dados = await conn.QueryAsync<string>(sql.ToString(), param);
        return dados.AsList();
    }

    public async Task<IReadOnlyList<string>> BuscarDepartamentosAsync()
    {
        await using var conn = new NpgsqlConnection(_connectionString);

        const string sql = @"
            SELECT tipo_depto
            FROM financeiro.tbl_tipo_depto
            WHERE tipo_depto IS NOT NULL
            ORDER BY tipo_depto;
        ";

        var dados = await conn.QueryAsync<string>(sql);
        return dados.AsList();
    }

    public async Task<IReadOnlyList<string>> BuscarTiposAsync()
    {
        await using var conn = new NpgsqlConnection(_connectionString);

        const string sql = @"
            SELECT tipo
            FROM financeiro.tbl_tipo
            WHERE tipo IS NOT NULL
            ORDER BY tipo;
        ";

        var dados = await conn.QueryAsync<string>(sql);
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
            SaldoAnterior = retorno is { Length: > 0 } ? Convert.ToDecimal(retorno[0]) : 0,
            SaldoAtual = retorno is { Length: > 1 } ? Convert.ToDecimal(retorno[1]) : 0
        };
    }

    public async Task<IReadOnlyList<FluxoDto>> CarregarFluxoAsync(FluxoFiltroDto filtro)
    {
        await using var conn = new NpgsqlConnection(_connectionString);

        var sql = new StringBuilder();
        var where = new List<string>();
        var param = new DynamicParameters();

        sql.AppendLine("""
        SELECT
            linha_fluxo,
            agencia,
            numero_conta,
            depto,
            classif,
            tipo,
            descricao,
            mes,
            forma_pagto,
            numero_documento,
            data_vencimento,
            data_pagamento,
            debito,
            credito,
            valor_previsto,
            conta,
            saldo_acumulado,
            id_compras,
            sub_classif,
            obs,
            ok,
            cadastradopor,
            cadastradoem,
            alteradopor,
            alteradoem,
            id_empresa,
            remover,
            previsao,
            parcela,
            data_emissao,
            class3,
            razao_social,
            mes_emissao,
            detalhe_cartao,
            cnpj
        """);
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

    public async Task<IReadOnlyList<CobrancaDto>> CarregarCobrancasAsync()
    {
        await using var conn = new NpgsqlConnection(_connectionString);

        const string sql = """
            SELECT
                nf_obs,
                nf_percent,
                nf_tipo,
                nf_cod,
                depto,
                classif,
                sub_classif,
                tipo,
                descricao,
                mes,
                razao_social,
                cnpj,
                forma_pagto,
                numero_documento,
                data_vencimento,
                data_pagamento,
                debito,
                credito,
                valor_previsto,
                conta,
                obs,
                ok,
                cadastradopor,
                cadastradoem,
                alteradopor,
                alteradoem,
                parcela,
                data_emissao,
                class3,
                linha_fluxo,
                previsao,
                base_calculo
            FROM financeiro.fluxo
            WHERE tipo LIKE 'N%' OR tipo LIKE 'F2%'
            ORDER BY data_pagamento;
        """;

        var dados = await conn.QueryAsync<CobrancaDto>(sql);
        return dados.AsList();
    }

    public async Task<(SaldoFluxoDto Saldo, IReadOnlyList<FluxoDto> Fluxos)> AtualizarTelaAsync(FluxoFiltroDto filtro)
    {
        var saldo = await MontarSaldoAsync(filtro);
        var fluxos = await CarregarFluxoAsync(filtro);

        return (saldo, fluxos);
    }

    public async Task<int> InserirFluxoAsync(FluxoModel fluxo)
    {
        await using var conn = new NpgsqlConnection(_connectionString);
        await conn.OpenAsync();
        await using var transaction = await conn.BeginTransactionAsync();

        try
        {
            const string sql = """
                INSERT INTO financeiro.fluxo (
                    depto, classif, tipo, descricao, mes, forma_pagto, numero_documento,
                    data_vencimento, data_pagamento, debito, credito, valor_previsto, conta,
                    saldo_acumulado, id_rh, id_compras, id_contrato, id_previsao, sub_classif,
                    obs, ok, tipo_fluxo, saldo_aplicacao, cadastradopor, cadastradoem,
                    alteradopor, alteradoem, remover, previsao, parcela, base_calculo, indice,
                    n_pessoas, id_zf1, id_zf2, id_logico_zf, data_emissao, class3, razao_social,
                    nf_obs, nf_percent, nf_tipo, nf_cod, mes_emissao, id_adiantamento,
                    detalhe_cartao, cnpj, conta_credito_folhamatic, tipo_pagto_matic,
                    descricao_fantasia, auditoria, aprovado, aprovado_por, aprovado_em, contabil
                )
                VALUES (
                    @depto, @classif, @tipo, @descricao, @mes, @forma_pagto, @numero_documento,
                    @data_vencimento, @data_pagamento, @debito, @credito, @valor_previsto, @conta,
                    @saldo_acumulado, @id_rh, @id_compras, @id_contrato, @id_previsao, @sub_classif,
                    @obs, @ok, @tipo_fluxo, @saldo_aplicacao, @cadastradopor, @cadastradoem,
                    @alteradopor, @alteradoem, @remover, @previsao, @parcela, @base_calculo, @indice,
                    @n_pessoas, @id_zf1, @id_zf2, @id_logico_zf, @data_emissao, @class3, @razao_social,
                    @nf_obs, @nf_percent, @nf_tipo, @nf_cod, @mes_emissao, @id_adiantamento,
                    @detalhe_cartao, @cnpj, @conta_credito_folhamatic, @tipo_pagto_matic,
                    @descricao_fantasia, @auditoria, @aprovado, @aprovado_por, @aprovado_em, @contabil
                )
                RETURNING linha_fluxo;
            """;

            var linhaFluxo = await conn.ExecuteScalarAsync<int>(sql, fluxo, transaction);
            await transaction.CommitAsync();
            return linhaFluxo;
        }
        catch
        {
            await transaction.RollbackAsync();
            throw;
        }
    }

    public async Task AtualizarFluxoAsync(FluxoModel fluxo)
    {
        if (!fluxo.linha_fluxo.HasValue)
            throw new ArgumentException("Linha do fluxo nao informada para alteracao.", nameof(fluxo));

        await using var conn = new NpgsqlConnection(_connectionString);
        await conn.OpenAsync();
        await using var transaction = await conn.BeginTransactionAsync();

        try
        {
            const string sql = """
                UPDATE financeiro.fluxo
                SET
                    conta = @conta,
                    depto = @depto,
                    classif = @classif,
                    class3 = @class3,
                    tipo = @tipo,
                    ok = @ok,
                    forma_pagto = @forma_pagto,
                    descricao = @descricao,
                    detalhe_cartao = @detalhe_cartao,
                    cnpj = @cnpj,
                    data_emissao = @data_emissao,
                    data_vencimento = @data_vencimento,
                    data_pagamento = @data_pagamento,
                    valor_previsto = @valor_previsto,
                    credito = @credito,
                    debito = @debito,
                    saldo_acumulado = @saldo_acumulado,
                    numero_documento = @numero_documento,
                    id_compras = @id_compras,
                    sub_classif = @sub_classif,
                    obs = @obs,
                    remover = @remover,
                    previsao = @previsao,
                    parcela = @parcela,
                    razao_social = @razao_social,
                    mes_emissao = @mes_emissao,
                    alteradopor = @alteradopor,
                    alteradoem = @alteradoem
                WHERE linha_fluxo = @linha_fluxo;
            """;

            var linhas = await conn.ExecuteAsync(sql, fluxo, transaction);
            if (linhas == 0)
                throw new InvalidOperationException($"Fluxo {fluxo.linha_fluxo} nao encontrado para alteracao.");

            await transaction.CommitAsync();
        }
        catch
        {
            await transaction.RollbackAsync();
            throw;
        }
    }
}
