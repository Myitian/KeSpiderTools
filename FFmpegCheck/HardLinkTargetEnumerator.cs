using System.Buffers;
using System.Collections;
using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;

namespace FFmpegCheck;

public partial struct HardLinkTargetEnumerator
    : IEnumerable<ReadOnlyMemory<char>>, IEnumerator<ReadOnlyMemory<char>>
{
    const int ERROR_HANDLE_EOF = 38;
    const int ERROR_MORE_DATA = 234;
    private IMemoryOwner<char> _mem;
    private nint _handle = -1;
    private int _length = 0;
    private bool _first = true;
    public readonly nint RawHandle => _handle;
    public readonly bool IsInvalid => _handle is -1;
    public readonly int Capacity => _mem.Memory.Length;
    public readonly ReadOnlyMemory<char> Current => _mem.Memory[.._length];
    readonly object IEnumerator.Current => Current;

    public HardLinkTargetEnumerator(string fileName, int? initalCapacity = null)
    {
        EnsureCapacity(initalCapacity ?? fileName.Length);
        int length = Capacity;
        while ((_handle = FindFirstFileNameW(fileName, 0, ref length, _mem.Memory.Span)) is -1)
        {
            int error = Marshal.GetLastPInvokeError();
            switch (error)
            {
                case ERROR_HANDLE_EOF:
                    _first = false;
                    return;
                case ERROR_MORE_DATA:
                    EnsureCapacity(length);
                    continue;
                default:
                    // _handle is -1, doesn't need Dispose();
                    throw new Win32Exception(error);
            }
        }
        _length = length - 1; // remove '\0'
    }
    [MemberNotNull(nameof(_mem))]
    public void EnsureCapacity(int capacity)
    {
        ArgumentOutOfRangeException.ThrowIfGreaterThan(capacity, Array.MaxLength);
        if (_mem is not null)
        {
            if (Capacity >= capacity)
                return;
            _mem.Dispose();
        }
        _mem = MemoryPool<char>.Shared.Rent(capacity);
    }

    public readonly void Reset() => throw new NotSupportedException();
    public readonly HardLinkTargetEnumerator GetEnumerator() => this;
    readonly IEnumerator<ReadOnlyMemory<char>> IEnumerable<ReadOnlyMemory<char>>.GetEnumerator() => this;
    readonly IEnumerator IEnumerable.GetEnumerator() => this;

    public bool MoveNext()
    {
        if (_first)
        {
            _first = false;
            return true;
        }
        if (IsInvalid)
            return false;
        int length = Capacity;
        while (!FindNextFileNameW(_handle, ref length, _mem.Memory.Span))
        {
            int error = Marshal.GetLastPInvokeError();
            switch (error)
            {
                case ERROR_HANDLE_EOF:
                    Dispose();
                    return false;
                case ERROR_MORE_DATA:
                    EnsureCapacity(length);
                    continue;
                default:
                    Dispose();
                    throw new Win32Exception(error);
            }
        }
        _length = length - 1; // remove '\0'
        return true;
    }
    public void Dispose()
    {
        if (_handle is not -1)
        {
            FindClose(_handle);
            _handle = -1;
        }
    }

    [LibraryImport("kernel32", SetLastError = true, StringMarshalling = StringMarshalling.Utf16)]
    private static partial nint FindFirstFileNameW(
        string lpFileName,
        int dwFlags /* Reserved, always 0 */,
        ref int StringLength,
        Span<char> LinkName);
    [LibraryImport("kernel32", SetLastError = true, StringMarshalling = StringMarshalling.Utf16)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static partial bool FindNextFileNameW(
        nint hFindStream,
        ref int StringLength,
        Span<char> LinkName);
    [LibraryImport("kernel32", SetLastError = true, StringMarshalling = StringMarshalling.Utf16)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static partial bool FindClose(
        nint hFindStream);
}