using System;
using System.Collections;
using System.Collections.Generic;

namespace DiegoG.WebWatcher;
public class CircularList<T> : ICollection<T>, IReadOnlyCollection<T>
{
    private readonly T[] _list;
    private int head = 0;
    private int fill = 0;

    public CircularList(int listSize)
    {
        _list = new T[listSize];
    }

    public void Add(T item)
    {
        fill = int.Min(fill + 1, _list.Length);
        _list[head++] = item;
        head %= _list.Length;
    }

    public void Clear()
    {
        fill = 0;
        head = 0;
        Array.Clear(_list);
    }

    public bool Contains(T item)
        => ((ICollection<T>)_list).Contains(item);

    public void CopyTo(T[] array, int arrayIndex)
    {
        for (int i = 0; i < array.Length - arrayIndex && i < fill; i++)
            array[i + arrayIndex] = _list[(i + head) % fill];
    }

    public bool Remove(T item)
    {
        if (((ICollection<T>)_list).Remove(item))
        {
            fill = int.Max(fill - 1, 0);
            head = int.Max(head - 1, 0);
            return true;
        }
        return false;
    }

    public int Count => fill;
    public bool IsReadOnly => false;

    public IEnumerator<T> GetEnumerator()
    {
        for (int i = 0; i < fill; i++)
            yield return _list[(i + head) % fill];
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        for (int i = 0; i < fill; i++)
            yield return _list[(i + head) % fill];
    }
}
