using System;
using System.Collections.Generic;

namespace UnhollowerBaseLib
{
    internal class MultiDictionary<S, T, U>
    {
        private readonly Dictionary<S, Dictionary<T, U>> dict = new Dictionary<S, Dictionary<T, U>>();
        /// <summary>Returns true if the dictionary has a value assigned to these keys.</summary>
        public bool ContainsKey(S key1, T key2)
        {
            if (!dict.ContainsKey(key1) || !dict[key1].ContainsKey(key2)) return false;
            else return true;
        }
        /// <summary>Gets the value in the dictionary for these keys.</summary>
        public U Get(S key1, T key2, bool throwOnNotFound)
        {
            if (!dict.ContainsKey(key1) || !dict[key1].ContainsKey(key2))
            {
                if (throwOnNotFound) throw new ArgumentException("Dictionary has no entry for those keys.");
                else return default;
            }
            else return dict[key1][key2];
        }
        /// <summary>Adds this value to the dictionary. Throws an error if a value is already assigned for those keys.</summary>
        public void Add(S key1, T key2, U value) => Add(key1, key2, value, "A value with the same keys already exists in the dictionary.");
        /// <summary>Adds this value to the dictionary. Throws a custom error if a value is already assigned for those keys.</summary>
        public void Add(S key1, T key2, U value, string errorMessage)
        {
            if (dict.ContainsKey(key1))
            {
                if (dict[key1].ContainsKey(key2)) throw new ArgumentException(errorMessage);
                
                else dict[key1].Add(key2, value);
            }
            else
            {
                dict.Add(key1, new Dictionary<T, U>());
                dict[key1].Add(key2, value);
            }
        }
        /// <summary>Sets this value in the dictionary. Adds an entry if it doesn't exist already.</summary>
        public void Set(S key1, T key2, U value)
        {
            if (dict.ContainsKey(key1))
            {
                if (dict[key1].ContainsKey(key2)) dict[key1][key2] = value;

                else dict[key1].Add(key2, value);
            }
            else
            {
                dict.Add(key1, new Dictionary<T, U>());
                dict[key1].Add(key2, value);
            }
        }
    }
    internal class MultiDictionary<S, T, U, V>
    {
        private readonly Dictionary<S, Dictionary<T, Dictionary<U, V>>> dict = new Dictionary<S, Dictionary<T, Dictionary<U, V>>>();
        /// <summary>Returns true if the dictionary has a value assigned to these keys.</summary>
        public bool ContainsKey(S key1, T key2, U key3)
        {
            if (!dict.ContainsKey(key1) || !dict[key1].ContainsKey(key2) || !dict[key1][key2].ContainsKey(key3)) return false;
            else return true;
        }
        /// <summary>Gets the value in the dictionary for these keys.</summary>
        public V Get(S key1, T key2, U key3, bool throwOnNotFound)
        {
            if (!dict.ContainsKey(key1) || !dict[key1].ContainsKey(key2) || !dict[key1][key2].ContainsKey(key3))
            {
                if (throwOnNotFound) throw new ArgumentException("Dictionary has no entry for those keys.");
                else return default;
            }
            else return dict[key1][key2][key3];
        }
        /// <summary>Adds this value to the dictionary. Throws an error if a value is already assigned for those keys.</summary>
        public void Add(S key1, T key2, U key3, V value) => Add(key1, key2, key3, value, "A value with the same keys already exists in the dictionary.");
        /// <summary>Adds this value to the dictionary. Throws a custom error if a value is already assigned for those keys.</summary>
        public void Add(S key1, T key2, U key3, V value, string errorMessage)
        {
            if (dict.ContainsKey(key1))
            {
                if (dict[key1].ContainsKey(key2))
                {
                    if (dict[key1][key2].ContainsKey(key3)) throw new ArgumentException(errorMessage);
                    
                    else dict[key1][key2].Add(key3, value);
                }
                else
                {
                    dict[key1].Add(key2, new Dictionary<U, V>());
                    dict[key1][key2].Add(key3, value);
                }
            }
            else
            {
                dict.Add(key1, new Dictionary<T, Dictionary<U, V>>());
                dict[key1].Add(key2, new Dictionary<U, V>());
                dict[key1][key2].Add(key3, value);
            }
        }
        /// <summary>Sets this value in the dictionary. Adds an entry if it doesn't exist already.</summary>
        public void Set(S key1, T key2, U key3, V value)
        {
            if (dict.ContainsKey(key1))
            {
                if (dict[key1].ContainsKey(key2))
                {
                    if (dict[key1][key2].ContainsKey(key3)) dict[key1][key2][key3] =  value;

                    else dict[key1][key2].Add(key3, value);
                }
                else
                {
                    dict[key1].Add(key2, new Dictionary<U, V>());
                    dict[key1][key2].Add(key3, value);
                }
            }
            else
            {
                dict.Add(key1, new Dictionary<T, Dictionary<U, V>>());
                dict[key1].Add(key2, new Dictionary<U, V>());
                dict[key1][key2].Add(key3, value);
            }
        }
    }
}
