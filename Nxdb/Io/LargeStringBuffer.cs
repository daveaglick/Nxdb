using System;
using System.Collections.Generic;
using System.Text;

namespace Nxdb.Io
{
    /// <summary>
    /// Holds large strings on disk. Relies on a segmenting approach where the string is broken up into
    /// segments based on how the string is constructed and appended to.
    /// </summary>
    internal class LargeStringBuffer
    {
        private readonly int _minAllocation;
        private readonly int _maxAllocation;
        private readonly List<StringBuilder> _segments;
        private int[] _startOffsets;         // if startOffsets[23] is 123456, then the first character in segment 23 is the 123456'th character of the CharSequence value.
        private int _len;

        /// <summary>
        /// Initializes a new instance of the <see cref="LargeStringBuffer"/> class with a default allocation strategy.
        /// The minimum segment allocation is 4096 bytes and the maximum segment allocation is 65536 bytes.
        /// </summary>
        public LargeStringBuffer()
            : this(4096, 65536)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="LargeStringBuffer"/> class with a specified allocation strategy.
        /// </summary>
        /// <param name="minAllocation">The minimum sgement allocation.</param>
        /// <param name="maxAllocation">The maximum segment allocation.</param>
        public LargeStringBuffer(int minAllocation, int maxAllocation)
        {
            _minAllocation = Math.Min(minAllocation, maxAllocation);
            _maxAllocation = maxAllocation;
            StringBuilder initial = new StringBuilder(minAllocation);
            _segments = new List<StringBuilder>(4) { initial };
            _startOffsets = new int[1];
            _startOffsets[0] = 0;
            _len = 0;
        }

        /// <summary>
        /// Gets the length of the string.
        /// </summary>
        /// <value>The length of the string.</value>
        public int Length
        {
            get
            {
                return _len;
            }
        }

        /// <summary>
        /// Appends a specified string to this one.
        /// </summary>
        /// <param name="value">The string to append.</param>
        public void Append(string value)
        {
            Append(value.ToCharArray());
        }

        public void Append(char[] chars)
        {
            Append(chars, 0, chars.Length);
        }

        public void Append(char[] chars, int startIndex, int count)
        {
            if (count == 0)
            {
                return;
            }
            StringBuilder last = _segments[_segments.Count - 1];
            if (last.Length + count <= _maxAllocation)
            {
                last.Append(chars, startIndex, count);
            }
            else
            {
                int[] s2 = new int[_startOffsets.Length + 1];
                Array.Copy(_startOffsets, s2, _startOffsets.Length);
                s2[_startOffsets.Length] = _len;
                _startOffsets = s2;
                last = new StringBuilder(Math.Max(_minAllocation, count));
                _segments.Add(last);
                last.Append(chars, startIndex, count);
            }
            _len += count;
        }

        /// <summary>
        /// Gets the <see cref="System.Char"/> with the specified index.
        /// </summary>
        /// <value>The index to get.</value>
        public char this[int i]
        {
            get
            {
                if (_startOffsets.Length == 1)
                {
                    return _segments[0][i];
                }
                if (i < 0 || i >= _len)
                {
                    throw new IndexOutOfRangeException();
                }
                int seg = Array.BinarySearch(_startOffsets, i);
                if (seg >= 0)
                {
                    return _segments[seg][0];
                }
                seg = -seg - 2;
                int offset = i - _startOffsets[seg];
                return _segments[seg][offset];
            }
        }

        /// <summary>
        /// Gets a substring of this string.
        /// </summary>
        /// <param name="startIndex">The start index.</param>
        /// <param name="length">The length.</param>
        /// <returns>The specified substring.</returns>
        public string Substring(int startIndex, int length)
        {
            int end = startIndex + length;
            if (_startOffsets.Length == 1)
            {
                return _segments[0].ToString(startIndex, length);
            }
            if (startIndex < 0 || end < 0 || end > _len || startIndex > end)
            {
                throw new IndexOutOfRangeException();
            }
            int seg0 = Array.BinarySearch(_startOffsets, startIndex);
            int offset0;
            if (seg0 >= 0)
            {
                offset0 = 0;
            }
            else
            {
                seg0 = -seg0 - 2;
                offset0 = startIndex - _startOffsets[seg0];
            }
            int seg1 = Array.BinarySearch(_startOffsets, end);
            int offset1;
            if (seg1 >= 0)
            {
                offset1 = 0;
            }
            else
            {
                seg1 = -seg1 - 2;
                offset1 = end - _startOffsets[seg1];
            }
            StringBuilder startSegment = _segments[seg0];
            if (seg0 == seg1)
            {
                // the required substring is all in one segment
                return startSegment.ToString(offset0, offset1 - offset0);
            }
            // copy the data into a new FastStringBuffer. This case should be exceptional
            StringBuilder sb = new StringBuilder(length);
            sb.Append(startSegment.ToString(offset0, startSegment.Length - offset0));
            for (int i = seg0 + 1; i < seg1; i++)
            {
                sb.Append(_segments[i]);
            }
            if (offset1 > 0)
            {
                sb.Append(_segments[seg1].ToString(0, offset1));
            }
            return sb.ToString();
        }

        /// <summary>
        /// Returns the full contained string.
        /// </summary>
        /// <returns>
        /// The full content of this string.
        /// </returns>
        public override string ToString()
        {
            return Substring(0, Length);
        }
    }
}