using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Windows.Controls;

namespace Db2Source
{
    public class TreeViewStatusStore
    {
        private Dictionary<string, bool> _expansion;
        private string _selectedPath;
        public bool IsEmpty { get; private set; }
        private static string EscapeForPath(string path)
        {
            StringBuilder buf = new StringBuilder();
            foreach (char c in path)
            {
                if ("\\\"".IndexOf(c) != -1)
                {
                    buf.Append('u');
                    foreach (byte b in Encoding.UTF8.GetBytes(new char[] { c }))
                    {
                        buf.AppendFormat(b.ToString("X2"));
                    }
                }
                else
                {
                    buf.Append(c);
                }
            }
            return buf.ToString();
        }
        private void SaveExpansionRecursive(TreeViewItem topItem, string basePath)
        {
            string path = Path.Combine(basePath, EscapeForPath(topItem.Header.ToString()));
            _expansion[path] = topItem.IsExpanded;
            foreach (TreeViewItem v in topItem.Items)
            {
                SaveExpansionRecursive(v, path);
            }
        }

        private static void GetTreeViewItemPathRecursive(StringBuilder buffer, TreeViewItem current, TreeViewItem topItem)
        {
            if (current == null || current == topItem)
            {
                return;
            }
            GetTreeViewItemPathRecursive(buffer, current.Parent as TreeViewItem, topItem);
            buffer.Append(Path.DirectorySeparatorChar);
            buffer.Append(current.Header.ToString());
        }

        private static TreeViewItem GetTreeViewItemByPath(string path, TreeViewItem topItem)
        {
            TreeViewItem sel = topItem;
            string[] items = path.Split(Path.DirectorySeparatorChar);
            foreach (string s in items)
            {
                if (string.IsNullOrEmpty(s))
                {
                    continue;
                }
                bool found = false;
                foreach (TreeViewItem item in sel.Items)
                {
                    if (item.Header.ToString() == s)
                    {
                        sel = item;
                        found = true;
                        break;
                    }
                }
                if (!found)
                {
                    return null;
                }
            }
            return sel;
        }

        public TreeViewStatusStore(TreeView treeView, TreeViewItem topItem)
        {
            IsEmpty = (topItem.Items.Count == 0);
            _expansion = new Dictionary<string, bool>();
            SaveExpansionRecursive(topItem, string.Empty);
            StringBuilder buf = new StringBuilder();
            GetTreeViewItemPathRecursive(buf, treeView.SelectedItem as TreeViewItem, topItem);
            _selectedPath = buf.ToString();
        }
        private void RestoreExpansionRecursive(TreeViewItem item, string basePath)
        {
            string path = Path.Combine(basePath, EscapeForPath(item.Header.ToString()));
            bool flag;
            if (!_expansion.TryGetValue(path, out flag))
            {
                flag = false;
            }
            item.IsExpanded = flag;
            foreach (TreeViewItem v in item.Items)
            {
                RestoreExpansionRecursive(v, path);
            }
        }
        public void Restore(TreeView treeView, TreeViewItem topItem)
        {
            if (IsEmpty)
            {
                return;
            }
            RestoreExpansionRecursive(topItem, string.Empty);
            TreeViewItem sel = GetTreeViewItemByPath(_selectedPath, topItem);
            if (sel == null)
            {
                return;
            }
            sel.IsSelected = true;
        }
    }
}
