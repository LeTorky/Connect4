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
    public partial class client : Form
    {
        /*---Class Fields---*/
        Lobby ActivateLobby;
        public client()
        {
            InitializeComponent();
        }

        /*---Event Handlers---*/
        private void Button1_Click(object sender, EventArgs e)
        {
            if (this.textBox1.Text.Length > 0)
            {
                ActivateLobby = new Lobby(this.textBox1.Text);
                ActivateLobby.Show();
                this.Hide();
            }
        }
    }
}
