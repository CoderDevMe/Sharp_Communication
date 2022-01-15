using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace proServer
{
    public partial class Form1 : Form
    {
        Socket serverSocket;
        List<Socket> lstClients;
        Queue<string> ReceivedPackets;
        Queue<string> SendPackets;
        byte[] rcBuff;
        bool isServerConnected = false;

        public Form1()
        {
            InitializeComponent();
            ReceivedPackets = new Queue<string>();
            SendPackets = new Queue<string>();
            rcBuff = new byte[1024];

        }

        private void Form1_Load(object sender, EventArgs e)
        {
            try
            {
                var host = Dns.GetHostEntry(Dns.GetHostName());
                foreach (var ip in host.AddressList)
                {
                    if (ip.AddressFamily == AddressFamily.InterNetwork)
                    {
                        cmbIP.Items.Add(ip.ToString());
                    }
                }
                cmbIP.Items.Add("127.0.0.1");
                lstClients = new List<Socket>();
                serverSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString());
            }
        }

        private void btnSend_Click(object sender, EventArgs e)
        {
            try
            {
                byte[] senddata = Encoding.ASCII.GetBytes(txtMessage.Text);
                lstClients[lstClients.Count - 1].BeginSend(senddata, 0, senddata.Length, SocketFlags.None,
                                        new AsyncCallback(SendCallBack), lstClients[lstClients.Count - 1]);
                SendPackets.Enqueue(txtMessage.Text);
                txtMessage.Text = "";
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString(), "send button");
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
                MessageBox.Show(ex.ToString(), "send method");
            }
        }

        private void btnStart_Click(object sender, EventArgs e)
        {
            try
            {
                IPAddress myIP = IPAddress.Parse(cmbIP.Text);
                IPEndPoint myEndpoint = new IPEndPoint(myIP, int.Parse(txtPort.Text));
                serverSocket.Bind(myEndpoint);
                serverSocket.Listen(20);
                serverSocket.BeginAccept(new AsyncCallback(onAccept), serverSocket);
                isServerConnected = true;
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString(), "start server");
            }
        }

        private void onAccept(IAsyncResult AR)
        {
            try
            {
                Socket socket = (Socket)AR.AsyncState;
                lstClients.Add(socket.EndAccept(AR));
                lstClients[lstClients.Count - 1].BeginReceive(rcBuff, 0, rcBuff.Length, SocketFlags.None,
                                new AsyncCallback(ReceiveCallBack), lstClients[lstClients.Count - 1]);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString(), "On Accept");
            }
        }

        private void ReceiveCallBack(IAsyncResult AR)
        {
            try
            {
                Socket socket = (Socket)AR.AsyncState;
                int received = socket.EndReceive(AR);

                byte[] buff = new byte[received];
                Array.Copy(rcBuff, buff, received);
                string text = Encoding.ASCII.GetString(buff);
                ReceivedPackets.Enqueue(text);
                socket.BeginReceive(rcBuff, 0, rcBuff.Length, SocketFlags.None, new AsyncCallback(ReceiveCallBack), socket);

            }
            catch (Exception ex)
            {
                lstClients[lstClients.Count - 1].Shutdown(SocketShutdown.Both);
                lstClients[lstClients.Count - 1].Close();
                lstClients[lstClients.Count - 1].Dispose();
                lstClients.RemoveAt(lstClients.Count - 1);
                serverSocket.BeginAccept(new AsyncCallback(onAccept), serverSocket);
                MessageBox.Show(ex.ToString(), "receive nethod");
            }
        }

        private void timer1_Tick(object sender, EventArgs e)
        {
            if (!isServerConnected)
            {
                txtPort.Enabled = true;
                cmbIP.Enabled = true;
                btnStart.Enabled = true;
                btnSend.Enabled = false;
                if (serverSocket != null && serverSocket.Connected)
                {
                    serverSocket.Disconnect(true);
                    serverSocket.Close();
                }
            }


            else
            {
                txtPort.Enabled = false;
                cmbIP.Enabled = false;
                btnStart.Enabled = false;
                btnSend.Enabled = true;
            }

            if (lstClients.Count > 0)
            {
                lblIsConnected.ForeColor = Color.Green;
                lblIsConnected.Text = "Peer Connected";
            }
            else
            {
                lblIsConnected.ForeColor = Color.Red;
                lblIsConnected.Text = "Peer Not Connected";
            }
            while (ReceivedPackets.Count > 0)
                txtAllMessages.AppendText("\n Client >>" + ReceivedPackets.Dequeue());
            while (SendPackets.Count > 0)
                txtAllMessages.AppendText("\n Me >> " + SendPackets.Dequeue());
        }
    }
}
