using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Net.Sockets;
using System.Net;
using System.IO;
using System.ComponentModel;

namespace Czat
{
    /// <summary>
    /// Logika interakcji dla klasy MainWindow.xaml
    /// </summary>
    
    public partial class MainWindow : Window
    {
        private BackgroundWorker worker = new BackgroundWorker();
        private BackgroundWorker worker2 = new BackgroundWorker();
        private TcpClient klient = null;
        private TcpListener listener = null;
        delegate void SetTextCallBack(string tekst);
        delegate void IPReadCallBack();
        delegate void PortReadCallBack();
        private bool conect = false;
        private BinaryReader czytanie = null;
        private BinaryWriter pisanie = null;
        string ip= "";
        string port = "";

        public MainWindow()
        {
            InitializeComponent();
            worker.DoWork += new DoWorkEventHandler(worker_DoWork);
            worker2.DoWork += new DoWorkEventHandler(worker2_DoWork);

        }

        private void Stop_Click(object sender, RoutedEventArgs e)
        {
            conect = false;
            Stop.Background = Brushes.Red;
            Start.Background = Brushes.Gray;
            IP.IsEnabled = true;
            port_nb.IsEnabled = true;
        }

        private void Start_Click(object sender, RoutedEventArgs e)
        {
            
            Stop.Background = Brushes.Gray;
            Start.Background = Brushes.Green;
            IP.IsEnabled = false;
            port_nb.IsEnabled = false;
            if (conect == true) { }
            else { 
                worker.RunWorkerAsync();
            }
        }
        
        private void SetText(string text)
        {
            if (!Dispatcher.CheckAccess())
            {
                Dispatcher.Invoke(new SetTextCallBack(SetText), text);
                return;
            }
            ChatBox.AppendText(text);
        }
        private void IPRead()
        {
            if (!Dispatcher.CheckAccess())
            {
                Dispatcher.Invoke( new IPReadCallBack(IPRead));
                return;
            }
            ip = IP.Text;
        }
        private void PortRead()
        {
            if (!Dispatcher.CheckAccess())
            {
                
                Dispatcher.Invoke(new PortReadCallBack(PortRead));
                return;
            }
            
            port = port_nb.Text;
        }
        private void worker_DoWork(object sender, DoWorkEventArgs e)
        {
           
            int portint = 0;
            IPAddress ipv4 = null;
            try
            {
                IPRead();
                ipv4 =  IPAddress.Parse(ip);
                
            }
            catch
            {
                
                MessageBox.Show("niewłaściwy adres ip");
                conect = false;
                return;
                
            }
            try
            {
                PortRead();
               
                portint = Int32.Parse(port);
                
            }
            catch
            {
                
                MessageBox.Show("niewłaściwy numer portu");
                conect = false;
                return;
                
            }
            
            listener = new TcpListener(ipv4, portint);
            listener.Start();
            
            SetText("Oczekiwanie na połączenie\n");
            try
            {
               

                SetText("Oczekiwanie na połączenie\n");
                klient = listener.AcceptTcpClient();
                NetworkStream ns = klient.GetStream();
                czytanie = new BinaryReader(ns);
                pisanie = new BinaryWriter(ns);
                conect = true;
                SetText(String.Concat("klient o numerz IP :", ((IPEndPoint)klient.Client.RemoteEndPoint).ToString(), "połączył się\n"));
                worker2.RunWorkerAsync();

            }
            catch
            {
                
                SetText("Połączenie zostało przerwane\n");
                conect = false;
            }
            
        }


        private void worker2_DoWork(object sender, DoWorkEventArgs e)
        {
            string wiadomosc;
            try
            {
                while ((wiadomosc = czytanie.ReadString()) != "exit") {
                    SetText(ip + " : " + wiadomosc+ "\n");
                }
                
                klient.Close();
                listener.Stop();
                SetText("Połączenie zostało przerwane\n");
            }
            catch
            {
                conect = false;
                klient.Close();
                listener.Stop();
                SetText("Połączenie zostało przerwane\n");
            }
        }

        private void Message_button_Click(object sender, RoutedEventArgs e)
        {
           
            
            if (conect)
            {
                pisanie.Write(Message.Text);
                SetText("Ja : " + Message.Text+"\n");
                Message.Text = "";
            }
        }
    }
}

