using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;

namespace Db2Source
{
    public static class ConnectionInfoTreeView
    {
        public class ConnectionInfoGroup
        {
            public string Name { get; set; }
            public bool IsLastConnectionFailed { get; set; }
        }
        private static TreeViewItem AddTreeViewItemRecursive(ItemCollection items, string[] paths, int pathIndex)
        {
            if (paths.Length <= pathIndex)
            {
                return null;
            }
            if (pathIndex == 0 && paths.Length == 1 && string.IsNullOrEmpty(paths[0]))
            {
                return null;
            }
            TreeViewItem parent = null;
            foreach (object o in items)
            {
                TreeViewItem item = o as TreeViewItem;
                if (item == null)
                {
                    continue;
                }
                if (item.Header.ToString() == paths[pathIndex])
                {
                    parent = item;
                    break;
                }
            }
            if (parent == null)
            {
                ConnectionInfoGroup group = new ConnectionInfoGroup() { Name = paths[pathIndex], IsLastConnectionFailed = false };
                parent = new TreeViewItem() { Header = group.Name, Tag = group };
                items.Add(parent);
            }
            TreeViewItem child = AddTreeViewItemRecursive(parent.Items, paths, pathIndex + 1);
            return child ?? parent;
        }
        private static void AddTreeViewItemToTreeView(ItemCollection TreeViewItems, ConnectionInfo info/*, RoutedEventHandler clickEvent*/)
        {
            string[] paths = info.CategoryPath.Split('\\');
            TreeViewItem parentItem = AddTreeViewItemRecursive(TreeViewItems, paths, 0);
            if (parentItem != null)
            {
                TreeViewItems = parentItem.Items;
            }
            TreeViewItem item = new TreeViewItem() { Header = info.Name, Tag = info };
            //item.Click += clickEvent;
            TreeViewItems.Add(item);
        }

        private static void TryCompact(ItemCollection TreeViewItems, bool isTop)
        {
            for (int i = 0; i < TreeViewItems.Count; i++)
            {
                TreeViewItem item = TreeViewItems[i] as TreeViewItem;
                if (item == null)
                {
                    continue;
                }
                TryCompact(item.Items, false);
                if (item.Items.Count == 1)
                {
                    TreeViewItem subItem = item.Items[0] as TreeViewItem;
                    if (subItem != null && !subItem.HasItems)
                    {
                        item.Items.Remove(subItem);
                        TreeViewItems[i] = subItem;
                    }
                }
            }
        }
        public static void AddTreeViewItem(ItemCollection TreeViewItems, IEnumerable<ConnectionInfo> connectionInfos/*, RoutedEventHandler clickEvent*/)
        {
            List<ConnectionInfo> l = new List<ConnectionInfo>(connectionInfos);
            l.Sort(ConnectionInfo.CompareByCategory);
            foreach (ConnectionInfo info in l)
            {
                AddTreeViewItemToTreeView(TreeViewItems, info/*, clickEvent*/);
            }
            TryCompact(TreeViewItems, true);
        }
    }
}
