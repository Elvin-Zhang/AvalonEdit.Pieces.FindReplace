using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;

namespace FindReplace
{
    // original from: https://github.com/aelij/RoslynPad/blob/master/src/RoslynPad.Editor.Windows/SearchReplacePanel.cs
    static class SearchCommandsEx
    {
        /// <summary>Replaces the next occurrence in the document.</summary>
        public static readonly RoutedCommand ReplaceNext = new RoutedCommand("ReplaceNext", typeof(SearchReplacePanel),
            new InputGestureCollection
            {
                new KeyGesture(Key.R, ModifierKeys.Alt)
            });

        /// <summary>Replaces all the occurrences in the document.</summary>
        public static readonly RoutedCommand ReplaceAll = new RoutedCommand("ReplaceAll", typeof(SearchReplacePanel),
            new InputGestureCollection
            {
                new KeyGesture(Key.A, ModifierKeys.Alt)
            });
    }
}
