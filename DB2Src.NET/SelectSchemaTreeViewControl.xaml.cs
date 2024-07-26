using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace Db2Source
{
    /// <summary>
    /// SelectSchemaTreeViewControl.xaml の相互作用ロジック
    /// </summary>
    public partial class SelectSchemaTreeViewControl : UserControl
    {
        public static readonly DependencyProperty CurrentDataSetProperty = DependencyProperty.Register("CurrentDataSet", typeof(Db2SourceContext), typeof(SelectSchemaTreeViewControl), new PropertyMetadata(new PropertyChangedCallback(OnCurrentDataSetPropertyChanged)));
        public Db2SourceContext CurrentDataSet
        {
            get { return (Db2SourceContext)GetValue(CurrentDataSetProperty); }
            set { SetValue(CurrentDataSetProperty, value); }
        }

        protected void OnCurrentDataSetPropertyChanged(DependencyPropertyChangedEventArgs e)
        {
            if (CurrentDataSet == null)
            {
                return;
            }
            //UpdateSchema();
            //CurrentDataSet.SchemaObjectReplaced += CurrentDataSet_SchemaObjectReplaced;
            //CurrentDataSet.Log += CurrentDataSet_Log;
            ////CurrentDataSet.SchemaLoaded += CurrentDataSet_SchemaLoaded;
        }

        private static void OnCurrentDataSetPropertyChanged(DependencyObject target, DependencyPropertyChangedEventArgs e)
        {
            (target as SelectSchemaTreeViewControl)?.OnCurrentDataSetPropertyChanged(e);
        }

        public SelectSchemaTreeViewControl()
        {
            InitializeComponent();
        }

        private void SetTreeView(TreeViewItem item, TreeNode node)
        {
            item.Tag = node;
            item.FontWeight = node.IsBold ? FontWeights.Bold : FontWeights.Normal;
            item.Header = node.Name;
            item.Style = FindResource("TreeViewItemStyleSchema") as Style;
            if (node.IsGrouped)
            {
                if (node.TargetType == typeof(Schema))
                {
                    if (node.IsHidden)
                    {
                        item.Style = FindResource("TreeViewItemStyleGrayedSchema") as Style;
                        item.HeaderTemplate = FindResource("ImageHiddenSchema") as DataTemplate;
                    }
                    else
                    {
                        item.HeaderTemplate = FindResource("ImageSchema") as DataTemplate;
                    }
                }
                else if (node.TargetType == typeof(Database))
                {
                    Database db = node.Target as Database;
                    if (db != null && db.IsCurrent)
                    {
                        item.HeaderTemplate = FindResource("ImageDatabase") as DataTemplate;
                        //item.MouseDoubleClick += TreeViewItemDatabase_MouseDoubleClick;
                        item.MouseDoubleClick += TreeViewItem_MouseDoubleClick;
                    }
                }
                else
                {
                    item.HeaderTemplate = FindResource("ImageTables") as DataTemplate;
                }
            }
            else
            {
                item.HeaderTemplate = FindResource("ImageTable") as DataTemplate;
                item.MouseDoubleClick += TreeViewItem_MouseDoubleClick;
            }
            item.ToolTip = node.Hint;
            //Binding b = new Binding("Tag.Target.CommentText");
            //b.RelativeSource = RelativeSource.Self;
            //item.SetBinding(FrameworkElement.ToolTipProperty, b);
            if (node.Children != null)
            {
                foreach (TreeNode chNode in node.Children)
                {
                    TreeViewItem chItem = new TreeViewItem();
                    SetTreeView(chItem, chNode);
                    item.Items.Add(chItem);
                }
            }
        }

        private void AddTreeView(TreeNode[] nodes)
        {
            treeViewItemTop.Header = CurrentDataSet.GetTreeNodeHeader();
            treeViewItemTop.Items.Clear();
            foreach (TreeNode node in nodes)
            {
                TreeViewItem item = new TreeViewItem();
                SetTreeView(item, node);
                treeViewItemTop.Items.Add(item);
            }
            //FilterTreeView(true);
        }

        private void UpdateTreeViewDB()
        {
            if (CurrentDataSet == null)
            {
                return;
            }
            TreeViewStatusStore store = new TreeViewStatusStore(treeViewDB, treeViewItemTop);
            try
            {
                TreeNode[] nodes = CurrentDataSet.GetVisualTree();
                AddTreeView(nodes);
            }
            finally
            {
                treeViewDB.UpdateLayout();
                if (store.IsEmpty)
                {
                    treeViewItemTop.IsExpanded = true;

                    TreeViewItem itemDb = null;
                    foreach (TreeViewItem item in treeViewItemTop.Items)
                    {
                        TreeNode node = item.Tag as TreeNode;
                        Database db = node?.Target as Database;
                        if (db != null && db.IsCurrent)
                        {
                            itemDb = item;
                            itemDb.IsExpanded = true;
                            break;
                        }
                    }
                    string cur = CurrentDataSet.CurrentSchema;
                    if (itemDb != null && !string.IsNullOrEmpty(cur))
                    {
                        foreach (TreeViewItem item in itemDb.Items)
                        {
                            if (item.Header.ToString() == cur)
                            {
                                item.IsExpanded = true;
                                break;
                            }
                        }
                    }
                }
                else
                {
                    store.Restore(treeViewDB, treeViewItemTop);
                }
            }
        }

        private void TreeViewItem_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {

        }
    }
}
