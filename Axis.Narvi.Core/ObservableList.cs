using Axis.Luna.Extensions;
using Axis.Narvi.Core.Notify;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;

namespace Axis.Narvi.Core
{

    public class ObservableList<T> : NotifiableBase, IList<T>, INotifyCollectionChanged
    {
        public ObservableList() : this(new List<T>())
        { }
        public ObservableList(IEnumerable<T> collection)
        {
            this.InternalList = new List<T>(collection);
            this.IsNotificationEnabled = true;
        }

        public ObservableList(int capacity) : this(new List<T>(capacity))
        { }

        private List<T> InternalList { get; set; }

        public bool IsNotificationEnabled { get; set; }


        private void NotifyCollection(NotifyCollectionChangedEventArgs args)
        {
            if (_collectionChanged == null) return;

            _collectionChanged.Invoke(this, args);
        }

        #region IList<T> Members

        public int IndexOf(T item)
        {
            return this.InternalList.IndexOf(item);
        }

        public void Insert(int index, T item)
        {
            this.InternalList.Insert(index, item);

            if (IsNotificationEnabled)
            {
                this.NotifyCollection(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Add, item, index));
                this.Notify(() => this.Count);
            }
        }

        public void RemoveAt(int index)
        {
            var olditem = this[index];
            this.InternalList.RemoveAt(index);

            if (IsNotificationEnabled)
            {
                this.NotifyCollection(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Remove, olditem, index));
                this.Notify(() => this.Count);
            }
        }

        public T this[int index]
        {
            get { return this.InternalList[index]; }
            set
            {
                var old = this.InternalList[index];
                this.InternalList[index] = value;

                if (this.IsNotificationEnabled)
                {
                    this.NotifyCollection(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Replace,
                                                                               value.Enumerate().ToList(),
                                                                               old.Enumerate().ToList()));
                }
            }
        }

        #endregion

        #region ICollection<T> Members

        public void Add(T item)
        {
            this.InternalList.Add(item);

            if (IsNotificationEnabled)
            {
                this.NotifyCollection(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Add, item));
                this.Notify(() => this.Count);
            }
        }

        public void Clear()
        {
            var oldItems = new List<T>(this.InternalList);
            this.InternalList.Clear();

            if (IsNotificationEnabled)
            {
                this.NotifyCollection(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Remove, oldItems, 0));
                this.Notify(() => this.Count);
            }
        }

        public bool Contains(T item)
        {
            return InternalList.Contains(item);
        }

        public void CopyTo(T[] array, int arrayIndex)
        {
            this.InternalList.CopyTo(array, arrayIndex);
        }

        public int Count { get { return InternalList.Count; } }

        public bool IsReadOnly { get { return false; } }

        public bool Remove(T item)
        {
            var indx = IndexOf(item);
            if (indx < 0 || indx >= this.Count) return false;

            RemoveAt(indx);
            return true;
        }

        #endregion

        #region IEnumerable<T> Members

        public IEnumerator<T> GetEnumerator()
        {
            return InternalList.GetEnumerator();
        }

        #endregion

        #region IEnumerable Members

        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
        {
            return InternalList.GetEnumerator();
        }

        #endregion

        #region INotifyCollectionChanged Members

        private NotifyCollectionChangedEventHandler _collectionChanged;
        public event NotifyCollectionChangedEventHandler CollectionChanged
        {
            add
            {
                _collectionChanged += new ManagedCallback<NotifyCollectionChangedEventHandler, NotifyCollectionChangedEventArgs>(value, d => _collectionChanged -= d.Invoke).Invoke;
            }
            remove
            {
                _collectionChanged = ManagedCallback.RemoveWeakCallback(_collectionChanged, value);
            }
        }

        #endregion


        #region List Parallels

        public int BinarySearch(T item)
        {
            return InternalList.BinarySearch(item);
        }
        public int BinarySearch(T item, IComparer<T> comparer)
        {
            return InternalList.BinarySearch(item, comparer);
        }
        public int BinarySearch(int index, int count, T item, IComparer<T> comparer)
        {
            return InternalList.BinarySearch(index, count, item, comparer);
        }


        public bool Exists(Predicate<T> match)
        {
            return InternalList.Exists(match);
        }

        public T Find(Predicate<T> match)
        {
            return InternalList.Find(match);
        }
        public List<T> FindAll(Predicate<T> match)
        {
            return InternalList.FindAll(match);
        }
        public int FindIndex(Predicate<T> match)
        {
            return InternalList.FindIndex(match);
        }
        public int FindIndex(int startIndex, Predicate<T> match)
        {
            return InternalList.FindIndex(startIndex, match);
        }
        public int FindIndex(int startIndex, int count, Predicate<T> match)
        {
            return InternalList.FindIndex(startIndex, count, match);
        }
        public T FindLast(Predicate<T> match)
        {
            return InternalList.FindLast(match);
        }
        public int FindLastIndex(Predicate<T> match)
        {
            return InternalList.FindLastIndex(match);
        }
        public int FindLastIndex(int startIndex, Predicate<T> match)
        {
            return InternalList.FindLastIndex(startIndex, match);
        }
        public void ForEach(Action<T> action)
        {
            InternalList.ForEach(action);
        }
        public void ForEach(Action<int, T> action)
        {
            for (int cnt = 0; cnt < this.InternalList.Count; cnt++) action(cnt, this[cnt]);
        }
        public List<T> GetRange(int index, int count)
        {
            return InternalList.GetRange(index, count);
        }
        public int IndexOf(T item, int index)
        {
            return InternalList.IndexOf(item, index);
        }
        public int IndexOf(T item, int index, int count)
        {
            return InternalList.IndexOf(item, index, count);
        }
        public void InsertRange(int index, IEnumerable<T> collection)
        {
            var oldCount = this.Count;
            var shiftCount = oldCount - index;
            InternalList.InsertRange(index, collection);
            if (IsNotificationEnabled)
            {
                var diff = this.Count - oldCount;
                //range to be shifted
                var shiftedRange = InternalList.GetRange(index + diff, shiftCount);

                //add event
                this.NotifyCollection(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Add,
                                                                           collection.ToList()));

                //move event
                this.NotifyCollection(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Move,
                                                                           shiftedRange,
                                                                           index + diff,
                                                                           index));

                this.Notify(() => this.Count);
            }

        }
        public int LastIndexOf(T item)
        {
            return InternalList.LastIndexOf(item);
        }
        public int LastIndexOf(T item, int index)
        {
            return InternalList.LastIndexOf(item, index);
        }
        public int LastIndexOf(T item, int index, int count)
        {
            return InternalList.LastIndexOf(item, index, count);
        }
        public int RemoveAll(Predicate<T> match)
        {
            var list = new List<int>();
            for (int cnt = this.Count - 1; cnt >= 0; cnt--)
            {
                if (match(this[cnt])) list.Add(cnt);
            }

            var removed = 0;
            list.ForEach(index =>
            {
                this.RemoveAt(index);
                removed++;
            });

            return removed;
        }
        public void RemoveRange(int index, int count)
        {
            var range = InternalList.GetRange(index, count);
            InternalList.RemoveRange(index, count);

            if (IsNotificationEnabled)
            {
                this.NotifyCollection(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Remove, range));
                this.Notify(() => this.Count);
            }
        }
        public void Reverse(int index, int count)
        {
            InternalList.Reverse(index, count);

            if (IsNotificationEnabled)
                this.NotifyCollection(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset));
        }
        public void Sort()
        {
            InternalList.Sort();

            if (IsNotificationEnabled)
                this.NotifyCollection(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset));
        }
        public void Sort(Comparison<T> comparison)
        {
            InternalList.Sort(comparison);

            if (IsNotificationEnabled)
                this.NotifyCollection(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset));
        }
        public void Sort(IComparer<T> comparer)
        {
            InternalList.Sort(comparer);

            if (IsNotificationEnabled)
                this.NotifyCollection(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset));
        }
        public T[] ToArray()
        {
            return InternalList.ToArray();
        }
        public void TrimExcess()
        {
            InternalList.TrimExcess();
        }
        public bool TrueForAll(Predicate<T> match)
        {
            return InternalList.TrueForAll(match);
        }
        #endregion
    }
}
