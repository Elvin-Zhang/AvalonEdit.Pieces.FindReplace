﻿using ICSharpCode.AvalonEdit;
using ICSharpCode.AvalonEdit.Document;
using ICSharpCode.AvalonEdit.Editing;
using ICSharpCode.AvalonEdit.Rendering;
using ICSharpCode.AvalonEdit.Search;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Windows.Threading;
using Localization = ICSharpCode.AvalonEdit.Search.Localization;



namespace CodeRunner.TextEditing
{
    // original from: https://github.com/aelij/RoslynPad/blob/master/src/RoslynPad.Editor.Windows/SearchReplacePanel.cs


    /// <summary>
    /// Interaction logic for SearchReplacePanel.xaml
    /// </summary>
    public partial class SearchReplacePanel : UserControl
    {
        private TextArea _textArea;
        private SearchReplaceInputHandler _handler;
        private TextDocument _currentDocument;
        private SearchReplaceResultBackgroundRenderer _renderer;
        private TextBox _searchTextBox;
        //private SearchReplacePanelAdorner _adorner;
        private GenericControlAdorner _adorner;
        private ISearchStrategy _strategy;

        private ToolTip _messageView = new ToolTip { Placement = PlacementMode.Bottom, StaysOpen = true, Focusable = false };

        #region DependencyProperties
        /// <summary>
        /// Dependency property for <see cref="UseRegex"/>.
        /// </summary>
        public static readonly DependencyProperty UseRegexProperty =
            DependencyProperty.Register("UseRegex", typeof(bool), typeof(SearchReplacePanel),
                new FrameworkPropertyMetadata(false, SearchPatternChangedCallback));

        /// <summary>
        /// Gets/sets whether the search pattern should be interpreted as regular expression.
        /// </summary>
        public bool UseRegex
        {
            get { return (bool)GetValue(UseRegexProperty); }
            set { SetValue(UseRegexProperty, value); }
        }

        /// <summary>
        /// Dependency property for <see cref="MatchCase"/>.
        /// </summary>
        public static readonly DependencyProperty MatchCaseProperty =
            DependencyProperty.Register("MatchCase", typeof(bool), typeof(SearchReplacePanel),
                new FrameworkPropertyMetadata(false, SearchPatternChangedCallback));

        /// <summary>
        /// Gets/sets whether the search pattern should be interpreted case-sensitive.
        /// </summary>
        public bool MatchCase
        {
            get { return (bool)GetValue(MatchCaseProperty); }
            set { SetValue(MatchCaseProperty, value); }
        }

        /// <summary>
        /// Dependency property for <see cref="WholeWords"/>.
        /// </summary>
        public static readonly DependencyProperty WholeWordsProperty =
            DependencyProperty.Register("WholeWords", typeof(bool), typeof(SearchReplacePanel),
                new FrameworkPropertyMetadata(false, SearchPatternChangedCallback));

        /// <summary>
        /// Gets/sets whether the search pattern should only match whole words.
        /// </summary>
        public bool WholeWords
        {
            get { return (bool)GetValue(WholeWordsProperty); }
            set { SetValue(WholeWordsProperty, value); }
        }

        /// <summary>
        /// Dependency property for <see cref="SearchPattern"/>.
        /// </summary>
        public static readonly DependencyProperty SearchPatternProperty =
            DependencyProperty.Register("SearchPattern", typeof(string), typeof(SearchReplacePanel),
                new FrameworkPropertyMetadata("", SearchPatternChangedCallback));

        /// <summary>
        /// Gets/sets the search pattern.
        /// </summary>
        public string SearchPattern
        {
            get { return (string)GetValue(SearchPatternProperty); }
            set { SetValue(SearchPatternProperty, value); }
        }

        /// <summary>
        /// Dependency property for <see cref="MarkerBrush"/>.
        /// </summary>
        public static readonly DependencyProperty MarkerBrushProperty =
            DependencyProperty.Register("MarkerBrush", typeof(Brush), typeof(SearchReplacePanel),
                new FrameworkPropertyMetadata(Brushes.LightGreen, MarkerBrushChangedCallback));

        /// <summary>
        /// Gets/sets the Brush used for marking search results in the TextView.
        /// </summary>
        public Brush MarkerBrush
        {
            get { return (Brush)GetValue(MarkerBrushProperty); }
            set { SetValue(MarkerBrushProperty, value); }
        }

        /// <summary>
        /// Dependency property for <see cref="Localization"/>.
        /// </summary>
        public static readonly DependencyProperty LocalizationProperty =
            DependencyProperty.Register("Localization", typeof(Localization), typeof(SearchReplacePanel),
                new FrameworkPropertyMetadata(new Localization()));

        /// <summary>
        /// Gets/sets the localization for the SearchReplacePanel.
        /// </summary>
        public Localization Localization
        {
            get { return (Localization)GetValue(LocalizationProperty); }
            set { SetValue(LocalizationProperty, value); }
        }
        #endregion

        static void MarkerBrushChangedCallback(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is SearchReplacePanel panel)
            {
                panel._renderer.MarkerBrush = (Brush)e.NewValue;
            }
        }

        static SearchReplacePanel()
        {
            DefaultStyleKeyProperty.OverrideMetadata(typeof(SearchReplacePanel), new FrameworkPropertyMetadata(typeof(SearchReplacePanel)));
        }

        static void SearchPatternChangedCallback(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is SearchReplacePanel panel)
            {
                panel.ValidateSearchText();
                panel.UpdateSearch();
            }
        }

        void UpdateSearch()
        {
            // only reset as long as there are results
            // if no results are found, the "no matches found" message should not flicker.
            // if results are found by the next run, the message will be hidden inside DoSearch ...
            if (_renderer.CurrentResults.Any())
                _messageView.IsOpen = false;
            _strategy = SearchStrategyFactory.Create(SearchPattern ?? "", !MatchCase, WholeWords, UseRegex ? SearchMode.RegEx : SearchMode.Normal);
            OnSearchOptionsChanged(new SearchOptionsChangedEventArgs(SearchPattern, MatchCase, UseRegex, WholeWords));
            DoSearch(true);
        }

        /// <summary>
        /// Creates a new SearchReplacePanel.
        /// </summary>
        public SearchReplacePanel(TextArea __textArea)
        {
            InitializeComponent();
           
            _searchTextBox = this.FindName("PART_searchTextBox") as TextBox;
            this._textArea = __textArea;

            this.AttachInternal();
            this._handler = new SearchReplaceInputHandler(this._textArea, this);
            this._textArea.DefaultInputHandler.NestedInputHandlers.Add(this._handler);
        }

        /// <summary>
        /// Adds the commands used by SearchReplacePanel to the given CommandBindingCollection.
        /// </summary>
        public void RegisterCommands(CommandBindingCollection commandBindings)
        {
            _handler.RegisterGlobalCommands(commandBindings);
        }

        /// <summary>
        /// Removes the SearchReplacePanel from the TextArea.
        /// </summary>
        public void Uninstall()
        {
            CloseAndRemove();
            _textArea.DefaultInputHandler.NestedInputHandlers.Remove(_handler);
        }

        void AttachInternal()
        {
            //_adorner = new SearchReplacePanelAdorner(textArea, this);

            _adorner = new GenericControlAdorner(_textArea)
            {
                Child = this
            };

            DataContext = this;

            _renderer = new SearchReplaceResultBackgroundRenderer();
            _currentDocument = this._textArea.Document;
            if (_currentDocument != null)
                _currentDocument.TextChanged += TextArea_Document_TextChanged;
            this._textArea.DocumentChanged += TextArea_DocumentChanged;
            KeyDown += SearchLayerKeyDown;

            CommandBindings.Add(new CommandBinding(SearchCommands.FindNext, (sender, e) => FindNext()));
            CommandBindings.Add(new CommandBinding(SearchCommands.FindPrevious, (sender, e) => FindPrevious()));
            CommandBindings.Add(new CommandBinding(SearchCommands.CloseSearchPanel, (sender, e) => Close()));

            CommandBindings.Add(new CommandBinding(ApplicationCommands.Find, (sender, e) =>
            {
                IsReplaceMode = false;
                Reactivate();
            }));
            CommandBindings.Add(new CommandBinding(ApplicationCommands.Replace, (sender, e) => IsReplaceMode = true));
            CommandBindings.Add(new CommandBinding(SearchCommandsEx.ReplaceNext, (sender, e) => ReplaceNext(), (sender, e) => e.CanExecute = IsReplaceMode));
            CommandBindings.Add(new CommandBinding(SearchCommandsEx.ReplaceAll, (sender, e) => ReplaceAll(), (sender, e) => e.CanExecute = IsReplaceMode));

            IsClosed = true;
        }

        void TextArea_DocumentChanged(object sender, EventArgs e)
        {
            if (_currentDocument != null)
                _currentDocument.TextChanged -= TextArea_Document_TextChanged;
            _currentDocument = _textArea.Document;
            if (_currentDocument != null)
            {
                _currentDocument.TextChanged += TextArea_Document_TextChanged;
                DoSearch(false);
            }
        }

        void TextArea_Document_TextChanged(object sender, EventArgs e)
        {
            DoSearch(false);
        }


        void ValidateSearchText()
        {
            if (_searchTextBox == null)
                return;
            var be = _searchTextBox.GetBindingExpression(TextBox.TextProperty);
            try
            {
                Validation.ClearInvalid(be);
                UpdateSearch();
            }
            catch (SearchPatternException ex)
            {
                var ve = new ValidationError(be.ParentBinding.ValidationRules[0], be, ex.Message, ex);
                Validation.MarkInvalid(be, ve);
            }
        }

        /// <summary>
        /// Reactivates the SearchReplacePanel by setting the focus on the search box and selecting all text.
        /// </summary>
        public void Reactivate()
        {
            if (_searchTextBox == null)
                return;
            _searchTextBox.Focus();
            _searchTextBox.SelectAll();
        }

        /// <summary>
        /// Moves to the next occurrence in the file.
        /// </summary>
        public void FindNext()
        {
            var result = _renderer.CurrentResults.FindFirstSegmentWithStartAfter(_textArea.Caret.Offset + 1);
            if (result == null)
                result = _renderer.CurrentResults.FirstSegment;
            if (result != null)
            {
                SelectResult(result);
            }
        }

        /// <summary>
        /// Moves to the previous occurrence in the file.
        /// </summary>
        public void FindPrevious()
        {
            var result = _renderer.CurrentResults.FindFirstSegmentWithStartAfter(_textArea.Caret.Offset);
            if (result != null)
                result = _renderer.CurrentResults.GetPreviousSegment(result);
            if (result == null)
                result = _renderer.CurrentResults.LastSegment;
            if (result != null)
            {
                SelectResult(result);
            }
        }

        void DoSearch(bool changeSelection)
        {
            if (IsClosed)
                return;
            _renderer.CurrentResults.Clear();

            if (!string.IsNullOrEmpty(SearchPattern))
            {
                var offset = _textArea.Caret.Offset;
                if (changeSelection)
                {
                    _textArea.ClearSelection();
                }
                // We cast from ISearchResult to SearchResult; this is safe because we always use the built-in strategy
                foreach (var result in _strategy.FindAll(_textArea.Document, 0, _textArea.Document.TextLength).OfType<TextSegment>())
                {
                    if (changeSelection && result.StartOffset >= offset)
                    {
                        SelectResult(result);
                        changeSelection = false;
                    }
                    _renderer.CurrentResults.Add(result);
                }
                if (!_renderer.CurrentResults.Any())
                {
                    _messageView.IsOpen = true;
                    _messageView.Content = Localization.NoMatchesFoundText;
                    _messageView.PlacementTarget = _searchTextBox;
                }
                else
                    _messageView.IsOpen = false;
            }
            _textArea.TextView.InvalidateLayer(KnownLayer.Selection);
        }

        void SelectResult(TextSegment textSement)
        {
            _textArea.Caret.Offset = textSement.StartOffset;
            _textArea.Selection = Selection.Create(_textArea, textSement.StartOffset, textSement.EndOffset);
            _textArea.Caret.BringCaretToView();
            // show caret even if the editor does not have the Keyboard Focus
            _textArea.Caret.Show();
        }

        void SearchLayerKeyDown(object sender, KeyEventArgs e)
        {
            switch (e.Key)
            {
                case Key.Enter:
                    e.Handled = true;
                    if ((Keyboard.Modifiers & ModifierKeys.Shift) == ModifierKeys.Shift)
                        FindPrevious();
                    else
                        FindNext();
                    if (_searchTextBox != null)
                    {
                        var error = Validation.GetErrors(_searchTextBox).FirstOrDefault();
                        if (error != null)
                        {
                            _messageView.Content = Localization.ErrorText + " " + error.ErrorContent;
                            _messageView.PlacementTarget = _searchTextBox;
                            _messageView.IsOpen = true;
                        }
                    }
                    break;
                case Key.Escape:
                    e.Handled = true;
                    Close();
                    break;
            }
        }

        /// <summary>
        /// Gets whether the Panel is already closed.
        /// </summary>
        public bool IsClosed { get; private set; }

        /// <summary>
        /// Closes the SearchReplacePanel.
        /// </summary>
        public void Close()
        {
            var hasFocus = IsKeyboardFocusWithin;

            var layer = AdornerLayer.GetAdornerLayer(_textArea);
            if (layer != null)
                layer.Remove(_adorner);
            _messageView.IsOpen = false;
            _textArea.TextView.BackgroundRenderers.Remove(_renderer);
            if (hasFocus)
                _textArea.Focus();
            IsClosed = true;

            // Clear existing search results so that the segments don't have to be maintained
            _renderer.CurrentResults.Clear();
        }

        /// <summary>
        /// Closes the SearchReplacePanel and removes it.
        /// </summary>
        private void CloseAndRemove()
        {
            Close();
            _textArea.DocumentChanged -= TextArea_DocumentChanged;
            if (_currentDocument != null)
                _currentDocument.TextChanged -= TextArea_Document_TextChanged;
        }

        /// <summary>
        /// Opens the an existing search panel.
        /// </summary>
        public void Open()
        {
            if (!IsClosed) return;

            /*
            var testWin1 = new Window();
            testWin1.Height = 300;
            testWin1.Width = 300;
            testWin1.Content = new SearchReplacePanel(this._textArea);
            testWin1.ShowDialog();
            */
            var layer = AdornerLayer.GetAdornerLayer(_textArea);
            if (layer != null)
            {
                layer.Add(_adorner);
                // try a different test too
                var adorner2 = new GenericControlAdorner(_textArea)
                {
                    Child = new Button
                    {
                        Content = "Hello World"
                    }
                };
                layer.Add(adorner2);
                /*
                var adorner3 = new GenericControlAdorner(_textArea)
                {
                    Child = new test.TestControl1()
                };
                layer.Add(adorner3);*/

                var adorner4 = new GenericControlAdorner(_textArea)
                {
                    Child = new SearchReplacePanel(_textArea)
                };
                layer.Add(adorner4);
            }
                
            _textArea.TextView.BackgroundRenderers.Add(_renderer);
            IsClosed = false;
            DoSearch(false);
        }

        /// <summary>
        /// Fired when SearchOptions are changed inside the SearchReplacePanel.
        /// </summary>
        public event EventHandler<SearchOptionsChangedEventArgs> SearchOptionsChanged;

        /// <summary>
        /// Raises the <see cref="SearchReplacePanel.SearchOptionsChanged" /> event.
        /// </summary>
        protected virtual void OnSearchOptionsChanged(SearchOptionsChangedEventArgs e)
        {
            SearchOptionsChanged?.Invoke(this, e);
        }

        public static readonly DependencyProperty IsReplaceModeProperty = DependencyProperty.Register(
            "IsReplaceMode", typeof(bool), typeof(SearchReplacePanel), new FrameworkPropertyMetadata());

        public bool IsReplaceMode
        {
            get => (bool)GetValue(IsReplaceModeProperty);
            set => SetValue(IsReplaceModeProperty, value);
        }

        public static readonly DependencyProperty ReplacePatternProperty = DependencyProperty.Register(
            "ReplacePattern", typeof(string), typeof(SearchReplacePanel), new FrameworkPropertyMetadata());

        public string ReplacePattern
        {
            get => (string)GetValue(ReplacePatternProperty);
            set => SetValue(ReplacePatternProperty, value);
        }

        /// <summary>
        /// Creates a SearchReplacePanel and installs it to the TextEditor's TextArea.
        /// </summary>
        public static SearchReplacePanel Install(TextEditor editor)
        {
            if (editor == null)
                throw new ArgumentNullException(nameof(editor));
            return Install(editor.TextArea);
        }

        /// <summary>
        /// Creates a SearchReplacePanel and installs it to the TextArea.
        /// </summary>
        public static SearchReplacePanel Install(TextArea textArea)
        {
            if (textArea == null)
                throw new ArgumentNullException(nameof(textArea));
            var panel = new SearchReplacePanel(textArea);

            return panel;
        }

        public void ReplaceNext()
        {
            if (!IsReplaceMode) return;

            FindNext();
            if (!_textArea.Selection.IsEmpty)
            {
                _textArea.Selection.ReplaceSelectionWithText(ReplacePattern ?? string.Empty);
            }
        }

        public void ReplaceAll()
        {
            if (!IsReplaceMode) return;

            var replacement = ReplacePattern ?? string.Empty;
            var document = _textArea.Document;
            using (document.RunUpdate())
            {
                var segments = _renderer.CurrentResults.OrderByDescending(x => x.EndOffset).ToArray();
                foreach (var textSegment in segments)
                {
                    document.Replace(textSegment.StartOffset, textSegment.Length,
                        new StringTextSource(replacement));
                }
            }
        }

        private class SearchReplaceInputHandler : TextAreaInputHandler
        {
            private readonly SearchReplacePanel _panel;

            internal SearchReplaceInputHandler(TextArea textArea, SearchReplacePanel panel)
                : base(textArea)
            {
                RegisterCommands();
                _panel = panel;
            }

            private void RegisterCommands()
            {
                CommandBindings.Add(new CommandBinding(ApplicationCommands.Find, ExecuteFind));
                CommandBindings.Add(new CommandBinding(ApplicationCommands.Replace, ExecuteReplace));
                CommandBindings.Add(new CommandBinding(SearchCommands.FindNext, ExecuteFindNext, CanExecuteWithOpenSearchPanel));
                CommandBindings.Add(new CommandBinding(SearchCommands.FindPrevious, ExecuteFindPrevious, CanExecuteWithOpenSearchPanel));
                CommandBindings.Add(new CommandBinding(SearchCommandsEx.ReplaceNext, ExecuteReplaceNext, CanExecuteWithOpenSearchPanel));
                CommandBindings.Add(new CommandBinding(SearchCommandsEx.ReplaceAll, ExecuteReplaceAll, CanExecuteWithOpenSearchPanel));
                CommandBindings.Add(new CommandBinding(SearchCommands.CloseSearchPanel, ExecuteCloseSearchPanel, CanExecuteWithOpenSearchPanel));
            }

            private void ExecuteFind(object sender, ExecutedRoutedEventArgs e)
            {
                FindOrReplace(isReplaceMode: false);
            }

            private void ExecuteReplace(object sender, ExecutedRoutedEventArgs e)
            {
                FindOrReplace(isReplaceMode: true);
            }

            private void FindOrReplace(bool isReplaceMode)
            {
                _panel.IsReplaceMode = isReplaceMode;
                _panel.Open();
                if (!TextArea.Selection.IsEmpty && !TextArea.Selection.IsMultiline)
                    _panel.SearchPattern = TextArea.Selection.GetText();
                TextArea.Dispatcher.InvokeAsync(() => _panel.Reactivate(), DispatcherPriority.Input);
            }

            private void CanExecuteWithOpenSearchPanel(object sender, CanExecuteRoutedEventArgs e)
            {
                if (_panel.IsClosed)
                {
                    e.CanExecute = false;
                    e.ContinueRouting = true;
                }
                else
                {
                    e.CanExecute = true;
                    e.Handled = true;
                }
            }

            private void ExecuteFindNext(object sender, ExecutedRoutedEventArgs e)
            {
                if (_panel.IsClosed)
                    return;
                _panel.FindNext();
                e.Handled = true;
            }

            private void ExecuteFindPrevious(object sender, ExecutedRoutedEventArgs e)
            {
                if (_panel.IsClosed)
                    return;
                _panel.FindPrevious();
                e.Handled = true;
            }

            private void ExecuteReplaceNext(object sender, ExecutedRoutedEventArgs e)
            {
                if (_panel.IsClosed)
                    return;
                _panel.ReplaceNext();
                e.Handled = true;
            }

            private void ExecuteReplaceAll(object sender, ExecutedRoutedEventArgs e)
            {
                if (_panel.IsClosed)
                    return;
                _panel.ReplaceAll();
                e.Handled = true;
            }

            private void ExecuteCloseSearchPanel(object sender, ExecutedRoutedEventArgs e)
            {
                if (_panel.IsClosed)
                    return;
                _panel.Close();
                e.Handled = true;
            }

            internal void RegisterGlobalCommands(CommandBindingCollection commandBindings)
            {
                commandBindings.Add(new CommandBinding(ApplicationCommands.Find, ExecuteFind));
                commandBindings.Add(new CommandBinding(SearchCommands.FindNext, ExecuteFindNext, CanExecuteWithOpenSearchPanel));
                commandBindings.Add(new CommandBinding(SearchCommands.FindPrevious, ExecuteFindPrevious, CanExecuteWithOpenSearchPanel));
            }
        }
    }
}