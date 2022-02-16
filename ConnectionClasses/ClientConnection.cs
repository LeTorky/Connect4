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
        public void ConnectToServer(object SocketInfo) //Connects to Main Server (Called Upon Construction).
        {
            LobbyClientRole = LobbyRole.LobbyClient; //Inits Lobby as a Lobby Client Before Joining or Hosting a Room.
            object[] SocketInfoArray = (object[])SocketInfo; //Casts SocketInfo Arg as an Object Array (Contains IP and Socket).
            HostConnection.Connect((IPAddress)SocketInfoArray[0], (int)SocketInfoArray[1]); //Starts Connecting to Server.
            HostStream = HostConnection.GetStream(); //Gets Servers Stream.
        }
        #endregion

        #region Host a Room
        public void HostRoom(Color SetColor, string SetSize) //Starts Hosting a Room (Takes Chosen Color and Size).
        {
            TokenColor = SetColor; //Sets Token Color for Host.
            BoardSize = SetSize; //Sets Board Size of the Game.
            LobbyClientRole = LobbyRole.PlayerOne; //Sets Role to Player One upon Hosting.
            HostIP = Dns.GetHostByName(Dns.GetHostName()).AddressList[0]; //Hosting Machine's IP to Host with.
            HostPort = 6500; //Port Number to Host With (Change Upon preference or free Port).
            HostingConnection = new TcpListener(HostIP, HostPort); //Creats a TCP Listiner for Hosting Connection.
            HostClientConnections = new List<ClientConnection>(); //Creates a List of Clients that will Connect onto the Host.
            HostingThread = new Thread(ListenToConnections); //Thread to start Listening for new Connections. (Not PoolThread to manually Abort).
            WriteToServerThread = new Thread(UpdateRoom); //Thread to Update Server with Information. (Not PoolThread to manually Abort).
            WriteToClients = new Thread(WriteRoomInfo); //Thread to Update Clients with Room Information. (Not PoolThread to manually Abort). 
            HostingConnection.Start(); //Starts Host Connection.
            byte [] DecodedHost = Encoding.ASCII //Prepares Hosting Message to Server to inform other players of this Room.
                .GetBytes("Host:"+LobbyClientName+";"+TokenColor.ToString()
                .Split(new string[] { "[", "]", "Color", " "}, StringSplitOptions.RemoveEmptyEntries)[0]+";"+BoardSize+";"); 
            HostStream.Write(DecodedHost, 0, DecodedHost.Length); //Writes to server the Hosting Message.
            HostingThread.Start(); //Starts listening for new Connection Thread.
            WriteToServerThread.Start(); //Starts updating Server with new room Information Thread.
            WriteToClients.Start(); //Starts updating Clients with latest Room Information.
        }
        #endregion

        #region Update Room List
        public void UpdateRoom()
        {
            Stack<int> RemoveSockets = new Stack<int>(); //A Stack to hold disconnected room index.
            int RemoveStackCount; //Creating a counter to loop on.
            while (true) //Maintain Thread Functionality.
            {
                RemoveStackCount = 0; //Initializing Counter with Zero
                RemoveSockets.Clear(); //Clear Stack with each Iteration.
                lock (HostClientConnections)lock(HostConnection) //Locks Client List and Connection with Server to prevent Updating Server with Old Information.
                {
                    foreach(ClientConnection SpecificConnection in HostClientConnections) //Looping on Each Client to Update it with Rooms Available.
                    {
                        if(SpecificConnection.ClientSocket.Poll(1, SelectMode.SelectRead) && (SpecificConnection.ClientSocket.Available == 0)) //If Socket is Disconnected.
                        {
                            RemoveSockets.Push(HostClientConnections.IndexOf(SpecificConnection)); //Add disconnected Socket to Stack.
                        }
                    }
                    foreach (int i in RemoveSockets) //For loop on each entry in the Stack to Disconnect from it and Remove it from the Client List.
                    {
                        HostClientConnections[i].ClientStream.Close(); //Closing disconnected Client Stream.
                        HostClientConnections[i].ClientSocket.Close(); //Closing disconnected Client Socket Connection.
                        HostClientConnections.RemoveAt(i); //Remove disconnected Client from Client List.
                    }
                    byte[] DecodedUpdate = Encoding.ASCII.GetBytes($"Info:{HostClientConnections.Count+1},{Status}"); //Prepares Room Count Information and Game Status.
                    HostStream.Write(DecodedUpdate, 0, DecodedUpdate.Length); //Writes to Server new Information.
                }
                Thread.Sleep(1000); //Waits one second to restart Operation.
            }
        }
        #endregion

        #region Listen for new Connections
        public void ListenToConnections() //Listen for new Client Connections.
        {
            Socket InsertSocket; //Socket Received Upon Accepting Connection.
            while (true) //Maintains Funcitonality 
            {
                try
                {
                    InsertSocket = HostingConnection.AcceptSocket(); //Initializing the Socket.
                    lock (HostClientConnections) //Locks Client Connection List to Prevent Manipulation While adding new Connection.
                    {
                        HostClientConnections.Add(new ClientConnection(InsertSocket)); //Adding New Connection to the List.
                        ThreadPool.QueueUserWorkItem(ReadClientName, HostClientConnections.Last<ClientConnection>()); //Creating a Read Thread to Obtain Client Name.
                    }
                }
                catch (Exception Obj)
                {
                    Thread.CurrentThread.Abort(); //On Disconnection of Host, Automatic Termination of Thread.
                }
            }
        }
        #endregion

        #region Read Name From Client
        private void ReadClientName(object Client) //Read Client Name Upon Connection to Host.
        {
            lock (HostClientConnections) //Locks Client Connection List In order to edit Clients Name
            {
                ClientConnection ClientConn = (ClientConnection)Client; //Converts Client Arg into a ClientConnection Object.
                byte[] EncodedName = new byte[256]; //Encoded Name Buffer.
                ClientConn.ClientStream.Read(EncodedName, 0, EncodedName.Length); //Read Into the Name Buffer.
                ClientConn.ClientStream.Flush(); //Flush Clients Stream for Later Use.
                ClientConn.ClientName = Encoding.ASCII.GetString(EncodedName).Trim((char)0); //Gets String From Buffer and Changes Client Name.
            }
        }
        #endregion

        #region Write Name List To Clients

        private void WriteRoomInfo() //Write Room Information to Client Connection List.
        {       
            string NameList; //Room Information String to be Sent.
            while (true) // Maintains Functionality of Thread.
            {
                lock (HostClientConnections) //Locks Client Connection List to prevent Manipulation or Addition While Sending Information.
                {
                    NameList = "Names:"+this.LobbyClientName; //Concats Names: Token to Notify Client that this is a Room Information String.
                    foreach(ClientConnection SpecificConnection in HostClientConnections) //For Loops Each Connection in the List to Get Names.
                    {
                        NameList += ("," + SpecificConnection.ClientName); //Concats Each Client Name to the Name List.
                    }
                    foreach (ClientConnection SpecificConnection in HostClientConnections) //For Loops on All Client Connections Again to Send List.
                    {
                        string[] RemoveList = NameList.Split(new string[] { "," + SpecificConnection.ClientName }, //Splits String into an Array of Strings
                            StringSplitOptions.RemoveEmptyEntries);// In Order to remove the Client Name of the Specific Connection so that He doesnt Receive his Name.
                        NameList = (RemoveList.Length == 2) ? RemoveList[0] + RemoveList[1] : RemoveList[0]; //If Client Name was in the Middle then Concat it else then Array has One Element.
                        byte[] EncodedNameList = Encoding.ASCII.GetBytes(NameList); // Get Encoded String Bytes
                        if(!SpecificConnection.ClientSocket.Poll(1, SelectMode.SelectRead) || (SpecificConnection.ClientSocket.Available > 0)) // If Socket is Connected.
                        {
                            SpecificConnection.ClientStream.Write(EncodedNameList, 0, EncodedNameList.Length); //Write to Client Name List.
                            NameList += ("," + SpecificConnection.ClientName);//Add to the Name List the Client Name we've removed.
                        }
                    }
                }
                Thread.Sleep(1000);// Thread Waits 1 second to repeat Write.
            }
        }

        #endregion

        #endregion

    }
    /*---------------------------------End of Lobby Client Identity----------------------------------------*/
    #endregion                                       

}