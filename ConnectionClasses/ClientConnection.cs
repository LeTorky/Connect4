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
        public TcpClient ClientToHostConnection { get; set; }
        public NetworkStream ClientToHostStream { get; set; }
        public Thread ConnectingToHostOrServerThread { get; set; }
        public TcpListener PlayerTwoToPlayerOneConnection { get; set; }
        public Socket PlayerOneRequestSocket { get; set; }
        public NetworkStream PlayerTwoToPlayerOneStream { get; set; }
        public LobbyRole GameClientRole { get; set; }
        public string GameClientName { get; }
        public Color TokenColor { get; set; }
        public List<string> LobbyNameList { get; set; }
        public List<string> LobbyMoveList { get; set; }
        #endregion

        #region Host Properties
        public TcpListener HostToClientConnection { get; set; }
        public List<ClientConnection> ConnectedClientsList { get; set; }
        public TcpClient PlayerOneToPlayerTwoConnection { get; set; }
        public NetworkStream PlayerOneToPlayerTwoStream { get; set; }
        public EndPoint PlayerTwoEndPoint { get; set; }
        public string PlayerTwoName { get; set; }
        public Thread HostingRoomThread { get; set; }
        public Thread WriteToServerThread { get; set; }
        public Thread WriteToClientsThread { get; set; }
        public GameStatus CurrentStatus { get; set; }
        public IPAddress HostIP { get; set; }
        public int HostPort { get; set; }
        public string BoardSize { get; set; }
        #endregion

        #region Lobby Client Constructor
        public LobbyClient(IPAddress SetIP, int SetPort, string SetClientName)
        {
            GameClientName = SetClientName; //Set Client Name Upon Connecting Game.
            ClientToHostConnection = new TcpClient(); //Starts a TcpClient to Server.
            ConnectingToHostOrServerThread = new Thread(ConnectToServer); //Creates Listening to Server Thread.
            ConnectingToHostOrServerThread.Start(new object[] { SetIP, SetPort }); //Starts Listening to Server Thread.
            CurrentStatus = GameStatus.waiting; //Sets Current Game Status to Waiting.
            LobbyMoveList = new List<string>(); //Creats a List to Hold Game Moves Into.
            LobbyNameList = new List<string>(); //Creats a List To Hold Player Names in Lobby Into.
        }
        #endregion

        #region Methods

        #region Connect To Server
        public void ConnectToServer(object SocketInfo) //Connects to Main Server (Called Upon Construction).
        {
            GameClientRole = LobbyRole.LobbyClient; //Inits Lobby as a Lobby Client Before Joining or Hosting a Room.
            object[] SocketInfoArray = (object[])SocketInfo; //Casts SocketInfo Arg as an Object Array (Contains IP and Socket).
            ClientToHostConnection.Connect((IPAddress)SocketInfoArray[0], (int)SocketInfoArray[1]); //Starts Connecting to Server.
            ClientToHostStream = ClientToHostConnection.GetStream(); //Gets Servers Stream.
        }
        #endregion

        #region Host a Room
        public void HostRoom(Color SetColor, string SetSize) //Starts Hosting a Room (Takes Chosen Color and Size).
        {
            TokenColor = SetColor; //Sets Token Color for Host.
            BoardSize = SetSize; //Sets Board Size of the Game.
            GameClientRole = LobbyRole.PlayerOne; //Sets Role to Player One upon Hosting.
            HostIP = Dns.GetHostByName(Dns.GetHostName()).AddressList[0]; //Hosting Machine's IP to Host with.
            HostPort = 6500; //Port Number to Host With (Change Upon preference or free Port).
            HostToClientConnection = new TcpListener(HostIP, HostPort); //Creats a TCP Listiner for Hosting Connection.
            ConnectedClientsList = new List<ClientConnection>(); //Creates a List of Clients that will Connect onto the Host.
            HostingRoomThread = new Thread(ListenToConnections); //Thread to start Listening for new Connections. (Not PoolThread to manually Abort).
            WriteToServerThread = new Thread(UpdateRoom); //Thread to Update Server with Information. (Not PoolThread to manually Abort).
            WriteToClientsThread = new Thread(WriteRoomInfo); //Thread to Update Clients with Room Information. (Not PoolThread to manually Abort). 
            byte [] EncodedHost = Encoding.ASCII //Prepares Hosting Message to Server to inform other players of this Room.
                .GetBytes("Host:"+GameClientName+";"+TokenColor.ToString()
                .Split(new string[] { "[", "]", "Color", " "}, StringSplitOptions.RemoveEmptyEntries)[0]+";"+BoardSize+";"); 
            HostToClientConnection.Start(); //Starts Host Connection.
            ClientToHostStream.Write(EncodedHost, 0, EncodedHost.Length); //Writes to server the Hosting Message.
            HostingRoomThread.Start(); //Starts listening for new Connection Thread.
            WriteToServerThread.Start(); //Starts updating Server with new room Information Thread.
            WriteToClientsThread.Start(); //Starts updating Clients with latest Room Information.
        }
        #endregion

        #region End Host
        public void EndHost() //End Hosting a Room.
        {
            byte [] EncodedEndHost = Encoding.ASCII.GetBytes("EndHost");
            lock (ConnectedClientsList) //Lock Host Client Connection to Prevent Manipulation of List (Adding)
            {
                foreach(ClientConnection SpecificConnection in ConnectedClientsList) //For Loops on Each Client 
                {
                    SpecificConnection.ClientStream.Close(); //Closes Client Stream.
                    SpecificConnection.ClientSocket.Close(); //Closes Client Connection.
                }
            }
            WriteToClientsThread.Abort(); //Aborts updating Clients with latest Room Information.
            WriteToServerThread.Abort(); //Aborts updating Server with new room Information Thread.
            ClientToHostStream.Write(EncodedEndHost, 0, EncodedEndHost.Length); //Writes to server End Hosting Message.
            HostToClientConnection.Stop(); //Stops Hosting Connection TCP Server.
        }
        #endregion

        #region Update Room List
        public void UpdateRoom() //Updates Server with Room Info.
        {
            Stack<int> RemoveSockets = new Stack<int>(); //A Stack to hold disconnected room index.
            int RemoveStackCount; //Creating a counter to loop on.
            while (true) //Maintain Thread Functionality.
            {
                RemoveSockets.Clear(); //Clear Stack with each Iteration.
                lock (ConnectedClientsList)lock(ClientToHostConnection) //Locks Client List and Connection with Server to prevent Updating Server with Old Information.
                {
                    foreach(ClientConnection SpecificConnection in ConnectedClientsList) //Looping on Each Client to Update Client Conneciton List.
                    {
                        if(SpecificConnection.ClientSocket.Poll(1, SelectMode.SelectRead) && (SpecificConnection.ClientSocket.Available == 0)) //If Socket is Disconnected.
                        {
                            RemoveSockets.Push(ConnectedClientsList.IndexOf(SpecificConnection)); //Add disconnected Socket to Stack.
                        }
                    }
                    foreach (int i in RemoveSockets) //For loop on each entry in the Stack to Disconnect from it and Remove it from the Client List.
                    {
                        ConnectedClientsList[i].ClientStream.Close(); //Closing disconnected Client Stream.
                        ConnectedClientsList[i].ClientSocket.Close(); //Closing disconnected Client Socket Connection.
                        ConnectedClientsList.RemoveAt(i); //Remove disconnected Client from Client List.
                    }
                    byte[] DecodedUpdate = Encoding.ASCII.GetBytes($"Info:{ConnectedClientsList.Count+1},{CurrentStatus}"); //Prepares Room Count Information and Game Status.
                    ClientToHostStream.Write(DecodedUpdate, 0, DecodedUpdate.Length); //Writes to Server new Information.
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
                    InsertSocket = HostToClientConnection.AcceptSocket(); //Initializing the Socket.
                    lock (ConnectedClientsList) //Locks Client Connection List to Prevent Manipulation While adding new Connection.
                    {
                        ConnectedClientsList.Add(new ClientConnection(InsertSocket)); //Adding New Connection to the List.
                        ThreadPool.QueueUserWorkItem(ReadClientName, ConnectedClientsList.Last<ClientConnection>()); //Creating a Read Thread to Obtain Client Name.
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
            lock (ConnectedClientsList) //Locks Client Connection List In order to edit Clients Name
            {
                ClientConnection ClientConn = (ClientConnection)Client; //Converts Client Arg into a ClientConnection Object.
                byte[] EncodedName = new byte[256]; //Encoded Name Buffer.
                ClientConn.ClientStream.Read(EncodedName, 0, EncodedName.Length); //Read Into the Name Buffer.
                ClientConn.ClientStream.Flush(); //Flush Clients Stream for Later Use.
                ClientConn.ClientName = Encoding.ASCII.GetString(EncodedName).Trim((char)0); //Gets String From Buffer and Changes Client Name.
            }
        }
        #endregion

        #region Write Room Information To Connected Clients

        private void WriteRoomInfo() //Write Room Information to Client Connection List.
        {       
            string NameString; //Client Names in Room Information String to be Sent.
            string MoveString; //Move List Information String to be Sent.
            string PlayRequestString; //Play Request String Information to be Sent.
            while (true) // Maintains Functionality of Thread.
            {
                lock (ConnectedClientsList) lock(LobbyMoveList) //Locks Client Connection List to prevent Manipulation or Addition While Sending Information.
                {
                    NameString = this.GameClientName; //Prepares Names to Notify Client that this is a Room Information String.
                    MoveString = ""; //Prepares Moves notification in a string to start appending moves into it.
                    if (PlayerTwoEndPoint != null)
                    {
                        lock (PlayerTwoEndPoint)
                        {
                            PlayRequestString = PlayerTwoEndPoint.ToString(); //Gets PlayerTwo IP Address to Request them to play.
                        }
                    }
                    else
                    {
                        PlayRequestString = "None"; //Player Request will be Null aswell since we Havent requested them to play.
                        PlayerTwoName = "Waiting..."; //Player Two is null which means Player Two name will be Waiting... for all Clients.
                    }
                    foreach(ClientConnection SpecificConnection in ConnectedClientsList) //For Loops Each Connection in the List to Get Names.
                    {
                        NameString += ("," + SpecificConnection.ClientName); //Concats Each Client Name to the Name List.
                    }
                    foreach(string Move in LobbyMoveList)//For Loops on Each Move String in the MoveList
                    {
                        MoveString += Move + "*"; //Concats Each Move into the MoveString.
                    }
                    foreach (ClientConnection SpecificConnection in ConnectedClientsList) //For Loops on All Client Connections Again to Send List.
                    {
                        string[] RemovedNameList = NameString.Split(new string[] { "," + SpecificConnection.ClientName }, //Splits String by Current Client Name into an Array of Strings
                            StringSplitOptions.RemoveEmptyEntries);// In Order to remove the Client Name of the Specific Connection so that He doesnt Receive his Name.
                        string NewNameList = (RemovedNameList.Length == 2) ? RemovedNameList[0] + RemovedNameList[1] : RemovedNameList[0]; //If Client Name was in the Middle then Concat it else then Array has One Element.
                        NewNameList += $";{CurrentStatus.ToString()};{PlayerTwoName};{PlayRequestString};{MoveString};#"; //Concats All Room Information with the New Name List.
                        byte[] EncodedNameList = Encoding.ASCII.GetBytes(NewNameList); // Get Encoded String Bytes
                        if(!SpecificConnection.ClientSocket.Poll(1, SelectMode.SelectRead) || (SpecificConnection.ClientSocket.Available > 0)) // If Socket is Connected.
                        {
                            SpecificConnection.ClientStream.Write(EncodedNameList, 0, EncodedNameList.Length); //Write to Client Room Information.
                        }
                    }
                }
                Thread.Sleep(50);// Thread Waits 0.05 second to repeat Write.
            }
        }

        #endregion

        #endregion

    }
    /*---------------------------------End of Lobby Client Identity----------------------------------------*/
    #endregion                                       

}