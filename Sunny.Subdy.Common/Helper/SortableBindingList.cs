using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Sunny.Subdy.Common.Helper
{
    public class SortableBindingList<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)] T> : BindingList<T>
    {
        private bool isSorted;
        private ListSortDirection sortDirection;
        private PropertyDescriptor sortProperty;

        /// <summary>
        /// Optional hook: called before an item is removed.
        /// Set this to ThrottledPropertyNotifier.Unregister to prevent stale
        /// PropertyChanged events from crashing DataGridView after item removal.
        /// </summary>
        public static Action<INotifyPropertyChanged>? OnBeforeRemove { get; set; }

        public SortableBindingList() : base(new List<T>())
        {
        }

        public SortableBindingList(IEnumerable<T> list) : base(new List<T>(list))
        {
        }
        protected override bool SupportsSortingCore => true;
        protected override bool IsSortedCore => isSorted;
        protected override PropertyDescriptor SortPropertyCore => sortProperty;
        protected override ListSortDirection SortDirectionCore => sortDirection;

        protected override void ApplySortCore(PropertyDescriptor prop, ListSortDirection direction)
        {
            var itemsList = (List<T>)Items;
            itemsList.Sort((x, y) =>
            {
                var xValue = prop.GetValue(x);
                var yValue = prop.GetValue(y);
                return direction == ListSortDirection.Ascending
                    ? Comparer<object>.Default.Compare(xValue, yValue)
                    : Comparer<object>.Default.Compare(yValue, xValue);
            });

            sortProperty = prop;
            sortDirection = direction;
            isSorted = true;
            OnListChanged(new ListChangedEventArgs(ListChangedType.Reset, -1));
        }

        protected override void RemoveSortCore()
        {
            isSorted = false;
        }

        /// <summary>
        /// Calls OnBeforeRemove hook before removing item so callers can unregister
        /// from ThrottledPropertyNotifier without creating a circular dependency.
        /// </summary>
        protected override void RemoveItem(int index)
        {
            if (OnBeforeRemove != null && this[index] is INotifyPropertyChanged owner)
                OnBeforeRemove(owner);
            base.RemoveItem(index);
        }

        /// <summary>
        /// Override ClearItems so OnBeforeRemove fires for every item before the list is cleared.
        /// BindingList.Clear() calls ClearItems() directly, bypassing RemoveItem(), so without
        /// this override the Unregister hook would never fire on Clear().
        /// </summary>
        protected override void ClearItems()
        {
            if (OnBeforeRemove != null)
            {
                foreach (var item in Items)
                {
                    if (item is INotifyPropertyChanged owner)
                        OnBeforeRemove(owner);
                }
            }
            base.ClearItems();
        }
    }
    public static class EnumerableExtensions
    {
        public static void ForEach<T>(this IEnumerable<T> source, Action<T> action)
        {
            foreach (var item in source)
                action(item);
        }
    }
}
