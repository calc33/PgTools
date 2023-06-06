using System;
using System.Text;
using System.Windows;

namespace Db2Source
{
    /// <summary>
    /// ログをStringBuilderに蓄える
    /// </summary>
    public class StringBuilderLogger
    {
        internal StringBuilder Buffer { get; } = new StringBuilder();
        public void Log(object sender, LogEventArgs e)
        {
            Buffer.AppendLine(e.Text);
        }

        public void LogException(Exception t, Db2SourceContext context)
        {
            Buffer.AppendLine(context.GetExceptionMessage(t));
        }

        /// <summary>
        /// ログがたまっていればMessageBoxで表示する
        /// ログがなければ何もしない
        /// </summary>
        /// <param name="owner">MessageBoxのowner</param>
        /// <param name="isError">MessageBoxのアイコンを指定 true:Error(赤丸に×) false:Information(青丸にi)</param>
        public void ShowLogByMessageBox(Window owner, bool isError)
        {
            string text = Buffer.ToString().TrimEnd();
            if (string.IsNullOrEmpty(text))
            {
                return;
            }
            MessageBoxImage icon = MessageBoxImage.Information;
            string caption = Properties.Resources.MessageBoxCaption_Result;
            if (isError)
            {
                icon = MessageBoxImage.Error;
                caption = Properties.Resources.MessageBoxCaption_Error;
            }
            MessageBox.Show(owner, text, caption, MessageBoxButton.OK, icon);
        }
    }

}
