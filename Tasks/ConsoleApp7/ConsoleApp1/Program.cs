using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace ConsoleApp1
{
    internal class Program
    {
        static void Main(string[] args)
        {
            while (true)
            {
                Console.WriteLine("1 - запросить время");
                Console.WriteLine("2 - запросить дату");
                Console.WriteLine("0 - выход");
                Console.Write("Ваш выбор: ");
                string key = Console.ReadLine();
                string request;
                if (key == "1")
                    request = "time";
                else if (key == "2")
                    request = "date";
                else if (key == "0")
                    break;
                else
                {
                    Console.WriteLine("Неверный ввод");
                    Console.WriteLine();
                    continue;
                }
                var client = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
                try
                {
                    client.Connect(new IPEndPoint(IPAddress.Parse("127.0.0.1"), 8080));
                    client.Send(Encoding.UTF8.GetBytes(request));
                    var buffer = new byte[1024];
                    var builder = new StringBuilder();
                    int recive;
                    while ((recive = client.Receive(buffer)) > 0)
                    {
                        builder.Append(Encoding.UTF8.GetString(buffer, 0, recive));
                    }
                    Console.WriteLine(builder.ToString());
                }
                catch (SocketException e)
                {
                    Console.WriteLine(e.SocketErrorCode + ": " + e.Message);
                }
                finally
                {
                    client.Close();
                }
                Console.WriteLine();
            }
        }
    }
}