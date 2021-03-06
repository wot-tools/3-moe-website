﻿using WGApi;
using MySql.Data.MySqlClient;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;

namespace MoeFetcher
{
    class DBClient
    {
        private string IP;
        private string Database;
        private string User;
        private string Password;
        private ILogger Logger;
        private Setting Settings;
        private string BasePath;
        private HashSet<int> AvailableTankIDs;

        private string ConnectionString
        {
            get
            {
                return $"server={IP};uid={User};pwd={Password};database={Database}";
            }
        }

        public DBClient(string ip, string user, string password, string database, ILogger logger, Setting settings)
        {
            IP = ip;
            User = user;
            Password = password;
            Database = database;
            Logger = logger;
            Settings = settings;

            AvailableTankIDs = new HashSet<int>();

            Uri baseUri = new Uri(System.Reflection.Assembly.GetExecutingAssembly().CodeBase);
            BasePath = System.Net.WebUtility.UrlDecode(Path.GetDirectoryName(baseUri.AbsolutePath));
        }

        public void UpsertTanks(Dictionary<int, Tank> tankDict)
        {
            using (MySqlConnection Connection = new MySqlConnection(ConnectionString))
            {
                Connection.Open();

                MySqlCommand command = GetTankInsertCommand(Connection);

                command.Parameters.AddWithValue("@id", 1);
                command.Parameters.AddWithValue("@name", "1");
                command.Parameters.AddWithValue("@nameshort", "1");
                command.Parameters.AddWithValue("@tierid", 1);
                command.Parameters.AddWithValue("@vehicletypeid", "1");
                command.Parameters.AddWithValue("@nationid", "1");
                command.Parameters.AddWithValue("@ispremium", true);
                command.Parameters.AddWithValue("@bigicon", "1");
                command.Parameters.AddWithValue("@smallicon", "1");
                command.Parameters.AddWithValue("@contouricon", "1");
                command.Parameters.AddWithValue("@now", DateTime.Now);

                foreach (var keyValuePair in tankDict)
                {
                    if (keyValuePair.Value.Tier >= 5)
                    {
                        AvailableTankIDs.Add(keyValuePair.Key);

                        command.Parameters["@id"].Value = keyValuePair.Key;
                        command.Parameters["@name"].Value = keyValuePair.Value.Name;
                        command.Parameters["@nameshort"].Value = keyValuePair.Value.ShortName;
                        command.Parameters["@tierid"].Value = keyValuePair.Value.Tier;
                        command.Parameters["@vehicletypeid"].Value = keyValuePair.Value.VehicleType;
                        command.Parameters["@nationid"].Value = keyValuePair.Value.Nation.ToString().ToLower();
                        command.Parameters["@ispremium"].Value = keyValuePair.Value.IsPremium;
                        command.Parameters["@bigicon"].Value = keyValuePair.Value.Icons.Big;
                        command.Parameters["@smallicon"].Value = keyValuePair.Value.Icons.Small;
                        command.Parameters["@contouricon"].Value = keyValuePair.Value.Icons.Contour;
                        command.Parameters["@now"].Value = DateTime.Now;

                        command.ExecuteNonQuery();
                    }
                }
            }
        }

        public void UpsertTank(Tank tank, int id)
        {
            using (MySqlConnection Connection = new MySqlConnection(ConnectionString))
            {
                Connection.Open();
                MySqlCommand command = GetTankInsertCommand(Connection);

                command.Parameters.AddWithValue("@id", id);
                command.Parameters.AddWithValue("@name", tank.Name);
                command.Parameters.AddWithValue("@nameshort", tank.ShortName);
                command.Parameters.AddWithValue("@tierid", tank.Tier);
                command.Parameters.AddWithValue("@vehicletypeid", tank.VehicleType);
                command.Parameters.AddWithValue("@nationid", tank.Nation.ToString().ToLower());
                command.Parameters.AddWithValue("@ispremium", tank.IsPremium);
                command.Parameters.AddWithValue("@bigicon", tank.Icons.Big);
                command.Parameters.AddWithValue("@smallicon", tank.Icons.Small);
                command.Parameters.AddWithValue("@contouricon", tank.Icons.Contour);
                command.Parameters.AddWithValue("@now", DateTime.Now);
                                
                command.ExecuteNonQuery();
                AvailableTankIDs.Add(id);
            }
        }

        private MySqlCommand GetTankInsertCommand(MySqlConnection connection)
        {
            MySqlCommand command = new MySqlCommand()
            {
                Connection = connection,
                CommandText = "INSERT INTO tanks (id, name, shortname, tier_id, vehicle_type_id, nation_id, ispremium, bigicon, contouricon, smallicon, created_at, updated_at) "
                            + "VALUES (@id, @name, @nameshort, @tierid, @vehicletypeid, @nationid, @ispremium, @bigicon, @contouricon, @smallicon, @now, @now)"
                            + "ON DUPLICATE KEY UPDATE name=@name, shortname=@nameshort, tier_id=@tierid, vehicle_type_id=@vehicletypeid, nation_id=@nationid, "
                            + "ispremium=@ispremium, bigicon=@bigicon, smallicon=@smallicon, updated_at=@now;"
            };

            command.Prepare();

            return command;
        }

        public void UpsertNations(Dictionary<string, string> nations)
        {
            foreach(var entry in nations)
            {
                UpsertNation(entry.Key, entry.Value);
            }
        }

        public void UpsertNation(string id, string name)
        {
            UpsertSimpleItem(id, name, "nations");
        }

        public void UpsertVehicleTypes(Dictionary<string, string> vehicleTypes)
        {
            foreach(var entry in vehicleTypes)
            {
                UpsertVehicleType(entry.Key, entry.Value);
            }
        }

        public void UpsertVehicleType(string id, string name)
        {
            UpsertSimpleItem(id, name, "vehicle_types");
        }

        public void UpsertTiers(IEnumerable<int> tiers)
        {
            foreach(int tier in tiers)
            {
                UpsertTier(tier);
            }
        }

        public void UpsertTier(int tier)
        {
            UpsertSimpleItem(tier, tier.ToString(), "tiers");
        }

        private void UpsertSimpleItem(int id, string name, string tableName)
        {
            using (MySqlConnection Connection = new MySqlConnection(ConnectionString))
            {
                Connection.Open();
                using (MySqlCommand command = GetSimpleInsertCommand(Connection, tableName))
                {
                    command.Connection = Connection;
                    command.Prepare();

                    command.Parameters.AddWithValue("@id", id);
                    command.Parameters.AddWithValue("@name", name);
                    command.Parameters.AddWithValue("@now", DateTime.Now);

                    command.ExecuteNonQuery();
                }
                Connection.Close();
            }
        }

        private void UpsertSimpleItem(string id, string name, string tableName)
        {
            using (MySqlConnection Connection = new MySqlConnection(ConnectionString))
            {
                Connection.Open();
                using (MySqlCommand command = GetSimpleInsertCommand(Connection, tableName))
                {
                    command.Connection = Connection;
                    command.Prepare();

                    command.Parameters.AddWithValue("@id", id);
                    command.Parameters.AddWithValue("@name", name);
                    command.Parameters.AddWithValue("@now", DateTime.Now);

                    command.ExecuteNonQuery();
                }
                Connection.Close();
            }
        }

        private MySqlCommand GetSimpleInsertCommand(MySqlConnection connection, string tableName)
        {
            MySqlCommand command = new MySqlCommand()
            {
                Connection = connection,
                CommandText = $"INSERT INTO {tableName} (id, name, created_at, updated_at) "
                + "VALUES (@id, @name, @now, @now)"
                + "ON DUPLICATE KEY UPDATE name=@name, updated_at=@now;"
            };

            command.Prepare();

            return command;
        }

        public void UpsertClans(Dictionary<int, Clan> clanDict)
        {
            using (MySqlConnection Connection = new MySqlConnection(ConnectionString))
            {
                Connection.Open();
                using (MySqlCommand command = GetClanInsertCommand(Connection))
                {
                    command.Parameters.AddWithValue("@id", 1);
                    command.Parameters.AddWithValue("@name", "1");
                    command.Parameters.AddWithValue("@tag", "1");
                    command.Parameters.AddWithValue("@cHex", "1");
                    command.Parameters.AddWithValue("@members", 1);
                    command.Parameters.AddWithValue("@updatedAtWG", DateTime.Now);
                    command.Parameters.AddWithValue("@clanCreated", DateTime.Now);
                    command.Parameters.AddWithValue("@icon24px", "1");
                    command.Parameters.AddWithValue("@icon32px", "1");
                    command.Parameters.AddWithValue("@icon64px", "1");
                    command.Parameters.AddWithValue("@icon195px", "1");
                    command.Parameters.AddWithValue("@icon256px", "1");
                    command.Parameters.AddWithValue("@now", DateTime.Now);

                    foreach (var keyValuePair in clanDict)
                    {
                        Clan clan = keyValuePair.Value;
                        command.Parameters["@id"].Value = clan.ID;
                        command.Parameters["@name"].Value = clan.Name;
                        command.Parameters["@tag"].Value = clan.Tag;
                        command.Parameters["@cHex"].Value = clan.Color;
                        command.Parameters["@members"].Value = clan.Count;
                        command.Parameters["@updatedAtWG"].Value = clan.UpdatedAt;
                        command.Parameters["@clanCreated"].Value = clan.CreatedAt;
                        command.Parameters["@icon24px"].Value = clan.Emblems["x24"].Portal;
                        command.Parameters["@icon32px"].Value = clan.Emblems["x32"].Portal;
                        command.Parameters["@icon64px"].Value = clan.Emblems["x64"].Portal;
                        command.Parameters["@icon195px"].Value = clan.Emblems["x195"].Portal;
                        command.Parameters["@icon256px"].Value = clan.Emblems["x256"].Wowp;
                        command.Parameters["@now"].Value = DateTime.Now;

                        command.ExecuteNonQuery();
                    }
                }
                Connection.Close();
            }
        }

        public void UpsertClan(Clan clan)
        {
            using (MySqlConnection Connection = new MySqlConnection(ConnectionString))
            {
                Connection.Open();
                using (MySqlCommand command = GetClanInsertCommand(Connection))
                {
                    command.Parameters.AddWithValue("@id", clan.ID);
                    command.Parameters.AddWithValue("@name", clan.Name);
                    command.Parameters.AddWithValue("@tag", clan.Tag);
                    command.Parameters.AddWithValue("@cHex", clan.Color);
                    command.Parameters.AddWithValue("@members", clan.Count);
                    command.Parameters.AddWithValue("@updatedAtWG", clan.UpdatedAt);
                    command.Parameters.AddWithValue("@clanCreated", clan.CreatedAt);
                    command.Parameters.AddWithValue("@icon24px", clan.Emblems["x24"].Portal);
                    command.Parameters.AddWithValue("@icon32px", clan.Emblems["x32"].Portal);
                    command.Parameters.AddWithValue("@icon64px", clan.Emblems["x64"].Portal);
                    command.Parameters.AddWithValue("@icon195px", clan.Emblems["x195"].Portal);
                    command.Parameters.AddWithValue("@icon256px", clan.Emblems["x256"].Wowp);
                    command.Parameters.AddWithValue("@now", DateTime.Now);

                    command.ExecuteNonQuery();
                }
                Connection.Close();
            }
        }

        private MySqlCommand GetClanInsertCommand(MySqlConnection connection)
        {
            MySqlCommand command = new MySqlCommand()
            {
                Connection = connection,
                CommandText = "INSERT INTO clans (id, name, tag, cHex, members, updatedAtWG, clanCreated, icon24px, icon32px, icon64px, icon195px, icon256px, created_at, updated_at) "
                            + "VALUES (@id, @name, @tag, @cHex, @members, @updatedAtWG, @clanCreated, @icon24px, @icon32px, @icon64px, @icon195px, @icon256px, @now, @now) "
                            + "ON DUPLICATE KEY UPDATE name=@name, tag=@tag, cHex=@cHex, members=@members, updatedAtWG=@updatedAtWG, "
                            + "icon24px=@icon24px, icon32px=@icon32px, icon64px=@icon64px, icon195px=@icon195px, icon256px=@icon256px, "
                            + "updated_at=@now;"
            };

            command.Prepare();

            return command;
        }

        public void UpsertPlayers(IEnumerable<Player> players)
        {
            UpsertPlayersWithoutMarks(players);

            foreach (Player player in players)
                UpsertMarks(player.Moes, player.ID);
        }

        public void UpsertPlayer(Player player)
        {
            UpsertPlayerWithoutMarks(player);
            UpsertMarks(player.Moes, player.ID);
        }

        public void UpsertPlayersWithoutMarks(IEnumerable<Player> players)
        {
            using (MySqlConnection Connection = new MySqlConnection(ConnectionString))
            {
                Connection.Open();
                using (MySqlCommand command = GetPlayerInsertCommand(Connection))
                {
                    command.Parameters.AddWithValue("@id", 1);
                    command.Parameters.AddWithValue("@name", "1");
                    command.Parameters.AddWithValue("@battles", 1);
                    command.Parameters.AddWithValue("@wgrating", 1);
                    command.Parameters.AddWithValue("@wn8", 0);
                    command.Parameters.AddWithValue("@winratio", 0.0);
                    command.Parameters.AddWithValue("@lastLogout", DateTime.Now);
                    command.Parameters.AddWithValue("@lastBattle", DateTime.Now);
                    command.Parameters.AddWithValue("@accountCreated", DateTime.Now);
                    command.Parameters.AddWithValue("@updatedAtWG", DateTime.Now);
                    command.Parameters.AddWithValue("@clientLang", "en");
                    command.Parameters.AddWithValue("@clan_id", 1);
                    command.Parameters.AddWithValue("@now", DateTime.Now);

                    foreach (Player player in players)
                    {
                        if (player.Moes.Length > 0)
                        {
                            command.Parameters["@id"].Value = player.ID;
                            command.Parameters["@name"].Value = player.PlayerInfo.Nick;
                            command.Parameters["@battles"].Value = player.PlayerInfo.Statistics.Random.Battles;
                            command.Parameters["@wgrating"].Value = player.PlayerInfo.WGRating;
                            command.Parameters["@wn8"].Value = player.WN8;
                            command.Parameters["@winratio"].Value = player.PlayerInfo.Statistics.Random.Winrate;
                            command.Parameters["@lastLogout"].Value = player.PlayerInfo.LastLogout;
                            command.Parameters["@lastBattle"].Value = player.PlayerInfo.LastBattle;
                            command.Parameters["@accountCreated"].Value = player.PlayerInfo.AccountCreated;
                            command.Parameters["@updatedAtWG"].Value = player.PlayerInfo.UpdatedAt;
                            command.Parameters["@clientLang"].Value = player.PlayerInfo.ClientLanguage;
                            command.Parameters["@clan_id"].Value = player.PlayerInfo.ClanID;
                            command.Parameters["@now"].Value = DateTime.Now;

                            command.ExecuteNonQuery();
                        }
                    }
                }
                Connection.Close();
            }
        }

        public void UpsertPlayerWithoutMarks(Player player)
        {
            using (MySqlConnection Connection = new MySqlConnection(ConnectionString))
            {
                Connection.Open();
                using (MySqlCommand command = GetPlayerInsertCommand(Connection))
                {

                    command.Parameters.AddWithValue("@id", player.ID);
                    command.Parameters.AddWithValue("@name", player.PlayerInfo.Nick);
                    command.Parameters.AddWithValue("@battles", player.PlayerInfo.Statistics.Random.Battles);
                    command.Parameters.AddWithValue("@wgrating", player.PlayerInfo.WGRating);
                    command.Parameters.AddWithValue("@wn8", player.WN8);
                    command.Parameters.AddWithValue("@winratio", player.PlayerInfo.Statistics.Random.Winrate);
                    command.Parameters.AddWithValue("@lastLogout", player.PlayerInfo.LastLogout);
                    command.Parameters.AddWithValue("@lastBattle", player.PlayerInfo.LastBattle);
                    command.Parameters.AddWithValue("@accountCreated", player.PlayerInfo.AccountCreated);
                    command.Parameters.AddWithValue("@updatedAtWG", player.PlayerInfo.UpdatedAt);
                    command.Parameters.AddWithValue("@clientLang", player.PlayerInfo.ClientLanguage);
                    command.Parameters.AddWithValue("@clan_id", player.PlayerInfo.ClanID);
                    command.Parameters.AddWithValue("@now", DateTime.Now);

                    command.ExecuteNonQuery();
                }
                Connection.Close();
            }
        }

        private MySqlCommand GetPlayerInsertCommand(MySqlConnection connection)
        {
            MySqlCommand command = new MySqlCommand()
            {
                Connection = connection,
                CommandText = "INSERT INTO players (id, name, battles, wgrating, wn8, winratio, lastLogout, lastBattle, accountCreated, updatedAtWG, clientLang, clan_id, created_at, updated_at)"
                + "VALUES (@id, @name, @battles, @wgrating, @wn8, @winratio, @lastLogout, @lastBattle, @accountCreated, @updatedAtWG, @clientLang, @clan_id, @now, @now) "
                + "ON DUPLICATE KEY UPDATE name=@name, battles=@battles, wgrating=@wgrating, wn8=@wn8, winratio=@winratio, lastLogout=@lastLogout, lastBattle=@lastBattle, updatedAtWG=@updatedAtWG, clientLang=@clientLang, "
                + "clan_id=@clan_id, updated_at=@now;"
            };

            command.Prepare();

            return command;
        }

        public void UpsertMarks(Moe[] moes, int playerID)
        {
            using (MySqlConnection Connection = new MySqlConnection(ConnectionString))
            {
                Connection.Open();
                using (MySqlCommand command = new MySqlCommand()
                {
                    Connection = Connection,
                    CommandText = "INSERT INTO marks (tank_id, player_id, created_at, updated_at) VALUES (@tank_id, @player_id, @now, @now)"
                                + "ON DUPLICATE KEY UPDATE updated_at=@now;"
                })
                {
                    command.Prepare();

                    command.Parameters.AddWithValue("@tank_id", 1);
                    command.Parameters.AddWithValue("@player_id", playerID);
                    command.Parameters.AddWithValue("@now", DateTime.Now);

                    foreach (Moe moe in moes)
                    {
                        if (!AvailableTankIDs.Contains(moe.TankID))
                        {
                            if (!ManualTankClient.TryLoadTank(Path.Combine(BasePath, Settings.RelativePathToManualTanksDirectory, $"{moe.TankID}.json"), out Tank tank))
                            {
                                Logger.Error($"Could not load manual tank with id {moe.TankID}, skipping this mark (player_id: {playerID}");
                                continue;
                            }

                            Logger.Info($"Loaded manual tank with id {moe.TankID} and upserting it into db");
                            UpsertTank(tank, moe.TankID);
                        }

                        command.Parameters["@tank_id"].Value = moe.TankID;
                        command.Parameters["@now"].Value = DateTime.Now;

                        try
                        {
                            command.ExecuteNonQuery();
                        }
                        catch (MySqlException ex)
                        {
                            Logger.Error($"Error {ex.ErrorCode}: {ex.Message}");
                            Logger.Info($"tank_id: {moe.TankID}, player_id: {playerID}");
                        }

                    }
                }
                Connection.Close();
            }
        }
    }
}
