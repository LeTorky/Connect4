using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Net;

namespace GameConfig
{
    public partial class client : Form
    {
        /*---Class Fields---*/
        Lobby ActivateLobby;
        Ipconfig IPConfig;
        IPAddress SetIP = IPAddress.Parse("192.168.0.107");

        public client()
        {
            InitializeComponent();
            IPConfig = new Ipconfig();
        }

        /*---Event Handlers---*/
        private void Button1_Click(object sender, EventArgs e)
        {
            if (this.textBox1.Text.Length > 0)
            {
                if (IPConfig.SetIP!=null)
                {
                    SetIP = IPConfig.SetIP;
                }
                ActivateLobby = new Lobby(this.textBox1.Text, SetIP);
                ActivateLobby.Show();
                this.Hide();
            }
        }

        private void Button2_Click(object sender, EventArgs e)
        {
            IPConfig.Show();
        }
    }
}
