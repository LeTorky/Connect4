
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using static System.Windows.Forms.VisualStyles.VisualStyleElement.Window;
using ConnectionClasses;
using System.Net;
using System.Threading;
using System.Net.Sockets;


namespace GameRoomSpace
{
    public partial class GameRoom : Form
    {
        #region Fields
        public Rectangle[] boardcolumns;
        public int[,] board;//The size of board
        public int turn;//Player 1 or player two
        private List<string> MoveStack;
        private string HostName;
        public string PlayerTwo;
        public LobbyClient LobbyClient;
        public Thread ReadRoomsThread;
        public Thread LobbyReadThread;
        public ConfirmPlayerTwo ConfirmPlayerTwoDiag;
        public ClientConnection PlayerTwoClient;
        private PlayAgain WinningDiag;
        public int RowNum;
        public int ColNum;
        SolidBrush Token1_Color;
        Brush Token2_Color;
        public Control Lobby;
        string[] RoomPlayers;
        public Graphics g;
        #endregion

        #region Constructor
        public GameRoom(LobbyClient SetLobbyClient, Color SetColor, string SetSize, Control SetLobby, string SetHostName, Thread SetLobbyThread)
        {
            InitializeComponent();
            g = this.CreateGraphics();
            Control.CheckForIllegalCrossThreadCalls = false;
            LobbyReadThread = SetLobbyThread;

            HostName = SetHostName;
            label2.Text = HostName;
            label3.Text = "Waiting...";
            LobbyClient = SetLobbyClient;
            Lobby = SetLobby;

            MoveStack = new List<string>();

            RowNum = int.Parse(SetSize.Split('*')[0]);
            ColNum = int.Parse(SetSize.Split('*')[1]);

            Token1_Color = new SolidBrush(SetColor);
            Token2_Color = Token1_Color.Color == Color.Black ? Brushes.White : Brushes.Black; 
              
            Invalidate();
     
            this.boardcolumns = new Rectangle[ColNum];
            this.board = new int[RowNum, ColNum];
            this.turn = 1;//player 1 will start

            switch (LobbyClient.LobbyClientRole)
            {
                case LobbyRole.PlayerOne:
                    ReadRoomsThread = new Thread(ReadPlayerOneList);
                    listBox1.MouseDoubleClick += ChoosePlayerTwo;
                    break;
                case LobbyRole.PlayerTwo:
                case LobbyRole.Audience:
                    ReadRoomsThread = new Thread(ReadPlayerTwoRequests);
                    break;
            }

            ReadRoomsThread.Start();
        }
        #endregion

        #region Event Handlers

        #region Closing Game
        private void GameRoom_FormClosing(object sender, FormClosingEventArgs e)
        {
            if (LobbyClient.LobbyClientRole == LobbyRole.PlayerOne)
            {
                if(LobbyClient.Status == GameStatus.started)
                {
                    WriteToServerMidGame();
                }
                byte[] EncodedDisconnection = Encoding.ASCII.GetBytes("EndHost");
                LobbyClient.HostStream.Write(EncodedDisconnection, 0, EncodedDisconnection.Length);
                LobbyClient.WriteToServerThread.Abort();
                LobbyClient.WriteToClients.Abort();
                LobbyClient.ConnectingThread.Abort();
                ReadRoomsThread.Abort();
                foreach (ClientConnection SpecificConneciton in LobbyClient.HostClientConnections)
                {
                    SpecificConneciton.ClientStream.Write(EncodedDisconnection, 0, EncodedDisconnection.Length);
                    SpecificConneciton.ClientStream.Close();
                    SpecificConneciton.ClientSocket.Close();
                }
                LobbyClient.HostingConnection.Stop();
                LobbyClient.HostClientConnections.Clear();
                Lobby.Show();
                this.Hide();
            }
            else
            {
                if((LobbyClient.LobbyClientRole == LobbyRole.PlayerTwo) && (LobbyClient.Status == GameStatus.started))
                {
                    byte[] EncodedDisconnection = Encoding.ASCII.GetBytes("EndConnection");
                    LobbyClient.HostStream.Write(EncodedDisconnection, 0, EncodedDisconnection.Length);
                }
                ReadRoomsThread.Abort();
                LobbyClient.HostStream.Close();
                LobbyClient.HostConnection.Close();
                LobbyClient.HostConnection = new TcpClient();
                LobbyClient.HostConnection.Connect(IPAddress.Parse("192.168.0.107"), 5500); //Change IP to Server IP
                LobbyClient.HostStream = LobbyClient.HostConnection.GetStream();
                LobbyReadThread.Resume();
                Lobby.Show();
                this.Hide();
            }
        }
        #endregion

        #region Mouse Click On GameBoard
        public void Form1_MouseClick(object sender, MouseEventArgs e)
        {
            int columnindex = this.columnnumber(e.Location);
            if (columnindex != -1)
            {
                int rowindex = this.Emptyrow(columnindex);
                if (rowindex != -1)//incase the column is not full
                {
                    if(LobbyClient.LobbyClientRole == LobbyRole.PlayerOne)
                    {
                        ThreadPool.QueueUserWorkItem(PlayerOnePlayAndRead, e.Location);
                    }
                    else
                    {
                        ThreadPool.QueueUserWorkItem(PlayerTwoWriteLocation, e.Location);
                    }
                }
            }
        }
        #endregion

        #region Form Paint Event
        private void Form1_Paint(object sender, PaintEventArgs e)
        {
            e.Graphics.FillRectangle(new SolidBrush(Color.FromArgb(77, 69, 97)), 25, 25, (340 * ColNum) / 7, (300 * RowNum) / 6);//Board size
            for (int i = 0; i < RowNum; i++)
            {
                this.boardcolumns[i] = new Rectangle(45 + 45 * i, 24, 32, (300 * RowNum) / 6);//Board columns
                for (int j = 0; j < ColNum; j++)
                {
                    e.Graphics.FillEllipse(new SolidBrush(Color.FromArgb(94, 87, 132)), 45 + 45 * j, 50 + 45 * i, 32, 32);//Board tokens Location l
                }
            }
        }
        #endregion

        #region Choose Player Two

        public void ChoosePlayerTwo(object sender, MouseEventArgs e)
        {
            ThreadPool.QueueUserWorkItem(SendPlayerTwoRequest);
        }

        #endregion

        #endregion

        #region Methods

        #region Write To Server Mid Game Disconnection

        private void WriteToServerMidGame() //Writing to Server that Game Ended MidWay.
        {
            lock (LobbyClient.HostStream) //Locks Stream between Host and Server to prevent Sending Data at Same Time.
            {
                byte[] LeaveMessage = Encoding.ASCII.GetBytes($"LeaveMidGame:{PlayerTwoClient.ClientName}"); //Prepares Message with Player Two Name.
                LobbyClient.HostStream.Write(LeaveMessage, 0, LeaveMessage.Length); //Sends Server New Message
            }
        }

        #endregion

        #region Send Player Two Request

        private void SendPlayerTwoRequest(object sender)
        {
            listBox1.MouseDoubleClick -= ChoosePlayerTwo;
            lock (LobbyClient.HostClientConnections)
            {
                byte[] EncodedRequest = Encoding.ASCII.GetBytes("PlayRequest:");
                byte[] EncodedResponse = new byte[265];
                string DecodedResponse = "";
                bool flag = true;
                ClientConnection SelectedClient = null;
                do
                {
                    try
                    {
                        SelectedClient = LobbyClient.HostClientConnections[listBox1.SelectedIndex];
                        flag = false;
                    }
                    catch(Exception Exc)
                    {
                        flag = true;
                    }
                }
                while (flag);

                NetworkStream SelectedPlayerStream = SelectedClient.ClientStream;
                SelectedPlayerStream.Write(EncodedRequest, 0, EncodedRequest.Length);
                try
                {
                    do
                    {
                        SelectedPlayerStream.Read(EncodedResponse, 0, EncodedResponse.Length);
                        DecodedResponse = Encoding.ASCII.GetString(EncodedResponse).Trim((char)0);
                    }
                    while (!DecodedResponse.Contains("PlayRequest:"));
                    SelectedPlayerStream.Flush();
                    if (DecodedResponse.Split(new string[] { "PlayRequest:" }, StringSplitOptions.RemoveEmptyEntries)[0] == "Yes")
                    {
                        LobbyClient.Status = GameStatus.started;
                        LobbyClient.HostClientConnections[listBox1.SelectedIndex].LobbyClientRole = LobbyRole.PlayerTwo;
                        PlayerTwoClient = LobbyClient.HostClientConnections[listBox1.SelectedIndex];
                        PlayerTwo = PlayerTwoClient.ClientName;
                        label3.Text = PlayerTwo;
                        this.MouseClick += Form1_MouseClick;
                        ThreadPool.QueueUserWorkItem(PlayerOneWriteLocation);
                        ThreadPool.QueueUserWorkItem(PlayerTwoDisconnect);
                        foreach (ClientConnection SpecificConnectin in LobbyClient.HostClientConnections)
                        {
                            byte[] PlayerTwo = Encoding.ASCII.GetBytes($"PlayerTwo:{LobbyClient.HostClientConnections[listBox1.SelectedIndex].ClientName}");
                            SpecificConnectin.ClientStream.Write(PlayerTwo, 0, PlayerTwo.Length);
                        }
                    }
                    else
                    {
                        listBox1.MouseDoubleClick += ChoosePlayerTwo;
                    }
                }
                catch (Exception Obj)
                {
                    listBox1.MouseDoubleClick += ChoosePlayerTwo;
                    //If Client Disconnects without writing
                }
            }
        }

        #endregion

        #region Player One Read List
        private void ReadPlayerOneList()
        {
            string[] LoadedRooms;
            while (true)
            {
                lock (LobbyClient.HostClientConnections)
                {
                    LoadedRooms = new string[LobbyClient.HostClientConnections.Count];
                    for(int i=0; i<LobbyClient.HostClientConnections.Count; i++)
                    {
                        LoadedRooms[i] = LobbyClient.HostClientConnections[i].ClientName;
                    }
                    if((RoomPlayers == null) || (LoadedRooms.Length != RoomPlayers.Length))
                    {
                        RoomPlayers = LoadedRooms;
                        UpdatePlayerList();
                    }
                    else
                    {
                        int flag;
                        for (flag = 0; (flag < LoadedRooms.Length) && (LoadedRooms[flag] == RoomPlayers[flag]); flag++);
                        if (flag != LoadedRooms.Length)
                        {
                            RoomPlayers = LoadedRooms;
                            UpdatePlayerList();
                        }
                    }
                }
                Thread.Sleep(1000);
            }
        }
        #endregion

        #region Player Two Read Requests
        public void ReadPlayerTwoRequests()
        {
            string[] LoadedRooms;
            while (true)
            {
                lock (LobbyClient.HostConnection)
                {
                    byte[] EncodedRooms = new byte[256];
                    LobbyClient.HostStream.Read(EncodedRooms, 0, EncodedRooms.Length);
                    string DecodedRooms = Encoding.ASCII.GetString(EncodedRooms).Trim((char)0);
                    if (DecodedRooms.Contains("Names:"))
                    {
                        LoadedRooms = DecodedRooms.Split(new string[] { "Names:" }, StringSplitOptions.RemoveEmptyEntries)[0].Split(new string[] { "," }, StringSplitOptions.RemoveEmptyEntries);
                        if ((RoomPlayers == null) || (LoadedRooms.Length != RoomPlayers.Length))
                        {
                            RoomPlayers = LoadedRooms;
                            UpdatePlayerList();
                        }
                        else
                        {
                            int flag;
                            for (flag = 0; (flag < LoadedRooms.Length) && (LoadedRooms[flag] == RoomPlayers[flag]); flag++) ;
                            if (flag != LoadedRooms.Length)
                            {
                                RoomPlayers = LoadedRooms;
                                UpdatePlayerList();
                            }
                        }
                    }
                    else if(DecodedRooms.Contains("PlayRequest:"))
                    {
                        ConfirmPlayerTwoDiag = new ConfirmPlayerTwo();
                        DialogResult Answer = ConfirmPlayerTwoDiag.ShowDialog();
                        byte[] DecodedAnswer;
                        if (Answer == DialogResult.Yes)
                        {
                            DecodedAnswer = Encoding.ASCII.GetBytes("PlayRequest:Yes");
                            LobbyClient.LobbyClientRole = LobbyRole.PlayerTwo;
                            LobbyClient.Status = GameStatus.started;
                            //Game Logic (Starts)
                        }
                        else
                        {
                            DecodedAnswer = Encoding.ASCII.GetBytes("PlayRequest:No");
                        }
                        LobbyClient.HostStream.Write(DecodedAnswer, 0, DecodedAnswer.Length);
                    }
                    else if (DecodedRooms.Contains("Location:"))
                    {

                        string [] points = DecodedRooms.Split(new string[] { "Location:" }, StringSplitOptions.RemoveEmptyEntries)[0].Split(new string[] { ";" }, StringSplitOptions.RemoveEmptyEntries);
                        if(points.Length != MoveStack.Count)
                        {
                            int Count = MoveStack.Count;
                            for(int i= Count; i<points.Length; i++)
                            {
                                Point point = new Point(int.Parse(points[i].Split(',')[0]), int.Parse(points[i].Split(',')[1]));
                                MoveStack.Add(points[i]);
                                gamelogic(point);
                            }
                            if(LobbyClient.LobbyClientRole == LobbyRole.PlayerTwo && turn == 2)
                            {
                                this.MouseClick += Form1_MouseClick;
                            }
                        }
                    }
                    else if (DecodedRooms.Contains("PlayerTwo:"))
                    {
                        PlayerTwo = DecodedRooms.Split(new string[] { "PlayerTwo:" }, StringSplitOptions.RemoveEmptyEntries)[0];
                        label3.Text = PlayerTwo;
                    }
                    else if (DecodedRooms.Contains("EndHost"))
                    {
                        this.Invoke((MethodInvoker)delegate { GameRoom_FormClosing(null, null); });
                    }
                    else if (DecodedRooms.Contains("RestartGame"))
                    {
                        ResetGameLogic();
                    }
                    LobbyClient.HostStream.Flush();
                }
            }
        }
        #endregion

        #region Update Player List
        private void UpdatePlayerList()
        {
            lock(RoomPlayers){
                listBox1.Items.Clear();
                foreach (string Name in RoomPlayers)
                {
                    listBox1.Items.Add(Name);
                }
            }               
        }
        #endregion

        #region Player One Play and Read Location

        private void PlayerOnePlayAndRead(object Obj)
        {

            Point p = (Point)Obj;
            lock (MoveStack)
            {
                MoveStack.Add($"{p.X},{p.Y}");
            }
            this.MouseClick -= Form1_MouseClick;
            gamelogic(p);

            byte[] EncodedPoint = new byte[256];
            string DecodedPoint;
            string[] point;
            do
            {
                try
                {
                    PlayerTwoClient.ClientStream.Read(EncodedPoint, 0, EncodedPoint.Length);
                }
                catch(Exception obj)
                {
                    Thread.CurrentThread.Abort();
                 
                }
                DecodedPoint = Encoding.ASCII.GetString(EncodedPoint).Trim((char)0);
            }
            while ( !DecodedPoint.Contains("LocationTwo:") );
            point = DecodedPoint.Split(new string[] { "LocationTwo:" }, StringSplitOptions.RemoveEmptyEntries)[0].Split(',');
            lock (MoveStack)
            {
                MoveStack.Add(point[0]+","+point[1]);
            }
            gamelogic(new Point(int.Parse(point[0]), int.Parse(point[1])));
            this.MouseClick += Form1_MouseClick;  
            

        }

        #endregion

        #region Player One Anticipating Player Two Disconnection

        private void PlayerTwoDisconnect(object Obj) //Listening to Player Two in Case of Mid Game Disconnection.
        {
            while (!PlayerTwoClient.ClientSocket.Poll(1, SelectMode.SelectRead) || (PlayerTwoClient.ClientSocket.Available > 0));
    
            if(LobbyClient.Status == GameStatus.started)
            {
                LobbyClient.Status = GameStatus.waiting; //Change Game Status to Waiting
                WriteToServerMidGame(); //Send to Server MidGame End Message.
                ResetGameLogic(); //Reset Game Logic.
                UpdateClientsRestart(); //Update Players with Game Restart Notification.
                listBox1.MouseDoubleClick += ChoosePlayerTwo; //Adds Event Handler so PlayerOne Can choose a new Player.
            }
        }

        #endregion

        #region Player One Updates Clients with Game Restart

        private void UpdateClientsRestart() //Sends to All Clients a Notification That Game Restarted.
        {
            lock (LobbyClient.HostClientConnections) //Locks HostClientConnections Incase of new Addition of Clinets While Writing.
            {
                foreach(ClientConnection SpecificConnection in LobbyClient.HostClientConnections) //Loops on All Clients Connected.
                {
                    if(!SpecificConnection.ClientSocket.Poll(1, SelectMode.SelectRead) || (PlayerTwoClient.ClientSocket.Available != 0)) //If Socket is Connected.
                    {
                        byte[] EncodedMessage = Encoding.ASCII.GetBytes("RestartGame"); //Prepares Encoded Message of Restart.
                        SpecificConnection.ClientStream.Write(EncodedMessage, 0, EncodedMessage.Length); //Sends Notification of Restart Game to Each Client.
                    }
                }
            }
        }

        #endregion

        #region Player One Write Location

        private void PlayerOneWriteLocation(object Obj)
        {
            string DecodedLocation;
            while (true)
            {
                lock (LobbyClient.HostClientConnections) lock(MoveStack)
                {
                    if(MoveStack.Count > 0)
                    {
                        DecodedLocation = "Location:";
                        foreach(string Location in MoveStack)
                        { 
                            DecodedLocation += (Location + ";");
                        }
                        byte[] EncodedLocation = Encoding.ASCII.GetBytes(DecodedLocation);
                        foreach(ClientConnection SpecificConnection in LobbyClient.HostClientConnections)
                        {
                            if(!SpecificConnection.ClientSocket.Poll(1, SelectMode.SelectRead) || (SpecificConnection.ClientSocket.Available > 0))
                            {
                                SpecificConnection.ClientStream.Write(EncodedLocation, 0, EncodedLocation.Length);
                            }
                            else
                            {

                                //Disconnect
                            }
                        }
                        Thread.Sleep(200);
                    }
                }
            }
        }

        #endregion

        #region Reset Game Logic

        private void ResetGameLogic() //Reset Game Logic (Graphics and Data holders) (Independant of Replaying; Hence we dont remove PlayerTwoClient yet)
        {
            MouseClick -= Form1_MouseClick;
            Invalidate(); //Reset GDI 
            boardcolumns = new Rectangle[ColNum]; //Reset Rectangles.
            board = new int[RowNum, ColNum]; //Reset Board.
            turn = 2;//Switch last Play as Player Two in order for Player One to Begin.
            SolidBrush Temp = (SolidBrush)Token2_Color;
            Token2_Color = Token1_Color;
            Token1_Color = Temp;
            label3.Text = "Waiting..."; //Resets Player Two Label Name.
            label2.ForeColor = Color.White; //Resets Player One Label Activitiy Color
            label3.ForeColor = Color.Gray; //Resets Player Two Label Activitiy Color
            lock (MoveStack) //Locks Moves Stack in Order to Prevent Enumaration while Clearing Stack from Receiving (Client) or Sending (Host) .
            {
                MoveStack.Clear(); //Clears Stack of all Moves
            }
        }

        #endregion

        #region Play Again Dialoge

        private void PlayAgainInvoke(int winner)
        {
            string message = "";

            switch (LobbyClient.LobbyClientRole)
            {
                case LobbyRole.PlayerOne:
                    message = winner == 1 ? "You've Won!" : "You've Lost :(";
                    WinningDiag = new PlayAgain(this, LobbyClient.LobbyClientRole, message);
                    WinningDiag.ShowDialog();
                    break;
                case LobbyRole.PlayerTwo:
                    message = winner == 2 ? "You've Won!" : "You've Lost :(";
                    WinningDiag = new PlayAgain(this, LobbyClient.LobbyClientRole, message);
                    WinningDiag.ShowDialog();
                    break;
                case LobbyRole.Audience:
                    message = winner == 1 ? HostName : PlayerTwo;
                    message += " Has Won!";
                    WinningDiag = new PlayAgain(this, LobbyClient.LobbyClientRole, message);
                    WinningDiag.ShowDialog();
                    break;
            }
        }

        #endregion

        #region Player Two Write Location
        private void PlayerTwoWriteLocation(object Obj)
        {
            Point p = (Point)Obj;
            byte[] EncodedLocation = Encoding.ASCII.GetBytes($"LocationTwo:{p.X},{p.Y}");
            byte[] EncodedPoint = new byte[265];
            lock (LobbyClient.HostStream)
            {
                LobbyClient.HostStream.Write(EncodedLocation, 0, EncodedLocation.Length);
                this.MouseClick -= Form1_MouseClick;
            }
         
        }
        #endregion

        #region GameLogic
        public void gamelogic(Point p)
        {
            int columnindex = this.columnnumber(p);
            if (columnindex != -1)
            {
                int rowindex = this.Emptyrow(columnindex);
                if (rowindex != -1)//incase the column is not full
                {
                    this.board[rowindex, columnindex] = this.turn;
                    if (this.turn == 1)//player 1
                    {
                        g = CreateGraphics();
                        g.FillEllipse(Token1_Color, 45 + 45 * columnindex, 50 + 45 * rowindex, 32, 32);
                    }
                    else if (this.turn == 2)//player 2
                    {
                        g = CreateGraphics();
                        g.FillEllipse(Token2_Color, 45 + 45 * columnindex, 50 + 45 * rowindex, 32, 32);
                    }

                    int winner = this.winplayer(this.turn);
                    if (winner != -1)//There is a winning player
                    {
                        ResetGameLogic();
                        PlayAgainInvoke(winner);
                    }
                    playerturn();
                }
            }
    
        }
        #endregion

        #region PlayerTurn Switch
        private int playerturn()
        {
            if (this.turn == 1)
            {
                label2.ForeColor = Color.Gray;
                label3.ForeColor = Color.White;
                this.turn = 2;
            }
            else
            {
                this.turn = 1;
                label3.ForeColor = Color.Gray;
                label2.ForeColor = Color.White;
            }
            return this.turn;
        }
        #endregion

        #region Winning Logic
        private int winplayer(int playertocheck)
        {
            //1-Vertical Win check(|)
            for(int row=0;row<this.board.GetLength(0)-3;row++)//as I check the upper three rows -3
            {
                for(int col=0;col<this.board.GetLength(1);col++)
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
                    if (this.AllnumberEqual(playertocheck, this.board[row, col], this.board[row , col + 1], this.board[row, col + 2], this.board[row , col + 3]))
                        return playertocheck;

                }
            }
            //3-Top Left Diagonal Win check(\)
            for (int row = 0; row < this.board.GetLength(0)-3; row++)//as I check the upper three rows -3
            {
                for (int col = 0; col < this.board.GetLength(1) - 3; col++)
                {
                    if (this.AllnumberEqual(playertocheck, this.board[row, col], this.board[row+1, col + 1], this.board[row+2, col + 2], this.board[row+3, col + 3]))
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
        private bool AllnumberEqual(int tocheck,params int[] numbers)//to check if all numbers in this array is equal to checknumber
        {
            foreach(int num in numbers)
            {
                if (num != tocheck)
                    return false;
            }
            return true;

        }
        #endregion

        #region ColNumber
        private int columnnumber(Point mouse)
        {
            for(int i=0;i<boardcolumns.Length;i++)
            {
                if ((mouse.X >=this.boardcolumns[i].X)&&(mouse.Y>=this.boardcolumns[i].Y))
                {
                    if((mouse.X <= this.boardcolumns[i].X+ this.boardcolumns[i].Width) && (mouse.Y <= this.boardcolumns[i].Y + this.boardcolumns[i].Height))
                    {
                        return i;//which column the token will fall into

                    }

                }

            }
            return -1;//if it is out side the board columns
        }
        #endregion

        #region Empty Row
        private int Emptyrow(int col)//it takes the column where the coin will fall in
        {
            for(int i = RowNum - 1; i >= 0 ; i--)
            {
                if(this.board[i,col]==0)
                     return i;//the empty row
            }
            return -1;//incase column is full
        }
        #endregion

        #endregion

    }
}
