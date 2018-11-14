using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Net;
using System.Net.Sockets;

namespace UdpMulticastChat
{
    public partial class MainForm : Form
    {
        bool alive = false; // будет ли работать поток для приема
        UdpClient client;
        //const int LOCALPORT = 8001; // порт для приема сообщений
        //const int REMOTEPORT = 8001; // порт для отправки сообщений
        //const int TTL = 20;
        //const string HOST = "235.5.5.1"; // хост для групповой рассылки

        int LOCALPORT; // порт для приема сообщений
        int REMOTEPORT; // порт для отправки сообщений
        const int TTL = 20;
        string HOST; // хост для групповой рассылки
        IPAddress groupAddress; // адрес для групповой рассылки

        string userName; // имя пользователя в чате
        public MainForm()
        {
            InitializeComponent();
            loginButton.Enabled = true; // кнопка входа
            logoutButton.Enabled = false; // кнопка выхода
            sendButton.Enabled = false; // кнопка отправки
            chatTextBox.ReadOnly = true; // поле для сообщений 
            HOST = "235.5.5.1";
            REMOTEPORT = LOCALPORT = 8001;
            txbIp.Text = HOST;
            txbPort.Text = REMOTEPORT.ToString();
            groupAddress = IPAddress.Parse(HOST);
            loginButton.Click += loginButton_Click;
            sendButton.Click += sendButton_Click;
            logoutButton.Click += logoutButton_Click;
            this.FormClosing += Form1_FormClosing;
        }
        // обработчик нажатия кнопки loginButton
        private void loginButton_Click(object sender, EventArgs e)
        {
            if (!IPAddress.TryParse(txbIp.Text, out groupAddress)) {
                HOST = "235.5.5.1";
                txbIp.Text = HOST;
                groupAddress = IPAddress.Parse(HOST);
            }
            if (int.TryParse(txbPort.Text, out REMOTEPORT))
                LOCALPORT = REMOTEPORT;
            else
            {
                REMOTEPORT = LOCALPORT = 8001;
                txbPort.Text = "8001";
            }
            userName = userNameTextBox.Text;
            userNameTextBox.ReadOnly = true;
            try
            {
                client = new UdpClient(LOCALPORT);
                // присоединяемся к групповой рассылке
                client.JoinMulticastGroup(groupAddress, TTL);

                // запускаем задачу на прием сообщений
                Task receiveTask = new Task(ReceiveMessages);
                receiveTask.Start();

                // отправляем первое сообщение о входе нового пользователя
                string message = userName + " вошел в чат";
                byte[] data = Encoding.Unicode.GetBytes(message);
                client.Send(data, data.Length, HOST, REMOTEPORT);

                loginButton.Enabled = false;
                logoutButton.Enabled = true;
                sendButton.Enabled = true;
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }
        // метод приема сообщений
        private void ReceiveMessages()
        {
            alive = true;
            try
            {
                while (alive)
                {
                    IPEndPoint remoteIp = null;
                    byte[] data = client.Receive(ref remoteIp);
                    string message = Encoding.Unicode.GetString(data);

                    // добавляем полученное сообщение в текстовое поле
                    this.Invoke(new MethodInvoker(() =>
                    {
                        string time = DateTime.Now.ToShortTimeString();
                        chatTextBox.Text = time + " " + message + "\r\n" + chatTextBox.Text;
                    }));
                }
            }
            catch (ObjectDisposedException)
            {
                if (!alive)
                    return;
                throw;
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }
        // обработчик нажатия кнопки sendButton
        private void sendButton_Click(object sender, EventArgs e)
        {
            try
            {
                string message = String.Format("{0}: {1}", userName, messageTextBox.Text);
                byte[] data = Encoding.Unicode.GetBytes(message);
                client.Send(data, data.Length, HOST, REMOTEPORT);
                messageTextBox.Clear();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }
        // обработчик нажатия кнопки logoutButton
        private void logoutButton_Click(object sender, EventArgs e)
        {
            ExitChat();
        }
        // выход из чата
        private void ExitChat()
        {
            string message = userName + " покидает чат";
            byte[] data = Encoding.Unicode.GetBytes(message);
            client.Send(data, data.Length, HOST, REMOTEPORT);
            client.DropMulticastGroup(groupAddress);

            alive = false;
            client.Close();

            loginButton.Enabled = true;
            logoutButton.Enabled = false;
            sendButton.Enabled = false;
        }
        // обработчик события закрытия формы
        private void Form1_FormClosing(object sender, FormClosingEventArgs e)
        {
            if (alive)
                ExitChat();
        }
    }
}
