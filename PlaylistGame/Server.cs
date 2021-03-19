using System;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Npgsql;
using Newtonsoft.Json.Linq;



namespace PlaylistGame
{
    public class Server
    {
        public static void Listen()
        {
            var listener = new TcpListener(IPAddress.Any, 10002);
            Console.WriteLine("Server listening at port 5000");

            listener.Start();
            Battle battle = new Battle();
            while (true)
            {
                //listen forever
                // Battle battle = null;
                var clientSocket = listener.AcceptSocket();

                var connection = new Thread(() => HandleRequest(clientSocket, battle));
                connection.Start(); //starts the thread that communicates with the client
            }

        }

        /** 
         * This handles each client in a separate Thread
         * IMO it doesnt need a lock, because it is just reading from a network stream, that is unique to each client
         * as it is created from a clients socket
         */
        private static void HandleRequest(Socket socket, Battle battle)
        {

            using var stream = new NetworkStream(socket);
            using var memoryStream = new MemoryStream();
            int bytesRead;
            var readBuffer = new byte[1024];


            while ((bytesRead = stream.Read(readBuffer, 0, readBuffer.Length)) > 0)
            {
                memoryStream.Write(readBuffer, 0, bytesRead);
                if (!stream.DataAvailable)
                {
                    break;
                }
            }


            var readDataString = Encoding.UTF8.GetString(memoryStream.ToArray(), 0, (int) memoryStream.Length);
            var request = new Request(readDataString);
            Console.WriteLine($"Got Request with Method: {request.Method}");
            Console.WriteLine($"Raw Request: {readDataString}");

            var response = new Response {ContentType = "plain/text", StatusCode = 200};

            /*string name = getBetween(request.ContentString,"{\"Name\": \"","\",");
            string url = getBetween(request.ContentString,"Url\": \"","\",");
            double rating = (double) getBetweend(request.ContentString,"Rating\":",",");
            string genre = getBetween(request.ContentString,"Genre\": \"","\"");
            string songname = getBetween(request.ContentString,"Name\": \"","\"}");
            string usernamelib = getBetween(request.UserAuthorization.ToString(), "Basic", "-ppbToken");*/
            if (request.Method.Contains("POST"))
            {


                response.AddHeader("Connection", "Close");

                if (request.Url.Path.Contains("users"))
                {

                    try
                    {
                        var username = request.ContentString.Split(new string[] {"Username\":\"", "\","},
                            StringSplitOptions.None);
                        string getusername = username[1];
                        var password =
                            request.ContentString.Split(new string[] {"Password\":\"", "\"}"}, StringSplitOptions.None);
                        string getpassw = password[1];
                        var con = new NpgsqlConnection(
                            "Server=localhost;Port=5435;Database=playlist;User Id=postgres;Password=postgres;");
                        con.Open();
                        var com = new NpgsqlCommand($"Select username,token from player where username = '{username}'",
                            con);
                        //com.Parameters.AddWithValue("username", getusername);
                        NpgsqlDataReader reader = com.ExecuteReader();
                        reader.Read();
                        if (!(reader.HasRows))
                        {
                            con.Close();
                            var conn = new NpgsqlConnection(
                                "Server=localhost;Port=5435;Database=playlist;User Id=postgres;Password=postgres;");
                            conn.Open();
                            using var command = new NpgsqlCommand(
                                "insert into player (username, password, token) values (:username, :password, :token)",
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
                            response.StatusCode = 200;
                            response.SetContent($"Done");
                            response.Send(stream);
                            // socket.Close();
                        }
                        else
                        {
                            response.StatusCode = 400;
                            response.SetContent($"{response.StatusCode}-User exists");
                            response.Send(stream);
                        }

                        con.Close();
                    }
                    catch (Exception e)
                    {
                        response.StatusCode = 404;
                        response.SetContent($"{e}");
                        response.Send(stream);
                    }

                    //response.SetContent($"A new User with Username :  {username}  and Password: {password}  was added"); 
                }



                if (request.Url.Path.Contains("sessions"))
                {
                    try
                    {
                        var username = request.ContentString.Split(new string[] {"Username\":\"", "\","},
                            StringSplitOptions.None);
                        string getusername = username[1];
                        var password =
                            request.ContentString.Split(new string[] {"Password\":\"", "\"}"}, StringSplitOptions.None);
                        string getpassw = password[1];
                        using var sha1 = new SHA1Managed();
                        var hashedPassword = sha1.ComputeHash(Encoding.UTF8.GetBytes(getpassw));
                        var hashedPasswordString = string.Concat(hashedPassword.Select(b => b.ToString("x2")));


                        var con = new NpgsqlConnection(
                            "Server=localhost;Port=5435;Database=playlist;User Id=postgres;Password=postgres;");
                        //var conn = new NpgsqlConnection(con);
                        con.Open();
                        NpgsqlCommand command;
                        NpgsqlDataReader reader;
                        command = new NpgsqlCommand(
                            $"SELECT username,password FROM users where username = '" + getusername +
                            "' and password = '" + hashedPasswordString + "'",
                            con);
                        // command.Parameters.AddWithValue("username", getusername);
                        //command.Parameters.AddWithValue("password", hashedPasswordString);
                        reader = command.ExecuteReader();
                        reader.Read();

                        if (reader.HasRows)
                        {

                            response.StatusCode = 200;
                            response.SetContent($" Loged In");
                            response.Send(stream);

                        }
                        else
                        {
                            //reader.Close();
                            response.StatusCode = 400;
                            response.SetContent($"NO");
                            response.Send(stream);
                        }

                        con.Close();
                    }
                    catch (Exception e)
                    {
                        response.StatusCode = 404;
                        response.SetContent($"{e}");
                        response.Send(stream);
                    }

                }

                if (request.Url.Path.Contains("lib"))
                {
                    var name = request.ContentString.Split(new string[] {"{\"Name\":\"", "\""},
                        StringSplitOptions.None);
                    string getname = name[3];
                    string geturl = name[7];
                    var r = request.ContentString.Split(new string[] {"Rating\":", ","},
                        StringSplitOptions.None);
                    string getrating = r[3];
                    string getgenre = name[13];

                    string[] usernamelib =
                        request.UserAuthorization.Split(new string[] {"Basic ", "-ppbToken"}, StringSplitOptions.None);

                    string getusernamelib = usernamelib[1];
                    var con = new NpgsqlConnection(
                        "Server=localhost;Port=5435;Database=playlist;User Id=postgres;Password=postgres;");
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
                    response.SetContent(
                        $"Song with Name :  {getname}  and url: {geturl}  for user: {getusernamelib} is saved");
                    response.Send(stream);
                    con.Close();

                }

                if (request.Url.Path.Contains("playlist"))
                {
                    string[] usernamelib = request.UserAuthorization.Split(new string[] {"Basic ", "-ppbToken"},
                        StringSplitOptions.None);

                    string getusernamelib = usernamelib[1];
                    var nameforplaylist =
                        request.ContentString.Split(new string[] {"Name\":  ", "\""}, StringSplitOptions.None);
                    string getnameforplaylist = nameforplaylist[3];

                    var con = new NpgsqlConnection(
                        "Server=localhost;Port=5435;Database=playlist;User Id=postgres;Password=postgres;");
                    con.Open();
                    using var command = new NpgsqlCommand(
                        "insert into playlist (songname, username) values (:songname,:username)",
                        con);

                    command.Parameters.AddWithValue("songname", getnameforplaylist);
                    command.Parameters.AddWithValue("username", getusernamelib);
                    command.ExecuteNonQuery();
                    response.SetContent(
                        $"Playlist with Name :  {getnameforplaylist}  for user: {getusernamelib} is saved");
                    response.Send(stream);
                    con.Close();
                }

                if (request.Url.RawUrl.Contains("battles"))
                {
                    var username =
                        request.ContentString.Split(new string[] {"\"Username1\"", "\""}, StringSplitOptions.None);
                    string username1 = username[2].ToString();
                    string username2 = username[6].ToString();
                    battle_logic(username1, username2);


                }

                if (request.Method.Contains("GET"))
                {
                    string text = request.Url.RawUrl;
                    string usernamegetmethod = text.Split("/").Last();




                    if (request.Url.Path.Contains("users"))
                    {
                        string[] usernamelib = request.UserAuthorization.Split(new string[] {"Basic ", "-ppbToken"},
                            StringSplitOptions.None);
                        if (usernamelib.Length > 0)
                        {
                            string getusernamelib = usernamelib[1];
                            if (usernamegetmethod.Equals(getusernamelib))
                            {
                                var con = new NpgsqlConnection(
                                    "Server=localhost;Port=5435;Database=playlist;User Id=postgres;Password=postgres;");

                                NpgsqlCommand command =
                                    new NpgsqlCommand(
                                        $"SELECT username, token  FROM users WHERE username='" + usernamegetmethod +
                                        "'",
                                        con);

                                //command.Parameters.AddWithValue("username", getusernamelib);
                                con.Open();
                                NpgsqlDataReader reader = command.ExecuteReader();
                                response.AddHeader("Connection", "Close");

                                if (reader.HasRows)
                                {
                                    string responseContent = null;
                                    while (reader.Read())
                                    {
                                        responseContent = reader[0].ToString() + " | " + reader[1].ToString();

                                    }

                                    response.SetContent(responseContent);
                                    response.Send(stream);
                                }


                                else
                                {
                                    response.SetContent("No rows found.");
                                }

                                response.Send(stream);
                            }
                            else
                            {
                                response.SetContent("Not authorized.");
                                response.Send(stream);
                            }
                        }

                    }

                    if (request.Url.RawUrl.Contains("lib"))
                    {
                        string[] usernamelib = request.UserAuthorization.Split(new string[] {"Basic ", "-ppbToken"},
                            StringSplitOptions.None);
                        if (usernamelib.Length > 0)
                        {
                            string getusernamelib = usernamelib[1];
                            var con = new NpgsqlConnection(
                                "Server=localhost;Port=5435;Database=playlist;User Id=postgres;Password=postgres;");

                            NpgsqlCommand command =
                                new NpgsqlCommand(
                                    $"SELECT * FROM library WHERE username='" + getusernamelib + "'",
                                    con);

                            //command.Parameters.AddWithValue("username", getusernamelib);
                            con.Open();
                            NpgsqlDataReader reader = command.ExecuteReader();
                            // response.AddHeader("Connection", "Close");
                            if (reader.HasRows)
                            {
                                List<Object[]> playlist = new List<Object[]>();
                                while (reader.Read())
                                {
                                    playlist.Add(new Object[4]
                                    {
                                        reader[0].ToString(), reader[1].ToString(), reader[3].ToString(),
                                        reader[4].ToString()
                                    });
                                }


                                JObject payload = new JObject();
                                int i = 0;
                                foreach (Object[] song in playlist)
                                {
                                    JObject JSong = new JObject();
                                    JSong.Add(song[0].ToString(), song[1].ToString());
                                    payload.Add(i.ToString(), JSong);
                                    i++;
                                }

                                Console.Write(payload.ToString());
                                response.SetContent(payload.ToString());
                                response.Send(stream);
                            }
                            else
                            {
                                response.SetContent("No rows found.");
                                response.Send(stream);
                            }
                        }
                    }

                    if (request.Url.Path.Contains("playlist"))
                    {
                        var con = new NpgsqlConnection(
                            "Server=localhost;Port=5435;Database=playlist;User Id=postgres;Password=postgres;");
                        con.Open();
                        NpgsqlCommand command =
                            new NpgsqlCommand(
                                $"SELECT songname, username  FROM playlist",
                                con);

                        //command.Parameters.AddWithValue("username", getusernamelib);

                        //  NpgsqlDataReader reader = command.ExecuteReader();
                        response.AddHeader("Connection", "Close");

                        NpgsqlDataReader reader = command.ExecuteReader();

                        if (reader.HasRows)
                        {
                            List<Object[]> playlist = new List<Object[]>();
                            while (reader.Read())
                            {
                                playlist.Add(new Object[2]
                                {
                                    reader[0].ToString(), reader[1].ToString()
                                });
                            }


                            JObject payload = new JObject();
                            int i = 0;
                            foreach (Object[] song in playlist)
                            {
                                JObject JSong = new JObject();
                                JSong.Add(song[0].ToString(), song[1].ToString());
                                payload.Add(i.ToString(), JSong);
                                i++;
                            }

                            response.SetContent(payload.ToString());
                            response.Send(stream);
                        }
                        else
                        {
                            response.SetContent("No rows found.");
                            response.Send(stream);
                        }


                    }

                    if (request.Url.RawUrl.Contains("stats"))
                    {
                        string[] usernamelib = request.UserAuthorization.Split(new string[] {"Basic ", "-ppbToken"},
                            StringSplitOptions.None);
                        if (usernamelib.Length > 0)
                        {
                            string getusernamelib = usernamelib[1];
                            var con = new NpgsqlConnection(
                                "Server=localhost;Port=5435;Database=playlist;User Id=postgres;Password=postgres;");

                            NpgsqlCommand command =
                                new NpgsqlCommand(
                                    $"SELECT * FROM stats WHERE username='" + getusernamelib + "'",
                                    con);

                            //command.Parameters.AddWithValue("username", getusernamelib);
                            con.Open();
                            NpgsqlDataReader reader = command.ExecuteReader();
                            // response.AddHeader("Connection", "Close");
                            if (reader.HasRows)
                            {
                                List<Object[]> playlist = new List<Object[]>();
                                while (reader.Read())
                                {
                                    playlist.Add(new Object[3]
                                    {
                                        reader[0].ToString(), reader[1].ToString(), reader[2].ToString()

                                    });
                                }


                                JObject payload = new JObject();
                                int i = 0;
                                foreach (Object[] song in playlist)
                                {
                                    JObject JSong = new JObject();
                                    JSong.Add(song[1].ToString(), song[2].ToString());
                                    payload.Add(i.ToString(), JSong);
                                    i++;
                                }

                                response.SetContent(payload.ToString());
                                response.Send(stream);
                            }
                            else
                            {
                                response.SetContent("No rows found.");
                                response.Send(stream);
                            }
                        }
                    }

                    if (request.Url.RawUrl.Contains("score"))
                    {
                        string[] usernamelib = request.UserAuthorization.Split(new string[] {"Basic ", "-ppbToken"},
                            StringSplitOptions.None);
                        if (usernamelib.Length > 0)
                        {
                            string getusernamelib = usernamelib[1];
                            var con = new NpgsqlConnection(
                                "Server=localhost;Port=5435;Database=playlist;User Id=postgres;Password=postgres;");

                            NpgsqlCommand command =
                                new NpgsqlCommand(
                                    $"SELECT * FROM scoreboard WHERE username='" + getusernamelib + "'",
                                    con);

                            //command.Parameters.AddWithValue("username", getusernamelib);
                            con.Open();
                            NpgsqlDataReader reader = command.ExecuteReader();
                            // response.AddHeader("Connection", "Close");
                            if (reader.HasRows)
                            {
                                List<Object[]> playlist = new List<Object[]>();
                                while (reader.Read())
                                {
                                    playlist.Add(new Object[3]
                                    {
                                        reader[0].ToString(), reader[1].ToString(), reader[2].ToString()

                                    });
                                }


                                JObject payload = new JObject();
                                int i = 0;
                                foreach (Object[] song in playlist)
                                {
                                    JObject JSong = new JObject();
                                    JSong.Add(song[1].ToString(), song[2].ToString());
                                    payload.Add(i.ToString(), JSong);
                                    i++;
                                }

                                response.SetContent(payload.ToString());
                                response.Send(stream);
                            }
                            else
                            {
                                response.SetContent("No rows found.");
                                response.Send(stream);
                            }
                        }
                    }
                }

                if (request.Method.Contains("DEL"))
                {
                    if (request.Url.RawUrl.Contains("lib"))
                    {
                        string urlteil = request.Url.RawUrl.Split("/").Last();
                        Console.WriteLine(urlteil);
                        var auth = request.UserAuthorization.Split(new string[] {"Basic ", "-ppbToken"},
                            StringSplitOptions.None);
                        string auth1 = auth[1];
                        Console.WriteLine(auth1);
                        try
                        {
                            var connection = new NpgsqlConnection(
                                "Server=localhost;Port=5435;Database=playlist;User Id=postgres;Password=postgres;");
                            connection.Open();
                            NpgsqlCommand comm =
                                new NpgsqlCommand(
                                    $"Select * FROM library where name = '{urlteil}' and username = '{auth1}'",
                                    connection);
                            NpgsqlDataReader reader = comm.ExecuteReader();
                            if (!(reader.HasRows))
                            {
                                response.SetContent("No rows found");
                                response.Send(stream);
                            }
                            else
                            {
                                try
                                {
                                    var con = new NpgsqlConnection(
                                        "Server=localhost;Port=5435;Database=playlist;User Id=postgres;Password=postgres;");
                                    con.Open();
                                    NpgsqlCommand command =
                                        new NpgsqlCommand(
                                            $"Delete FROM library where name = '{urlteil}' and username = '{auth1}'",
                                            con);
                                    command.ExecuteNonQuery();
                                    response.SetContent("Deleted");
                                    response.Send(stream);
                                }
                                catch (Exception e)
                                {
                                    response.SetContent($"{e}");
                                    response.Send(stream);
                                    throw;
                                }
                            }

                        }
                        catch (Exception e)
                        {
                            response.SetContent($"{e}");
                            response.Send(stream);
                            throw;
                        }

                    }
                }

                if (request.Method.Contains("PUT"))
                {

                    if (request.Url.RawUrl.Contains("users"))
                    {
                        var updateuser =
                            request.ContentString.Split(new string[] {"\"Name\": \" ", "\""}, StringSplitOptions.None);
                        string name = updateuser[3];
                        string bio = updateuser[7];
                        string img = updateuser[11];
                        Console.WriteLine(name + " " + bio + " " + img);
                        string urlteil = request.Url.RawUrl.Split("/").Last();
                        Console.WriteLine(urlteil);
                        var auth = request.UserAuthorization.Split(new string[] {"Basic ", "-ppbToken"},
                            StringSplitOptions.None);
                        string auth1 = auth[1];
                        Console.WriteLine(auth1);

                        if (urlteil.Equals(auth1))
                        {
                            try
                            {
                                var connection = new NpgsqlConnection(
                                    "Server=localhost;Port=5435;Database=playlist;User Id=postgres;Password=postgres;");
                                connection.Open();
                                NpgsqlCommand comm =
                                    new NpgsqlCommand(
                                        $"Select * FROM users where username = '{auth1}'",
                                        connection);
                                NpgsqlDataReader reader = comm.ExecuteReader();
                                if (!(reader.HasRows))
                                {
                                    response.SetContent("No rows found");
                                    response.Send(stream);
                                }
                                else
                                {
                                    try
                                    {
                                        var con = new NpgsqlConnection(
                                            "Server=localhost;Port=5435;Database=playlist;User Id=postgres;Password=postgres;");
                                        con.Open();
                                        NpgsqlCommand command =
                                            new NpgsqlCommand(
                                                $" Update users set nickname = '{name}', bio = '{bio}', image = '{img}' where username = '{auth1}'",
                                                con);
                                        command.ExecuteNonQuery();
                                        response.SetContent("Updated");
                                        response.Send(stream);
                                    }
                                    catch (Exception e)
                                    {
                                        response.SetContent($"{e}");
                                        response.Send(stream);
                                        throw;
                                    }
                                }

                            }
                            catch (Exception e)
                            {
                                Console.WriteLine($"{e}");
                                //response.Send(stream);
                                throw;
                            }

                        }
                        else
                        {
                            response.SetContent($"Not Authorized");
                            response.Send(stream);
                        }
                    }

                    if (request.Url.RawUrl.Contains("actions"))
                    {
                        var auth = request.UserAuthorization.Split(new string[] {"Basic ", "-ppbToken"},
                            StringSplitOptions.None);
                        string auth1 = auth[1];
                        var actions = request.ContentString.Split(new string[] {"actions\": \"", "\"}"},
                            StringSplitOptions.None);
                        string action = actions[1];
                        Console.WriteLine(action);
                        try
                        {
                            var con = new NpgsqlConnection(
                                "Server=localhost;Port=5435;Database=playlist;User Id=postgres;Password=postgres;");
                            con.Open();
                            NpgsqlCommand command =
                                new NpgsqlCommand(
                                    $" Update player set  actions = '{action}' where username = '{auth1}'",
                                    con);
                            command.ExecuteNonQuery();

                            response.SetContent("Updated");
                            response.Send(stream);
                        }
                        catch (Exception e)
                        {
                            response.SetContent($"{e}");
                            response.Send(stream);
                            throw;
                        }






                    }



                }



                socket.Close();
            }
        }
           public static void battle_logic(string username1, string username2)
        {

            Console.WriteLine(username1);
            Console.WriteLine(username2);
            string responseContent1 = null;
            string responseContent2 = null;
            string responseContent3 = null;
            DateTime time = DateTime.Now;
            var con = new NpgsqlConnection(
                "Server=localhost;Port=5435;Database=playlist;User Id=postgres;Password=postgres;");

            NpgsqlCommand command = new NpgsqlCommand(
                $"INSERT INTO battle(username1,username2,timeofplay) values('" + username1 +
                "'," + "'" + username2 + "','" + time + "')",
                con);

            //command.Parameters.AddWithValue("username", getusernamelib);
            con.Open();
            command.ExecuteNonQuery();
            Console.WriteLine("DONE");
            con.Close();
            Console.WriteLine(username1 + " please put your actions");
            NpgsqlCommand command1 =
                new NpgsqlCommand(
                    $"SELECT actions FROM player WHERE username='kienboec'",
                    con);

            //command.Parameters.AddWithValue("username", getusernamelib);
            con.Open();
            NpgsqlDataReader reader1 = command1.ExecuteReader();
            if (reader1.HasRows)
            {
                //  string responseContent = null;
                reader1.Read();

                responseContent1 = reader1[0].ToString();
                //   response.SetContent(responseContent);
                // response.Send(stream);
                Console.WriteLine(responseContent1);
                if (!(responseContent1.Equals(" ")))
                {
                    Console.WriteLine(responseContent1);
                }
                else
                {
                    Console.WriteLine(username1 + " please enter your actions");
                }

            }
            else
            {
                Console.WriteLine(" not found");

            }

            con.Close();
            Console.WriteLine(username2 + " please put your actions");
            NpgsqlCommand command2 =
                new NpgsqlCommand(
                    $"SELECT actions FROM player WHERE username='{username2}'",
                    con);

            //command.Parameters.AddWithValue("username", getusernamelib);
            con.Open();
            NpgsqlDataReader reader2 = command2.ExecuteReader();
            if (reader2.HasRows)
            {

                reader2.Read();

                responseContent2 = reader2[0].ToString();
                //   response.SetContent(responseContent);
                // response.Send(stream);
                if (!(responseContent2.Equals(" ")))
                {
                    Console.WriteLine(responseContent2);
                }
                else
                {
                    Console.WriteLine(username1 + " please enter your actions");
                }

            }
            else
            {
                Console.WriteLine(" not found");

            }

            con.Close();
            NpgsqlCommand command3 =
                new NpgsqlCommand(
                    $"Update battle SET actionofuser1 = '{responseContent1}',actionofuser2='{responseContent2}' where username1='{username1}' and username2 = '{username2}'",
                    con);

            //command.Parameters.AddWithValue("username", getusernamelib);
            con.Open();
            command3.ExecuteNonQuery();
            Console.WriteLine("Updated");
            con.Close();
            int win = 0;
            int games_played = 0;
            if (responseContent1.Equals(responseContent2))
            {
                win = 0;
            }

            if ((responseContent1.Contains("RRRRR") && (responseContent2.Contains("SSSSS")) ||
                 ((responseContent1.Contains("RRRRR") && responseContent2.Contains("LLLLL")) ||
                  (responseContent1.Contains("SSSSS") && responseContent2.Contains("PPPPP")) ||
                  (responseContent1.Contains("SSSSS") && responseContent2.Contains("LLLLL")) ||
                  (responseContent1.Contains("LLLLL") && responseContent2.Contains("PPPPP")) ||
                  (responseContent1.Contains("LLLLL") && responseContent2.Contains("sssss")) ||
                  (responseContent1.Contains("PPPPP") && responseContent2.Contains("sssss")) ||
                  (responseContent1.Contains("PPPPP") && responseContent2.Contains("RRRRR")) ||
                  (responseContent1.Contains("sssss") && responseContent2.Contains("RRRRR")) ||
                  (responseContent1.Contains("sssss") && responseContent2.Contains("SSSSS")))))
            {
                NpgsqlCommand command4 = new NpgsqlCommand(
                    $"SELECT games_played, points FROM player WHERE username='{username1}'",
                    con);

                //command.Parameters.AddWithValue("username", getusernamelib);
                con.Open();
                NpgsqlDataReader reader3 = command4.ExecuteReader();
                reader3.Read();
                //  string winning = reader3[1].ToString();
                int winpoint = reader3.GetInt32(1);
                Console.WriteLine(winpoint);
                int points = winpoint + 1;
                Console.WriteLine(points);
                con.Close();
                NpgsqlCommand command5 = new NpgsqlCommand(
                    $"Update player SET points = '{points}' where username='{username1}' ",
                    con);

                //command.Parameters.AddWithValue("username", getusernamelib);
                con.Open();
                command5.ExecuteNonQuery();
                Console.WriteLine("Winner1");
                con.Close();

                NpgsqlCommand command9 = new NpgsqlCommand(
                    $"Update battle SET winner = '{username1}' where username1='{username1}' and username2 = '{username2}' ",
                    con);

                //command.Parameters.AddWithValue("username", getusernamelib);
                con.Open();
                command9.ExecuteNonQuery();
                Console.WriteLine($"{username1}");
            }
            else
            {
                NpgsqlCommand command6 = new NpgsqlCommand(
                    $"SELECT games_played, points FROM player WHERE username='{username2}'",
                    con);

                //command.Parameters.AddWithValue("username", getusernamelib);
                con.Open();
                NpgsqlDataReader reader3 = command6.ExecuteReader();
                reader3.Read();
                //  string winning = reader3[1].ToString();
                int winpoint = reader3.GetInt32(1);
                Console.WriteLine(winpoint);
                int points = winpoint + 1;
                Console.WriteLine(points);
                con.Close();
                NpgsqlCommand command7 = new NpgsqlCommand(
                    $"Update player SET points = '{points}' where username='{username2}' ",
                    con);

                //command.Parameters.AddWithValue("username", getusernamelib);
                con.Open();
                command7.ExecuteNonQuery();
                Console.WriteLine("Winner2");
                con.Close();
                NpgsqlCommand command8 = new NpgsqlCommand(
                    $"Update battle SET winner = '{username2}' where username1='{username1}' and username2 = '{username2}' ",
                    con);

                //command.Parameters.AddWithValue("username", getusernamelib);
                con.Open();
                command8.ExecuteNonQuery();
                Console.WriteLine($"{username2}");

            }
        }




        public static string UserIdToName(int userid)
        {
            var con = new NpgsqlConnection(
                "Server=localhost;Port=5435;Database=playlist;User Id=postgres;Password=postgres;");
            con.Open();
            NpgsqlCommand command;
            NpgsqlDataReader reader;
            command = new NpgsqlCommand($"SELECT login FROM users WHERE userid={userid};", con);
            reader = command.ExecuteReader();
            if (reader.Read())
            {
                string name = reader[0].ToString();
                con.Close();
                return name;
            }

            con.Close();
            return null;
        }
        public static Response joinBattle(Request req, Battle battle) {
            int userid = tokenToUserId(req.token);
            if (userid != -1)
            {
                foreach (UserBattleInfo user in battle.user_infos)
                {
                    if (user.username == UserIdToName(userid))
                    {
                        foreach (BActions action in user.actions) {
                            if (action == BActions.NULL) { 
                                return new Response(req, Status.Status_Code.NOK, AdditionalPayload: "Set the actions first!. ");
                            }
                        }
                        string payload=battle.joinBattle(user);
                        return new Response(req, Status.Status_Code.NOK, AdditionalPayload: payload);
                    }
                }
                return new Response(req, Status.Status_Code.NOK, AdditionalPayload: "Set the actions first!. ");
            }
            return new Response(req, Status.Status_Code.NOK, AdditionalPayload: "Coulnd't verify connection. ");
        }


        public static int nameToUserid(string name)
        {
            var con = new NpgsqlConnection(
                "Server=localhost;Port=5435;Database=playlist;User Id=postgres;Password=postgres;");
            con.Open();
            NpgsqlCommand command;
            NpgsqlDataReader reader;

            command = new NpgsqlCommand(" SELECT userid FROM users WHERE username = :username;", con);
            reader = command.ExecuteReader();

            if (reader.Read())
            {
                int val = Int32.Parse(reader[0].ToString());
                con.Close();
                return val;
            }

            con.Close();
            return -1;
        }

        public static int tokenToUserId(string token)
        {
            if (token != null)
            {
                return nameToUserid(token.Substring(6, token.Length - 15));
            }

            return -1;
        }

        public static string firstplayer(string user)
        {
            Console.WriteLine($"{user} started the tournament. You have 15s to set 4 Actions");
                    for (int i = 15; i >= 0; i--)
                    {
                        if (i != 0)
                        {
                            Console.Write(i + " ");
                            Thread.Sleep(1000);
                        }
                        else Console.WriteLine("\n done");
                    }
                    var con = new NpgsqlConnection(
                            "Server=localhost;Port=5435;Database=playlist;User Id=postgres;Password=postgres;");

                        NpgsqlCommand command =
                            new NpgsqlCommand(
                                $"SELECT username, actions FROM player WHERE username='" + user + "' and actions = 'RRRRR'",
                                con);

                        //command.Parameters.AddWithValue("username", getusernamelib);
                        con.Open();
                        NpgsqlDataReader reader = command.ExecuteReader();
                        // response.AddHeader("Connection", "Close");
                        if (reader.HasRows)
                        {
                            List<Object[]> playlist = new List<Object[]>();
                            reader.Read();
                            
                                playlist.Add(new Object[2]
                                {
                                    reader[0].ToString(), reader[1].ToString()
                                   
                                });
                                
                            


                            JObject payload = new JObject();
                            int i = 0;
                            
                            foreach (Object[] song in playlist)
                            {
                                JObject JSong = new JObject();
                                JSong.Add(song[0].ToString(), song[1].ToString());
                                payload.Add(i.ToString(), JSong);
                                i++;
                                
                            }
                           // string attrib1Value = payload["kienboec"][0]["0"].Value<string>();
                            //string act = payload.GetValue("kienboec").Value<string>();
                           
                           Console.Write(payload.ToString());
                           
                            string actionsUser1 = reader[1].ToString();
                            Console.Write(actionsUser1);
                            return actionsUser1;
                        }
                        else
                        {
                            return "No rows found.";
                           
                        }

                        
        }
        
         public static string secondplayer(string user)
        {
            Console.WriteLine($"{user} started the tournament. You have 15s to set 4 Actions");
                    for (int i = 15; i >= 0; i--)
                    {
                        if (i != 0)
                        {
                            Console.Write(i + " ");
                            Thread.Sleep(1000);
                        }
                        else Console.WriteLine("\n done");
                    }
                    var con = new NpgsqlConnection(
                            "Server=localhost;Port=5435;Database=playlist;User Id=postgres;Password=postgres;");

                        NpgsqlCommand command =
                            new NpgsqlCommand(
                                $"SELECT username, actions FROM player WHERE username='" + user + "' and actions = 'RRRRR'",
                                con);

                        //command.Parameters.AddWithValue("username", getusernamelib);
                        con.Open();
                        NpgsqlDataReader reader = command.ExecuteReader();
                        // response.AddHeader("Connection", "Close");
                        if (reader.HasRows)
                        {
                            List<Object[]> playlist = new List<Object[]>();
                            reader.Read();
                            
                                playlist.Add(new Object[2]
                                {
                                    reader[0].ToString(), reader[1].ToString()
                                   
                                });
                                
                            


                            JObject payload = new JObject();
                            int i = 0;
                            
                            foreach (Object[] song in playlist)
                            {
                                JObject JSong = new JObject();
                                JSong.Add(song[0].ToString(), song[1].ToString());
                                payload.Add(i.ToString(), JSong);
                                i++;
                                
                            }
                           // string attrib1Value = payload["kienboec"][0]["0"].Value<string>();
                            //string act = payload.GetValue("kienboec").Value<string>();
                           
                           Console.Write(payload.ToString());
                           
                            string actionsUser1 = reader[1].ToString();
                            Console.Write(actionsUser1);
                            return actionsUser1;
                        }
                        else
                        {
                            return "No rows found.";
                           
                        }

                        
        }

         public static void battlelog(string username1, string username2)
         {
             Console.WriteLine("Please send the first request.");
             firstplayer(username1);
             Console.WriteLine("Please send the second request.");
             firstplayer(username2);

             int win = 0;
             if (firstplayer(username1).Equals(firstplayer(username2)))
             {
                 Console.WriteLine("Draw");
             }
         }
         public List<UserBattleInfo> user_infos = new List<UserBattleInfo>();
         public List<UserBattleInfo> blocked_users = new List<UserBattleInfo>();
         public List<UserBattleInfo> active_users= new List<UserBattleInfo>();
         public string log;
         public int currentAdminId = -1;
         //int battleCountdown = -1;
         bool battleActive = false;

         
         Task timer;
         public string joinBattle( UserBattleInfo user ) {
             if (!battleActive) {
                 battleActive = true;
                 timer = Task.Run(startTimerAsync);
             }
             active_users.Add(user);
             //add active user

             timer.Wait();
             return log;
         }
         public virtual void startTimerAsync()
         {
             Console.WriteLine("Battle will start soon. ");
             for (int i = 15; i >= 0; i--)
             {
                 Console.WriteLine(i);
                 Thread.Sleep(1000);
             }
             start_tournament();
         }
            public void start_tournament() {
            int playerCount = active_users.Count;
            if (playerCount > 1)
            {
                for (int i = 0; i < playerCount - 1; i++)
                {
                    for (int j = i + 1; j < playerCount; j++)
                    {
                        bool block = false;
                        foreach (UserBattleInfo b_user in blocked_users)
                        {
                            if (active_users[i] == b_user || active_users[j] == b_user)
                            {
                                block = true;
                            }
                        }
                        if (!block)
                        {
                            fight(active_users[i], active_users[j]);
                        }
                    }
                }
                log += "Results: \r\n";
                int highest = -1;
                foreach (UserBattleInfo player in active_users)
                {
                    log += "  " + player.username + ": " + player.battle_score;
                    if (player.battle_score > highest)
                    {
                        highest = player.battle_score;
                    }
                }
                log += "\r\n";
                List<UserBattleInfo> winnerList = new List<UserBattleInfo>();
                foreach (UserBattleInfo player in active_users)
                {
                    if (player.battle_score == highest) { winnerList.Add(player); }
                }
                if (winnerList.Count > 1)
                {
                    log += "Our tournament ended in a draw between ";
                    foreach (UserBattleInfo player in winnerList)
                    {
                        log += player.username + " and ";
                    }
                    log = log.Remove(log.Length - 5);
                    log += ". What a travesty!!!\r\n";
                    currentAdminId = -1;
                }
                else
                {
                    log += winnerList[0].username + " is the Winner! Congrats!\r\n";
                    currentAdminId=finishResult(winnerList);
                    //currentAdminId = DB_Tools.nameToUserid(winnerList[0].username);
                    //DB_Tools.incrementUserWin(DB_Tools.nameToUserid(winnerList[0].username));
                }
            }
            else {
                log += active_users[0].username + " is the Winner! Congrats!\r\n";
                currentAdminId=finishResult(active_users);
                //currentAdminId = DB_Tools.nameToUserid(active_users[0].username);
                //DB_Tools.incrementUserWin(DB_Tools.nameToUserid(active_users[0].username));
            }
            foreach (UserBattleInfo user in active_users) {
                user.battle_score = 0;
            }
            blocked_users = new List<UserBattleInfo>();
            active_users = new List<UserBattleInfo>();
            battleActive = false;

        }
             public virtual int finishResult(List<UserBattleInfo> active_users) { 
            DB.incrementUserWin(DB.nameToUserid(active_users[0].username));
            return DB.nameToUserid(active_users[0].username); ;
        }

        /// <summary>
        /// 1 win, 0 draw, -1 lose
        /// </summary>
        public static int action_eval(BActions action_1, BActions action_2) {
            if (action_1 == BActions.Lizard) {
                if (action_2 == BActions.Lizard) {
                    return 0;
                }
                if (action_2 == BActions.Spock || action_2 == BActions.Paper) {
                    return 1;
                }
                return -1;
            }

            if (action_1 == BActions.Spock) {
                if (action_2 == BActions.Spock) {
                    return 0;
                }
                if (action_2 == BActions.Rock || action_2 == BActions.Scissors) {
                    return 1;
                }
                return -1;
            }

            if (action_1 == BActions.Scissors) {
                if (action_2 == BActions.Scissors) {
                    return 0;
                }
                if (action_2 == BActions.Paper || action_2 == BActions.Lizard) {
                    return 1;
                }
                return -1;
            }

            if (action_1 == BActions.Rock) {
                if (action_2 == BActions.Rock) {
                    return 0;
                }
                if (action_2 == BActions.Scissors || action_2 == BActions.Lizard) {
                    return 1;
                }
                return -1;
            }

            if (action_1 == BActions.Paper) {
                if (action_2 == BActions.Paper) {
                    return 0;
                }
                if (action_2 == BActions.Rock || action_2 == BActions.Spock) {
                    return 1;
                }
                return -1;
            }

            return 0;
        }


        public void fight(UserBattleInfo pA, UserBattleInfo pB) {
            int favorA = 0;
            for (int i = 0; i < 5; i++) {
                log+= pA.username + " vs " + pB.username + "\r\n";
                favorA +=action_eval(pA.actions[i], pB.actions[i]);
                log += "   "+pA.actions[i].ToString() +" vs "+ pB.actions[i] + "\r\n";
            }
            if (favorA > 0) {
                pA.battle_score++;
                log += pA.username + " wins the round! \r\n";
            }
            if (favorA < 0) {
                pB.battle_score++;
                log += pB.username + " wins the round! \r\n";
            }
            if (favorA == 0) {
                log += "A draw?!? A conspiracy?\r\n";
                blocked_users.Add(pA);
                blocked_users.Add(pB);
            }
            log += "\r\n";
        }
        
        



    }
}
