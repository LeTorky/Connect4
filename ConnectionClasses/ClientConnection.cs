using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net.Sockets;
using System.Net;
using System.Threading;
using System.IO;
using System.Drawing;

namespace ConnectionClasses
{
    #region Enums
    /*--------------------------------------Start of Status Enum------------------------------------------*/
    public enum GameStatus { started, waiting };
    /*----------------------------------------End of Status Enum------------------------------------------*/


    /*--------------------------------------Start of Status Enum------------------------------------------*/
    public enum LobbyRole { PlayerOne, PlayerTwo, Audience, LobbyClient };
    /*----------------------------------------End of Status Enum------------------------------------------*/


    /*--------------------------------------Start of Status Enum------------------------------------------*/
    public enum TokenColor { Orange, Green };
    /*----------------------------------------End of Status Enum------------------------------------------*/
    //ENUMS END
    #endregion

    #region Client Class
    /*---------------------------------------------Client Class-------------------------------------------*/
    public class ClientConnection
    {
        #region Fields
        public Socket ClientSocket { get; }
        public NetworkStream ClientStream { get; }
        public int PlayerCount { get; set; }
        public LobbyRole LobbyClientRole { get; set; }
        public GameStatus Status { get; set; }
        public string ClientName { get; set; }
        public Color TokenColor { get; set; }
        public string BoardSize { get; set; }
        #endregion

        #region Constructor
        public ClientConnection(Socket SetSocket)
        {
            ClientSocket = SetSocket;
            ClientStream = new NetworkStream(ClientSocket);
            PlayerCount = 0;
            Status = GameStatus.waiting;
            ClientName = "";
            LobbyClientRole = LobbyRole.Audience;
        }
        #endregion
    }
    /*---------------------------------------End of Client Class------------------------------------------*/
    #endregion

    #region Lobby Client Class
    /*--------------------------------------Lobby Client Identity-----------------------------------------*/
    public class LobbyClient
    {
        #region Client Properties
        public TcpClient HostConnection { get; set; }
        public NetworkStream HostStream { get; set; }
        public LobbyRole LobbyClientRole { get; set; }
        public string LobbyClientName { get; }
        public Thread ConnectingThread { get; set; }
        public Color TokenColor { get; set; }
        #endregion

        #region Host Properties
        public TcpListener HostingConnection { get; set; }
        public List<ClientConnection> HostClientConnections { get; set; }
        public Thread HostingThread { get; set; }
        public Thread WriteToServerThread { get; set; }
        public Thread WriteToClients { get; set; }
        public GameStatus Status { get; set; }
        public IPAddress HostIP { get; set; }
        public int HostPort { get; set; }
        public string BoardSize { get; set; }
        #endregion

        #region Lobby Client Constructor
        public LobbyClient(IPAddress SetIP, int SetPort, string SetClientName)
        {
            LobbyClientName = SetClientName;
            HostConnection = new TcpClient();
            ConnectingThread = new Thread(ConnectToServer);
            ConnectingThread.Start(new object[] { SetIP, SetPort });
            Status = GameStatus.waiting;
        }
        #endregion

        #region Methods

        #region Connect To Server
        public void ConnectToServer(object SocketInfo)
        {
            LobbyClientRole = LobbyRole.LobbyClient;
            object[] SocketInfoArray = (object[])SocketInfo;
            HostConnection.Connect((IPAddress)SocketInfoArray[0], (int)SocketInfoArray[1]);
            HostStream = HostConnection.GetStream();
        }
        #endregion

        #region Host a Room
        public void HostRoom(Color SetColor, string SetSize)
        {
            HostClientConnections = new List<ClientConnection>();
            WriteToServerThread = new Thread(UpdateRoom);
            WriteToClients = new Thread(WriteRoomInfo);
            HostingThread = new Thread(ListenToConnections);
            HostIP = Dns.GetHostByName(Dns.GetHostName()).AddressList[0];
            HostPort = 6500; //Change Port For Host Connection
            HostingConnection = new TcpListener(HostIP, HostPort);
            HostingConnection.Start();
            TokenColor = SetColor;
            BoardSize = SetSize;
            LobbyClientRole = LobbyRole.PlayerOne;
            byte [] DecodedHost = Encoding.ASCII.GetBytes("Host:"+LobbyClientName+";"+TokenColor.ToString().Split(new string[] { "[", "]", "Color", " "}, StringSplitOptions.RemoveEmptyEntries)[0]+";"+BoardSize+";");
            HostStream.Write(DecodedHost, 0, DecodedHost.Length);
            HostingThread.Start();
            WriteToServerThread.Start();
            WriteToClients.Start();
        }
        #endregion

        #region Update Room List
        public void UpdateRoom()
        {
            Stack<int> RemoveSockets = new Stack<int>();
            int RemoveStackCount;
            while (true)
            {
                RemoveStackCount = 0;
                RemoveSockets.Clear();
                lock (HostClientConnections)lock(HostConnection)
                {
                    foreach(ClientConnection SpecificConnection in HostClientConnections)
                    {
                        if(SpecificConnection.ClientSocket.Poll(1, SelectMode.SelectRead) && (SpecificConnection.ClientSocket.Available == 0))
                        {
                            RemoveSockets.Push(HostClientConnections.IndexOf(SpecificConnection));
                        }
                    }
                    foreach (int i in RemoveSockets)
                    {
                        HostClientConnections[i].ClientStream.Close();
                        HostClientConnections[i].ClientSocket.Close();
                        HostClientConnections.RemoveAt(i);
                    }
                    byte[] DecodedUpdate = Encoding.ASCII.GetBytes($"Info:{HostClientConnections.Count+1},{Status}");
                    HostStream.Write(DecodedUpdate, 0, DecodedUpdate.Length);
                }
                Thread.Sleep(1000);
            }
        }
        #endregion

        #region Listen for new Connections
        public void ListenToConnections()
        {
            Socket InsertSocket;
            while (true)
            {
                InsertSocket = HostingConnection.AcceptSocket();
                lock (HostClientConnections)
                {
                    HostClientConnections.Add(new ClientConnection(InsertSocket));
                    ThreadPool.QueueUserWorkItem(ReadClientName, HostClientConnections.Last<ClientConnection>());
                }
            }
        }
        #endregion

        #region Read Name From Client
        private void ReadClientName(object Client)
        {
            lock (HostClientConnections)
            {
                ClientConnection ClientConn = (ClientConnection)Client;
                byte[] EncodedName = new byte[256];
                ClientConn.ClientStream.Read(EncodedName, 0, EncodedName.Length);
                ClientConn.ClientStream.Flush();
                ClientConn.ClientName = Encoding.ASCII.GetString(EncodedName).Trim((char)0);
            }
        }
        #endregion

        #region Write Name List To Clients

        private void WriteRoomInfo()
        {       
            string NameList;
            while (true)
            {
                lock (HostClientConnections)
                {
                    NameList = "Names:"+this.LobbyClientName;
                    foreach(ClientConnection SpecificConnection in HostClientConnections)
                    {
                        NameList += ("," + SpecificConnection.ClientName);
                    }
                    foreach (ClientConnection SpecificConnection in HostClientConnections)
                    {
                        string[] RemoveList = NameList.Split(new string[] { "," + SpecificConnection.ClientName }, StringSplitOptions.RemoveEmptyEntries);
                        NameList = (RemoveList.Length == 2) ? RemoveList[0] + RemoveList[1] : RemoveList[0];
                        byte[] EncodedNameList = Encoding.ASCII.GetBytes(NameList);
                        SpecificConnection.ClientStream.Write(EncodedNameList, 0, EncodedNameList.Length);
                        NameList += ("," + SpecificConnection.ClientName);
                    }
                }
                Thread.Sleep(1000);
            }
        }

        #endregion

        #endregion

    }
    /*---------------------------------End of Lobby Client Identity----------------------------------------*/
    #endregion                                       

}