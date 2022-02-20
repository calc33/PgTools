using System;
using System.Collections;
using System.Collections.ObjectModel;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Threading;

namespace Db2Source
{
    /// <summary>
    /// 各要素の幅を均等にするStackPanel
    /// </summary>
    public class EquallyStackPanel: StackPanel
    {
        protected override Size MeasureOverride(Size availableSize)
        {
            double w = 0;
            Size maxSize = new Size(double.PositiveInfinity, double.PositiveInfinity);
            foreach (FrameworkElement element in Children)
            {
                element.ClearValue(WidthProperty);
                element.Measure(maxSize);
                w = Math.Max(w, element.DesiredSize.Width);
            }
            foreach (FrameworkElement element in Children)
            {
                element.Width = w;
            }
            Size s = base.MeasureOverride(availableSize);
            return s;
        }
    }
}
