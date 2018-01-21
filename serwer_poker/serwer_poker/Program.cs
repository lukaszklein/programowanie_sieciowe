using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;


namespace serwer_poker
{
    class Program
    {

        //static System.Windows.Forms.Timer Timer = new System.Windows.Forms.Timer();


        /*Tworzenie talii złożonej z 52 kart do wykorzystania przy rozgrywce.
         * Karty są zapisywane jako zmienne byte i opisywane w następujący sposób:
         * - 4 najmniej znaczące bity odpowiadają za wartość karty według starszeństwa kart w Texas Hold'em:
         *      0010 - 2, 0011 - 3, 0100 - 4, 0101 - 5, 0110 - 6, 0111 - 7, 1000 - 8, 1001 - 9, 1010 - 10,
         *      1011 - walet(11), 1100 - dama(12), 1101 - król(13), 1110 - as(14)
         * - piąty i szósty bit odpowiadają za kolor karty:
         *      00 - kier(0), 01 - pik(16), 10 - karo(32), 11 - trefl(48)
         * - siódmy i ósmy bit są niewykorzystane
         * Przykład:
         * 6 kier - 00000110, as karo - 00101110
         Przy tworzeniu talii stosowane są liczby w systemie dziesiętnym*/
        static List<byte> CreateDeck()
        {
            byte Card;
            List<byte> TempDeck = new List<byte>();
            for (int i = 0; i <= 48; i+=16)
            {
                for (int j = 2; j <= 14; j++)
                {
                    Card = (byte)(i + j);
                    TempDeck.Add(Card);
                }
            }
            return TempDeck;
        }

        /*Usuwanie z listy graczy nieaktywnych użytkowników*/
        static List<Player> WhoIsPlaying(List<Player> Players)
        {
            Console.WriteLine("kolejnosć graczy");
            foreach (var item in Players)
            {
                Console.WriteLine("gracz " + item.ID);
            }
            bool AllClear = false;
            while (!AllClear)
            {
                foreach (Player Player in Players)
                {
                    if (!Player.IsPlaying)
                    {
                        Players.Remove(Player);
                        break;
                    }
                    else
                    {
                        AllClear = true;
                    }
                }
            }
            return Players;
        }

        /*Rozdawanie NumberOfCards kart z talii Deck*/
        static List<byte> DealCards(List<byte> Deck, int NumberOfCards)
        {
            Random RandomNumber = new Random();
            List<byte> DealtCards = new List<byte>();
            int IndexOfCard;
            byte Card;
            for (int i = 0; i < NumberOfCards; i++)
            {
                IndexOfCard = RandomNumber.Next(0, Deck.Count - 1);
                Card = Deck.ElementAt(IndexOfCard);
                DealtCards.Add(Card);
                Deck.RemoveAt(IndexOfCard);
            }
            return DealtCards;
        }

        /*Zmiana kolejności graczy po rundzie*/
        static List<Player> OrderPlayers(List<Player> ListOfPlayers)
        {
            int NumberOfPlayers = ListOfPlayers.Count();
            Player TempPlayer = ListOfPlayers.ElementAt(0);
            Console.WriteLine("w tempplayer jest gracz " + TempPlayer.ID);
            List<Player> TempList = new List<Player>();
            int Index = 1;
            while(Index < NumberOfPlayers)
            {
                Console.WriteLine("do tymczasowej listy dodaję gracza " + ListOfPlayers.ElementAt(1).ID);
                TempList.Add(ListOfPlayers.ElementAt(Index));
                Index++;
            }
            TempList.Add(TempPlayer);

            return TempList;
        }

        /*Pierwsze rozdanie kart*/
        static void FirstDeal(List<Player> Players, List<byte> Deck)
        {
            int IndexofCard = 0;
            List<byte> TempDeck = DealCards(Deck, Players.Count()*2);
            foreach (Player Player in Players)
            {
                Player.AddCard(TempDeck.ElementAt(IndexofCard));
                SendToOne(TempDeck.ElementAt(IndexofCard).ToString(), Player);
                Console.WriteLine("Wysłano graczowi " + Player.ID + " kartę " + TempDeck.ElementAt(IndexofCard) + " o indeksie " + IndexofCard);
                //Console.ReadKey();
                IndexofCard++;
                
            }
            foreach (Player Player in Players)
            {
                Player.AddCard(TempDeck.ElementAt(IndexofCard));
                SendToOne(TempDeck.ElementAt(IndexofCard).ToString(), Player);
                Console.WriteLine("Wysłano graczowi " + Player.ID + " kartę " + TempDeck.ElementAt(IndexofCard) + " o indeksie " + IndexofCard);
                //Console.ReadKey();
                IndexofCard++;
                
            }
            TempDeck.Clear();
        }

        static void SmallBlind(Player Player, Table Table, List<Player> Players)
        {
            if (Player.Chips < 50)/*wartości stawek do ustalenia*/
            {
                Table.Pot += Player.Chips;
                Player.Bet = Player.Chips;
                Player.Chips = 0;
            }
            else
            {
                Table.Pot += 50;
                Player.Bet = 50;
                Player.Chips -= 50;
            }
            string Message = "coin " + Player.ID + " " + Player.Bet + " " + Player.Chips;
            SendToAll(Message, Players);
        }

        static void BigBlind(Player Player, Table Table, List<Player> Players)
        {
            if (Player.Chips < 100)/*wartości stawek do ustalenia*/
            {
                Table.Pot += Player.Chips;
                Player.Bet = Player.Chips;
                Table.Bid = Player.Chips;//jak bigblind
                Player.Chips = 0;
            }
            else
            {
                Table.Pot += 100;
                Player.Bet = 100;
                Player.Chips -= 100;
                Table.Bid = 100;//jak bigblind
            }
            string Message = "coin " + Player.ID + " " + Player.Bet + " " + Player.Chips;
            SendToAll(Message, Players);
        }

        static void Call(Player Player, Table Table)
        {
            int Difference = Table.Bid - Player.Bet;
            if (Difference > Player.Chips)
            {
                Table.Pot += Player.Chips;
                Player.Bet += Player.Chips;
                Player.Chips = 0;
            }
            else
            {
                Table.Pot += Difference;
                Player.Bet += Difference;
                Player.Chips -= Difference;
            }  
        }

        static void Raise(Player Player, Table Table, int Bid)
        {
            Table.Pot += Bid;
            Player.Bet += Bid;
            Table.Bid += Bid;
            Player.Chips -= Bid;
        }

        public static int IndexOfPlayer;
        /*Pierwsza licytacja obejmująca Blindy*/
        static bool FirstBetting(List<Player> Players, Table Table)
        {
            bool EndOfRound;
            /*Ustalenie rozpoczynającego gracza i przydzielenie blindów*/
            if (Players.Count() == 2)
            {
                SmallBlind(Players.ElementAt(0), Table, Players);
                BigBlind(Players.ElementAt(1), Table, Players);
                IndexOfPlayer = 0;
            }
            else if (Players.Count() == 3)
            {
                SmallBlind(Players.ElementAt(1), Table, Players);
                BigBlind(Players.ElementAt(2), Table, Players);
                IndexOfPlayer = 0;
            }
            else
            {
                SmallBlind(Players.ElementAt(1), Table, Players);
                BigBlind(Players.ElementAt(2), Table, Players);
                IndexOfPlayer = 3;
            }

            EndOfRound = Betting(Players, Table);
            return EndOfRound;
        }

        /*Każda następna licytacja*/
        static bool NextBetting(List<Player> Players, Table Table)
        {
            bool EndOfRound;
            EndOfRound = Betting(Players, Table);
            return EndOfRound;
        }

        /*Metoda licytacji*/
        static bool Betting(List<Player> Players, Table Table)
        {
            int Decision = 0;
            bool ContinueBetting = true;
            bool EndOfRound = false;
            while (ContinueBetting)
            {
                bool Control = true;
                if (!Players.ElementAt(IndexOfPlayer).Fold || !Players.ElementAt(IndexOfPlayer).AllIn)
                {
                    Console.WriteLine("tura gracza " + Players.ElementAt(IndexOfPlayer).ID);
                    Console.WriteLine("aktualna stawka: " + Table.Bid + " żetonów");
                    Console.WriteLine("aktualna pula " + Table.Pot + " żetonów");
                    Console.WriteLine("do tej pory dałeś " + Players.ElementAt(IndexOfPlayer).Bet + " żetonów");
                    Console.WriteLine("masz aktualnie " + Players.ElementAt(IndexOfPlayer).Chips + " żetonów");
                    Console.WriteLine("Stan graczy:");
                    foreach (var Player in Players)
                    {
                        Console.WriteLine("Gracz " + Player.ID);
                        Console.WriteLine("Fold:" + Player.Fold);
                        Console.WriteLine("Check:" + Player.Check);
                    }
                    //Console.ReadKey();
                    /*decyzja gracza zapisana do zmiennej
                     przesyłana jako int:
                     -1 - fold, 0 - call/check, wartość_int - raise, 50000 - all in*/
                    SendToOne("play", Players.ElementAt(IndexOfPlayer));
                    //Console.ReadKey();
                    Console.WriteLine("podejmij decyzję");
                    string decyzja = ReadFromOne(Players.ElementAt(IndexOfPlayer));
                    Decision = int.Parse(decyzja);
                    
                    Console.WriteLine("podjęto decyzję " + Decision);
                    switch (Decision)
                    {
                        case -1:/*fold*/
                            {
                                Console.WriteLine("fold");
                                Players.ElementAt(IndexOfPlayer).Fold = true;
                                break;
                            }
                        case 0:/*call/check*/
                            {
                                if (Table.Bid == Players.ElementAt(IndexOfPlayer).Bet)
                                {
                                    Console.WriteLine("check");
                                    Players.ElementAt(IndexOfPlayer).Check = true;
                                    break;
                                }
                                else
                                {
                                    Console.WriteLine("call");
                                    Call(Players.ElementAt(IndexOfPlayer), Table);
                                    Players.ElementAt(IndexOfPlayer).Check = true;
                                    break;
                                }
                            }
                        case 50000:/*all in - wartość do edycji*/
                            {
                                Console.WriteLine("all in");
                                Call(Players.ElementAt(IndexOfPlayer), Table);
                                Raise(Players.ElementAt(IndexOfPlayer), Table, Players.ElementAt(IndexOfPlayer).Chips);
                                
                                foreach (Player Player in Players)
                                {
                                    if (!Player.Fold)
                                    {
                                        Player.Check = false;
                                    }
                                }
                                Players.ElementAt(IndexOfPlayer).Check = true;
                                Players.ElementAt(IndexOfPlayer).AllIn = true;
                                break;
                            }
                        default:/*call+raise*/
                            {
                                Console.WriteLine("raise");
                                if (Decision > Players.ElementAt(IndexOfPlayer).Chips)
                                {
                                    Call(Players.ElementAt(IndexOfPlayer), Table);
                                    Raise(Players.ElementAt(IndexOfPlayer), Table, Players.ElementAt(IndexOfPlayer).Chips);
                                    foreach (Player Player in Players)
                                    {
                                        if (!Player.Fold)
                                        {
                                            Player.Check = false;
                                        }
                                    }
                                    Players.ElementAt(IndexOfPlayer).Check = true;
                                    break;
                                }
                                else
                                {
                                    Call(Players.ElementAt(IndexOfPlayer), Table);
                                    Raise(Players.ElementAt(IndexOfPlayer), Table, Decision);
                                    foreach (Player Player in Players)
                                    {
                                        if (!Player.Fold)
                                        {
                                            Player.Check = false;
                                        }
                                    }
                                    Players.ElementAt(IndexOfPlayer).Check = true;
                                    break;
                                } 
                            }
                    }
                    string Message = "coin " + Players.ElementAt(IndexOfPlayer).ID + " " + Players.ElementAt(IndexOfPlayer).Bet + " " +
                        Players.ElementAt(IndexOfPlayer).Chips;
                    SendToAll(Message, Players);

                }

                /*Ustalenie następnego gracza do licytacji*/
                if (IndexOfPlayer == Players.Count() - 1)
                {
                    IndexOfPlayer = 0;
                }
                else
                {
                    IndexOfPlayer++;
                }
                int NumberOfChecks = 0;
                
                /*Sprawdzenie czy niefoldujący gracze dokonali checka*/
                foreach (Player Player in Players)
                {
                    if (!Player.Fold)
                    {
                        Control &= Player.Check;
                        NumberOfChecks++;
                    }  
                }
                /*Jeśli jest tylko jeden gracz niefoldujący schekowany, to następuje przejście do końca*/
                if (NumberOfChecks == 1)
                {
                    EndOfRound = true;
                    ContinueBetting = false;
                    return EndOfRound;
                }
                else
                /*Jeśli wszyscy schekowali, to następuje wyjście z licytacji*/
                {
                    ContinueBetting = !Control;
                    EndOfRound = false;
                }
                Console.WriteLine("zmienna continuebetting: " + ContinueBetting);
            }

            /*Przygotowanie do następnej rundy*/
            foreach (Player Player in Players)
            {
                if (!Player.Fold)
                {
                    Player.Check = false;
                }
            }
            return EndOfRound;
        }

        /*Usunięcie z ręki powtarzających się wartości kart na potrzeby ewaluacji*/
        static List<byte> DistinctHand(List<byte> Deck)
        {
            List<byte> DistinctDeck = new List<byte>();
            byte Card;
            for (int Index = 0; Index <= Deck.Count() - 2; Index++)
            {
                if (Deck.ElementAt(Index) != Deck.ElementAt(Index + 1))
                {
                    Card = Deck.ElementAt(Index);
                    DistinctDeck.Add(Card);
                }
            }
            Card = Deck.ElementAt(6);
            if (!DistinctDeck.Contains(Card))
            {
                DistinctDeck.Add(Card);
            }

            return DistinctDeck;
        }

        static void Evaluate(List<Player> Players, List<byte> TableCards)
        {
            foreach (Player Player in Players)
            {
                if (!Player.Fold)
                {
                    Console.WriteLine("Gracz " + Player.ID + " nie sfoldował");
                    List<byte> WholeHand = new List<byte>();
                    WholeHand.AddRange(Player.Hand);
                    WholeHand.AddRange(TableCards);
                    Console.WriteLine("cała ręka");
                    foreach (var card in WholeHand)
                    {
                        Console.WriteLine(card);
                    }
                    byte ValueCard;
                    List<byte> ValueHand = new List<byte>();
                    Console.WriteLine("wartości na ręce");
                    foreach (byte Card in WholeHand)
                    {
                        ValueCard = (byte)(Card % 16);
                        ValueHand.Add(ValueCard);
                        Console.WriteLine(ValueCard);
                    }
                    Player.TieBreaker = ValueHand;
                    Player.TieBreaker.Sort();
                    ValueHand.Sort();

                    bool StraightColor = false;
                    bool Quad = false;
                    bool Full = false;
                    bool Color = false;
                    bool Straight = false;
                    bool Three = false;
                    bool DoublePair = false;
                    bool Pair = false;
                    bool HighCard = false;

                    uint StraightColor1 = 0;
                    uint Quad1 = 0;
                    uint Straight1 = 0;
                    uint Three1 = 0;
                    uint Pair1 = 0;
                    uint Pair2 = 0;
                    uint HighCard1 = 0;



                    /*kolor*/

                    List<byte> Hearts = new List<byte>();
                    List<byte> Spades = new List<byte>();
                    List<byte> Diamonds = new List<byte>();
                    List<byte> Clubs = new List<byte>();
                    List<byte> ColorHand = new List<byte>();
                    uint Color1 = 15000000;

                    foreach (byte Card in WholeHand)
                    {
                        if (Card < 16)
                        {
                            Hearts.Add(Card);
                        }
                        else if (16 <= Card && Card < 32)
                        {
                            Spades.Add(Card);
                        }
                        else if (32 <= Card && Card < 48)
                        {
                            Diamonds.Add(Card);
                        }
                        else if (48 <= Card)
                        {
                            Clubs.Add(Card);
                        }
                    }
                    if (Hearts.Count() >= 5)
                    {
                        byte ColorCard;
                        Color = true;
                        foreach (byte Card in Hearts)
                        {
                            ColorCard = (byte)(Card % 16);
                            ColorHand.Add(ColorCard);
                        }
                        ColorHand.Sort();
                    }
                    else if (Spades.Count >= 5)
                    {
                        byte ColorCard;
                        Color = true;
                        foreach (byte Card in Spades)
                        {
                            ColorCard = (byte)(Card % 16);
                            ColorHand.Add(ColorCard);
                        }
                        ColorHand.Sort();
                    }
                    else if (Diamonds.Count >= 5)
                    {
                        byte ColorCard;
                        Color = true;
                        foreach (byte Card in Diamonds)
                        {
                            ColorCard = (byte)(Card % 16);
                            ColorHand.Add(ColorCard);
                        }
                        ColorHand.Sort();
                    }
                    else if (Clubs.Count >= 5)
                    {
                        byte ColorCard;
                        Color = true;
                        foreach (byte Card in Clubs)
                        {
                            ColorCard = (byte)(Card % 16);
                            ColorHand.Add(ColorCard);
                        }
                        ColorHand.Sort();
                    }
                    if (ColorHand.Count() >= 5)
                    {
                        Console.WriteLine("jest kolor");
                        foreach (var card in ColorHand)
                        {
                            Console.WriteLine(card);
                        }
                    }
                    else
                        Console.WriteLine("nie ma koloru ani pokera");
                    //Console.ReadKey();
                    /*koniec kolor*/

                    /*zapełnianie tiebreakera dla koloru*/
                    if (Color)
                    {
                        Player.TieBreakerColor.AddRange(ColorHand);
                        Player.TieBreakerColor.Sort();
                        foreach (byte Card in ColorHand)
                        {
                            if (Player.TieBreaker.Contains(Card))
                            {
                                Player.TieBreaker.Remove(Card);
                            }
                        }
                    }
                    /*koniec tiebreakera*/


                    /*poker (strit+kolor*/
                    if (Color)
                    {

                        Console.WriteLine("sprawdzam czy jest poker");
                        int DifferenceOneColor = 0;
                        StraightColor1 = 0;
                        List<byte> DistinctColor = ColorHand;
                        DistinctColor.Sort();
                        Console.WriteLine("distinct color");
                        foreach (var item in DistinctColor)
                        {
                            Console.WriteLine(item);
                        }
                        //Console.ReadKey();

                        if (DistinctColor.ElementAt(DistinctColor.Count() - 1) == 14)
                        {
                            Console.WriteLine("jest as");
                            DifferenceOneColor = 0;
                            for (int i = 0; i < 5; i++)
                            {
                                if (DistinctColor.ElementAt(i) == i + 2)
                                {
                                    DifferenceOneColor++;
                                }
                            }

                            if (DifferenceOneColor == 4)
                            {
                                Console.WriteLine("jest poker z asem na początku");
                                StraightColor1 = (uint)DistinctColor.ElementAt(3) * 1000000;
                                StraightColor = true;
                                Player.TieBreaker.Clear();
                                foreach (byte Card in WholeHand)
                                {
                                    ValueCard = (byte)(Card % 16);
                                    Player.TieBreaker.Add(ValueCard);
                                    Console.WriteLine(ValueCard);
                                }
                                for (byte i = 0; i <= 3; i++)
                                {
                                    Player.TieBreaker.Remove(DistinctColor.ElementAt(i));
                                }
                                Player.TieBreaker.Remove(14);
                                Console.WriteLine("Karty na ręce gracza");
                                foreach (var item in ValueHand)
                                {
                                    Console.WriteLine(item);
                                }

                                Console.WriteLine("Karty do tiebrekera");
                                foreach (var item in Player.TieBreaker)
                                {
                                    Console.WriteLine(item);
                                }
                                //Console.ReadKey();
                            }

                        }
                        if (DistinctColor.Count() >= 5)
                        {
                            DifferenceOneColor = 0;
                            Console.WriteLine("co najmniej 5kart");
                            for (byte i = 0; i < 4; i++)
                            {
                                if (DistinctColor.ElementAt(i + 1) - DistinctColor.ElementAt(i) == 1)
                                {
                                    DifferenceOneColor++;
                                }
                            }
                            if (DifferenceOneColor == 4)
                            {
                                Console.WriteLine("jest poker z 5");
                                StraightColor1 = (uint)DistinctColor.ElementAt(4) * 1000000;
                                StraightColor = true;
                                for (byte i = 0; i <= 4; i++)
                                {
                                    Player.TieBreaker.Remove(DistinctColor.ElementAt(i));
                                }
                                Console.WriteLine("Karty na ręce gracza");
                                foreach (var item in ValueHand)
                                {
                                    Console.WriteLine(item);
                                }

                                Console.WriteLine("Karty do tiebrekera");
                                foreach (var item in Player.TieBreaker)
                                {
                                    Console.WriteLine(item);
                                }
                                //Console.ReadKey();

                            }
                        }
                        if (DistinctColor.Count() >= 6)
                        {
                            DifferenceOneColor = 0;
                            Console.WriteLine("jest co najmniej 6 kart");
                            DifferenceOneColor = 0;
                            for (byte i = 1; i < 5; i++)
                            {
                                if (DistinctColor.ElementAt(i + 1) - DistinctColor.ElementAt(i) == 1)
                                {
                                    DifferenceOneColor++;
                                }
                            }
                            if (DifferenceOneColor == 4)
                            {
                                Console.WriteLine("jest poker z 6");
                                StraightColor1 = (uint)DistinctColor.ElementAt(5) * 1000000; ;
                                StraightColor = true;
                                Player.TieBreaker.Clear();
                                foreach (byte Card in WholeHand)
                                {
                                    ValueCard = (byte)(Card % 16);
                                    Player.TieBreaker.Add(ValueCard);
                                    Console.WriteLine(ValueCard);
                                }
                                for (byte i = 1; i <= 5; i++)
                                {
                                    Player.TieBreaker.Remove(DistinctColor.ElementAt(i));
                                }
                                Console.WriteLine("Karty na ręce gracza");
                                foreach (var item in ValueHand)
                                {
                                    Console.WriteLine(item);
                                }

                                Console.WriteLine("Karty do tiebrekera");
                                foreach (var item in Player.TieBreaker)
                                {
                                    Console.WriteLine(item);
                                }
                                //Console.ReadKey();
                            }
                        }
                        if (DistinctColor.Count() >= 7)
                        {
                            DifferenceOneColor = 0;
                            Console.WriteLine("jest co najmniej 7 kart");
                            DifferenceOneColor = 0;
                            for (byte i = 2; i < 6; i++)
                            {
                                if (DistinctColor.ElementAt(i + 1) - DistinctColor.ElementAt(i) == 1)
                                {
                                    DifferenceOneColor++;
                                }
                            }
                            if (DifferenceOneColor == 4)
                            {
                                Console.WriteLine("jest poker z 7");
                                StraightColor1 = (uint)DistinctColor.ElementAt(6) * 1000000; ;
                                StraightColor = true;
                                Player.TieBreaker.Clear();
                                foreach (byte Card in WholeHand)
                                {
                                    ValueCard = (byte)(Card % 16);
                                    Player.TieBreaker.Add(ValueCard);
                                    Console.WriteLine(ValueCard);
                                }
                                for (byte i = 2; i <= 6; i++)
                                {
                                    Player.TieBreaker.Remove(DistinctColor.ElementAt(i));
                                }
                                Console.WriteLine("Karty na ręce gracza");
                                foreach (var item in ValueHand)
                                {
                                    Console.WriteLine(item);
                                }

                                Console.WriteLine("Karty do tiebrekera");
                                foreach (var item in Player.TieBreaker)
                                {
                                    Console.WriteLine(item);
                                }
                                //Console.ReadKey();
                                DifferenceOneColor = 0;
                            }
                        }
                    }
                    //Console.ReadKey();
                    /*koniec poker*/

                    /*kareta*/
                    if (!StraightColor)
                    {
                        Console.WriteLine("nie ma pokera, sprawdzam karetę");
                        Quad1 = 0;
                        uint Value = 0;
                        for (uint Index = 2; Index <= 14; Index++)
                        {
                            if ((ValueHand.LastIndexOf((byte)Index) - ValueHand.IndexOf((byte)Index) + 1) == 4)
                            {
                                Console.WriteLine("jest kareta");
                                Value = Index * 100000000;
                                Quad1 = Value;
                                Quad = true;
                                Player.TieBreaker = ValueHand;
                                Player.TieBreaker.RemoveAll((Card => Card == Index));
                            }
                        }
                        /*koniec karety*/
                        /*full*/
                        if (!Quad)
                        {
                            Console.WriteLine("nie ma karety, sprawdzam fulla");
                            Three1 = 0;
                            Pair1 = 0;
                            for (uint Index = 2; Index <= 14; Index++)
                            {
                                if ((ValueHand.LastIndexOf((byte)Index) - ValueHand.IndexOf((byte)Index) + 1) == 3)
                                {
                                    Console.WriteLine("znaleziono trójkę do fula: " + Index);
                                    Three1 = Index;
                                    Three = true;
                                }
                                if ((ValueHand.LastIndexOf((byte)Index) - ValueHand.IndexOf((byte)Index) + 1) == 2)
                                {
                                    Console.WriteLine("znaleziono dwójkę do fula: " + Index);
                                    Pair1 = Index;
                                    Pair = true;
                                }
                            }
                            if (Pair && Three)
                            {
                                Console.WriteLine("jest full");
                                Player.TieBreaker = ValueHand;
                                Player.TieBreaker.RemoveAll((Card => Card == Three1));
                                Console.WriteLine("usunięto z tiebreakera: " + Three1);
                                foreach (var card in Player.TieBreaker)
                                {
                                    Console.WriteLine(card);
                                }
                                Player.TieBreaker.RemoveAll((Card => Card == Pair1));
                                Console.WriteLine("usunięto z tiebreakera: " + Pair1);
                                foreach (var card in Player.TieBreaker)
                                {
                                    Console.WriteLine(card);
                                }
                                Three1 *= 100000;
                                Pair1 *= 1000;
                                Console.WriteLine("Wartości punktowe trójki: " + Three1 + " i pary: " + Pair1);
                                Full = true;
                            }
                            /*koniec full*/
                            /*strit*/
                            if (!Full && !Color)
                            {
                                int DifferenceOne = 0;
                                Straight1 = 0;
                                List<byte> StraightHand = DistinctHand(ValueHand);
                                Console.WriteLine("pozbycie się duplikatów wartości");
                                foreach (var item in StraightHand)
                                {
                                    Console.WriteLine(item);
                                }
                                //Console.ReadKey();

                                if (StraightHand.ElementAt(StraightHand.Count() - 1) == 14)
                                {
                                    DifferenceOne = 0;
                                    for (int i = 0; i < 5; i++)
                                    {
                                        if (StraightHand.ElementAt(i) == i + 2)
                                        {
                                            DifferenceOne++;
                                        }
                                    }

                                    if (DifferenceOne == 4)
                                    {
                                        Console.WriteLine("jest strit z asem na początku");
                                        Straight1 = (uint)StraightHand.ElementAt(3)*1000000;
                                        Straight = true;
                                        Player.TieBreaker.Clear();
                                        foreach (byte Card in WholeHand)
                                        {
                                            ValueCard = (byte)(Card % 16);
                                            Player.TieBreaker.Add(ValueCard);
                                            Console.WriteLine(ValueCard);
                                        }
                                        for (byte i = 0; i <= 3; i++)
                                        {
                                            Player.TieBreaker.Remove(StraightHand.ElementAt(i));
                                        }
                                        Player.TieBreaker.Remove(14);
                                        Console.WriteLine("Karty na ręce gracza");
                                        foreach (var item in ValueHand)
                                        {
                                            Console.WriteLine(item);
                                        }

                                        Console.WriteLine("Karty do tiebrekera");
                                        foreach (var item in Player.TieBreaker)
                                        {
                                            Console.WriteLine(item);
                                        }
                                        //Console.ReadKey();
                                    }
                                }
                                if (StraightHand.Count() >= 5)
                                {
                                    for (byte i = 0; i < 4; i++)
                                    {
                                        if (StraightHand.ElementAt(i + 1) - StraightHand.ElementAt(i) == 1)
                                        {
                                            DifferenceOne++;
                                        }
                                    }
                                    if (DifferenceOne == 4)
                                    {
                                        Console.WriteLine("jest strit z 5");
                                        Straight1 = (uint)StraightHand.ElementAt(4) * 1000000;
                                        Straight = true;
                                        for (byte i = 0; i <= 4; i++)
                                        {
                                            Player.TieBreaker.Remove(StraightHand.ElementAt(i));
                                        }
                                        Console.WriteLine("Karty na ręce gracza");
                                        foreach (var item in ValueHand)
                                        {
                                            Console.WriteLine(item);
                                        }

                                        Console.WriteLine("Karty do tiebrekera");
                                        foreach (var item in Player.TieBreaker)
                                        {
                                            Console.WriteLine(item);
                                        }
                                        //Console.ReadKey();

                                    }
                                }
                                if (StraightHand.Count() >= 6)
                                {
                                    DifferenceOne = 0;
                                    for (byte i = 1; i < 5; i++)
                                    {
                                        if (StraightHand.ElementAt(i + 1) - StraightHand.ElementAt(i) == 1)
                                        {
                                            DifferenceOne++;
                                        }
                                    }
                                    if (DifferenceOne == 4)
                                    {
                                        Console.WriteLine("jest strit z 6");
                                        Straight1 = (uint)StraightHand.ElementAt(5) * 1000000; ;
                                        Straight = true;
                                        Player.TieBreaker.Clear();
                                        foreach (byte Card in WholeHand)
                                        {
                                            ValueCard = (byte)(Card % 16);
                                            Player.TieBreaker.Add(ValueCard);
                                            Console.WriteLine(ValueCard);
                                        }
                                        for (byte i = 1; i <= 5; i++)
                                        {
                                            Player.TieBreaker.Remove(StraightHand.ElementAt(i));
                                        }
                                        Console.WriteLine("Karty na ręce gracza");
                                        foreach (var item in ValueHand)
                                        {
                                            Console.WriteLine(item);
                                        }

                                        Console.WriteLine("Karty do tiebrekera");
                                        foreach (var item in Player.TieBreaker)
                                        {
                                            Console.WriteLine(item);
                                        }
                                        //Console.ReadKey();
                                    }
                                }
                                if (StraightHand.Count() >= 7)
                                {
                                    DifferenceOne = 0;
                                    for (byte i = 2; i < 6; i++)
                                    {
                                        if (StraightHand.ElementAt(i + 1) - StraightHand.ElementAt(i) == 1)
                                        {
                                            DifferenceOne++;
                                        }
                                    }
                                    if (DifferenceOne == 4)
                                    {
                                        Console.WriteLine("jest strit z 7");
                                        Straight1 = (uint)StraightHand.ElementAt(6) * 1000000; ;
                                        Straight = true;
                                        Player.TieBreaker.Clear();
                                        foreach (byte Card in WholeHand)
                                        {
                                            ValueCard = (byte)(Card % 16);
                                            Player.TieBreaker.Add(ValueCard);
                                            Console.WriteLine(ValueCard);
                                        }
                                        for (byte i = 2; i <= 6; i++)
                                        {
                                            Player.TieBreaker.Remove(StraightHand.ElementAt(i));
                                        }
                                        Console.WriteLine("Karty na ręce gracza");
                                        foreach (var item in ValueHand)
                                        {
                                            Console.WriteLine(item);
                                        }

                                        Console.WriteLine("Karty do tiebrekera");
                                        foreach (var item in Player.TieBreaker)
                                        {
                                            Console.WriteLine(item);
                                        }
                                        //Console.ReadKey();
                                    }
                                }
                                /*koniec strita*/
                                /*trójka*/
                                if (!Straight)
                                {
                                    Console.WriteLine("nie ma strita, sprawdzam trójki");
                                    Three1 = 0;
                                    Three = false;
                                    for (uint Index = 2; Index <= 14; Index++)
                                    {
                                        if ((ValueHand.LastIndexOf((byte)Index) - ValueHand.IndexOf((byte)Index) + 1) == 3)
                                        {
                                            Three1 = Index;
                                            Three = true;
                                        }
                                    }
                                    if (Three)
                                    {
                                        Console.WriteLine("jest trójka");
                                        Player.TieBreaker = ValueHand;
                                        Player.TieBreaker.RemoveAll((Card => Card == Three1));
                                        Three1 *= 100000;
                                    }
                                    /*koniec trójki*/

                                    /*dwie pary i para*/
                                    if (!Three)
                                    {
                                        Console.WriteLine("nie ma trójki, sprawdzam pary");
                                        Pair = false;
                                        Pair1 = 0;
                                        Pair2 = 0;
                                        Value = 0;
                                        Console.WriteLine("valuehand");
                                        foreach (var card in ValueHand)
                                        {
                                            Console.WriteLine(card);
                                        }
                                        for (uint Index = 2; Index <= 14; Index++)
                                        {
                                            if ((ValueHand.LastIndexOf((byte)Index) - ValueHand.IndexOf((byte)Index) + 1) == 2)
                                            {
                                                if (Pair1 == 0)
                                                {
                                                    Pair1 = Index;
                                                    Pair = true;
                                                }
                                                else
                                                {
                                                    Pair2 = Pair1;
                                                    Pair1 = Index;
                                                    Pair = false;
                                                    DoublePair = true;
                                                }

                                            }
                                        }

                                        if (DoublePair)
                                        {
                                            Console.WriteLine("są dwie pary");
                                            Player.TieBreaker = ValueHand;
                                            Console.WriteLine("baza kart");
                                            foreach (var card in Player.TieBreaker)
                                            {
                                                Console.WriteLine(card);
                                            }
                                            Player.TieBreaker.RemoveAll((Card => Card == Pair1));
                                            Console.WriteLine("usunięto karty: " + Pair1);
                                            foreach (var card in Player.TieBreaker)
                                            {
                                                Console.WriteLine(card);
                                            }
                                            Player.TieBreaker.RemoveAll((Card => Card == Pair2));
                                            Console.WriteLine("usunięto karty: " + Pair2);
                                            foreach (var card in Player.TieBreaker)
                                            {
                                                Console.WriteLine(card);
                                            }
                                            Pair1 *= 1000;
                                            Pair2 *= 10;
                                        }

                                        if (Pair && !DoublePair)
                                        {
                                            Console.WriteLine("jest jedna para");
                                            Player.TieBreaker = ValueHand;
                                            Player.TieBreaker.RemoveAll((Card => Card == Pair1));
                                            Pair1 *= 10;
                                        }
                                        /*koniec par*/

                                        /*najwyższa karta*/
                                        if (!DoublePair && !Pair)
                                        {
                                            Console.WriteLine("do sprawdzenia zostały tylko najwyższe karty");
                                            int NumberOfCards = ValueHand.Count();
                                            HighCard1 = ValueHand.ElementAt(NumberOfCards - 1);
                                            HighCard = true;
                                            Player.TieBreaker = ValueHand;
                                            Player.TieBreaker.RemoveAll((Card => Card == HighCard1));
                                        }


                                    }
                                }
                            }
                        }
                    }

                    if (StraightColor)
                    {
                        Console.WriteLine("punkty za pokera");
                        Player.ValueOfHand = (StraightColor1 + Color1) * 100;
                    }
                    else if (Quad)
                    {
                        Console.WriteLine("punkty za karetę");
                        Player.ValueOfHand = Quad1;
                    }
                    else if (Full)
                    {
                        Console.WriteLine("punkty za fulla");
                        Player.ValueOfHand = (Three1 + Pair1) * 100;
                    }
                    else if (Color)
                    {
                        Console.WriteLine("punkty za kolor");
                        Player.ValueOfHand = Color1;
                    }
                    else if (Straight)
                    {
                        Console.WriteLine("punkty za strita");
                        Player.ValueOfHand = Straight1;
                    }
                    else if (Three)
                    {
                        Console.WriteLine("punkty za trójkę");
                        Player.ValueOfHand = Three1;
                    }
                    else if (DoublePair)
                    {
                        Console.WriteLine("punkty za dwie pary");
                        Player.ValueOfHand = Pair1 + Pair2;
                    }
                    else if (Pair)
                    {
                        Console.WriteLine("punkty za parę");
                        Player.ValueOfHand = Pair1;
                    }
                    else if (HighCard)
                    {
                        Console.WriteLine("punkty za najwyższą kartę");
                        Player.ValueOfHand = HighCard1;
                    }
                }
                Console.WriteLine("Gracz: " + Player.ID + " ma rękę wartą " + Player.ValueOfHand);
                //Console.ReadKey();
                Player.TieBreaker.Sort();
                Console.WriteLine("Ręka do tiebrekera");
                foreach (var card in Player.TieBreaker)
                {
                    Console.WriteLine(card);
                }
            }
        }

        static void WhoWon(List<Player> Players, Table Table)
        {
            uint Max = 0;
            int Who = 0;
            List<Player> SameValue = new List<Player>();
            foreach (Player Player in Players)
            {
                if (!Player.Fold)
                {
                    Max = Players.ElementAt(0).ValueOfHand;
                    Who = Players.ElementAt(0).ID;
                    Console.WriteLine("obecny max " + Max + "obecny gracz " + Who);
                    break;
                }
            }

            foreach (Player Player in Players)
            {
                if (Player.ValueOfHand > Max)
                {
                    Max = Player.ValueOfHand;
                    Who = Player.ID;
                    Console.WriteLine("zmiana maxa na " + Max + "obecny gracz " + Who);
                    SameValue.Clear();
                    SameValue.Add(Player);
                }
                else if (Player.ValueOfHand == Max)
                {
                    SameValue.Add(Player);
                    Console.WriteLine("remis, dodaję gracza " + Player.ID + " do samevalue");
                }
            }

            if (SameValue.Count() <= 1)
            {
                Console.WriteLine("Wygrał gracz " + Who);
                string Message = "koniec " + Who;
                SendToAll(Message, Players);
                SameValue.ElementAt(0).Chips += Table.Pot;
                Message = "coin " + Who + " 0 " + SameValue.ElementAt(0).Chips;
                SendToAll(Message, Players);
                //Console.ReadKey();
            }
            else
            {
                Console.WriteLine("Potzrebny tiebreak ");
                TieBreaker(SameValue, Max, 1, Table, Players);
                //Console.ReadKey();
            }
        }

        static void TieBreaker(List<Player> TiedPlayers, uint TiedValue, int Cards, Table Table, List<Player> Players)
        {
            Console.WriteLine("tiebreaker nr " + Cards);
            uint Max = 0;
            int Who = 0;
            List<Player> SameValue = new List<Player>();
            int LastItem;
            if (TiedValue != 15000000)
            {
                LastItem = TiedPlayers.ElementAt(0).TieBreaker.Count() - Cards;
                if (LastItem < 0)
                {
                    /*remis nie do rozstrzygnięcia*/
                    int NumberOfPlayers = TiedPlayers.Count();
                    Console.WriteLine("Koniec kart w tiebreakerze. Remis między graczami: ");
                    string Message1 = "remis";
                    foreach (Player Player in TiedPlayers)
                    {
                        Console.WriteLine(Player.ID);
                        Message1 += " " + Player.ID;
                        Player.Chips += Table.Pot / NumberOfPlayers;
                        string Message2 = "coin " + Player.ID + " 0 " + Player.Chips;
                        SendToAll(Message2, Players);
                    }
                    SendToAll(Message1, Players);
                }
                else
                {
                    Max = TiedPlayers.ElementAt(0).TieBreaker.ElementAt(LastItem);
                    Who = TiedPlayers.ElementAt(0).ID;
                    Console.WriteLine("obecny max " + Max + "obecny gracz " + Who);

                    foreach (Player Player in TiedPlayers)
                    {
                        LastItem = Player.TieBreaker.Count() - Cards;
                        if (Player.TieBreaker.ElementAt(LastItem) > Max)
                        {
                            Max = Player.TieBreaker.ElementAt(LastItem);
                            Who = Player.ID;
                            Console.WriteLine("zmiana maxa na " + Max + "obecny gracz " + Who);
                        }
                        else if (Player.TieBreaker.ElementAt(LastItem) == Max)
                        {
                            SameValue.Add(Player);
                            Console.WriteLine("remis w tiebreakerze, dodaję gracza " + Player.ID + " do samevalue");
                        }
                    }

                    if (SameValue.Count() <= 1)
                    {
                        Console.WriteLine("Koniec tiebreakera. Wygrał gracz " + Who);
                        string Message = "koniec " + Who;
                        SendToAll(Message, Players);
                        SameValue.ElementAt(0).Chips += Table.Pot;
                        Message = "coin " + Who + " 0 " + SameValue.ElementAt(0).Chips;
                        SendToAll(Message, Players);
                        //Console.ReadKey();
                    }
                    else
                    {
                        Console.WriteLine("potrzebny dalszy tiebreker");
                        //Console.ReadKey();
                        Cards++;
                        TieBreaker(SameValue, TiedValue, Cards, Table, Players);
                    }
                    
                }
            }
            else
            {
                LastItem = TiedPlayers.ElementAt(0).TieBreakerColor.Count() - Cards;
                if (LastItem < 0)
                {
                    /*remis nie do rozstrzygnięcia*/
                    int NumberOfPlayers = TiedPlayers.Count();
                    Console.WriteLine("Remis między graczami: ");
                    string Message1 = "remis";
                    foreach (Player Player in TiedPlayers)
                    {
                        Console.WriteLine(Player.ID);
                        Message1 += " " + Player.ID;                        
                        Player.Chips += Table.Pot / NumberOfPlayers;
                        string Message2 = "coin " + Player.ID + " 0 " + Player.Chips;
                        SendToAll(Message2, Players);
                    }
                    SendToAll(Message1, Players);
                    //Console.ReadKey();
                }
                else
                {
                    Max = TiedPlayers.ElementAt(0).TieBreakerColor.ElementAt(LastItem);
                    Who = TiedPlayers.ElementAt(0).ID;
                    Console.WriteLine("obecny max " + Max + "obecny gracz " + Who);

                    foreach (Player Player in TiedPlayers)
                    {
                        LastItem = Player.TieBreakerColor.Count() - Cards;
                        if (Player.TieBreakerColor.ElementAt(LastItem) > Max)
                        {
                            Max = Player.TieBreakerColor.ElementAt(LastItem);
                            Who = Player.ID;
                            Console.WriteLine("zmiana maxa na " + Max + "obecny gracz " + Who);
                        }
                        else if (Player.TieBreakerColor.ElementAt(LastItem) == Max)
                        {
                            SameValue.Add(Player);
                            Console.WriteLine("remis w tiebreakerze, dodaję gracza " + Player.ID + " do samevalue");
                        }
                    }

                    if (SameValue.Count() <= 1)
                    {
                        Console.WriteLine("Koniec tiebreakera. Wygrał gracz " + Who);
                        string Message = "koniec " + Who;
                        SendToAll(Message, Players);
                        SameValue.ElementAt(0).Chips += Table.Pot;
                        Message = "coin " + Who + " 0 " + SameValue.ElementAt(0).Chips;
                        SendToAll(Message, Players);
                        //Console.ReadKey();
                    }
                    else
                    {
                        Console.WriteLine("potrzebny kolejny tiebreaker");
                        Cards++;
                        TieBreaker(SameValue, Max, Cards, Table, Players);
                    }
                    
                }
            }

        }

        static List<Player> PreperationForNextRound(List<Player> Players, Table Table)
        {
            Table.Bid = 0;
            Table.CommunityCards.Clear();
            Table.Pot = 0;
            Console.WriteLine("przesyłam karty do wglądu");
            ShowHands(Players);

            foreach (Player Player in Players)
            {                
                Player.Hand.Clear();
                Player.TieBreaker.Clear();
                Player.TieBreakerColor.Clear();
                Player.Fold = false;
                Player.Check = false;
                Player.AllIn = false;
                Player.Bet = 0;
                Player.ValueOfHand = 0;                
                
                if (Player.Chips==0)
                {
                    Player.IsPlaying = false;
                }
            }

            Console.WriteLine("przesyłam nowy stan żetonów");
            foreach (Player Player in Players)
            {                
                string Message = "coin " + Player.ID + " " + Player.Bet + " " + Player.Chips;
                SendToAll(Message, Players);
            }

            Console.WriteLine("Kolejność graczy w poprzedniej rundzie");
            foreach (var item in Players)
            {
                Console.WriteLine("Gracz " + item.ID);
            }
            Players = OrderPlayers(Players);
            Players = WhoIsPlaying(Players);
            Console.WriteLine("Nowa kolejność graczy");
            foreach (var item in Players)
            {
                Console.WriteLine("Gracz " + item.ID);
            }
            //Console.ReadKey();
            return Players;
        }

        private static TcpListener server;
        private static TcpClient tcpClient;   
        delegate void WriteCallBack(string tekst);

        static List<Player> Communication()
        {
            IPAddress ip = null;
            int port = 0;
            // deklarecja adresu IP
            while (ip == null)
            {
                Console.WriteLine("Podaj adres IP sewera");
                try
                {
                    //ip = IPAddress.Parse(Console.ReadLine());
                    ip = IPAddress.Parse("25.93.171.67");
                }
                catch
                {
                    Console.WriteLine("Niewłaściwy sposób podania typ powinien być 127.0.0.1");
                }
            }
            Console.WriteLine(ip);
            //deklaracja portu
            while (port == 0)
            {
                Console.WriteLine("Podaj numer portu serwera");
                try
                {
                    //port = Int32.Parse(Console.ReadLine());
                    port = Int32.Parse("8000");
                }
                catch
                {
                    Console.WriteLine("Niewłaściwy sposób podania liczba powina być całkowita");
                }
                Console.WriteLine(port);
            }
            server = new TcpListener(ip, port);
            server.Start();
            Console.WriteLine("Serwer oczekuje na połączenia ...");
            List<Player> AllPlayers = new List<Player>();
            int NumerClient = 0;//zmienna do inkrementacji
            //oczekiwanie na klientów
            while (NumerClient < 4)
            {
                int NumberOfClients = AllPlayers.Count();
                tcpClient = server.AcceptTcpClient();

                if (NumberOfClients == 0)
                {
                    Player Player1 = new Player();
                    AllPlayers.Add(Player1);
                }
                else if (NumberOfClients == 1)
                {
                    Player Player2 = new Player();
                    AllPlayers.Add(Player2);
                }
                else if (NumberOfClients == 2)
                {
                    Player Player3 = new Player();
                    AllPlayers.Add(Player3);
                }
                else
                {
                    Player Player4 = new Player();
                    AllPlayers.Add(Player4);
                }
                Console.WriteLine("port: " + ((IPEndPoint)tcpClient.Client.RemoteEndPoint).Port);
                Console.WriteLine("ip: " + ((IPEndPoint)tcpClient.Client.RemoteEndPoint).Address);
                AllPlayers.ElementAt(NumerClient).PortEnd = ((IPEndPoint)tcpClient.Client.RemoteEndPoint).Port;
                AllPlayers.ElementAt(NumerClient).IPEnd = ((IPEndPoint)tcpClient.Client.RemoteEndPoint).Address;
                AllPlayers.ElementAt(NumerClient).NS = tcpClient.GetStream();
                AllPlayers.ElementAt(NumerClient).Reader = new BinaryReader(AllPlayers.ElementAt(NumerClient).NS);
                AllPlayers.ElementAt(NumerClient).Writer = new BinaryWriter(AllPlayers.ElementAt(NumerClient).NS);
                AllPlayers.ElementAt(NumerClient).IsPlaying = true;
                AllPlayers.ElementAt(NumerClient).ID = NumerClient + 1;
                AllPlayers.ElementAt(NumerClient).Chips = 10000;
                AllPlayers.ElementAt(NumerClient).Fold = false;
                AllPlayers.ElementAt(NumerClient).Check = false;
                AllPlayers.ElementAt(NumerClient).AllIn = false;
                AllPlayers.ElementAt(NumerClient).Bet = 0;
                Console.WriteLine("Połączono klienta numer: " + (NumerClient + 1));
                string Message = "coin " + AllPlayers.ElementAt(NumerClient).ID + " " + AllPlayers.ElementAt(NumerClient).Bet + " "
                    + AllPlayers.ElementAt(NumerClient).Chips;
                SendToAll(Message, AllPlayers);
                NumerClient++;
            }
            return AllPlayers;
        }  

        static void SendingCards(List<Player> Players)
        {
            foreach  (Player Player in Players)
            {
                string Message = "card " + Player.ID;
                foreach (byte Card in Player.Hand)
                {
                    Message += " " + Card;
                }
                SendToOne(Message, Player);
            }
        }

        static void SendingCards(List<byte> TableCards, List<Player> Players)
        {        
            string Message = "card C1";
            for (int i = 0; i < 3; i++)
            {
                Console.WriteLine("dodaję do wiadomości kartę z indeksu " + i + " o wartości " + TableCards.ElementAt(i));
                Message += " " + TableCards.ElementAt(i);
            }            
            SendToAll(Message, Players);
        }
        
        // wysłanie wiadomości do wszystkich
        public static void SendToAll(string message, List<Player> Players)
        {
            foreach (Player Player in Players)
            {
                try               
                {
                    Console.WriteLine(message);
                    Player.Writer.Write(message);
                }
                catch
                {
                    Console.WriteLine("Bład wysyłania do gracza " + Player.ID);
                }
                
            }
        }

        // wysyłanie wiadomości do jednego 
        public static void SendToOne(string message, Player Player)
        {
            try
            {
                Console.WriteLine(message);
                Player.Writer.Write(message);
            }
            catch
            {
                Console.WriteLine("Bład wysyłania do gracza " + Player.ID);
            }
        }

        //odebranie wiadomości
        public static string ReadFromOne(Player Player)
        {
            string Response;
            Response = Player.Reader.ReadString();
            Console.WriteLine("Wiadomość od gracza" + Player.ID + ": " + Response);
            return Response;
        }

        public static void ShowHands(List<Player> Players)
        {
            string Message = "";
            foreach  (Player Player in Players)
            {
                Message = "card " + Player.ID;
                foreach (byte Card in Player.Hand)
                {
                    Message += " " + Card;
                }
                SendToAll(Message, Players);
            }
            Thread.Sleep(30000);
        }
        //static void TimeEventProcessor(object MyObject, EventArgs MyEventArg)
        //{
            
        //}

        static void Main(string[] args)
        {
            bool EndOfRound = false;
            string Message = "";
            List<Player> AllPlayers = Communication();
            List<Player> EndPlayers = AllPlayers;
            foreach (Player Player in AllPlayers)
            {
                Console.WriteLine("Gracz " + Player.ID + " gotowy");
            }            
            Table Table = new Table { Pot = 0, Bid = 0 };            
            AllPlayers = WhoIsPlaying(AllPlayers);//usuwanie z listy graczy niegrających
            while (AllPlayers.Count() >= 2)
            {
                Console.WriteLine("nowe rozdanie");
                foreach (Player Player in AllPlayers)
                {
                    Message = "start " + Player.ID;
                    SendToOne(Message, Player);
                    Message = "coin " + Player.ID + " " + Player.Bet + " " + Player.Chips;
                    SendToAll(Message, AllPlayers);
                }
                foreach (Player Player in AllPlayers)
                {
                    Console.WriteLine("Gracz " + Player.ID + " gra");
                }
                List<byte> DeckToPlay = CreateDeck();//Przypisanie talii do nowej zmiennej, która będzie modyfikowana
                Console.WriteLine("Rozdanie kart graczom");
                FirstDeal(AllPlayers, DeckToPlay);
                SendingCards(AllPlayers);
                foreach (Player Player in AllPlayers)
                {
                    Player.ShowCards();
                }
                List<byte> CardsOnTable = DealCards(DeckToPlay, 5);                
                Console.WriteLine("Licytacja 1");
                //Timer.Tick += new EventHandler(TimeEventProcessor);
                EndOfRound = FirstBetting(AllPlayers, Table);
                if (!EndOfRound)
                {
                    Console.WriteLine("Pierwsze rozdanie kart na stół");
                    Table.Deal(CardsOnTable, 3);
                    Table.ShowCards();
                    SendingCards(Table.CommunityCards, AllPlayers);
                    Console.WriteLine("Licytacja 2");
                    EndOfRound = NextBetting(AllPlayers, Table);
                    if (!EndOfRound)
                    {
                        Console.WriteLine("Drugie rozdanie kart na stół");
                        Table.Deal(CardsOnTable, 1);
                        Table.ShowCards();
                        Message = "card C2 " + Table.CommunityCards.ElementAt(3);
                        SendToAll(Message, AllPlayers);
                        Console.WriteLine("Licytacja 3");
                        EndOfRound = NextBetting(AllPlayers, Table);
                        if (!EndOfRound)
                        {
                            Console.WriteLine("Trzecie i ostatnie rozdanie kart na stół");
                            Table.Deal(CardsOnTable, 1);
                            Console.WriteLine("Kart w talii: " + DeckToPlay.Count());
                            Table.ShowCards();
                            Message = "card C3 " + Table.CommunityCards.ElementAt(4);
                            SendToAll(Message, AllPlayers);
                            Console.WriteLine("Licytacja 4");
                            EndOfRound = NextBetting(AllPlayers, Table);
                            if (!EndOfRound)
                            {
                                //Console.ReadKey();
                                Evaluate(AllPlayers, Table.CommunityCards);
                                //Console.ReadKey();
                                WhoWon(AllPlayers, Table);
                            }
                        }  
                    } 
                }
                if(EndOfRound)
                {
                    Console.WriteLine("Wszyscy sfoldowali");
                    foreach (Player Player  in AllPlayers)
                    {
                        if (!Player.Fold)
                        {
                            Console.WriteLine("Wygrał gracz " + Player.ID);
                            Message = "koniec " + Player.ID;
                            SendToAll(Message, AllPlayers);
                            Player.Chips += Table.Pot;
                            //Console.ReadKey();
                        }
                    }
                }

                AllPlayers = PreperationForNextRound(AllPlayers, Table);
                //Console.ReadKey();
            }
            Message = "wygrana " + AllPlayers.ElementAt(0).ID;
            SendToAll(Message, EndPlayers);
            Console.ReadKey();
        }
    }
}
