#region license

// Copyright (c) 2021, andreakarasho
// All rights reserved.
// 
// Redistribution and use in source and binary forms, with or without
// modification, are permitted provided that the following conditions are met:
// 1. Redistributions of source code must retain the above copyright
//    notice, this list of conditions and the following disclaimer.
// 2. Redistributions in binary form must reproduce the above copyright
//    notice, this list of conditions and the following disclaimer in the
//    documentation and/or other materials provided with the distribution.
// 3. All advertising materials mentioning features or use of this software
//    must display the following acknowledgement:
//    This product includes software developed by andreakarasho - https://github.com/andreakarasho
// 4. Neither the name of the copyright holder nor the
//    names of its contributors may be used to endorse or promote products
//    derived from this software without specific prior written permission.
// 
// THIS SOFTWARE IS PROVIDED BY THE COPYRIGHT HOLDERS ''AS IS'' AND ANY
// EXPRESS OR IMPLIED WARRANTIES, INCLUDING, BUT NOT LIMITED TO, THE IMPLIED
// WARRANTIES OF MERCHANTABILITY AND FITNESS FOR A PARTICULAR PURPOSE ARE
// DISCLAIMED. IN NO EVENT SHALL THE COPYRIGHT HOLDER BE LIABLE FOR ANY
// DIRECT, INDIRECT, INCIDENTAL, SPECIAL, EXEMPLARY, OR CONSEQUENTIAL DAMAGES
// (INCLUDING, BUT NOT LIMITED TO, PROCUREMENT OF SUBSTITUTE GOODS OR SERVICES;
// LOSS OF USE, DATA, OR PROFITS; OR BUSINESS INTERRUPTION) HOWEVER CAUSED AND
// ON ANY THEORY OF LIABILITY, WHETHER IN CONTRACT, STRICT LIABILITY, OR TORT
// (INCLUDING NEGLIGENCE OR OTHERWISE) ARISING IN ANY WAY OUT OF THE USE OF THIS
// SOFTWARE, EVEN IF ADVISED OF THE POSSIBILITY OF SUCH DAMAGE.

#endregion

namespace ClassicUO.IO.Audio.MP3Sharp.Decoding
{
    /// <summary>
    ///     Implementation of Bit Reservoir for Layer III.
    ///     The implementation stores single bits as a word in the buffer. If
    ///     a bit is set, the corresponding word in the buffer will be non-zero.
    ///     If a bit is clear, the corresponding word is zero. Although this
    ///     may seem waseful, this can be a factor of two quicker than
    ///     packing 8 bits to a byte and extracting.
    /// </summary>

    // REVIEW: there is no range checking, so buffer underflow or overflow
    // can silently occur.
    internal sealed class BitReserve
    {
        /// <summary>
        ///     Size of the internal buffer to store the reserved bits.
        ///     Must be a power of 2. And x8, as each bit is stored as a single
        ///     entry.
        /// </summary>
        private const int BUFSIZE = 4096 * 8;

        /// <summary>
        ///     Mask that can be used to quickly implement the
        ///     modulus operation on BUFSIZE.
        /// </summary>
        private static readonly int BUFSIZE_MASK = BUFSIZE - 1;

        private int[] buf;
        private int offset, totbit, buf_byte_idx;

        internal BitReserve()
        {
            InitBlock();

            offset = 0;
            totbit = 0;
            buf_byte_idx = 0;
        }

        private void InitBlock()
        {
            buf = new int[BUFSIZE];
        }

        /// <summary>
        ///     Return totbit Field.
        /// </summary>
        public int hsstell()
        {
            return totbit;
        }

        /// <summary>
        ///     Read a number bits from the bit stream.
        /// </summary>
        public int ReadBits(int N)
        {
            totbit += N;

            int val = 0;

            int pos = buf_byte_idx;

            if (pos + N < BUFSIZE)
            {
                while (N-- > 0)
                {
                    val <<= 1;
                    val |= buf[pos++] != 0 ? 1 : 0;
                }
            }
            else
            {
                while (N-- > 0)
                {
                    val <<= 1;
                    val |= buf[pos] != 0 ? 1 : 0;
                    pos = (pos + 1) & BUFSIZE_MASK;
                }
            }

            buf_byte_idx = pos;

            return val;
        }

        /// <summary>
        ///     Read 1 bit from the bit stream.
        /// </summary>
        public int ReadOneBit()
        {
            totbit++;
            int val = buf[buf_byte_idx];
            buf_byte_idx = (buf_byte_idx + 1) & BUFSIZE_MASK;

            return val;
        }

        /// <summary>
        ///     Write 8 bits into the bit stream.
        /// </summary>
        public void hputbuf(int val)
        {
            int ofs = offset;
            buf[ofs++] = val & 0x80;
            buf[ofs++] = val & 0x40;
            buf[ofs++] = val & 0x20;
            buf[ofs++] = val & 0x10;
            buf[ofs++] = val & 0x08;
            buf[ofs++] = val & 0x04;
            buf[ofs++] = val & 0x02;
            buf[ofs++] = val & 0x01;

            offset = ofs == BUFSIZE ? 0 : ofs;
        }

        /// <summary>
        ///     Rewind n bits in Stream.
        /// </summary>
        public void RewindStreamBits(int bitCount)
        {
            totbit -= bitCount;
            buf_byte_idx -= bitCount;

            if (buf_byte_idx < 0)
            {
                buf_byte_idx += BUFSIZE;
            }
        }

        /// <summary>
        ///     Rewind n bytes in Stream.
        /// </summary>
        public void RewindStreamBytes(int byteCount)
        {
            int bits = byteCount << 3;
            totbit -= bits;
            buf_byte_idx -= bits;

            if (buf_byte_idx < 0)
            {
                buf_byte_idx += BUFSIZE;
            }
        }
    }
}