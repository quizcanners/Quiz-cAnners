using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

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
            return number switch
            {
                LargeNumber.Thousand => "K",// 3 
                LargeNumber.Million => "M",// 6    
                LargeNumber.Billion => "B",// 9 
                LargeNumber.Trillion => "t",// 12 
                LargeNumber.Quadrillion => "q",// 15     
                LargeNumber.Quintillion => "Q",// 18  
                LargeNumber.Sextillion => "s",// 21 
                LargeNumber.Septillion => "S",// 24 
                LargeNumber.Octillion => "o",// 27 
                LargeNumber.Nonillion => "n",// 30 
                LargeNumber.Decillion => "d",// 33 
                LargeNumber.Undecillion => "U",// 36 
                LargeNumber.Duodecillion => "D",// 39 
                LargeNumber.Tredecillion => "T",// 42      
                LargeNumber.Quattuordecillion => "Qt",// 45 
                LargeNumber.Quindecillion => "Qd",// 48 
                LargeNumber.Sexdecillion => "Sd",// 51
                LargeNumber.Septendecillion => "St",// 54
                LargeNumber.Octodecillion => "O",// 57
                LargeNumber.Novemdecillion => "N",// 60
                LargeNumber.Vigintillion => "v",// 63
                LargeNumber.Unvigintillion => "c",// 66
                LargeNumber.Duovigintillion => "Dd",// 69
                LargeNumber.Trevigintillion => "Td",// 72   Quad=Q    Quint=I   Sex=S    Sep=P   Oc=O  Non=N  Vigint=V   Dec=D   Unde=U    Duo=D   Tre=T
                LargeNumber.Quattuorvigintillion => "Qv",// 75    vigintillion=v
                LargeNumber.Quinvigintillion => "Iv",// 78
                LargeNumber.Sexvigintillion => "Sv",// 81
                LargeNumber.Septenvigintillion => "Pv",// 84
                LargeNumber.Octovigintillion => "Ov",// 87
                LargeNumber.Novemvigintillion => "Nv",// 90
                LargeNumber.Trigintillion => "Tv",// 93
                LargeNumber.Untrigintillion => "Uv",// 96
                LargeNumber.Duotrigintillion => "Dv",// 99
                LargeNumber.Googol => "Go",// [SPECIAL] 100
                LargeNumber.Tretrigintillion => "TT",// 102  ... trigintillion = T
                LargeNumber.Quattuortrigintillion => "QT",// 105 
                LargeNumber.Quintrigintillion => "IT",// 108
                LargeNumber.Sextrigintillion => "ST",// 111
                LargeNumber.Septentrigintillion => "PT",// 114
                LargeNumber.Octotrigintillion => "OT",// 117
                LargeNumber.Novemtrigintillion => "NT",// 120
                LargeNumber.Centillion => "Ce",// [SPECIAL] 303
                _ => "??",
            };
        }

        private static readonly System.Globalization.CultureInfo provider = new("en-US");

        public static byte[] StringToByteArray(string name) => Encoding.ASCII.GetBytes(name);
        
        public static string ByteArrayToString(byte[] arr)
        {
            int nullIndex = Array.IndexOf(arr, (byte)0);

            if (nullIndex == -1)
            {
                nullIndex = arr.Length;
            }

            return Encoding.UTF8.GetString(arr, 0, nullIndex);
        }

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

#       if UNITY_2021_1_OR_NEWER
            var gotDot = result.Contains('.') || result.Contains(',');
#       else
            var gotDot = result.Contains(".") || result.Contains(",");
#       endif

            if (gotDot)
                maxNumbers += 1;

            if (result.Length > maxNumbers)
            {
                result = result[..maxNumbers];

                if (gotDot)
                    result = result.TrimEnd('.', ',');

                // Now trim zeros in fractional part
                gotDot = result.Contains(".") || result.Contains(",");

                if (gotDot)
                {
                    while (result[^1] == '0')
                    {
                        result = result[..^1];
                    }

                    result = result.TrimEnd('.', ',');
                }

            }

            var finalNumber = "{0}{1}{2}".F(sign, result, unit);
            return finalNumber;
        }

        public static string ToMegabytes(uint bytes)
        {
            bytes >>= 10;
            bytes /= 1024; // On new line to workaround IL2CPP bug
            return "{0} Mb".F(bytes.ToString());
        }

        internal static string ToMegabytes(long bytes)
        {
            bytes >>= 10;
            bytes /= 1024; // On new line to workaround IL2CPP bug
            return "{0} Mb".F(bytes.ToString());
        }

        public static string MetersToReadableString(float distance) 
        {
            if (distance < 2000)
                return "{0} m".F(distance.ToString("F0"));

            float kms = distance / 1000;

            return "{0} km".F(GetKmNumber(distance));

            static string GetKmNumber(float distance) 
            {
                float kms = distance / 1000;

                if (kms > 10)
                    return kms.ToString("F0");

                float fraction = Mathf.Round((kms - Mathf.Floor(kms))*10)/10f;

                if (fraction < 0.1f || fraction>0.9f)
                    return kms.ToString("F0");

                return kms.ToString("F1");
            }
        }

        public static string ToRelativeString(this DateTime date, bool showHours)
        {
            DateTime today = DateTime.Today;

            if (showHours)
            {
                TimeSpan diff = date - DateTime.Now;
                var inTotalHours = diff.TotalHours;

                if (Math.Abs(inTotalHours) < 1)
                    return "Now";

                if (Math.Abs(inTotalHours) < 24)
                {
                    if (inTotalHours > 0)
                    {
                        return "in {0} hours".F(Math.Floor(inTotalHours));
                    }

                    if (inTotalHours < 0)
                    {
                        return "{0} hours ago".F(Math.Floor(-inTotalHours));
                    }
                }
            }

            if (date.Date == today)
                return AddTimeIfneeded("Today");

            if (date.Date == today.AddDays(1))     return AddTimeIfneeded("Tomorrow");
            if (date.Date == today.AddDays(-1))    return AddTimeIfneeded("Yesterday");
            if (date.Date == today.AddDays(2)) return AddTimeIfneeded("After Tomorrow");
            if (date.Date == today.AddDays(-2)) return AddTimeIfneeded("Before Yesterday");

            if (date.Year != DateTime.Now.Year)
                return AddTimeIfneeded(date.ToString("dd-MMMM-yyyy"));

            return AddTimeIfneeded(date.ToString("dd MMMM"));

            string AddTimeIfneeded(string text)
            {
                if (!showHours)
                    return text;

                return "{0} at {1}".F(text, date.ToString("HHmm"));
            }
        }
    }
}
