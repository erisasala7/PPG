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
                        var com = new NpgsqlCommand($"Select username,token from users where username = '{username}'",
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
                                "insert into USERS (username, password, token) values (:username, :password, :token)",
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

              /*  if (request.Url.RawUrl.Contains("battles"))
                {
                    
                     joinBattle(request,battle);
                }
                */
              
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
                                    $"SELECT username, token  FROM users WHERE username='" + usernamegetmethod + "'",
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
                    var auth = request.UserAuthorization.Split(new string[] {"Basic ", "-ppbToken"}, StringSplitOptions.None);
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
                        NpgsqlDataReader reader =  comm.ExecuteReader();
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
                    var updateuser = request.ContentString.Split(new string[] {"\"Name\": \" ", "\""}, StringSplitOptions.None);
            string name = updateuser[3];
            string bio = updateuser[7];
            string img = updateuser[11];
            Console.WriteLine(name +" " + bio +" "+ img );
            string urlteil = request.Url.RawUrl.Split("/").Last();
            Console.WriteLine(urlteil);
            var auth = request.UserAuthorization.Split(new string[] {"Basic ", "-ppbToken"}, StringSplitOptions.None);
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
            }






            socket.Close();
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




    }
}
