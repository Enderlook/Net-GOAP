using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace Enderlook.GOAP
{
    internal readonly struct SingleElementList<T> : IList<T>
    {
        private readonly T element;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public SingleElementList(T element) => this.element = element;

        T IList<T>.this[int index] {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get {
                Debug.Assert(index == 0);
                return element;
            }
            set => throw new NotImplementedException();
        }

        int ICollection<T>.Count {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => 1;
        }

        bool ICollection<T>.IsReadOnly => throw new NotImplementedException();

        void ICollection<T>.Add(T item) => throw new NotImplementedException();

        void ICollection<T>.Clear() => throw new NotImplementedException();

        bool ICollection<T>.Contains(T item) => throw new NotImplementedException();

        void ICollection<T>.CopyTo(T[] array, int arrayIndex) => throw new NotImplementedException();

        IEnumerator<T> IEnumerable<T>.GetEnumerator() => throw new NotImplementedException();

        IEnumerator IEnumerable.GetEnumerator() => throw new NotImplementedException();

        int IList<T>.IndexOf(T item) => throw new NotImplementedException();

        void IList<T>.Insert(int index, T item) => throw new NotImplementedException();

        bool ICollection<T>.Remove(T item) => throw new NotImplementedException();

        void IList<T>.RemoveAt(int index) => throw new NotImplementedException();
    }
}
