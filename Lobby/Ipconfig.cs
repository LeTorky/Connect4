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
    public partial class Ipconfig : Form
    {
        public IPAddress SetIP;
        public Ipconfig()
        {
            InitializeComponent();
        }

        private void Button1_Click(object sender, EventArgs e)
        {
            SetIP = IPAddress.Parse(textBox1.Text);
            this.Close();
        }
    }
}
