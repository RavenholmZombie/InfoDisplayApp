using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;

namespace InfoDisplayApp
{
    public partial class frmMessageWindow : Form
    {
        private string _messageText = "";
        private string _messageType = "info";
        public frmMessageWindow()
        {
            InitializeComponent();
        }

        private void frmMessageWindow_Load(object sender, EventArgs e)
        {

        }

        public string SetMessage(string messageText)
        {
            _messageText = messageText;
            rtbMessage.Text = _messageText;
            return _messageText;
        }

        public string SetIcon(string messageType)
        {
            _messageType = messageType;
            switch (messageType.ToLower())
            {
                case "info":
                    pboxIcn.BackgroundImage = Properties.Resources.info_icn;
                    break;
                case "warning":
                    pboxIcn.BackgroundImage = Properties.Resources.alert_icn_msg;
                    break;
                case "error":
                    pboxIcn.BackgroundImage = Properties.Resources.error_icn;
                    break;
                default:
                    pboxIcn.BackgroundImage = Properties.Resources.info_icn;
                    break;
            }
            return _messageType;
        }

        private void btnClose_Click(object sender, EventArgs e)
        {
            Close();
        }
    }
}
