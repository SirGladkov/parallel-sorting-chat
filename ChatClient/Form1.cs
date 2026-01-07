using System;
using System.IO;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Windows.Forms;

namespace ChatClient
{
    public partial class Form1 : Form
    {
        private TcpClient _client;
        private NetworkStream _stream;
        private volatile bool _connected = false;

        public Form1()
        {
            InitializeComponent();
            btnSend.Enabled = false;
            txtMessage.Enabled = false;
        }

        private void btnConnect_Click(object sender, EventArgs e)
        {
            if (!_connected)
            {
                new Thread(ConnectToServer).Start();
            }
            else
            {
                DisconnectFromServer();
            }
        }

        private void ConnectToServer()
        {
            try
            {
                var client = new TcpClient();
                client.Connect("127.0.0.1", 8888);

                if (this.InvokeRequired)
                {
                    this.Invoke((MethodInvoker)delegate
                    {
                        _client = client;
                        _stream = client.GetStream();
                        _connected = true;
                        btnConnect.Text = "Отключиться";
                        txtMessage.Enabled = true;
                        btnSend.Enabled = true;
                        lstMessages.Items.Add("[Соединение установлено]");
                    });
                }

                new Thread(ListenForMessages).Start();
            }
            catch (Exception ex)
            {
                if (this.InvokeRequired)
                {
                    this.Invoke((MethodInvoker)delegate
                    {
                        MessageBox.Show($"Не удалось подключиться к серверу:\n{ex.Message}", "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        btnConnect.Enabled = true;
                    });
                }
            }
        }

        private void ListenForMessages()
        {
            try
            {
                var reader = new StreamReader(_stream, Encoding.UTF8);
                string line;
                while (_connected && (line = reader.ReadLine()) != null)
                {
                    if (this.InvokeRequired)
                    {
                        this.Invoke((MethodInvoker)delegate
                        {
                            lstMessages.Items.Add(line);
                            lstMessages.TopIndex = lstMessages.Items.Count - 1; // автопрокрутка
                        });
                    }
                }
            }
            catch
            {
                // Канал закрыт
            }
            finally
            {
                if (_connected)
                {
                    DisconnectFromServer();
                }
            }
        }

        private void btnSend_Click(object sender, EventArgs e)
        {
            SendMessage();
        }

        private void txtMessage_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter)
            {
                SendMessage();
                e.SuppressKeyPress = true;
            }
        }

        private void SendMessage()
        {
            if (!_connected || string.IsNullOrWhiteSpace(txtMessage.Text))
                return;

            try
            {
                var writer = new StreamWriter(_stream, Encoding.UTF8) { AutoFlush = true };
                writer.WriteLine(txtMessage.Text);
                txtMessage.Clear();
            }
            catch
            {
                DisconnectFromServer();
                MessageBox.Show("Сервер больше не доступен.", "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }

        private void DisconnectFromServer()
        {
            _connected = false;

            _stream?.Close();
            _client?.Close();

            btnConnect.Text = "Подключиться к серверу";
            txtMessage.Enabled = false;
            btnSend.Enabled = false;
            lstMessages.Items.Add("[Отключено от сервера]");
        }

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            if (_connected)
            {
                DisconnectFromServer();
            }
            base.OnFormClosing(e);
        }
    }
}