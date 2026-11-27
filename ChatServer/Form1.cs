using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;
using ChatServer.Models;
using ChatServer.Services;

namespace ChatServer
{
    public partial class Form1 : Form
    {
        private readonly ServerNetworkManager _serverManager = new();

        private TextBox txtPort = null!;
        private Button btnStart = null!;
        private Button btnStop = null!;
        private Label lblStatus = null!;
        private ListView lvClients = null!;
        private ListBox lstLogs = null!;

        public Form1()
        {
            InitializeComponent();
            SetupCustomUI();
            BindEvents();
        }

        private void SetupCustomUI()
        {
            this.Text = "YMH219 - Chat Sunucusu (Server)";
            this.Size = new Size(820, 520);
            this.StartPosition = FormStartPosition.CenterScreen;
            this.Font = new Font("Segoe UI", 9.5F);

            // Üst Kontrol Paneli
            Panel topPanel = new Panel { Dock = DockStyle.Top, Height = 55, Padding = new Padding(10) };

            Label lblPort = new Label { Text = "Port:", AutoSize = true, Location = new Point(12, 18) };
            txtPort = new TextBox { Text = "5000", Width = 80, Location = new Point(55, 15) };

            btnStart = new Button
            {
                Text = "Sunucuyu Başlat",
                Width = 135,
                Height = 30,
                Location = new Point(150, 13),
                BackColor = Color.FromArgb(40, 167, 69),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat
            };

            btnStop = new Button
            {
                Text = "Sunucuyu Durdur",
                Width = 135,
                Height = 30,
                Location = new Point(295, 13),
                BackColor = Color.FromArgb(220, 53, 69),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Enabled = false
            };

            lblStatus = new Label
            {
                Text = "Durum: Kapalı",
                AutoSize = true,
                ForeColor = Color.DarkRed,
                Font = new Font("Segoe UI", 9.5F, FontStyle.Bold),
                Location = new Point(450, 18)
            };

            topPanel.Controls.AddRange(new Control[] { lblPort, txtPort, btnStart, btnStop, lblStatus });

            // Ana Bölünmüş Alan (Sol: Bağlı İstemciler, Sağ: Sunucu Logları)
            SplitContainer split = new SplitContainer
            {
                Dock = DockStyle.Fill,
                SplitterDistance = 370,
                Padding = new Padding(10)
            };

            GroupBox grpClients = new GroupBox { Text = "Bağlı İstemciler", Dock = DockStyle.Fill, Padding = new Padding(8) };
            lvClients = new ListView
            {
                Dock = DockStyle.Fill,
                View = View.Details,
                FullRowSelect = true,
                GridLines = true
            };
            lvClients.Columns.Add("Kullanıcı Adı", 110);
            lvClients.Columns.Add("IP : Port", 145);
            lvClients.Columns.Add("Bağlantı Saati", 90);
            grpClients.Controls.Add(lvClients);

            GroupBox grpLogs = new GroupBox { Text = "Sunucu Olay ve Mesaj Kayıtları", Dock = DockStyle.Fill, Padding = new Padding(8) };
            lstLogs = new ListBox { Dock = DockStyle.Fill, HorizontalScrollbar = true };
            grpLogs.Controls.Add(lstLogs);

            split.Panel1.Controls.Add(grpClients);
            split.Panel2.Controls.Add(grpLogs);

            this.Controls.Add(split);
            this.Controls.Add(topPanel);
        }

        private void BindEvents()
        {
            btnStart.Click += async (s, e) =>
            {
                if (!int.TryParse(txtPort.Text, out int port) || port < 1024 || port > 65535)
                {
                    MessageBox.Show("Lütfen 1024 ile 65535 arasında geçerli bir port numarası girin.", "Geçersiz Port", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                btnStart.Enabled = false;
                txtPort.Enabled = false;
                btnStop.Enabled = true;
                lblStatus.Text = $"Durum: Çalışıyor (Port: {port})";
                lblStatus.ForeColor = Color.DarkGreen;

                await _serverManager.StartAsync(port);
            };

            btnStop.Click += (s, e) =>
            {
                _serverManager.Stop();
                btnStart.Enabled = true;
                txtPort.Enabled = true;
                btnStop.Enabled = false;
                lblStatus.Text = "Durum: Kapalı";
                lblStatus.ForeColor = Color.DarkRed;
            };

            _serverManager.OnLog += message =>
            {
                if (InvokeRequired)
                {
                    Invoke(new Action(() => AppendLog(message)));
                }
                else
                {
                    AppendLog(message);
                }
            };

            _serverManager.OnClientListChanged += clients =>
            {
                if (InvokeRequired)
                {
                    Invoke(new Action(() => RefreshClientList(clients)));
                }
                else
                {
                    RefreshClientList(clients);
                }
            };

            this.FormClosing += (s, e) => _serverManager.Stop();
        }

        private void AppendLog(string message)
        {
            lstLogs.Items.Add(message);
            lstLogs.TopIndex = lstLogs.Items.Count - 1;
        }

        private void RefreshClientList(List<ConnectedClient> clients)
        {
            lvClients.Items.Clear();
            foreach (var client in clients)
            {
                var item = new ListViewItem(client.Username);
                item.SubItems.Add(client.EndPoint);
                item.SubItems.Add(client.ConnectedAt.ToString("HH:mm:ss"));
                lvClients.Items.Add(item);
            }
        }
    }
}