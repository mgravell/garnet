// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Tsavorite.core
{
    /// <summary>
    /// A frame is an in-memory circular buffer of log pages
    /// </summary>
    internal sealed class BlittableFrame : IDisposable
    {
        public readonly int frameSize, pageSize, sectorSize;
        public readonly byte[][] frame;
        public readonly long[] pointers;
#if !NET5_0_OR_GREATER
        private readonly GCHandle[] pins;
#endif

        public BlittableFrame(int frameSize, int pageSize, int sectorSize)
        {
            this.frameSize = frameSize;
            this.pageSize = pageSize;
            this.sectorSize = sectorSize;

            frame = new byte[frameSize][];
            pointers = new long[frameSize];
            pins = new GCHandle[frameSize];
        }

        public unsafe void Allocate(int index)
        {
            var adjustedSize = pageSize + 2 * sectorSize;
#if NET5_0_OR_GREATER
            byte[] tmp = GC.AllocateArray<byte>(adjustedSize, pinned: true);
#else
            byte[] tmp = new byte[adjustedSize];
            pins[index] = GCHandle.Alloc(tmp, GCHandleType.Pinned);
#endif
            long p = (long)Unsafe.AsPointer(ref tmp[0]);
            pointers[index] = (p + (sectorSize - 1)) & ~((long)sectorSize - 1);
            frame[index] = tmp;
        }

        public void Clear(int pageIndex)
        {
            Array.Clear(frame[pageIndex], 0, frame[pageIndex].Length);
        }

        public long GetPhysicalAddress(long frameNumber, long offset)
        {
            return pointers[frameNumber % frameSize] + offset;
        }

        public void Dispose()
        {
#if !NET5_0_OR_GREATER
            for (int i = 0; i < pins.Length; i++)
            {
                if (pins[i].IsAllocated)
                {
                    pins[i].Free();
                    pins[i] = default; // in case of double-dispose
                }
            }
#endif
        }
    }
}