using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;

namespace Db2Source
{
    public class AxisCollection : ObservableCollection<Axis>
    {
        private void Invalidate()
        {
            foreach (Axis axis in Items)
            {
                axis.InvalidateIndex();
            }
        }
        protected override void OnCollectionChanged(NotifyCollectionChangedEventArgs e)
        {
            Invalidate();
            base.OnCollectionChanged(e);
        }
        protected override void OnPropertyChanged(PropertyChangedEventArgs e)
        {
            Invalidate();
            base.OnPropertyChanged(e);
        }
    }
}
