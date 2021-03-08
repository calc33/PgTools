using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;

namespace Db2Source
{
    class GridSplitterEx: GridSplitter
    {
        private Grid _ownerGrid;
        private Orientation _orientation;
        private GridResizeBehavior _actualResizeBehavior;
        private void UpdateOwnerGrid()
        {
            Grid g = App.FindVisualParent<Grid>(this);
            if (_ownerGrid == g)
            {
                return;
            }
            if (_ownerGrid != null)
            {
                _ownerGrid.LayoutUpdated -= OwnerGrid_LayoutUpdated;
            }
            if (g != null)
            {
                g.LayoutUpdated += OwnerGrid_LayoutUpdated;
            }
            _ownerGrid = g;
        }

        private void UpdateOrientation()
        {
            if (HorizontalAlignment == HorizontalAlignment.Stretch)
            {
                _orientation = Orientation.Vertical;
            }
            else
            {
                _orientation = Orientation.Horizontal;
            }
        }

        private GridResizeBehavior GetActualResizeBehavior()
        {
            if (ResizeBehavior != GridResizeBehavior.BasedOnAlignment)
            {
                return ResizeBehavior;
            }
            throw new NotImplementedException();
        }

        private void OwnerGrid_LayoutUpdated(object sender, EventArgs e)
        {
        }

        protected override void OnInitialized(EventArgs e)
        {
            base.OnInitialized(e);
            UpdateOwnerGrid();
        }
        protected override void OnVisualParentChanged(DependencyObject oldParent)
        {
            base.OnVisualParentChanged(oldParent);
            UpdateOwnerGrid();
        }
    }
}
