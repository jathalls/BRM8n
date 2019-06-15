using DataVirtualizationLibrary;
using Microsoft.VisualStudio.Language.Intellisense;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace BatRecordingManager
{
    /// <summary>
    ///     Interaction logic for RecordingSessionListDetailControl.xaml
    /// </summary>
    public partial class RecordingSessionListDetailControl : UserControl
    {
        #region recordingSessionList

        /// <summary>
        ///     Gets or sets the recordingSessionList property. This dependency property indicates ....
        /// </summary>
        #region recordingSessionDataList

        /// <summary>
        /// recordingSessiondataList Dependency Property
        /// </summary>
        public static readonly DependencyProperty recordingSessionDataListProperty =
            DependencyProperty.Register("recordingSessionDataList", typeof(AsyncVirtualizingCollection<RecordingSessionData>), typeof(RecordingSessionListDetailControl),
                new FrameworkPropertyMetadata(null));

        /// <summary>
        /// Gets or sets the recordingSessiondataList property.  This dependency property 
        /// indicates ....
        /// </summary>
        public AsyncVirtualizingCollection<RecordingSessionData> recordingSessionDataList
        {
            get { return (AsyncVirtualizingCollection<RecordingSessionData>)GetValue(recordingSessionDataListProperty); }
            set { SetValue(recordingSessionDataListProperty, value); }
        }

        #endregion




       // public AsyncVirtualizingCollection<RecordingSessionData> recordingSessionDataList = new AsyncVirtualizingCollection<RecordingSessionData>(new RecordingSessionDataProvider(), 50, 100);
        #endregion recordingSessionList

        #region IsLoading

        /// <summary>
        /// IsLoading Dependency Property
        /// </summary>
        public static readonly DependencyProperty IsLoadingProperty =
            DependencyProperty.Register("IsLoading", typeof(bool), typeof(RecordingSessionListDetailControl),
                new FrameworkPropertyMetadata((bool)false));

        /// <summary>
        /// Gets or sets the IsLoading property.  This dependency property 
        /// indicates ....
        /// </summary>
        public bool IsLoading
        {
            get {
                if (recordingSessionDataList is AsyncVirtualizingCollection<RecordingSessionData>)
                {
                    return recordingSessionDataList.IsLoading;
                }
                else
                {
                    return (false);
                }
            }
            set {
                if (recordingSessionDataList is AsyncVirtualizingCollection<RecordingSessionData>)
                {
                    recordingSessionDataList.IsLoading = value;
                }
            }
        }

        #endregion



        /// <summary>
        /// The index of the first session on the current page or the page to be loaded
        /// </summary>
        public int currentTopOfScreen = 0;

        /// <summary>
        /// A string representing the field to be used for ordering the page entries before loading
        /// The Datagrid will sort within the page, this controls the items loaded into the page
        /// The value is taken from the navigation combobox.
        /// if NONE then the items are loaded in native databse order, i.e. the order in which they were
        /// added tothe database.
        /// The final character except for NONE is an arrow indicating if ascending or descending order
        /// i.e. ^ or v
        /// </summary>
        public string field = "NONE";

        public int MaxRecordingSessions = 0;

        /// <summary>
        /// The number of sessions to load ina page view
        /// </summary>
        public int pageSize = 100;

        /// <summary>
        ///     Initializes a new instance of the <see cref="RecordingSessionListDetailControl"/> class.
        /// </summary>
        public RecordingSessionListDetailControl()
        {
            //displayedRecordings = new BulkObservableCollection<Recording>();

            InitializeComponent();
            this.DataContext = this;

            SegmentImageScroller.AddImageButton.IsEnabled = false;
            RecordingSessionListView.Initialized += RecordingSessionListView_Initialized;

            RecordingsListControl.RecordingChanged += RecordingsListControl_RecordingChanged;
            RecordingsListControl.SegmentSelectionChanged += RecordingsListControl_SegmentSelectionChanged;
            SegmentImageScroller.IsReadOnly = true;
            SegmentImageScroller.AddImageButton.IsEnabled = false;
            SegmentImageScroller.Title = "Segment Images";
            
            MaxRecordingSessions = DBAccess.getRecordingSessionCount();

            //RefreshData(pageSize,currentTopOfScreen);
            //RecordingsListView.ItemsSource = displayedRecordingControls;
        }

        /// <summary>
        /// Returns the currently selected recording if any or a null
        /// </summary>
        /// <returns></returns>
        internal Recording GetSelectedRecording()
        {
            Recording result = null;

            if (RecordingsListControl != null)
            {
                if (RecordingsListControl.RecordingsListView != null)
                {
                    if (RecordingsListControl.RecordingsListView.SelectedItems != null && RecordingsListControl.RecordingsListView.SelectedItems.Count > 0)
                    {
                        result = RecordingsListControl.RecordingsListView.SelectedItems[0] as Recording;
                    }
                }
            }

            return (result);
        }

        /// <summary>
        /// if a session has been selected it is returned from this function, otherwise a
        /// null is returned
        /// </summary>
        /// <returns></returns>
        internal RecordingSession GetSelectedSession()
        {
            RecordingSession result = null;
            if (RecordingSessionListView != null)
            {
                if (RecordingSessionListView.SelectedItems != null && RecordingSessionListView.SelectedItems.Count > 0)
                {
                    RecordingSessionData sessionData= RecordingSessionListView.SelectedItems[0] as RecordingSessionData;
                    if (sessionData != null)
                    {
                        result = DBAccess.GetRecordingSession(sessionData.Id);
                    }
                    else
                    {
                        result = null;
                    }
                }
            }
            return (result);
        }

        internal void RefreshData()
        {
            this.Dispatcher.BeginInvoke(System.Windows.Threading.DispatcherPriority.Background,
                            //Application.Current.Dispatcher.BeginInvoke(System.Windows.Threading.DispatcherPriority.Background,
                            new Action(() =>
                            {
                            this.RefreshData(pageSize, currentTopOfScreen);
                            }));
           
            
        }

        /// <summary>
        ///     Refreshes the data in the display when this pane is made visible; It might slow down
        ///     context switches, but is necessary if other panes have changed the data. A more
        ///     sophisticated approach would be to have any display set a 'modified' flag which
        ///     would trigger the update or not as necessary;
        /// </summary>
        /// <exception cref="System.NotImplementedException">
        ///     </exception>
        internal void RefreshData(int pageSize, int topOfScreen)
        {
            
            if (RecordingSessionListView == null) return;
            using (new WaitCursor("Refresh screen data"))
            {
                //  Stopwatch overallWatch = Stopwatch.StartNew();
                int old_selection = -1;
                old_selection = RecordingSessionListView.SelectedIndex;
                //recordingSessionDataList.Clear();
                //recordingSessionDataList.AddRange(DBAccess.GetPagedRecordingSessionDataList(pageSize, topOfScreen, field));
                recordingSessionDataList = null;
                    recordingSessionDataList = new AsyncVirtualizingCollection<RecordingSessionData>(new RecordingSessionDataProvider(), 50, 100);
                if (!recordingSessionDataList.IsLoading)
                {
                    recordingSessionDataList.Refresh();
                }
                if (old_selection >= 0 && old_selection < recordingSessionDataList.Count)
                {
                    RecordingSessionListView.SelectedIndex = old_selection;
                }

                

                SegmentImageScroller.Clear();
                if (RecordingSessionListView.SelectedItem == null)
                {
                    recordingSessionControl.recordingSession = null;
                    RecordingsListControl.recordingsList.Clear();
                }
                else
                {
                    int selectedID = (RecordingSessionListView.SelectedItem as RecordingSessionData).Id;
                    if ((recordingSessionControl.recordingSession??new RecordingSession()).Id != selectedID)
                    {
                        recordingSessionControl.recordingSession = DBAccess.GetRecordingSession(selectedID);
                        RecordingsListControl.recordingsList.Clear();
                        RecordingsListControl.recordingsList.AddRange(recordingSessionControl.recordingSession.Recordings);
                    }
                }
                
            }

            //CollectionViewSource.GetDefaultView(RecordingSessionListView.ItemsSource).Refresh();
        }

        /// <summary>
        ///     Selects the specified recording session.
        /// </summary>
        /// <param name="recordingSessionId">
        ///     The recording session.
        /// </param>
        internal void Select(int recordingSessionId)
        {
            for (int i = 0; i < RecordingSessionListView.Items.Count; i++)
            {
                RecordingSessionData sessionData = RecordingSessionListView.Items[i] as RecordingSessionData;
                if (sessionData.Id == recordingSessionId)
                {
                    RecordingSessionListView.SelectedIndex = i;
                    break;
                }
            }
        }

        /// <summary>
        /// selects and brings into view the session indicated by the sessionTag
        /// </summary>
        /// <param name="sessionUpdated"></param>
        internal void SelectSession(string sessionUpdated)
        {
            if (RecordingSessionListView.Items != null)
            {
                foreach (var item in RecordingSessionListView.Items)
                {
                    var rs = item as RecordingSessionData;
                    if (rs!=null && rs.SessionTag == sessionUpdated)
                    {
                        RecordingSessionListView.SelectedItem = rs;
                        RecordingSessionListView.ScrollIntoView(item);
                        //rs.BringIntoView();
                    }
                }
            }
        }

        private void AddEditRecordingSession(RecordingSessionForm recordingSessionForm)
        {
            Mouse.OverrideCursor = Cursors.Wait;
            int selectedIndex = RecordingSessionListView.SelectedIndex;

            string error = "No Data Entered";
            Mouse.OverrideCursor = null;
            if (!recordingSessionForm.ShowDialog() ?? false)
            {
                if (recordingSessionForm.DialogResult ?? false)
                {
                    if (!string.IsNullOrWhiteSpace(error))
                    {
                        MessageBox.Show(error);
                    }
                }
            }

            RefreshData(pageSize, currentTopOfScreen);
            /*this.recordingSessionList = DBAccess.GetRecordingSessionList();
if (selectedIndex >= 0 && selectedIndex <= this.RecordingSessionListView.Items.Count)
{
    RecordingSessionListView.SelectedIndex = selectedIndex;
}
Mouse.OverrideCursor = null;*/
        }

        private void AddRecordingSessionButton_Click(object sender, RoutedEventArgs e)
        {
            Mouse.OverrideCursor = Cursors.Wait;
            RecordingSessionForm recordingSessionForm = new RecordingSessionForm();

            recordingSessionForm.Clear();
            RecordingSession newSession = new RecordingSession();
            newSession.LocationGPSLatitude = null;
            newSession.LocationGPSLongitude = null;
            newSession.SessionDate = DateTime.Today;
            newSession.EndDate = newSession.SessionDate;
            newSession.SessionStartTime = new TimeSpan(18, 0, 0);
            newSession.SessionEndTime = new TimeSpan(24, 0, 0);
            recordingSessionForm.SetRecordingSession(newSession);
            Mouse.OverrideCursor = null;
            AddEditRecordingSession(recordingSessionForm);
        }

        private void CompareImagesButton_Click(object sender, RoutedEventArgs e)
        {
            using (new WaitCursor("Add all images to the comparison window..."))
            {
                
                RecordingSessionData sessionData = RecordingSessionListView.SelectedItem as RecordingSessionData;

                //BulkObservableCollection<StoredImage> images = DBAccess.GetAllImagesForSession(session);
                if (sessionData != null)
                {
                    RecordingSession session = DBAccess.GetRecordingSession(sessionData.Id);
                    if (session != null)
                    {
                        BulkObservableCollection<StoredImage> images = session.GetImageList();
                        if(images==null || images.Count <= 0)
                        {
                            ComparisonHost.Instance.AddImage(new StoredImage(null, "", "", -1));
                            ComparisonHost.Instance.Close();

                        }
                        ComparisonHost.Instance.AddImageRange(images);
                    }
                }
            }
        }

        private void DeleteRecordingSessionButton_Click(object sender, RoutedEventArgs e)
        {
            using (new WaitCursor("Deleting Recording Session"))
            {
                if (RecordingSessionListView.SelectedItem != null && RecordingSessionListView.SelectedItems.Count > 0)
                {
                    int oldIndex = RecordingSessionListView.SelectedIndex;
                    RecordingSession session = DBAccess.GetRecordingSession((RecordingSessionListView.SelectedItems[0] as RecordingSessionData).Id);
                    //recordingSessionDataList.RemoveAt(oldIndex);
                    if (RecordingSessionListView.Items.Count > 0)
                    {
                        oldIndex--;
                        if (oldIndex < 0) oldIndex = 0;
                        RecordingSessionListView.SelectedIndex = oldIndex;
                    }
                    else
                    {
                        recordingSessionControl.recordingSession = null;
                        RecordingsListControl.recordingsList.Clear();
                    }

                    DBAccess.DeleteSession(session);
                    RefreshData(pageSize, currentTopOfScreen);
                    if (RecordingSessionListView.SelectedItem != null)
                    {
                        RecordingSessionListView.ScrollIntoView(RecordingSessionListView.SelectedItem);
                    }

                }
            }
           
            
        }

        private void EditRecordingSessionButton_Click(object sender, RoutedEventArgs e)
        {
            using (new WaitCursor("Set Recording session form data"))
            {
                RecordingSessionForm form = new RecordingSessionForm();
                if (RecordingSessionListView.SelectedItems != null && RecordingSessionListView.SelectedItems.Count > 0)
                {
                    RecordingSessionData sessionData= RecordingSessionListView.SelectedItems[0] as RecordingSessionData;
                    form.recordingSessionControl.recordingSession = DBAccess.GetRecordingSession(sessionData.Id);
                }
                else
                {
                    form.Clear();
                }
                AddEditRecordingSession(form);
            }
        }

        /// <summary>
        ///     Handles the Click event of the ExportSessionDataButton control.
        /// </summary>
        /// <param name="sender">
        ///     The source of the event.
        /// </param>
        /// <param name="e">
        ///     The <see cref="RoutedEventArgs"/> instance containing the event data.
        /// </param>
        private void ExportSessionDataButton_Click(object sender, RoutedEventArgs e)
        {
            using (new WaitCursor("Export session data"))
            {
                if (RecordingSessionListView.SelectedItems.Count > 0)
                {
                    foreach (var item in RecordingSessionListView.SelectedItems)
                    {
                        RecordingSessionData sessionData = item as RecordingSessionData;
                        RecordingSession session = DBAccess.GetRecordingSession(sessionData!=null?sessionData.Id:-1);
                        if (session != null)
                        {

                            BulkObservableCollection<BatStats> statsForSession = session.GetStats();
                            statsForSession = Tools.CondenseStatsList(statsForSession);
                            String folder = @"C:\ExportedBatData\";

                            if (!Directory.Exists(folder))
                            {
                                try
                                {
                                    var info = Directory.CreateDirectory(folder);
                                    if (!info.Exists)
                                    {
                                        folder = @"C:\ExportedBatData\";
                                        Directory.CreateDirectory(folder);
                                    }
                                }
                                catch (Exception ex)
                                {
                                    Tools.ErrorLog(ex.Message);
                                    folder = @"C:\ExportedBatData\";
                                    Directory.CreateDirectory(folder);
                                }
                                if (!Directory.Exists(folder))
                                {
                                    MessageBox.Show("Unable to create folder for export files: " + folder);
                                    Mouse.OverrideCursor = null;
                                    return;
                                }
                            }
                            string file = session.SessionTag.Trim() + ".csv";
                            if (File.Exists(folder + file))
                            {
                                if (File.Exists(folder + session.SessionTag.Trim() + ".bak"))
                                {
                                    File.Delete(folder + session.SessionTag.Trim() + ".bak");
                                }
                                File.Move(folder + file, folder + session.SessionTag.Trim() + ".bak");
                            }

                            StreamWriter sw = File.AppendText(folder + file);
                            sw.Write("Date,Place,Gridref,Comment,Observer,Species,Abundance=Passes,Additional Info" + Environment.NewLine);
                            foreach (var stat in statsForSession)
                            {
                                String line = session.SessionDate.ToShortDateString();
                                line += "," + session.Location;
                                line += ",\"" + session.LocationGPSLatitude + "," + session.LocationGPSLongitude + "\"";
                                line += "," + (session.SessionStartTime != null ? (session.SessionStartTime.Value.ToString() + " - " + (session.SessionEndTime != null ? session.SessionEndTime.Value.ToString() : "")) : "") + "; "
                                    + "\"" + session.Equipment + "; " + session.Microphone + "\"";
                                line += "," + "\"" + session.Operator + "\"";
                                line += "," + DBAccess.GetBatLatinName(stat.batCommonName);
                                line += "," + stat.passes;
                                line += "," + "\"" + session.SessionNotes.Replace("\n", "\t") + "\"" + Environment.NewLine;
                                sw.Write(line);
                            }
                            sw.Close();
                        }
                    }
                }
            }
        }
        /*
        private void NavOrderByComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            using (new WaitCursor("Set default pre-load order"))
            {
                if (!e.Handled)
                {
                    e.Handled = true;
                    if (e.AddedItems != null && e.AddedItems.Count > 0)
                    {
                        //string selectedItem = (NavOrderByComboBox.SelectedItem as ComboBoxItem).Content.ToString();
                        string selectedItem = "NONE";
                        if (!string.IsNullOrWhiteSpace(selectedItem))
                        {
                            field = selectedItem;
                            RefreshData();
                            if (field.StartsWith("DATE"))
                            {
                                Tools.SortColumn(RecordingSessionListView, DateColumn.DisplayIndex);
                            }
                            else if (field.StartsWith("TAG"))
                            {
                                Tools.SortColumn(RecordingSessionListView, TagColumn.DisplayIndex);
                            }
                            else if (field.StartsWith("LOCATION"))
                            {
                                Tools.SortColumn(RecordingSessionListView, LocationColumn.DisplayIndex);
                            }
                            else if (field.StartsWith("RECORDINGS"))
                            {
                                Tools.SortColumn(RecordingSessionListView, RecordingsColumn.DisplayIndex);
                            }
                        }
                    }
                }
            }
        }*/

    /*
        /// <summary>
        /// Move to the end of the list of sessions in the database
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void NavToLastPage_Click(object sender, RoutedEventArgs e)
        {
            using (new WaitCursor("Move to Last Page"))
            {
                if (!e.Handled)
                {
                    e.Handled = true;
                    if (pageSize <= 0) return;
                    currentTopOfScreen = MaxRecordingSessions - pageSize;
                    if (currentTopOfScreen < 0)
                    {
                        currentTopOfScreen = 0;
                    }
                    RefreshData();
                }
            }
        }*/

        /*

        /// <summary>
        /// moves forward by 2/3 of a page in the database
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void NavToNextPage_Click(object sender, RoutedEventArgs e)
        {
            using (new WaitCursor("Move to Next Page"))
            {
                if (!e.Handled)
                {
                    e.Handled = true;
                    if (pageSize <= 0) return;
                    currentTopOfScreen += (int)(pageSize * 0.667);
                    if (currentTopOfScreen > (MaxRecordingSessions - pageSize))
                    {
                        currentTopOfScreen = MaxRecordingSessions - pageSize;
                    }
                    if (currentTopOfScreen < 0)
                    {
                        currentTopOfScreen = 0;
                    }
                    RefreshData();
                }
            }
        }*/

        /*

        /// <summary>
        /// Moves up 2/3 of a  page in the list of sessions from the database
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void NavToPrevPage_Click(object sender, RoutedEventArgs e)
        {
            using (new WaitCursor("Load previous page"))
            {
                if (!e.Handled)
                {
                    e.Handled = true;
                    if (pageSize <= 0) return;
                    currentTopOfScreen -= (int)(pageSize * 0.667);
                    if (currentTopOfScreen < 0)
                    {
                        currentTopOfScreen = 0;
                    }
                    RefreshData();
                }
            }
        }*/

        /*

        /// <summary>
        /// Moves to the start of the list of items in the database
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void NavToStartButton_Click(object sender, RoutedEventArgs e)
        {
            using (new WaitCursor("Move to First page"))
            {
                if (!e.Handled)
                {
                    e.Handled = true;
                    if (pageSize <= 0) return;
                    currentTopOfScreen = 0;
                    RefreshData();
                }
            }
        }*/

        private void OnListViewItemFocused(object sender, RoutedEventArgs e)
        {
            //ListViewItem lvi = sender as D
            //lvi.IsSelected = true;
            //lvi.BringIntoView();
        }
    /*
        /// <summary>
        /// Refreshes the page with the new pagesize settings
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void PageSizeComboBox_DropDownClosed(object sender, EventArgs e)
        {
        }*/

        /*

        /// <summary>
        /// Adjust the page size - re-population will be done on combobox closed
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void PageSizeComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            using (new WaitCursor("Changing page size"))
            {
                if (!e.Handled)
                {
                    e.Handled = true;
                    if (e.AddedItems.Count > 0)
                    {
                        String selectedItem = (PageSizeComboBox.SelectedItem as ComboBoxItem).Content.ToString();
                        if (!String.IsNullOrWhiteSpace(selectedItem))
                        {
                            if (selectedItem.ToUpper() == "ALL")
                            {
                                currentTopOfScreen = 0;
                                pageSize = 0;
                            }
                            else
                            {
                                int size = 0;
                                int.TryParse(selectedItem, out size);
                                if (size > 0)
                                {
                                    pageSize = size;
                                }
                            }
                            RefreshData(pageSize, currentTopOfScreen);
                        }
                    }
                }
            }
        }*/

        /// <summary>
        /// called when the control is initialized and the data can be refreshed for the first time
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void RecordingSessionListView_Initialized(object sender, EventArgs e)
        {
            if (recordingSessionDataList==null || (!recordingSessionDataList.IsLoading && recordingSessionDataList.Count <= 0))
            {
                RefreshData();
            }
        }

        /// <summary>
        ///     Handles the SelectionChanged event of the RecordingSessionListView control.
        ///     Selection has changed in the list, so update the details panel with the newly
        ///     selected item.
        /// </summary>
        /// <param name="sender">
        ///     The source of the event.
        /// </param>
        /// <param name="e">
        ///     The <see cref="SelectionChangedEventArgs"/> instance containing the event data.
        /// </param>
        private void RecordingSessionListView_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            using (new WaitCursor("Change Recording Session Selection"))

            {
                if (RecordingSessionListView.SelectedItems == null || RecordingSessionListView.SelectedItems.Count <= 0) return;
                int id = (RecordingSessionListView.SelectedItems[0] as RecordingSessionData).Id;
                recordingSessionControl.recordingSession = DBAccess.GetRecordingSession(id);
                SegmentImageScroller.Clear();
                if (recordingSessionControl.recordingSession == null)
                {
                    SessionSummaryStackPanel.Children.Clear();

                    //displayedRecordings.Clear();
                    RecordingsListControl.selectedSession = recordingSessionControl.recordingSession;
                    ExportSessionDataButton.IsEnabled = false;
                    EditRecordingSessionButton.IsEnabled = false;
                    DeleteRecordingSessionButton.IsEnabled = false;
                    CompareImagesButton.IsEnabled = false;
                }
                else
                {
                    //BulkObservableCollection<BatStats> statsForSession = recordingSessionControl.recordingSession.GetStats();

                    //statsForSession = CondenseStatsList(statsForSession);
                    SessionSummaryStackPanel.Children.Clear();
                    //foreach (var batstat in statsForSession)
                    //{
                    //    BatPassSummaryControl batPassSummary = new BatPassSummaryControl();
                    //    batPassSummary.Content = Tools.GetFormattedBatStats(batstat, false);
                    //    SessionSummaryStackPanel.Children.Add(batPassSummary);
                    //}
                    List<string> SessionSummary = Tools.GetSessionSummary(recordingSessionControl.recordingSession);
                    foreach (var item in SessionSummary)
                    {
                        BatPassSummaryControl batPassSummary = new BatPassSummaryControl();
                        batPassSummary.Content = item;
                        SessionSummaryStackPanel.Children.Add(batPassSummary);
                    }

                    //displayedRecordings.Clear();
                    //displayedRecordings.AddRange(recordingSessionControl.recordingSession.Recordings);
                    RecordingsListControl.selectedSession = recordingSessionControl.recordingSession;
                    ExportSessionDataButton.IsEnabled = true;
                    EditRecordingSessionButton.IsEnabled = true;
                    DeleteRecordingSessionButton.IsEnabled = true;
                    CompareImagesButton.IsEnabled = true;
                }
            }
        }

        private void RecordingsListControl_RecordingChanged(object sender, EventArgs e)
        {
            RefreshData(pageSize, currentTopOfScreen);
        }

        /// <summary>
        /// Event handler triggered when the user selects a new labelled segment within the list
        /// The event args contain the list of images which are to be displayed in the ImageScroller
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void RecordingsListControl_SegmentSelectionChanged(object sender, EventArgs e)
        {
            ImageListEventArgs ile = e as ImageListEventArgs;

            Debug.WriteLine("RecordingsListControl_SegmentSelectionChanged:- " + ile.imageList.Count + " images");
            SegmentImageScroller.Clear();
            if (ile.imageList != null && ile.imageList.Count > 0)
            {
                foreach (var im in ile.imageList)
                {
                    SegmentImageScroller.AddImage(im);
                }
            }
        }

        /// <summary>
        /// Generates a report for the selected sessions or for all sessions if none are selected
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        public void ReportSessionDataButton_Click(object sender, RoutedEventArgs e)
        {
            
            bool doFullExport = false;
            if(sender is AnalyseAndImportClass)
            {
                doFullExport = true;
            }
            List<BatStatistics> ReportBatStatsList = new List<BatStatistics>();
            List<RecordingSession> ReportSessionList = new List<RecordingSession>();
            List<Recording> ReportRecordingList = new List<Recording>();
            ReportMainWindow reportWindow = new ReportMainWindow();
            BulkObservableCollection<BatStats> StatsForAllSessions = new BulkObservableCollection<BatStats>();

            using (new WaitCursor("Generate Report Data"))
            {
                if (RecordingSessionListView.SelectedItems != null && RecordingSessionListView.SelectedItems.Count > 0)
                {
                    foreach (var item in RecordingSessionListView.SelectedItems)
                    {
                        RecordingSessionData sessionData = item as RecordingSessionData;
                        if (sessionData == null) return;
                        RecordingSession session = DBAccess.GetRecordingSession(sessionData.Id);
                        if (session == null)
                        {
                            return;
                        }
                        StatsForAllSessions.AddRange(session.GetStats());

                        ReportSessionList.Add(session);
                    }
                }
                else
                {
                    if (RecordingSessionListView.Items != null && RecordingSessionListView.Items.Count > 0)
                    {
                        foreach (var item in RecordingSessionListView.Items)
                        {
                            RecordingSessionData sessionData = item as RecordingSessionData;
                            if (sessionData == null) return;
                            RecordingSession session = DBAccess.GetRecordingSession(sessionData.Id);
                            if (session == null)
                            {
                                return;
                            }
                            StatsForAllSessions.AddRange(session.GetStats());

                            ReportSessionList.Add(session);
                        }
                    }
                }
                StatsForAllSessions = Tools.CondenseStatsList(StatsForAllSessions);

                foreach (var bs in StatsForAllSessions)
                {
                    BatStatistics bstat = new BatStatistics(DBAccess.GetNamedBat(bs.batCommonName));
                    ReportBatStatsList.Add(bstat);
                    var recordingsToreport = (from brLink in bstat.bat.BatRecordingLinks
                                              join sess in ReportSessionList on brLink.Recording.RecordingSessionId equals sess.Id
                                              select brLink.Recording).Distinct();

                    if (recordingsToreport != null)
                    {
                        foreach (var rec in recordingsToreport)
                        {
                            if (!ReportRecordingList.Any(existingRec => existingRec.Id == rec.Id))
                            {
                                ReportRecordingList.Add(rec);
                            }
                        }
                    }
                    //ReportRecordingList.AddRange(recordingsToreport);
                }

                reportWindow.setReportData(ReportBatStatsList, ReportSessionList, ReportRecordingList);
                if (doFullExport)
                {
                    reportWindow.ExportAll();
                }
            }
            
            reportWindow.ShowDialog();
        }
    }
}