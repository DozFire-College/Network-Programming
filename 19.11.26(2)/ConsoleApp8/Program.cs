using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace ConsoleApp8
{
    internal class Program
    {
        static string[] quotes =
        {
            "Стремитесь не к успеху, а к ценностям, которые он дает",
            "Самое ценное, что у нас есть, — это будущее...",
            "Любовь не терпит объяснений. Ей нужны поступки",
            "Вдохновение существует, но оно должно найти тебя работающим",
            "Трудности часто готовят обычных людей к необыкновенной судьбе"
        };
        static string login = "коч";
        static string password = "братан";
        static int maxQuotes = 3;
        static int maxClients = 3;
        static int clientsCount = 0;
        static Random rnd = new Random();
        static object locker = new object();

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
                bool full;
                lock (locker)
                {
                    full = clientsCount >= maxClients;
                    if (!full)
                        clientsCount++;
                }
                if (full)
                    _ = Task.Run(() => RejectClient(client));
                else
                    _ = Task.Run(() => HandleClient(client));
            }
        }

        static string GetQuote()
        {
            lock (locker)
            {
                return quotes[rnd.Next(quotes.Length)];
            }
        }

        static async Task RejectClient(Socket client)
        {
            string who = client.RemoteEndPoint.ToString();
            try
            {
                byte[] data = Encoding.UTF8.GetBytes("Сервер переполнен, достигнут максимум клиентов (" + maxClients + "). Соединение разорвано.");
                var segmentS = new ArraySegment<byte>(data, 0, data.Length);
                await client.SendAsync(segmentS, SocketFlags.None);
                client.Shutdown(SocketShutdown.Both);
            }
            catch (Exception e)
            {
                lock (locker)
                {
                    Console.WriteLine(who + ": " + e.Message);
                }
            }
            finally
            {
                client.Close();
            }
            WriteReject(who, "сервер переполнен");
        }

        static void WriteReject(string who, string reason)
        {
            string text = "Отклонён: " + who + " (" + DateTime.Now + ") - " + reason;
            lock (locker)
            {
                Console.Write(text);
                File.AppendAllText("log.txt", text, Encoding.UTF8);
            }
        }

        static async Task HandleClient(Socket client)
        {
            string who = client.RemoteEndPoint.ToString();
            DateTime timeConnect = DateTime.Now;
            var list = new List<string>();
            string error = null;
            try
            {
                var buffer = new byte[1024];
                var segmentR = new ArraySegment<byte>(buffer, 0, buffer.Length);
                byte[] data = Encoding.UTF8.GetBytes("login");
                var segmentA = new ArraySegment<byte>(data, 0, data.Length);
                await client.SendAsync(segmentA, SocketFlags.None);
                int recive = await client.ReceiveAsync(segmentR, SocketFlags.None);
                string userLogin = Encoding.UTF8.GetString(buffer, 0, recive);
                if (userLogin != login)
                    error = "Неверный логин или пароль. Соединение разорвано.";
                else
                {
                    data = Encoding.UTF8.GetBytes("password");
                    segmentA = new ArraySegment<byte>(data, 0, data.Length);
                    await client.SendAsync(segmentA, SocketFlags.None);
                    recive = await client.ReceiveAsync(segmentR, SocketFlags.None);
                    string userPassword = Encoding.UTF8.GetString(buffer, 0, recive);
                    if (userPassword != password)
                        error = "Неверный логин или пароль. Соединение разорвано.";
                }
                if (error != null)
                {
                    data = Encoding.UTF8.GetBytes(error);
                    segmentA = new ArraySegment<byte>(data, 0, data.Length);
                    await client.SendAsync(segmentA, SocketFlags.None);
                    client.Shutdown(SocketShutdown.Both);
                    return;
                }
                data = Encoding.UTF8.GetBytes("Подключено к серверу.");
                segmentA = new ArraySegment<byte>(data, 0, data.Length);
                await client.SendAsync(segmentA, SocketFlags.None);
                lock (locker)
                {
                    Console.WriteLine("Подключился: " + who + " (" + timeConnect + ")");
                }
                while (true)
                {
                    recive = await client.ReceiveAsync(segmentR, SocketFlags.None);
                    if (recive == 0)
                        break;
                    string request = Encoding.UTF8.GetString(buffer, 0, recive);
                    if (request == "exit")
                        break;
                    string answer;
                    if (request == "quote")
                    {
                        if (list.Count >= maxQuotes)
                        {
                            byte[] limit = Encoding.UTF8.GetBytes("Лимит цитат исчерпан (" + maxQuotes + "). Сервер разорвал соединение.");
                            var segmentL = new ArraySegment<byte>(limit, 0, limit.Length);
                            await client.SendAsync(segmentL, SocketFlags.None);
                            client.Shutdown(SocketShutdown.Both);
                            break;
                        }
                        answer = GetQuote();
                        list.Add(answer);
                    }
                    else
                        answer = "Не понятный запрос";
                    byte[] response = Encoding.UTF8.GetBytes(answer);
                    var segmentS = new ArraySegment<byte>(response, 0, response.Length);
                    await client.SendAsync(segmentS, SocketFlags.None);
                }
            }
            catch (Exception e)
            {
                lock (locker)
                {
                    Console.WriteLine(who + ": " + e.Message);
                }
            }
            finally
            {
                client.Close();
                lock (locker)
                {
                    clientsCount--;
                }
                if (error != null)
                    WriteReject(who, error);
            }
            WriteLog(who, timeConnect, list, DateTime.Now);
        }

        static void WriteLog(string who, DateTime timeConnect, List<string> list, DateTime timeDisconnect)
        {
            var builder = new StringBuilder();
            builder.AppendLine("Кто подключился: " + who);
            builder.AppendLine("Когда подключился: " + timeConnect);
            builder.AppendLine("Набор цитат:");
            if (list.Count == 0)
                builder.AppendLine("    нет");
            foreach (string q in list)
                builder.AppendLine("    " + q);
            builder.AppendLine("Время отключения: " + timeDisconnect);
            lock (locker)
            {
                Console.Write(builder.ToString());
                File.AppendAllText("log.txt", builder.ToString(), Encoding.UTF8);
            }
        }
    }
}
