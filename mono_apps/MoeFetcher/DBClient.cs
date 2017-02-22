using MoeFetcher.WgApi;
using MySql.Data.MySqlClient;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MoeFetcher
{
    class DBClient
    {
        private MySqlConnection Connection;
        private string IP;
        private string Database;
        private string User;
        private string Password;

        private string ConnectionString
        {
            get
            {
                return $"server={IP};uid={User};pwd={Password};database={Database}";
            }
        }

        public DBClient(string ip, string user, string password, string database)
        {
            IP = ip;
            User = user;
            Password = password;
            Database = database;
            Connection = new MySqlConnection(ConnectionString);
            Connection.OpenAsync()
        }

        ~DBClient()
        {
            Connection.Close();
        }

        public void UpsertPlayer(IEnumerable<Player> players)
        {

        }

        public void UpsertPlayer(Player player)
        {
            string commandString = 
            MySqlCommand command = new MySqlCommand()
        }
    }
}
