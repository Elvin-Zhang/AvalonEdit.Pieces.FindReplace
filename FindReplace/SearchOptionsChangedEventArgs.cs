using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FindReplace
{
    // original from: https://github.com/aelij/RoslynPad/blob/master/src/RoslynPad.Editor.Windows/SearchReplacePanel.cs


    /// <summary>
    /// EventArgs for <see cref="SearchReplacePanel.SearchOptionsChanged"/> event.
    /// </summary>
    public class SearchOptionsChangedEventArgs : EventArgs
    {
        /// <summary>
        /// Gets the search pattern.
        /// </summary>
        public string SearchPattern { get; private set; }

        /// <summary>
        /// Gets whether the search pattern should be interpreted case-sensitive.
        /// </summary>
        public bool MatchCase { get; private set; }

        /// <summary>
        /// Gets whether the search pattern should be interpreted as regular expression.
        /// </summary>
        public bool UseRegex { get; private set; }

        /// <summary>
        /// Gets whether the search pattern should only match whole words.
        /// </summary>
        public bool WholeWords { get; private set; }

        /// <summary>
        /// Creates a new SearchOptionsChangedEventArgs instance.
        /// </summary>
        public SearchOptionsChangedEventArgs(string searchPattern, bool matchCase, bool useRegex, bool wholeWords)
        {
            SearchPattern = searchPattern;
            MatchCase = matchCase;
            UseRegex = useRegex;
            WholeWords = wholeWords;
        }
    }
}
