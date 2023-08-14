using System;
using System.IO;

namespace Plukit.Span;

public ref struct ReadOnlySpanStream {
    public ReadOnlySpan<byte> BackingSpan;
    int _position;
    int _length;

    public ReadOnlySpanStream(ReadOnlySpan<byte> buffer) {
        BackingSpan = buffer;
        _length = buffer.Length;
        _position = 0;
    }

    public ReadOnlySpanStream(ReadOnlySpan<byte> buffer, int initialLength) {
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

    public void SetLength(int value) {
        if ((value < 0) || (value > BackingSpan.Length))
            ThrowArgumentOutOfRangeException();
        Length = value;
        if (Length < _position) {
            _position = value;
        }
    }

    public ReadOnlySpan<byte> ReadSpan(int length) {
        if ((length + _position) > Length)
            ThrowArgumentOutOfRangeException();
        var result = BackingSpan.Slice(_position, length);
        _ = Seek(length, SeekOrigin.Current);
        return result;
    }

    public ReadOnlySpanStream ReadSpanStream(int length) {
        if ((length + _position) > Length)
            ThrowArgumentOutOfRangeException();
        var result = BackingSpan.Slice(_position, length);
        _ = Seek(length, SeekOrigin.Current);
        return new(result);
    }

    public ReadOnlySpan<byte> ReadSubStream() {
        var length = checked((int)ReadVlqu());
        if ((length + _position) > Length)
            ThrowArgumentOutOfRangeException();
        var result = BackingSpan.Slice(Position, length);
        _ = Seek(length, SeekOrigin.Current);
        return result;
    }

    public void ReadSubStream(Stream result) {
        var length = checked((int)ReadVlqu());
        ReadInto(result, length);
    }

    public unsafe T ReadValue<T>() where T : unmanaged {
        var size = sizeof(T);
        T value;
        ReadFully(new(&value, size));
        return value;
    }

    public unsafe void ReadRange<T>(Span<T> range) where T : unmanaged {
        fixed (T* p = range)
            ReadFully(new((byte*)p, range.Length * sizeof(T)));
    }

    public ulong ReadVlqu() {
        unchecked {
            var b = ReadByte();
            if (b == -1)
                goto error;
            ulong result = (byte)(b & 0x7f);
            if ((b & 0x80) == 0)
                return result;
            b = ReadByte();
            if (b == -1)
                goto error;
            result = (result << 7) | (byte)(b & 0x7f);
            if ((b & 0x80) == 0)
                goto done;
            b = ReadByte();
            if (b == -1)
                goto error;
            result = (result << 7) | (byte)(b & 0x7f);
            if ((b & 0x80) == 0)
                goto done;
            b = ReadByte();
            if (b == -1)
                goto error;
            result = (result << 7) | (byte)(b & 0x7f);
            if ((b & 0x80) == 0)
                goto done;
            b = ReadByte();
            if (b == -1)
                goto error;
            result = (result << 7) | (byte)(b & 0x7f);
            if ((b & 0x80) == 0)
                goto done;
            b = ReadByte();
            if (b == -1)
                goto error;
            result = (result << 7) | (byte)(b & 0x7f);
            if ((b & 0x80) == 0)
                goto done;
            b = ReadByte();
            if (b == -1)
                goto error;
            result = (result << 7) | (byte)(b & 0x7f);
            if ((b & 0x80) == 0)
                goto done;
            b = ReadByte();
            if (b == -1)
                goto error;
            result = (result << 7) | (byte)(b & 0x7f);
            if ((b & 0x80) == 0)
                goto done;
            b = ReadByte();
            if (b == -1)
                goto error;
            result = (result << 7) | (byte)(b & 0x7f);
            if ((b & 0x80) != 0)
                goto error;
            done:
            {
                return result;
            }
            error:
            {
                UnexpectedEOF();
                return 0;
            }
        }
    }

    static void UnexpectedEOF() {
        throw new("Unexpected end of stream.");
    }

    public ReadOnlySpan<byte> ContentSpan => BackingSpan.Slice(_position, Length - _position);

    public void Skip(int length) {
        if ((length + _position) > Length)
            ThrowArgumentOutOfRangeException();
        _ = Seek(length, SeekOrigin.Current);
    }
}