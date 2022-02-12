using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace GameRoomSpace
{
    //private Color TokenColor;
    public partial class Config : Form
    {
        Color Token_Clr;

        public Config()
        {
            InitializeComponent();
        }
        public Brush ColorToPlay //Color to play property
        {
            get {
               
                switch (comboBox2.Text)
                {
                    case "Red":
                        Token_Clr = Color.Red;
                        break;
                    case "Green":
                        Token_Clr = Color.Green;
                        break;
                    case "Yellow":
                        Token_Clr = Color.Yellow;
                        break;

                }
      
                Brush TokenBr = new SolidBrush(Token_Clr);
                return TokenBr;
              
                } //get color
   
        }
        public string BoardSize
        {
            get { return comboBox1.Text; } //get size of board
            set { comboBox1.Text = value; } //set board size
        }

        private void button2_Click(object sender, EventArgs e) //Ok button
        {
            DialogResult = DialogResult.OK;    //Store result ok in dialogresult
            //open game form


            //Acceptance a = new Acceptance();
            //a.PName = "hager";
            //a.ShowDialog();


            this.Close();    //Close config form

        }

        private void button3_Click(object sender, EventArgs e) //Cancel button
        {
            DialogResult = DialogResult.Cancel; //Store result Cancel in dialogresult
            this.Close();  //Close config form
        }

     
    }
}
