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
    public partial class Acceptance : Form
    {
        public Acceptance()
        {
            InitializeComponent();
        }
        public string PName
        {
            set { PlayerName.Text = value+" ?"; }
        }
        private void button1_Click(object sender, EventArgs e)
        {
            DialogResult = DialogResult.Yes;
        }

        private void button2_Click(object sender, EventArgs e)
        {
            DialogResult = DialogResult.No;
        }
    }
}
