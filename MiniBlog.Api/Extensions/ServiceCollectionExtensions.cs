using Microsoft.Data.SqlClient; // using System.Data.SqlClient (Obsolete);
using System.Data; 
using static MiniBlog.Api.Data.MiniBlogContext;

namespace MiniBlog.Api.Extensions
{
    public static class ServiceCollectionExtensions
    {
        // Método de extensão que adiciona serviços customizados ao builder
        public static WebApplicationBuilder AddMiniBlogCustomServices(this WebApplicationBuilder builder)
        {
            // Obtém a string de conexão do arquivo de configuração (appsettings.json)
            var defaultConnection = builder.Configuration.GetConnectionString("DefaultConnection");

            // Adiciona um serviço scoped que fornece uma conexão com o banco de dados
            builder.Services.AddScoped(ProviderConnection(defaultConnection));

            return builder;
        }

        // Função que retorna um delegate para injeção da conexão com base na string de conexão
        private static Func<IServiceProvider, ConfigureDbConnection> ProviderConnection(string? defaultConnection) => serviceProvider => async () => await GetDbConnection(defaultConnection);

        // Método assíncrono que cria e abre uma conexão com o banco de dados
        private static async Task<IDbConnection> GetDbConnection(string? defaultConnection)
        {
            var connection = new SqlConnection(defaultConnection);
            await connection.OpenAsync();

            return connection;
        }
    }
}
