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

            while (true)
            {
                //listen forever
               // Battle battle = null;
                var clientSocket = listener.AcceptSocket();
                var connection = new Thread(() => HandleRequest(clientSocket));
                connection.Start(); //starts the thread that communicates with the client
            }
        }

        /** 
         * This handles each client in a separate Thread
         * IMO it doesnt need a lock, because it is just reading from a network stream, that is unique to each client
         * as it is created from a clients socket
         */
        private static void HandleRequest(Socket socket)
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



            /*string name = getBetween(request.ContentString,"{\"Name\": \"","\",");
            string url = getBetween(request.ContentString,"Url\": \"","\",");
            double rating = (double) getBetweend(request.ContentString,"Rating\":",",");
            string genre = getBetween(request.ContentString,"Genre\": \"","\"");
            string songname = getBetween(request.ContentString,"Name\": \"","\"}");
            string usernamelib = getBetween(request.UserAuthorization.ToString(), "Basic", "-ppbToken");*/
            if (request.Method.Contains("POST"))
            {

                var response = new Response {ContentType = "plain/text", StatusCode = 200};
                response.AddHeader("Connection", "Close");
                var username = request.ContentString.Split(new string[] {"Username\":\"", "\","},
                    StringSplitOptions.None);
                string getusername = username[1];
                var name = request.ContentString.Split(new string[] {"{\"Name\":\"", "\""},
                    StringSplitOptions.None);
                string getname = name[3];
                string geturl = name[7];
                var r = request.ContentString.Split(new string[] {"Rating\":", ","},
                    StringSplitOptions.None);
                string getrating = r[3];
                string getgenre = name[13];

                //response.SetContent($"{getname}, {geturl}, {getgenre}, {getrating}");
                //response.Send(stream);
                var password =
                    request.ContentString.Split(new string[] {"Password\":\"", "\"}"}, StringSplitOptions.None);
                string getpassw = password[1];
                string[] usernamelib =
                    request.UserAuthorization.Split(new string[] {"Basic ", "-ppbToken"}, StringSplitOptions.None);

                string getusernamelib = usernamelib[1];
                if (request.Url.Path.Contains("users"))
                {

                    try
                    {
                        var con = new NpgsqlConnection(
                            "Server=localhost;Port=5435;Database=playlist;User Id=postgres;Password=postgres;");
                        con.Open();
                        var com = new NpgsqlCommand("Select username,token from users where username = :username",
                            con);
                        com.Parameters.AddWithValue("username", getusername);
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
                            response.Send(stream);
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

                //reader.Close();

                // userlogin(username, password);
                //response.SetContent($"User with Username :  {username}  and Password: {password}  logged in");


                if (request.Url.Path.Contains("lib"))
                {
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
            } /*
                  if (request.Url.Path.Contains("playlist"))
                  {
                      var con = new NpgsqlConnection(
                          "Server=localhost;Port=5435;Database=playlist;User Id=postgres;Password=postgres;");
                      using var command = new NpgsqlCommand(
                          "insert into playlist (songname, username) values (:songname,:username)",
                          con);
                      con.Open();
                      command.Parameters.AddWithValue("songname", songname);
                      command.Parameters.AddWithValue("username", usernamelib);
                      command.ExecuteNonQuery();
                      response.SetContent($"Playlist with Name :  {songname}  for user: {usernamelib} is saved");
                      con.Close();
                  }
                 // if (request.Url.Path.Contains("battle"))
                  //{
                    //  JoinBattle(request,battle);
                  //}
                 
                  response.Send(stream);
                }*/


            if (request.Method.Contains("GET"))
            {
                string text = request.Url.RawUrl;
                string usernamegetmethod = text.Split("/").Last();

                Response response = new Response {ContentType = "plain/text", StatusCode = 200};


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
                            string responseContent = null;
                            while (reader.Read())
                            {

                                responseContent = reader[0].ToString() + " | " + reader[1].ToString() + " | " +
                                                  reader[2].ToString() + " | "
                                                  + reader[3].ToString() + " | " + reader[4].ToString() + " | " +
                                                  reader[5].ToString() + " | ";


                            }

                            response.SetContent(responseContent);
                            response.Send(stream);
                        }


                        else
                        {
                            response.SetContent("No rows found.");

                            response.Send(stream);
                        }

                    }







                    // response.SetContent($"{Environment.NewLine}hello client, i hear you came from '{request.UserAgent}'.{Environment.NewLine}" +
                    //                   $"is it nice there?{Environment.NewLine}" +
                    //                 $"you have {request.HeaderCount} headers? whoa *.*" +
                    //               $"Row done  {username}"); 











                    socket.Close();
                }
            }
        }

        public static Response JoinBattle(Request req, Battle battle) {
            int userid = tokenToUserId(req.token);
            if (userid != -1)
            {
                foreach (UserBattleInfo user in battle.user_infos)
                {
                    if (user.username == userIdToName(userid))
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
        public static Response InsertRegistration(string username, string password, Request req)
        {
            try
            {var con = new NpgsqlConnection(
                    "Server=localhost;Port=5435;Database=playlist;User Id=postgres;Password=postgres;");
                var com = new NpgsqlCommand("Select username from user where username = {:username}", con);
                com.Parameters.AddWithValue("username", username);
                NpgsqlDataReader reader = com.ExecuteReader();
                reader.Read();
                if (reader.HasRows)
                    return new Response(req, Status.Status_Code.NOK, AdditionalPayload: "User already exists");
                else
                {
                using var command = new NpgsqlCommand(
                    "insert into USERS (username, password, token) values (:userName, :password, :token)",
                    con);
                con.Open();

                //hash the password using sha1 algorithm
                using var sha1 = new SHA1Managed();
                var hashedPassword = sha1.ComputeHash(Encoding.UTF8.GetBytes(password));
                var hashedPasswordString = string.Concat(hashedPassword.Select(b => b.ToString("x2")));

                //build the token (we store the token, so we can get users just by their token)
                var tokenBuilder = new StringBuilder();
                tokenBuilder.Append(username);
                tokenBuilder.Append("-ppbgToken");
                var token = tokenBuilder.ToString();

                command.Parameters.AddWithValue("username", username);
                command.Parameters.AddWithValue("password", hashedPasswordString);
                command.Parameters.AddWithValue("token", token);
                command.ExecuteNonQuery();
                return new Response(req, Status.Status_Code.OK, AdditionalPayload: "User added");
                }
            }
            catch (Exception)
            {
                return new Response(req, Status.Status_Code.NOK, AdditionalPayload: "Request failed");
            }
        }
       /* public static string getBetween(string strSource, string strStart, string strEnd)
        {
            try
            { 
               
                int start = strSource.IndexOf(strStart, 0, StringComparison.Ordinal) + strStart.Length;
                int  end = strSource.IndexOf(strEnd, start, StringComparison.Ordinal);
                if (strEnd == " ") return (strSource.Substring(Convert.ToInt32(strStart)));
                return strSource.Substring(start, end - start);
                    
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                throw;
            }
        
        }
        public static double? getBetweend(string strSourced, string strStartd, string strEndd)
        {
            if (strSourced.Contains(strStartd) && strSourced.Contains(strEndd))
            {
                int Start, End;
                Start = strSourced.IndexOf(strStartd, 0) + strStartd.Length;
                End = strSourced.IndexOf(strEndd, Start);
                return Convert.ToDouble(strSourced.Substring(Start, End - Start));
            }

            return 0.0;
        }*/
        public static bool userlogin(string username, string password) {
            using (SHA256 hasher = SHA256.Create())
            {
                password = BitConverter.ToString(hasher.ComputeHash(Encoding.ASCII.GetBytes(password))).Replace("-", string.Empty);
            }
            var con = new NpgsqlConnection(
                "Server=localhost;Port=5435;Database=playlist;User Id=postgres;Password=postgres;");
            //var conn = new NpgsqlConnection(con);
            con.Open();
            NpgsqlCommand command;
            NpgsqlDataReader reader;
            command = new NpgsqlCommand("SELECT userid,username,password FROM users;", con);
            reader = command.ExecuteReader();
            while (reader.Read())
            {
                if (reader[1].ToString() == username) {
                    if (reader[2].ToString() == password) {
                        reader.Close();
                        return true;
                    }
                    reader.Close();
                    return false;
                }
            }
            reader.Close();
            return false;
        }
        public static List<string> userData(string username) {
            List<string> usrdata = new List<string>(); ;
            var con = new NpgsqlConnection(
                "Server=localhost;Port=5435;Database=playlist;User Id=postgres;Password=postgres;");
            con.Open();
            NpgsqlCommand command;
            NpgsqlDataReader reader;
            try
            {
                command = new NpgsqlCommand($"SELECT * FROM users WHERE username={username};", con);
                reader = command.ExecuteReader();
            }
            catch (Exception e) {
                Console.WriteLine(e);
                return null;
            }
            if (reader.Read())
            {
                for (int i = 3; i < reader.FieldCount; i++)
                {
                    usrdata.Add(reader.GetName(i) + "\r\n" + reader[i].ToString());
                }
            }
            return usrdata;
        }
        public static Response login(Request req)
        {
            if (req.ctype == "json")
            {
                JObject jObject = JObject.Parse(req.payload);
                if (userlogin(jObject.GetValue("Username").ToString(), jObject.GetValue("Password").ToString()))
                {
                    JObject payload = new JObject();
                    var tokenBuilder = new StringBuilder();
                    tokenBuilder.Append(jObject.GetValue("Username").ToString());
                    tokenBuilder.Append("-ppbgToken");
                    var token = tokenBuilder.ToString();
                    payload.Add("Authorization", token);
                    return new Response(req, Status.Status_Code.OK,AdditionalPayload:(payload.ToString()));
                }
            }
            return new Response(req, Status.Status_Code.NOK, AdditionalPayload:"Login Failed. ");
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
       
        public static int tokenToUserId(string token) {
            if (token != null)
            {
                return nameToUserid(token.Substring(6, token.Length - 15));
            }
            return -1;
        }

        public static void getUserData(string username, string password, string token)
        {
            var con = new NpgsqlConnection(
                "Server=localhost;Port=5435;Database=playlist;User Id=postgres;Password=postgres;");
            con.Open();
            NpgsqlCommand command = new NpgsqlCommand("SELECT username, password, token FROM users WHERE username=:username;", con);
            NpgsqlDataReader dr  =  command.ExecuteReader();
            command.Parameters.AddWithValue("username", username);
         
            while (dr.Read())
                Console.Write("{0}\t{1} \n", dr[0], dr[1]);
 
            con.Close();
        }
        public static string userIdToName(int userid) {
            var conn = new NpgsqlConnection(
                "Server=localhost;Port=5435;Database=playlist;User Id=postgres;Password=postgres;");
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
    }   

}