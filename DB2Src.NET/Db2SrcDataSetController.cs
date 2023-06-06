using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace Db2Source
{
    public static class Db2SrcDataSetController
    {
        public static void ShowErrorPosition(Exception t, TextBox textBox, Db2SourceContext dataSet, int offset)
        {
            Tuple<int, int> ret = dataSet.GetErrorPosition(t, textBox.Text, offset);
            if (ret == null)
            {
                return;
            }
            textBox.Select(ret.Item1, ret.Item2);
            int l = textBox.GetLineIndexFromCharacterIndex(ret.Item1);
            textBox.ScrollToLine(l);
            textBox.Focus();
        }
    }
}
