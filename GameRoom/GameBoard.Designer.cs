namespace GameRoomSpace
{
    partial class GameBoard
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            this.PlayerOneLabel = new System.Windows.Forms.Label();
            this.PlayerTwoLabel = new System.Windows.Forms.Label();
            this.PlayersListBox = new System.Windows.Forms.ListBox();
            this.PlayersListLabel = new System.Windows.Forms.Label();
            this.SuspendLayout();
            // 
            // PlayerOneLabel
            // 
            this.PlayerOneLabel.Anchor = System.Windows.Forms.AnchorStyles.None;
            this.PlayerOneLabel.AutoSize = true;
            this.PlayerOneLabel.Font = new System.Drawing.Font("Microsoft Sans Serif", 15F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.PlayerOneLabel.ForeColor = System.Drawing.Color.White;
            this.PlayerOneLabel.Location = new System.Drawing.Point(40, 9);
            this.PlayerOneLabel.Name = "PlayerOneLabel";
            this.PlayerOneLabel.Size = new System.Drawing.Size(141, 29);
            this.PlayerOneLabel.TabIndex = 4;
            this.PlayerOneLabel.Text = "PlayerOne";
            // 
            // PlayerTwoLabel
            // 
            this.PlayerTwoLabel.Anchor = System.Windows.Forms.AnchorStyles.None;
            this.PlayerTwoLabel.AutoSize = true;
            this.PlayerTwoLabel.Font = new System.Drawing.Font("Microsoft Sans Serif", 15F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.PlayerTwoLabel.ForeColor = System.Drawing.Color.White;
            this.PlayerTwoLabel.Location = new System.Drawing.Point(464, 9);
            this.PlayerTwoLabel.Name = "PlayerTwoLabel";
            this.PlayerTwoLabel.Size = new System.Drawing.Size(129, 29);
            this.PlayerTwoLabel.TabIndex = 5;
            this.PlayerTwoLabel.Text = "Waiting...";
            // 
            // PlayersListBox
            // 
            this.PlayersListBox.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(77)))), ((int)(((byte)(69)))), ((int)(((byte)(97)))));
            this.PlayersListBox.BorderStyle = System.Windows.Forms.BorderStyle.None;
            this.PlayersListBox.Font = new System.Drawing.Font("Tahoma", 10F);
            this.PlayersListBox.ForeColor = System.Drawing.SystemColors.Window;
            this.PlayersListBox.FormattingEnabled = true;
            this.PlayersListBox.ItemHeight = 21;
            this.PlayersListBox.Location = new System.Drawing.Point(644, 66);
            this.PlayersListBox.Name = "PlayersListBox";
            this.PlayersListBox.Size = new System.Drawing.Size(120, 357);
            this.PlayersListBox.TabIndex = 6;
            // 
            // PlayersListLabel
            // 
            this.PlayersListLabel.Anchor = System.Windows.Forms.AnchorStyles.None;
            this.PlayersListLabel.AutoSize = true;
            this.PlayersListLabel.Font = new System.Drawing.Font("Microsoft Sans Serif", 15F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.PlayersListLabel.ForeColor = System.Drawing.Color.White;
            this.PlayersListLabel.Location = new System.Drawing.Point(648, 9);
            this.PlayersListLabel.Name = "PlayersListLabel";
            this.PlayersListLabel.Size = new System.Drawing.Size(105, 29);
            this.PlayersListLabel.TabIndex = 7;
            this.PlayersListLabel.Text = "Players";
            // 
            // GameBoard
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(7F, 16F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(94)))), ((int)(((byte)(87)))), ((int)(((byte)(132)))));
            this.ClientSize = new System.Drawing.Size(800, 450);
            this.Controls.Add(this.PlayersListLabel);
            this.Controls.Add(this.PlayersListBox);
            this.Controls.Add(this.PlayerTwoLabel);
            this.Controls.Add(this.PlayerOneLabel);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedSingle;
            this.MaximizeBox = false;
            this.Name = "GameBoard";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            this.Text = "Form1";
            this.FormClosing += new System.Windows.Forms.FormClosingEventHandler(this.GameBoard_FormClosing);
            this.Paint += new System.Windows.Forms.PaintEventHandler(this.GameBoard_Paint);
            this.MouseClick += new System.Windows.Forms.MouseEventHandler(this.GameBoard_MouseClick);
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Label PlayerOneLabel;
        private System.Windows.Forms.Label PlayerTwoLabel;
        private System.Windows.Forms.ListBox PlayersListBox;
        private System.Windows.Forms.Label PlayersListLabel;
    }
}

