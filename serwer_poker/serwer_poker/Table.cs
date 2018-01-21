using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace serwer_poker
{
    class Table
    {
        public List<byte> CommunityCards = new List<byte>();
        public int Pot;//aktualna pula
        public int Bid;//aktualna stawka

        public void Deal(List<byte> Deck, int NumberOfDealtCrads)
        {
            while (NumberOfDealtCrads > 0)
            {
                CommunityCards.Add(Deck.ElementAt(0));
                Deck.RemoveAt(0);
                NumberOfDealtCrads--;
            }
        }

        public void AddCard(byte Card)
        {
            CommunityCards.Add(Card);
        }

        public void ShowCards()
        {
            Console.WriteLine("Karty na stole:");
            foreach (byte Card in CommunityCards)
            {
                Console.WriteLine(Card);
            }
        }
    }
}
