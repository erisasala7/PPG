using System;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using Npgsql;

namespace PlaylistGame
{
    static class Program
    {
        static void Main(string[] args)
        {
            Server.Listen();
           
          
        }
    }
}