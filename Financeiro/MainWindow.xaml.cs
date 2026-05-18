using BibliotecasSIG;
using Financeiro.DataBase;
using Financeiro.Views;
using System.Collections.Specialized;
using System.Configuration;
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
        private const string UPDATE_URL = "http://192.168.0.49/downloads/financeiro/version.json";
        private readonly string CURRENT_VERSION = Assembly.GetExecutingAssembly().GetName().Version.ToString();

        public MainWindow()
        {
            InitializeComponent();
            StyleManager.ApplicationTheme = new Office2016Theme();

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

        private async Task CheckForUpdatesAsync()
        {
            try
            {
                var updateChecker = new UpdateChecker(UPDATE_URL, CURRENT_VERSION);
                var updateInfo = await updateChecker.CheckForUpdatesAsync();

                var updateInfoJson = JsonSerializer.Serialize<UpdateInfo>(updateInfo);

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
                        var options = new JsonSerializerOptions { WriteIndented = true };
                        string jsonString = JsonSerializer.Serialize(updateInfo, options);
                        string jsonData = JsonSerializer.Serialize(updateInfo);
                        string appName = "Financeiro.exe";
                        string arguments = $"\"{jsonData.Replace("\"", "\\\"")}\" \"{appName}\"";

                        Process.Start("Update.exe", arguments);
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
                var appSettings = ConfigurationManager.GetSection("appSettings") as NameValueCollection;
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
                        BaseSettings.ConnectionString = $"Host={BaseSettings.Host};Database={BaseSettings.Database};Username={BaseSettings.Username};Password={BaseSettings.Password}";
                        radDocking.Items.Clear();
                    }
                }
            });
        }

        private void AbrirFormularioDinamico(Type tipoFormulario, string titulo, object parametro = null)
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
    }
}