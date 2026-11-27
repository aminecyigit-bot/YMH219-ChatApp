using System;
using System.IO;
using System.Net.Sockets;
using System.Text;

namespace ChatServer.Models
{
    public class ConnectedClient : IDisposable
    {
        public string Username { get; set; } = "Bilinmeyen";
        public string EndPoint { get; }
        public DateTime ConnectedAt { get; }
        public TcpClient TcpClient { get; }
        public StreamReader Reader { get; }
        public StreamWriter Writer { get; }

        public ConnectedClient(TcpClient tcpClient)
        {
            TcpClient = tcpClient;
            EndPoint = tcpClient.Client.RemoteEndPoint?.ToString() ?? "Bilinmiyor";
            ConnectedAt = DateTime.Now;

            NetworkStream stream = tcpClient.GetStream();
            Reader = new StreamReader(stream, Encoding.UTF8);
            Writer = new StreamWriter(stream, Encoding.UTF8) { AutoFlush = true };
        }

        public void Dispose()
        {
            try { Reader?.Dispose(); } catch { }
            try { Writer?.Dispose(); } catch { }
            try { TcpClient?.Close(); } catch { }
        }
    }
}