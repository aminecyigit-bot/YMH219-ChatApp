using System;
using System.Text.Json;

namespace ChatClient.Models
{
    public enum MessageType
    {
        Connect,      // İstemci ilk bağlandığında kullanıcı adını bildirir
        Broadcast,    // Herkese açık genel sohbet mesajı
        Private,      // Belirli bir kullanıcıya özel mesaj (/w kullanıcı mesaj)
        System,       // Sunucu bildirimleri (katıldı, ayrıldı, hata vb.)
        UserList      // Sunucunun istemcilere güncel kullanıcı listesini göndermesi
    }

    public class ChatMessage
    {
        public MessageType Type { get; set; }
        public string Sender { get; set; } = string.Empty;
        public string Recipient { get; set; } = string.Empty; // Özel mesaj için hedef kullanıcı
        public string Content { get; set; } = string.Empty;
        public DateTime Timestamp { get; set; } = DateTime.Now;

        // Nesneyi ağ üzerinden göndermek için tek satırlık JSON metnine çevirir
        public string ToJson()
        {
            return JsonSerializer.Serialize(this);
        }

        // Ağdan gelen JSON satırını tekrar ChatMessage nesnesine dönüştürür
        public static ChatMessage? FromJson(string json)
        {
            try
            {
                return JsonSerializer.Deserialize<ChatMessage>(json);
            }
            catch
            {
                return null;
            }
        }

        // Arayüzde zaman damgalı gösterim formatı
        public string FormatForDisplay()
        {
            string time = Timestamp.ToString("HH:mm:ss");
            return Type switch
            {
                MessageType.Private => $"[{time}] [ÖZEL - {Sender} -> {Recipient}]: {Content}",
                MessageType.System => $"[{time}] [SİSTEM]: {Content}",
                _ => $"[{time}] {Sender}: {Content}"
            };
        }
    }
}