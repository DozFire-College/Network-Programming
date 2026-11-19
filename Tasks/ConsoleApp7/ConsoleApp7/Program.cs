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
        static void Main(string[] args)
        {
            var serv = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            serv.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);
            serv.Bind(new IPEndPoint(IPAddress.Any, 8080));
            serv.Listen(1000);
            Console.WriteLine("Сервер запущен.");
            while (true)
            {
                Socket client = serv.Accept();
                try
                {
                    HandleClient(client);
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
        static void HandleClient(Socket client)
        {
            var buffer = new byte[1024];
            int recive = client.Receive(buffer);
            string request = Encoding.UTF8.GetString(buffer, 0, recive);
            Console.WriteLine("Клиент " + client.RemoteEndPoint + " запросил: " + request);
            string answer;
            if (request == "time")
                answer = "Время на сервере: " + DateTime.Now.ToLongTimeString();
            else if (request == "date")
                answer = "Дата на сервере: " + DateTime.Now.ToShortDateString();
            else
                answer = "Неизвестный запрос";
            byte[] response = Encoding.UTF8.GetBytes(answer);
            client.Send(response);
            Console.WriteLine("Отправлено: " + answer);
        }
    }
}