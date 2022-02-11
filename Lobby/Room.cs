using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace GameConfig
{
    public class Room
    {

        //#Room Name Field  (label1)
        Color m_RoomNameColor;
        int m_RoomNameSize;
        Font m_RoomNameFont;
        Label m_RoomName;
        //#Players Number Field (label2)
        Color m_PlayersNumberColor;
        int m_PlayersNumberSize;
        Font m_PlayersNumberFont;
        Label m_PlayersNumber;

        //#Button Field
        string m_ButtonTxt; //join 
        Font m_ButtonsFont;
        Color m_ButtonFontColor;
        Color m_ButtonBackColor;
        int m_ButtonHeight;
        Button m_Button;

        //#Socket Field
        public Room()
        {
            InitRoom();
        }

        public Label RoomName
        {
            get { return m_RoomName; }
            set { m_RoomName = value; }
        }
        public Label PlayersNumber
        {
            get { return m_PlayersNumber; }
            set { m_PlayersNumber = value; }
        }
        public Button RoomButton
        {
            get { return m_Button; }
            set { m_Button = value; }
        }

        private void InitRoom() //Room intialization function
        {
            //Room Name Intialization
            /*Styling of label 1*/
            m_RoomNameColor = Color.Black;
            m_RoomNameSize = 10;
            m_RoomNameFont = new Font("Microsoft Sans Serif", m_RoomNameSize, FontStyle.Bold);
            m_RoomName = new Label();
            m_RoomName.ForeColor = m_RoomNameColor;
            m_RoomName.Font = m_RoomNameFont;

            //Players Number Intialization
            /*Styling of label 2*/
            m_PlayersNumberColor = Color.Gray;
            m_PlayersNumberSize = 10;
            m_PlayersNumberFont = new Font("Microsoft Sans Serif", m_PlayersNumberSize, FontStyle.Italic);
            m_PlayersNumber = new Label();
            m_PlayersNumber.ForeColor = m_PlayersNumberColor;
            m_PlayersNumber.Font = m_PlayersNumberFont;

            //Button Intialization
            /*Styling of button*/
            m_ButtonTxt = "Join";
            m_ButtonsFont = new Font("Microsoft Sans Serif", 10, FontStyle.Bold);
            m_ButtonFontColor = Color.White;
            m_ButtonBackColor = Color.LightSeaGreen;
            m_ButtonHeight = 30;
            m_Button = new Button();
            m_Button.Font = m_ButtonsFont;
            m_Button.ForeColor = m_ButtonFontColor;
            m_Button.BackColor = m_ButtonBackColor;
            m_Button.Height = m_ButtonHeight;
            m_Button.Text = m_ButtonTxt;
        }
    }
}
