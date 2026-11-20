using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace ConsoleApp1
{
    internal class Class1
    {
        Socket client;

        public async Task Connect(string ip, int port)
        {
            client = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            await client.ConnectAsync(new IPEndPoint(IPAddress.Parse(ip), port));
        }

        public async Task<string> GetQuote()
        {
            byte[] data = Encoding.UTF8.GetBytes("quote");
            var segmentS = new ArraySegment<byte>(data, 0, data.Length);
            await client.SendAsync(segmentS, SocketFlags.None);
            var buffer = new byte[1024];
            var segmentR = new ArraySegment<byte>(buffer, 0, buffer.Length);
            int recive = await client.ReceiveAsync(segmentR, SocketFlags.None);
            if (recive == 0)
                throw new SocketException((int)SocketError.ConnectionReset);
            return Encoding.UTF8.GetString(buffer, 0, recive);
        }

        public async Task Disconnect()
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
