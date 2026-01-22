using Dapper;
using System.Data;
using Collector.Data.Interfaces;

namespace Collector.Data.Repositories
{
    public class CollectorRepository : ICollectorRepository
    {
        readonly IDbConnection _dbConnection;

        public CollectorRepository(IDbConnection dbConnection)
        {
            _dbConnection = dbConnection;
        }

        public void ResetAllSequences()
        {
            _dbConnection.Execute("[dbo].[ResetAllSequences]", commandType: CommandType.StoredProcedure);
        }
    }
}
