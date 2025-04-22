using System.Data;

namespace MiniBlog.Api.Data
{
    public class MiniBlogContext
    {
        public delegate Task<IDbConnection> ConfigureDbConnection();
    }
}
