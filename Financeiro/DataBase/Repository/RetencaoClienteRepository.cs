using Dapper;
using Financeiro.DataBase.Models;
using Npgsql;

namespace Financeiro.DataBase.Repository;

public sealed class RetencaoClienteRepository(string connectionString)
{
    private const string ClienteTable = "comercial.clientes";
    private const string RetencaoTable = "financeiro.tbl_retencao_faturamento";

    private readonly string _connectionString = string.IsNullOrWhiteSpace(connectionString)
        ? throw new ArgumentException("Connection string nao informada.", nameof(connectionString))
        : connectionString;

    public async Task<IReadOnlyList<ClienteModel>> CarregarClientesAsync()
    {
        await using var conn = new NpgsqlConnection(_connectionString);

        var sql = $"""
            SELECT
                grupo, sigla, nome, tipo, internacional, rsocial, nome_fantasia, endereco,
                cidade, bairro, est, cep, cep_internacional, praca, regiao, pais, ddi,
                ddd, fone1, fone2, fax, cnpj, inscestad, aniversario, abl, lojas,
                abrasce, website, email, inativo, obsinatvo, atualizacao,
                respatualizacao, pisos, reg_atend, obs, fluxo, referencia_cliente,
                publico, area_construida, publico_mensal, qtdpiso, qtdvao,
                qtdcorredores, area_praca_principal, area_praca_aliment, lojas_ancora,
                ccm, opiniao, publico_classe, publico_sexo, publico_fluxo, distancia,
                totvs, aux, id_cliente, cnpj_retencao, sigla_externa
            FROM {ClienteTable}
            WHERE sigla IS NOT NULL
            ORDER BY sigla, nome;
        """;

        var dados = await conn.QueryAsync<ClienteModel>(sql);
        return dados.AsList();
    }

    public async Task<IReadOnlyList<RetencaoFaturamentoModel>> CarregarRetencoesAsync(string cnpjCliente)
    {
        await using var conn = new NpgsqlConnection(_connectionString);

        var sql = $"""
            SELECT
                conta, natal, codigo_cliente, nf, data_emissao, valor_contabil,
                data_baixa, iss_retido, irpj_retido, pis_retido, cofins_retida,
                csll_retida, servico, id_retencao_faturamento, inss_retido
            FROM {RetencaoTable}
            WHERE codigo_cliente = @CnpjCliente
            ORDER BY data_emissao, nf;
        """;

        var dados = await conn.QueryAsync<RetencaoFaturamentoModel>(sql, new
        {
            CnpjCliente = cnpjCliente
        });

        return dados.AsList();
    }

    public async Task<int> InserirRetencaoAsync(RetencaoFaturamentoModel retencao)
    {
        await using var conn = new NpgsqlConnection(_connectionString);
        await conn.OpenAsync();
        await using var transaction = await conn.BeginTransactionAsync();

        try
        {
            var sql = $"""
                INSERT INTO {RetencaoTable} (
                    conta, natal, codigo_cliente, nf, data_emissao, valor_contabil,
                    data_baixa, iss_retido, irpj_retido, pis_retido, cofins_retida,
                    csll_retida, servico, inss_retido
                )
                VALUES (
                    @conta, @natal, @codigo_cliente, @nf, @data_emissao, @valor_contabil,
                    @data_baixa, @iss_retido, @irpj_retido, @pis_retido, @cofins_retida,
                    @csll_retida, @servico, @inss_retido
                )
                RETURNING id_retencao_faturamento;
            """;

            var id = await conn.ExecuteScalarAsync<int>(sql, CriarParametros(retencao), transaction);
            await transaction.CommitAsync();
            return id;
        }
        catch
        {
            await transaction.RollbackAsync();
            throw;
        }
    }

    public async Task AtualizarRetencaoAsync(RetencaoFaturamentoModel retencao)
    {
        if (!retencao.id_retencao_faturamento.HasValue)
            throw new ArgumentException("Id da retencao de faturamento nao informado.", nameof(retencao));

        await using var conn = new NpgsqlConnection(_connectionString);
        await conn.OpenAsync();
        await using var transaction = await conn.BeginTransactionAsync();

        try
        {
            var sql = $"""
                UPDATE {RetencaoTable}
                SET
                    conta = @conta,
                    natal = @natal,
                    codigo_cliente = @codigo_cliente,
                    nf = @nf,
                    data_emissao = @data_emissao,
                    valor_contabil = @valor_contabil,
                    data_baixa = @data_baixa,
                    iss_retido = @iss_retido,
                    irpj_retido = @irpj_retido,
                    pis_retido = @pis_retido,
                    cofins_retida = @cofins_retida,
                    csll_retida = @csll_retida,
                    servico = @servico,
                    inss_retido = @inss_retido
                WHERE id_retencao_faturamento = @id_retencao_faturamento;
            """;

            var linhas = await conn.ExecuteAsync(sql, CriarParametros(retencao), transaction);
            if (linhas == 0)
                throw new InvalidOperationException("Retencao de faturamento nao encontrada para alteracao.");

            await transaction.CommitAsync();
        }
        catch
        {
            await transaction.RollbackAsync();
            throw;
        }
    }

    private static object CriarParametros(RetencaoFaturamentoModel retencao)
    {
        return new
        {
            retencao.conta,
            retencao.natal,
            retencao.codigo_cliente,
            retencao.nf,
            data_emissao = retencao.data_emissao?.ToDateTime(TimeOnly.MinValue),
            retencao.valor_contabil,
            data_baixa = retencao.data_baixa?.ToDateTime(TimeOnly.MinValue),
            retencao.iss_retido,
            retencao.irpj_retido,
            retencao.pis_retido,
            retencao.cofins_retida,
            retencao.csll_retida,
            retencao.servico,
            retencao.id_retencao_faturamento,
            retencao.inss_retido
        };
    }
}
