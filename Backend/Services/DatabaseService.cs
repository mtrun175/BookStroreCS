using System;
using MySql.Data.MySqlClient;

namespace BookStoreConsoleApp.Services
{
    public class DatabaseService : IDisposable
    {
        private readonly string _connectionString;
        private MySqlConnection _connection;

        public DatabaseService(string connectionString)
        {
            _connectionString = connectionString;
            _connection = new MySqlConnection(_connectionString);
        }

        public void OpenConnection()
        {
            if (_connection.State == System.Data.ConnectionState.Closed)
            {
                _connection.Open();
            }
        }

        public void CloseConnection()
        {
            if (_connection.State == System.Data.ConnectionState.Open)
            {
                _connection.Close();
            }
        }

        public MySqlCommand CreateCommand(string query)
        {
            MySqlCommand cmd = new MySqlCommand(query, _connection);
            return cmd;
        }

        public void Dispose()
        {
            CloseConnection();
            _connection.Dispose();
        }
    }
}
