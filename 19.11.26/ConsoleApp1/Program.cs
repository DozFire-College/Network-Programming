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
        static int shift = 3;

        static async Task Main(string[] args)
        {
            try
            {
                await Connect("127.0.0.1", 8080);
                Console.WriteLine("Подключено к серверу.");
            }
            catch (SocketException e)
            {
                Console.WriteLine(e.SocketErrorCode + ": " + e.Message);
                Console.ReadKey();
                return;
            }
            while (true)
            {
                Console.WriteLine("Текущий сдвиг: " + shift);
                Console.WriteLine("1 - зашифровать текст");
                Console.WriteLine("2 - указать сдвиг");
                Console.WriteLine("0 - выход");
                string key = Console.ReadLine();
                if (key == "1")
                {
                    Console.WriteLine("Введите текст");
                    string text = Console.ReadLine();
                    if (string.IsNullOrEmpty(text))
                        Console.WriteLine("Текст не может быть пустым");
                    else
                    {
                        try
                        {
                            string answer = await Request(shift + "|" + text);
                            Console.WriteLine("Шифр: " + answer);
                        }
                        catch (SocketException e)
                        {
                            Console.WriteLine(e.SocketErrorCode + ": " + e.Message);
                            Console.ReadKey();
                            break;
                        }
                    }
                }
                else if (key == "2")
                {
                    Console.WriteLine("Введите сдвиг");
                    int value;
                    if (int.TryParse(Console.ReadLine(), out value))
                        shift = value;
                    else
                        Console.WriteLine("Сдвиг должен быть числом");
                }
                else if (key == "0")
                    break;
                else
                    Console.WriteLine("Неверный ввод");
                Console.WriteLine();
            }
            await Disconnect();
        }

        static async Task Connect(string ip, int port)
        {
            client = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            await client.ConnectAsync(new IPEndPoint(IPAddress.Parse(ip), port));
        }

        static async Task<string> Request(string text)
        {
            byte[] data = Encoding.UTF8.GetBytes(text);
            var segmentS = new ArraySegment<byte>(data, 0, data.Length);
            await client.SendAsync(segmentS, SocketFlags.None);
            var buffer = new byte[4096];
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
