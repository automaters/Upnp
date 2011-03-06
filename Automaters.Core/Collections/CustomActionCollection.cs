using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Automaters.Core.Collections
{
    public class CustomActionCollection<T> : ICollection<T>
    {

        public CustomActionCollection(Action<T> itemAdded, Action<T> itemRemoved)
        {
            this.InternalList = new List<T>();
            this.ItemAdded = itemAdded;
            this.ItemRemoved = itemRemoved;
        }

        protected List<T> InternalList
        {
            get;
            private set;
        }

        protected Action<T> ItemAdded
        {
            get;
            set;
        }

        protected Action<T> ItemRemoved
        {
            get;
            set;
        }

        protected virtual void OnItemAdded(T item)
        {
            var action = this.ItemAdded;
            if (action != null)
                action(item);
        }

        protected virtual void OnItemRemoved(T item)
        {
            var action = this.ItemRemoved;
            if (action != null)
                action(item);
        }

        public void Add(T item)
        {
            this.InternalList.Add(item);
            this.OnItemAdded(item);
        }

        public void Clear()
        {
            List<T> temp = new List<T>(this.InternalList);
            this.InternalList.Clear();

            foreach (var item in temp)
                this.OnItemRemoved(item);
        }

        public bool Contains(T item)
        {
            return this.InternalList.Contains(item);
        }

        public void CopyTo(T[] array, int arrayIndex)
        {
            this.InternalList.CopyTo(array, arrayIndex);
        }

        public int Count
        {
            get { return this.InternalList.Count; }
        }

        public bool IsReadOnly
        {
            get { return false; }
        }

        public bool Remove(T item)
        {
            var value = this.InternalList.Remove(item);
            this.OnItemRemoved(item);
            return value;
        }

        public IEnumerator<T> GetEnumerator()
        {
            return this.InternalList.GetEnumerator();
        }

        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
        {
            return this.InternalList.GetEnumerator();
        }
    }
}
