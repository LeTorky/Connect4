using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Threading;
using System.Windows.Forms;

namespace GameRoomSpace
{
    public partial class PlayAgain : Form
    {
        #region Fields
        private GameRoom CurrentGame;
        private ConnectionClasses.LobbyRole ClientRole;
        #endregion

        #region Constructor
        public PlayAgain(GameRoom SetRoom, ConnectionClasses.LobbyRole SetRole, string SetMessage)
        {
            InitializeComponent();
            CurrentGame = SetRoom;
            ClientRole = SetRole;
            label1.Text = SetMessage;
            switch (ClientRole)
            {
                case ConnectionClasses.LobbyRole.PlayerTwo:
                    ThreadPool.QueueUserWorkItem(ListenForPlayAgain);
                    button2.Enabled = false;
                    break;
                case ConnectionClasses.LobbyRole.Audience:
                    this.Controls.Remove(button2);
                    this.Controls.Remove(button3);
                    break;
            }
        }
        #endregion

        #region Event Handlers

        #region Accept Button
        private void Button2_Click(object sender, EventArgs e)
        {
            ThreadPool.QueueUserWorkItem(PlayAgainRequest);
        }
        #endregion

        #region Reject Button
        private void Button3_Click(object sender, EventArgs e)
        {
            byte[] EncodedMessage = Encoding.ASCII.GetBytes("PlayAgain:No");
            switch (ClientRole)
            {
                case ConnectionClasses.LobbyRole.PlayerOne:
                    CurrentGame.LobbyClient.Status = ConnectionClasses.GameStatus.waiting;
                    lock (CurrentGame.PlayerTwoClient)
                    {
                        CurrentGame.PlayerTwoClient.ClientStream.Write(EncodedMessage, 0, EncodedMessage.Length);
                    }
                    CurrentGame.listBox1.MouseDoubleClick += CurrentGame.ChoosePlayerTwo;
                    CurrentGame.MouseClick += CurrentGame.Form1_MouseClick;
                    break;
                case ConnectionClasses.LobbyRole.PlayerTwo:
                    CurrentGame.LobbyClient.LobbyClientRole = ConnectionClasses.LobbyRole.Audience;
                    lock (CurrentGame.LobbyClient.HostStream)
                    {
                        CurrentGame.LobbyClient.HostStream.Write(EncodedMessage, 0, EncodedMessage.Length);
                    }
                    break;
            }
     
            this.Close();
        }
        #endregion

        #endregion

        #region Methods

        #region Play Again

        private void PlayAgainRequest(object Obj)
        {
           
            byte[] EncodedMessage;
            byte[] EncodedResponse = new byte[256];
            string DecodedResponse;

            switch (CurrentGame.LobbyClient.LobbyClientRole)
            {
                case ConnectionClasses.LobbyRole.PlayerOne:
                    button2.Enabled = false;
                    EncodedMessage = Encoding.ASCII.GetBytes("PlayAgain:Yes");
                    lock (CurrentGame.PlayerTwoClient)
                    {
                        CurrentGame.PlayerTwoClient.ClientStream.Write(EncodedMessage, 0, EncodedMessage.Length);
                        do
                        {
                            try
                            {
                                CurrentGame.PlayerTwoClient.ClientStream.Read(EncodedResponse, 0, EncodedResponse.Length);
                            }
                            catch(Exception Exc)
                            {
                                Thread.CurrentThread.Abort();
                            }
                            DecodedResponse = Encoding.ASCII.GetString(EncodedResponse).Trim((char)0);
                        }
                        while (!DecodedResponse.Contains("PlayAgain:"));

                        CurrentGame.PlayerTwoClient.ClientStream.Flush();

                        if(DecodedResponse.Split(new string[] { "PlayAgain:"}, StringSplitOptions.RemoveEmptyEntries)[0] == "Yes")
                        {
                            CurrentGame.MouseClick += CurrentGame.Form1_MouseClick;
                        }
                        else
                        {
                            CurrentGame.LobbyClient.Status = ConnectionClasses.GameStatus.waiting;
                            CurrentGame.listBox1.MouseDoubleClick += CurrentGame.ChoosePlayerTwo;
                            CurrentGame.PlayerTwo = "Waiting...";
                            CurrentGame.label3.Text = "Waiting...";
                        }
                    }
                    this.Close();
                    break;

                case ConnectionClasses.LobbyRole.PlayerTwo:
                    EncodedMessage = Encoding.ASCII.GetBytes("PlayAgain:Yes");
                    lock (CurrentGame.LobbyClient.HostStream)
                    {
                        CurrentGame.LobbyClient.HostStream.Write(EncodedMessage, 0, EncodedMessage.Length);
                        //Thread.Sleep(500);
                        //CurrentGame.LobbyClient.HostStream.Write(EncodedMessage, 0, EncodedMessage.Length);
                    }
                    this.Close();
                    break;
            }
        }

        #endregion

        #region Wait For Player One Request

        public void ListenForPlayAgain(object Obj)
        {
            byte[] EncodedResponse = new byte[256];
            string DecodedResponse;
            do
            {
                try
                {
                    CurrentGame.LobbyClient.HostStream.Read(EncodedResponse, 0, EncodedResponse.Length);
                }
                catch (Exception Exc)
                {
                    Thread.CurrentThread.Abort();
                }
                DecodedResponse = Encoding.ASCII.GetString(EncodedResponse).Trim((char)0);
            }
            while (!DecodedResponse.Contains("PlayAgain:"));
            CurrentGame.LobbyClient.HostStream.Flush();
            DecodedResponse = DecodedResponse.Split(new string[] { "PlayAgain:" }, StringSplitOptions.RemoveEmptyEntries)[0];
            if (DecodedResponse.Contains("Yes"))
            {
                button2.Enabled = true;
            }
            else
            {

                this.Close();
            }
        }

        #endregion

        #endregion
    }
}
