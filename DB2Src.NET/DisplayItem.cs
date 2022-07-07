using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Db2Source
{
    public class DisplayItem
    {
        public const string NEWITEM_NAME = "(NEW ITEM)";
        public NamedObject Item { get; private set; }
        public string Text { get; private set; }
        public string ItemName { get; private set; }
        public bool IsNew { get; private set; }
        public DisplayItem(NamedObject item)
        {
            if (item == null)
            {
                throw new ArgumentNullException("item");
            }
            Item = item;
            Text = item.FullIdentifier;
            ItemName = item.FullIdentifier;
            IsNew = false;
        }
        public DisplayItem(NamedObject item, string text)
        {
            if (item == null)
            {
                throw new ArgumentNullException("item");
            }
            Item = item;
            Text = text;
            ItemName = NEWITEM_NAME;
            IsNew = true;
        }
        public DisplayItem(string text)
        {
            Item = null;
            Text = text;
            ItemName = null;
            IsNew = false;
        }
        public static DisplayItem[] ToDisplayItemArray(IEnumerable<NamedObject> source, string nullText, NamedObject newItem, string newItemText)
        {
            List<DisplayItem> l = new List<DisplayItem>();
            if (source == null)
            {
                return l.ToArray();
            }
            if (nullText != null)
            {
                l.Add(new DisplayItem(nullText));
            }
            foreach (NamedObject obj in source)
            {
                l.Add(new DisplayItem(obj));
            }
            if (newItem != null)
            {
                l.Add(new DisplayItem(newItem, newItemText));
            }
            return l.ToArray();
        }

        public override bool Equals(object obj)
        {
            if (!(obj is DisplayItem))
            {
                return false;
            }
            return object.Equals(Item, ((DisplayItem)obj).Item);
        }
        public override int GetHashCode()
        {
            if (Item == null)
            {
                return 0;
            }
            return Item.GetHashCode();
        }
        public override string ToString()
        {
            return Text;
        }
    }
}
