using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace TradingBot.TheAlphaEngine.TradingAlgorithms.AlphaEngine
{
    public class NetworkState : IEnumerable<bool>
    {
        private BitArray bitArray;
        private int dimension;

        public NetworkState(int dimension, int state)
        {
            this.dimension = dimension;

            bitArray = new BitArray(dimension);

            var allBits = new BitArray(new[] { state });

            for (int i = 0; i < dimension; i++)
            {
                bitArray[i] = allBits[i];
            }
        }

        public NetworkState(params bool[] bools)
        {
            bitArray = new BitArray(bools);
            dimension = bitArray.Length;
        }

        public NetworkState(IEnumerable<AlgorithmMode> modes)
        {
            bitArray = new BitArray(modes.Select(x => x == AlgorithmMode.Up).ToArray());
            dimension = bitArray.Length;
        }

        public int Differencies(NetworkState another)
        {
            if (dimension != another.dimension)
                throw new ArgumentException("Dimensions mismatch", nameof(another));

            int differencies = 0;
            for (int i = 0; i < dimension; i++)
            {
                if (bitArray[i] != another.bitArray[i])
                {
                    differencies++;
                }
            }

            return differencies;
        }

        public int FirstDifference()
        {
            int i;
            for (i = 1; i < bitArray.Length && bitArray[i] == bitArray[0]; i++)
            {
            }

            int firstDifference = i < bitArray.Length ? i : 1;

            return firstDifference;
        }

        public int ToInteger()
        {
            if (bitArray.Length > 32)
                throw new ArgumentException("Argument length shall be at most 32 bits.");
            
            int value = 0;

            for (int i = 0; i < bitArray.Length; i++)
            {
                if (bitArray[i])
                    value += 1 << i;
            }

            return value;
        }

        public bool this[int index] => bitArray[index];

        public override string ToString()
        {
            List<bool> bools = new List<bool>();
            foreach (var bit in bitArray)
            {
                bools.Add((bool)bit);
            }
            return string.Join("", bools.Select(x => x ? "1" : "0"));
        }
        
        public override bool Equals(object obj)
        {
            var another = obj as NetworkState;
            if (another == null)
                return false;

            if (dimension != another.dimension)
                return false;

            return Differencies(another) == 0;
        }

        public override int GetHashCode()
        {
            return bitArray.GetHashCode();
        }

        public IEnumerator<bool> GetEnumerator()
        {
            foreach (var bit in bitArray)
            {
                yield return (bool)bit;
            }
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return bitArray.GetEnumerator();
        }
    }
}
