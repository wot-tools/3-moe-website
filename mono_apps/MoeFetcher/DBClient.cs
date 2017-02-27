using WGApi;
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
            Connection.Open();
        }

        ~DBClient()
        {
            Connection.Close();
        }

        public void UpsertPlayer(IEnumerable<Player> players)
        {

        }

        public void UpsertPlayer(Player player, string id, Moe[] moes)
        {
            StringBuilder builder = new StringBuilder();

            string playerFormat = "INSERT INTO T_Players "//(p_account_id, f_clan_id, name, client_lang, "
                //+ "battles, wins, last_battle, account_created, wg_rating, wn8) "
                + "VALUES ('{0}', '{1}', '{2}', '{3}', '{4}', '{5}', '{6}', '{7}', '{8}', '{9}') "
                + "ON DUPLICATE KEY UPDATE p_account_id='{0}', f_clan_id='{1}', name='{2}', client_lang='{3}', "
                + "battles='{4}', wins='{5}', last_battle='{6}', account_created='{7}', wg_rating='{8}', wn8='{9}';";

            string markFormat = "INSERT INTO T_Marks "//(p_f_account_id, p_f_tank_id, battles, damage, spots, kills, decap, cap, wins, marks) "
                + "VALUES ('{0}', '{1}', '0', '0', '0', '0', '0', '0', '0', '3') "
                + "ON DUPLICATE KEY UPDATE p_f_account_id='{0}', p_f_tank_id='{1}', "
                + "battles='0', damage='0', spots='0', kills='0', decap='0', cap='0', wins='0', marks='3';";

            builder.AppendFormat(playerFormat, id, player.ClanID ?? 0, player.Nick, player.ClientLanguage,
                player.Statistics.Random.Battles, player.Statistics.Random.Victories, player.LastBattle, player.AccountCreated, player.WGRating, "wn8");

            foreach (Moe moe in moes)
                builder.AppendFormat(markFormat, id, moe.TankID);

            MySqlCommand command = new MySqlCommand(builder.ToString(), Connection);
            command.ExecuteNonQuery();
            StringBuilder query = new StringBuilder();
        }
    }
}
