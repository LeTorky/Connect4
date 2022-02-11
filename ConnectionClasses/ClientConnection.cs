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
        public GameStatus Status { get; set; }
        public string ClientName { get; set; }
        #endregion

        #region Constructor
        public ClientConnection(Socket SetSocket)
        {
            ClientSocket = SetSocket;
            ClientStream = new NetworkStream(ClientSocket);
            PlayerCount = 0;
            Status = GameStatus.waiting;
            ClientName = "";
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
            HostingThread = new Thread(ListenToConnections);
            HostIP = Dns.GetHostByName(Dns.GetHostName()).AddressList[0];
            HostPort = 6500; //Change Port For Host Connection
            HostingConnection = new TcpListener(HostIP, HostPort);
            HostingConnection.Start();
            byte [] DecodedHost = Encoding.ASCII.GetBytes("Host: "+LobbyClientName);
            HostStream.Write(DecodedHost, 0, DecodedHost.Length);
            TokenColor = SetColor;
            BoardSize = SetSize;
            WriteToServerThread.Start();
            HostingThread.Start();
        }
        #endregion

        #region Update Room List
        public void UpdateRoom()
        {
            while (true)
            {
                lock (HostClientConnections)lock(HostConnection)
                {
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
                    //ThreadPool.QueueUserWorkItem(ReadHostRequest, HostClientConnections.Last<ClientConnection>());
                }
            }
        }
        #endregion

        #endregion

    }
    /*---------------------------------End of Lobby Client Identity----------------------------------------*/
    #endregion                                       

}
