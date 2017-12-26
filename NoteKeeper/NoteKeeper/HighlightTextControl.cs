using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Media;

namespace NoteKeeper
{
    public sealed class HighlightTextControl : ContentControl
    {
        public static readonly DependencyProperty HighlightPartsProperty =
            DependencyProperty.Register(
                "HighlightParts",
                typeof(IEnumerable<string>),
                typeof(HighlightTextControl),
                new PropertyMetadata(default(IEnumerable<string>), HighlightPartsPropertyChangedCallback));

        public static readonly DependencyProperty TextProperty =
            DependencyProperty.Register(
                "Text", typeof(string),
                typeof(HighlightTextControl),
                new PropertyMetadata(default(string), TextPropertyChangedCallback));

        private readonly TextBlock _textBlock;
        private readonly SolidColorBrush _highlightedTextBackgroundBrush;
        private readonly SolidColorBrush _highlightedTextForegroundBrush;

        private static void HighlightPartsPropertyChangedCallback(DependencyObject d,
            DependencyPropertyChangedEventArgs e)
        {
            var _this = (HighlightTextControl) d;
            if (e.OldValue is INotifyCollectionChanged incc)
                incc.CollectionChanged -= _this.HighlightParts_OnCollectionChanged;

            incc = e.NewValue as INotifyCollectionChanged;
            if (incc != null)
                incc.CollectionChanged += _this.HighlightParts_OnCollectionChanged;

            _this.TextChanged();
        }

        private void HighlightParts_OnCollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            TextChanged();
        }

        private static void TextPropertyChangedCallback(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            ((HighlightTextControl) d).TextChanged();
        }

        public IEnumerable<string> HighlightParts
        {
            get => (IEnumerable<string>) GetValue(HighlightPartsProperty);
            set => SetValue(HighlightPartsProperty, value);
        }

        public string Text
        {
            get => (string) GetValue(TextProperty);
            set => SetValue(TextProperty, value);
        }

        public HighlightTextControl()
        {
            Content = _textBlock = new TextBlock();

            _highlightedTextBackgroundBrush = new SolidColorBrush(Color.FromArgb(255, 255, 229, 107));
            _highlightedTextBackgroundBrush.Freeze();

            _highlightedTextForegroundBrush = new SolidColorBrush(SystemColors.ActiveCaptionTextColor);
            _highlightedTextForegroundBrush.Freeze();
        }

        private void TextChanged()
        {
            _textBlock.Inlines.Clear();
            var coloredText = GetColoredText().ToArray();
            _textBlock.Inlines.AddRange(coloredText);
        }

        private Dictionary<int, string> GetIndexes()
        {
            var indexes = new Dictionary<int, string>();
            var idx = 0;
            foreach (var h in HighlightParts.Where(s => !string.IsNullOrEmpty(s)))
            {
                var indexOf = idx > Text.Length
                    ? -1
                    : Text.IndexOf(h, idx, StringComparison.OrdinalIgnoreCase);

                indexes.Add(indexOf, h);
                idx = indexOf + h.Length;

                if (indexOf < 0)
                    break;
            }

            return indexes;
        }

        private IEnumerable<Inline> GetColoredText()
        {
            if (string.IsNullOrEmpty(Text) || HighlightParts == null || !HighlightParts.Any() ||
                HighlightParts.All(string.IsNullOrEmpty))
            {
                yield return new Run(Text);
                yield break;
            }

            var indexes = GetIndexes();

            var normalRun = new Run();
            var highlightRun = new Run
            {
                Background = _highlightedTextBackgroundBrush,
                Foreground = _highlightedTextForegroundBrush
            };

            for (var i = 0; i < Text.Length; ++i)
            {
                if (indexes.ContainsKey(i))
                {
                    highlightRun.Text += Text.Substring(i, indexes[i].Length);
                    i += indexes[i].Length - 1;

                    if (!string.IsNullOrEmpty(normalRun.Text))
                    {
                        yield return normalRun;
                        normalRun = new Run();
                    }
                }
                else
                {
                    normalRun.Text += Text[i];

                    if (!string.IsNullOrEmpty(highlightRun.Text))
                    {
                        yield return highlightRun;
                        highlightRun = new Run
                        {
                            Background = _highlightedTextBackgroundBrush,
                            Foreground = _highlightedTextForegroundBrush
                        };
                    }
                }
            }

            if (!string.IsNullOrEmpty(highlightRun.Text))
                yield return highlightRun;

            if (!string.IsNullOrEmpty(normalRun.Text))
                yield return normalRun;
        }
    }
}