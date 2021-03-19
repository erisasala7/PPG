using System;
using Npgsql;

namespace PlaylistGame
{
    public class DB
    {
        static string connstring = "Host=localhost;Username=postgres;Database=postgres";
        public static int nameToUserid(string name)
        {
            var conn = new NpgsqlConnection(connstring);
            conn.Open();
            NpgsqlCommand command;
            NpgsqlDataReader reader;

            command = new NpgsqlCommand("PREPARE findUsername AS SELECT userid FROM users WHERE login=$1;", conn);
            reader = command.ExecuteReader();
            reader.Close();


            command = new NpgsqlCommand($"EXECUTE findUsername('{name}');", conn);
            reader = command.ExecuteReader();
            if (reader.Read())
            {
                int val = Int32.Parse(reader[0].ToString());
                conn.Close();
                return val;
            }
            conn.Close();
            return -1;
        }
        public static int tokenToUserId(string token) {
            if (token != null)
            {
                return DB.nameToUserid(token.Substring(6, token.Length - 15));
            }
            return -1;
        }
        public static string userIdToName(int userid) {
            var conn = new NpgsqlConnection(connstring);
            conn.Open();
            NpgsqlCommand command;
            NpgsqlDataReader reader;
            command = new NpgsqlCommand($"SELECT login FROM users WHERE userid={userid};", conn);
            reader = command.ExecuteReader();
            if (reader.Read())
            {
                string name = reader[0].ToString();
                conn.Close();
                return name;
            }
            conn.Close();
            return null;
        }
        public static bool incrementUserWin(int userId)
        {
            var conn = new NpgsqlConnection(connstring);
            conn.Open();
            NpgsqlCommand command;
            NpgsqlDataReader reader;
            command = new NpgsqlCommand($"SELECT wins FROM scoreboard WHERE userid={userId};", conn);
            reader = command.ExecuteReader();
            int wins;
            try
            {
                reader.Read();
                wins = Int32.Parse(reader[0].ToString());
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                reader.Close();
                conn.Close();
                return false;
            }
            reader.Close();
            wins += 1;
            try
            {
                command = new NpgsqlCommand($"UPDATE scoreboard SET wins={wins} WHERE userid={userId}", conn);
                reader = command.ExecuteReader();
            }
            catch (Exception e) {
                Console.WriteLine(e);
                reader.Close();
                conn.Close();
                return false;
            }
            reader.Close();
            conn.Close();
            return true;
        }
    }
}