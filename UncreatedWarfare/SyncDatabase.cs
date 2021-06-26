﻿using MySql.Data.MySqlClient;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Uncreated.Warfare
{
    public class SyncDatabase
    {
        public Config<SQLConfig> config;

        private MySqlConnection Connection;

        public bool IsConnected;

        public SyncDatabase()
        {
            config = new Config<SQLConfig>(Data.SQLStorage + "config.json");

            IsConnected = false;

            string ConnectionInfo = $"SERVER={config.data.LoginInfo.Host};" +
               $"DATABASE={config.data.LoginInfo.Database};" +
               $"UID={config.data.LoginInfo.Username};" +
               $"PASSWORD={config.data.LoginInfo.Password};" +
               $"PORT={config.data.LoginInfo.Port};";

            try
            {
                Connection = new MySqlConnection(ConnectionInfo);
                F.Log($"DATABASE CONNECTION: Connection created to {config.data.LoginInfo.Host}:{config.data.LoginInfo.Port} under user: {config.data.LoginInfo.Username}", ConsoleColor.DarkYellow);
                IsConnected = true;
            }
            catch
            {
                F.Log($"DATABASE CONNECTION FAILED: Could not create connection to {config.data.LoginInfo.Host}:{config.data.LoginInfo.Port} under user: {config.data.LoginInfo.Username}", ConsoleColor.Yellow);
                IsConnected = false;
            }
        }

        public bool Open()
        {
            try
            {
                Connection.Open();
                IsConnected = true;
                F.Log($"DATABASE CONNECTION: Successfully connected to {config.data.LoginInfo.Host}:{config.data.LoginInfo.Port} under user: {config.data.LoginInfo.Username}", ConsoleColor.DarkYellow);
                return true;
            }
            catch (MySqlException ex)
            {
                switch (ex.Number)
                {
                    case 0:
                        F.Log($"DATABASE CONNECTION FAILED: Could not find a host called '{config.data.LoginInfo.Host}'", ConsoleColor.Yellow);
                        break;

                    case 1045:
                        F.Log($"DATABASE CONNECTION FAILED: Host was found, but password was incorrect.", ConsoleColor.Yellow);
                        break;
                    default:
                        F.Log($"DATABASE CONNECTION FAILED: An unknown error occured...", ConsoleColor.Yellow);
                        break;
                }
                F.Log($"DATABASE CONNECTION ERROR CODE: {ex.Number} - {ex.Message}", ConsoleColor.Yellow);
                return false;
            }
        }
        public bool Close()
        {
            try
            {
                Connection.Close();
                return true;
            }
            catch (MySqlException)
            {
                return false;
            }
        }
        public int GetXP(ulong playerID, ulong team)
        {
            int balance = 0;

            if (IsConnected)
            {
                string query = $"SELECT XP FROM levels WHERE Steam64 = @playerID AND TEAM = @team;";

                MySqlCommand command = new MySqlCommand(query, Connection);
                command.Parameters.AddWithValue("@playerID", playerID);
                command.Parameters.AddWithValue("@team", team);
                MySqlDataReader reader = command.ExecuteReader();

                while (reader.Read())
                    balance = reader.GetInt32("XP");

                reader.Close();
                command.Dispose();
            }
            return balance;
        }
        public int AddXP(ulong playerID, ulong team, int amount)
        {
            int balance = GetXP(playerID, team);
            if (balance + amount < 0)
                return 0;

            if (IsConnected)
            {
                string query = $"INSERT INTO levels (Steam64, Team, OfficerPoints, XP) VALUES (@playerID, @team, 0, @absxp) ON DUPLICATE KEY UPDATE XP = XP + @xp;";

                MySqlCommand command = new MySqlCommand(query, Connection);
                command.Parameters.AddWithValue("@playerID", playerID);
                command.Parameters.AddWithValue("@team", team);
                command.Parameters.AddWithValue("@absxp", Math.Abs(amount));
                command.Parameters.AddWithValue("@xp", amount);
                command.ExecuteNonQuery();
                command.Dispose();
            }
            return balance + amount;
        }
        public int GetOfficerPoints(ulong playerID, ulong team)
        {
            int balance = 0;

            if (IsConnected)
            {
                string query = $"SELECT OfficerPoints FROM levels WHERE Steam64 = @playerID AND TEAM = @team;";

                MySqlCommand command = new MySqlCommand(query, Connection);
                command.Parameters.AddWithValue("@playerID", playerID);
                command.Parameters.AddWithValue("@team", team);
                MySqlDataReader reader = command.ExecuteReader();

                while (reader.Read())
                    balance = reader.GetInt32("OfficerPoints");

                reader.Close();
                command.Dispose();
            }
            return balance;
        }
        public int AddOfficerPoints(ulong playerID, ulong team, int amount)
        {
            int balance = GetOfficerPoints(playerID, team);
            if (balance - amount < 0)
                return 0;

            if (IsConnected)
            {
                string query = $"INSERT INTO levels (Steam64, Team, OfficerPoints, XP) VALUES (@playerID, @team, @abspoints, 0) ON DUPLICATE KEY UPDATE OfficerPoints = OfficerPoints + @points;";

                MySqlCommand command = new MySqlCommand(query, Connection);
                command.Parameters.AddWithValue("@playerID", playerID);
                command.Parameters.AddWithValue("@team", team);
                command.Parameters.AddWithValue("@abspoints", Math.Abs(amount));
                command.Parameters.AddWithValue("@points", amount);
                command.ExecuteNonQuery();
                command.Dispose();
            }
            return balance + amount;
        }

        public class SQLConfig : ConfigData
        {
            public MySqlData LoginInfo;
            public override void SetDefaults()
            {
                LoginInfo = new MySqlData
                {
                    Database = "warfare",
                    Host = "75.189.141.56",
                    Password = "bruh1234!",
                    Port = 3306,
                    Username = "admin",
                    CharSet = "utf8mb4"
                };
            }
            public SQLConfig() { }
        }
    }
}
