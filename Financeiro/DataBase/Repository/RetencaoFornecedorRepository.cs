using Dapper;
using Financeiro.DataBase.Models;
using Npgsql;

namespace Financeiro.DataBase.Repository;

public sealed class RetencaoFornecedorRepository(string connectionString)
{
    private const string FornecedorTable = "compras.fornecedores";
    private const string PagamentoTable = "financeiro.tbl_retencoes_pagamentos";

    private readonly string _connectionString = string.IsNullOrWhiteSpace(connectionString)
        ? throw new ArgumentException("Connection string nao informada.", nameof(connectionString))
        : connectionString;

    public async Task<IReadOnlyList<FornecedorModel>> CarregarFornecedoresAsync()
    {
        await using var conn = new NpgsqlConnection(_connectionString);

        var sql = $"""
            SELECT
                idfornecedor, nomefantasia, razao_social, pessoa_f, pessoa_j, cnpj_cpf,
                insc_municipal, insc_estadual, rg, enderaco, bairro, cidade, estado,
                cep, numero, complemento, site, email, fone1, fone2, fone3, fax, obs,
                cadastro_por, cadastro_em, alterado_por, id_totvs, id_sage, simples,
                alterado_em, apelido
            FROM {FornecedorTable}
            ORDER BY razao_social, cnpj_cpf;
        """;

        var dados = await conn.QueryAsync<FornecedorModel>(sql);
        return dados.AsList();
    }

    public async Task<int> InserirFornecedorAsync(FornecedorModel fornecedor)
    {
        await using var conn = new NpgsqlConnection(_connectionString);
        await conn.OpenAsync();
        await using var transaction = await conn.BeginTransactionAsync();

        try
        {
            var sql = $"""
                INSERT INTO {FornecedorTable} (
                    nomefantasia, razao_social, pessoa_f, pessoa_j, cnpj_cpf,
                    insc_municipal, insc_estadual, rg, enderaco, bairro, cidade,
                    estado, cep, numero, complemento, site, email, fone1, fone2,
                    fone3, fax, obs, cadastro_por, cadastro_em, alterado_por,
                    id_totvs, id_sage, simples, alterado_em, apelido
                )
                VALUES (
                    @nomefantasia, @razao_social, @pessoa_f, @pessoa_j, @cnpj_cpf,
                    @insc_municipal, @insc_estadual, @rg, @enderaco, @bairro, @cidade,
                    @estado, @cep, @numero, @complemento, @site, @email, @fone1, @fone2,
                    @fone3, @fax, @obs, @cadastro_por, @cadastro_em, @alterado_por,
                    @id_totvs, @id_sage, @simples, @alterado_em, @apelido
                )
                RETURNING idfornecedor;
            """;

            var id = await conn.ExecuteScalarAsync<int>(sql, fornecedor, transaction);
            await transaction.CommitAsync();
            return id;
        }
        catch
        {
            await transaction.RollbackAsync();
            throw;
        }
    }

    public async Task AtualizarFornecedorAsync(FornecedorModel fornecedor)
    {
        if (!fornecedor.idfornecedor.HasValue || fornecedor.idfornecedor.Value == 0)
            throw new ArgumentException("Id do fornecedor nao informado.", nameof(fornecedor));

        await using var conn = new NpgsqlConnection(_connectionString);
        await conn.OpenAsync();
        await using var transaction = await conn.BeginTransactionAsync();

        try
        {
            var sql = $"""
                UPDATE {FornecedorTable}
                SET
                    nomefantasia = @nomefantasia,
                    razao_social = @razao_social,
                    pessoa_f = @pessoa_f,
                    pessoa_j = @pessoa_j,
                    cnpj_cpf = @cnpj_cpf,
                    enderaco = @enderaco,
                    bairro = @bairro,
                    cidade = @cidade,
                    estado = @estado,
                    cep = @cep,
                    numero = @numero,
                    complemento = @complemento,
                    site = @site,
                    email = @email,
                    fone1 = @fone1,
                    alterado_por = @alterado_por,
                    alterado_em = @alterado_em,
                    apelido = @apelido
                WHERE idfornecedor = @idfornecedor;
            """;

            var linhas = await conn.ExecuteAsync(sql, fornecedor, transaction);
            if (linhas == 0)
                throw new InvalidOperationException("Fornecedor nao encontrado para alteracao.");

            await transaction.CommitAsync();
        }
        catch
        {
            await transaction.RollbackAsync();
            throw;
        }
    }

    public async Task<IReadOnlyList<PagamentoRetencaoModel>> CarregarPagamentosAsync(string cnpjCpfFornecedor)
    {
        await using var conn = new NpgsqlConnection(_connectionString);

        var sql = $"""
            SELECT
                id_retencao, empresa, id_fornecedor, nf, data_emissao, inss, iss,
                irrf, csrf, id_pedido, ok_fiscal, sem_retencao, cadastradopor,
                alteradopor, valor, cadastradoem, alteradoem
            FROM {PagamentoTable}
            WHERE id_fornecedor = @CnpjCpfFornecedor
            ORDER BY data_emissao, nf;
        """;

        var dados = await conn.QueryAsync<PagamentoRetencaoModel>(sql, new
        {
            CnpjCpfFornecedor = cnpjCpfFornecedor
        });

        return dados.AsList();
    }

    public async Task<int> InserirPagamentoAsync(PagamentoRetencaoModel pagamento)
    {
        await using var conn = new NpgsqlConnection(_connectionString);
        await conn.OpenAsync();
        await using var transaction = await conn.BeginTransactionAsync();

        try
        {
            var sql = $"""
                INSERT INTO {PagamentoTable} (
                    empresa, id_fornecedor, nf, data_emissao, inss, iss, irrf, csrf,
                    id_pedido, ok_fiscal, sem_retencao, cadastradopor, alteradopor,
                    valor, cadastradoem, alteradoem
                )
                VALUES (
                    @empresa, @id_fornecedor, @nf, @data_emissao, @inss, @iss, @irrf, @csrf,
                    @id_pedido, @ok_fiscal, @sem_retencao, @cadastradopor, @alteradopor,
                    @valor, @cadastradoem, @alteradoem
                )
                RETURNING id_retencao;
            """;

            var id = await conn.ExecuteScalarAsync<int>(sql, CriarParametros(pagamento), transaction);
            await transaction.CommitAsync();
            return id;
        }
        catch
        {
            await transaction.RollbackAsync();
            throw;
        }
    }

    public async Task AtualizarPagamentoAsync(PagamentoRetencaoModel pagamento)
    {
        if (!pagamento.id_retencao.HasValue)
            throw new ArgumentException("Id da retencao nao informado.", nameof(pagamento));

        await using var conn = new NpgsqlConnection(_connectionString);
        await conn.OpenAsync();
        await using var transaction = await conn.BeginTransactionAsync();

        try
        {
            var sql = $"""
                UPDATE {PagamentoTable}
                SET
                    empresa = @empresa,
                    id_fornecedor = @id_fornecedor,
                    nf = @nf,
                    data_emissao = @data_emissao,
                    inss = @inss,
                    iss = @iss,
                    irrf = @irrf,
                    csrf = @csrf,
                    id_pedido = @id_pedido,
                    ok_fiscal = @ok_fiscal,
                    sem_retencao = @sem_retencao,
                    alteradopor = @alteradopor,
                    valor = @valor,
                    alteradoem = @alteradoem
                WHERE id_retencao = @id_retencao;
            """;

            var linhas = await conn.ExecuteAsync(sql, CriarParametros(pagamento), transaction);
            if (linhas == 0)
                throw new InvalidOperationException("Pagamento nao encontrado para alteracao.");

            await transaction.CommitAsync();
        }
        catch
        {
            await transaction.RollbackAsync();
            throw;
        }
    }

    private static object CriarParametros(PagamentoRetencaoModel pagamento)
    {
        return new
        {
            pagamento.id_retencao,
            pagamento.empresa,
            pagamento.id_fornecedor,
            pagamento.nf,
            data_emissao = pagamento.data_emissao?.ToDateTime(TimeOnly.MinValue),
            pagamento.inss,
            pagamento.iss,
            pagamento.irrf,
            pagamento.csrf,
            pagamento.id_pedido,
            pagamento.ok_fiscal,
            pagamento.sem_retencao,
            pagamento.cadastradopor,
            pagamento.alteradopor,
            pagamento.valor,
            pagamento.cadastradoem,
            pagamento.alteradoem
        };
    }
}
