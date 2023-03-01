using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Automation.Peers;
using System.Windows.Automation.Provider;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;

namespace Db2Source
{
    public class TreeViewUtil
    {
        //private TreeView _target;

        private static ItemCollection GetOwnerCollection(TreeViewItem item)
        {
            if (item.Parent is TreeViewItem)
            {
                return ((TreeViewItem)item.Parent).Items;
            }
            if (item.Parent is TreeView)
            {
                return ((TreeView)item.Parent).Items;
            }
            throw new NotImplementedException();
        }

        private static TreeViewItem GetPreviousVisibleItem(ItemCollection items, int index)
        {
            for (int i = index - 1; 0 <= i; i--)
            {
                TreeViewItem item = items[i] as TreeViewItem;
                if (item.IsVisible)
                {
                    return item;
                }
            }
            return null;
        }

        private static TreeViewItem GetTailTreeViewItem(TreeViewItem item)
        {
            if (item.IsVisible && !(item.HasItems && item.IsExpanded))
            {
                return item;
            }
            TreeViewItem ret = GetPreviousVisibleItem(item.Items, item.Items.Count);
            return GetTailTreeViewItem(ret);
        }

        private static TreeViewItem GetNextVisibleItem(ItemCollection items, int index)
        {
            for (int i = index + 1, n = items.Count; i < n; i++)
            {
                TreeViewItem item = items[i] as TreeViewItem;
                if (item.IsVisible)
                {
                    return item;
                }
            }
            return null;
        }

        private static TreeViewItem GetNextVisibleItem(TreeViewItem item)
        {
            ItemCollection items = GetOwnerCollection(item);
            return GetNextVisibleItem(items, items.IndexOf(item));
        }

        /// <summary>
        /// itemの親アイテムを返す
        /// </summary>
        /// <param name="item"></param>
        /// <returns></returns>
        public static TreeViewItem GetParentTreeViewItem(TreeViewItem item)
        {
            if (item == null)
            {
                return null;
            }
            return item.Parent as TreeViewItem;
        }

        /// <summary>
        /// treeViewで現在選択されているアイテムの親アイテムを返す
        /// </summary>
        /// <param name="treeView"></param>
        /// <returns></returns>
        public static TreeViewItem GetParentTreeViewItem(TreeView treeView)
        {
            TreeViewItem item = treeView.SelectedItem as TreeViewItem;
            if (item == null)
            {
                return null;
            }
            return item.Parent as TreeViewItem;
        }

        /// <summary>
        /// treeViewで選択されているアイテムの親アイテムを選択する
        /// </summary>
        /// <param name="treeView"></param>
        public static void SelectParentTreeViewItem(TreeView treeView)
        {
            TreeViewItem item = GetParentTreeViewItem(treeView);
            if (item == null)
            {
                return;
            }
            item.IsSelected = true;
            (new TreeViewItemAutomationPeer(item) as IScrollItemProvider).ScrollIntoView();
        }

        /// <summary>
        /// itemの直前に表示されているアイテムを返す
        /// </summary>
        /// <param name="item"></param>
        /// <returns></returns>
        public static TreeViewItem GetPreviousSiblingTreeViewItem(TreeViewItem item)
        {
            if (item == null)
            {
                return null;
            }
            ItemCollection items = GetOwnerCollection(item);
            TreeViewItem ret = GetPreviousVisibleItem(items, items.IndexOf(item));
            if (ret != null)
            {
                return GetTailTreeViewItem(ret);
            }
            return GetParentTreeViewItem(item);
        }

        /// <summary>
        /// treeViewで選択されたアイテムの直前に表示されているアイテムを返す
        /// </summary>
        /// <param name="treeView"></param>
        /// <returns></returns>
        public static TreeViewItem GetPreviousSiblingTreeViewItem(TreeView treeView)
        {
            if (treeView == null)
            {
                throw new ArgumentNullException("treeView");
            }
            return GetPreviousSiblingTreeViewItem(treeView.SelectedItem as TreeViewItem);
        }

        /// <summary>
        /// treeViewで選択されたアイテムの直前に表示されているアイテムを選択する
        /// </summary>
        /// <param name="treeView"></param>
        public static void SelectPreviousSiblingTreeViewItem(TreeView treeView)
        {
            if (treeView == null)
            {
                throw new ArgumentNullException("treeView");
            }
            TreeViewItem item;
            if (treeView.SelectedItem == null)
            {
                item = GetNextVisibleItem(treeView.Items, -1);
                if (item != null)
                {
                    item.IsSelected = true;
                    (new TreeViewItemAutomationPeer(item) as IScrollItemProvider).ScrollIntoView();
                }
                return;
            }
            item = GetPreviousSiblingTreeViewItem(treeView);
            if (item == null)
            {
                return;
            }
            item.IsSelected = true;
            (new TreeViewItemAutomationPeer(item) as IScrollItemProvider).ScrollIntoView();
        }

        /// <summary>
        /// itemの直後に表示されているアイテムを返す
        /// </summary>
        /// <param name="item"></param>
        /// <returns></returns>
        public static TreeViewItem GetNextSiblingTreeViewItem(TreeViewItem item)
        {
            if (item == null)
            {
                return null;
            }
            TreeViewItem ret;
            if (item.IsVisible && item.HasItems && item.IsExpanded)
            {
                ret = GetNextVisibleItem(item.Items, -1);
                if (ret != null)
                {
                    return ret;
                }
            }
            ItemCollection items = GetOwnerCollection(item);
            ret = GetNextVisibleItem(item);
            if (ret != null)
            {
                return ret;
            }
            TreeViewItem parent = GetParentTreeViewItem(item);
            while (parent != null)
            {
                ret = GetNextVisibleItem(parent);
                if (ret != null)
                {
                    return ret;
                }
                parent = GetParentTreeViewItem(parent);
            }
            return null;
        }

        /// <summary>
        /// treeViewで選択されているアイテムの直後に表示されているアイテムを返す
        /// </summary>
        /// <param name="treeView"></param>
        /// <returns></returns>
        public static TreeViewItem GetNextSiblingTreeViewItem(TreeView treeView)
        {
            if (treeView == null)
            {
                throw new ArgumentNullException("treeView");
            }
            return GetNextSiblingTreeViewItem(treeView.SelectedItem as TreeViewItem);
        }

        /// <summary>
        /// treeViewで選択されているアイテムの直後のアイテムを選択する
        /// </summary>
        /// <param name="treeView"></param>
        public static void SelectNextSiblingTreeViewItem(TreeView treeView)
        {
            if (treeView == null)
            {
                throw new ArgumentNullException("treeView");
            }
            TreeViewItem item;
            if (treeView.SelectedItem == null)
            {
                item = GetNextVisibleItem(treeView.Items, -1);
                if (item != null)
                {
                    item.IsSelected = true;
                    (new TreeViewItemAutomationPeer(item) as IScrollItemProvider).ScrollIntoView();
                }
                return;
            }
            item = GetNextSiblingTreeViewItem(treeView);
            if (item == null)
            {
                return;
            }
            item.IsSelected = true;
            (new TreeViewItemAutomationPeer(item) as IScrollItemProvider).ScrollIntoView();
        }

        public static void BecomeExpanded(TreeView treeView, TreeViewItem item)
        {
            TreeViewItem parent = item.Parent as TreeViewItem;
            if (parent == null)
            {
                return;
            }
            if (!parent.IsExpanded)
            {
                parent.IsExpanded = true;
            }
            BecomeExpanded(treeView, parent);
        }
    }
    public class TreeViewFilterKeyEventController
    {
        private TreeView _treeView;
        private TextBox _filterTextBox;
        private void FilterTextBox_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            TreeViewItem sel = _treeView.SelectedItem as TreeViewItem;
            KeyEventArgs e2;
            switch (e.Key)
            {
                case Key.Up:
                    TreeViewUtil.SelectPreviousSiblingTreeViewItem(_treeView);
                    e.Handled = true;
                    break;
                case Key.Down:
                    TreeViewUtil.SelectNextSiblingTreeViewItem(_treeView);
                    e.Handled = true;
                    break;
                case Key.Left:
                    if (_filterTextBox.SelectionStart != 0 || _filterTextBox.SelectionLength != 0)
                    {
                        break;
                    }
                    if (sel != null && sel.IsExpanded)
                    {
                        sel.IsExpanded = false;
                    }
                    else
                    {
                        TreeViewUtil.SelectParentTreeViewItem(_treeView);
                    }
                    e.Handled = true;
                    break;
                case Key.Right:
                    if (_filterTextBox.SelectionStart < _filterTextBox.Text.Length)
                    {
                        break;
                    }
                    if (sel != null && sel.HasItems)
                    {
                        sel.IsExpanded = true;
                    }
                    e.Handled = true;
                    break;
                case Key.PageUp:
                case Key.PageDown:
                    _treeView.RaiseEvent(e);
                    e2 = new KeyEventArgs(e.KeyboardDevice, e.InputSource, e.Timestamp, e.Key)
                    {
                        RoutedEvent = UIElement.KeyDownEvent
                    };
                    _treeView.RaiseEvent(e2);
                    e.Handled = e2.Handled;
                    _filterTextBox.Focus();
                    break;
            }
        }

        private void FilterTextBox_PreviewKeyUp(object sender, KeyEventArgs e)
        {
            TreeViewItem sel = _treeView.SelectedItem as TreeViewItem;
            KeyEventArgs e2;
            switch (e.Key)
            {
                case Key.PageUp:
                case Key.PageDown:
                    _treeView.RaiseEvent(e);
                    e2 = new KeyEventArgs(e.KeyboardDevice, e.InputSource, e.Timestamp, e.Key)
                    {
                        RoutedEvent = UIElement.KeyUpEvent
                    };
                    _treeView.RaiseEvent(e2);
                    e.Handled = e2.Handled;
                    break;
            }
        }
        public TreeViewFilterKeyEventController(TreeView treeView, TextBox filterTextBox)
        {
            _treeView = treeView;
            _filterTextBox = filterTextBox;
            _filterTextBox.PreviewKeyDown += FilterTextBox_PreviewKeyDown;
            _filterTextBox.PreviewKeyUp += FilterTextBox_PreviewKeyUp;
        }
    }
}
