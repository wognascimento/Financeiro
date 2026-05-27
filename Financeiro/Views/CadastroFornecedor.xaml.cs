using CommunityToolkit.Mvvm.ComponentModel;
using Financeiro.DataBase;
using Financeiro.DataBase.Models;
using Financeiro.DataBase.Repository;
using System.Collections.ObjectModel;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json.Serialization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace Financeiro.Views;

/// <summary>
/// Cadastro de fornecedores de compras.
/// </summary>
public partial class CadastroFornecedor : UserControl
{
    public CadastroFornecedor()
    {
        InitializeComponent();
        DataContext = new CadastroFornecedorViewModel();
        Loaded += (_, _) => txtCnpjCpf.Focus();
    }

    private void OnNovoClick(object sender, RoutedEventArgs e)
    {
        if (DataContext is CadastroFornecedorViewModel vm)
        {
            vm.Novo();
            txtCnpjCpf.Focus();
        }
    }

    private async void OnGravarClick(object sender, RoutedEventArgs e)
    {
        if (DataContext is CadastroFornecedorViewModel vm)
        {
            await vm.GravarAsync();
            txtCnpjCpf.Focus();
        }
    }

    private void GridFornecedores_MouseDoubleClick(object sender, MouseButtonEventArgs e)
    {
        if (DataContext is CadastroFornecedorViewModel vm &&
            GridFornecedores.SelectedItem is FornecedorModel fornecedor)
        {
            vm.Editar(fornecedor);
            txtCnpjCpf.Focus();
        }
    }

    private async void CnpjCpf_LostFocus(object sender, RoutedEventArgs e)
    {
        if (DataContext is CadastroFornecedorViewModel vm)
        {
            vm.AtualizarTipoPessoa();
            await vm.BuscarCnpjAsync();
        }
    }

    private async void Cep_LostFocus(object sender, RoutedEventArgs e)
    {
        if (DataContext is CadastroFornecedorViewModel vm)
            await vm.BuscarCepAsync();
    }
}

public partial class CadastroFornecedorViewModel : ObservableObject
{
    private static readonly HttpClient httpClient = new()
    {
        Timeout = TimeSpan.FromSeconds(10)
    };

    private readonly RetencaoFornecedorRepository _repository = new(DataBaseSettings.Instance.ConnectionString ?? string.Empty);

    [ObservableProperty]
    private FornecedorModel fornecedorAtual = new();

    [ObservableProperty]
    private ObservableCollection<FornecedorModel> fornecedores = [];

    [ObservableProperty]
    private bool enderecoManual = true;

    public bool EnderecoSomenteLeitura => !EnderecoManual;

    public CadastroFornecedorViewModel()
    {
        _ = CarregarFornecedoresAsync();
    }

    partial void OnEnderecoManualChanged(bool value)
    {
        OnPropertyChanged(nameof(EnderecoSomenteLeitura));
    }

    public void Novo()
    {
        FornecedorAtual = new FornecedorModel();
        EnderecoManual = true;
    }

    public void Editar(FornecedorModel fornecedor)
    {
        FornecedorAtual = Copiar(fornecedor);
        EnderecoManual = true;
        AtualizarTipoPessoa();
    }

    public void AtualizarTipoPessoa()
    {
        var digitos = SomenteDigitos(FornecedorAtual.cnpj_cpf);

        FornecedorAtual.pessoa_j = digitos.Length switch
        {
            11 => "Física",
            14 => "Jurídica",
            _ => string.Empty
        };

        OnPropertyChanged(nameof(FornecedorAtual));
    }

    public async Task BuscarCepAsync()
    {
        var cep = SomenteDigitos(FornecedorAtual.cep);
        if (cep.Length != 8)
        {
            EnderecoManual = true;
            return;
        }

        try
        {
            var resposta = await httpClient.GetFromJsonAsync<ViaCepResponse>($"https://viacep.com.br/ws/{cep}/json/");

            if (resposta is null || resposta.erro == true || string.IsNullOrWhiteSpace(resposta.logradouro))
            {
                EnderecoManual = true;
                return;
            }

            FornecedorAtual.enderaco = resposta.logradouro;
            FornecedorAtual.bairro = resposta.bairro;
            FornecedorAtual.cidade = resposta.localidade;
            FornecedorAtual.estado = resposta.uf;
            EnderecoManual = false;
            OnPropertyChanged(nameof(FornecedorAtual));
        }
        catch
        {
            EnderecoManual = true;
        }
    }

    public async Task BuscarCnpjAsync()
    {
        var cnpj = SomenteDigitos(FornecedorAtual.cnpj_cpf);
        if (cnpj.Length != 14)
            return;

        try
        {
            var dados = await BuscarOpenCnpjAsync(cnpj) ?? await BuscarBrasilApiAsync(cnpj);
            if (dados is null)
                return;

            FornecedorAtual.razao_social = string.IsNullOrWhiteSpace(dados.RazaoSocial)
                ? FornecedorAtual.razao_social
                : dados.RazaoSocial;

            FornecedorAtual.nomefantasia = string.IsNullOrWhiteSpace(dados.NomeFantasia)
                ? FornecedorAtual.nomefantasia
                : dados.NomeFantasia;

            FornecedorAtual.email = string.IsNullOrWhiteSpace(dados.Email)
                ? FornecedorAtual.email
                : dados.Email;

            FornecedorAtual.fone1 = string.IsNullOrWhiteSpace(dados.Telefone)
                ? FornecedorAtual.fone1
                : dados.Telefone;

            FornecedorAtual.cep = string.IsNullOrWhiteSpace(dados.Cep)
                ? FornecedorAtual.cep
                : dados.Cep;

            FornecedorAtual.enderaco = string.IsNullOrWhiteSpace(dados.Logradouro)
                ? FornecedorAtual.enderaco
                : dados.Logradouro;

            FornecedorAtual.numero = string.IsNullOrWhiteSpace(dados.Numero)
                ? FornecedorAtual.numero
                : dados.Numero;

            FornecedorAtual.complemento = string.IsNullOrWhiteSpace(dados.Complemento)
                ? FornecedorAtual.complemento
                : dados.Complemento;

            FornecedorAtual.bairro = string.IsNullOrWhiteSpace(dados.Bairro)
                ? FornecedorAtual.bairro
                : dados.Bairro;

            FornecedorAtual.cidade = string.IsNullOrWhiteSpace(dados.Cidade)
                ? FornecedorAtual.cidade
                : dados.Cidade;

            FornecedorAtual.estado = string.IsNullOrWhiteSpace(dados.Estado)
                ? FornecedorAtual.estado
                : dados.Estado;

            EnderecoManual =
                string.IsNullOrWhiteSpace(FornecedorAtual.enderaco) ||
                string.IsNullOrWhiteSpace(FornecedorAtual.bairro) ||
                string.IsNullOrWhiteSpace(FornecedorAtual.cidade) ||
                string.IsNullOrWhiteSpace(FornecedorAtual.estado);

            OnPropertyChanged(nameof(FornecedorAtual));
        }
        catch
        {
        }
    }

    private static async Task<CnpjDados?> BuscarOpenCnpjAsync(string cnpj)
    {
        try
        {
            var resposta = await httpClient.GetFromJsonAsync<OpenCnpjResponse>($"https://kitana.opencnpj.com/cnpj/{cnpj}");
            if (resposta?.success != true || resposta.data is null)
                return null;

            return new CnpjDados
            {
                RazaoSocial = resposta.data.razaoSocial,
                NomeFantasia = resposta.data.nomeFantasia,
                Email = resposta.data.email,
                Telefone = resposta.data.telefone,
                Cep = resposta.data.cep,
                Logradouro = resposta.data.logradouro,
                Numero = resposta.data.numero,
                Complemento = resposta.data.complemento,
                Bairro = resposta.data.bairro,
                Cidade = resposta.data.municipio,
                Estado = resposta.data.uf
            };
        }
        catch
        {
            return null;
        }
    }

    private static async Task<CnpjDados?> BuscarBrasilApiAsync(string cnpj)
    {
        try
        {
            var resposta = await httpClient.GetFromJsonAsync<BrasilApiCnpjResponse>($"https://brasilapi.com.br/api/cnpj/v1/{cnpj}");
            if (resposta is null)
                return null;

            return new CnpjDados
            {
                RazaoSocial = resposta.razao_social,
                NomeFantasia = resposta.nome_fantasia,
                Email = resposta.email,
                Telefone = resposta.ddd_telefone_1,
                Cep = resposta.cep,
                Logradouro = resposta.logradouro,
                Numero = resposta.numero,
                Complemento = resposta.complemento,
                Bairro = resposta.bairro,
                Cidade = resposta.municipio,
                Estado = resposta.uf
            };
        }
        catch
        {
            return null;
        }
    }

    public async Task GravarAsync()
    {
        try
        {
            AtualizarTipoPessoa();

            if (string.IsNullOrWhiteSpace(FornecedorAtual.cnpj_cpf))
                throw new InvalidOperationException("Informe o CNPJ/CPF.");

            if (string.IsNullOrWhiteSpace(FornecedorAtual.razao_social))
                throw new InvalidOperationException("Informe a razao social.");

            FornecedorAtual.apelido = FornecedorAtual.nomefantasia;

            if (!FornecedorAtual.idfornecedor.HasValue || FornecedorAtual.idfornecedor.Value == 0)
            {
                FornecedorAtual.cadastro_por = DataBaseSettings.Instance.Username;
                FornecedorAtual.cadastro_em = DateTimeOffset.Now;
                FornecedorAtual.idfornecedor = await _repository.InserirFornecedorAsync(FornecedorAtual);
            }
            else
            {
                FornecedorAtual.alterado_por = DataBaseSettings.Instance.Username;
                FornecedorAtual.alterado_em = DateTimeOffset.Now;
                await _repository.AtualizarFornecedorAsync(FornecedorAtual);
            }

            await CarregarFornecedoresAsync();
            Novo();
        }
        catch (Exception ex)
        {
            MessageBox.Show(
                $"Erro ao gravar fornecedor.\n\n{ex.Message}",
                "Erro",
                MessageBoxButton.OK,
                MessageBoxImage.Error);
        }
    }

    private async Task CarregarFornecedoresAsync()
    {
        try
        {
            var dados = await _repository.CarregarFornecedoresAsync();
            Fornecedores = new ObservableCollection<FornecedorModel>(dados);
        }
        catch (Exception ex)
        {
            MessageBox.Show(
                $"Erro ao carregar fornecedores.\n\n{ex.Message}",
                "Erro",
                MessageBoxButton.OK,
                MessageBoxImage.Error);
        }
    }

    private static FornecedorModel Copiar(FornecedorModel origem)
    {
        return new FornecedorModel
        {
            idfornecedor = origem.idfornecedor,
            nomefantasia = origem.nomefantasia,
            razao_social = origem.razao_social,
            pessoa_f = origem.pessoa_f,
            pessoa_j = origem.pessoa_j,
            cnpj_cpf = origem.cnpj_cpf,
            insc_municipal = origem.insc_municipal,
            insc_estadual = origem.insc_estadual,
            rg = origem.rg,
            enderaco = origem.enderaco,
            bairro = origem.bairro,
            cidade = origem.cidade,
            estado = origem.estado,
            cep = origem.cep,
            numero = origem.numero,
            complemento = origem.complemento,
            site = origem.site,
            email = origem.email,
            fone1 = origem.fone1,
            fone2 = origem.fone2,
            fone3 = origem.fone3,
            fax = origem.fax,
            obs = origem.obs,
            cadastro_por = origem.cadastro_por,
            cadastro_em = origem.cadastro_em,
            alterado_por = origem.alterado_por,
            id_totvs = origem.id_totvs,
            id_sage = origem.id_sage,
            simples = origem.simples,
            alterado_em = origem.alterado_em,
            apelido = origem.apelido
        };
    }

    private static string SomenteDigitos(string? valor)
    {
        return string.IsNullOrWhiteSpace(valor)
            ? string.Empty
            : new string(valor.Where(char.IsDigit).ToArray());
    }

    private sealed class ViaCepResponse
    {
        public string? logradouro { get; set; }
        public string? bairro { get; set; }
        public string? localidade { get; set; }
        public string? uf { get; set; }
        [JsonPropertyName("erro")]
        public bool? erro { get; set; }
    }

    private sealed class CnpjDados
    {
        public string? RazaoSocial { get; set; }
        public string? NomeFantasia { get; set; }
        public string? Email { get; set; }
        public string? Telefone { get; set; }
        public string? Cep { get; set; }
        public string? Logradouro { get; set; }
        public string? Numero { get; set; }
        public string? Complemento { get; set; }
        public string? Bairro { get; set; }
        public string? Cidade { get; set; }
        public string? Estado { get; set; }
    }

    private sealed class OpenCnpjResponse
    {
        public bool? success { get; set; }
        public OpenCnpjData? data { get; set; }
    }

    private sealed class OpenCnpjData
    {
        public string? razaoSocial { get; set; }
        public string? nomeFantasia { get; set; }
        public string? email { get; set; }
        public string? telefone { get; set; }
        public string? cep { get; set; }
        public string? logradouro { get; set; }
        public string? numero { get; set; }
        public string? complemento { get; set; }
        public string? bairro { get; set; }
        public string? municipio { get; set; }
        public string? uf { get; set; }
    }

    private sealed class BrasilApiCnpjResponse
    {
        public string? razao_social { get; set; }
        public string? nome_fantasia { get; set; }
        public string? email { get; set; }
        public string? ddd_telefone_1 { get; set; }
        public string? cep { get; set; }
        public string? logradouro { get; set; }
        public string? numero { get; set; }
        public string? complemento { get; set; }
        public string? bairro { get; set; }
        public string? municipio { get; set; }
        public string? uf { get; set; }
    }
}
