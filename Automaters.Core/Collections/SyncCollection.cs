using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Automaters.Core.Collections
{

    /// <summary>
    /// Synchronized collection
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class SyncCollection<T> : ICollection<T>
    {

        #region Constructors

        /// <summary>
        /// Initializes a new instance of the <see cref="SyncCollection&lt;T&gt;"/> class.
        /// </summary>
        public SyncCollection()
            : this(new List<T>())
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="SyncCollection&lt;T&gt;"/> class.
        /// </summary>
        /// <param name="capacity">The capacity.</param>
        public SyncCollection(int capacity)
            : this(new List<T>(capacity))
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="SyncCollection&lt;T&gt;"/> class.
        /// </summary>
        /// <param name="collection">The collection.</param>
        public SyncCollection(IEnumerable<T> collection)
            : this(new List<T>(collection))
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="SyncCollection&lt;T&gt;"/> class.
        /// </summary>
        /// <param name="list">The list.</param>
        protected SyncCollection(List<T> list)
        {
            if (list == null)
                throw new ArgumentNullException();

            this.InternalList = list;
        }

        #endregion

        #region ICollection Implementation

        /// <summary>
        /// Gets the internal list.
        /// </summary>
        protected List<T> InternalList
        {
            get;
            private set;
        }

        /// <summary>
        /// Adds an item to the <see cref="T:System.Collections.Generic.ICollection`1"/>.
        /// </summary>
        /// <param name="item">The object to add to the <see cref="T:System.Collections.Generic.ICollection`1"/>.</param>
        /// <exception cref="T:System.NotSupportedException">
        /// The <see cref="T:System.Collections.Generic.ICollection`1"/> is read-only.
        ///   </exception>
        public void Add(T item)
        {
            lock (this.InternalList)
            {
                this.InternalList.Add(item);
            }
        }

        /// <summary>
        /// Removes all items from the <see cref="T:System.Collections.Generic.ICollection`1"/>.
        /// </summary>
        /// <exception cref="T:System.NotSupportedException">
        /// The <see cref="T:System.Collections.Generic.ICollection`1"/> is read-only.
        ///   </exception>
        public void Clear()
        {
            lock (this.InternalList)
            {
                this.InternalList.Clear();
            }
        }

        /// <summary>
        /// Determines whether the <see cref="T:System.Collections.Generic.ICollection`1"/> contains a specific value.
        /// </summary>
        /// <param name="item">The object to locate in the <see cref="T:System.Collections.Generic.ICollection`1"/>.</param>
        /// <returns>
        /// true if <paramref name="item"/> is found in the <see cref="T:System.Collections.Generic.ICollection`1"/>; otherwise, false.
        /// </returns>
        public bool Contains(T item)
        {
            lock (this.InternalList)
            {
                return this.InternalList.Contains(item);
            }
        }

        /// <summary>
        /// Copies to.
        /// </summary>
        /// <param name="array">The array.</param>
        /// <param name="arrayIndex">Index of the array.</param>
        public void CopyTo(T[] array, int arrayIndex)
        {
            lock (this.InternalList)
            {
                this.InternalList.CopyTo(array, arrayIndex);
            }
        }

        /// <summary>
        /// Gets the number of elements contained in the <see cref="T:System.Collections.Generic.ICollection`1"/>.
        /// </summary>
        /// <returns>
        /// The number of elements contained in the <see cref="T:System.Collections.Generic.ICollection`1"/>.
        ///   </returns>
        public int Count
        {
            get
            {
                lock (this.InternalList)
                {
                    return this.InternalList.Count;
                }
            }
        }

        /// <summary>
        /// Gets a value indicating whether the <see cref="T:System.Collections.Generic.ICollection`1"/> is read-only.
        /// </summary>
        /// <returns>true if the <see cref="T:System.Collections.Generic.ICollection`1"/> is read-only; otherwise, false.
        ///   </returns>
        public bool IsReadOnly
        {
            get { return false; }
        }

        /// <summary>
        /// Removes the first occurrence of a specific object from the <see cref="T:System.Collections.Generic.ICollection`1"/>.
        /// </summary>
        /// <param name="item">The object to remove from the <see cref="T:System.Collections.Generic.ICollection`1"/>.</param>
        /// <returns>
        /// true if <paramref name="item"/> was successfully removed from the <see cref="T:System.Collections.Generic.ICollection`1"/>; otherwise, false. This method also returns false if <paramref name="item"/> is not found in the original <see cref="T:System.Collections.Generic.ICollection`1"/>.
        /// </returns>
        /// <exception cref="T:System.NotSupportedException">
        /// The <see cref="T:System.Collections.Generic.ICollection`1"/> is read-only.
        ///   </exception>
        public bool Remove(T item)
        {
            lock (this.InternalList)
            {
                return this.InternalList.Remove(item);
            }
        }

        /// <summary>
        /// Returns an enumerator that iterates through the collection.
        /// </summary>
        /// <returns>
        /// A <see cref="T:System.Collections.Generic.IEnumerator`1"/> that can be used to iterate through the collection.
        /// </returns>
        public IEnumerator<T> GetEnumerator()
        {
            lock (this.InternalList)
            {
                foreach (T item in this.InternalList)
                    yield return item;
            }
        }

        /// <summary>
        /// Returns an enumerator that iterates through a collection.
        /// </summary>
        /// <returns>
        /// An <see cref="T:System.Collections.IEnumerator"/> object that can be used to iterate through the collection.
        /// </returns>
        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
        {
            return (this as IEnumerable<T>).GetEnumerator();
        }

        #endregion

    }

}
