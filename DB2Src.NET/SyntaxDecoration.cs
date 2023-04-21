using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Media;
using System.Xaml;

namespace Db2Source
{
    public class SyntaxDecorationCollection : ObservableCollection<SyntaxDecorationSetting>
    {
        private Dictionary<Tuple<TokenKind, bool>, List<SyntaxDecorationSetting>> _keyToSettings = null;

        private void UpdateKeyToSettings()
        {
            if (_keyToSettings != null)
            {
                return;
            }
            _keyToSettings = new Dictionary<Tuple<TokenKind, bool>, List<SyntaxDecorationSetting>>();
            foreach (SyntaxDecorationSetting setting in this)
            {
                var key = setting.Key;
                if (key.IsReservedWord.HasValue)
                {
                    List<SyntaxDecorationSetting> l;
                    var k = new Tuple<TokenKind, bool>(key.Kind, key.IsReservedWord.Value);
                    if (!_keyToSettings.TryGetValue(k, out l))
                    {
                        l = new List<SyntaxDecorationSetting>();
                        _keyToSettings.Add(k, l);
                    }
                    l.Add(setting);
                }
                else
                {
                    List<SyntaxDecorationSetting> l;
                    var k = new Tuple<TokenKind, bool>(key.Kind, false);
                    if (!_keyToSettings.TryGetValue(k, out l))
                    {
                        l = new List<SyntaxDecorationSetting>();
                        _keyToSettings.Add(k, l);
                    }
                    l.Add(setting);
                    k = new Tuple<TokenKind, bool>(key.Kind, true);
                    if (!_keyToSettings.TryGetValue(k, out l))
                    {
                        l = new List<SyntaxDecorationSetting>();
                        _keyToSettings.Add(k, l);
                    }
                    l.Add(setting);
                }
            }
        }
        protected override void OnCollectionChanged(NotifyCollectionChangedEventArgs e)
        {
            _keyToSettings = null;
            base.OnCollectionChanged(e);
        }

        public SyntaxDecorationSetting this[TokenKind kind, NpgsqlDataSet.TokenID tokenId, bool isReserved]
        {
            get
            {
                UpdateKeyToSettings();
                List<SyntaxDecorationSetting> l;
                if (_keyToSettings.TryGetValue(new Tuple<TokenKind, bool>(kind, isReserved), out l))
                {
                    foreach (SyntaxDecorationSetting setting in l)
                    {
                        if (!setting.Key.TokenId.HasValue || setting.Key.TokenId.Value == tokenId)
                        {
                            return setting;
                        }
                    }
                }
                return null;
            }
        }

        public SyntaxDecorationSetting this[Token token]
        {
            get
            {
                return this[token.Kind, (NpgsqlDataSet.TokenID)token.ID, token.IsReservedWord];
            }
        }

        public void ApplyTo(SQLTextBox textBox, Token token)
        {
            try
            {
                TextPointer pStart = textBox.ToTextPointer(token.StartPos, 0);
                TextPointer pEnd = textBox.ToTextPointer(token.EndPos, 1);
                if (pStart == null || pEnd == null)
                {
                    return;
                }
                TextRange range;
                try
                {
                    range = new TextRange(pStart, pEnd);
                }
                catch (ArgumentException t)
                {
                    Logger.Default.Log(string.Format("ApplyTo(textBox, {0}({1})) failed create range. retry: {2}", token.ToString(), token.StartPos, t.ToString()));
                    pEnd = textBox.ToTextPointer(token.EndPos, 0);
                    range = new TextRange(pStart, pEnd);
                }
                SyntaxDecorationSetting setting = this[token];
                setting?.Value?.Apply(range, true);
            }
            catch (Exception t)
            {
                Logger.Default.Log(string.Format("ApplyTo(textBox, {0}({1})) failed: {2}", token.ToString(), token.StartPos, t.ToString()));
                MessageBox.Show(App.Current.MainWindow, t.ToString(), Properties.Resources.MessageBoxCaption_Error, MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
    public struct SyntaxSettingKey
    {
        public TokenKind Kind { get; set; }
        public NpgsqlDataSet.TokenID? TokenId { get; set; }
        public bool? IsReservedWord { get; set; }
        public string Text { get; set; }
        public string Legend { get; set; }

        public SyntaxSettingKey(TokenKind kind, NpgsqlDataSet.TokenID? tokenId, bool? isReservedWord, string text, string legend)
        {
            Kind = kind;
            TokenId = tokenId;
            IsReservedWord = isReservedWord;
            Text = text;
            Legend = legend;
        }

        public bool Matches(Token token)
        {
            return Kind == token.Kind
                && (!TokenId.HasValue || (int)TokenId.Value == token.ID)
                && (!IsReservedWord.HasValue || IsReservedWord == token.IsReservedWord);
        }

        public override string ToString()
        {
            return Text;
        }
        
        public override int GetHashCode()
        {
            return Kind.GetHashCode() * 13 + (TokenId.HasValue ? TokenId.GetHashCode() : 0);
        }

        public override bool Equals(object obj)
        {
            if (!(obj is SyntaxSettingKey))
            {
                return false;
            }
            SyntaxSettingKey info = (SyntaxSettingKey)obj;
            return Kind == info.Kind && TokenId == info.TokenId;
        }
    }

    public class SyntaxDecoration
    {
        public FontFamily Family { get; set; }
        public double? Size { get; set; }
        public FontWeight? Weight { get; set; }
        public FontStyle? Style { get; set; }
        public Brush Foreground { get; set; }
        public TextDecorationCollection Decorations { get; set; }

        public void Apply(TextRange range, bool clearBeforeApply)
        {
            if (clearBeforeApply)
            {
                range.ClearAllProperties();
            }
            if (Family != null)
            {
                range.ApplyPropertyValue(Control.FontFamilyProperty, Family);
            }
            if (Size.HasValue)
            {
                range.ApplyPropertyValue(Control.FontSizeProperty, Size.Value);
            }
            if (Weight.HasValue)
            {
                range.ApplyPropertyValue(Control.FontWeightProperty, Weight.Value);
            }
            if (Style.HasValue)
            {
                range.ApplyPropertyValue(Control.FontStyleProperty, Style.Value);
            }
            if (Foreground != null)
            {
                range.ApplyPropertyValue(Control.ForegroundProperty, Foreground);
            }
            if (Decorations != null)
            {
                range.ApplyPropertyValue(Inline.TextDecorationsProperty, Decorations);
            }
        }

        public void Apply(Run run, bool clearBeforeApply)
        {
            if (clearBeforeApply)
            {
                run.ClearValue(Control.FontFamilyProperty);
                run.ClearValue(Control.FontSizeProperty);
                run.ClearValue(Control.FontWeightProperty);
                run.ClearValue(Control.FontStyleProperty);
                run.ClearValue(Control.ForegroundProperty);
                run.ClearValue(Inline.TextDecorationsProperty);
            }
            if (Family != null)
            {
                run.FontFamily = Family;
            }
            if (Size.HasValue)
            {
                run.FontSize = Size.Value;
            }
            if (Weight.HasValue)
            {
                run.FontWeight = Weight.Value;
            }
            if (Style.HasValue)
            {
                run.FontStyle = Style.Value;
            }
            if (Foreground != null)
            {
                run.Foreground = Foreground;
            }
            if (Decorations != null)
            {
                run.TextDecorations.Add(Decorations);
            }
        }


        public override string ToString()
        {
            List<string> l = new List<string>();
            if (Family != null)
            {
                l.Add(Family.ToString());
            }
            if (Size.HasValue)
            {
                l.Add(string.Format("{0}pt", Size.Value));
            }
            if (Weight.HasValue)
            {
                l.Add(Enum.GetName(typeof(FontWeight), Weight.Value));
            }
            if (Style.HasValue)
            {
                l.Add(Enum.GetName(typeof(FontStyle), Style.Value));
            }
            if (l.Count == 0)
            {
                return string.Empty;
            }
            StringBuilder buf = new StringBuilder(l[0]);
            for (int i = 1, n = l.Count; i < n; i++)
            {
                buf.Append(", ");
                buf.Append(l[i]);
            }
            return buf.ToString();
        }
    }

    public class SyntaxDecorationSetting
    {
        public SyntaxSettingKey Key { get; set; }
        public SyntaxDecoration Value { get; set; }

        public override int GetHashCode()
        {
            return Key.GetHashCode();
        }

        public override bool Equals(object obj)
        {
            if (!(obj is SyntaxDecorationSetting))
            {
                return false;
            }
            return Key.Equals(((SyntaxDecorationSetting)obj).Key);
        }

        public override string ToString()
        {
            return string.Format("{0}: {1}", Key, Value);
        }
    }
}
