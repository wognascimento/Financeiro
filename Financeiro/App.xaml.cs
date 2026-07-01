using Financeiro.DataBase;
using Financeiro.Localization;
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
        DataBaseSettings.Instance.LoadFromConfiguration();
        StyleManager.ApplicationTheme = new FluentTheme();
        LocalizationManager.Manager = new LocalizationManager()
        {
            ResourceManager = GridViewResources.ResourceManager
        };

        DispatcherUnhandledException += OnDispatcherUnhandledException;
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
                            "Erro critico", MessageBoxButton.OK, MessageBoxImage.Stop);
        }
    }

    protected override void OnStartup(StartupEventArgs e)
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
