using System;
using System.Collections.Generic;
using System.Text;

namespace kontrolaparzystosci
{
    internal class Program
    {
        public static void Main(string[] args)
        {
            int akumulator = 0;
            int temp = 0;
            byte maska = 1;
            Console.WriteLine("Podaj dane:");
            string dane = Console.ReadLine();           
            UTF8Encoding encoding = new UTF8Encoding();
            byte[] buf = encoding.GetBytes(dane);
            StringBuilder binaryStringBuilder = new StringBuilder();
            foreach (var b in buf)
            {
                for (int i = 0; i < 8; i++)
                {
                    maska = (byte) (1 << i);
                    //Console.WriteLine(maska);
                    temp = (byte) (b & maska);
                    //Console.WriteLine(temp);
                    akumulator ^= temp >> i;
                }
            }
            Console.WriteLine(akumulator);

        }
    }
}