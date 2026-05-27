using Dapper;
using Financeiro.DataBase.Models;
using Npgsql;

namespace Financeiro.DataBase.Repository;

public sealed class ContratoRepository(string connectionString)
{
    private readonly string _connectionString = string.IsNullOrWhiteSpace(connectionString)
        ? throw new ArgumentException("Connection string nao informada.", nameof(connectionString))
        : connectionString;

    public async Task<IReadOnlyList<ContratoModel>> CarregarContratosAsync()
    {
        await using var conn = new NpgsqlConnection(_connectionString);

        const string sql = """
            SELECT
                ano AS ano_original,
                sigla AS sigla_original,
                sigla_contrato AS sigla_contrato_original,
                ano,
                sigla,
                sigla_contrato,
                revisado_fin,
                contrato,
                confirmado,
                tema,
                nome_fantasia,
                data_contrato,
                inicio_montagem,
                final_montagem,
                transporte_por_conta,
                "Moeda" AS Moeda,
                valor_total_contrato,
                valor_contratada,
                valor_subcontratada,
                valor_venda,
                subcontratada,
                vendedora,
                area_deposito,
                inicio_desmontagem,
                final_desmontagem,
                manutencao,
                observacoes,
                locacao,
                venda,
                servico,
                tipo_servico,
                internacional,
                corrigir,
                aditivo,
                nemorial,
                conferido,
                remetido,
                assinado,
                via_remessa,
                apoio,
                projeto
            FROM comercial.contrato
            ORDER BY ano DESC, sigla, sigla_contrato, contrato;
        """;

        var dados = await conn.QueryAsync<ContratoModel>(sql);
        return dados.AsList();
    }

    public async Task<IReadOnlyList<ClienteModel>> BuscarClientesAsync()
    {
        await using var conn = new NpgsqlConnection(_connectionString);

        const string sql = """
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
            FROM comercial.clientes
            WHERE sigla IS NOT NULL
            ORDER BY sigla;
        """;

        var dados = await conn.QueryAsync<ClienteModel>(sql);
        return dados.AsList();
    }

    public async Task<IReadOnlyList<TemaModel>> BuscarTemasAsync()
    {
        await using var conn = new NpgsqlConnection(_connectionString);

        const string sql = """
            SELECT
                ano, tipos, caracteristicas, temas, atalho, cod_tema, catalogo2006,
                dir_ideia, dir_criacao, assist_tema, palavra_chave, ativo, inic,
                video, novo, descricao, preco, idealizador, obs, temas_novos_2007,
                local_tema, inserido_por, data_cadastro, nome_ingles, nome_espanhol,
                faixapreco, indiceproposta, briefing, idvalor, estoque, pedido,
                aprovado, fichatema, data_atualizacao, status, idtema, alterado_por,
                data_altera, nome_fantasia, composto, kit_fotos, planta_referencia,
                reuniao_conceito, orient_catalogo, exibe_catalogo, maquete,
                tema_interno, obs_tema_interno, construcao_tipo, construcao_principal,
                interatividade_principal, cor_predominante, animatronico, conceito_2,
                conceito_3, linha, codinome
            FROM comercial.temas
            WHERE temas IS NOT NULL
            ORDER BY temas;
        """;

        var dados = await conn.QueryAsync<TemaModel>(sql);
        return dados.AsList();
    }

    public async Task InserirContratoAsync(ContratoModel contrato)
    {
        await using var conn = new NpgsqlConnection(_connectionString);
        await conn.OpenAsync();
        await using var transaction = await conn.BeginTransactionAsync();

        try
        {
            const string sql = """
                INSERT INTO comercial.contrato (
                    ano, sigla, sigla_contrato, revisado_fin, contrato, confirmado, tema,
                    nome_fantasia, data_contrato, inicio_montagem, final_montagem,
                    transporte_por_conta, "Moeda", valor_total_contrato, valor_contratada,
                    valor_subcontratada, valor_venda, subcontratada, vendedora, area_deposito,
                    inicio_desmontagem, final_desmontagem, manutencao, observacoes, locacao,
                    venda, servico, tipo_servico, internacional, corrigir, aditivo, nemorial,
                    conferido, remetido, assinado, via_remessa, apoio, projeto
                )
                VALUES (
                    @ano, @sigla, @sigla_contrato, @revisado_fin, @contrato, @confirmado, @tema,
                    @nome_fantasia, @data_contrato, @inicio_montagem, @final_montagem,
                    @transporte_por_conta, @Moeda, @valor_total_contrato, @valor_contratada,
                    @valor_subcontratada, @valor_venda, @subcontratada, @vendedora, @area_deposito,
                    @inicio_desmontagem, @final_desmontagem, @manutencao, @observacoes, @locacao,
                    @venda, @servico, @tipo_servico, @internacional, @corrigir, @aditivo, @nemorial,
                    @conferido, @remetido, @assinado, @via_remessa, @apoio, @projeto
                );
            """;

            await conn.ExecuteAsync(sql, CriarParametros(contrato), transaction);
            await transaction.CommitAsync();
        }
        catch
        {
            await transaction.RollbackAsync();
            throw;
        }
    }

    public async Task AtualizarContratoAsync(ContratoModel contrato)
    {
        if (!contrato.ano_original.HasValue ||
            string.IsNullOrWhiteSpace(contrato.sigla_original) ||
            string.IsNullOrWhiteSpace(contrato.sigla_contrato_original))
        {
            throw new ArgumentException("Chaves originais do contrato nao informadas.", nameof(contrato));
        }

        await using var conn = new NpgsqlConnection(_connectionString);
        await conn.OpenAsync();
        await using var transaction = await conn.BeginTransactionAsync();

        try
        {
            const string sql = """
                UPDATE comercial.contrato
                SET
                    ano = @ano,
                    sigla = @sigla,
                    sigla_contrato = @sigla_contrato,
                    contrato = @contrato,
                    confirmado = @confirmado,
                    tema = @tema,
                    nome_fantasia = @nome_fantasia,
                    data_contrato = @data_contrato,
                    inicio_montagem = @inicio_montagem,
                    final_montagem = @final_montagem,
                    transporte_por_conta = @transporte_por_conta,
                    valor_contratada = @valor_contratada,
                    inicio_desmontagem = @inicio_desmontagem,
                    final_desmontagem = @final_desmontagem,
                    observacoes = @observacoes,
                    aditivo = @aditivo,
                    remetido = @remetido,
                    assinado = @assinado
                WHERE ano = @ano_original
                  AND sigla = @sigla_original
                  AND sigla_contrato = @sigla_contrato_original;
            """;

            var linhas = await conn.ExecuteAsync(sql, CriarParametros(contrato), transaction);
            if (linhas == 0)
                throw new InvalidOperationException("Contrato nao encontrado para alteracao.");

            await transaction.CommitAsync();
        }
        catch
        {
            await transaction.RollbackAsync();
            throw;
        }
    }

    private static object CriarParametros(ContratoModel contrato)
    {
        return new
        {
            contrato.ano,
            contrato.ano_original,
            contrato.sigla,
            contrato.sigla_original,
            contrato.sigla_contrato,
            contrato.sigla_contrato_original,
            contrato.revisado_fin,
            contrato.contrato,
            contrato.confirmado,
            contrato.tema,
            contrato.nome_fantasia,
            data_contrato = ToDateTime(contrato.data_contrato),
            inicio_montagem = ToDateTime(contrato.inicio_montagem),
            final_montagem = ToDateTime(contrato.final_montagem),
            contrato.transporte_por_conta,
            contrato.Moeda,
            contrato.valor_total_contrato,
            contrato.valor_contratada,
            contrato.valor_subcontratada,
            contrato.valor_venda,
            contrato.subcontratada,
            contrato.vendedora,
            contrato.area_deposito,
            inicio_desmontagem = ToDateTime(contrato.inicio_desmontagem),
            final_desmontagem = ToDateTime(contrato.final_desmontagem),
            contrato.manutencao,
            contrato.observacoes,
            contrato.locacao,
            contrato.venda,
            contrato.servico,
            contrato.tipo_servico,
            contrato.internacional,
            contrato.corrigir,
            aditivo = ToDateTime(contrato.aditivo),
            nemorial = ToDateTime(contrato.nemorial),
            conferido = ToDateTime(contrato.conferido),
            remetido = ToDateTime(contrato.remetido),
            assinado = ToDateTime(contrato.assinado),
            contrato.via_remessa,
            contrato.apoio,
            contrato.projeto
        };
    }

    private static DateTime? ToDateTime(DateOnly? data)
    {
        return data?.ToDateTime(TimeOnly.MinValue);
    }
}
