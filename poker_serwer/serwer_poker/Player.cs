using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace serwer_poker
{
    class Player
    {
        public int PortEnd;
        public IPAddress IPEnd;
        public NetworkStream NS;
        public BinaryReader Reader;
        public BinaryWriter Writer;

        public List<byte> Hand = new List<byte>();
        public List<byte> TieBreaker = new List<byte>();
        public List<byte> TieBreakerColor = new List<byte>();
        public int Chips;
        public bool IsPlaying;//czy połączony
        public bool Fold;
        public bool Check;
        public int Bet;//ile postawił do tej pory
        public int ID;
        public uint ValueOfHand;
        public bool AllIn;

        public void AddCard(byte Card)
        {
            Hand.Add(Card);
        }

        public void ShowCards()
        {
            Console.WriteLine("Ręka gracza " + ID);
            foreach (var Card in Hand)
            {
                Console.WriteLine(Card);
            }
        }
    }
}
