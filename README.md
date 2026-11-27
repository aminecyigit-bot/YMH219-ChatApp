# YMH219-ChatApp — İstemci-Sunucu Sohbet Uygulaması

**Ders:** Nesne Tabanlı Programlama (YMH 219)  
**Öğrenci Numarası:** 250541048  
**GitHub:** [aminecyigit-bot](https://github.com/aminecyigit-bot)

---

## 1. Projenin Amacı
Bu proje, TCP/IP protokolü üzerinden birden fazla istemci örneğinin bir sunucu aracılığıyla gerçek zamanlı ve eş zamanlı olarak mesajlaşabilmesini sağlayan nesne tabanlı bir masaüstü sohbet uygulamasıdır. Uygulama hem genel sohbet odası hem de kullanıcılar arası özel mesajlaşma desteği sunar.

---

## 2. Kullanılan Teknolojiler ve Ders Kavramları
* **Dil ve Çatı:** C# / .NET Windows Forms (WinForms)
* **Ağ İletişimi:** `System.Net.Sockets` (`TcpListener`, `TcpClient`, `NetworkStream`)
* **Veri Akışı ve Serileştirme:** `StreamReader`, `StreamWriter` (UTF-8) ve `System.Text.Json`
* **Asenkron Programlama:** `async` / `await` ve `CancellationToken` ile arayüzü kilitlemeyen ağ dinleme döngüleri
* **Nesne Tabanlı Tasarım (OOP):**
  * `ChatMessage`: Mesaj türlerini (`Connect`, `Broadcast`, `Private`, `System`, `UserList`), gönderici/alıcı bilgilerini ve zaman damgalarını kapsülleyen veri modeli.
  * `ConnectedClient`: Sunucuya bağlanan her istemcinin soket, okuma/yazma akışları ve bağlantı zamanı bilgilerini yöneten `IDisposable` sınıf.
  * `ServerNetworkManager` & `ClientNetworkManager`: Ağ mantığını kullanıcı arayüzünden ayıran ve olay tabanlı (`event Action`) arayüz güncellemesi sağlayan servis sınıfları.

---

## 4. Kurulum ve Çalıştırma Adımları
1. Depoyu bilgisayarınıza klonlayın:
   ```bash
   git clone [https://github.com/aminecyigit-bot/YMH219-ChatApp.git](https://github.com/aminecyigit-bot/YMH219-ChatApp.git)