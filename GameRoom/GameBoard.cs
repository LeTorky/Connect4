using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using ConnectionClasses;
using System.Threading;
using System.Net;
using System.Net.Sockets;

namespace GameRoomSpace
{
    public partial class GameBoard : Form
    {
        #region Fields
        //Connection and Thread Fields
        LobbyClient GameClient; //Game Client Information (Host / Audience / Player Two).
        Thread ReadFromHostThread; //A Thread to Read Requests from Host.

        //Game Logic and GUI Fields
        Notifications Notification; //Notification Form to Display Information to Player.
        Control Lobby; //References the Caller (Lobby) in Order to Show Form Again After Disconnection / Closing.
        ConfirmPlayerTwo ConfirmPlayerTwoDiag; //A Dialong to Confirm Playing or Not with Player One.
        Rectangle[] boardcolumns;//Column Rectangles
        int[,] board; //Board Array 
        int turn; //Player Turn
        int RowNum; //Row Numbers
        int ColNum; //Column Numbers
        SolidBrush Token1_Color; //Token Color One.
        Brush Token2_Color; //Token Color Two.
        Graphics g; //Graphics Object.
        #endregion

        #region Room Constructor
        public GameBoard(LobbyClient SetGameClient, string SetBoardSize, Color SetTokenColor, string SetHostName, Control SetLobby) //Form Constructor.
        {
            InitializeComponent(); //Prepare UI of Game Room.
            InitializeClientRoom(SetGameClient, SetBoardSize, SetTokenColor, SetHostName, SetLobby); //Initializes GameBoard Room and Game Client Properties.
        }
        #endregion

        #region Event Handlers

        #region Select Player Two

        void PlayersListBox_MouseDoubleClick(object sender, MouseEventArgs e) //List Box Double Click Event Handler (Choosing Player Two).
        {
            if (PlayersListBox.SelectedIndex != -1) //If Game Client is Player One and Game Status is Waiting.
            {
                PlayersListBox.MouseDoubleClick -= PlayersListBox_MouseDoubleClick;
                ThreadPool.QueueUserWorkItem(WaitForPlayerTwoResponse, GameClient.ConnectedClientsList[PlayersListBox.SelectedIndex]); //Starts a Thread to Listen to Player Two's Response
            }
        }

        #endregion

        #region Playing Mouse Click
        void GameBoard_MouseClick(object sender, MouseEventArgs e) //Event Handling Mouse Click to Play Game.
        {
            int columnindex = this.columnnumber(e.Location);
            if (columnindex != -1)
            {
                int rowindex = this.Emptyrow(columnindex);
                if (rowindex != -1)//incase the column is not full
                {
                    switch (GameClient.GameClientRole) //Switch Between Player One or Player Two Roles Only.
                    {
                        case LobbyRole.PlayerOne: //Case Player One.
                            if (turn == 1 && GameClient.CurrentStatus == GameStatus.started) //If its player One's Turn and Game Started.
                            {
                                gamelogic(e.Location); //Initiate Game Logic.
                            }
                            else
                            {
                                Notification = new Notifications();
                                Notification.label1.Text = turn == 2 ? "Wait for your turn!" : "Select Player Two!"; //Handles Notification Incase of MisClick.
                                Notification.Show();
                            }
                            break;
                        case LobbyRole.PlayerTwo: //Case Player Two.
                            if (turn == 2 && GameClient.CurrentStatus == GameStatus.started) //If its player Two's Turn and Game Started.
                            {
                                PlayerTwoWriteLocation(e.Location); //Initiate Game Logic.
                            }
                            else
                            {
                                Notification = new Notifications();
                                Notification.label1.Text = turn == 1 ? "Wait for your turn!" : "Game Hasnt Started Yet!"; //Handles Notification Incase of MisClick.
                                Notification.Show();
                            }
                            break;
                    }
                }
            }
        }
        #endregion

        #region Painting Game Board
        void GameBoard_Paint(object sender, PaintEventArgs e)
        {
            e.Graphics.FillRectangle(new SolidBrush(Color.FromArgb(77, 69, 97)), 50, 50, (340 * ColNum) / 7, (300 * RowNum) / 6);//Board size
            for (int i = 0; i < RowNum; i++)
            {
                this.boardcolumns[i] = new Rectangle(65 + 45 * i, 70, 32, (300 * RowNum) / 6);//Board columns
                for (int j = 0; j < ColNum; j++)
                {
                    e.Graphics.FillEllipse(new SolidBrush(Color.FromArgb(94, 87, 132)), 65 + 45 * j, 70 + 45 * i, 32, 32);//Board tokens Location l
                }
            }
        }
        #endregion

        #region Closing Game

        void GameBoard_FormClosing(object sender, FormClosingEventArgs e) //Handles Before Closing Event
        {
            switch (GameClient.GameClientRole) //Switches on Client Roles.
            {
                case LobbyRole.PlayerOne: //If Player One.
                    GameClient.EndHost(); //Calls End Host to End Any Threads Running and Inform Server of Closure.
                    break;
                case LobbyRole.Audience: //If Audience.
                    DisconnectFromPlayerOne();
                    break;
                case LobbyRole.PlayerTwo: //Or Player Two.
                    EndPlayerTwoHost(); //Ends Player Two Host.
                    DisconnectFromPlayerOne(); //Ends Connection with Player One.
                    break;
            }
            Lobby.Invoke((MethodInvoker)delegate { Lobby.Show(); }); //Opens Lobby via MethodInvoker (Cross Thread Control Calls).
        }
        #endregion

        #endregion

        #region Methods

        #region Initializers

        #region Client Initializer
        void InitializeClientRoom(LobbyClient SetGameClient, string SetBoardSize, Color SetTokenColor, string SetHostName, Control SetLobby) //Initialize GameBoard Room according to Client Role Type.
        {
            Lobby = SetLobby; //Sets Lobby Control into Caller.
            GameClient = SetGameClient; //Sets Game Client to Client Passed from Lobby.
            GameClient.BoardSize = SetBoardSize; //Sets Board Size for Client.
            GameClient.TokenColor = SetTokenColor; //Sets Token Color for Client.
            PlayerOneLabel.Text = SetHostName; // Sets Player One Label Text Into Host Name.
            switch (GameClient.GameClientRole) //Switch on Client Role Types.
            {
                case LobbyRole.PlayerOne: //Prepares Host Event Handlers and Threads
                    ThreadPool.QueueUserWorkItem(UpdatePlayerOnePlayerList); //Starts a Thread that updates the player list for player one.
                    break;
                case LobbyRole.Audience: //Prepares Host Event Handlers and Threads
                    ConfirmPlayerTwoDiag = new ConfirmPlayerTwo(); //Creates a Dialog Box for accepting a game request.
                    ReadFromHostThread = new Thread(ReadHostRequest); //Starts a Thread that Reads Updates from the Host.
                    ReadFromHostThread.Start(); //Starts the Updating Thread.
                    PlayersListBox.Enabled = false;
                    break;
            }
            RowNum = int.Parse(SetBoardSize.Split('x')[0]); //Sets the Horizontal Dimension of the Board.
            ColNum = int.Parse(SetBoardSize.Split('x')[1]); //Sets the Vertical Dimension of the Board.
            InitializeGameBoard(); //Prepares the UI of Game Board.
        }
        #endregion

        #region Initialize GameBoard GUI
        void InitializeGameBoard() //Initializes GameBoard Graphic Variables.
        {
            if (GameClient.GameClientRole == LobbyRole.PlayerOne) //If Player One.
            {
                lock (GameClient.ConnectedClientsList) //Locks Client Connections to Prevent Sending Old Information While Setting new Ones.
                {
                    foreach(ClientConnection SpecificConnection in GameClient.ConnectedClientsList) //For Looping over each Client to Flush Client Stream.
                    {
                        SpecificConnection.ClientStream.Flush(); //Flushing Client Stream from Old Information.
                    }
                }
                GameClient.PlayerTwoEndPoint = null; //Sets Player Two Request End Point to Null.
                GameClient.CurrentStatus = GameStatus.waiting; //Sets Game Status to Waiting.
                PlayerTwoLabel.Text = "Waiting..."; //Sets Player Two Label to Waiting.
                Thread.Sleep(50); //Wait for Players to Get Last Move Before Clearing Move List.  (Should get a handshake of information reception instead).
                GameClient.LobbyMoveList.Clear(); //Clears Move List.
                PlayersListBox.MouseDoubleClick += PlayersListBox_MouseDoubleClick; //Adds Event Handler for Choosing Player Two.
            }
            else if(GameClient.GameClientRole == LobbyRole.PlayerTwo) //If Player Two.
            { 
                EndPlayerTwoHost(); //Ends Player Two Hosting Session as Player Two.
                GameClient.GameClientRole = LobbyRole.Audience; //Changes Role to Audience.
            }
            GameClient.LobbyMoveList.Clear(); //Clears Move List
            g = CreateGraphics(); //Create Graphic Object to Draw With.
            Token1_Color = new SolidBrush(GameClient.TokenColor); //Initializes Token Color for Player One
            if(Token1_Color.Color == Color.LightSeaGreen)
            {
                Token2_Color = new SolidBrush(Color.FromArgb(252, 175, 23));
            }
            else
            {
                Token2_Color = Brushes.LightSeaGreen;
            }
            boardcolumns = new Rectangle[ColNum]; //Initializes Board Columns.
            board = new int[RowNum, ColNum]; //Initializes Board Array.
            turn = 1; //Initializes Turn with Player One.
            PlayerOneLabel.ForeColor = Color.White; //Lights Player One Label
            PlayerTwoLabel.ForeColor = Color.Gray; //Dims Player Two Label
            Invalidate(); //RePaint.
        }
        #endregion

        #endregion

        #region Updating Player List

        #region Check And Update Player List
        void CheckAndUpdatePlayerList(string InformationArray)
        {
            lock (PlayersListBox)
            {
                if (IsHandleCreated) // Check if Handle is Created Before Invoking.
                {
                    //Checking Differences between NameList and RoomNameString.
                    string[] RoomNamesString = InformationArray.Split(','); //Splits Names in RoomNameString.
                    if ((RoomNamesString.Length != GameClient.LobbyNameList.Count)) //If Length of RoomNamesString and Name List are not the same then update Name List and update Players List.
                    {
                        GameClient.LobbyNameList.Clear(); //Clears Name List.
                        foreach (string ClientName in RoomNamesString) //For Loops on Each Client Name from the String.
                        {
                            GameClient.LobbyNameList.Add(ClientName); //Adds New Client Name into Name List.
                        }
                        this.Invoke((MethodInvoker)delegate
                        { //Update Players List.
                            PlayersListBox.Items.Clear(); //Clears ListBox Items from the Player Names inside of it.
                            foreach (string Name in GameClient.LobbyNameList) //For Loops on Each Name in the Name List.
                            {
                                PlayersListBox.Items.Add(Name); //Adds Each name within the Name List into the List Box.
                            }
                        });
                    }
                    else //If Length is the Same, Name List could still be different (One disconnected and someone else connected, Classic Switcharoo :D)
                    {
                        int flag; //Flag to base upon the reason of Loop Break.
                        for (flag = 0; (flag < RoomNamesString.Length) && (RoomNamesString[flag] == GameClient.LobbyNameList[flag]); flag++) ; //For Loops on Both Name List and String.
                        if (flag != RoomNamesString.Length) //If Flag aborted due to un equal values then update Name List and update Players List.
                        {
                            GameClient.LobbyNameList.Clear(); //Clear Name List.
                            foreach (string ClientName in RoomNamesString) //For Loop in Each in Name within the String.
                            {
                                GameClient.LobbyNameList.Add(ClientName); //Import Each Name into the NameList.
                            }
                            this.Invoke((MethodInvoker)delegate
                            { //Update Players List.
                                PlayersListBox.Items.Clear(); //Clears ListBox Items from the Player Names inside of it.
                                foreach (string Name in GameClient.LobbyNameList) //For Loops on Each Name in the Name List .
                                {
                                    PlayersListBox.Items.Add(Name); //Adds Each name within the Name List into the List Box.
                                }
                            });
                        }
                    }
                }
            }
        }
        #endregion

        #region Update Player One Player List
        void UpdatePlayerOnePlayerList(object Obj)
        {
            try
            {
                while (true)
                {
                    Thread.Sleep(100);
                    string LoadedRooms = "";
                    bool EndGameFlag = true;
                    lock (GameClient.ConnectedClientsList)
                    {
                        foreach(ClientConnection SpecificConnection in GameClient.ConnectedClientsList) //For Each Connection in the Client Connected List.
                        {
                            LoadedRooms += SpecificConnection.ClientName + ',';
                            if(SpecificConnection.ClientSocket.RemoteEndPoint == GameClient.PlayerTwoEndPoint)
                            {
                                EndGameFlag = false; //Prevents Disconnection and Game Re Initialization.
                            }
                        }
                        if(LoadedRooms != "")
                        {
                            LoadedRooms = LoadedRooms.Remove(LoadedRooms.Length - 1);
                            CheckAndUpdatePlayerList(LoadedRooms); //Checks Name Differences Between Connection List and Saved Name List.
                        }
                        else
                        {
                            Thread.Sleep(200); //Waits 0.2 seconds before changing list box with names.
                            CheckAndUpdatePlayerList(LoadedRooms); //Checks Name Differences Between Connection List and Saved Name List.
                        }
                        if(GameClient.PlayerTwoEndPoint != null && EndGameFlag)
                        {
                            DisconnectFromPlayerTwo(); //Disconnects From Player Two Host Session.
                            BeginInvoke((MethodInvoker)InitializeGameBoard); //Begins Initializing Game Board from Start.
                        }
                    }
                }
            }
            catch(Exception Exc)
            {
                Thread.CurrentThread.Abort();
            }
        }
        #endregion

        #endregion

        #region Read Host Requests
        void ReadHostRequest()
        {
            byte[] EncodedHostRequest; //Encoded Host Request.
            string DecodedHostRequest; //Decoded Host Request.
            while (true) //Maintains Thread Functionality.
            {
                EncodedHostRequest = new byte[1000]; //Initializing Encoded Host Request.
                DecodedHostRequest = ""; //Initializing Decoded Host Request.
                try //Try Reading From Host
                {
                    GameClient.ClientToHostStream.Read(EncodedHostRequest, 0, EncodedHostRequest.Length); //Read Host Requests
                    GameClient.ClientToHostStream.Flush(); //Flushes Stream Preparing for New Information.
                    DecodedHostRequest = Encoding.ASCII.GetString(EncodedHostRequest).Trim((char)0); //Decodes Encoded Host Request.
                    string[] InformationArray = DecodedHostRequest //Splits Array on last # to get latest info and slices information on ; char.
                        .Split(new string[]{ "#" }, StringSplitOptions.RemoveEmptyEntries).Last().Split(';');
                    Array.Resize(ref InformationArray, InformationArray.Length-1);
                    if(InformationArray.Length == 5)
                    {
                        lock (GameClient.LobbyMoveList) lock (GameClient.LobbyNameList) lock(GameClient.ClientToHostStream) //Locks Move List and Name List to prevent Manipulation While Comparing New Info.
                        {
                            //Working with Each Room Information Element

                            //Checking Differences between NameList and RoomNameString.
                            CheckAndUpdatePlayerList(InformationArray[0]); //Checks Name Differences Between Host String List and Saved Name List.

                            //Checking Differences between Move List String and Current Move List.
                            string[] MoveListString = InformationArray[4].Split(new string[] {"*"}, StringSplitOptions.RemoveEmptyEntries); //Splits Moves in Move List.
                            if (MoveListString.Length != GameClient.LobbyMoveList.Count && GameClient.CurrentStatus == GameStatus.started) //Checks if theres an Extra Move.
                            {
                                int Count = GameClient.LobbyMoveList.Count; //Creats an Iteration Variable that starts with the Last Movement.
                                for (int i = Count; i < MoveListString.Length; i++) //Iterates over the new Moves.
                                {
                                    Point point = new Point(int.Parse(MoveListString[i].Split(',')[0]), //Creates a new point to push into the Move List,
                                    int.Parse(MoveListString[i].Split(',')[1])); //And Start GDI With.
                                    GameClient.LobbyMoveList.Add(MoveListString[i]); //Pushes Point into MoveList Stack.
                                    this.Invoke((MethodInvoker)delegate { gamelogic(point); });//gamelogic(point); //Starts GameLogic.
                                }
                            }

                            //Checking Differences between GameStatus String and Current Game Status.
                            string GameStatusString = InformationArray[1]; //Extracts Game Status From String Array.
                            if(GameClient.CurrentStatus == GameStatus.started && GameStatusString == "waiting") //If Game started and Received a Waiting String.  //HANDLE LATER.
                            {
                                GameClient.CurrentStatus = GameStatus.waiting; //Change Status to Waiting.
                                this.Invoke((MethodInvoker)delegate { InitializeGameBoard(); }); // Resets Game Logic and GUI. (Incase of Reset MidGame).
                            }
                            else if(GameClient.CurrentStatus == GameStatus.waiting && GameStatusString == "started") //Else if Game was Waiting and Received a Started String.
                            {
                                GameClient.CurrentStatus = GameStatus.started; //Switches Game Status to Started.
                            }

                            //Checking Differences between PlayerTwo String and Current PlayerTwo.
                            string PlayerTwoString = InformationArray[2];  //Extracts PlayerTwo Name From String Array.
                            this.Invoke((MethodInvoker)delegate { //Changes Label if there are Differences between Strings.
                                PlayerTwoLabel.Text = PlayerTwoString != PlayerTwoLabel.Text ? PlayerTwoString //Switches if Player Two String is Different than current,
                                : PlayerTwoLabel.Text; //Else Remain the Same.
                            });

                            //Checking for Play Request
                            string PlayerRequestString = InformationArray[3]; //Extracts Play Request From String Array.
                            if((GameClient.GameClientRole != LobbyRole.PlayerTwo) && //Checks if Game Client is not Already PlayerTwo and,
                                        (PlayerRequestString == GameClient.ClientToHostConnection.Client.LocalEndPoint.ToString())) //Request is for this Game Client. 
                            {
                                IPAddress ClientIP = Dns.GetHostByName(Dns.GetHostName()).AddressList[0];
                                int ClientPort = 7500; //Gets Port of PlayerTwo Connection.
                                GameClient.PlayerTwoToPlayerOneConnection = new TcpListener(ClientIP, ClientPort); //Create a Connection to Start Listening to Player Two Response and Plays.
                                GameClient.PlayerTwoToPlayerOneConnection.Server.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, 1);
                                GameClient.PlayerTwoToPlayerOneConnection.Start(); //Starts Connection.
                                GameClient.PlayerOneRequestSocket = GameClient.PlayerTwoToPlayerOneConnection.AcceptSocket();
                                GameClient.PlayerTwoToPlayerOneStream = new NetworkStream(GameClient.PlayerOneRequestSocket);
                                DialogResult Answer = ConfirmPlayerTwoDiag.ShowDialog(); //Shows a Prompt Dialog to ask if Client Wants to Play.
                                byte[] DecodedAnswer; //Decoded Answer.
                                if (Answer == DialogResult.Yes) //If Answer is Yes.
                                {
                                    DecodedAnswer = Encoding.ASCII.GetBytes("Yes"); //Initialize Decoded Answer with Yes.
                                    GameClient.PlayerTwoToPlayerOneStream.Write(DecodedAnswer, 0, DecodedAnswer.Length); //Write Decoded Answer to Host.
                                    GameClient.GameClientRole = LobbyRole.PlayerTwo; //And Turn Game Client into Player Two.
                                }
                                else //If Answer is No.
                                {
                                    DecodedAnswer = Encoding.ASCII.GetBytes("No"); //Initalize Decoed Answer with No.
                                    GameClient.PlayerTwoToPlayerOneStream.Write(DecodedAnswer, 0, DecodedAnswer.Length); //Write Decoded Answer to Host.
                                    EndPlayerTwoHost(); //Ends Hosting Player Two Session.
                                }
                            }
                        }
                    }
                    else
                    {
                        GameClient.ClientToHostStream.Flush();
                    }
                }
                catch (System.IO.IOException Exc) //If Exception Occurs, Then this Means Host Disconnected and We Cant Read From Them.
                {
                    //Shows Notification.
                    try
                    {
                        this.Invoke((MethodInvoker)delegate { Notification = new Notifications(); Notification.label1.Text = "Disconnected!"; Notification.ShowDialog(); this.Close(); }); //Closes GameRoom via MethodInvoker (Cross Thread Control Calls).
                    }
                    catch(InvalidOperationException Exc2)
                    {
                        this.Invoke((MethodInvoker)delegate { this.Close(); });
                        //No need to Act, just to prevent Game from Crashing from Calling a Form via a Disposed Object.
                    }
                    Lobby.Invoke((MethodInvoker)delegate { Lobby.Show(); }); //Opens Lobby via MethodInvoker (Cross Thread Control Calls).
                }
                catch (InvalidOperationException Exc) //If Host Disconnected and Reading Rooms / Moves Occurs.
                {
                    try
                    {
                        this.Invoke((MethodInvoker)delegate { Notification = new Notifications(); Notification.label1.Text = "Disconnected!"; Notification.ShowDialog(); this.Close(); }); //Closes GameRoom via MethodInvoker (Cross Thread Control Calls).
                    }
                    catch (InvalidOperationException Exc2)
                    {
                        if (this.IsHandleCreated)
                        {
                            this.Invoke((MethodInvoker)delegate { this.Close(); });
                        }
                        //No need to Act, just to prevent Game from Crashing from Calling a Form via a Disposed Object.
                    }
                }
            }
        }
        #endregion

        #region Wait For Player Two Response
        void WaitForPlayerTwoResponse(object ClientSocketPassed) //Waits for Player Two Response
        {
            ClientConnection ClientSocket = (ClientConnection)ClientSocketPassed; //Casts incoming object from ThreadPool Call into a Client Connection Object.
            byte[] EncodedResponse = new byte[256]; //Encoded Player Two Response.
            string DecodedResponse = ""; //Decoded Player Two Response.
            string ClientSocketString = ClientSocket.ClientSocket.RemoteEndPoint.ToString(); //ClientSocket Changed to String.
            GameClient.PlayerTwoEndPoint = ClientSocket.ClientSocket.RemoteEndPoint; //Parsing String to IP Address.
            int ClientPort = 7500; //Client Player Two Connection Port.
            GameClient.PlayerOneToPlayerTwoConnection = new TcpClient(); //Starts Player Two Connection TCP Client.
            try //Tries to Read From Player Two. 
            {
                GameClient.PlayerOneToPlayerTwoConnection.Connect(GameClient.PlayerTwoEndPoint.ToString().Split(':')[0], ClientPort); //Connects to Player Two Connection for Writing.
                GameClient.PlayerOneToPlayerTwoStream = GameClient.PlayerOneToPlayerTwoConnection.GetStream(); //Gets Stream of Player Two Connection.
                GameClient.PlayerOneToPlayerTwoStream.Read(EncodedResponse, 0, EncodedResponse.Length); //Starts Reading For Player Two's Response Whether He Wants to Play or Not.
                GameClient.PlayerOneToPlayerTwoStream.Flush(); //Flushes Stream Incase of Acceptance and then Location Transmittion.
                DecodedResponse = Encoding.ASCII.GetString(EncodedResponse); //Gets Decoded Version of Player Two Response.
                if (DecodedResponse.Contains("Yes")) //If Response is Yes.
                {
                    GameClient.CurrentStatus = GameStatus.started; //Change Game Status to Started.
                    this.Invoke((MethodInvoker) delegate { PlayerTwoLabel.Text = ClientSocket.ClientName; }); //Changes Player Two Label Into Chosen Client Name. 
                    GameClient.PlayerTwoName = ClientSocket.ClientName; //Changes Player Two Name Into Client Chosen.
                }
                else //Else then Response is No.
                {
                    PlayersListBox.MouseDoubleClick += PlayersListBox_MouseDoubleClick; //ReAdds Mouse Double Click Event.
                    DisconnectFromPlayerTwo(); //Disconnects From Player Two Host Connection.
                }
            }
            catch (Exception Exc) //If Player Two Disconnected without Writing.
            {
                PlayersListBox.MouseDoubleClick += PlayersListBox_MouseDoubleClick; //ReAdds Mouse Double Click Event.
                DisconnectFromPlayerTwo(); //Disconnects From Player Two Host Connection.
            } 
        }
        #endregion

        #region Write And Read Location

        #region Player Two Write Location to Player One
        void PlayerTwoWriteLocation(Point p) //Player Two Write Location to Player One.
        {
            byte[] EncodedMessage = Encoding.ASCII.GetBytes($"LocationTwo:{p.X},{p.Y}"); //Encoded Play Location to be Sent
            GameClient.PlayerTwoToPlayerOneStream.Write(EncodedMessage, 0, EncodedMessage.Length); //Writing to Host Stream the Location of Play.  
        }
        #endregion

        #region Listen to Player Two Play Location
        void ListenForPlayerTwoPlay(object Obj) //Listens for Player Two Play Location.
        {
            try
            {
                string DecodedResponse = ""; //Initializes Decoded Response.
                byte[] EncodedResponse = new byte[256]; //Encoded Response of Player Two.
                GameClient.PlayerOneToPlayerTwoStream.Read(EncodedResponse, 0, EncodedResponse.Length); //Start Reading From Player Two Stream.
                GameClient.PlayerOneToPlayerTwoStream.Flush(); //Flush Stream After Reading.
                DecodedResponse = Encoding.ASCII.GetString(EncodedResponse).Trim((char)0); //Decodes Response and Trims Control Characters.
                DecodedResponse = DecodedResponse.Split(new string []{ "LocationTwo:"}, StringSplitOptions.RemoveEmptyEntries)[0]; //Splits LocationTwo Token to get Actual Played Point.
                Point p = new Point(int.Parse(DecodedResponse.Split(',')[0]), int.Parse(DecodedResponse.Split(',')[1])); //Creates a new Point to Start Game Logic With.
                this.Invoke((MethodInvoker)delegate { gamelogic( p ); }); //Initiates Game Logic with Newly Acquired Point.
            }
            catch (Exception Exc) //If Failed to Read due to Disconnection.
            {
                int CreatedFlag = 0; //A Flag to notify the while loop to stop after invoking method.
                DisconnectFromPlayerTwo(); //Disconnects From Player Two Host Connection.
                do
                {
                    if (IsHandleCreated) //If Handle is Created Before Invoking
                    {
                        this.Invoke((MethodInvoker)delegate { InitializeGameBoard(); }); //Resets Game Graphics and Logic.
                        CreatedFlag = 1; //Exits while loop after initializing GameBoard.
                    }
                }
                while (CreatedFlag == 0); //While Flag is still 0.
            }
        }
        #endregion

        #endregion

        #region Disconnection

        #region Disconnect From Player Two

        void DisconnectFromPlayerTwo() //Disconnects from Player Two Host.
        {
            GameClient.PlayerTwoEndPoint = null; //Change Player Two End Point into Null.
            GameClient.PlayerOneToPlayerTwoStream.Close(); //Closes Player Two Play Stream.
            GameClient.PlayerOneToPlayerTwoConnection.Close(); //Closes Player Two Client Connection
        }

        #endregion

        #region Disconnect From Player One

        void DisconnectFromPlayerOne()
        {
            ReadFromHostThread.Abort();
            GameClient.ClientToHostStream.Close(); //Close Stream with Host.
            GameClient.ClientToHostConnection.Close(); //Closes Connection with Host.
            GameClient.ClientToHostConnection = new TcpClient(); //Creates a new TCP Client.
            GameClient.ClientToHostConnection.Connect(GameClient.ServerIP, 5500); //Reconnects to Server //Change IP to Server IP.
            GameClient.ClientToHostStream = GameClient.ClientToHostConnection.GetStream(); //Gets Server Stream.
            GameClient.LobbyMoveList.Clear(); //Clears Move List for a New Game.
            GameClient.LobbyNameList.Clear(); //Clears Name List for a New Game.
        }

        #endregion

        #region End Player Two Host

        void EndPlayerTwoHost() //Ends Hosting for Player Two
        {
            GameClient.PlayerTwoToPlayerOneStream.Close(); //Closes Player Two Stream.
            GameClient.PlayerOneRequestSocket.Close(); //Closes Socket Used For Hosting.
            GameClient.PlayerTwoToPlayerOneConnection.Stop(); //Closes Player Two Connection.
        }

        #endregion

        #endregion

        #region Game Logic

        #region Game Logic Parts Invoker
        void gamelogic(Point p)
        {
            int columnindex = this.columnnumber(p);
            if (columnindex != -1)
            {
                int rowindex = this.Emptyrow(columnindex);
                if (rowindex != -1)//incase the column is not full
                {
                    if (GameClient.GameClientRole == LobbyRole.PlayerOne)
                    {
                        lock (GameClient.LobbyMoveList)
                        {
                            GameClient.LobbyMoveList.Add(p.X.ToString() + ',' + p.Y.ToString());
                        }
                    }
                    
                    this.board[rowindex, columnindex] = this.turn;
                    if (this.turn == 1)//player 1
                    {
                        g = CreateGraphics();
                        g.FillEllipse(Token1_Color, 65 + 45 * columnindex, 70 + 45 * rowindex, 32, 32);
                    }
                    else if (this.turn == 2)//player 2
                    {
                        g = CreateGraphics();
                        g.FillEllipse(Token2_Color, 65 + 45 * columnindex, 70 + 45 * rowindex, 32, 32);
                    }
                    int winner = this.winplayer(this.turn);
                    if (winner != -1)//There is a winning player
                    {
                        string message = "";                          
                        byte[] EncodedMessage;
                        if(winner == 1)
                        {
                            switch (GameClient.GameClientRole)
                            {
                                case LobbyRole.PlayerOne:
                                    message = "You've Won!";
                                    EncodedMessage = Encoding.ASCII.GetBytes($"Score:{GameClient.ClientToHostConnection.Client.LocalEndPoint.ToString()},{GameClient.PlayerTwoEndPoint}*1");
                                    GameClient.ClientToHostStream.Write(EncodedMessage, 0, EncodedMessage.Length);
                                    break;
                                case LobbyRole.PlayerTwo:
                                    message = "You've Lost!";
                                    break;
                                case LobbyRole.Audience:
                                    message = PlayerOneLabel.Text + " Has Won!";
                                    break;
                            }
                        }
                        else{
                            switch (GameClient.GameClientRole)
                            {
                                case LobbyRole.PlayerOne:
                                    message = "You've Lost!";
                                    EncodedMessage = Encoding.ASCII.GetBytes($"Score:{GameClient.ClientToHostConnection.Client.LocalEndPoint.ToString()},{GameClient.PlayerTwoEndPoint}*2");
                                    GameClient.ClientToHostStream.Write(EncodedMessage, 0, EncodedMessage.Length);
                                    break;
                                case LobbyRole.PlayerTwo:
                                    message = "You've Won!";
                                    break;
                                case LobbyRole.Audience:
                                    message = PlayerTwoLabel.Text + " Has Won!";
                                    break;
                            }
                        }
                        InitializeGameBoard();
                        Notification = new Notifications();
                        Notification.label1.Text = message;
                        Notification.Show();
                    }
                    else if(GameClient.LobbyMoveList.Count == RowNum * ColNum)
                    {
                        InitializeGameBoard();
                        Notification = new Notifications();
                        Notification.label1.Text = "Its a Draw!";
                        Notification.Show();
                    }
                    else
                    {
                        if (GameClient.GameClientRole == LobbyRole.PlayerOne && turn == 1)
                        {
                            ThreadPool.QueueUserWorkItem(ListenForPlayerTwoPlay);
                        }
                        playerturn();
                    }
                }
            }

        }
        #endregion

        #region PlayerTurn Switch
        int playerturn()
        {
            if (this.turn == 1)
            {
                PlayerOneLabel.ForeColor = Color.Gray;
                PlayerTwoLabel.ForeColor = Color.White;
                this.turn = 2;
            }
            else
            {
                PlayerOneLabel.ForeColor = Color.White;
                PlayerTwoLabel.ForeColor = Color.Gray;
                this.turn = 1;
            }
            return this.turn;
        }
        #endregion

        #region Winning Logic
        int winplayer(int playertocheck)
        {
            //1-Vertical Win check(|)
            for (int row = 0; row < this.board.GetLength(0) - 3; row++)//as I check the upper three rows -3
            {
                for (int col = 0; col < this.board.GetLength(1); col++)
                {
                    if (this.AllnumberEqual(playertocheck, this.board[row, col], this.board[row + 1, col], this.board[row + 2, col], this.board[row + 3, col]))
                        return playertocheck;
                }
            }
            //2-Horizontal Win check(-)
            for (int row = 0; row < this.board.GetLength(0); row++)//as I check the upper three rows -3
            {
                for (int col = 0; col < this.board.GetLength(1) - 3; col++)
                {
                    if (this.AllnumberEqual(playertocheck, this.board[row, col], this.board[row, col + 1], this.board[row, col + 2], this.board[row, col + 3]))
                        return playertocheck;

                }
            }
            //3-Top Left Diagonal Win check(\)
            for (int row = 0; row < this.board.GetLength(0) - 3; row++)//as I check the upper three rows -3
            {
                for (int col = 0; col < this.board.GetLength(1) - 3; col++)
                {
                    if (this.AllnumberEqual(playertocheck, this.board[row, col], this.board[row + 1, col + 1], this.board[row + 2, col + 2], this.board[row + 3, col + 3]))
                        return playertocheck;

                }
            }
            //4-Top Right Diagonal Win check(/)
            for (int row = 0; row < this.board.GetLength(0) - 3; row++)//as I check the upper three rows -3
            {
                for (int col = 3; col < this.board.GetLength(1) - 3; col++)//col starts from three as before that it will go outside
                {
                    if (this.AllnumberEqual(playertocheck, this.board[row, col], this.board[row + 1, col - 1], this.board[row + 2, col - 2], this.board[row + 3, col - 3]))
                        return playertocheck;

                }
            }



            return -1;
        }
        #endregion

        #region Equal Number Method
        bool AllnumberEqual(int tocheck, params int[] numbers)//to check if all numbers in this array is equal to checknumber
        {
            foreach (int num in numbers)
            {
                if (num != tocheck)
                    return false;
            }
            return true;

        }
        #endregion

        #region ColNumber
        int columnnumber(Point mouse)
        {
            for (int i = 0; i < boardcolumns.Length; i++)
            {
                if ((mouse.X >= this.boardcolumns[i].X) && (mouse.Y >= this.boardcolumns[i].Y))
                {
                    if ((mouse.X <= this.boardcolumns[i].X + this.boardcolumns[i].Width) && (mouse.Y <= this.boardcolumns[i].Y + this.boardcolumns[i].Height))
                    {
                        return i;//which column the token will fall into

                    }

                }

            }
            return -1;//if it is out side the board columns
        }
        #endregion

        #region Empty Row
        int Emptyrow(int col)//it takes the column where the coin will fall in
        {
            for (int i = RowNum - 1; i >= 0; i--)
            {
                if (this.board[i, col] == 0)
                    return i;//the empty row
            }
            return -1;//incase column is full
        }
        #endregion

        #endregion

        #endregion
    }
}
