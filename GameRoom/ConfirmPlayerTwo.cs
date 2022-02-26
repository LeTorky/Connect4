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
    public partial class ConfirmPlayerTwo : Form
    {
        public ConfirmPlayerTwo()
        {
            InitializeComponent();

        }

        private void Button2_Click(object sender, EventArgs e)
        {
            DialogResult = DialogResult.Yes;
        }

        private void Button3_Click(object sender, EventArgs e)
        {
            DialogResult = DialogResult.No;
        }
    }
}
