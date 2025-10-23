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

        // ✅ Thêm constructor này để nhận danh sách ban đầu
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
    }
}
