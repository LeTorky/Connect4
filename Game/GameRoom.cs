
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

namespace Trial_1
{
    public partial class Form1 : Form
    {
        private Rectangle[] boardcolumns;
        private int[,] board;//The size of board
        private int turn;//Player 1 or player two

        int RowNum;
        int ColNum;
        Brush Token1_Color;
        //Brush Token2_Color;
        public Form1()
        {
            InitializeComponent();

            Config dlg = new Config();
            DialogResult dResult;

            dResult = dlg.ShowDialog();
            if (dResult == DialogResult.OK)
            {
                String size = dlg.BoardSize;
                string[] words = (size).Split('*');
   
                RowNum = int.Parse(words[0]);
                ColNum = int.Parse(words[1]);

                Token1_Color = dlg.ColorToPlay;
              
                Invalidate();
            }

            this.boardcolumns = new Rectangle[ColNum];
            this.board = new int[RowNum, ColNum];
            this.turn = 1;//player 1 will start
 
        }
    
        private void Form1_Paint(object sender, PaintEventArgs e)
        {
            e.Graphics.FillRectangle(Brushes.Blue, 25, 25, (340 * ColNum) / 7, (300 * RowNum) / 6);//Board size
            for (int i = 0; i < RowNum; i++)
            {
                for (int j = 0; j < ColNum; j++)
                {
                    if (i == 0)
                    {
                        this.boardcolumns[j] = new Rectangle(45 + 45 * j, 24, 32, (300 * RowNum) / 6);//Board columns
                    }
                    e.Graphics.FillEllipse(Brushes.White, 45 + 45 * j, 50 + 45 * i, 32, 32);//Board tokens Location l
                }
            }

        }
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
                        g.FillEllipse(Brushes.Purple, 45 + 45 * columnindex, 50 + 45 * rowindex, 32, 32);

                    }
                    int winner = this.winplayer(this.turn);
                    if (winner != -1)//There is a winning player
                    {
                        string player = (winner == 1) ? "Player 1" : "Player 2";
                        MessageBox.Show("Congratulations! " + player + " has won!");
                        Application.Restart();
                    }
                    playerturn();
                }
            }

        }

        private void Form1_MouseClick(object sender, MouseEventArgs e)
        {
            gamelogic(e.Location);
        }
        private int playerturn()
        {
            if (this.turn == 1)
                this.turn = 2;
            else
                this.turn = 1;

            return this.turn;
        }
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
        private bool AllnumberEqual(int tocheck,params int[] numbers)//to check if all numbers in this array is equal to checknumber
        {
            foreach(int num in numbers)
            {
                if (num != tocheck)
                    return false;
            }
            return true;

        }
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
        private int Emptyrow(int col)//it takes the column where the coin will fall in
        {
            for(int i = RowNum - 1; i >= 0 ; i--)
            {
                if(this.board[i,col]==0)
                     return i;//the empty row
            }
            return -1;//incase column is full
        }

        private void Form1_Load(object sender, EventArgs e)
        {
           
        }
    }
}
