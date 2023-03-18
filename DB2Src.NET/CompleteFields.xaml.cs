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
using System.Windows.Shapes;

namespace Db2Source
{
    /// <summary>
    /// CompleteFields.xaml の相互作用ロジック
    /// </summary>
    public partial class CompleteFieldWindow : Window
    {
        public static readonly DependencyProperty TargetProperty = DependencyProperty.Register("Target", typeof(Selectable), typeof(CompleteFieldWindow));
        public static readonly DependencyProperty TextBoxProperty = DependencyProperty.Register("TextBox", typeof(SQLTextBox), typeof(CompleteFieldWindow));

        public static CompleteFieldWindow Start(Selectable target, SQLTextBox textBox)
        {
            if (target == null)
            {
                return null;
            }
            if (textBox == null)
            {
                return null;
            }
            CompleteFieldWindow window = new CompleteFieldWindow()
            {
                Target = target,
                TextBox = textBox,
                Owner = GetWindow(textBox),
            };
            window.InitStartPosition();
            Rect rect = textBox.ToTextPointer(window.StartPosition, 0).GetCharacterRect(LogicalDirection.Forward);
            WindowLocator.LocateNearby(textBox, rect, window, NearbyLocation.DownLeft);
            Rect area = WindowLocator.GetWorkingAreaOf(textBox);
            window.MaxHeight = Math.Min(window.MaxHeight, area.Bottom - rect.Bottom);
            window.Show();
            return window;
        }

        private void InitStartPosition()
        {
            int selStart = TextBox.SelectionStart;
            Db2SourceContext context = Target.Context;
            TokenizedSQL tsql = context.Tokenize(TextBox.Text);
            Token tSel = tsql.QueryTokenByPosition(selStart, GapAlignment.Before);
            if (tSel != null && tSel.Kind == TokenKind.Identifier)
            {
                selStart = tSel.StartPos;
            }
            StartPosition = selStart;
        }

        public Selectable Target
        {
            get { return (Selectable)GetValue(TargetProperty); }
            set { SetValue(TargetProperty, value); }
        }

        public SQLTextBox TextBox
        {
            get { return (SQLTextBox)GetValue(TextBoxProperty); }
            set { SetValue(TextBoxProperty, value); }
        }

        public int StartPosition { get; private set; }
        public int CurrentPosition {
            get
            {
                if (TextBox == null)
                {
                    return StartPosition;
                }
                return TextBox.ToCharacterPosition(TextBox.Selection.End);
            }
            set
            {
                if (TextBox == null)
                {
                    return;
                }
                //TextBox.SelectionLength = Math.Max(0, value - TextBox.SelectionStart);
                TextBox.Select(value, 0);
            }
        }

        private string[] _fieldNamesBase;

        private void UpdateFieldNamesBase()
        {
            if (Target == null)
            {
                _fieldNamesBase = new string[0];
                return;
            }
            List<string> l = new List<string>();
            foreach (Column c in Target.Columns)
            {
                l.Add(c.Name);
            }
            //l.Sort();
            _fieldNamesBase = l.ToArray();
        }

        private bool _updateListBoxFieldsPosted = false;
        private void UpdateListBoxFields()
        {
            try
            {
                if (TextBox == null)
                {
                    return;
                }
                int pSel0 = StartPosition;
                int pSel1 = TextBox.SelectionStart;
                if (pSel1 < pSel0)
                {
                    CancelInput();
                    return;
                }
                if (pSel0 < pSel1)
                {
                    Db2SourceContext context = Target.Context;
                    TokenizedSQL tsql = context.Tokenize(TextBox.Text);
                    Token[] tokens = tsql.QueryTokenByPosition(new int[] { pSel0, pSel1 }, new GapAlignment[] { GapAlignment.After, GapAlignment.Before });
                    if (tokens[0] != tokens[1])
                    {
                        CancelInput();
                        return;
                    }
                }
                List<string> l = new List<string>();
                string lastSel = (string)listBoxFields.SelectedItem;
                string selText = TextBox.Text.Substring(StartPosition, Math.Min(TextBox.SelectionEnd, TextBox.Text.Length) - StartPosition);
                foreach (string s in _fieldNamesBase)
                {
                    if (string.IsNullOrEmpty(selText) || s.StartsWith(selText, StringComparison.CurrentCultureIgnoreCase))
                    {
                        l.Add(s);
                    }
                }
                listBoxFields.ItemsSource = l.ToArray();
                listBoxFields.SelectedIndex = Math.Min(Math.Max(0, l.IndexOf(lastSel)), l.Count - 1);
            }
            finally
            {
                _updateListBoxFieldsPosted = false;
            }
        }

        private void DelayedUpdateListBoxFields()
        {
            if (_updateListBoxFieldsPosted)
            {
                return;
            }
            Dispatcher.InvokeAsync(UpdateListBoxFields);
        }

        private void CommitInput()
        {
            Dispatcher.InvokeAsync(Close);
            if (TextBox == null)
            {
                return;
            }
            string s = (string)listBoxFields.SelectedItem;
            if (string.IsNullOrEmpty(s))
            {
                return;
            }
            TextBox.Select(StartPosition, TextBox.SelectionEnd - StartPosition);
            TextBox.SelectedText = s;
            TextBox.Select(TextBox.SelectionEnd, 0);
        }

        private void CancelInput()
        {
            Dispatcher.InvokeAsync(Close);
        }

        private void Unlink(TextBox textBox)
        {
            if (textBox == null)
            {
                return;
            }
            textBox.PreviewKeyDown -= TextBox_PreviewKeyDown;
            textBox.PreviewKeyUp -= TextBox_PreviewKeyUp;
            textBox.KeyDown -= TextBox_KeyDown;
            textBox.KeyUp -= TextBox_KeyUp;
            textBox.TextInput -= TextBox_TextInput;
        }

        private void Link(TextBox textBox)
        {
            if (textBox == null)
            {
                return;
            }
            FontFamily = textBox.FontFamily;
            FontSize = textBox.FontSize;
            FontStretch = textBox.FontStretch;
            FontStyle = textBox.FontStyle;
            FontWeight = textBox.FontWeight;
            MaxHeight = FontSize * 10;

            textBox.PreviewKeyDown += TextBox_PreviewKeyDown;
            textBox.PreviewKeyUp += TextBox_PreviewKeyUp;
            textBox.KeyDown += TextBox_KeyDown;
            textBox.KeyUp += TextBox_KeyUp;
            textBox.TextInput += TextBox_TextInput;
            textBox.TextChanged += TextBox_TextChanged;
        }

        private void TextBoxPropertyChanged(DependencyPropertyChangedEventArgs e)
        {
            if (e.NewValue != e.OldValue)
            {
                Unlink((TextBox)e.OldValue);
                Link((TextBox)e.NewValue);
            }
            DelayedUpdateListBoxFields();
        }

        private void TargetPropertyChanged(DependencyPropertyChangedEventArgs e)
        {
            UpdateFieldNamesBase();
            DelayedUpdateListBoxFields();
        }
        protected override void OnPropertyChanged(DependencyPropertyChangedEventArgs e)
        {
            if (e.Property == TextBoxProperty)
            {
                TextBoxPropertyChanged(e);
            }
            if (e.Property == TargetProperty)
            {
                TargetPropertyChanged(e);
            }
            base.OnPropertyChanged(e);
        }

        public CompleteFieldWindow()
        {
            InitializeComponent();
        }

        public void Dispose()
        {
            if (IsVisible)
            {
                Dispatcher.InvokeAsync(Close);
            }
            TextBox = null;
            Target = null;
        }

        private void TextBox_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            switch (e.Key)
            {
                case Key.Tab:
                case Key.Enter:
                    CommitInput();
                    e.Handled = true;
                    return;
                case Key.Escape:
                    CancelInput();
                    e.Handled = true;
                    Dispatcher.InvokeAsync(Close);
                    return;
                case Key.Space:
                    CommitInput();
                    //e.Handled = false;
                    return;
                //case Key.Left:
                //case Key.Right:
                //case Key.Home:
                //case Key.End:
                //    e.Handled = true;
                //    Dispatcher.InvokeAsync(Close);
                //    return;
                case Key.Down:
                    Dispatcher.InvokeAsync(() => { listBoxFields.SelectedIndex = Math.Min(listBoxFields.SelectedIndex + 1, listBoxFields.Items.Count - 1); });
                    e.Handled = true;
                    return;
                case Key.Up:
                    Dispatcher.InvokeAsync(() => { listBoxFields.SelectedIndex = Math.Max(0, listBoxFields.SelectedIndex - 1); });
                    e.Handled = true;
                    return;
                default:
                    return;
            }
        }
        private void TextBox_PreviewKeyUp(object sender, KeyEventArgs e)
        {

        }
        private void TextBox_KeyDown(object sender, KeyEventArgs e)
        {
            //DelayedUpdateListBoxFields();
        }
        private void TextBox_KeyUp(object sender, KeyEventArgs e)
        {

        }
        private void TextBox_TextInput(object sender, TextCompositionEventArgs e)
        {
        //    DelayedUpdateListBoxFields();
        }

        private void TextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            DelayedUpdateListBoxFields();
        }


        private void window_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            switch (e.Key)
            {
                case Key.Tab:
                case Key.Enter:
                    CommitInput();
                    e.Handled = true;
                    return;
                case Key.Escape:
                    e.Handled = true;
                    Dispatcher.InvokeAsync(Close);
                    return;
                case Key.Left:
                case Key.Right:
                case Key.Home:
                case Key.End:
                    e.Handled = true;
                    Dispatcher.InvokeAsync(Close);
                    return;
                case Key.Down:
                    Dispatcher.InvokeAsync(() => { listBoxFields.SelectedIndex = Math.Min(listBoxFields.SelectedIndex + 1, listBoxFields.Items.Count - 1); });
                    e.Handled = true;
                    return;
                case Key.Up:
                    Dispatcher.InvokeAsync(() => { listBoxFields.SelectedIndex = Math.Max(0, listBoxFields.SelectedIndex - 1); });
                    e.Handled = true;
                    return;
                default:
                    if (TextBox == null)
                    {
                        return;
                    }
                    TextBox.RaiseEvent(e);
                    return;
            }
        }

        private void window_PreviewKeyUp(object sender, KeyEventArgs e)
        {
            if (TextBox == null)
            {
                return;
            }
            TextBox.RaiseEvent(e);
        }

        private void window_KeyDown(object sender, KeyEventArgs e)
        {
            if (TextBox == null)
            {
                return;
            }
            TextBox.RaiseEvent(e);
        }

        private void window_KeyUp(object sender, KeyEventArgs e)
        {
            if (TextBox == null)
            {
                return;
            }
            TextBox.RaiseEvent(e);
        }

        private void window_Closed(object sender, EventArgs e)
        {
            Target = null;
            TextBox = null;
        }

        private void window_Activated(object sender, EventArgs e)
        {
            //if (TextBox != null)
            //{
            //    Dispatcher.InvokeAsync(TextBox.Focus);
            //}
        }

        private void window_Deactivated(object sender, EventArgs e)
        {
            //Dispatcher.InvokeAsync(Close);
        }

        private void window_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            if (TextBox == null)
            {
                return;
            }
            e.Handled = true;
            Dispatcher.InvokeAsync(() =>
            {
                TextBox.SelectedText = e.Text;
                TextBox.Select(TextBox.SelectionEnd, 0);
                TextBox.Focus();
            });
        }

        private void listBoxFields_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            CommitInput();
        }
    }

    public class CompleteFieldController
    {
        private Selectable _target;
        public Selectable Target
        {
            get { return _target; }
            set
            {
                _target = value;
                DropDownWindow = null;
            }
        }
        public SQLTextBox TextBox { get; }
        public CompleteFieldWindow DropDownWindow { get; private set; }

        public CompleteFieldController(Selectable target, SQLTextBox textBox)
        {
            Target = target;
            TextBox = textBox;
            TextBox.PreviewKeyDown += TextBox_PreviewKeyDown;
        }

        private void DisposeDropDownWindow()
        {
            if (DropDownWindow == null)
            {
                return;
            }
        }

        private void ShowFieldsDropDown()
        {
            if (DropDownWindow != null)
            {
                return;
            }
            DropDownWindow = CompleteFieldWindow.Start(Target, TextBox);
            DropDownWindow.Closed += DropDownWindow_Closed;
        }

        private void DropDownWindow_Closed(object sender, EventArgs e)
        {
            DropDownWindow = null;
        }

        private void TextBox_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Space && (e.KeyboardDevice.Modifiers & ModifierKeys.Control) != 0)
            {
                ShowFieldsDropDown();
                e.Handled = true;
            }
        }
    }
}
