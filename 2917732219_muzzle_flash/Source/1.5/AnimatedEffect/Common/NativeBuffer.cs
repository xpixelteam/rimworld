﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace MuzzleFlash
{
    public unsafe struct NativeBuffer<T> : IReadOnlyList<T>, IDisposable where T : unmanaged
    {
        private void* _ptrBuffer;
        private readonly int _size;
        private static readonly int _stride = UtilsUnsafe.SizeOf<T>();

        public int Length => _size;

        public T* Raw => (T*)_ptrBuffer;
        public int Size => _size;
        public int Stride => _stride;
        public int Count => Size;

        public T this[int index]
        {
            get
            {
                unsafe
                {
                    if (NotInRange(index)) throw new IndexOutOfRangeException();
                    return ((T*)_ptrBuffer)[index];
                }
            }
            set
            {
                unsafe
                {
                    if (NotInRange(index)) throw new IndexOutOfRangeException();
                    ((T*)_ptrBuffer)[index] = value;
                }
            }
        }

        public NativeBuffer(int size)
        {
            if (size <= 0) throw new IndexOutOfRangeException();
            _ptrBuffer = UtilsUnsafe.Alloc(size * _stride);
            this._size = size;
        }

        public void Dispose()
        {
            if (_ptrBuffer != null)
            {
                UtilsUnsafe.Free(_ptrBuffer);
                _ptrBuffer = null;
            }
            GC.SuppressFinalize(this);
        }

        public IEnumerator<T> GetEnumerator()
        {
            for (int i = 0; i < _size; i++)
            {
                yield return this[i];
            }
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private bool NotInRange(int index)
        {
            return index < 0 || index >= _size;
        }
    }
}
