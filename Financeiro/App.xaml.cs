using Financeiro.DataBase;
using Financeiro.Localization;
using System.Collections.Specialized;
using System.Configuration;
using System.Globalization;
using System.Windows;
using System.Windows.Markup;
using Telerik.Windows.Controls;

namespace Financeiro;

/// <summary>
/// Interaction logic for App.xaml
/// </summary>
public partial class App : Application
{
    public App()
    {
        var appSettings = ConfigurationManager.GetSection("appSettings") as NameValueCollection;

        DataBaseSettings BaseSettings = DataBaseSettings.Instance;
        if (appSettings[0].Length > 0)
            BaseSettings.AppSetting = appSettings;

        BaseSettings.Database = DateTime.Now.Year.ToString();
        BaseSettings.Host = "192.168.0.23";
        BaseSettings.Username = BaseSettings.AppSetting != null ? BaseSettings.AppSetting[0] : Environment.UserName;
        BaseSettings.Password = "123mudar";
        BaseSettings.ConnectionString = $"Host={BaseSettings.Host};Database={BaseSettings.Database};Username={BaseSettings.Username};Password={BaseSettings.Password}";

        LocalizationManager.Manager = new LocalizationManager()
        {
            ResourceManager = GridViewResources.ResourceManager
        };

        this.DispatcherUnhandledException += OnDispatcherUnhandledException;
        AppDomain.CurrentDomain.UnhandledException += OnUnhandledException;
    }

    private void OnDispatcherUnhandledException(object sender, System.Windows.Threading.DispatcherUnhandledExceptionEventArgs e)
    {
        MessageBox.Show("Ocorreu um erro inesperado: " + e.Exception.Message,
                        "Erro", MessageBoxButton.OK, MessageBoxImage.Error);
        e.Handled = true;
    }

    private void OnUnhandledException(object sender, UnhandledExceptionEventArgs e)
    {
        if (e.ExceptionObject is Exception ex)
        {
            MessageBox.Show("Erro fatal: " + ex.Message,
                            "Erro crítico", MessageBoxButton.OK, MessageBoxImage.Stop);
        }
    }

    protected override async void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        CultureInfo culture = new("pt-BR");
        CultureInfo.DefaultThreadCurrentCulture = culture;
        CultureInfo.DefaultThreadCurrentUICulture = culture;

        FrameworkElement.LanguageProperty.OverrideMetadata(
            typeof(FrameworkElement),
            new FrameworkPropertyMetadata(
                XmlLanguage.GetLanguage(culture.IetfLanguageTag)));

    }
}
