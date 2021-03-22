using System;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using Newtonsoft.Json.Linq;

namespace PlaylistGame
{
    public class Server
    {
        public static void Listen()
        {
            var port = 10002;
            var listener = new TcpListener(IPAddress.Any, port);
            Console.WriteLine($"Server listening at port {port}");

            listener.Start();

            while (true)
            {
                //listen forever

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
                if (!stream.DataAvailable) break;
            }


            var readDataString = Encoding.UTF8.GetString(memoryStream.ToArray(), 0, (int) memoryStream.Length);
            var request = new Request(readDataString);
            Console.WriteLine($"Got Request with Method: {request.Method}");
            Console.WriteLine($"Raw Request: {readDataString}");


            var response = new Response {ContentType = "application/json", StatusCode = 200};
            response.AddHeader("Connection", "Close");
            if (request.Method.Contains("POST"))
            {
                if (request.Url.Path.Contains("users"))
                {
                    var username = request.ContentString.Split(new[] {"Username\":\"", "\","},
                        StringSplitOptions.None);
                    var getusername = username[1];
                    var password =
                        request.ContentString.Split(new[] {"Password\":\"", "\"}"}, StringSplitOptions.None);
                    var getpassw = password[1];
                  string end = Game.user_register(getusername, getpassw);
                    response.SetContent(end);
                    response.Send(stream);
                }

                //response.SetContent($"A new User with Username :  {username}  and Password: {password}  was added"); 


                if (request.Url.Path.Contains("sessions"))
                {
                    var username = request.ContentString.Split(new[] {"Username\":\"", "\","},
                        StringSplitOptions.None);
                    var getusername = username[1];
                    var password =
                        request.ContentString.Split(new[] {"Password\":\"", "\"}"}, StringSplitOptions.None);
                    var getpassw = password[1];
                    Game.user_login(getpassw, getusername);
                    response.SetContent(Game.user_login(getpassw, getusername).ToString());
                    response.Send(stream);
                }


                if (request.Url.Path.Contains("lib"))
                {
                    var name = request.ContentString.Split(new[] {"{\"Name\":\"", "\""},
                        StringSplitOptions.None);
                    var getname = name[3];
                    var geturl = name[7];
                    var r = request.ContentString.Split(new[] {"Rating\":", ","},
                        StringSplitOptions.None);
                    var getrating = r[3];
                    var getgenre = name[13];

                    var usernamelib =
                        request.UserAuthorization.Split(new[] {" ", "-ppbgToken"}, StringSplitOptions.None);

                    var getusernamelib = usernamelib[1];
                    Game.add_in_lib(getname, geturl, getgenre, getrating, getusernamelib);
                    response.SetContent(Game.add_in_lib(getname, geturl, getgenre, getrating, getusernamelib).ToString());
                    response.Send(stream);
                }

                if (request.Url.Path.Contains("playlist"))
                {
                    var usernamelib = request.UserAuthorization.Split(new[] {" ", "-ppbgToken"},
                        StringSplitOptions.None);

                    var getusernamelib = usernamelib[1];
                    var nameforplaylist =
                        request.ContentString.Split(new[] {"Name\":  ", "\""}, StringSplitOptions.None);
                    var getnameforplaylist = nameforplaylist[3];
                    Game.add_in_playlist(getusernamelib, getnameforplaylist);
                    response.SetContent( Game.add_in_playlist(getusernamelib, getnameforplaylist));
                    response.Send(stream);
                }

                if (request.Url.RawUrl.Contains("battles"))
                {
                    var username =
                        request.ContentString.Split(new[] {"\"Username1\"", "\""}, StringSplitOptions.None);
                    var username1 = username[2];
                    var username2 = username[6];
                    Game.battle_logic(username1, username2);
                    response.SetContent("Game Done.");
                    response.Send(stream);
                }
            }

            if (request.Method.Contains("GET"))
            {
                var text = request.Url.RawUrl;
                var usernamegetmethod = text.Split("/").Last();


                if (request.Url.Path.Contains("users"))
                {
                    var usernamelib = request.UserAuthorization.Split(new[] {" ", "-ppbgToken"},
                        StringSplitOptions.None);
                    if (usernamelib.Length > 0)
                    {
                        var getusernamelib = usernamelib[1];
                        Console.WriteLine(getusernamelib);
                        Game.get_user_data(usernamegetmethod, getusernamelib);
                        response.SetContent( Game.get_user_data(usernamegetmethod, getusernamelib));
                        response.Send(stream);
                    }
                }

                if (request.Url.Path.Contains("actions"))
                {
                    var usernamelib = request.UserAuthorization.Split(new[] {" ", "-ppbgToken"},
                        StringSplitOptions.None);
                    if (usernamelib.Length > 0)
                    {
                        var getusernamelib = usernamelib[1];
                        Game.get_actions(getusernamelib);
                        response.SetContent(Game.get_actions(getusernamelib).ToString());
                        response.Send(stream);
                    }
                }

                if (request.Url.RawUrl.Contains("lib"))
                {
                    var usernamelib = request.UserAuthorization.Split(new[] {" ", "-ppbgToken"},
                        StringSplitOptions.None);
                    if (usernamelib.Length > 0)
                    {
                        var getusernamelib = usernamelib[1];
                        Game.get_lib(getusernamelib);
                        response.SetContent(Game.get_lib(getusernamelib));
                        response.Send(stream);
                    }
                }

                if (request.Url.Path.Contains("playlist"))
                {
                    Game.get_playlist();
                    response.SetContent( Game.get_playlist());
                    response.Send(stream);
                }

                if (request.Url.RawUrl.Contains("stats"))
                {
                    var usernamelib = request.UserAuthorization.Split(new[] {" ", "-ppbgToken"},
                        StringSplitOptions.None);
                    if (usernamelib.Length > 0)
                    {
                        var getusernamelib = usernamelib[1];
                        Game.get_stats(getusernamelib);
                        response.SetContent(Game.get_stats(getusernamelib));
                        response.Send(stream);
                    }
                }

                if (request.Url.RawUrl.Contains("score"))
                {
                    var usernamelib = request.UserAuthorization.Split(new[] {" ", "-ppbgToken"},
                        StringSplitOptions.None);
                    if (usernamelib.Length > 0)
                    {
                        var getusernamelib = usernamelib[1];
                        Game.get_score(getusernamelib);
                        response.SetContent(Game.get_score(getusernamelib));
                        response.Send(stream);
                    }
                }
            }

            if (request.Method.Contains("DEL"))
                if (request.Url.RawUrl.Contains("lib"))
                {
                    var urlteil = request.Url.RawUrl.Split("/").Last();
                    Console.WriteLine(urlteil);
                    var auth = request.UserAuthorization.Split(new[] {" ", "-ppbgToken"},
                        StringSplitOptions.None);
                    var auth1 = auth[1];
                    Game.del_lib(urlteil, auth1);
                    response.SetContent(Game.del_lib(urlteil, auth1));
                    response.Send(stream);
                }


            if (request.Method.Contains("PUT"))
            {
                if (request.Url.RawUrl.Contains("users"))
                {
                    var updateuser =
                        request.ContentString.Split(new[] {"\"Name\": \" ", "\""}, StringSplitOptions.None);
                    var name = updateuser[3];
                    var bio = updateuser[7];
                    var img = updateuser[11];
                    Console.WriteLine(name + " " + bio + " " + img);
                    var urlteil = request.Url.RawUrl.Split("/").Last();
                    Console.WriteLine(urlteil);
                    var auth = request.UserAuthorization.Split(new[] {" ", "-ppbgToken"},
                        StringSplitOptions.None);
                    var auth1 = auth[1];
                    Console.WriteLine(auth1);

                    if (urlteil.Equals(auth1))
                    {
                        Game.update_user(auth1, name, bio, img);
                        response.SetContent(Game.update_user(auth1, name, bio, img));
                        response.Send(stream);
                    }
                    else
                    {
                        response.SetContent("Not Authorized");
                        response.Send(stream);
                    }
                }

                if (request.Url.RawUrl.Contains("actions"))
                {
                    var auth = request.UserAuthorization.Split(new[] {" ", "-ppbgToken"},
                        StringSplitOptions.None);
                    var auth1 = auth[1];
                    var actions = request.ContentString.Split(new[] {"actions\": \"", "\"}"},
                        StringSplitOptions.None);
                    var action = actions[1];
                    if (((action =="VVVVV" || action == "SSSSS" || action =="RRRRR" ||
                        action=="LLLLL"
                        || action == "PPPPP")&& action.Length == 5))
                    {
                        Game.update_actions(action, auth1);
                        response.SetContent(Game.update_actions(action, auth1));
                    }


                    else
                    {
                        Console.WriteLine("Please enter the correct Actions");
                        response.SetContent("Please enter the correct Actions");
                    }
                   
                    response.Send(stream);
                }

                if (request.Url.RawUrl.Contains("playlist"))
                {
                    var auth = request.UserAuthorization.Split(new[] {" ", "-ppbgToken"},
                        StringSplitOptions.None);
                    var auth1 = auth[1];
                    var id = request.ContentString.Split(new[] {"\"FromPosition\": ", ","}, StringSplitOptions.None);
                    var id1 = id[1];
                    var pos = request.ContentString.Split(new[] {"\"ToPosition\": ", "}"}, StringSplitOptions.None);
                    var pos2 = pos[1];
                    Game.update_playlist(auth1, id1, pos2);
                    response.SetContent(Game.update_playlist(auth1, id1, pos2));
                    response.Send(stream);
                }
            }


            socket.Close();
        }
    }
}
