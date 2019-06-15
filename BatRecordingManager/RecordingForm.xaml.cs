using Microsoft.Maps.MapControl.WPF;
using Microsoft.VisualStudio.Language.Intellisense;
using System;
using System.Diagnostics;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;

namespace BatRecordingManager
{
    /*
	RecordingDetailGrid
	RecordingNameTextBox - Binding {RecordinName}
	BrowseForFileButton
	GPSLatitudeTextBox - Binding {RecordingGPSLatitude}
	GPSLongitudeTextBox - Binding {RecordingGPSLongitude}
	StartTimeTimePicker - Binding {RecordingStartTime}
	EndTimeTimePicker - Binding {RecordingEndTime}
	RecordingNotesTextBox - Binding {RecordingNotes}
	LabelledSegmentsListView
	ButtonBarStackPanel
	OKButton
	CancelButton

		RecordingForm
	LabelledSegmentsList(P)
	ModifiedFlag
	recording(P)
		Get
			populates recording from form fields
		Set
			populates LabelledSegmentsList and ModifiedFlag from value
			populates form fields from value
	RecordingForm()
		DataContext=this
	AddEditSegment()
		Creates and Shows segmentForm
		if OK adds the new segment to the recording
	AddSegmentButton_Click - calls AddEditSegment
	BrowseForFileButton_Click()
		Creates a new FileBrowser()
		fileBrowser.SelectWavFile()
		recording.RecordingName=selected file name
		updates the form RecordingName text box
	ButtonSaveSegment_Click()
		Extracts the segment for the specific button clicked
		FileProcessor.IsLabelFileLine() extracts times and comments
		segment modified with new values
		Button set to hidden
	CancelButton_Click()
		sets DialogResult to false
		closes the dialog
	DeleteSegmentButton_Click()
		extracts the selected segment
		Displays warning MessageBox for confirmation
		DBAccess.DeleteSegment()
		DBAccess.GetRecording() // loses any added segments not yet committed
		sets the selected segment to be the same or nearest index
	OKButton_Click()
		foreach segment in the listview
			DBAccess.GetDescribedBats()
			Tools.FormattedSegmentLine()
			FileProcessor.ProcessLabelledSegment(formattedLine,describedBats)
			set segment ID
			processedSegments.Add()
		DBAccess.UpdateRecording(recording,processedSegments)
		set DialogResult
		Close dialog
	OnListViewItemFocused()
		sender as ListViewItem.Iselected=true
	OnTextBoxFocused()
		extract Button connected to this textbox
		make the button Visible

	*/

    /// <summary>
    ///     Interaction logic for RecordingForm.xaml
    /// </summary>
    public partial class RecordingForm : Window
    {
        #region LabelledSegmentsList

        /// <summary>
        ///     LabelledSegmentsList Dependency Property
        /// </summary>
        public static readonly DependencyProperty LabelledSegmentsListProperty =
            DependencyProperty.Register("LabelledSegmentsList", typeof(BulkObservableCollection<LabelledSegment>), typeof(RecordingForm),
                new FrameworkPropertyMetadata((BulkObservableCollection<LabelledSegment>)new BulkObservableCollection<LabelledSegment>()));

        /// <summary>
        ///     Gets or sets the LabelledSegmentsList property. This dependency property indicates ....
        /// </summary>
        public BulkObservableCollection<LabelledSegment> LabelledSegmentsList
        {
            get
            {
                return (BulkObservableCollection<LabelledSegment>)GetValue(LabelledSegmentsListProperty);
            }
            set
            {
                SetValue(LabelledSegmentsListProperty, value);
                ModifiedFlag = new BulkObservableCollection<bool>();
                RecordingFormImageScroller.Clear();
                if (value.Count > 0)
                {
                    foreach (var item in value)
                    {
                        ModifiedFlag.Add(false);
                        RecordingFormImageScroller.ListofCallImageLists.Add(item.GetImageList());
                    }
                }
            }
        }

        #endregion LabelledSegmentsList

        /// <summary>
        /// list with an element for each labelled segment.  Each segment has a list of
        /// images which may (and often will) be empty
        /// </summary>

        /// <summary>
        ///     Initializes a new instance of the <see cref="RecordingForm"/> class.
        /// </summary>
        public RecordingForm()
        {
            InitializeComponent();
            this.DataContext = this;
            RecordingFormImageScroller.ImageScrollerDisplaysBatImages = false;
            RecordingFormImageScroller.ImageDeleted += RecordingFormImageScroller_ImageDeleted;
            RecordingFormImageScroller.UnassociatedImage += RecordingFormImageScroller_UnassociatedImage;
        }

        /// <summary>
        /// AutoFill uses data from the recordingSession parameter to partially fill the new Recording instance
        /// created in the form, using the tag plu .wav as the file name and copying the sessions GPS
        /// co-ordinates.  The session notes are copied to the recording notes and then split into lines
        /// each of which is used to create a new LabelledSegment with start and end times set to 0.
        /// The RecordingForm may then be displayed for the user to modify (by the calling process) and the
        /// recording and segments are saved by the OK button.
        /// </summary>
        /// <param name="recordingSession"></param>
        internal void AutoFill(RecordingSession recordingSession)
        {
            Recording recording = new Recording();
            recording.RecordingSessionId = recordingSession.Id;
            if (string.IsNullOrWhiteSpace(recording.RecordingName))
            {
                recording.RecordingName = recordingSession.SessionTag + ".wav";
            }
            recording.RecordingDate = recordingSession.SessionDate;
            if (string.IsNullOrWhiteSpace(recording.RecordingGPSLatitude) || recording.RecordingGPSLatitude.Trim().StartsWith("0"))
            {
                recording.RecordingGPSLatitude = recordingSession.LocationGPSLatitude.ToString();
                recording.RecordingGPSLongitude = recordingSession.LocationGPSLongitude.ToString();
            }
            recording.RecordingStartTime = recordingSession.SessionStartTime;
            recording.RecordingEndTime = recordingSession.SessionEndTime;
            if (string.IsNullOrWhiteSpace(recording.RecordingNotes))
            {
                recording.RecordingNotes = recordingSession.SessionNotes;
            }

            if (!string.IsNullOrWhiteSpace(recording.RecordingNotes))
            {
                LabelledSegmentsList.Clear();
                ModifiedFlag.Clear();
                recording.LabelledSegments.Clear();
                var lines = recording.RecordingNotes.Split('\n');
                if (!lines.IsNullOrEmpty())
                {
                    foreach (var line in lines)
                    {
                        LabelledSegment newSegment = new LabelledSegment();
                        newSegment.Comment = line;
                        recording.LabelledSegments.Add(newSegment);
                        ModifiedFlag.Add(true);
                    }
                    LabelledSegmentsList.Clear();
                    LabelledSegmentsList.AddRange(recording.LabelledSegments);
                    var view = CollectionViewSource.GetDefaultView(LabelledSegmentsListView.ItemsSource);
                    if (view != null) view.Refresh();
                    Debug.WriteLine(LabelledSegmentsListView.Items.Count);
                }
            }
            this.recording = recording;
        }

        private BulkObservableCollection<bool> ModifiedFlag = new BulkObservableCollection<bool>();

        #region recording

        /// <summary>
        ///     recording Dependency Property
        /// </summary>
        public static readonly DependencyProperty recordingProperty =
            DependencyProperty.Register("recording", typeof(Recording), typeof(RecordingForm),
                new FrameworkPropertyMetadata((Recording)new Recording()));

        /// <summary>
        ///     Gets or sets the recording property. This dependency property indicates ....
        /// </summary>
        public Recording recording
        {
            get
            {
                Recording recording = (Recording)GetValue(recordingProperty);
                recording.RecordingEndTime = new TimeSpan((EndTimeTimePicker.Value ?? DateTime.Now).Ticks);
                recording.RecordingStartTime = new TimeSpan((StartTimeTimePicker.Value ?? DateTime.Now).Ticks);
                DateTime date = DateTime.Now;
                if (recording.RecordingSession != null)
                {
                    recording.RecordingDate = recording.RecordingSession.SessionDate;
                }
                if (RecordingDatePicker.SelectedDate != null)
                {
                    recording.RecordingDate = RecordingDatePicker.SelectedDate.Value;
                }
                recording.RecordingName = RecordingNameTextBox.Text;
                recording.RecordingNotes = RecordingNotesTextBox.Text;
                recording.RecordingGPSLatitude = GPSLatitudeTextBox.Text;
                recording.RecordingGPSLongitude = GPSLongitudeTextBox.Text;

                return (recording);
            }
            set
            {
                using (new WaitCursor("Collecting recording data..."))
                {
                    SetValue(recordingProperty, value);
                    LabelledSegmentsList = new BulkObservableCollection<LabelledSegment>();
                    RecordingFormImageScroller.Clear();
                    if (value != null)
                    {
                        RecordingFormImageScroller.defaultCaption = value.RecordingName;
                        RecordingFormImageScroller.defaultDescription = value.RecordingNotes;
                        if (!value.LabelledSegments.IsNullOrEmpty())
                        {
                            LabelledSegmentsList.AddRange(value.LabelledSegments);
                            if (value.LabelledSegments != null)
                            {
                                foreach (var seg in value.LabelledSegments)
                                {
                                    RecordingFormImageScroller.ListofCallImageLists.Add(seg.GetImageList());
                                    string key = value.RecordingName + " - " + seg.StartOffset.ToString();
                                    while (RecordingFormImageScroller.ListOfCaptionAndDescription.ContainsKey(key)) key = key + " ";
                                    RecordingFormImageScroller.ListOfCaptionAndDescription.Add(key, seg.Comment);
                                }
                            }
                        }
                    }

                    //LabelledSegmentsListView.ItemsSource = LabelledSegmentsList;
                    DateTime date = DateTime.Now;
                    if (value.RecordingSession != null && value.RecordingSession.SessionDate != null)
                    {
                        date = value.RecordingSession.SessionDate;
                    }
                    RecordingDatePicker.SelectedDate = value.RecordingDate ?? date;
                    EndTimeTimePicker.Value = new DateTime((value.RecordingEndTime ?? new TimeSpan(22, 0, 0)).Ticks);
                    StartTimeTimePicker.Value = new DateTime((value.RecordingStartTime ?? new TimeSpan(18, 0, 0)).Ticks);
                    RecordingNameTextBox.Text = value.RecordingName ?? "";
                    RecordingNotesTextBox.Text = value.RecordingNotes ?? "";
                    GPSLatitudeTextBox.Text = value.RecordingGPSLatitude ?? "";
                    GPSLongitudeTextBox.Text = value.RecordingGPSLongitude ?? "";
                    //isSegmentModified = false;
                }
            }
        }

        #endregion recording

        /// <summary>
        ///     Adds a new segment. Editing is not done here but is done in place.
        /// </summary>
        /// <param name="segmentToEdit">
        ///     The segment to edit.
        /// </param>
        private bool AddEditSegment(LabelledSegment segmentToEdit)
        {
            LabelledSegmentForm segmentForm = new LabelledSegmentForm();
            //int os = LabelledSegmentsListView.SelectedIndex;
            if (segmentToEdit == null)
            {
                segmentForm.labelledSegment = new LabelledSegment();
            }
            else
            {
                segmentForm.labelledSegment = segmentToEdit;
            }

            if ((segmentForm.ShowDialog() ?? false))
            {
                if (segmentForm.DialogResult ?? false)
                {
                    Debug.WriteLine("AdEditSegment OK");
                    Debug.Write(recording.LabelledSegments.Count + " -> ");
                    RecordingFormImageScroller.ListofCallImageLists.Add(new BulkObservableCollection<StoredImage>());
                    recording.LabelledSegments.Add(segmentForm.labelledSegment);
                    LabelledSegmentsList.Clear();
                    LabelledSegmentsList.AddRange(recording.LabelledSegments);

                    var view = CollectionViewSource.GetDefaultView(LabelledSegmentsListView.ItemsSource);
                    if (view != null) view.Refresh();
                    Debug.WriteLine(LabelledSegmentsListView.Items.Count);

                    return (true);
                }
            }
            return (false);
        }

        /// <summary>
        ///     Handles the 1 event of the AddSegmentButton_Click control.
        /// </summary>
        /// <param name="sender">
        ///     The source of the event.
        /// </param>
        /// <param name="e">
        ///     The <see cref="RoutedEventArgs"/> instance containing the event data.
        /// </param>
        private void AddSegmentButton_Click_1(object sender, RoutedEventArgs e)
        {
            if (AddEditSegment(null))
            {
                //RecordingFormImageScroller.CallListofImageLists.Add(new BulkObservableCollection<StoredImage>());
            }
            while (LabelledSegmentsListView.Items.Count > RecordingFormImageScroller.ListofCallImageLists.Count)
            {
                RecordingFormImageScroller.ListofCallImageLists.Add(new BulkObservableCollection<StoredImage>());
            }
            //isSegmentModified = true;
        }

        /// <summary>
        ///     Handles the Click event of the BrowseForFileButton control.
        /// </summary>
        /// <param name="sender">
        ///     The source of the event.
        /// </param>
        /// <param name="e">
        ///     The <see cref="RoutedEventArgs"/> instance containing the event data.
        /// </param>
        private void BrowseForFileButton_Click(object sender, RoutedEventArgs e)
        {
            FileBrowser fileBrowser = new FileBrowser();
            fileBrowser.SelectWavFile();
            if (fileBrowser.TextFileNames != null && fileBrowser.TextFileNames.Count > 0)
            {
                string filename = fileBrowser.GetUnqualifiedFilename(0);
                RecordingNameTextBox.Text = filename;
            }

            recording.RecordingName = RecordingNameTextBox.Text;
        }

        /// <summary>
        ///     Handles the Click event of the ButtonSaveSegment control.
        /// </summary>
        /// <param name="sender">
        ///     The source of the event.
        /// </param>
        /// <param name="e">
        ///     The <see cref="RoutedEventArgs"/> instance containing the event data.
        /// </param>
        private void ButtonSaveSegment_Click(object sender, RoutedEventArgs e)
        {
            //TODO update segment with imagelist from image scroller control
            Button thisButton = sender as Button;
            String text = ((thisButton.Parent as WrapPanel).Children[1] as TextBox).Text;
            // pattern for regex to remove (n images) from the comment
            String pattern = @"\(\s*[0-9]*\s*images?\s*\)";
            text = Regex.Replace(text, pattern, "");

            LabelledSegment segment = ((thisButton.Parent as WrapPanel).TemplatedParent as ContentPresenter).Content as LabelledSegment;

            LabelledSegmentsListView.SelectedItem = segment;
            int index = LabelledSegmentsListView.SelectedIndex;

            TimeSpan start;
            TimeSpan end;
            string comment;

            // parse the label string into times and comment
            if (FileProcessor.IsLabelFileLine(text, out start, out end, out comment))
            {
                segment.StartOffset = start;
                segment.EndOffset = end;
                segment.Comment = comment;
                if (segment.Id > 0)
                {
                    SegmentAndBatList segAndBatList = SegmentAndBatList.Create(segment);
                    DBAccess.UpdateLabelledSegment(segAndBatList, recording.Id, RecordingFormImageScroller.CallImageList, null);
                    //DBAccess.UpdateLabelledSegment(segment);
                }

                thisButton.Visibility = Visibility.Hidden; // hide the button again now it has successfully saved
            }

            //update the segment in the recording's segment list with the new data
            if (index >= 0 && index < recording.LabelledSegments.Count)
            {
                recording.LabelledSegments[index].StartOffset = segment.StartOffset;
                recording.LabelledSegments[index].EndOffset = segment.EndOffset;
                recording.LabelledSegments[index].Comment = segment.Comment;

                // re=create the list of labelled segments from the modified recording
                //LabelledSegmentsList.Clear();
                //LabelledSegmentsList.AddRange(recording.LabelledSegments);

                //LabelledSegmentsListView.ItemsSource = LabelledSegmentsList;
            }
            //isSegmentModified = true;
        }

        /// <summary>
        ///     Handles the Click event of the CancelButton control.
        /// </summary>
        /// <param name="sender">
        ///     The source of the event.
        /// </param>
        /// <param name="e">
        ///     The <see cref="RoutedEventArgs"/> instance containing the event data.
        /// </param>
        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            this.Close();
        }

        /// <summary>
        ///     Handles the 1 event of the DeleteSegmentButton_Click control.
        /// </summary>
        /// <param name="sender">
        ///     The source of the event.
        /// </param>
        /// <param name="e">
        ///     The <see cref="RoutedEventArgs"/> instance containing the event data.
        /// </param>
        private void DeleteSegmentButton_Click_1(object sender, RoutedEventArgs e)
        {
            LabelledSegment segmentToDelete = null;
            if (LabelledSegmentsListView == null) return;

            if (LabelledSegmentsList == null) return;
            if (LabelledSegmentsList.Count <= 0) return;
            try
            {
                int indexToDelete = LabelledSegmentsListView.SelectedIndex;

                segmentToDelete = LabelledSegmentsList[indexToDelete];
                if (segmentToDelete == null) return;
                LabelledSegmentsList.Remove(segmentToDelete);
                if (ModifiedFlag != null && ModifiedFlag.Count > indexToDelete)
                {
                    ModifiedFlag.RemoveAt(indexToDelete);
                }
            }
            catch (Exception ex)
            {
                Tools.ErrorLog(ex.Message);
                return;
            }
            int index = LabelledSegmentsListView.SelectedIndex;
            /*var result = MessageBox.Show("Are you sure you want to permanently delete this Segment?", "Deleting \"" + segmentToDelete.Comment + "\"", MessageBoxButton.YesNo);
			if (result == MessageBoxResult.Yes)
			{
				DBAccess.DeleteSegment(segmentToDelete);
			}*/

            recording.LabelledSegments.Remove(segmentToDelete);
            if (index >= 0 && index < RecordingFormImageScroller.ListofCallImageLists.Count)
            {
                RecordingFormImageScroller.ListofCallImageLists.RemoveAt(index);
            }
            var processedSegments = recording.LabelledSegments;
            //recording = DBAccess.GetRecording(recording.Id);
            //recording.LabelledSegments = processedSegments;
            //LabelledSegmentsListView.ItemsSource = recording.LabelledSegments;
            if (index >= recording.LabelledSegments.Count)
            {
                index = recording.LabelledSegments.Count - 1;
            }
            LabelledSegmentsListView.SelectedIndex = index;
            //isSegmentModified = true;
            DBAccess.DeleteSegment(segmentToDelete);
        }

        private void LabelledSegmentsListView_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            using (new WaitCursor("Selected new Segment"))
            {
                Debug.WriteLine("LabelledSegmentsListView_SelectionChanged:- index=" + LabelledSegmentsListView.SelectedIndex);
                //if (e.AddedItems != null && e.AddedItems.Count > 0)
                //{
                //    RecordingFormImageScroller.IsReadOnly = false;
                //    RecordingFormImageScroller.CanAdd = true;
                //}
                //else
                //{
                //    RecordingFormImageScroller.IsReadOnly = true;

                //}
                RecordingFormImageScroller.IsReadOnly = false;
                RecordingFormImageScroller.CanAdd = true;
                RecordingFormImageScroller.SelectedCallIndex = LabelledSegmentsListView.SelectedIndex;
            }
        }

        private void MapButton_Click(object sender, RoutedEventArgs e)
        {
            MapWindow map = new MapWindow(true);
            Location location = Tools.ValidCoordinates(GPSLatitudeTextBox.Text, GPSLongitudeTextBox.Text);
            if (location != null)
            {
                map.Coordinates = location;
            }
            if (map.ShowDialog() ?? false)
            {
                if (map.DialogResult ?? false)
                {
                    location = Tools.ValidCoordinates(map.lastSelectedLocation);
                    if (location != null)
                    {
                        GPSLatitudeTextBox.Text = location.Latitude.ToString();
                        GPSLongitudeTextBox.Text = location.Longitude.ToString();
                    }
                }
            }
        }

        /// <summary>
        ///     Handles the Click event of the OKButton control.
        /// </summary>
        /// <param name="sender">
        ///     The source of the event.
        /// </param>
        /// <param name="e">
        ///     The <see cref="RoutedEventArgs"/> instance containing the event data.
        /// </param>
        private void OKButton_Click(object sender, RoutedEventArgs e)
        {
            string err = "";
            BulkObservableCollection<SegmentAndBatList> processedSegments = new BulkObservableCollection<SegmentAndBatList>();
            using (new WaitCursor("Saving recording data..."))
            {
                DBAccess.ResolveOrphanImages();
                if (LabelledSegmentsList != null && LabelledSegmentsList.Count > 0)
                {
                    processedSegments.Clear();
                    foreach (var seg in LabelledSegmentsList)
                    {
                        LabelledSegment segment = seg as LabelledSegment;

                        BulkObservableCollection<Bat> bats = DBAccess.GetDescribedBats(segment.Comment);

                        String segmentLine = Tools.FormattedSegmentLine(segment);
                        SegmentAndBatList thisProcessedSegment = SegmentAndBatList.ProcessLabelledSegment(segmentLine, bats);
                        thisProcessedSegment.segment = segment;
                        processedSegments.Add(thisProcessedSegment);
                    }
                }
                /*if (isSegmentModified)
                {
                    DBAccess.DeleteAllSegmentsForRecording(recording.Id);
                    recording.LabelledSegments.Clear();
                    foreach(var bsl in processedSegments)
                    {
                        bsl.segment.Id = 0;
                        bsl.segment.RecordingID = 0;
                    }
                }*/
            }

            err = DBAccess.UpdateRecording(recording, processedSegments, RecordingFormImageScroller.ListofCallImageLists);

            if (String.IsNullOrWhiteSpace(err))
            {
                DialogResult = true;
                this.Close();
            }
            else
            {
                MessageBox.Show(err);
            }
        }

        /// <summary>
        ///     Called when [ListView item focused].
        /// </summary>
        /// <param name="sender">
        ///     The sender.
        /// </param>
        /// <param name="e">
        ///     The <see cref="RoutedEventArgs"/> instance containing the event data.
        /// </param>
        private void OnListViewItemFocused(object sender, RoutedEventArgs e)
        {
            ListViewItem lvi = sender as ListViewItem;
            lvi.IsSelected = true;
            var content = lvi.Content as LabelledSegment;
            Debug.WriteLine("Selected item:- <" + content.Comment + ">");

            //LabelledSegmentsListView.SelectedItem = content;
            foreach (var item in LabelledSegmentsListView.Items)
            {
                if ((item as LabelledSegment).Id == content.Id)
                {
                    LabelledSegmentsListView.SelectedItem = item;
                    break;
                }
            }

            RecordingFormImageScroller.SelectedCallIndex = LabelledSegmentsListView.SelectedIndex;
        }

        /// <summary>
        ///     Called when [text box focused].
        /// </summary>
        /// <param name="sender">
        ///     The sender.
        /// </param>
        /// <param name="e">
        ///     The <see cref="RoutedEventArgs"/> instance containing the event data.
        /// </param>
        private void OnTextBoxFocused(object sender, RoutedEventArgs e)
        {
            TextBox segmentTextBox = sender as TextBox;
            Button mySaveButton = ((segmentTextBox.Parent as WrapPanel).Children[0] as Button);
            mySaveButton.Visibility = Visibility.Visible;
        }

        /// <summary>
        /// Event handler fired when the underlying imagescroller has an image deleted.  The image
        /// is deleted from the database here by removing the link to the selected segment and
        /// the image itself if there are no remaining links to it.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void RecordingFormImageScroller_ImageDeleted(object sender, EventArgs e)
        {
            ImageDeletedEventArgs args = e as ImageDeletedEventArgs;
            int deletedImageID = args.imageID;
            if (deletedImageID < 0 || LabelledSegmentsListView == null || LabelledSegmentsListView.Items.Count <= 0 || LabelledSegmentsListView.SelectedItem == null)
            {
                return;
            }

            int SelectedSegmentID = LabelledSegmentsList[LabelledSegmentsListView.SelectedIndex].Id;
            if (SelectedSegmentID < 0)
            {
                return;
            }

            DBAccess.DeleteImageForSegment(deletedImageID, SelectedSegmentID);

            this.UpdateLayout();
        }

        //private bool isSegmentModified = false;
        /// <summary>
        ///     Handles the event when new image/s have been added to the image scroller although no
        ///     Adds the images to the database as unlinked images - if the caption starts with the recording
        ///     name (as the default caption) then clicking OK on the form will do an image Update
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e">     </param>
        private void RecordingFormImageScroller_UnassociatedImage(object sender, EventArgs e)
        {
            if (LabelledSegmentsListView.SelectedIndex < 0)
            {
                // we do indeed have no selected segment and therefore need to do something about it
                var newImageList = RecordingFormImageScroller.GetCurrentImageList(); // get the set of images we need to deal with
                if (newImageList != null)
                {
                    foreach (var image in newImageList)
                    {
                        DBAccess.InsertImage(image);
                    }
                }
            }
        }
    }
}