using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Data.SqlClient;

namespace DeveloperJosephBittner.DataMart
{
    public static class DataMartClient
    {
        /// <summary>
        /// Retrieves the first N user tables from the database and returns them as a list.
        /// </summary>
        public static async Task<List<(string Schema, string Name)>> GetTopTablesAsync(string connectionString, int top = 20)
        {
            const string sql = @"SELECT TOP (@top)
                    s.name AS SchemaName,
                    t.name AS TableName
                FROM sys.tables t
                INNER JOIN sys.schemas s ON t.schema_id = s.schema_id
                ORDER BY s.name, t.name";

            var tables = new List<(string Schema, string Name)>();

            await using var connection = new SqlConnection(connectionString);
            await connection.OpenAsync();

            await using var command = new SqlCommand(sql, connection);
            command.Parameters.AddWithValue("@top", top);

            await using var reader = await command.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                tables.Add((reader.GetString(0), reader.GetString(1)));
            }

            return tables;
        }

        /// <summary>
        /// Retrieves the first N user tables from the database and prints them to the console.
        /// </summary>
        public static async Task PrintTopTablesAsync(string connectionString, int top = 20)
        {
            var tables = await GetTopTablesAsync(connectionString, top);

            Console.WriteLine($"Found {tables.Count} tables (top {top}):");
            foreach (var (schema, name) in tables)
            {
                Console.WriteLine($"  {schema}.{name}");
            }
        }

        /// <summary>
        /// Creates a SQL Server connection string targeting the configured server and database.
        /// </summary>
        public static string BuildConnectionString(
            string server = "vFS-SQL02-NS",
            string database = "DeveloperJosephBittner",
            bool trustedConnection = true,
            bool trustServerCertificate = true)
        {
            var builder = new SqlConnectionStringBuilder
            {
                DataSource = server,
                InitialCatalog = database,
                IntegratedSecurity = trustedConnection,
                TrustServerCertificate = trustServerCertificate
            };

            return builder.ConnectionString;
        }
    }
}
