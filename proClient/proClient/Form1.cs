using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace proClient
{
    public partial class Form1 : Form
    {
        Socket client_sock;
        IPAddress serverIP;
        Queue<string> ReceivedPackets;
        Queue<string> SendPackets;
        byte[] rcBuff;
        bool isConnected = false;
        public Form1()
        {
            InitializeComponent();
            rcBuff = new byte[1024];
            ReceivedPackets = new Queue<string>();
            SendPackets = new Queue<string>();
        }

        private void btnConnect_Click(object sender, EventArgs e)
        {
            if (txtIp.Text.Length < 8 || txtPort.Text.Length == 0)
            {
                MessageBox.Show("Error");
                return;
            }
            try
            {
                if (client_sock != null && client_sock.Connected)
                {
                    client_sock.Shutdown(SocketShutdown.Both);
                    client_sock.Close();
                    client_sock.Dispose();
                }
                client_sock = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
                IPEndPoint endPoint = null;
                if (IPAddress.TryParse(txtIp.Text, out serverIP))
                {
                    endPoint = new IPEndPoint(serverIP, int.Parse(txtPort.Text));
                    client_sock.BeginConnect(endPoint, new AsyncCallback(OnConnect), null);
                }
                isConnected = true;
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString(), "Connect");
                isConnected = false;
            }
        }

        private void OnConnect(IAsyncResult AR)
        {
            try
            {
                client_sock.BeginReceive(rcBuff, 0, rcBuff.Length, SocketFlags.None,
                                        new AsyncCallback(ReceiveCallBack), client_sock);

            }
            catch (Exception ex)
            {
                isConnected = false;
                MessageBox.Show(ex.ToString(), "On Connect Function Crashed");
            }
        }
        private void ReceiveCallBack(IAsyncResult AR)
        {
            try
            {
                Socket socket = (Socket)AR.AsyncState;
                int received = socket.EndReceive(AR);

                byte[] buff = new byte[received];
                Bitmap bmp;
                using (var ms = new MemoryStream(buff))
                {
                    bmp = new Bitmap(ms);
                    pictureBox1.Image = bmp;
                }
              //  Array.Copy(rcBuff, buff, received);
              //  string text = Encoding.ASCII.GetString(buff);
                //ReceivedPackets.Enqueue(text);
                socket.BeginReceive(rcBuff, 0, rcBuff.Length, SocketFlags.None, new AsyncCallback(ReceiveCallBack), socket);
            }
            catch (Exception ex)
            {
                isConnected = false;
                MessageBox.Show(ex.ToString());
            }

        }

        private void btnSend_Click(object sender, EventArgs e)
        {
            try
            {
                byte[] senddata = Encoding.ASCII.GetBytes(txtMessage.Text);
                client_sock.BeginSend(senddata, 0, senddata.Length, SocketFlags.None, new AsyncCallback(SendCallBack), client_sock);
                SendPackets.Enqueue(txtMessage.Text);
                txtMessage.Text = "";
            }
            catch (Exception ex)
            {
                isConnected = false;
                MessageBox.Show(ex.ToString());
            }
        }

        private void SendCallBack(IAsyncResult AR)
        {
            try
            {
                Socket socket = (Socket)AR.AsyncState;
                socket.EndSend(AR);
            }
            catch (Exception ex)
            {
                isConnected = false;
                MessageBox.Show(ex.ToString());
            }
        }

        private void timer1_Tick(object sender, EventArgs e)
        {
            if (!isConnected)
            {
                btnConnect.Enabled = true;
                txtIp.Enabled = true;
                txtPort.Enabled = true;
                btnSend.Enabled = false;
                if (client_sock != null && client_sock.Connected)
                {
                    client_sock.Shutdown(SocketShutdown.Both);
                    client_sock.Close();
                    client_sock.Dispose();
                }
                lblIsConnected.ForeColor = Color.Red;
                lblIsConnected.Text = "Peer Not Connected";
            }
            else
            {
                btnConnect.Enabled = false;
                txtIp.Enabled = false;
                txtPort.Enabled = false;
                btnSend.Enabled = true;
                lblIsConnected.ForeColor = Color.Green;
                lblIsConnected.Text = "Peer Connected";
            }
            while (ReceivedPackets.Count > 0)
                txtAllMessages.AppendText("\n Server>>" + ReceivedPackets.Dequeue());

            while (SendPackets.Count > 0)
                txtAllMessages.AppendText("\n Me >> " + SendPackets.Dequeue());
        }

        private void Form1_Load(object sender, EventArgs e)
        {
        }
    }
}
