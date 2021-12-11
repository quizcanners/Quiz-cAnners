using System;
using System.Collections.Generic;

namespace QuizCanners.Utils
{

    public static partial class QcSharp
    {
       

        private static readonly int charA = Convert.ToInt32('a');

        public enum LargeNumber
        {
            Thousand = 1,
            Million = 2,
            Billion = 3,
            Trillion = 4,
            Quadrillion = 5,
            Quintillion = 6,
            Sextillion = 7,
            Septillion = 8,
            Octillion = 9,
            Nonillion = 10,
            Decillion = 11,
            Undecillion = 12,
            Duodecillion = 13,
            Tredecillion = 14,
            Quattuordecillion = 15,
            Quindecillion = 16,
            Sexdecillion = 17,
            Septendecillion = 18,
            Octodecillion = 19,
            Novemdecillion = 20,
            Vigintillion = 21,
            Unvigintillion = 22,
            Duovigintillion = 23,
            Trevigintillion = 24,
            Quattuorvigintillion = 25,
            Quinvigintillion = 26,
            Sexvigintillion = 27,
            Septenvigintillion = 28,
            Octovigintillion = 29,
            Novemvigintillion = 30,
            Trigintillion = 31,
            Untrigintillion = 32,
            Duotrigintillion = 33,
            //Googol ,
            Tretrigintillion = 34,
            Quattuortrigintillion = 35,
            Quintrigintillion = 36,
            Sextrigintillion = 37,
            Septentrigintillion = 38,
            Octotrigintillion = 39,
            Novemtrigintillion = MAX_NUMBER_INDEX,
          

            // Special Number
            Googol = 100,
            Centillion = 303,

        }
        private const int MAX_NUMBER_INDEX = 40;

        //https://simple.wikipedia.org/wiki/Names_for_large_numbers
        //https://crusaders-of-the-lost-idols.fandom.com/wiki/Large_Number_Abbreviations

        public static string GetAbbreviation(this LargeNumber number)
        {
            switch (number)
            {
                case LargeNumber.Thousand: return "K";// 3 
                case LargeNumber.Million: return "M";// 6    
                case LargeNumber.Billion: return "B";// 9 
                case LargeNumber.Trillion: return "t"; // 12 
                case LargeNumber.Quadrillion: return "q"; // 15     
                case LargeNumber.Quintillion: return "Q";// 18  
                case LargeNumber.Sextillion: return "s";// 21 
                case LargeNumber.Septillion: return "S";// 24 
                case LargeNumber.Octillion: return "o";// 27 
                case LargeNumber.Nonillion: return "n";// 30 
                case LargeNumber.Decillion: return "d";// 33 
                case LargeNumber.Undecillion: return "U";// 36 
                case LargeNumber.Duodecillion: return "D";// 39 
                case LargeNumber.Tredecillion: return "T";// 42      
                case LargeNumber.Quattuordecillion: return "Qt";// 45 
                case LargeNumber.Quindecillion: return "Qd";// 48 
                case LargeNumber.Sexdecillion: return "Sd";// 51
                case LargeNumber.Septendecillion: return "St";// 54
                case LargeNumber.Octodecillion: return "O";// 57
                case LargeNumber.Novemdecillion: return "N";// 60
                case LargeNumber.Vigintillion: return "v";// 63
                case LargeNumber.Unvigintillion: return "c";// 66
                case LargeNumber.Duovigintillion: return "Dd";// 69
                case LargeNumber.Trevigintillion: return "Td";// 72   Quad=Q    Quint=I   Sex=S    Sep=P   Oc=O  Non=N  Vigint=V   Dec=D   Unde=U    Duo=D   Tre=T
                case LargeNumber.Quattuorvigintillion: return "Qv";// 75    vigintillion=v
                case LargeNumber.Quinvigintillion: return "Iv";// 78
                case LargeNumber.Sexvigintillion: return "Sv";// 81
                case LargeNumber.Septenvigintillion: return "Pv";// 84
                case LargeNumber.Octovigintillion: return "Ov";// 87
                case LargeNumber.Novemvigintillion: return "Nv";// 90
                case LargeNumber.Trigintillion: return "Tv";// 93
                case LargeNumber.Untrigintillion: return "Uv";// 96
                case LargeNumber.Duotrigintillion: return "Dv";// 99
                case LargeNumber.Googol: return "Go";// [SPECIAL] 100
                case LargeNumber.Tretrigintillion: return "TT";// 102  ... trigintillion = T
                case LargeNumber.Quattuortrigintillion: return "QT";// 105 
                case LargeNumber.Quintrigintillion: return "IT";// 108
                case LargeNumber.Sextrigintillion: return "ST";// 111
                case LargeNumber.Septentrigintillion: return "PT";// 114
                case LargeNumber.Octotrigintillion: return "OT";// 117
                case LargeNumber.Novemtrigintillion: return "NT";// 120
                case LargeNumber.Centillion: return "Ce";// [SPECIAL] 303

                default: return "??";
            }
        }

        private static readonly System.Globalization.CultureInfo provider = new System.Globalization.CultureInfo("en-US");

        public static string ToReadableString(this int value) => ToReadableString((double)value);
    
        public static string ToReadableString(this double value, int maxNumbers = 3)
        {
            string sign = value < 0 ? "-" : "";
            string unit = "";
            value = Math.Abs(value);
            string result;

            string FORMAT = "G" + maxNumbers.ToString();

            if (value < 1000)
            {
                result = value.ToString(FORMAT, provider);
            }
            else
            {
                const int THE_LOG = 1000;
                int n = (int)Math.Log(value, THE_LOG);
                var numeral = (value * 100 / Math.Pow(THE_LOG, n)) / 100;
                result = numeral.ToString(FORMAT, provider);

                if (n > 0)
                {
                    if (n < MAX_NUMBER_INDEX)
                    {
                        var enumNumber = (LargeNumber)n;
                        unit = NonBreakableString + enumNumber.GetAbbreviation();
                    }
                    else
                    {
                        int unitInt = n - MAX_NUMBER_INDEX;

                        const int LETTER_COUNT = 26;

                        char notationA = Convert.ToChar((unitInt / LETTER_COUNT) % (LETTER_COUNT) + charA);
                        char notationB = Convert.ToChar(unitInt % LETTER_COUNT + charA);
                        unit = NonBreakableString + notationA + notationB;
                    }
                }
            }

            var gotDot = result.Contains('.') || result.Contains(',');

            if (gotDot)
                maxNumbers += 1;

            if (result.Length > maxNumbers)
            {
                result = result.Substring(0, maxNumbers);

                if (gotDot)
                    result = result.TrimEnd('.', ',');

                // Now trim zeros in fractional part
                gotDot = result.Contains(".") || result.Contains(",");

                if (gotDot)
                {
                    while (result[result.Length - 1] == '0')
                    {
                        result = result.Substring(0, result.Length - 1);
                    }

                    result = result.TrimEnd('.', ',');
                }

            }

            var finalNumber = "{0}{1}{2}".F(sign, result, unit);
            return finalNumber;
        }
    }
}
