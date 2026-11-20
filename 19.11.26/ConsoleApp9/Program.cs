using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace ConsoleApp9
{
    internal class Program
    {
        static string alphabetRu = "абвгдеёжзийклмнопрстуфхцчшщъыьэюя";
        static string alphabetEn = "abcdefghijklmnopqrstuvwxyz";
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
                _ = Task.Run(() => HandleClient(client));
            }
        }

        static string Caesar(string text, int shift)
        {
            var builder = new StringBuilder();
            foreach (char c in text)
                builder.Append(ShiftChar(c, shift));
            return builder.ToString();
        }

        static char ShiftChar(char c, int shift)
        {
            char lower = char.ToLower(c);
            string alphabet;
            if (alphabetRu.IndexOf(lower) >= 0)
                alphabet = alphabetRu;
            else if (alphabetEn.IndexOf(lower) >= 0)
                alphabet = alphabetEn;
            else
                return c;
            int n = alphabet.Length;
            int index = (alphabet.IndexOf(lower) + shift % n + n) % n;
            char result = alphabet[index];
            if (char.IsUpper(c))
                result = char.ToUpper(result);
            return result;
        }

        static async Task HandleClient(Socket client)
        {
            string who = client.RemoteEndPoint.ToString();
            WriteLog("Подключился: " + who + " (" + DateTime.Now + ")");
            try
            {
                var buffer = new byte[4096];
                var segmentR = new ArraySegment<byte>(buffer, 0, buffer.Length);
                while (true)
                {
                    int recive = await client.ReceiveAsync(segmentR, SocketFlags.None);
                    if (recive == 0)
                        break;
                    string request = Encoding.UTF8.GetString(buffer, 0, recive);
                    if (request == "exit")
                        break;
                    string answer;
                    int index = request.IndexOf('|');
                    int shift;
                    if (index < 0 || !int.TryParse(request.Substring(0, index), out shift))
                    {
                        answer = "Не понятный запрос";
                        WriteMessage(who, request, null, answer);
                    }
                    else
                    {
                        string text = request.Substring(index + 1);
                        if (text.Length == 0)
                            answer = "Пустой текст";
                        else
                            answer = Caesar(text, shift);
                        WriteMessage(who, text, shift, answer);
                    }
                    byte[] response = Encoding.UTF8.GetBytes(answer);
                    var segmentS = new ArraySegment<byte>(response, 0, response.Length);
                    await client.SendAsync(segmentS, SocketFlags.None);
                }
            }
            catch (Exception e)
            {
                WriteLog(who + ": " + e.Message);
            }
            finally
            {
                client.Close();
                WriteLog("Отключился: " + who + " (" + DateTime.Now + ")");
            }
        }

        static void WriteMessage(string who, string text, int? shift, string answer)
        {
            var builder = new StringBuilder();
            builder.AppendLine("Кто отправил: " + who);
            builder.AppendLine("Когда отправил: " + DateTime.Now);
            builder.AppendLine("Текст: " + text);
            builder.AppendLine("Сдвиг: " + (shift.HasValue ? shift.ToString() : "нет"));
            builder.AppendLine("Ответ: " + answer);
            WriteLog(builder.ToString().TrimEnd());
        }

        static void WriteLog(string text)
        {
            lock (locker)
            {
                Console.WriteLine(text);
                File.AppendAllText("log.txt", text + Environment.NewLine, Encoding.UTF8);
            }
        }
    }
}
