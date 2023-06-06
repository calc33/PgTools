using System.Windows;

namespace Db2Source
{
    public class PgDumpFormatOption : DependencyObject
    {
        public static readonly DependencyProperty TextProperty = DependencyProperty.Register("Text", typeof(string), typeof(PgDumpFormatOption));
        public static readonly DependencyProperty OptionProperty = DependencyProperty.Register("Option", typeof(string), typeof(PgDumpFormatOption));
        public static readonly DependencyProperty CanCompressProperty = DependencyProperty.Register("CanCompress", typeof(bool), typeof(PgDumpFormatOption));
        public static readonly DependencyProperty IsFileProperty = DependencyProperty.Register("IsFile", typeof(bool), typeof(PgDumpFormatOption));
        public static readonly DependencyProperty DefaultExtProperty = DependencyProperty.Register("DefaultExt", typeof(string), typeof(PgDumpFormatOption));
        public static readonly DependencyProperty DialogFilterProperty = DependencyProperty.Register("DialogFilter", typeof(string), typeof(PgDumpFormatOption));
        public string Text
        {
            get
            {
                return (string)GetValue(TextProperty);
            }
            set
            {
                SetValue(TextProperty, value);
            }
        }
        public string Option
        {
            get
            {
                return (string)GetValue(OptionProperty);
            }
            set
            {
                SetValue(OptionProperty, value);
            }
        }
        public bool CanCompress
        {
            get
            {
                return (bool)GetValue(CanCompressProperty);
            }
            set
            {
                SetValue(CanCompressProperty, value);
            }
        }
        public bool IsFile
        {
            get
            {
                return (bool)GetValue(IsFileProperty);
            }
            set
            {
                SetValue(IsFileProperty, value);
            }
        }
        public bool IsDirectory
        {
            get
            {
                return !IsFile;
            }
            set
            {
                IsFile = !value;
            }
        }
        public string DefaultExt
        {
            get
            {
                return (string)GetValue(DefaultExtProperty);
            }
            set
            {
                SetValue(DefaultExtProperty, value);
            }
        }
        public string DialogFilter
        {
            get
            {
                return (string)GetValue(DialogFilterProperty);
            }
            set
            {
                SetValue(DialogFilterProperty, value);
            }
        }
        public override string ToString()
        {
            return Text;
        }
    }
}
