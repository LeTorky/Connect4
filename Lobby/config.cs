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
    public partial class Config : Form
    {
        Color Token_Clr;

        public Config()
        {
            InitializeComponent();
        }
        public Color TokenColor //Color to play property
        {
            get
            {
                switch (comboBox2.Text)
                {
                    case "Orange":
                        Token_Clr = Color.Orange;
                        break;
                    case "Green":
                        Token_Clr = Color.Green;
                        break;
                }
                return Token_Clr;

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
            this.Close();    //Close config form
        }

        private void button3_Click(object sender, EventArgs e) //Cancel button
        {
            DialogResult = DialogResult.Cancel; //Store result Cancel in dialogresult
            this.Close();  //Close config form
        }
    }
}
