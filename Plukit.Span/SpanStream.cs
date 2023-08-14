using System;
using System.IO;

namespace Plukit.Span;

public ref struct SpanStream {
    public Span<byte> BackingSpan;
    int _position;
    int _length;

    public SpanStream(Span<byte> buffer) {
        BackingSpan = buffer;
        _length = buffer.Length;
        _position = 0;
    }

    public SpanStream(Span<byte> buffer, int initialLength) {
        if (initialLength > buffer.Length)
            ThrowArgumentOutOfRangeException();
        BackingSpan = buffer;
        _length = initialLength;
        _position = 0;
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

    public void WriteZeroRange(int bytes) {
        if (bytes > (BackingSpan.Length - _position))
            ThrowArgumentOutOfRangeException();
        BackingSpan.Slice(_position, bytes).Clear();
        _position += bytes;
    }

    public void CopyFrom(Stream source) {
        var remainder = source.Length - source.Position;
        if (remainder > (BackingSpan.Length - _position))
            ThrowArgumentOutOfRangeException();
        const int maxReadSize = 1 << 30;
        while (remainder > 0) {
            var step = maxReadSize;
            if (step > remainder)
                step = unchecked((int)remainder);
            step = source.Read(BackingSpan.Slice(_position, step));
            remainder -= step;
            _position += step;
            if (_position > Length)
                Length = _position;
        }
    }

    public void CopyTo(Stream destination, int bufferSize) {
        var remainder = Length - _position;
        while (remainder > 0) {
            var step = bufferSize;
            if (step > remainder)
                step = remainder;
            destination.Write(BackingSpan.Slice(_position, step));
            _position += step;
        }
    }

    public void CopyTo(Stream destination) {
        var step = Length - _position;
        destination.Write(BackingSpan.Slice(_position, step));
        _position += step;
    }

    public void ReadInto(Stream target, int length) {
        var remainder = Length - _position;
        if (remainder < length)
            throw new ArgumentOutOfRangeException(nameof(length), length, null);
        if (remainder > length)
            remainder = length;
        target.Write(ReadSpan(remainder));
    }

    public void ReadFrom(Stream target, int length) {
        if (length > Remainder)
            throw new ArgumentOutOfRangeException(nameof(length), length, null);
        var remainder = length;
        while (remainder > 0) {
            var stepSize = target.Read(BackingSpan.Slice(_position, remainder));
            if ((stepSize <= 0) || (stepSize > remainder))
                throw new IOException();
            remainder -= stepSize;
            _position += stepSize;
        }
    }

    public void ReadInto(ref SpanStream target, int length) {
        var remainder = Length - _position;
        if (remainder < length)
            throw new ArgumentOutOfRangeException(nameof(length), length, null);
        if (remainder > length)
            remainder = length;
        target.Write(ReadSpan(remainder));
    }

    public int Read(Span<byte> buffer) {
        var length = Length - _position;
        if (length == 0)
            return -1;
        if (length > buffer.Length)
            length = buffer.Length;
        if (length == 0)
            return 0;
        BackingSpan.Slice(_position, length).CopyTo(buffer.Slice(0, length));
        _position += length;
        return length;
    }

    public void ReadFully(Span<byte> buffer) {
        var length = buffer.Length;
        if (length > (Length - _position))
            throw new EndOfStreamException();
        if (length == 0)
            return;
        BackingSpan.Slice(_position, length).CopyTo(buffer);
        _position += length;
    }

    public void ReadFully(byte[] buffer, int offset, int count) {
        var length = count;
        if (length > (Length - _position))
            throw new EndOfStreamException();
        if (length == 0)
            return;
        BackingSpan.Slice(_position, length).CopyTo(new(buffer, offset, length));
        _position += length;
    }

    public int ReadByte() {
        if (_position == Length)
            return -1;
        var result = BackingSpan[_position];
        _position++;
        return result;
    }

    public void Write(ReadOnlySpan<byte> buffer) {
        if ((_position + buffer.Length) > BackingSpan.Length)
            ThrowArgumentOutOfRangeException();
        buffer.CopyTo(BackingSpan.Slice(_position, buffer.Length));
        _position += buffer.Length;
        if (_position > Length)
            Length = _position;
    }

    public void WriteByte(byte value) {
        if (_position >= BackingSpan.Length)
            ThrowArgumentOutOfRangeException();
        BackingSpan[_position] = value;
        _position += 1;
        if (_position > Length)
            Length = _position;
    }

    public int Read(byte[] buffer, int offset, int count) {
        return Read(new(buffer, offset, count));
    }

    public int Seek(int offset, SeekOrigin origin) {
        switch (origin) {
            case SeekOrigin.Begin:
                Position = offset;
                return _position;
            case SeekOrigin.Current:
                Position = _position + offset;
                return _position;
            case SeekOrigin.End:
                Position = Length + offset;
                return _position;
            default:
                throw new ArgumentOutOfRangeException(nameof(origin), origin, null);
        }
    }

    public void Write(byte[] buffer, int offset, int count) {
        Write(new(buffer, offset, count));
    }

    public void SetLength(int value) {
        if ((value < 0) || (value > BackingSpan.Length))
            ThrowArgumentOutOfRangeException();
        Length = value;
        if (Length < _position) {
            _position = value;
        }
    }

    public void Flip() {
        Length = _position;
        _position = 0;
    }

    public void Skip(int length) {
        if ((length + _position) > Length)
            ThrowArgumentOutOfRangeException();
        _ = Seek(length, SeekOrigin.Current);
    }

    public ReadOnlySpan<byte> ReadSpan(int length) {
        if ((length + _position) > Length)
            ThrowArgumentOutOfRangeException();
        var result = BackingSpan.Slice(_position, length);
        _ = Seek(length, SeekOrigin.Current);
        return result;
    }

    public Span<byte> PrepareWriteSpan(int length) {
        if ((length + _position) > Length)
            ThrowArgumentOutOfRangeException();
        var result = BackingSpan.Slice(_position, length);
        _ = Seek(length, SeekOrigin.Current);
        return result;
    }

    public unsafe T ReadValue<T>() where T : unmanaged {
        var size = sizeof(T);
        T value;
        ReadFully(new(&value, size));
        return value;
    }

    public unsafe void WriteValue<T>(T value) where T : unmanaged {
        var size = sizeof(T);
        Write(new(&value, size));
    }

    public unsafe void ReadRange<T>(Span<T> range) where T : unmanaged {
        fixed (T* p = range)
            ReadFully(new((byte*)p, range.Length * sizeof(T)));
    }

    public unsafe void WriteRange<T>(ReadOnlySpan<T> range) where T : unmanaged {
        fixed (T* p = range)
            Write(new((byte*)p, range.Length * sizeof(T)));
    }

    public unsafe void WriteRange<T>(Span<T> range) where T : unmanaged {
        fixed (T* p = range)
            Write(new((byte*)p, range.Length * sizeof(T)));
    }

    public void FillWithZeroes() {
        var remainder = Length - _position;
        if (remainder > 0) {
            BackingSpan.Slice(_position, remainder).Fill(0);
            _position += remainder;
        }
    }

    public Span<byte> ContentSpan => BackingSpan[_position..Length];
}