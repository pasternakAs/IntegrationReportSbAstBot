using System.Data.Common;

namespace IntegrationReportSbAstBot.Interfaces
{
    public interface IDbConnectionFactory
    {
        DbConnection CreateConnection();
    }
}
