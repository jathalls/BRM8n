using Microsoft.VisualStudio.Language.Intellisense;
using System;
using System.Text.RegularExpressions;
using System.Windows;

namespace BatRecordingManager
{
    /// <summary>
    /// Interaction logic for SearchDialog.xaml
    /// This dialog permits the user to specify and implement searches on a
    /// collection of strings.  The user may select the type of search,
    /// with or withou case matching and may request to use a regular expression
    /// which will be directly used as a pattern inn Regex.
    /// Search results and moves are reported back through an event handler.
    /// </summary>
    public partial class SearchDialog : Window
    {
#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member

        public BulkObservableCollection<String> targetStrings
#pragma warning restore CS1591 // Missing XML comment for publicly visible type or member
        {
            get;
            set;
        }

        private int currentIndex = -1;

#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member

        public SearchDialog()
#pragma warning restore CS1591 // Missing XML comment for publicly visible type or member
        {
            InitializeComponent();
        }

        /// <summary>
        /// Triggered by clicking the Find button, saves the search string and
        /// performs an initial search of the collection of target strings.  When a
        /// match is found triggers the event handler to report it.
        /// If a match is found, enables the Next button.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void FindButton_Click(object sender, RoutedEventArgs e)
        {
            FindNextButton.IsEnabled = false;
            FindPrevButton.IsEnabled = false;
            if ((targetStrings == null || targetStrings.Count <= 0) || (String.IsNullOrWhiteSpace(SimpleSearchTextBox.Text)))
            {
                return;
            }

            currentIndex = 0;
            FindNextButton_Click(sender, e);
        }

        //-------------------------------------------------------------------------------------------------------
        private readonly object SearchedEventLock = new object();

        private EventHandler SearchedEvent;

#pragma warning disable CS1574 // XML comment has cref attribute that could not be resolved

        /// <summary>
        /// Event raised after the <see cref="Text" /> property value has changed.
        /// </summary>
        public event EventHandler Searched
#pragma warning restore CS1574 // XML comment has cref attribute that could not be resolved
        {
            add
            {
                lock (SearchedEventLock)
                {
                    SearchedEvent += value;
                }
            }
            remove
            {
                lock (SearchedEventLock)
                {
                    SearchedEvent -= value;
                }
            }
        }

        /// <summary>
        /// Raises the <see cref="Searched" /> event.
        /// </summary>
        /// <param name="e"><see cref="SearchedEventArgs" /> object that provides the arguments for the event.</param>
        protected virtual void OnSearched(SearchedEventArgs e)
        {
            EventHandler handler = null;

            lock (SearchedEventLock)
            {
                handler = SearchedEvent;

                if (handler == null)
                    return;
            }

            handler(this, e);
        }

        /// <summary>
        /// Either triggered by clicking the Next button, or is called after
        /// initialization by the Find button.  Advances forwards through the list looking
        /// for a match to the defined search pattern and if one is found, makes that the
        /// current position and triggers a 'searched' event.  If no match is found
        /// then triggers a 'searched' event with a null 'foundItem' and a -1 index..
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        public void FindNextButton_Click(object sender, RoutedEventArgs e)
        {
            if ((targetStrings == null || targetStrings.Count <= 0) || (String.IsNullOrWhiteSpace(SimpleSearchTextBox.Text)))
            {
                return; // do nothing if there are no strings to search, or no pattern
            }
            bool result = (RegexCheckBox.IsChecked ?? false) ? RegexSearch() : SimpleSearch();
            if (currentIndex >= targetStrings.Count - 1)
            {
                FindNextButton.IsEnabled = false;
            }
            else
            {
                FindNextButton.IsEnabled = true;
            }
            if (currentIndex <= 0)
            {
                FindPrevButton.IsEnabled = false;
            }
            else
            {
                FindPrevButton.IsEnabled = true;
            }
        }

        /// <summary>
        /// Searches through the collection of target strings performing a Regex on each using the
        /// pattern supplied in the search text box.  Triggers a searched event with the result.
        /// </summary>
        /// <returns></returns>
        private bool RegexSearch()
        {
            currentIndex++; // so we don't re-search the last found string - was -1 if no preior search
            // caseCheckBox is irrelevant in a Regex
            Regex regex = new Regex(SimpleSearchTextBox.Text);
            while (targetStrings != null && currentIndex < targetStrings.Count)
            {
                if (!string.IsNullOrWhiteSpace(targetStrings[currentIndex]))
                {
                    Match match = regex.Match(targetStrings[currentIndex]);
                    if (match.Success)
                    {
                        MatchFound(SimpleSearchTextBox.Text, targetStrings[currentIndex], currentIndex);
                        return (true);
                    }
                }
                currentIndex++;
            }
            MatchFound(SimpleSearchTextBox.Text, null, -1);
            return (false);
        }

        /// <summary>
        /// Performs a simple search through the collection of strings untils match is found,
        /// then triggers a searched event
        /// </summary>
        private bool SimpleSearch()
        {
            currentIndex++;
            string searchFor = SimpleSearchTextBox.Text.Trim();
            if (!(CaseCheckBox.IsChecked ?? false)) searchFor = searchFor.ToUpper();
            if (currentIndex == targetStrings.Count) currentIndex = 0;
            while (targetStrings != null && currentIndex < targetStrings.Count)
            {
                if (!string.IsNullOrWhiteSpace(targetStrings[currentIndex]))
                {
                    if (!(CaseCheckBox.IsChecked ?? false))
                    {
                        if (targetStrings[currentIndex].ToUpper().Contains(searchFor))
                        {
                            MatchFound(searchFor, targetStrings[currentIndex], currentIndex);
                            return (true);
                        }
                    }
                    else
                    {
                        if (targetStrings[currentIndex].Contains(searchFor))
                        {
                            MatchFound(searchFor, targetStrings[currentIndex], currentIndex);
                            return (true);
                        }
                    }
                }
                currentIndex++;
            }
            MatchFound(searchFor, null, -1);
            return (false);
        }

        /// <summary>
        /// when a search match is found triggers the searched event handler with the supplied
        /// result string and index
        /// </summary>
        /// <param name="pattern"></param>
        /// <param name="result"></param>
        /// <param name="index"></param>
        /// <param name="p"></param>
        /// <param name="v"></param>
        private void MatchFound(String pattern, String result, int index)
        {
            SearchedEventArgs seArgs = new SearchedEventArgs(index, pattern, result);
            OnSearched(seArgs);
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            this.Visibility = Visibility.Hidden;
        }
    }

    /// <summary>
    /// Provides arguments containing the results of the latest search, next or prev
    /// request
    /// </summary>
    [Serializable]
    public class SearchedEventArgs : EventArgs
    {
#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member
        public new static readonly SearchedEventArgs Empty = new SearchedEventArgs(-1, "", "");
#pragma warning restore CS1591 // Missing XML comment for publicly visible type or member

        #region Public Properties

#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member
        public string foundItem;
#pragma warning restore CS1591 // Missing XML comment for publicly visible type or member
#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member
        public string searchPattern;
#pragma warning restore CS1591 // Missing XML comment for publicly visible type or member
#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member
        public int indexOfFoundItem;
#pragma warning restore CS1591 // Missing XML comment for publicly visible type or member

        #endregion Public Properties



        #region Constructors

        /// <summary>
        /// Constructs a new instance of the <see cref="SearchedEventArgs" /> class.
        /// </summary>
        public SearchedEventArgs(int index, string pattern, string result)
        {
            indexOfFoundItem = index;
            searchPattern = pattern;
            foundItem = result;
        }

        #endregion Constructors
    }
}