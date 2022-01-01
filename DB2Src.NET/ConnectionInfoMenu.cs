using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;

namespace Db2Source
{
    public static class ConnectionInfoMenu
    {
        private static MenuItem AddMenuItemRecursive(ItemCollection items, string[] paths, int pathIndex)
        {
            if (paths.Length <= pathIndex)
            {
                return null;
            }
            if (pathIndex == 0 && paths.Length == 1 && string.IsNullOrEmpty(paths[0]))
            {
                return null;
            }
            MenuItem parent = null;
            foreach (MenuItem item in items)
            {
                if (item.Header.ToString() == paths[pathIndex])
                {
                    parent = item;
                    break;
                }
            }
            if (parent == null)
            {
                parent = new MenuItem() { Header = paths[pathIndex] };
                items.Add(parent);
            }
            MenuItem child = AddMenuItemRecursive(parent.Items, paths, pathIndex + 1);
            return child ?? parent;
        }
        private static void AddMenuItemToContextMenu(ItemCollection menuItems, ConnectionInfo info, RoutedEventHandler clickEvent)
        {
            string[] paths = info.CategoryPath.Split('\\');
            MenuItem parentItem = AddMenuItemRecursive(menuItems, paths, 0);
            if (parentItem != null)
            {
                menuItems = parentItem.Items;
            }
            MenuItem item = new MenuItem() { Header = info.Name, Tag = info };
            item.Click += clickEvent;
            menuItems.Add(item);
        }

        private static void TryCompact(ItemCollection menuItems, bool isTop)
        {
            for (int i = 0; i < menuItems.Count; i++)
            {
                MenuItem item = menuItems[i] as MenuItem;
                if (item == null)
                {
                    continue;
                }
                TryCompact(item.Items, false);
                if (item.Items.Count == 1)
                {
                    MenuItem subItem = item.Items[0] as MenuItem;
                    if (subItem != null && !subItem.HasItems)
                    {
                        if (isTop)
                        {
                            subItem.Header = string.Format("{0} > {1}", item.Header, subItem.Header);
                        }
                        item.Items.Remove(subItem);
                        menuItems[i] = subItem;
                    }
                }
            }
        }
        public static void AddMenuItem(ItemCollection menuItems, IEnumerable<ConnectionInfo> connectionInfos, RoutedEventHandler clickEvent)
        {
            List<ConnectionInfo> l = new List<ConnectionInfo>(connectionInfos);
            l.Sort(ConnectionInfo.CompareByCategory);
            foreach (ConnectionInfo info in l)
            {
                AddMenuItemToContextMenu(menuItems, info, clickEvent);
            }
            TryCompact(menuItems, true);

        }
    }
}
