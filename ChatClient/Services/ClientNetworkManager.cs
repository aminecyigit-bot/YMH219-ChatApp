using System;
using System.IO;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using ChatClient.Models;

namespace ChatClient.Services
{
    public class ClientNetworkManager
    {
        private TcpClient? _tcpClient;
        private StreamReader? _reader;
        private StreamWriter? _writer;
        private CancellationTokenSource? _cts;

        public bool IsConnected => _tcpClient?.Connected ?? false;
        public string Username { get; private set; } = string.Empty;

        public event Action<ChatMessage>? OnMessageReceived;
        public event Action<string[]>? OnUserListUpdated;
        public event Action<string>? OnDisconnected;

        public async Task ConnectAsync(string host, int port, string username)
        {
            if (IsConnected) return;

            Username = username.Trim();
            _tcpClient = new TcpClient();
            _cts = new CancellationTokenSource();

            // Bağlantı kurulumu (hata olursa çağıran Form'da yakalanıp kullanıcıya gösterilir)
            await _tcpClient.ConnectAsync(host, port);

            NetworkStream stream = _tcpClient.GetStream();
            _reader = new StreamReader(stream, Encoding.UTF8);
            _writer = new StreamWriter(stream, Encoding.UTF8) { AutoFlush = true };

            // İlk paket olarak sunucuya kullanıcı adını bildir
            ChatMessage connectPacket = new ChatMessage
            {
                Type = MessageType.Connect,
                Sender = Username,
                Content = "Merhaba"
            };
            await _writer.WriteLineAsync(connectPacket.ToJson());

            // Arka planda sunucudan gelen mesajları dinlemeye başla
            _ = ListenServerAsync(_cts.Token);
        }

        private async Task ListenServerAsync(CancellationToken token)
        {
            string disconnectReason = "Sunucu ile bağlantı kesildi.";
            try
            {
                while (!token.IsCancellationRequested && _reader != null)
                {
                    string? jsonLine = await _reader.ReadLineAsync(token);
                    if (jsonLine == null)
                    {
                        disconnectReason = "Sunucu bağlantıyı kapattı.";
                        break;
                    }

                    ChatMessage? msg = ChatMessage.FromJson(jsonLine);
                    if (msg == null) continue;

                    if (msg.Type == MessageType.UserList)
                    {
                        string[] users = string.IsNullOrWhiteSpace(msg.Content)
                            ? Array.Empty<string>()
                            : msg.Content.Split(',', StringSplitOptions.RemoveEmptyEntries);
                        OnUserListUpdated?.Invoke(users);
                    }
                    else
                    {
                        OnMessageReceived?.Invoke(msg);
                    }
                }
            }
            catch (OperationCanceledException)
            {
                disconnectReason = "Bağlantı kullanıcı tarafından kesildi.";
            }
            catch (Exception ex)
            {
                disconnectReason = $"Bağlantı hatası: {ex.Message}";
            }
            finally
            {
                Cleanup();
                OnDisconnected?.Invoke(disconnectReason);
            }
        }

        public async Task SendMessageAsync(string rawInput)
        {
            if (!IsConnected || _writer == null || string.IsNullOrWhiteSpace(rawInput))
                return;

            ChatMessage msg;
            string trimmed = rawInput.Trim();

            // Özel mesaj kontrolü: /w HedefKullanici Mesaj içeriği
            if (trimmed.StartsWith("/w ", StringComparison.OrdinalIgnoreCase))
            {
                string[] parts = trimmed.Split(' ', 3, StringSplitOptions.RemoveEmptyEntries);
                if (parts.Length < 3)
                {
                    OnMessageReceived?.Invoke(new ChatMessage
                    {
                        Type = MessageType.System,
                        Sender = "İstemci",
                        Content = "Özel mesaj formatı: /w KullanıcıAdı Mesajınız"
                    });
                    return;
                }

                msg = new ChatMessage
                {
                    Type = MessageType.Private,
                    Sender = Username,
                    Recipient = parts[1],
                    Content = parts[2]
                };
            }
            else
            {
                msg = new ChatMessage
                {
                    Type = MessageType.Broadcast,
                    Sender = Username,
                    Content = trimmed
                };
            }

            await _writer.WriteLineAsync(msg.ToJson());
        }

        public void Disconnect()
        {
            _cts?.Cancel();
            Cleanup();
        }

        private void Cleanup()
        {
            try { _reader?.Dispose(); } catch { }
            try { _writer?.Dispose(); } catch { }
            try { _tcpClient?.Close(); } catch { }
            _reader = null;
            _writer = null;
            _tcpClient = null;
        }
    }
}