using System;
using System.Collections.Generic;
using System.Linq;
using AssemblyUnhollower.Extensions;

namespace AssemblyUnhollower.Utils
{
    public class UniquificationContext
    {
        private readonly UnhollowerOptions myUnhollowerOptions;
        private readonly Dictionary<string, int> myUniquifiersCount = new Dictionary<string, int>();
        private readonly SortedSet<(string, float)> myPrefixes = new SortedSet<(string, float)>(new Item2Comparer());

        public UniquificationContext(UnhollowerOptions unhollowerOptions)
        {
            myUnhollowerOptions = unhollowerOptions;
        }

        public bool CheckFull()
        {
            return myUniquifiersCount.Count >= myUnhollowerOptions.TypeDeobfuscationMaxUniquifiers;
        }

        public void Push(string str, bool noSubstring = false)
        {
            if (str.IsInvalidInSource()) return;
            
            var stringPrefix = noSubstring ? str : SubstringBounded(str, 0, myUnhollowerOptions.TypeDeobfuscationCharsPerUniquifier);
            var currentCount = myUniquifiersCount[stringPrefix] = myUniquifiersCount.GetOrCreate(stringPrefix, _ => 0) + 1;
            myPrefixes.Add((stringPrefix, myUniquifiersCount.Count + currentCount * 2 + myPrefixes.Count / 100f));
        }
        
        public void Push(List<string> strings, bool noSubstring = false)
        {
            foreach (var str in strings) 
                Push(str, noSubstring);
        }

        public string GetTop()
        {
            return string.Join("", myPrefixes.Take(myUnhollowerOptions.TypeDeobfuscationMaxUniquifiers).Select(it => it.Item1));
        }

        private class Item2Comparer : IComparer<(string, float)>
        {
            public int Compare((string, float) x, (string, float) y) => x.Item2.CompareTo(y.Item2);
        }
        
        private static string SubstringBounded(string str, int startIndex, int length)
        {
            length = Math.Min(length, str.Length);
            return str.Substring(startIndex, length);
        }
    }
}