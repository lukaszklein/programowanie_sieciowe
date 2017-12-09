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
            long dzielnik = 0b10100000;
            long warunek = 0;
            long maska = 0b10000000;
            long bajt = 0b0;
            bool poczatek = true;
            Console.WriteLine("Podaj dane:");
            string dane = Console.ReadLine();
            UTF8Encoding encoding = new UTF8Encoding();
            byte[] buf = encoding.GetBytes(dane);
            StringBuilder binaryStringBuilder = new StringBuilder();
            foreach (var b in buf)
            {
                if (!poczatek)//przesunięcie przy obecności większej niż 1 liczby znaków
                {
                    bajt <<= 8;//przesuniecie reszty z poprzedniego dzielenia
                    maska <<= 8;
                    dzielnik = 0b101;//ze względu na wypchnięcie jednej z jedynek poza zakres, trzeba ponownie przypisać wartość dzielnika
                    dzielnik <<= 7;
                }
                bajt += b;//przypisanie odczytanego znaku do zmiennej bajt poprzez dodanie jej do obecnej wartości bajta
                if (poczatek)//obliczenie reszty z dzielenia
                {
                    for (int i = 0; i < 6; i++)
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
                    poczatek = false;
                }
                else//dzielenie dla nastepnych znakow
                {
                    for (int i = 0; i < 8; i++)
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
                }
            }
            bajt <<= 2;//dodanie zer na końcu
            maska <<= 2;
            dzielnik = 0b1010;//przypisanie wartosci dzielnika
            for (int i = 0; i < 2; i++)//obliczanie crc
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