using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;

namespace MSNPSharpClient
{
    public partial class MusicForm : Form
    {
        public MusicForm()
        {
            InitializeComponent();
        }


        public string Artist
        {
            get
            {
                return txtArtist.Text;
            }
        }

        public string Song
        {
            get
            {
                return txtSong.Text;
            }
        }

        public string Album
        {
            get
            {
                return txtSong.Text;
            }
        }

    }
}