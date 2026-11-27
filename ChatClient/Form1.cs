using System;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Windows.Forms;
using ChatClient.Models;
using ChatClient.Services;

namespace ChatClient
{
    public partial class Form1 : Form
    {
        private readonly ClientNetworkManager _clientManager = new();

        private TextBox txtHost = null!;
        private TextBox txtPort = null!;
        private TextBox txtUsername = null!;
        private Button btnConnect = null!;
        private Button btnDisconnect = null!;
        private Label lblConnectionStatus = null!;

        private ListBox lstMessages = null!;
        private ListBox lstOnlineUsers = null!;
        private TextBox txtMessage = null!;
        private Button btnSend = null!;
        private Button btnSaveHistory = null!;

        public Form1()
        {
            InitializeComponent();
            SetupCustomUI();
            BindEvents();
        }

        private void SetupCustomUI()
        {
            this.Text = "YMH219 - Chat İstemcisi (Client)";
            this.Size = new Size(860, 560);
            this.StartPosition = FormStartPosition.CenterScreen;
            this.Font = new Font("Segoe UI", 9.5F);

            // Üst Bağlantı Paneli
            Panel topPanel = new Panel { Dock = DockStyle.Top, Height = 65, Padding = new Padding(10) };

            Label lblHost = new Label { Text = "Sunucu IP:", AutoSize = true, Location = new Point(12, 22) };
            txtHost = new TextBox { Text = "127.0.0.1", Width = 110, Location = new Point(82, 19) };

            Label lblPort = new Label { Text = "Port:", AutoSize = true, Location = new Point(202, 22) };
            txtPort = new TextBox { Text = "5000", Width = 60, Location = new Point(240, 19) };

            Label lblUser = new Label { Text = "Kullanıcı Adı:", AutoSize = true, Location = new Point(310, 22) };
            txtUsername = new TextBox { Text = "Amine", Width = 115, Location = new Point(398, 19) };

            btnConnect = new Button
            {
                Text = "Bağlan",
                Width = 95,
                Height = 30,
                Location = new Point(525, 17),
                BackColor = Color.FromArgb(0, 123, 255),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat
            };

            btnDisconnect = new Button
            {
                Text = "Bağlantıyı Kes",
                Width = 115,
                Height = 30,
                Location = new Point(628, 17),
                BackColor = Color.FromArgb(220, 53, 69),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Enabled = false
            };

            topPanel.Controls.AddRange(new Control[]
            {
                lblHost, txtHost, lblPort, txtPort, lblUser, txtUsername, btnConnect, btnDisconnect
            });

            // Alt Durum ve Mesaj Gönderme Paneli
            Panel bottomPanel = new Panel { Dock = DockStyle.Bottom, Height = 75, Padding = new Padding(10) };

            txtMessage = new TextBox
            {
                Location = new Point(12, 10),
                Width = 580,
                Enabled = false,
                PlaceholderText = "Mesajınızı yazın... (Özel mesaj için: /w KullanıcıAdı Mesaj)"
            };

            btnSend = new Button
            {
                Text = "Mesaj Gönder",
                Width = 120,
                Height = 29,
                Location = new Point(602, 8),
                BackColor = Color.FromArgb(40, 167, 69),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Enabled = false
            };

            btnSaveHistory = new Button
            {
                Text = "Geçmişi Kaydet",
                Width = 115,
                Height = 29,
                Location = new Point(728, 8),
                FlatStyle = FlatStyle.Standard
            };

            lblConnectionStatus = new Label
            {
                Text = "Bağlantı Durumu: Bağlı Değil",
                AutoSize = true,
                ForeColor = Color.DarkRed,
                Font = new Font("Segoe UI", 9F, FontStyle.Bold),
                Location = new Point(12, 46)
            };

            bottomPanel.Controls.AddRange(new Control[] { txtMessage, btnSend, btnSaveHistory, lblConnectionStatus });

            // Orta Alan (Sol: Mesaj Listesi, Sağ: Çevrimiçi Kullanıcılar)
            SplitContainer split = new SplitContainer
            {
                Dock = DockStyle.Fill,
                SplitterDistance = 630,
                Padding = new Padding(10, 0, 10, 0)
            };

            GroupBox grpChat = new GroupBox { Text = "Sohbet Mesajları", Dock = DockStyle.Fill, Padding = new Padding(8) };
            lstMessages = new ListBox { Dock = DockStyle.Fill, HorizontalScrollbar = true };
            grpChat.Controls.Add(lstMessages);

            GroupBox grpUsers = new GroupBox { Text = "Çevrimiçi Kullanıcılar (Özel mesaj için çift tıkla)", Dock = DockStyle.Fill, Padding = new Padding(8) };
            lstOnlineUsers = new ListBox { Dock = DockStyle.Fill };
            grpUsers.Controls.Add(lstOnlineUsers);

            split.Panel1.Controls.Add(grpChat);
            split.Panel2.Controls.Add(grpUsers);

            this.Controls.Add(split);
            this.Controls.Add(bottomPanel);
            this.Controls.Add(topPanel);

            this.AcceptButton = btnSend;
        }

        private void BindEvents()
        {
            btnConnect.Click += async (s, e) =>
            {
                string host = txtHost.Text.Trim();
                string username = txtUsername.Text.Trim();

                if (string.IsNullOrWhiteSpace(host) || string.IsNullOrWhiteSpace(username))
                {
                    MessageBox.Show("Lütfen Sunucu IP ve Kullanıcı Adı alanlarını doldurun.", "Eksik Bilgi", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                if (!int.TryParse(txtPort.Text, out int port) || port < 1024 || port > 65535)
                {
                    MessageBox.Show("Lütfen geçerli bir port numarası girin.", "Geçersiz Port", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                try
                {
                    btnConnect.Enabled = false;
                    lblConnectionStatus.Text = "Bağlantı Durumu: Bağlanıyor...";
                    lblConnectionStatus.ForeColor = Color.DarkOrange;

                    await _clientManager.ConnectAsync(host, port, username);

                    SetConnectedState(true);
                    this.Text = $"YMH219 - Chat İstemcisi ({username})";
                }
                catch (Exception ex)
                {
                    SetConnectedState(false);
                    MessageBox.Show($"Sunucuya bağlanılamadı:\n{ex.Message}", "Bağlantı Hatası", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            };

            btnDisconnect.Click += (s, e) =>
            {
                _clientManager.Disconnect();
                SetConnectedState(false);
            };

            btnSend.Click += async (s, e) =>
            {
                if (string.IsNullOrWhiteSpace(txtMessage.Text)) return;

                try
                {
                    string messageToSend = txtMessage.Text;
                    txtMessage.Clear();
                    await _clientManager.SendMessageAsync(messageToSend);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Mesaj gönderilemedi:\n{ex.Message}", "Gönderim Hatası", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            };

            btnSaveHistory.Click += (s, e) =>
            {
                if (lstMessages.Items.Count == 0)
                {
                    MessageBox.Show("Kaydedilecek mesaj bulunmuyor.", "Bilgi", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    return;
                }

                using SaveFileDialog sfd = new SaveFileDialog
                {
                    Filter = "Metin Dosyası (*.txt)|*.txt",
                    FileName = $"ChatGecmisi_{DateTime.Now:yyyyMMdd_HHmmss}.txt"
                };

                if (sfd.ShowDialog() == DialogResult.OK)
                {
                    var lines = lstMessages.Items.Cast<string>();
                    File.WriteAllLines(sfd.FileName, lines);
                    MessageBox.Show("Sohbet geçmişi başarıyla kaydedildi.", "Kaydedildi", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
            };

            lstOnlineUsers.DoubleClick += (s, e) =>
            {
                if (lstOnlineUsers.SelectedItem is string selectedUser && txtMessage.Enabled)
                {
                    txtMessage.Text = $"/w {selectedUser} ";
                    txtMessage.Focus();
                    txtMessage.SelectionStart = txtMessage.Text.Length;
                }
            };

            _clientManager.OnMessageReceived += msg =>
            {
                if (InvokeRequired)
                    Invoke(new Action(() => AppendChatMessage(msg.FormatForDisplay())));
                else
                    AppendChatMessage(msg.FormatForDisplay());
            };

            _clientManager.OnUserListUpdated += users =>
            {
                if (InvokeRequired)
                    Invoke(new Action(() => UpdateOnlineUsers(users)));
                else
                    UpdateOnlineUsers(users);
            };

            _clientManager.OnDisconnected += reason =>
            {
                if (InvokeRequired)
                    Invoke(new Action(() => HandleDisconnect(reason)));
                else
                    HandleDisconnect(reason);
            };

            this.FormClosing += (s, e) => _clientManager.Disconnect();
        }

        private void SetConnectedState(bool connected)
        {
            txtHost.Enabled = !connected;
            txtPort.Enabled = !connected;
            txtUsername.Enabled = !connected;
            btnConnect.Enabled = !connected;

            btnDisconnect.Enabled = connected;
            txtMessage.Enabled = connected;
            btnSend.Enabled = connected;

            if (connected)
            {
                lblConnectionStatus.Text = $"Bağlantı Durumu: Bağlı ({txtHost.Text}:{txtPort.Text})";
                lblConnectionStatus.ForeColor = Color.DarkGreen;
                txtMessage.Focus();
            }
            else
            {
                lblConnectionStatus.Text = "Bağlantı Durumu: Bağlı Değil";
                lblConnectionStatus.ForeColor = Color.DarkRed;
                lstOnlineUsers.Items.Clear();
            }
        }

        private void AppendChatMessage(string line)
        {
            lstMessages.Items.Add(line);
            lstMessages.TopIndex = lstMessages.Items.Count - 1;
        }

        private void UpdateOnlineUsers(string[] users)
        {
            lstOnlineUsers.Items.Clear();
            foreach (string user in users)
            {
                lstOnlineUsers.Items.Add(user);
            }
        }

        private void HandleDisconnect(string reason)
        {
            SetConnectedState(false);
            AppendChatMessage($"[{DateTime.Now:HH:mm:ss}] [SİSTEM]: {reason}");
        }
    }
}