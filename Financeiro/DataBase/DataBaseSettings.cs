using System.Collections.Specialized;
using System.Configuration;

namespace Financeiro.DataBase;

public sealed class DataBaseSettings
{
    private static readonly DataBaseSettings instance = new();
    private const string EnvHost = "FINANCEIRO_DB_HOST";
    private const string EnvDatabase = "FINANCEIRO_DB_NAME";
    private const string EnvUsername = "FINANCEIRO_DB_USER";
    private const string EnvPassword = "FINANCEIRO_DB_PASSWORD";

    public string? Host { get; set; }
    public string? Database { get; set; }
    public string? Username { get; set; }
    public string? Password { get; set; }
    public string? ConnectionString { get; set; }
    public NameValueCollection? AppSetting { get; set; }
    public string CaminhoSistema { get; set; } = @"C:\SIG\Financeiro S.I.G\";
    public string? UpdateInfoUrl { get; set; }
    public static DataBaseSettings Instance => instance;

    public void LoadFromConfiguration()
    {
        AppSetting = ConfigurationManager.GetSection("appSettings") as NameValueCollection;

        Host = ReadSetting("DatabaseHost", EnvHost, null);
        Database = ReadSetting("Database", EnvDatabase, DateTime.Now.Year.ToString());
        Username = ReadSetting("Username", EnvUsername, Environment.UserName);
        Password = ReadSetting("Password", EnvPassword, null);
        CaminhoSistema = ReadSetting("SystemPath", null, CaminhoSistema);
        UpdateInfoUrl = ReadSetting("UpdateInfoUrl", null, null);

        RefreshConnectionString();
    }

    public void RefreshConnectionString()
    {
        if (string.IsNullOrWhiteSpace(Host))
            throw new InvalidOperationException("Host do banco de dados nao configurado.");

        if (string.IsNullOrWhiteSpace(Database))
            throw new InvalidOperationException("Nome do banco de dados nao configurado.");

        if (string.IsNullOrWhiteSpace(Username))
            throw new InvalidOperationException("Usuario do banco de dados nao configurado.");

        if (string.IsNullOrWhiteSpace(Password))
            throw new InvalidOperationException("Senha padrao do banco de dados nao configurada.");

        ConnectionString = $"Host={Host};Database={Database};Username={Username};Password={Password}";
    }

    private string ReadSetting(string key, string? environmentVariable, string? defaultValue)
    {
        if (!string.IsNullOrWhiteSpace(environmentVariable))
        {
            var environmentValue = Environment.GetEnvironmentVariable(environmentVariable);
            if (!string.IsNullOrWhiteSpace(environmentValue))
                return environmentValue;
        }

        var configValue = AppSetting?[key];
        return string.IsNullOrWhiteSpace(configValue) ? defaultValue ?? string.Empty : configValue;
    }
}
