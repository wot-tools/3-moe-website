using MySql.Data.MySqlClient;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MoE_Console_Application
{
    class MoEMySqlWrapper
    {
        public static MySqlConnection GetMySqlConnection()
        {
            string myConnectionString = "Server=db;Database=moe;Uid=root;Pwd=root";
            //Log.AddInfo($"Connection string: {myConnectionString}");

            //Log.AddInfo("Opening connection...");
            MySqlConnection connection = new MySqlConnection();
            connection.ConnectionString = myConnectionString;
            connection.Open();
            //Log.AddInfo("Opened connection successfully");
            return connection;
        }
    }
}
