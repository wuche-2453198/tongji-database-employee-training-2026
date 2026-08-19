using Oracle.ManagedDataAccess.Client;
using System.Data;

namespace TrainingManagement.Api.Common
{
    public class OracleConnectionFactory
    {
        private readonly string _connectionString;
        public OracleConnectionFactory(IConfiguration configuration)
        {
            _connectionString = configuration.GetConnectionString("OracleConnection");
        }

        public IDbConnection CreateConnection()
        {
            var connection = new OracleConnection(_connectionString);
            connection.Open();
            using(var cmd = connection.CreateCommand())
            {
                cmd.CommandText = "ALTER SESSION SET CURRENT_SCHEMA = TRAINING_OWNER";
                cmd.ExecuteNonQuery();
            }

            return connection;
        }
    }
}
