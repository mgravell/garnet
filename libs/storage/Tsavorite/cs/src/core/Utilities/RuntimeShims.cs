// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Buffers;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Tsavorite.core.Utilities
{
    internal static class RuntimeShims
    {
#if !NETCOREAPP3_0_OR_GREATER
        public static ValueTask<int> ReadAsync(this Stream stream, Memory<byte> buffer, CancellationToken token = default)
        {
            if (buffer.IsEmpty)
            {
                // zero-length read can represent a "wait"; need to preserve it
                return new(stream.ReadAsync([], 0, 0, token));
            }
            else if (MemoryMarshal.TryGetArray<byte>(buffer, out var segment))
            {
                return new(stream.ReadAsync(segment.Array, segment.Offset, segment.Count, token));
            }
            else
            {
                var oversized = ArrayPool<byte>.Shared.Rent(buffer.Length);
                var pending = stream.ReadAsync(oversized, 0, buffer.Length, token);
                if (pending.Status != TaskStatus.RanToCompletion) return Awaited(pending, buffer, oversized);
                var count = pending.GetAwaiter().GetResult();
                if (count > 0)
                {
                    new ReadOnlySpan<byte>(oversized, 0, count).CopyTo(buffer.Span);
                }
                ArrayPool<byte>.Shared.Return(oversized);
                return new(count);
            }

            static async ValueTask<int> Awaited(Task<int> pending, Memory<byte> buffer, byte[] oversized)
            {
                var count = await pending.ConfigureAwait(false);
                if (count > 0)
                {
                    new ReadOnlySpan<byte>(oversized, 0, count).CopyTo(buffer.Span);
                }
                ArrayPool<byte>.Shared.Return(oversized);
                return count;
            }
        }

        public static ValueTask WriteAsync(this Stream stream, ReadOnlyMemory<byte> buffer, CancellationToken token = default)
        {
            if (buffer.IsEmpty)
            {
                // zero-length write can represent a flush; need to preserve it
                return new(stream.WriteAsync([], 0, 0, token));
            }
            else if (MemoryMarshal.TryGetArray<byte>(buffer, out var segment))
            {
                return new(stream.WriteAsync(segment.Array, segment.Offset, segment.Count, token));
            }
            else
            {
                var oversized = ArrayPool<byte>.Shared.Rent(buffer.Length);
                buffer.CopyTo(oversized);
                stream.Write(oversized, 0, buffer.Length);
                var pending = stream.WriteAsync(oversized, 0, buffer.Length, token);
                if (pending.Status != TaskStatus.RanToCompletion) return Awaited(pending, oversized);
                pending.GetAwaiter().GetResult(); // just to observe exception (we don't expect one)
                ArrayPool<byte>.Shared.Return(oversized);
                return default;
            }

            static async ValueTask Awaited(Task pending, byte[] oversized)
            {
                await pending.ConfigureAwait(false);
                ArrayPool<byte>.Shared.Return(oversized);
            }
        }

        public static unsafe string GetString(this Encoding encoding, ReadOnlySpan<byte> buffer)
        {
            if (buffer.IsEmpty) return "";
            fixed (byte* ptr = buffer)
            {
                return encoding.GetString(ptr, buffer.Length);
            }
        }

        public static bool Remove<TKey, TValue>(this Dictionary<TKey, TValue> lookup, TKey key, out TValue value)
        {
            if (lookup.TryGetValue(key, out value))
            {
                lookup.Remove(key);
                return true;
            }
            return false;
        }
#endif
    }
}
