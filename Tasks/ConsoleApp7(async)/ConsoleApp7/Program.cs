using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace ConsoleApp7
{
    internal class Program
    {
        static async Task Main(string[] args)
        {
            var serv = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            serv.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);
            serv.Bind(new IPEndPoint(IPAddress.Any, 8080));
            serv.Listen(1000);
            Console.WriteLine("Сервер запущен.");
            while (true)
            {
                Socket client = await serv.AcceptAsync();
                try
                {
                    await HandleClient(client);
                }
                catch (Exception e)
                {
                    Console.WriteLine(e.Message);
                }
                finally
                {
                    client.Close();
                }
            }
        }
        static async Task HandleClient(Socket client)
        {
            var buffer = new byte[1024];
            var segmentR = new ArraySegment<byte>(buffer, 0, buffer.Length);
            int recive = await client.ReceiveAsync(segmentR, SocketFlags.None);
            string request = Encoding.UTF8.GetString(buffer, 0, recive);
            string answer;
            if (request == "time")
                answer = "Время на сервере: " + DateTime.Now.ToLongTimeString();
            else if (request == "date")
                answer = "Дата на сервере: " + DateTime.Now.ToShortDateString();
            else
                answer = "Не понятный запрос";
            byte[] response = Encoding.UTF8.GetBytes(answer);
            var segmentS = new ArraySegment<byte>(response, 0, response.Length);
            await client.SendAsync(segmentS, SocketFlags.None);
            Console.WriteLine("Отправлено: " + answer);
        }
    }
}