using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Npgsql;

namespace PlaylistGame
{
    public class Game
    {
       

        private static readonly string connection =
            "Server=localhost;Port=5435;Database=playlist;User Id=postgres;Password=postgres;";

        //var response = new Response {ContentType = "plain/text", StatusCode = 200};
        public static string user_register(string getusername, string getpassw)
        {
            try
            {
                var con = new NpgsqlConnection(
                    connection);
                con.Open();
                var com = new NpgsqlCommand($"Select username,token from player where username = '{getusername}'",
                    con);
                var reader = com.ExecuteReader();
                reader.Read();
                if (!reader.HasRows)
                {
                    con.Close();
                    var conn = new NpgsqlConnection(
                        connection);
                    conn.Open();
                    using var command = new NpgsqlCommand(
                        "insert into player (username, password, token,points,admin) values (:username, :password, :token,0,0)",
                        conn);



                    //hash the password using sha1 algorithm
                    using var sha1 = new SHA1Managed();
                    var hashedPassword = sha1.ComputeHash(Encoding.UTF8.GetBytes(getpassw));
                    var hashedPasswordString = string.Concat(hashedPassword.Select(b => b.ToString("x2")));

                    //build the token (we store the token, so we can get users just by their token)
                    var tokenBuilder = new StringBuilder();
                    tokenBuilder.Append(getusername);
                    tokenBuilder.Append("-ppbgToken");
                    var token = tokenBuilder.ToString();
                    command.Parameters.AddWithValue("username", getusername);
                    command.Parameters.AddWithValue("password", hashedPasswordString);
                    command.Parameters.AddWithValue("token", token);
                    command.ExecuteNonQuery();

                    Console.Write($"{Environment.NewLine}User added");
                    return "\n User added";
                }
                else
                {
                    Console.Write($"{Environment.NewLine}User exists");
                    return "\n User exists";
                }

                con.Close();
            }
            catch (Exception e)
            {
                Console.Write($"{Environment.NewLine}{e}");
                return e.ToString();
            }
        }

        public static string user_login(string getpassw, string getusername)
        {
            try
            {
                using var sha1 = new SHA1Managed();
                var hashedPassword = sha1.ComputeHash(Encoding.UTF8.GetBytes(getpassw));
                var hashedPasswordString = string.Concat(hashedPassword.Select(b => b.ToString("x2")));


                var con = new NpgsqlConnection(
                    connection);
                //var conn = new NpgsqlConnection(con);
                con.Open();
                NpgsqlCommand command;
                NpgsqlDataReader reader;
                command = new NpgsqlCommand(
                    "SELECT username,password FROM player where username = '" + getusername +
                    "' and password = '" + hashedPasswordString + "'",
                    con);
                reader = command.ExecuteReader();
                reader.Read();

                if (reader.HasRows)
                    Console.Write($"{Environment.NewLine}Logged In");
                else
                    //reader.Close();

                    Console.Write($"{Environment.NewLine}User does not exist");

                con.Close();
            }
            catch (Exception e)
            {
                Console.Write($"{Environment.NewLine}{e}");
            }

            return null;
        }

        public static string add_in_lib(string getname, string geturl, string getgenre, string getrating,
            string getusernamelib)
        {
            var con = new NpgsqlConnection(connection);
            var com = new NpgsqlCommand($"Select from player where username = '{getusernamelib}'", con);
            con.Open();
            var reader = com.ExecuteReader();

            if (!reader.HasRows)
            {
                Console.WriteLine($"{Environment.NewLine}User does not exist");

                con.Close();
                return "User does not exist";
            }
            else
            {
                con.Close();
                using var command = new NpgsqlCommand(
                    "insert into library (name, url, rating, genre,username) values (:name, :url, :rating, :genre, :username)",
                    con);
                con.Open();
                command.Parameters.AddWithValue("name", getname);
                command.Parameters.AddWithValue("url", geturl);
                command.Parameters.AddWithValue("rating", getrating);
                command.Parameters.AddWithValue("genre", getgenre);
                command.Parameters.AddWithValue("username", getusernamelib);
                command.ExecuteNonQuery();
                Console.WriteLine(
                    $"{Environment.NewLine}Song with Name :  {getname}  and url: {geturl}  for user: {getusernamelib} is saved");

                con.Close();

                return
                    $"{Environment.NewLine}Song with Name :  {getname}  and url: {geturl}  for user: {getusernamelib} is saved";
            }
        }

        public static string add_in_playlist(string getusernamelib, string getnameforplaylist)
        {
            var con = new NpgsqlConnection(
                connection);
            var com = new NpgsqlCommand($"Select * from player where username = '{getusernamelib}'", con);
            con.Open();
            var reader = com.ExecuteReader();
            reader.Read();
            if (!reader.HasRows)
            {
                Console.WriteLine($"{Environment.NewLine}User does not exist");
                con.Close();
                return $"{Environment.NewLine}User does not exist";
            }
            else
            {
                con.Close();
                using var command = new NpgsqlCommand(
                    "insert into playlist (songname, username ) values (:songname,:username)",
                    con);
                con.Open();
                command.Parameters.AddWithValue("songname", getnameforplaylist);
                command.Parameters.AddWithValue("username", getusernamelib);

                command.ExecuteNonQuery();
                Console.WriteLine(
                    $"{Environment.NewLine}Playlist with Name :  {getnameforplaylist}  for user: {getusernamelib} is saved");
                con.Close();
                return
                    $"{Environment.NewLine}Playlist with Name :  {getnameforplaylist}  for user: {getusernamelib} is saved";
            }
        }

        public static String get_user_data(string usernamegetmethod, string getusernamelib)
        {
            var payload = new JObject();
            if (usernamegetmethod.Equals(getusernamelib))
            {
                var con = new NpgsqlConnection(
                    connection);

                var command =
                    new NpgsqlCommand(
                        "SELECT username, token  FROM player WHERE username='" + usernamegetmethod +
                        "'",
                        con);

                //command.Parameters.AddWithValue("username", getusernamelib);
                con.Open();
                var reader = command.ExecuteReader();


                if (reader.HasRows)
                {
                    var playlist = new List<object[]>();
                    while (reader.Read())
                        playlist.Add(new object[2]
                        {
                            reader[0].ToString(), reader[1].ToString()
                        });



                    var i = 0;
                    foreach (var song in playlist)
                    {
                        var JSong = new JObject();
                        JSong.Add(song[0].ToString(), song[1].ToString());
                        payload.Add(i.ToString(), JSong);
                        i++;
                    }

                    Console.Write($"{Environment.NewLine}{payload}");
                    return payload.ToString();
                }

                return "No rows";
            }

            return "Not authorized";
        }

        public static string get_actions(string usernamegetmethod)
        {
            var con = new NpgsqlConnection(
                connection);

            var command =
                new NpgsqlCommand(
                    "SELECT username, actions  FROM player WHERE username='" + usernamegetmethod +
                    "'",
                    con);

            //command.Parameters.AddWithValue("username", getusernamelib);
            con.Open();
            var reader = command.ExecuteReader();


            if (reader.HasRows)
            {
                var playlist = new List<object[]>();
                while (reader.Read())
                    playlist.Add(new object[2]
                    {
                        reader[0].ToString(), reader[1].ToString()
                    });


                var payload = new JObject();
                var i = 0;
                foreach (var song in playlist)
                {
                    var JSong = new JObject();
                    JSong.Add(song[0].ToString(), song[1].ToString());
                    payload.Add(i.ToString(), JSong);
                    i++;
                }

                Console.Write($"{Environment.NewLine}{payload}");
                return payload.ToString();
            }


            else
            {
                Console.WriteLine($"{Environment.NewLine}No rows found.");
                return $"{Environment.NewLine}No rows found.";
            }
        }

        public static string get_lib(string getusernamelib)
        {
            var con = new NpgsqlConnection(
                connection);

            var command =
                new NpgsqlCommand(
                    "SELECT * FROM library WHERE username='" + getusernamelib + "'",
                    con);


            con.Open();
            var reader = command.ExecuteReader();

            if (reader.HasRows)
            {
                var playlist = new List<object[]>();
                while (reader.Read())
                    playlist.Add(new object[4]
                    {
                        reader[0].ToString(), reader[1].ToString(), reader[3].ToString(),
                        reader[4].ToString()
                    });


                var payload = new JObject();
                var i = 0;
                foreach (var song in playlist)
                {
                    var JSong = new JObject();
                    JSong.Add(song[0].ToString(), song[1].ToString());
                    payload.Add(i.ToString(), JSong);
                    i++;
                }

                Console.Write($"{Environment.NewLine}{payload}");
                return payload.ToString();
            }
            else
            {
                Console.Write($"{Environment.NewLine}No rows found.");
                return $"{Environment.NewLine}No rows found.";
            }
        }

        public static string get_playlist()
        {
            var con = new NpgsqlConnection(
                connection);
            con.Open();
            var command =
                new NpgsqlCommand(
                    "SELECT songname, username  FROM playlist",
                    con);


            var reader = command.ExecuteReader();

            if (reader.HasRows)
            {
                var playlist = new List<object[]>();
                while (reader.Read())
                    playlist.Add(new object[2]
                    {
                        reader[0].ToString(), reader[1].ToString()
                    });


                var payload = new JObject();
                var i = 0;
                foreach (var song in playlist)
                {
                    var JSong = new JObject();
                    JSong.Add(song[0].ToString(), song[1].ToString());
                    payload.Add(i.ToString(), JSong);
                    i++;
                }

                Console.Write($"{Environment.NewLine}{payload}");
                return payload.ToString();
            }
            else
            {
                Console.Write($"{Environment.NewLine}No rows found.");
                return $"{Environment.NewLine}No rows found.";
            }
        }

        public static string get_stats(string getusernamelib)
        {
            var con = new NpgsqlConnection(
                connection);

            var command =
                new NpgsqlCommand(
                    "SELECT * FROM stats WHERE username='" + getusernamelib + "'",
                    con);

            //command.Parameters.AddWithValue("username", getusernamelib);
            con.Open();
            var reader = command.ExecuteReader();
            // response.AddHeader("Connection", "Close");
            if (reader.HasRows)
            {
                var playlist = new List<object[]>();
                while (reader.Read())
                    playlist.Add(new object[3]
                    {
                        reader[0].ToString(), reader[1].ToString(), reader[2].ToString()
                    });


                var payload = new JObject();
                var i = 0;
                foreach (var song in playlist)
                {
                    var JSong = new JObject();
                    JSong.Add(song[1].ToString(), song[2].ToString());
                    payload.Add(i.ToString(), JSong);
                    i++;
                }

                Console.Write($"{Environment.NewLine}{payload}");
                return payload.ToString();
            }
            else
            {
                Console.Write($"{Environment.NewLine}No rows found.");
                return $"{Environment.NewLine}No rows found.";
            }
        }

        public static string get_score(string getusernamelib)
        {
            var con = new NpgsqlConnection(
                connection);

            var command =
                new NpgsqlCommand(
                    "SELECT * FROM scoreboard WHERE username='" + getusernamelib + "'",
                    con);


            con.Open();
            var reader = command.ExecuteReader();

            if (reader.HasRows)
            {
                var playlist = new List<object[]>();
                while (reader.Read())
                    playlist.Add(new object[3]
                    {
                        reader[0].ToString(), reader[1].ToString(), reader[2].ToString()
                    });


                var payload = new JObject();
                var i = 0;
                foreach (var song in playlist)
                {
                    var JSong = new JObject();
                    JSong.Add(song[1].ToString(), song[2].ToString());
                    payload.Add(i.ToString(), JSong);
                    i++;
                }

                Console.Write($"{Environment.NewLine}{payload}");
                return payload.ToString();
            }
            else
            {
                Console.Write($"{Environment.NewLine}No rows found.");
                return $"{Environment.NewLine}No rows found.";
            }
        }

        public static string del_lib(string urlteil, string auth1)
        {

            var conn = new NpgsqlConnection(
                connection);
            conn.Open();
            var comm =
                new NpgsqlCommand(
                    $"Select * FROM library where name = '{urlteil}' and username = '{auth1}'",
                    conn);
            var reader = comm.ExecuteReader();
            if (!reader.HasRows)
                Console.Write($"{Environment.NewLine}No rows found");
            else
                try
                {
                    var con = new NpgsqlConnection(
                        connection);
                    con.Open();
                    var command =
                        new NpgsqlCommand(
                            $"Delete FROM library where name = '{urlteil}' and username = '{auth1}'",
                            con);
                    command.ExecuteNonQuery();
                    Console.Write($"{Environment.NewLine}Deleted");
                    return $"{Environment.NewLine}Deleted";
                }
                catch (Exception e)
                {
                    Console.Write($"{Environment.NewLine}{e}");


                    return $"{Environment.NewLine}{e}";
                }

            return null;
        }

        public static string update_user(string auth1, string name, string bio, string img)
        {

            var conn = new NpgsqlConnection(
                connection);
            conn.Open();
            var comm =
                new NpgsqlCommand(
                    $"Select * FROM player where username = '{auth1}'",
                    conn);
            var reader = comm.ExecuteReader();
            if (!reader.HasRows)
                Console.Write($"{Environment.NewLine}No rows found");
            else
                try
                {
                    var con = new NpgsqlConnection(
                        connection);
                    con.Open();
                    var command =
                        new NpgsqlCommand(
                            $" Update player set nickname = '{name}', bio = '{bio}', image = '{img}' where username = '{auth1}'",
                            con);
                    command.ExecuteNonQuery();
                    Console.Write($"{Environment.NewLine}Updated");
                    return $"{Environment.NewLine}Updated";
                }
                catch (Exception e)
                {
                    Console.Write($"{Environment.NewLine}{e}");
                    return $"{e}";

                }

            return null;
        }

        public static string update_actions(string action, string auth1)
        {
            try
            {
                var con = new NpgsqlConnection(
                    "Server=localhost;Port=5435;Database=playlist;User Id=postgres;Password=postgres;");
                con.Open();
                var command =
                    new NpgsqlCommand(
                        $" Update player set  actions = '{action}' where username = '{auth1}'",
                        con);
                command.ExecuteNonQuery();

                Console.Write($"{Environment.NewLine}Updated");
                return $"{Environment.NewLine}Updated";
            }
            catch (Exception e)
            {
                Console.Write($"{Environment.NewLine}{e}");
                return $"{e}";

            }
        }

        public static string update_playlist(string auth1, string id, string pos)
        {
            try
            {
                var conn = new NpgsqlConnection(
                    connection);
                conn.Open();
                var comm =
                    new NpgsqlCommand(
                        $"Select * FROM player where username = '{auth1}' and admin = 1 ",
                        conn);
                var reader = comm.ExecuteReader();
                reader.Read();
                if (!reader.HasRows)
                {
                    Console.Write($"{Environment.NewLine}Not Admin");
                    conn.Close();
                }
                else
                {
                    try
                    {
                        conn.Close();
                        var con = new NpgsqlConnection(
                            connection);
                        con.Open();
                        var command =
                            new NpgsqlCommand(
                                $" Update playlist set position = '{id}' where username = '{auth1}'  and pid = {pos}",
                                con);
                        command.ExecuteNonQuery();
                        Console.Write($"{Environment.NewLine}Updated");
                        return $"{Environment.NewLine}Updated";
                    }
                    catch (Exception e)
                    {
                        Console.Write($"{Environment.NewLine}{e}");

                        throw;
                    }
                }
            }
            catch (Exception e)
            {
                Console.Write($"{Environment.NewLine}{e}");
                return $"{e}";


            }

            return null;
        }

        public static string put_actions(string username)
        {
            Console.WriteLine(username + " please put your actions");
            return username + " please put your actions";
        }

        public static string battle_logic(string username1, string username2)
        {
            Console.WriteLine(username1);
            Console.WriteLine(username2);
            string responseContent1 = null;
            string responseContent2 = null;
            string responseContent3 = null;
            var time = DateTime.Now;
            var con = new NpgsqlConnection(
                connection);

            var command = new NpgsqlCommand(
                "INSERT INTO battle(username1,username2,timeofplay) values('" + username1 +
                "'," + "'" + username2 + "','" + time + "')",
                con);

            //command.Parameters.AddWithValue("username", getusernamelib);
            con.Open();
            command.ExecuteNonQuery();
            Console.WriteLine("DONE");
            con.Close();
            put_actions(username1);
            var command1 =
                new NpgsqlCommand(
                    $"SELECT actions FROM player WHERE username='{username1}'",
                    con);

            //command.Parameters.AddWithValue("username", getusernamelib);
            con.Open();
            var reader1 = command1.ExecuteReader();
            if (reader1.HasRows)
            {
                //  string responseContent = null;
                reader1.Read();

                responseContent1 = reader1[0].ToString();
                //   response.SetContent(responseContent);
                // response.Send(stream);
                Console.WriteLine(responseContent1);
                
                if (responseContent1.Equals(" "))

                {
                    put_actions(username1);

                }
                else
                {
                    Console.WriteLine(responseContent1);
                    
                }
                con.Close();
                put_actions(username2);
                var command2 =
                    new NpgsqlCommand(
                        $"SELECT actions FROM player WHERE username='{username2}'",
                        con);

                //command.Parameters.AddWithValue("username", getusernamelib);
                con.Open();
                var reader2 = command2.ExecuteReader();
                if (reader2.HasRows)
                {
                    reader2.Read();

                    responseContent2 = reader2[0].ToString();
                    //   response.SetContent(responseContent);
                    // response.Send(stream);
                    if (responseContent2.Equals(" "))

                    {
                        put_actions(username2);

                    }
                    else
                    {
                        Console.WriteLine(responseContent2);
                        //return responseContent2;
                    }

                }
                else
                {
                    Console.WriteLine(" not found");
                    
                }

                con.Close();
                var command3 =
                    new NpgsqlCommand(
                        $"Update battle SET actionofuser1 = '{responseContent1}',actionofuser2='{responseContent2}' where username1='{username1}' and username2 = '{username2}'",
                        con);

                //command.Parameters.AddWithValue("username", getusernamelib);
                con.Open();
                command3.ExecuteNonQuery();
                Console.WriteLine("Updated");
                con.Close();
                var win = 0;
                var games_played = 0;
                if ((responseContent1.Contains("RRRRR") && responseContent2.Contains("RRRRR")))
                {
                    //win = 0;
                    Console.WriteLine("Draw");
                   
                }

                if (responseContent1.Contains("RRRRR") && responseContent2.Contains("SSSSS") ||
                    responseContent1.Contains("RRRRR") && responseContent2.Contains("LLLLL") ||
                    responseContent1.Contains("SSSSS") && responseContent2.Contains("PPPPP") ||
                    responseContent1.Contains("SSSSS") && responseContent2.Contains("LLLLL") ||
                    responseContent1.Contains("LLLLL") && responseContent2.Contains("PPPPP") ||
                    responseContent1.Contains("LLLLL") && responseContent2.Contains("sssss") ||
                    responseContent1.Contains("PPPPP") && responseContent2.Contains("sssss") ||
                    responseContent1.Contains("PPPPP") && responseContent2.Contains("RRRRR") ||
                    responseContent1.Contains("vvvvv") && responseContent2.Contains("RRRRR") ||
                    responseContent1.Contains("vvvvv") && responseContent2.Contains("SSSSS"))
                {
                    var command4 = new NpgsqlCommand(
                        $"SELECT games_played, points FROM player WHERE username='{username1}'",
                        con);

                    //command.Parameters.AddWithValue("username", getusernamelib);
                    con.Open();
                    var reader3 = command4.ExecuteReader();
                    reader3.Read();
                    //  string winning = reader3[1].ToString();
                    var winpoint = reader3.GetInt32(1);
                    Console.WriteLine(winpoint);
                    var points = winpoint + 1;
                    Console.WriteLine(points);
                    con.Close();
                    var command5 = new NpgsqlCommand(
                        $"Update player SET points = '{points}', admin = 1,actions = ' ' where username='{username1}' ",
                        con);

                    //command.Parameters.AddWithValue("username", getusernamelib);
                    con.Open();
                    command5.ExecuteNonQuery();
                    Console.WriteLine("Winner1");
                    con.Close();

                    var command9 = new NpgsqlCommand(
                        $"Update battle SET winner = '{username1}' where username1='{username1}' and username2 = '{username2}' ",
                        con);

                    //command.Parameters.AddWithValue("username", getusernamelib);
                    con.Open();
                    command9.ExecuteNonQuery();
                    Console.WriteLine($"THE WINNER IS {username1}");
                    
                }
                else
                {
                    var command6 = new NpgsqlCommand(
                        $"SELECT games_played, points FROM player WHERE username='{username2}'",
                        con);

                    //command.Parameters.AddWithValue("username", getusernamelib);
                    con.Open();
                    var reader3 = command6.ExecuteReader();
                    reader3.Read();
                    //  string winning = reader3[1].ToString();
                    var winpoint = reader3.GetInt32(1);
                    Console.WriteLine(winpoint);
                    var points = winpoint + 1;
                    Console.WriteLine(points);
                    con.Close();
                    var command7 = new NpgsqlCommand(
                        $"Update player SET points = '{points}', admin = 1, actions = ' ' where username='{username2}' ",
                        con);

                    //command.Parameters.AddWithValue("username", getusernamelib);
                    con.Open();
                    command7.ExecuteNonQuery();
                    Console.WriteLine("Winner2");
                    con.Close();
                    var command8 = new NpgsqlCommand(
                        $"Update battle SET winner = '{username2}' where username1='{username1}' and username2 = '{username2}' ",
                        con);

                    //command.Parameters.AddWithValue("username", getusernamelib);
                    con.Open();
                    command8.ExecuteNonQuery();
                    Console.WriteLine($"THE WINNER IS {username2}");
                   
                }
            }

            return null;
        }
    }
}