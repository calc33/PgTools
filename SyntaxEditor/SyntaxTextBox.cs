using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;

namespace SyntaxEditor
{
    public class SyntaxTextBox: Control
    {
        private readonly TextDocument _document;
        public string Text
        {
            get { return _document.Text; }
            set { _document.Text = value; }
        }
        protected override Size MeasureOverride(Size constraint)
        {
            return base.MeasureOverride(constraint);
        }

        public SyntaxTextBox()
        {
            _document = new TextDocument(this);
            FontFamilyProperty.OverrideMetadata(typeof(SyntaxTextBox), new PropertyMetadata(OnFontFamilyPropertyChanged));
            FontSizeProperty.OverrideMetadata(typeof(SyntaxTextBox), new PropertyMetadata(OnFontSizePropertyChanged));
            FontStretchProperty.OverrideMetadata(typeof(SyntaxTextBox), new PropertyMetadata(OnFontStretchPropertyChanged));
            FontStyleProperty.OverrideMetadata(typeof(SyntaxTextBox), new PropertyMetadata(OnFontStylePropertyChanged));
            FontWeightProperty.OverrideMetadata(typeof(SyntaxTextBox), new PropertyMetadata(OnFontWeightPropertyChanged));
        }

        private void OnFontFamilyPropertyChanged(DependencyPropertyChangedEventArgs e)
        {
            InvalidateMeasure();
        }
        private static void OnFontFamilyPropertyChanged(DependencyObject target, DependencyPropertyChangedEventArgs e)
        {
            (target as SyntaxTextBox)?.OnFontFamilyPropertyChanged(e);
        }

        private void OnFontSizePropertyChanged(DependencyPropertyChangedEventArgs e)
        {
            InvalidateMeasure();
        }
        private static void OnFontSizePropertyChanged(DependencyObject target, DependencyPropertyChangedEventArgs e)
        {
            (target as SyntaxTextBox)?.OnFontSizePropertyChanged(e);
        }

        private void OnFontStretchPropertyChanged(DependencyPropertyChangedEventArgs e)
        {
            InvalidateMeasure();
        }
        private static void OnFontStretchPropertyChanged(DependencyObject target, DependencyPropertyChangedEventArgs e)
        {
            (target as SyntaxTextBox)?.OnFontStretchPropertyChanged(e);
        }

        private void OnFontStylePropertyChanged(DependencyPropertyChangedEventArgs e)
        {
            InvalidateMeasure();
        }
        private static void OnFontStylePropertyChanged(DependencyObject target, DependencyPropertyChangedEventArgs e)
        {
            (target as SyntaxTextBox)?.OnFontStylePropertyChanged(e);
        }

        private void OnFontWeightPropertyChanged(DependencyPropertyChangedEventArgs e)
        {
            InvalidateMeasure();
        }
        private static void OnFontWeightPropertyChanged(DependencyObject target, DependencyPropertyChangedEventArgs e)
        {
            (target as SyntaxTextBox)?.OnFontWeightPropertyChanged(e);
        }
    }
}
