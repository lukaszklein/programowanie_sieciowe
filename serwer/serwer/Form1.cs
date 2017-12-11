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
using System.Net.Sockets;

namespace serwer
{
    public partial class Form1 : Form
    {
        private TcpListener serwer;
        private TcpClient klient;
        
        public Form1()
        {
            InitializeComponent();
        }

        private void button1_Click(object sender, EventArgs e)
        {
            IPAddress adresIP = null;

            try
            {
                adresIP = IPAddress.Parse(textBox1.Text);
            }
            catch
            {
                MessageBox.Show("Błędny format adresu IP!");
                textBox1.Text = String.Empty;
                return;
            }

            int port = System.Convert.ToInt16(numericUpDown1.Value);

            try
            {
                serwer = new TcpListener(adresIP, port);
                serwer.Start();
                klient = serwer.AcceptTcpClient();
                IPEndPoint IP = (IPEndPoint)klient.Client.RemoteEndPoint;
                listBox1.Items.Add("["+IP.ToString()+"] :Nawiązano połączenie");
                button1.Enabled = false;
                button2.Enabled = true;
                klient.Close();
                serwer.Stop();
            }
            catch (Exception ex)
            {
                listBox1.Items.Add("Błąd startu serwera!");
                MessageBox.Show(ex.ToString());
            }
        }

        private void button2_Click(object sender, EventArgs e)
        {
            serwer.Stop();
            klient.Close();
            listBox1.Items.Add("Zakończono pracę serwera.");
            button1.Enabled = true;
            button2.Enabled = false;
        }
    }
}
