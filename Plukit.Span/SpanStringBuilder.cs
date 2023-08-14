using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Plukit.Span;
public ref struct SpanStringBuilder {
    public Span<char> BackingSpan;
    int _position;
    int _length;

    public SpanStringBuilder(Span<char> buffer) {
        BackingSpan = buffer;
        _position = 0;
        _length = 0;
    }

    public int Length {
        get => _length;
        private set {
            if ((value < 0) || (value > BackingSpan.Length))
                ThrowArgumentOutOfRangeException();
            _length = value;
        }
    }

    public int Position {
        get => _position;
        set {
            if ((value < 0) || (value > Length))
                ThrowArgumentOutOfRangeException();
            _position = value;
        }
    }

    public int Remainder => Length - _position;

    static void ThrowArgumentOutOfRangeException() {
        throw new ArgumentOutOfRangeException();
    }

    public void Append(char value) {
        if ((_position + 1) > BackingSpan.Length)
            ThrowArgumentOutOfRangeException();
        BackingSpan[_position] = value;
        _position += 1;
        if (_position > Length)
            Length = _position;
    }

    public void Append(string value) {
        Append(value.AsSpan());
    }

    public void Append(ReadOnlySpan<char> buffer) {
        if ((_position + buffer.Length) > BackingSpan.Length)
            ThrowArgumentOutOfRangeException();
        buffer.CopyTo(BackingSpan.Slice(_position, buffer.Length));
        _position += buffer.Length;
        if (_position > Length)
            Length = _position;
    }

    public void Append(char[] buffer, int offset, int count) {
        Append(new ReadOnlySpan<char>(buffer, offset, count));
    }

    public void Append(StringBuilder value) {
        value.CopyTo(0, PrepareWriteSpan(value.Length), value.Length);
    }

    public void Append(ref SpanStringBuilder value) {
        Append(value.ContentSpan);
    }

    public void Append(StringBuilder value, int offset, int length) {
        if ((offset < 0) || (offset > value.Length))
            throw new ArgumentOutOfRangeException(nameof(offset));
        if ((length < 0) || (offset + length > value.Length))
            throw new ArgumentOutOfRangeException(nameof(length));
        value.CopyTo(offset, PrepareWriteSpan(length), length);
    }

    // TODO, alloc free formatting of int, long, float, double etc. MemoryExtensions.TryWriteInterpolatedStringHandler is smart about this maybe we can hook into those

    public void SetLength(int value) {
        if ((value < 0) || (value > BackingSpan.Length))
            ThrowArgumentOutOfRangeException();
        Length = value;
        if (Length < _position) {
            _position = value;
        }
    }

    public Span<char> PrepareWriteSpan(int length) {
        if ((length + _position) > Length)
            ThrowArgumentOutOfRangeException();
        var result = BackingSpan.Slice(_position, length);
        _position += length;
        return result;
    }

    public Span<char> ContentSpan => BackingSpan[_position..Length];
}
