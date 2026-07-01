using BibliotecasSIG;
using Financeiro.DataBase;
using Financeiro.Views;
using System.Diagnostics;
using System.Net.Http;
using System.Reflection;
using System.Text.Json;
using System.Windows;
using System.Windows.Controls;
using Telerik.Windows.Controls;

namespace Financeiro
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private DataBaseSettings BaseSettings = DataBaseSettings.Instance;
        private readonly string CURRENT_VERSION = Assembly.GetExecutingAssembly().GetName().Version?.ToString() ?? "0.0.0.0";

        public MainWindow()
        {
            InitializeComponent();
            

            txtUsername.Text = BaseSettings.Username;
            txtDataBase.Text = BaseSettings.Database;

            Loaded += async (s, e) => await InitializeAsync();
        }

        private async Task InitializeAsync()
        {
            try
            {
                await CheckForUpdatesAsync();
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    $"Erro ao inicializar o aplicativo: {ex.Message}",
                    "Erro",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error
                );
            }
        }

        private async Task CheckForUpdatesAsync(bool showUpToDate = false)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(BaseSettings.UpdateInfoUrl))
                    return;

                var updateChecker = new UpdateChecker(BaseSettings.UpdateInfoUrl, CURRENT_VERSION);
                var updateInfo = await updateChecker.CheckForUpdatesAsync();

                if (updateInfo == null)
                {
                    if (showUpToDate)
                        MessageBox.Show($"O sistema já está atualizado.\n\nVersão atual: {CURRENT_VERSION}", "Atualização do sistema", MessageBoxButton.OK, MessageBoxImage.Information);
                    return;
                }

                if (updateInfo != null)
                {
                    var result = MessageBox.Show(
                        $"Nova versão disponível!\n\n" +
                        $"Versão atual: {CURRENT_VERSION}\n" +
                        $"Nova versão: {updateInfo.updateVersion}\n\n" +
                        "Changelog:\n" +
                        string.Join("\n", updateInfo.changelog) +
                        "\n\nDeseja baixar a atualização?",
                        "Atualização Disponível",
                        MessageBoxButton.YesNo,
                        MessageBoxImage.Information
                    );

                    if (result == MessageBoxResult.Yes)
                    {
                        string jsonData = JsonSerializer.Serialize(updateInfo);
                        string appName = "Financeiro.exe";
                        string updateExecutable = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Update.exe");
                        var startInfo = new ProcessStartInfo(updateExecutable);
                        startInfo.ArgumentList.Add(jsonData);
                        startInfo.ArgumentList.Add(appName);

                        Process.Start(startInfo);
                        Application.Current.Shutdown();
                    }
                }
            }
            catch (HttpRequestException ex)
            {
                MessageBox.Show(
                    $"Erro ao verificar atualizações: {ex.Message}",
                    "Erro",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error
                );
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    $"Erro ao verificar atualizações: {ex.Message}",
                    "Erro",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error
                );
            }
        }

        private void OnAlterarUsuarioClick(object sender, RoutedEventArgs e)
        {
            Login window = new();
            window.ShowDialog();
            try
            {
                txtUsername.Text = BaseSettings.Username;
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }

        private void OnAlterarBancoDadosClick(object sender, RoutedEventArgs e)
        {
            RadWindow.Prompt(new DialogParameters()
            {
                Header = "Ano Sistema",
                Content = "Alterar o Ano do Sistema",
                Closed = (sender, e) =>
                {
                    if (e.PromptResult != null)
                    {
                        BaseSettings.Database = e.PromptResult;
                        txtDataBase.Text = BaseSettings.Database;
                        BaseSettings.RefreshConnectionString();
                        radDocking.Items.Clear();
                    }
                }
            });
        }

        private void AbrirFormularioDinamico(Type tipoFormulario, string titulo, object? parametro = null)
        {
            var paneGroup = radDocking.FindChildByType<RadPaneGroup>();

            var paneExistente = paneGroup?.Items.OfType<RadPane>()
                .FirstOrDefault(p => p.Header.ToString() == titulo);

            if (paneExistente != null)
            {
                paneExistente.IsActive = true;
            }
            else
            {
                // Cria instância do formulário
                var formulario = Activator.CreateInstance(tipoFormulario) as UserControl;

                // Se tiver parâmetro, passa via DataContext ou propriedade
                if (parametro != null && formulario != null)
                {
                    formulario.DataContext = parametro;
                }

                var novoPane = new RadPane
                {
                    Header = titulo,
                    CanUserClose = true,
                    Content = formulario,
                    Tag = tipoFormulario.Name // Útil para identificar depois
                };

                paneGroup?.Items.Add(novoPane);
                novoPane.IsActive = true;

                /*
                 * AbrirFormularioDinamico(typeof(MeuFormulario), "Meu Formulário");
                 */

                /*
                 * var dados = new { Id = 123, Nome = "Wesley" };
                 * AbrirFormularioDinamico(typeof(OutroFormulario), "Outro Formulário", dados);
                 */
            }
        }

        private void OnEmpresaClick(object sender, Telerik.Windows.RadRoutedEventArgs e)
        {
            AbrirFormularioDinamico(typeof(FluxoCaixa), "FLUXO DE CAIXA");
        }

        private void OnTodosLancamentosClick(object sender, Telerik.Windows.RadRoutedEventArgs e)
        {
            AbrirFormularioDinamico(typeof(TodosLancamentos), "TODOS LANCAMENTOS");
        }

        private void OnCobrancaClick(object sender, Telerik.Windows.RadRoutedEventArgs e)
        {
            AbrirFormularioDinamico(typeof(Cobranca), "COBRANÇA");
        }

        private void OnContratoClick(object sender, Telerik.Windows.RadRoutedEventArgs e)
        {
            AbrirFormularioDinamico(typeof(Contrato), "CONTRATO");
        }

        private void OnRetencaoFornecedoresClick(object sender, Telerik.Windows.RadRoutedEventArgs e)
        {
            AbrirFormularioDinamico(typeof(RetencaoFornecedores), "RETENÇÕES - FORNECEDORES");
        }

        private void OnRetencaoClientesClick(object sender, Telerik.Windows.RadRoutedEventArgs e)
        {
            AbrirFormularioDinamico(typeof(RetencaoClientes), "RETENÇÕES - CLIENTES");
        }

        private void OnCadastroFornecedorClick(object sender, Telerik.Windows.RadRoutedEventArgs e)
        {
            AbrirFormularioDinamico(typeof(CadastroFornecedor), "CADASTRO DE FORNECEDOR");
        }

        private async void OnAtualizarSistemaClick(object sender, Telerik.Windows.RadRoutedEventArgs e)
        {
            await CheckForUpdatesAsync(true);
        }

        private void OnSobreSistemaClick(object sender, Telerik.Windows.RadRoutedEventArgs e)
        {
            MessageBox.Show($"Sistema Integrado de Gerenciamento - Financeiro\n\nVersão atual: {CURRENT_VERSION}", "Sobre o sistema", MessageBoxButton.OK, MessageBoxImage.Information);
        }
    }
}
