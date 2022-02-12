﻿
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


namespace GameRoomSpace
{
    public partial class GameRoom : Form
    {
        #region Fields
        private Rectangle[] boardcolumns;
        private int[,] board;//The size of board
        private int turn;//Player 1 or player two
        public LobbyClient LobbyClient;
        int RowNum;
        int ColNum;
        SolidBrush Token1_Color;
        Brush Token2_Color;
        Control Lobby;
        #endregion

        #region Constructor
        public GameRoom(LobbyClient SetLobbyClient, Color SetColor, string SetSize, Control SetLobby)
        {
            InitializeComponent();
            LobbyClient = SetLobbyClient;
            Lobby = SetLobby;
   
            RowNum = int.Parse(SetSize.Split('*')[0]);
            ColNum = int.Parse(SetSize.Split('*')[1]);

            Token1_Color = new SolidBrush(SetColor);
            Token2_Color = Token1_Color.Color == Color.Green ? Brushes.Orange : Brushes.Green; 
              
            Invalidate();
     
            this.boardcolumns = new Rectangle[ColNum];
            this.board = new int[RowNum, ColNum];
            this.turn = 1;//player 1 will start

            if (LobbyClient.LobbyClientRole == LobbyRole.PlayerOne)
            {

            }
            else
            {

            }
        }
        #endregion


        #region Event Handlers

        #region Closing Game
        private void GameRoom_FormClosing(object sender, FormClosingEventArgs e)
        {
            if (LobbyClient.LobbyClientRole == LobbyRole.PlayerOne)
            {
                foreach(ClientConnection SpecificConneciton in LobbyClient.HostClientConnections)
                {
                    SpecificConneciton.ClientStream.Close();
                    SpecificConneciton.ClientSocket.Close();
                }
                LobbyClient.HostingThread.Abort();
                LobbyClient.HostStream.Close();
                LobbyClient.HostConnection.Close();
                LobbyClient.HostingConnection.Stop();
            }
            else
            {
                LobbyClient.HostStream.Close();
                LobbyClient.HostConnection.Close();
                this.Hide();
                LobbyClient.HostConnection = new System.Net.Sockets.TcpClient();
                LobbyClient.HostConnection.Connect(IPAddress.Parse("192.168.0.107"), 5500);
                LobbyClient.HostStream = LobbyClient.HostConnection.GetStream();
                Lobby.Show();
            }
        }
        #endregion

        #region Mouse Click On GameBoard
        private void Form1_MouseClick(object sender, MouseEventArgs e)
        {
            gamelogic(e.Location);
        }
        #endregion

        #region Form Paint Event
        private void Form1_Paint(object sender, PaintEventArgs e)
        {
            e.Graphics.FillRectangle(Brushes.Blue, 25, 25, (340 * ColNum) / 7, (300 * RowNum) / 6);//Board size
            for (int i = 0; i < RowNum; i++)
            {
                this.boardcolumns[i] = new Rectangle(45 + 45 * i, 24, 32, (300 * RowNum) / 6);//Board columns
                for (int j = 0; j < ColNum; j++)
                {
                    e.Graphics.FillEllipse(Brushes.White, 45 + 45 * j, 50 + 45 * i, 32, 32);//Board tokens Location l
                }
            }
        }
        #endregion

        #endregion


        #region Methods

        #region UpdatePlayerList

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
                        Graphics g = CreateGraphics();
                        g.FillEllipse(Token1_Color, 45 + 45 * columnindex, 50 + 45 * rowindex, 32, 32);
                    }
                    else if (this.turn == 2)//player 2
                    {
                        Graphics g = CreateGraphics();
                        g.FillEllipse(Token2_Color, 45 + 45 * columnindex, 50 + 45 * rowindex, 32, 32);
                    }
                    int winner = this.winplayer(this.turn);
                    if (winner != -1)//There is a winning player
                    {
                        string player = (winner == 1) ? "Player 1" : "Player 2";
                        MessageBox.Show("Congratulations! " + player + " has won!");
                        //Application.Restart(); Replace With Play Again?
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
                this.turn = 2;
            else
                this.turn = 1;

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
