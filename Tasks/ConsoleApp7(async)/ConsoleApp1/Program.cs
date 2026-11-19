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
        static async Task Main(string[] args)
        {
            while (true)
            {
                Console.WriteLine("1 - запросить время");
                Console.WriteLine("2 - запросить дату");
                Console.WriteLine("0 - выход");
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
                    continue;
                }
                var client = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
                try
                {
                    await client.ConnectAsync(new IPEndPoint(IPAddress.Parse("127.0.0.1"), 8080));
                    byte[] data = Encoding.UTF8.GetBytes(request);
                    var segmentS = new ArraySegment<byte>(data, 0, data.Length);
                    await client.SendAsync(segmentS, SocketFlags.None);
                    var buffer = new byte[1024];
                    var segmentR = new ArraySegment<byte>(buffer, 0, buffer.Length);
                    var builder = new StringBuilder();
                    int recive;
                    while ((recive = await client.ReceiveAsync(segmentR, SocketFlags.None)) > 0)
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