using DataVirtualizationLibrary;
using Microsoft.VisualStudio.Language.Intellisense;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;

namespace BatRecordingManager
{
    /// <summary>
    ///     Provides arguments for an event.
    /// </summary>
    [Serializable]
    public class SessionActionEventArgs : EventArgs
    {
        /// <summary>
        ///     The empty
        /// </summary>
        public new static readonly SessionActionEventArgs Empty = new SessionActionEventArgs("");

        #region Public Properties

        /// <summary>
        ///     The recording session
        /// </summary>
        public RecordingSession recordingSession { get; set; }

        public int RecordingSessionId
        {
            get
            {
                return (recordingSession.Id);
            }
        }

        #endregion Public Properties

        #region Constructors

        /// <summary>
        ///     Constructs a new instance of the <see cref="SessionActionEventArgs"/> class.
        /// </summary>
        public SessionActionEventArgs(RecordingSession session)
        {
            recordingSession = session;
        }

        /// <summary>
        /// Constructor using just the sessiontag to retrieve the full recording session
        /// </summary>
        /// <param name="SessionTag"></param>
        public SessionActionEventArgs(string SessionTag)
        {
            recordingSession = DBAccess.GetRecordingSession(SessionTag);
        }

        #endregion Constructors
    }

    /// <summary>
    ///     Interaction logic for SessionsAndRecordingsControl.xaml
    /// </summary>
    public partial class SessionsAndRecordingsControl : UserControl
    {
        /// <summary>
        ///     The selected bat identifier
        /// </summary>
        public int SelectedBatId = 0;

        private readonly object SessionActionEventLock = new object();
        private BatStatistics _selectedBatDetails;

        private EventHandler<SessionActionEventArgs> SessionActionEvent;

        public bool IsLoading
        {
            get
            {
                if (matchingRecordingData != null)
                {
                    return (matchingRecordingData.IsLoading);
                }
                else
                {
                    return (!this.IsLoaded);
                }
            }
        }

        /// <summary>
        ///     Initializes a new instance of the <see cref="SessionsAndRecordingsControl"/> class.
        /// </summary>
        public SessionsAndRecordingsControl()
        {
            RecordingImageScroller = Activator.CreateInstance<ImageScrollerControl>();
            InitializeComponent();
            this.DataContext = this;

            
            //SessionsDataGrid.ItemsSource = matchingSessions;
            

            SessionsDataGrid.EnableColumnVirtualization = true;
            SessionsDataGrid.EnableRowVirtualization = true;
            //RecordingsDataGrid.ItemsSource = matchingRecordings;
            //RecordingsDataGrid.EnableColumnVirtualization = true;
            //RecordingsDataGrid.EnableRowVirtualization = true;
            RecordingImageScroller.Title = "Recording Images";
            RecordingImageScroller.IsReadOnly = true;
            //BindingOperations.EnableCollectionSynchronization(matchingRecordingData, matchingRecordingData);
        }

        /// <summary>
        ///     Event raised after the session property value has changed.
        /// </summary>
        public event EventHandler<SessionActionEventArgs> SessionAction
        {
            add
            {
                lock (SessionActionEventLock)
                {
                    SessionActionEvent += value;
                }
            }
            remove
            {
                lock (SessionActionEventLock)
                {
                    SessionActionEvent -= value;
                }
            }
        }

        /// <summary>
        ///     Gets or sets the selected bat details.
        /// </summary>
        /// <value>
        ///     The selected bat details.
        /// </value>
        public BatStatistics SelectedBatDetails
        {
            get
            {
                return (_selectedBatDetails);
            }
            set
            {
                using (new WaitCursor("Changed selected bat"))
                {
                    _selectedBatDetails = value;
                    //matchingSessions.Clear();
                    matchingSessionData.Clear();
                    //matchingRecordings.Clear();
                    //matchingRecordingData.Clear();

                    //imageScroller.Clear();
                    if (value != null)
                    {
                        //matchingSessions.AddRange(value.sessions);
                        //foreach(var bsLink in value.bat.BatSessionLinks)
                        //{
                        //    matchingSessions.Add(new BatSession(bsLink.RecordingSession, value.bat));
                        //}
                        matchingSessionData.AddRange(DBAccess.GetBatSessionData(value.bat.Id));
                        SessionsDataGrid.SelectAll();

                        //matchingSessions.AddRange(value.sessions);
                        //SessionsDataGrid.Items.Refresh();
                        //NB returns image and segments count a stotals not those specific to the bat
                        /*
                        foreach (var brLink in value.bat.BatRecordingLinks)
                        {
                            bool recordingHasBat = false;
                            var data = new BatSessionRecordingData(brLink.Recording.RecordingSessionId, brLink.RecordingID, brLink.BatID,
                                brLink.Recording.RecordingName, brLink.Recording.RecordingDate, brLink.Recording.RecordingStartTime,
                                brLink.Recording.LabelledSegments.Count(),
                                brLink.Recording.GetImageCount(brLink.Bat, out recordingHasBat));
                            matchingRecordingData.Add(data);
                        }
                        //matchingRecordings.AddRange(value.recordings);

                        RecordingsDataGrid.Items.Refresh();*/
                    }
                }
            }
        }

        /// <summary>forces a select all for sessions data grid
        ///
        /// </summary>
        internal void SelectAll()
        {
            SessionsDataGrid.SelectAll();
        }

        /// <summary>
        /// A list of all the recordings in the selected sessions
        /// </summary>
        //public BulkObservableCollection<BatRecording> matchingRecordings { get; } = new BulkObservableCollection<BatRecording>();

        //public BulkObservableCollection<BatSessionRecordingData> matchingRecordingData { get; } = new BulkObservableCollection<BatSessionRecordingData>();
        public AsyncVirtualizingCollection<BatSessionRecordingData> matchingRecordingData { get; set; }
        /// <summary>
        /// list of tailored class of items containg displayable data for the sessions list
        /// </summary>
        public BulkObservableCollection<BatSessionData> matchingSessionData { get; } = new BulkObservableCollection<BatSessionData>();

        

        

        private BulkObservableCollection<BatStatistics> _SelectedBatDetailsList = new BulkObservableCollection<BatStatistics>();

        /// <summary>
        /// Accommodates multiple selections of BatStatistics to populate the sessions panel
        /// </summary>
        public BulkObservableCollection<BatStatistics> SelectedBatDetailsList
        {
            get { return (_SelectedBatDetailsList); }

            internal set
            {
                using (new WaitCursor("Changed select bat details"))
                {
                    _SelectedBatDetailsList = value;
                    
                }
            }
        }

        //public BatAndCallImageScrollerControl imageScroller { get; internal set; }

        /// <summary>
        ///     Raises the <see cref="SessionAction"/> event.
        /// </summary>
        /// <param name="e">
        ///     <see cref="SessionActionEventArgs"/> object that provides the arguments for the event.
        /// </param>
        protected virtual void OnSessionAction(SessionActionEventArgs e)
        {
            EventHandler<SessionActionEventArgs> handler = null;

            lock (SessionActionEventLock)
            {
                handler = SessionActionEvent;

                if (handler == null)
                    return;
            }

            handler(this, e);
        }

        internal void SetMatchingSessionsAndRecordings()
        {
            //matchingSessions.Clear();
            matchingSessionData.Clear();
            //matchingRecordings.Clear();

            foreach (var bs in SelectedBatDetailsList)
            {
                matchingSessionData.AddRange(DBAccess.GetBatSessionData(bs.bat.Id));
            }
            SessionsDataGrid.SelectAll();
            
        }

        /// <summary>
        /// Returns a list of the selected recordings or if no recordings are selected
        /// all of the displayed list of recordings.
        /// </summary>
        /// <returns></returns>
        internal List<Recording> GetSelectedRecordings()
        {
            List<Recording> result = new List<Recording>();
            if (RecordingsDataGrid.SelectedItems != null && RecordingsDataGrid.SelectedItems.Count > 0)
            {
                foreach (var item in RecordingsDataGrid.SelectedItems)
                {
                    result.Add(DBAccess.GetRecording((item as BatSessionRecordingData).RecordingId ?? 0));
                }
            }
            else
            {
                foreach (var item in RecordingsDataGrid.Items)
                {
                    result.Add(DBAccess.GetRecording((item as BatSessionRecordingData).RecordingId ?? 0));
                }
            }
            return (result);
        }

        /// <summary>
        /// Returns a lest of the selected sessions, or if no sessions are selected
        /// all of the displayed sessions.
        /// </summary>
        /// <returns></returns>
        internal List<RecordingSession> GetSelectedSessions()
        {
            List<RecordingSession> result = new List<RecordingSession>();
            if (SessionsDataGrid.SelectedItems != null && SessionsDataGrid.SelectedItems.Count > 0)
            {
                foreach (var item in SessionsDataGrid.SelectedItems)
                {
                    result.Add(DBAccess.GetRecordingSession((item as BatSessionData).id));
                }
            }
            else
            {
                foreach (var item in SessionsDataGrid.Items)
                {
                    result.Add(DBAccess.GetRecordingSession((item as BatSessionData).id));
                }
            }
            return (result);
        }

        private void RecordingsDataGrid_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            DataGrid dg = sender as DataGrid;
            var selectedItem = (dg.SelectedItem as BatSessionRecordingData);
            
            
            Tools.OpenWavFile(DBAccess.GetRecording(selectedItem.RecordingId??-1));
        }

        private void SessionsDataGrid_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            DataGrid dg = sender as DataGrid;
            BatSessionData selectedSession = dg.SelectedItem as BatSessionData;
            if (selectedSession == null) return;

            OnSessionAction(new SessionActionEventArgs(selectedSession.SessionTag));
        }

        private void SessionsDataGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            //BatSession selected;
            DataGrid dg = sender as DataGrid;
            Debug.WriteLine("Session selection changed:-");
            foreach(var item in e.AddedItems??new List<BatSessionData>())
            {
                Debug.Write("+"+(item as BatSessionData).id + ", ");
            }
            Debug.WriteLine("");
            foreach(var item in e.RemovedItems??new List<BatSessionData>())
            {
                Debug.Write("-" + (item as BatSessionData).id + ", ");
            }

            if ((e.AddedItems != null && e.AddedItems.Count > 0) || (e.RemovedItems != null && e.RemovedItems.Count > 0))
            {
                

                if (dg.SelectedItems == null || dg.SelectedItems.Count <= 0)
                {// if no selection, select all items
                    dg.SelectAll();
                }

                if (dg.SelectedItems != null && dg.SelectedItems.Count > 0)
                {
                    List<int> sessionIdList = new List<int>();
                    List<int> batIdList = new List<int>();
                    // only need to do something if there are some selected items

                    batIdList = SelectedBatDetailsList.Select(selbat => selbat.bat.Id).ToList();
                    int numRecordings = 0;
                    foreach (var item in dg.SelectedItems)
                    {

                        
                        var batSession = item as BatSessionData;
                        if (!sessionIdList.Contains(batSession.id)) sessionIdList.Add(batSession.id);
                        numRecordings += batSession.BatRecordingsCount;
                        
                       
                    }
                    matchingRecordingData = new AsyncVirtualizingCollection<BatSessionRecordingData>(new BatSessionRecordingDataProvider(batIdList, sessionIdList,numRecordings),25, 100);
                    matchingRecordingData.PropertyChanged += MatchingRecordingData_PropertyChanged;
                    RecordingsDataGrid.SetBinding(ItemsControl.ItemsSourceProperty, new Binding { Source = matchingRecordingData,IsAsync=true });
                    

                }
                
            }
        }

        private void MatchingRecordingData_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            Debug.WriteLine("recording data changed:-" + e.PropertyName);
            if (e.PropertyName == "IsLoading")
            {
                if (matchingRecordingData.IsLoading)
                {
                    Debug.WriteLine("Set wait cursor");
                    RecordingsDataGrid.Cursor = Cursors.Wait;
                }
                else
                {
                    Debug.WriteLine("Clear wait cursor");
                    RecordingsDataGrid.Cursor = Cursors.Arrow;
                }
            }
        }

        /// <summary>
        /// The selected recording has changed so find the newly selected recording and populate the
        /// image control with the images for this recording if any.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void RecordingsDataGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            RecordingImageScroller.Clear();
            if ((e.AddedItems != null && e.AddedItems.Count > 0) || (e.RemovedItems != null && e.RemovedItems.Count > 0))
            {
                if (RecordingsDataGrid.SelectedItems != null)
                {
                    foreach (var item in RecordingsDataGrid.SelectedItems)
                    {
                        if (item != null && item is BatSessionRecordingData)
                        {
                            BatSessionRecordingData brec = item as BatSessionRecordingData;
                            //Recording recording = DBAccess.GetRecording(brec.RecordingId??-1);
                            //var recordingImages = recording.GetImageList(brec.BatId);

                            var recordingImages = DBAccess.GetRecordingImagesForBat(brec.RecordingId, brec.BatId);

                            if (!recordingImages.IsNullOrEmpty())
                            {
                                foreach (var image in recordingImages)
                                {
                                    RecordingImageScroller.AddImage(image);
                                }
                                RecordingImageScroller.IsReadOnly = true;
                            }
                        }
                    }
                }
            }
            if (importPictureDialog != null && IsPictureDialogOpen)
            {
                if (RecordingsDataGrid.SelectedItem != null && (RecordingsDataGrid.SelectedItem as BatSessionRecordingData)!=null)
                {
                    importPictureDialog.setCaption((RecordingsDataGrid.SelectedItem as BatSessionRecordingData).RecordingName);
                }
            }
        }

        private ImportPictureDialog importPictureDialog = null;
        private bool IsPictureDialogOpen = false;

        /// <summary>
        /// Use a right mouse button click ending on a recording to bring up the Import Picture
        /// Dialog as a non-modal window witht he caption set to the name of the recording file.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void RecordingsDataGrid_MouseRightButtonUp(object sender, MouseButtonEventArgs e)
        {
            if (!IsPictureDialogOpen)
            {
                importPictureDialog = new ImportPictureDialog();
                importPictureDialog.Closed += ImportPictureDialog_Closed;
            }

            if (RecordingsDataGrid.SelectedItem != null && (RecordingsDataGrid.SelectedItem as BatSessionRecordingData)!=null)
            {
                string fileName = (RecordingsDataGrid.SelectedItem as BatSessionRecordingData).RecordingName;
                importPictureDialog.setCaption(fileName);
            }
            if (!IsPictureDialogOpen)
            {
                importPictureDialog.Show();
                IsPictureDialogOpen = true;
            }
        }

        private void ImportPictureDialog_Closed(object sender, EventArgs e)
        {
            IsPictureDialogOpen = false;
        }

    }

    /// <summary>
    /// special class to combine an instance of a recording with a specific bat so
    /// that the combined object can be the source fot a datagrid and elements of the
    /// recording that refer to the named bat can be displayed in the grid
    /// </summary>
    public class BatRecording
    {
        /// <summary>
        /// recording for this bat/recording pair
        /// </summary>
        public Recording recording { get; set; }

        /// <summary>
        /// bat for this bat/recording pair
        /// </summary>
        public Bat bat { get; set; }

        /// <summary>
        /// number of images in the recordings with this bat
        /// </summary>
        public int imageCount { get; }

        /// <summary>
        /// number of segments in this recording with this bat
        /// </summary>
        public int segmentCountForBat { get; }

        /// <summary>
        /// default constructor
        /// </summary>
        /// <param name="recording"></param>
        /// <param name="bat"></param>
        public BatRecording(Recording recording, Bat bat)
#pragma warning restore CS1591 // Missing XML comment for publicly visible type or member
        {
            try
            {
                this.recording = recording;
                this.bat = bat;
                if (recording != null && bat != null)
                {
                    imageCount = (from seg in recording.LabelledSegments
                                  from link in seg.BatSegmentLinks
                                  where link.BatID == bat.Id
                                  select seg.SegmentDatas.Count).Sum();

                    segmentCountForBat = (from seg in recording.LabelledSegments
                                          from lnk in seg.BatSegmentLinks
                                          where lnk.BatID == bat.Id
                                          select seg).Count();
                }
                //segmentCountForBat = recording.GetSegmentCount(bat);
            }
            catch (Exception ex)
            {
                Tools.ErrorLog(ex.Message);
                Debug.WriteLine(ex);
            }
        }
    }

    /// <summary>
    /// special class to combine an instance of a RecordingSession with a specific bat
    /// so that the the combination can be used as the ItemSource for a DataGrid and
    /// that datagrid can display only infomration relevant to the selected bat.
    /// </summary>
    public class BatSession
    {
        /// <summary>
        /// the session for this bat/session pair
        /// </summary>
        public RecordingSession session { get; set; }

        /// <summary>
        /// The bat for this bat/session pair
        /// </summary>
        public Bat bat { get; set; }

        /// <summary>
        /// collection of recordings related to a specific bat
        /// </summary>
        public BulkObservableCollection<Recording> batRecordings { get; set; } = new BulkObservableCollection<Recording>();

        /// <summary>
        /// The number of images for recordings with this bat
        /// </summary>
        public int imageCount { get; }

        /// <summary>
        /// A composite class with data about recordings with a specified bat in the specified session
        /// </summary>
        /// <param name="session"></param>
        /// <param name="bat"></param>
        public BatSession(RecordingSession session, Bat bat)
#pragma warning restore CS1591 // Missing XML comment for publicly visible type or member
        {
            try
            {
                this.session = session;
                this.bat = bat;
                imageCount = 0;
                //var segmentDatasForBat = DBAccess.GetImportedSegmentDatasForBat(bat);
                batRecordings.Clear();
                //foreach (var rec in session.Recordings)
                //{
                /*
                bool recordingHasBat = false;
                imageCount += rec.GetImageCount(bat,out recordingHasBat); // includes imported image count
                if (recordingHasBat)
                {
                    batRecordings.Add(rec);
                }*/
                var recordingsWithBat = from rec in session.Recordings
                                        from brLink in rec.BatRecordingLinks
                                        where brLink.BatID == bat.Id
                                        select rec;

                imageCount = (from rec in recordingsWithBat
                              from seg in rec.LabelledSegments
                              from lnk in seg.BatSegmentLinks
                              where lnk.BatID == bat.Id
                              select seg.SegmentDatas.Count).Sum();
                batRecordings.AddRange(recordingsWithBat);

                //}
            }
            catch (Exception ex)
            {
                Tools.ErrorLog(ex.Message);
                Debug.WriteLine(ex);
            }
        }
    }
}