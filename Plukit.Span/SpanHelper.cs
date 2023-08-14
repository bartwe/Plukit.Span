using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime;

namespace Plukit.Span;

public static class SpanHelper {
    [TargetedPatchingOptOut("")]
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Span<TResult> Cast<TFrom, TResult>(this Span<TFrom> from) where TFrom : struct where TResult : struct {
        return MemoryMarshal.Cast<TFrom, TResult>(from);
    }

    [TargetedPatchingOptOut("")]
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static ReadOnlySpan<TResult> Cast<TFrom, TResult>(this ReadOnlySpan<TFrom> from) where TFrom : struct where TResult : struct {
        return MemoryMarshal.Cast<TFrom, TResult>(from);
    }

    [TargetedPatchingOptOut("")]
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static T SingleElement<T>(this Span<T> span) {
        if (span.Length != 1) {
            throw new ArgumentException("Span must have exactly one element.", nameof(span));
        }
        return span[0];
    }

    [TargetedPatchingOptOut("")]
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static T SingleElement<T>(this ReadOnlySpan<T> span) {
        if (span.Length != 1) {
            throw new ArgumentException("Span must have exactly one element.", nameof(span));
        }
        return span[0];
    }
}