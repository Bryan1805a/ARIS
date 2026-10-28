using System;
using MySql.Data.MySqlClient;

namespace ARIS_WinForms
{
    public static class DatabaseConnection
    {
        // Kết nối tới Database ResidenceDB của nhóm
        private static string connectionString = "Server=localhost;Port=3306;Database=ResidenceDB;Uid=root;Pwd=;charset=utf8;";

        public static MySqlConnection GetConnection()
        {
            return new MySqlConnection(connectionString);
        }
    }
}