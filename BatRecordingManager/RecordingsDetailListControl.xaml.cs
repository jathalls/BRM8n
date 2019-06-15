using Microsoft.Maps.MapControl.WPF;
using Microsoft.VisualStudio.Language.Intellisense;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;

namespace BatRecordingManager
{
    /// <summary>
    ///     Interaction logic for RecordingsDetailListControl.xaml
    /// </summary>
    public partial class RecordingsDetailListControl : UserControl
    {
        private RecordingSession _selectedSession;
        private SearchDialog searchDialog = new SearchDialog();

        private SearchableCollection searchTargets = new SearchableCollection();

        //private double vo = -1.0;

        //-----------------------------------------------------------------------------------
        /// <summary>
        ///     Initializes a new instance of the <see cref="RecordingsDetailListControl"/> class.
        /// </summary>
        public RecordingsDetailListControl()
        {
            recordingsList = new BulkObservableCollection<Recording>();
            selectedSession = null;
            InitializeComponent();
            this.DataContext = this;
            RecordingsListView.ItemsSource = recordingsList;
            CreateSearchDialog();
        }

        private void SearchDialog_Closed(object sender, EventArgs e)
        {
            
            //CreateSearchDialog();
        }

        private void CreateSearchDialog()
        {
            searchDialog = new SearchDialog();
            searchDialog.Searched += SearchDialog_Searched;
            searchDialog.Closed += SearchDialog_Closed;
            searchTargets.Clear();
            foreach (var recording in recordingsList)
            {
                searchTargets.Add(recording.Id, -1, recording.RecordingNotes);
                searchTargets.AddRange(recording.Id, GetSegmentComments(recording));
                searchDialog.targetStrings = searchTargets.GetStringCollection();
            }
            if (SearchButton != null)
            {
                if (recordingsList.Count <= 0)
                {
                    SearchButton.IsEnabled = false;
                }
                else
                {
                    SearchButton.IsEnabled = true;
                }
            }
        }

        /// <summary>
        /// If the search dialog is activated, this event indicates that a search
        /// has been performed.  If successful the args will contain the found string,
        /// the search pattern and the index of the string in the string collection
        /// that was used to initialize the dialog.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void SearchDialog_Searched(object sender, EventArgs e)
        {
            try
            {
                SearchedEventArgs seArgs = e as SearchedEventArgs;
                //Debug.WriteLine("Found:- " + seArgs.searchPattern + " in " + seArgs.foundItem + " @ " + seArgs.indexOfFoundItem);
                //Debug.WriteLine("Resolves to:- " + searchTargets.searchableCollection[seArgs.indexOfFoundItem].ToString());
                if (RecordingsListView.SelectedIndex >= 0)
                {
                    var currentSelectedListBoxItem = this.RecordingsListView.ItemContainerGenerator.ContainerFromIndex(RecordingsListView.SelectedIndex) as ListBoxItem;
                    ListView lsegListView = Tools.FindDescendant<ListView>(currentSelectedListBoxItem);
                    if (lsegListView != null)
                    {
                        lsegListView.UnselectAll();
                    }
                    RecordingsListView.UnselectAll();
                }
                if (seArgs.indexOfFoundItem < 0) return;//not found
                var foundItem = searchTargets.searchableCollection[seArgs.indexOfFoundItem];
                if (foundItem == null) return; // invalid result

                int recordingId = foundItem.Item1;
                int segmentIndex = foundItem.Item2;

                Recording foundRecording = null;
                var recordings = (from rec in recordingsList
                                  where rec.Id == recordingId
                                  select rec);
                if (!recordings.IsNullOrEmpty())
                {
                    foundRecording = recordings.First();
                }
                RecordingsListView.Focus();
                RecordingsListView.SelectedItem = foundRecording;
                RecordingsListView.ScrollIntoView(foundRecording);

                if (segmentIndex >= 0)
                {
                    LabelledSegment foundSegment = foundRecording.LabelledSegments[segmentIndex];
                    if (foundSegment != null)
                    {
                        var currentSelectedListBoxItem = this.RecordingsListView.ItemContainerGenerator.ContainerFromIndex(RecordingsListView.SelectedIndex) as ListBoxItem;
                        ListView lsegListView = Tools.FindDescendant<ListView>(currentSelectedListBoxItem);
                        if (lsegListView != null)
                        {
                            lsegListView.UnselectAll();
                            lsegListView.SelectedItem = foundSegment;
                            //lsegListView.ScrollIntoView(foundSegment);
                            ListViewItem lvi = (ListViewItem)lsegListView.ItemContainerGenerator.ContainerFromItem(lsegListView.SelectedItem);
                            OnListViewItemFocused(lvi, new RoutedEventArgs());
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Tools.ErrorLog(ex.Message);
                Debug.WriteLine("Search internal error:-" + ex);
            }
            finally
            {
                (sender as SearchDialog).Focus();
            }
        }

        /// <summary>
        ///     Gets or sets the recordings list.
        /// </summary>
        /// <value>
        ///     The recordings list.
        /// </value>
        public BulkObservableCollection<Recording> recordingsList { get; } = new BulkObservableCollection<Recording>();

        /// <summary>
        ///     Gets or sets the selected session.
        /// </summary>
        /// <value>
        ///     The selected session.
        /// </value>
        public RecordingSession selectedSession
        {
            get
            {
                return (_selectedSession);
            }
            set
            {
                _selectedSession = value;
                Refresh();
            }
        }

        /// <summary>
        /// given an instance of a recording, returns the comments from all the LabelledSegments as an
        /// ObservableCollection of strings.
        /// </summary>
        /// <param name="recording"></param>
        /// <returns></returns>
        private BulkObservableCollection<string> GetSegmentComments(Recording recording)
        {
            BulkObservableCollection<string> result = new BulkObservableCollection<string>();
            foreach (var seg in recording.LabelledSegments)
            {
                result.Add(seg.Comment);
            }

            return (result);
        }

        /// <summary>
        ///     Called when [ListView item focused].
        /// </summary>
        /// <param name="sender">
        ///     The sender.
        /// </param>
        /// <param name="args">
        ///     The <see cref="RoutedEventArgs"/> instance containing the event data.
        /// </param>
        internal void OnListViewItemFocused(object sender, RoutedEventArgs args)
        {
            ListBoxItem lvi = sender as ListBoxItem;

            lvi.BringIntoView();
            lvi.IsSelected = true;
        }

        /// <summary>
        ///     Adds the edit recording.
        /// </summary>
        /// <param name="recording">
        ///     The recording.
        /// </param>
        private void AddEditRecording(Recording recording)
        {
            int oldIndex = RecordingsListView.SelectedIndex;
            if (selectedSession == null) return;
            if (recording == null) recording = new Recording();
            if (recording.RecordingSession == null)
            {
                recording.RecordingSessionId = selectedSession.Id;
            }

            RecordingForm recordingForm = new RecordingForm();
            recordingForm.recording = recording;

            if (recordingForm.ShowDialog() ?? false)
            {
                if (recordingForm.DialogResult ?? false)
                {
                    //DBAccess.UpdateRecording(recordingForm.recording, null);
                }
            }

            PopulateRecordingsList();
            if (oldIndex > 0 && oldIndex < RecordingsListView.Items.Count)
            {
                RecordingsListView.SelectedIndex = oldIndex;
            }

            if (recordingsList.Count <= 0)
            {
                SearchButton.IsEnabled = false;
            }
            else
            {
                SearchButton.IsEnabled = true;
            }
        }

        private void AddRecordingButton_Click(object sender, RoutedEventArgs e)
        {
            Recording recording = new Recording();
            AddEditRecording(recording);
            OnRecordingChanged(new EventArgs());
        }

        /// <summary>
        ///     Handles the Click event of the DeleteRecordingButton control. Deletes the selected
        ///     recording and removes it from the database
        /// </summary>
        /// <param name="sender">
        ///     The source of the event.
        /// </param>
        /// <param name="e">
        ///     The <see cref="RoutedEventArgs"/> instance containing the event data.
        /// </param>
        private void DeleteRecordingButton_Click(object sender, RoutedEventArgs e)
        {
            int oldIndex = RecordingsListView.SelectedIndex;
            if (RecordingsListView.SelectedItem != null)
            {
                DBAccess.DeleteRecording(RecordingsListView.SelectedItem as Recording);
            }
            PopulateRecordingsList();
            if (oldIndex >= 0 && oldIndex < RecordingsListView.Items.Count)
            {
                RecordingsListView.SelectedIndex = oldIndex;
            }
            if (recordingsList.Count <= 0)
            {
                SearchButton.IsEnabled = false;
            }
            else
            {
                SearchButton.IsEnabled = true;
            }
            OnRecordingChanged(new EventArgs());
        }

        private void EditRecordingButton_Click(object sender, RoutedEventArgs e)
        {
            Recording recording = new Recording();
            if (RecordingsListView.SelectedItem != null)
            {
                recording = (RecordingsListView.SelectedItem as Recording);
            }
            AddEditRecording(recording);
            OnRecordingChanged(new EventArgs());
            if (recordingsList.Count > 0)
            {
                SearchButton.IsEnabled = true;
            }
            else
            {
                DisableSearchButton();
            }
        }

        private void DisableSearchButton()
        {
            SearchButton.IsEnabled = false;
        }

        /// <summary>
        /// Searches the comments in this session for matching strings.
        /// The search string may be a regular expression.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void SearchButton_Click(object sender, RoutedEventArgs e)
        {
            if (searchDialog.IsLoaded)
            {
                searchDialog.Visibility = Visibility.Visible;
                searchDialog.Focus();
                searchDialog.FindNextButton_Click(sender, e);
            }
            else
            {
                searchDialog = null;
                CreateSearchDialog();
                searchDialog.Show();
            }
            this.UpdateLayout();
            searchDialog.Activate();
        }

        /// <summary>
        ///     Populates the segment list. The recordingSessionControl has been automatically
        ///     updated by writing the selected session to it. This function uses the selected
        ///     recordingSession to fill in the list of LabelledSegments.
        /// </summary>
        private void PopulateRecordingsList()
        {
            // TODO each recording will give access to a passes summary these should be merged into
            // a session summary and each 'bat' in the summary must be added to the SessionSummaryStackPanel

            if (selectedSession != null)
            {
                recordingsList.Clear();
                //recordingsList.AddRange(DBAccess.GetRecordingsForSession(selectedSession));
                recordingsList.AddRange(selectedSession.Recordings);
            }
            else
            {
                recordingsList.Clear();
            }
            if (RecordingsListView != null)
            {
                //RecordingsListView.ItemsSource = recordingsList;
                //var view = CollectionViewSource.GetDefaultView(RecordingsListView.ItemsSource);
                //if (view != null) { view.Refresh(); }
            }
            if (recordingsList.Count > 0)
            {
                SearchButton.IsEnabled = true;
            }
            else
            {
                SearchButton.IsEnabled = false;
            }
        }

        private void RecordingsListView_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (!e.Handled)
            {
                IsSegmentSelected = false;
                e.Handled = true;
                if (e.AddedItems != null && e.AddedItems.Count > 0)
                {
                    if (AddRecordingButton != null && DeleteRecordingButton != null && EditRecordingButton != null)
                    {
                        EditRecordingButton.IsEnabled = true;
                        DeleteRecordingButton.IsEnabled = true;
                        if (RecordingsListView.Items != null)
                        {
                            foreach (var item in RecordingsListView.Items)
                            {
                                if (item is ListView)
                                {
                                    if ((item as ListView).SelectedIndex >= 0)
                                    {
                                        IsSegmentSelected = true;
                                    }
                                    else
                                    {
                                        IsSegmentSelected = false;
                                    }
                                }
                            }
                        }
                        if (!IsSegmentSelected)
                        {
                            AddSegImgButton.Content = "Add Segment";
                            AddSegImgButton.IsEnabled = true;
                            Debug.WriteLine("Recording Selected");
                        }

                        IsSegmentSelected = false;
                    }
                }
                else
                {
                    if (AddRecordingButton != null && DeleteRecordingButton != null && EditRecordingButton != null)
                    {
                        EditRecordingButton.IsEnabled = false;
                        DeleteRecordingButton.IsEnabled = false;
                        AddSegImgButton.IsEnabled = false;
                        Debug.WriteLine("Recording De-Selected");
                        IsSegmentSelected = false;
                    }
                }
            }
        }

        private void RecordingNameLabel_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            Label thisLabel = sender as Label;
            Recording recording = RecordingsListView.SelectedItem as Recording;
            string labelContent = thisLabel.Content as string;
            if (!String.IsNullOrWhiteSpace(labelContent))
            {
                if (labelContent.ToUpper().Contains(".WAV"))
                {
                    int pos = labelContent.ToUpper().IndexOf(".WAV");
                    labelContent = labelContent.Substring(0, pos) + ".wav";
                    labelContent = selectedSession.OriginalFilePath + labelContent;

                    Tools.OpenWavFile(recording);
                }
            }
        }

        private void GPSLabel_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            Label thisLabel = sender as Label;
            string labelContent = thisLabel.Content as String;
            if (!String.IsNullOrWhiteSpace(labelContent))
            {
                if (labelContent.Contains(","))
                {
                    var numbers = labelContent.Split(',');
                    if (numbers.Count() >= 2)
                    {
                        double lat = -200.0d;
                        double longit = -200.0d;
                        double.TryParse(numbers[0].Trim(), out lat);
                        double.TryParse(numbers[1].Trim(), out longit);
                        if (Math.Abs(lat) <= 90.0d && Math.Abs(longit) <= 180.0d)
                        {
                            Location oldLocation = new Location(lat, longit);
                            MapWindow mapWindow = new MapWindow(false);

                            mapWindow.mapControl.mapControl.Center = oldLocation;
                            mapWindow.mapControl.AddPushPin(oldLocation);

                            mapWindow.Title = mapWindow.Title + " Recording Location";
                            mapWindow.Show();
                        }
                    }
                }
            }
        }

        /// <summary>
        /// If the Calls toggle button is Checked then the call parameter data must be
        /// made visible.  If there is no such data the button should be disabled
        /// anyway
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void CallsToggleButton_Checked(object sender, RoutedEventArgs e)
        {
        }

        /// <summary>
        /// Unchecking the Calls toggle button hides the call parameter data associated
        /// with any relevnat segments.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void CallsToggleButton_Unchecked(object sender, RoutedEventArgs e)
        {
        }

        private void LabelledSegmentTextBlock_MouseEnter(object sender, MouseEventArgs e)
        {
            if (CallsToggleButton.IsChecked ?? false)
            {
                if (sender is TextBlock segTextBlock)
                {
                    BatCallControl callControl = new BatCallControl();

                    LabelledSegment seg = segTextBlock.DataContext as LabelledSegment;
                    if (seg.SegmentCalls != null && seg.SegmentCalls.Count > 0)
                    {
                        Call call = seg.SegmentCalls[0].Call;
                        if (call != null)
                        {
                            callControl.BatCall = call;
                        }
                    (segTextBlock.Parent as StackPanel).Children.Add(callControl);
                    }
                }
            }
        }

        private void LabelledSegmentTextBlock_MouseLeave(object sender, MouseEventArgs e)
        {
            if (CallsToggleButton.IsChecked ?? false)
            {
                if (sender is TextBlock segTextBlock)
                {
                    for (int i = (segTextBlock.Parent as StackPanel).Children.Count - 1; i >= 0; i--)
                    {
                        var child = (segTextBlock.Parent as StackPanel).Children[i];
                        if (child.GetType() == typeof(BatCallControl))
                        {
                            (segTextBlock.Parent as StackPanel).Children.RemoveAt(i);
                        }
                    }
                }
            }
        }

        private readonly object RecordingChangedEventLock = new object();
        private EventHandler<EventArgs> RecordingChangedEvent;

#pragma warning disable CS1574 // XML comment has cref attribute that could not be resolved

        /// <summary>
        /// Event raised after the <see cref="Text" /> property value has changed.
        /// </summary>
        public event EventHandler<EventArgs> RecordingChanged
#pragma warning restore CS1574 // XML comment has cref attribute that could not be resolved
        {
            add
            {
                lock (RecordingChangedEventLock)
                {
                    RecordingChangedEvent += value;
                }
            }
            remove
            {
                lock (RecordingChangedEventLock)
                {
                    RecordingChangedEvent -= value;
                }
            }
        }

        /// <summary>
        /// Raises the <see cref="RecordingChanged" /> event.
        /// </summary>
        /// <param name="e"><see cref="EventArgs" /> object that provides the arguments for the event.</param>
        protected virtual void OnRecordingChanged(EventArgs e)
        {
            EventHandler<EventArgs> handler = null;

            lock (RecordingChangedEventLock)
            {
                handler = RecordingChangedEvent;

                if (handler == null)
                    return;
            }

            handler(this, e);
        }

        private bool IsSegmentSelected = false;
        private LabelledSegment selectedSegment = null;

        private void LabelledSegmentListView_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (!e.Handled)
            {
                e.Handled = true;
                if (e.AddedItems != null && e.AddedItems.Count > 0)
                {
                    LabelledSegment selectedSegment = e.AddedItems[e.AddedItems.Count - 1] as LabelledSegment;
                    ListView lvSender = sender as ListView;

                    Debug.WriteLine("LabelledSegmentListView_SelectionChanged:- Selected <" + selectedSegment.Comment + ">");
                    var images = selectedSegment.GetImageList();
                    OnSegmentSelectionChanged(new ImageListEventArgs(selectedSegment, images));

                    AddSegImgButton.Content = "Add Image";
                    AddSegImgButton.IsEnabled = true;
                    IsSegmentSelected = true;
                    this.selectedSegment = selectedSegment;
                    bool IsRecordingSelected = false;
                    if (RecordingsListView.SelectedItem != null)
                    {
                        if (selectedSegment.RecordingID == (RecordingsListView.SelectedItem as Recording).Id)
                        {
                            IsRecordingSelected = true;
                        }
                    }
                    if (!IsRecordingSelected)
                    {
                        RecordingsListView.SelectedItem = selectedSegment.Recording;
                    }
                }
                else
                {
                    Debug.WriteLine("LabelledSegmentListView_SelectionChanged-RESET");
                    AddSegImgButton.Content = "Add Segment";
                    IsSegmentSelected = false;
                    selectedSegment = null;
                }
            }
        }

        private readonly object SegmentSelectionChangedEventLock = new object();
        private EventHandler<EventArgs> SegmentSelectionChangedEvent;

#pragma warning disable CS1574 // XML comment has cref attribute that could not be resolved

        /// <summary>
        /// Event raised after the <see cref="Text" /> property value has changed.
        /// </summary>
        [Category("Property Changed")]
#pragma warning restore CS1574 // XML comment has cref attribute that could not be resolved
        [Description("Event raised after the SegmentSelection property value has changed.")]
        public event EventHandler<EventArgs> SegmentSelectionChanged
        {
            add
            {
                lock (SegmentSelectionChangedEventLock)
                {
                    SegmentSelectionChangedEvent += value;
                }
            }
            remove
            {
                lock (SegmentSelectionChangedEventLock)
                {
                    SegmentSelectionChangedEvent -= value;
                }
            }
        }

        /// <summary>
        /// Raises the <see cref="SegmentSelectionChanged" /> event.
        /// </summary>
        /// <param name="e"><see cref="EventArgs" /> object that provides the arguments for the event.</param>
        protected virtual void OnSegmentSelectionChanged(EventArgs e)
        {
            EventHandler<EventArgs> handler = null;

            lock (SegmentSelectionChangedEventLock)
            {
                handler = SegmentSelectionChangedEvent;
            }

            handler?.Invoke(this, e);
        }

        private void AddSegImgButton_Click(object sender, RoutedEventArgs e)
        {
            Button button = sender as Button;
            Recording selectedRecording = RecordingsListView.SelectedItem as Recording;
            BulkObservableCollection<SegmentAndBatList> processedSegments = new BulkObservableCollection<SegmentAndBatList>();
            BulkObservableCollection<BulkObservableCollection<StoredImage>> listOfCallImageLists = new BulkObservableCollection<BulkObservableCollection<StoredImage>>();
            StoredImage newImage = null;

            if (button != null && (button.Content as string) == "Add Segment")
            {
                LabelledSegmentForm segmentForm = new LabelledSegmentForm();

                segmentForm.labelledSegment = new LabelledSegment();

                var result = segmentForm.ShowDialog();
                {
                    if (result ?? false)
                    {
                        if (selectedRecording != null)
                        {
                            selectedRecording.LabelledSegments.Add(segmentForm.labelledSegment);
                        }
                        else
                        {
                            return; // dialog was cancelled
                        }
                    }
                }
            }
            else if (button != null && (button.Content as string) == "Add Image")
            {
                ImageDialog imageDialog = new ImageDialog(selectedSegment != null ? (selectedSegment.Recording != null ? selectedSegment.Recording.RecordingName : "image caption") : "image caption",
                    selectedSegment != null ? selectedSegment.Comment : "new image");
                var result = imageDialog.ShowDialog();
                result = imageDialog.DialogResult;
                if (result ?? false)
                {
                    newImage = imageDialog.GetStoredImage();
                }
                else
                {
                    return; // dialog was cancelled
                }
            }

            // applies to both types of add...
            if (selectedSegment != null && selectedSegment.Recording != null)
            {
                selectedRecording = selectedSegment.Recording;
            }
            else
            {
                if ((RecordingsListView.SelectedItem as Recording) != null)
                {
                    selectedRecording = RecordingsListView.SelectedItem as Recording;
                }
            }
            if (selectedRecording == null) return;
            foreach (var seg in selectedRecording.LabelledSegments)
            {
                LabelledSegment segment = seg as LabelledSegment;

                BulkObservableCollection<Bat> bats = DBAccess.GetDescribedBats(segment.Comment);
                String segmentLine = Tools.FormattedSegmentLine(segment);
                SegmentAndBatList thisProcessedSegment = SegmentAndBatList.ProcessLabelledSegment(segmentLine, bats);
                thisProcessedSegment.segment = segment;
                processedSegments.Add(thisProcessedSegment);
                var segmentImageList = segment.GetImageList();
                if (newImage != null)
                {
                    if (segment.Id == selectedSegment.Id)
                    {
                        segmentImageList.Add(newImage);
                    }
                }
                listOfCallImageLists.Add(segmentImageList);
            }
            DBAccess.UpdateRecording(selectedRecording, processedSegments, listOfCallImageLists);
            //this.Parent.RefreshData();
            Tools.FindParent<RecordingSessionListDetailControl>(this).RefreshData();
            this.Refresh();
        }

        private void Refresh()
        {
            int oldIndex = -1;
            if (RecordingsListView != null)
            {
                oldIndex = RecordingsListView.SelectedIndex;
            }
            if (selectedSession != null)
            {
                recordingsList.Clear();
                //recordingsList.AddRange(DBAccess.GetRecordingsForSession(value));
                recordingsList.AddRange(selectedSession.Recordings);
                if (AddRecordingButton != null && DeleteRecordingButton != null && EditRecordingButton != null)
                {
                    AddRecordingButton.IsEnabled = true;
                    DeleteRecordingButton.IsEnabled = false;
                    EditRecordingButton.IsEnabled = false;
                    SearchButton.IsEnabled = false;
                }
            }
            else
            {
                recordingsList.Clear();
                if (AddRecordingButton != null && DeleteRecordingButton != null && EditRecordingButton != null)
                {
                    AddRecordingButton.IsEnabled = false;
                    DeleteRecordingButton.IsEnabled = false;
                    EditRecordingButton.IsEnabled = false;
                }
            }
            if (RecordingsListView != null)
            {
                RecordingsListView.SelectedIndex = (oldIndex >= 0 && oldIndex < recordingsList.Count) ? oldIndex : -1;
                RecordingsListView.ItemsSource = recordingsList;
                var view = CollectionViewSource.GetDefaultView(RecordingsListView.ItemsSource);
                if (view != null) { view.Refresh(); }
            }

            CreateSearchDialog();
        }

        private void LabelledSegmentTextBlock_MouseRightButtonUp(object sender, MouseButtonEventArgs e)
        {
            
            var textBlock = sender as TextBlock;
            LabelledSegment selectedSegment = textBlock.DataContext as LabelledSegment;

            Debug.WriteLine("right clicked on:-" + selectedSegment.Recording.RecordingName + " at " + selectedSegment.StartOffset + " - " + selectedSegment.EndOffset);
            string wavFile = selectedSegment.Recording.RecordingSession.OriginalFilePath + selectedSegment.Recording.RecordingName;
            wavFile = wavFile.Replace(@"\\", @"\");
            if (File.Exists(wavFile))
            {
                Tools.OpenWavFile(wavFile, selectedSegment.StartOffset, selectedSegment.EndOffset);
            }
            e.Handled = true;
        }

        private void LabelledSegmentListView_PreviewMouseWheel(object sender, MouseWheelEventArgs e)
        {
            if (!e.Handled)
            {
                e.Handled = true;
                var eventArg = new MouseWheelEventArgs(e.MouseDevice, e.Timestamp, e.Delta);
                eventArg.RoutedEvent = UIElement.MouseWheelEvent;
                eventArg.Source = sender;
                var parent = ((Control)sender).Parent as UIElement;
                parent.RaiseEvent(eventArg);
            }
        }

        private void PlayButton_Click(object sender, RoutedEventArgs e)
        {
            //var playWindow = new AudioPlayer();
            //playWindow.Show();
            
            List<LabelledSegment> selection = GetSelectedSegments();
            if (selection.IsNullOrEmpty())
            {
                selection = GetSegmentsForSelectedRecordings();
            }
            if (!selection.IsNullOrEmpty())
            {
                foreach(var sel in selection)
                {
                    AudioHost.Instance.audioPlayer.AddToList(sel);
                }
            }
        }


        private List<LabelledSegment> GetSegmentsForSelectedRecordings()
        {
            List<LabelledSegment> result = new List<LabelledSegment>();
            if (RecordingsListView.SelectedItem != null)
            {
                Recording selectedRecording = RecordingsListView.SelectedItem as Recording;
                foreach(var segment in selectedRecording.LabelledSegments)
                {
                    if ((segment.Duration() ?? new TimeSpan()).Ticks > 0L)
                    {
                        result.Add(segment);
                    }
                }
            }
            return (result);
        }

        private List<LabelledSegment> GetSelectedSegments()
        {
            List<LabelledSegment> result = new List<LabelledSegment>();
            if (selectedSegment != null)
            {
                result.Add(selectedSegment);
            }
            if((selectedSegment.Duration()??new TimeSpan()).Ticks==0L)
            {
                return (GetSegmentsForSelectedRecordings());
            }
            return (result);
        }

        private void ContentControl_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (sender is ContentControl)
            {
                if ((sender as ContentControl).Content is TextBlock)
                {

                    LabelledSegmentTextBlock_MouseRightButtonUp((sender as ContentControl).Content, e);
                    e.Handled = true;
                }
            }
        }
    }// End of Class RecordingDetailListControl

    /////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    /////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    /////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    /// <summary>
    /// Provides arguments for an event.
    /// </summary>
    [Serializable]
    public class ImageListEventArgs : EventArgs
    {
#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member
        public new static readonly ImageListEventArgs Empty = new ImageListEventArgs(null, new BulkObservableCollection<StoredImage>());
#pragma warning restore CS1591 // Missing XML comment for publicly visible type or member

        #region Public Properties

#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member
        public BulkObservableCollection<StoredImage> imageList { get; } = new BulkObservableCollection<StoredImage>();
#pragma warning restore CS1591 // Missing XML comment for publicly visible type or member
#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member
        public object listOwner;
#pragma warning restore CS1591 // Missing XML comment for publicly visible type or member

        #endregion Public Properties



        #region Constructors
#pragma warning disable CS1574 // XML comment has cref attribute that could not be resolved

        /// <summary>
        /// Constructs a new instance of the <see cref="CustomEventArgs" /> class.
        /// </summary>
        public ImageListEventArgs(object ListOwner, BulkObservableCollection<StoredImage> ImageList)
#pragma warning restore CS1574 // XML comment has cref attribute that could not be resolved
        {
            imageList.Clear();
            if (ImageList != null && ImageList.Count > 0)
            {
                foreach (var im in ImageList)
                {
                    imageList.Add(im);
                }
            }
            listOwner = ListOwner;
        }

        #endregion Constructors
    }

    #region RecordingToGPSConverter (ValueConverter)

    /// <summary>
    ///     Converter to extract GPS data from a recording instance and format it into a string
    /// </summary>
    public class RecordingToGPSConverter : IValueConverter
    {
        /// <summary>
        ///     Converts the specified value.
        /// </summary>
        /// <param name="value">
        ///     The value.
        /// </param>
        /// <param name="targetType">
        ///     Type of the target.
        /// </param>
        /// <param name="parameter">
        ///     The parameter.
        /// </param>
        /// <param name="culture">
        ///     The culture.
        /// </param>
        /// <returns>
        ///     </returns>
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            try
            {
                // Here's where you put the code do handle the value conversion.
                String result = "";
                if (value is Recording recording)
                {
                    if (!String.IsNullOrWhiteSpace(recording.RecordingGPSLatitude) && !String.IsNullOrWhiteSpace(recording.RecordingGPSLongitude))
                    {
                        result = recording.RecordingGPSLatitude + ", " + recording.RecordingGPSLongitude;
                    }
                    else
                    {
                        if (recording.RecordingEndTime != null && recording.RecordingStartTime != null)
                        {
                            result = recording.RecordingStartTime.Value.ToString(@"hh\:mm\:ss") + " - " + recording.RecordingEndTime.Value.ToString(@"hh\:mm\:ss");
                        }
                    }
                }
                return (result);
            }
            catch
            {
                return "";
            }
        }

        /// <summary>
        ///     Not implemented
        /// </summary>
        /// <param name="value">
        ///     The value.
        /// </param>
        /// <param name="targetType">
        ///     Type of the target.
        /// </param>
        /// <param name="parameter">
        ///     The parameter.
        /// </param>
        /// <param name="culture">
        ///     The culture.
        /// </param>
        /// <returns>
        ///     </returns>
        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            // Not implemented
            return null;
        }
    }

    #endregion RecordingToGPSConverter (ValueConverter)

    #region RecordingDetailsConverter (ValueConverter)

    /// <summary>
    ///     Converts the essential details of a Recording instance to a string
    /// </summary>
    public class RecordingDetailsConverter : IValueConverter
    {
        /// <summary>
        ///     Converts the specified value.
        /// </summary>
        /// <param name="value">
        ///     The value.
        /// </param>
        /// <param name="targetType">
        ///     Type of the target.
        /// </param>
        /// <param name="parameter">
        ///     The parameter.
        /// </param>
        /// <param name="culture">
        ///     The culture.
        /// </param>
        /// <returns>
        ///     </returns>
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            try
            {
                // Here's where you put the code do handle the value conversion.
                string result = "";

                if (value is Recording recording)
                {
                    TimeSpan dur = new TimeSpan();

                    dur = Tools.GetRecordingDuration(recording);

                    string durStr = dur.ToString(@"dd\:hh\:mm\:ss");
                    while (durStr.StartsWith("00:"))
                    {
                        durStr = durStr.Substring(3);
                    }
                    string recDate = "";
                    if (recording.RecordingDate != null)
                    {
                        recDate = recording.RecordingDate.Value.ToShortDateString();
                        if (recording.RecordingDate.Value.Hour > 0 || recording.RecordingDate.Value.Minute > 0 || recording.RecordingDate.Value.Second > 0)
                        {
                            recDate = recDate + " " + recording.RecordingDate.Value.ToShortTimeString();
                        }
                    }

                    result = recording.RecordingName + " " + recDate + " " + durStr;
                }

                return result;
            }
            catch
            {
                return "";
            }
        }

        /// <summary>
        ///     ConvertBack not implemented
        /// </summary>
        /// <param name="value">
        ///     The value.
        /// </param>
        /// <param name="targetType">
        ///     Type of the target.
        /// </param>
        /// <param name="parameter">
        ///     The parameter.
        /// </param>
        /// <param name="culture">
        ///     The culture.
        /// </param>
        /// <returns>
        ///     </returns>
        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            // Not implemented
            return null;
        }
    }

    #endregion RecordingDetailsConverter (ValueConverter)

    #region RecordingPassSummaryConverter (ValueConverter)

    /// <summary>
    ///     From an instance of Recording provides a list of strings summarising the number of
    ///     passes organised by type of bat
    /// </summary>
    public class RecordingPassSummaryConverter : IValueConverter
    {
        /// <summary>
        ///     Converts the specified value.
        /// </summary>
        /// <param name="value">
        ///     The value.
        /// </param>
        /// <param name="targetType">
        ///     Type of the target.
        /// </param>
        /// <param name="parameter">
        ///     The parameter.
        /// </param>
        /// <param name="culture">
        ///     The culture.
        /// </param>
        /// <returns>
        ///     </returns>
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            try
            {
                // Here's where you put the code do handle the value conversion.
                String result = "";
                if (value is Recording recording)
                {
                    var summary = recording.GetStats();
                    if (summary != null && summary.Count > 0)
                    {
                        foreach (var batType in summary)
                        {
                            result = result + "\n" + (Tools.GetFormattedBatStats(batType, false));
                        }
                    }
                }

                return (result);
            }
            catch
            {
                return "";
            }
        }

        /// <summary>
        ///     ConvertBack not implemented
        /// </summary>
        /// <param name="value">
        ///     The value.
        /// </param>
        /// <param name="targetType">
        ///     Type of the target.
        /// </param>
        /// <param name="parameter">
        ///     The parameter.
        /// </param>
        /// <param name="culture">
        ///     The culture.
        /// </param>
        /// <returns>
        ///     </returns>
        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            // Not implemented
            return null;
        }
    }

    #endregion RecordingPassSummaryConverter (ValueConverter)
}