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

using ClassicUO.IO.Audio.MP3Sharp.Decoding.Decoders.LayerI;

namespace ClassicUO.IO.Audio.MP3Sharp.Decoding.Decoders
{
    /// <summary>
    ///     Implements decoding of MPEG Audio Layer I frames.
    /// </summary>
    internal class LayerIDecoder : IFrameDecoder
    {
        // new Crc16[1] to enable CRC checking.

        public LayerIDecoder()
        {
            crc = new Crc16();
        }

        public virtual void DecodeFrame()
        {
            num_subbands = header.number_of_subbands();
            subbands = new ASubband[32];
            mode = header.mode();

            CreateSubbands();

            ReadAllocation();
            ReadScaleFactorSelection();

            if (crc != null || header.IsChecksumOK())
            {
                ReadScaleFactors();

                ReadSampleData();
            }
        }

        protected internal ABuffer buffer;
        protected internal Crc16 crc;
        protected internal SynthesisFilter filter1, filter2;
        protected internal Header header;
        protected internal int mode;
        protected internal int num_subbands;
        protected internal Bitstream stream;
        protected internal ASubband[] subbands;
        protected internal int which_channels;

        public virtual void Create
        (
            Bitstream stream0,
            Header header0,
            SynthesisFilter filtera,
            SynthesisFilter filterb,
            ABuffer buffer0,
            int whichCh0
        )
        {
            stream = stream0;
            header = header0;
            filter1 = filtera;
            filter2 = filterb;
            buffer = buffer0;
            which_channels = whichCh0;
        }

        protected internal virtual void CreateSubbands()
        {
            int i;

            if (mode == Header.SINGLE_CHANNEL)
            {
                for (i = 0; i < num_subbands; ++i)
                {
                    subbands[i] = new SubbandLayer1(i);
                }
            }
            else if (mode == Header.JOINT_STEREO)
            {
                for (i = 0; i < header.intensity_stereo_bound(); ++i)
                {
                    subbands[i] = new SubbandLayer1Stereo(i);
                }

                for (; i < num_subbands; ++i)
                {
                    subbands[i] = new SubbandLayer1IntensityStereo(i);
                }
            }
            else
            {
                for (i = 0; i < num_subbands; ++i)
                {
                    subbands[i] = new SubbandLayer1Stereo(i);
                }
            }
        }

        protected internal virtual void ReadAllocation()
        {
            // start to read audio data:
            for (int i = 0; i < num_subbands; ++i)
            {
                subbands[i].read_allocation(stream, header, crc);
            }
        }

        protected internal virtual void ReadScaleFactorSelection()
        {
            // scale factor selection not present for layer I. 
        }

        protected internal virtual void ReadScaleFactors()
        {
            for (int i = 0; i < num_subbands; ++i)
            {
                subbands[i].read_scalefactor(stream, header);
            }
        }

        protected internal virtual void ReadSampleData()
        {
            bool readReady = false;
            bool writeReady = false;
            int hdrMode = header.mode();

            do
            {
                int i;

                for (i = 0; i < num_subbands; ++i)
                {
                    readReady = subbands[i].read_sampledata(stream);
                }

                do
                {
                    for (i = 0; i < num_subbands; ++i)
                    {
                        writeReady = subbands[i].put_next_sample(which_channels, filter1, filter2);
                    }

                    filter1.calculate_pcm_samples(buffer);

                    if (which_channels == OutputChannels.BOTH_CHANNELS && hdrMode != Header.SINGLE_CHANNEL)
                    {
                        filter2.calculate_pcm_samples(buffer);
                    }
                } while (!writeReady);
            } while (!readReady);
        }
    }
}