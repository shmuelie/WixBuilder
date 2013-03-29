using System;
using System.Collections.Generic;

namespace WixBuilder
{
    public class UniqueCollection<T> : IEnumerable<T> where T : IComparable<T>
    {
        private List<T> collection;

        public UniqueCollection()
        {
            collection = new List<T>();
        }

        public void Add(T item)
        {
            if (item == null)
            {
                throw new ArgumentNullException("item", "'item' cannot be null (Nothing in Visual Basic)");
            }

            int low = 0;
            int high = collection.Count - 1;
            int index;
            while (low <= high)
            {
                index = (low + high) / 2;
                int comparison = collection[index].CompareTo(item);
                if (comparison < 0)
                {
                    low = index + 1;
                }
                else if (comparison > 0)
                {
                    high = index - 1;
                }
                else
                {
                    return;
                }
            }
            collection.Insert(low, item);
        }

        public bool Has(T item)
        {
            if (item == null)
            {
                throw new ArgumentNullException("item", "'item' cannot be null (Nothing in Visual Basic)");
            }

            int low = 0;
            int high = collection.Count - 1;
            int index;
            while (low <= high)
            {
                index = (low + high) / 2;
                int comparison = collection[index].CompareTo(item);
                if (comparison < 0)
                {
                    low = index + 1;
                }
                else if (comparison > 0)
                {
                    high = index - 1;
                }
                else
                {
                    return true;
                }
            }
            return false;
        }

        public void Remove(T item)
        {
            if (item == null)
            {
                throw new ArgumentNullException("item", "'item' cannot be null (Nothing in Visual Basic)");
            }

            int low = 0;
            int high = collection.Count - 1;
            int index;
            while (low <= high)
            {
                index = (low + high) / 2;
                int comparison = collection[index].CompareTo(item);
                if (comparison < 0)
                {
                    low = index + 1;
                }
                else if (comparison > 0)
                {
                    high = index - 1;
                }
                else
                {
                    collection.RemoveAt(index);
                    return;
                }
            }
        }

        #region IEnumerable<T> Members

        public IEnumerator<T> GetEnumerator()
        {
            foreach (T item in collection)
            {
                yield return item;
            }
        }

        #endregion

        #region IEnumerable Members

        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
        {
            foreach (T item in collection)
            {
                yield return item;
            }
        }

        #endregion
    }
}
