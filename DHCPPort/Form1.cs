using System;
using System.Net;
using System.Collections.Generic;

using System.Linq;
using System.Text;

using System.Windows.Forms;
using DotNetProjects.DhcpServer;
using TCPClientLib;


namespace DHCPPort
{
    public partial class Form1 : Form
    {
        static byte nextIP = 10;
        static Dictionary<string, IPAddress> leases = new Dictionary<string, IPAddress>();
        DHCPServer server = null;
        TCPClient tcpclient = null;
        public Form1()
        {
            InitializeComponent();
            Console.WriteLine("Initilized Completed");
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            Console.WriteLine("Form Loaded");
        }

        private void Form1_FormClosing(object sender, FormClosingEventArgs e)
        {
            server.Dispose();
        }

        static void Request(DHCPRequest dhcpRequest)
        {
            try
            {
                var type = dhcpRequest.GetMsgType();
                var mac = ByteArrayToString(dhcpRequest.GetChaddr());
                // IP for client
                IPAddress ip;
                if (!leases.TryGetValue(mac, out ip))
                {
                    ip = new IPAddress(new byte[] { 192, 168, 1, nextIP++ });
                    leases[mac] = ip;
                }
                Console.WriteLine(type.ToString() + " request from " + mac + ", it will be " + ip.ToString());

                var options = dhcpRequest.GetAllOptions();
                Console.Write("Options:");
                foreach (DHCPOption option in options.Keys)
                {
                    Console.WriteLine(option.ToString() + ": " + ByteArrayToString(options[option]));
                }
                // Lets show some request info
                var requestedOptions = dhcpRequest.GetRequestedOptionsList();
                if (requestedOptions != null)
                {
                    Console.Write("Requested options:");
                    foreach (DHCPOption option in requestedOptions) Console.Write(" " + option.ToString());
                    Console.WriteLine();
                }
                // Option 82 info
                var relayInfoN = dhcpRequest.GetRelayInfo();
                if (relayInfoN != null)
                {
                    var relayInfo = (RelayInfo)relayInfoN;
                    if (relayInfo.AgentCircuitID != null)
                        Console.WriteLine("Relay agent circuit ID: " + ByteArrayToString(relayInfo.AgentCircuitID));
                    if (relayInfo.AgentRemoteID != null)
                        Console.WriteLine("Relay agent remote ID: " + ByteArrayToString(relayInfo.AgentRemoteID));
                }
                Console.WriteLine();

                var replyOptions = new DHCPReplyOptions();
                // Options should be filled with valid data. Only requested options will be sent.
                replyOptions.SubnetMask = IPAddress.Parse("255.255.255.0");
                replyOptions.DomainName = "SharpDHCPServer";
                replyOptions.ServerIdentifier = IPAddress.Parse("10.0.0.1");
                replyOptions.RouterIP = IPAddress.Parse("10.0.0.1");
                replyOptions.DomainNameServers = new IPAddress[]
                {IPAddress.Parse("192.168.100.2"), IPAddress.Parse("192.168.100.3")};
                // Some static routes
                replyOptions.StaticRoutes = new NetworkRoute[]
                {
                    new NetworkRoute(IPAddress.Parse("10.0.0.0"), IPAddress.Parse("255.0.0.0"),
                        IPAddress.Parse("10.0.0.1")),
                    new NetworkRoute(IPAddress.Parse("192.168.0.0"), IPAddress.Parse("255.255.0.0"),
                        IPAddress.Parse("10.0.0.1")),
                    new NetworkRoute(IPAddress.Parse("172.16.0.0"), IPAddress.Parse("255.240.0.0"),
                        IPAddress.Parse("10.0.0.1")),
                    new NetworkRoute(IPAddress.Parse("80.252.130.248"), IPAddress.Parse("255.255.255.248"),
                        IPAddress.Parse("10.0.0.1")),
                    new NetworkRoute(IPAddress.Parse("80.252.128.88"), IPAddress.Parse("255.255.255.248"),
                        IPAddress.Parse("10.0.0.1")),
                };

                // Lets send reply to client!
                if (type == DHCPMsgType.DHCPDISCOVER)
                    dhcpRequest.SendDHCPReply(DHCPMsgType.DHCPOFFER, ip, replyOptions);
                if (type == DHCPMsgType.DHCPREQUEST)
                    dhcpRequest.SendDHCPReply(DHCPMsgType.DHCPACK, ip, replyOptions);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
            }
        }

        static string ByteArrayToString(byte[] ar)
        {
            var res = new StringBuilder();
            foreach (var b in ar)
            {
                res.Append(b.ToString("X2"));
            }
            res.Append(" (");
            foreach (var b in ar)
            {
                if ((b >= 32) && (b < 127))
                    res.Append(Encoding.ASCII.GetString(new byte[] { b }));
                else res.Append(" ");
            }
            res.Append(")");
            return res.ToString();
        }

        private void StartButton_Click(object sender, EventArgs e)
        {
            server = new DHCPServer();
            server.ServerName = "SharpDHCPServer";
            server.OnDataReceived += Request;
            server.BroadcastAddress = IPAddress.Broadcast;
            server.SendDhcpAnswerNetworkInterface = null;
            server.Start();
            Console.WriteLine("DHCPPort server Running");
        }

        private void StopButton_Click(object sender, EventArgs e)
        {
            server.Dispose();
            Console.WriteLine("DHCPPort server stopped");
        }

        private void ClientDropBox_SelectedIndexChanged(object sender, EventArgs e)
        {

        }

        private void RefreshButton_Click(object sender, EventArgs e)
        {
            ClientDropBox.Items.Clear();
            for (int i = 0; i < leases.Count; i++)
            {
                ClientDropBox.Items.Add(leases.ElementAt(i).Value);
            }
        }

        private void ConnectButton_Click(object sender, EventArgs e)
        {
            var ipaddress = ClientDropBox.SelectedItem.ToString();
            tcpclient = new TCPClient(IPAddress.Parse(ipaddress));
        }

        private void Disconnect_Click(object sender, EventArgs e)
        {
            if (tcpclient != null)
            {
                tcpclient.Disconnect();
                Console.WriteLine("Socket Disconnected");
            }
            else
            {
                Console.WriteLine("Socket doesnt exist");
            }
        }

        private void sendButton_Click(object sender, EventArgs e)
        {
            if (tcpclient != null)
            {
                //tcpclient.SendMessage();
                tcpclient.SendMessage();
                Console.WriteLine("Message Sent");
            }
            else
            {
                Console.WriteLine("Socket doesnt exist");
            }
        }

        private void tabPage1_Click(object sender, EventArgs e)
        {

        }
    }
}
