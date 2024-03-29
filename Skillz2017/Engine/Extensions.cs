﻿using Pirates;
using System.Collections.Generic;
using System.Linq;
using MyBot.Engine;

namespace MyBot
{
    static class Extensions
    {
        public static double Power(this int i, double p)
        {
            return System.Math.Pow(i, p);
        }
        public static bool isBetween(this MapObject loc, MapObject bound1, MapObject bound2)
        {
            return isBetween(loc.GetLocation().Row, bound1.GetLocation().Row, bound2.GetLocation().Row) && isBetween(loc.GetLocation().Col, bound1.GetLocation().Col, bound2.GetLocation().Col);
        }
        public static Location Add(this Location loc, int X, int Y)
        {
            return new Location(loc.Row + X, loc.Col + Y);
        }
        private static bool isBetween(int num, int bound1, int bound2)
        {
            if (bound1 > bound2)
                return num >= bound2 && num <= bound1;
            else
                return num >= bound1 && num <= bound2;
        }

        public static Delegates.AircraftScoringFunction Times(this Delegates.AircraftScoringFunction f, Delegates.AircraftScoringFunction other)
        {
            return x => f(x) * other(x);
        }
        public static bool IsEmpty<T>(this IEnumerable<T> source)
        {
            return !source.Any();
        }
        public static T Transform<T>(this T arg, bool Condition, System.Func<T, T> Transformation)
        {
            if (Condition)
                return Transformation(arg);
            else return arg;
        }
        public static T WithSideEffect<T>(this T arg, System.Func<T, object> se)
        {
            se(arg);
            return arg;
        }
        public static T FirstOr<T>(this IEnumerable<T> c, T or)
        {
            if (c.IsEmpty()) return or;
            else return c.First();
        }

        public static string Join(this string[] arr, string delimiter)
        {
            string str = "";
            foreach (string s in arr)
            {
                str += delimiter;
                str += s;
            }
            return str.Substring(1);
        }
    }
}
