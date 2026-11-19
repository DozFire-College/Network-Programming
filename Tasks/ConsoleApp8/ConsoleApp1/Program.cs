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
        static Socket client;

        static async Task Main(string[] args)
        {
            Console.WriteLine("Введите логин");
            string login = Console.ReadLine();
            Console.WriteLine("Введите пароль");
            string password = Console.ReadLine();
            try
            {
                string answer = await Connect("127.0.0.1", 8080);
                if (answer == "login")
                    answer = await Request(login);
                if (answer == "password")
                    answer = await Request(password);
                Console.WriteLine(answer);
                if (answer != "Подключено к серверу.")
                {
                    client.Close();
                    client = null;
                    Console.ReadKey();
                    return;
                }
            }
            catch (SocketException e)
            {
                Console.WriteLine(e.SocketErrorCode + ": " + e.Message);
                Console.ReadKey();
                return;
            }
            while (true)
            {
                Console.WriteLine("1 - получить цитату");
                Console.WriteLine("0 - выход");
                string key = Console.ReadLine();
                if (key == "1")
                {
                    try
                    {
                        string quote = await GetQuote();
                        Console.WriteLine(quote);
                        if (quote.StartsWith("Лимит цитат исчерпан"))
                        {
                            client.Close();
                            client = null;
                            Console.ReadKey();
                            break;
                        }
                    }
                    catch (SocketException e)
                    {
                        Console.WriteLine(e.SocketErrorCode + ": " + e.Message);
                        break;
                    }
                }
                else if (key == "0")
                    break;
                else
                    Console.WriteLine("Неверный ввод");
                Console.WriteLine();
            }
            await Disconnect();
        }

        static async Task<string> Connect(string ip, int port)
        {
            client = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            await client.ConnectAsync(new IPEndPoint(IPAddress.Parse(ip), port));
            var buffer = new byte[1024];
            var segmentR = new ArraySegment<byte>(buffer, 0, buffer.Length);
            int recive = await client.ReceiveAsync(segmentR, SocketFlags.None);
            if (recive == 0)
                throw new SocketException((int)SocketError.ConnectionReset);
            return Encoding.UTF8.GetString(buffer, 0, recive);
        }

        static async Task<string> GetQuote()
        {
            return await Request("quote");
        }

        static async Task<string> Request(string text)
        {
            byte[] data = Encoding.UTF8.GetBytes(text);
            var segmentS = new ArraySegment<byte>(data, 0, data.Length);
            await client.SendAsync(segmentS, SocketFlags.None);
            var buffer = new byte[1024];
            var segmentR = new ArraySegment<byte>(buffer, 0, buffer.Length);
            int recive = await client.ReceiveAsync(segmentR, SocketFlags.None);
            if (recive == 0)
                throw new SocketException((int)SocketError.ConnectionReset);
            return Encoding.UTF8.GetString(buffer, 0, recive);
        }

        static async Task Disconnect()
        {
            if (client == null)
                return;
            try
            {
                byte[] data = Encoding.UTF8.GetBytes("exit");
                var segmentS = new ArraySegment<byte>(data, 0, data.Length);
                await client.SendAsync(segmentS, SocketFlags.None);
                client.Shutdown(SocketShutdown.Both);
            }
            catch (SocketException)
            {
            }
            finally
            {
                client.Close();
                client = null;
            }
        }
    }
}
