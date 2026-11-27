using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using ChatServer.Models;

namespace ChatServer.Services
{
    public class ServerNetworkManager
    {
        private TcpListener? _listener;
        private CancellationTokenSource? _cts;
        private readonly List<ConnectedClient> _clients = new();
        private readonly object _lock = new();

        public bool IsRunning { get; private set; }

        // Arayüzü güvenli şekilde güncellemek için olaylar (Events)
        public event Action<string>? OnLog;
        public event Action<List<ConnectedClient>>? OnClientListChanged;

        public async Task StartAsync(int port)
        {
            if (IsRunning) return;

            _cts = new CancellationTokenSource();
            _listener = new TcpListener(IPAddress.Any, port);
            _listener.Start();
            IsRunning = true;

            OnLog?.Invoke($"[{DateTime.Now:HH:mm:ss}] Sunucu {port} portunda başlatıldı. İstemciler bekleniyor...");

            try
            {
                while (!_cts.Token.IsCancellationRequested)
                {
                    TcpClient tcpClient = await _listener.AcceptTcpClientAsync(_cts.Token);
                    _ = HandleClientAsync(tcpClient, _cts.Token);
                }
            }
            catch (OperationCanceledException)
            {
                // Sunucu normal şekilde durduruldu
            }
            catch (Exception ex)
            {
                OnLog?.Invoke($"[{DateTime.Now:HH:mm:ss}] [HATA] Sunucu dinleme hatası: {ex.Message}");
            }
        }

        public void Stop()
        {
            if (!IsRunning) return;

            IsRunning = false;
            _cts?.Cancel();
            _listener?.Stop();

            lock (_lock)
            {
                foreach (var client in _clients)
                {
                    client.Dispose();
                }
                _clients.Clear();
            }

            NotifyClientListChanged();
            OnLog?.Invoke($"[{DateTime.Now:HH:mm:ss}] Sunucu durduruldu.");
        }

        private async Task HandleClientAsync(TcpClient tcpClient, CancellationToken token)
        {
            ConnectedClient? client = null;
            try
            {
                client = new ConnectedClient(tcpClient);

                // İlk satırda istemciden Connect paketini (kullanıcı adını) bekliyoruz
                string? firstLine = await client.Reader.ReadLineAsync(token);
                if (string.IsNullOrWhiteSpace(firstLine))
                {
                    client.Dispose();
                    return;
                }

                ChatMessage? connectMsg = ChatMessage.FromJson(firstLine);
                if (connectMsg == null || connectMsg.Type != MessageType.Connect || string.IsNullOrWhiteSpace(connectMsg.Sender))
                {
                    client.Dispose();
                    return;
                }

                string requestedName = connectMsg.Sender.Trim();

                lock (_lock)
                {
                    // Aynı kullanıcı adı varsa sonuna rakam ekle
                    if (_clients.Any(c => c.Username.Equals(requestedName, StringComparison.OrdinalIgnoreCase)))
                    {
                        requestedName = $"{requestedName}_{new Random().Next(10, 99)}";
                    }
                    client.Username = requestedName;
                    _clients.Add(client);
                }

                OnLog?.Invoke($"[{DateTime.Now:HH:mm:ss}] Bağlandı: {client.Username} ({client.EndPoint})");
                NotifyClientListChanged();

                // Tüm istemcilere yeni katılan kullanıcıyı bildir ve güncel listeyi gönder
                await BroadcastMessageAsync(new ChatMessage
                {
                    Type = MessageType.System,
                    Sender = "Sunucu",
                    Content = $"{client.Username} sohbete katıldı."
                });
                await BroadcastUserListAsync();

                // İstemciden gelen mesajları sürekli dinle
                while (!token.IsCancellationRequested)
                {
                    string? jsonLine = await client.Reader.ReadLineAsync(token);
                    if (jsonLine == null) break; // Bağlantı kapandı

                    ChatMessage? incoming = ChatMessage.FromJson(jsonLine);
                    if (incoming == null) continue;

                    incoming.Sender = client.Username;
                    incoming.Timestamp = DateTime.Now;

                    if (incoming.Type == MessageType.Private && !string.IsNullOrWhiteSpace(incoming.Recipient))
                    {
                        await RoutePrivateMessageAsync(client, incoming);
                    }
                    else
                    {
                        incoming.Type = MessageType.Broadcast;
                        OnLog?.Invoke(incoming.FormatForDisplay());
                        await BroadcastMessageAsync(incoming);
                    }
                }
            }
            catch (Exception)
            {
                // İstemci bağlantısı aniden koptuğunda uygulama çökmeden yakalanır
            }
            finally
            {
                if (client != null)
                {
                    bool removed = false;
                    lock (_lock)
                    {
                        removed = _clients.Remove(client);
                    }

                    if (removed)
                    {
                        OnLog?.Invoke($"[{DateTime.Now:HH:mm:ss}] Ayrıldı: {client.Username} ({client.EndPoint})");
                        NotifyClientListChanged();

                        await BroadcastMessageAsync(new ChatMessage
                        {
                            Type = MessageType.System,
                            Sender = "Sunucu",
                            Content = $"{client.Username} sohbetten ayrıldı."
                        });
                        await BroadcastUserListAsync();
                    }

                    client.Dispose();
                }
            }
        }

        private async Task RoutePrivateMessageAsync(ConnectedClient senderClient, ChatMessage msg)
        {
            ConnectedClient? targetClient;
            lock (_lock)
            {
                targetClient = _clients.FirstOrDefault(c =>
                    c.Username.Equals(msg.Recipient.Trim(), StringComparison.OrdinalIgnoreCase));
            }

            if (targetClient == null)
            {
                await SendToClientAsync(senderClient, new ChatMessage
                {
                    Type = MessageType.System,
                    Sender = "Sunucu",
                    Content = $"'{msg.Recipient}' adlı kullanıcı çevrimiçi değil."
                });
                return;
            }

            OnLog?.Invoke(msg.FormatForDisplay());
            await SendToClientAsync(targetClient, msg);
            if (targetClient != senderClient)
            {
                await SendToClientAsync(senderClient, msg);
            }
        }

        private async Task BroadcastUserListAsync()
        {
            string joinedUsers;
            lock (_lock)
            {
                joinedUsers = string.Join(",", _clients.Select(c => c.Username));
            }

            await BroadcastMessageAsync(new ChatMessage
            {
                Type = MessageType.UserList,
                Sender = "Sunucu",
                Content = joinedUsers
            });
        }

        private async Task BroadcastMessageAsync(ChatMessage message)
        {
            List<ConnectedClient> snapshot;
            lock (_lock)
            {
                snapshot = _clients.ToList();
            }

            string json = message.ToJson();
            foreach (var client in snapshot)
            {
                try
                {
                    await client.Writer.WriteLineAsync(json);
                }
                catch { }
            }
        }

        private static async Task SendToClientAsync(ConnectedClient client, ChatMessage message)
        {
            try
            {
                await client.Writer.WriteLineAsync(message.ToJson());
            }
            catch { }
        }

        private void NotifyClientListChanged()
        {
            List<ConnectedClient> snapshot;
            lock (_lock)
            {
                snapshot = _clients.ToList();
            }
            OnClientListChanged?.Invoke(snapshot);
        }
    }
}