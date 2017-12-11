using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace kontrolaparzystosci
{
    internal class Program
    {
        public static void Main(string[] args)
        {
            long dzielnik = 0b1010000000;
            long warunek = 0;
            long maska = 0b1000000000;
            long bajt = 0b0;
            bool poczatek = true;
            Console.WriteLine("Podaj dane:");
            string dane = Console.ReadLine();
            UTF8Encoding encoding = new UTF8Encoding();
            byte[] buf = encoding.GetBytes(dane);
            StringBuilder binaryStringBuilder = new StringBuilder();
            foreach (var b in buf)
            {
                if (poczatek)//przepisanie przy pierwszym znaku
                {
                    bajt = b;
                    poczatek = false;
                }
                else//wykonanie xor dla każdego następnego bajtu z poprzednimi
                {
                    bajt ^= b;
                }
            }
            bajt <<= 2;//dodanie dwóch zer 
            for (int i = 0; i < 8; i++)//operacja dzielenia
            {
                warunek = maska & bajt;
                if (warunek > 0)
                {
                    bajt ^= dzielnik;
                    maska = maska >> 1;
                    dzielnik = dzielnik >> 1;
                }
                else
                {
                    maska = maska >> 1;
                    dzielnik = dzielnik >> 1;
                }
            }
            Console.WriteLine("CRC: {0}", bajt);
            Console.ReadLine();
        }
    }
}