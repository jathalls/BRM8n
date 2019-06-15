using Microsoft.VisualStudio.Language.Intellisense;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace BatRecordingManager
{
    #region BatRecordingListDetailControl

    /// <summary>
    ///     Interaction logic for BatRecordingsListDetailControl.xaml
    /// </summary>
    public partial class BatRecordingsListDetailControl : UserControl
    {
        /// <summary>
        ///     Initializes a new instance of the <see cref="BatRecordingsListDetailControl"/> class.
        /// </summary>
        public BatRecordingsListDetailControl()
        {
            InitializeComponent();
            //this.DataContext = this.BatStatisticsList;
            this.DataContext = this;
            //BatStatisticsList = DBAccess.GetBatStatistics();

            //BatStatsDataGrid.ItemsSource = BatStatisticsList;
            //RefreshData();
            ListByBatsImageScroller.IsReadOnly = true;
            BatStatsDataGrid.EnableColumnVirtualization = true;
            BatStatsDataGrid.EnableRowVirtualization = true;
            //sessionsAndRecordings.imageScroller = ListByBatsImageScroller;
        }

        /// <summary>
        ///     Gets or sets the bat statistics list.
        /// </summary>
        /// <value>
        ///     The bat statistics list.
        /// </value>
        public BulkObservableCollection<BatStatistics> BatStatisticsList { get; } = new BulkObservableCollection<BatStatistics>();

        /// <summary>
        ///     Refreshes the data from the databse during a context switch from any other display
        ///     screen to this one.
        /// </summary>
        /// <exception cref="System.NotImplementedException">
        ///     </exception>
        internal void RefreshData()
        {
            int old_selection = BatStatsDataGrid.SelectedIndex;
            BatStatisticsList.Clear();
            //BatStatisticsList.AddRange(DBAccess.GetBatStatistics());
            // Stopwatch watch = Stopwatch.StartNew();
            BatStatisticsList.AddRange(DBAccess.GetBatStatistics());
            // watch.Stop();
            // Debug.WriteLine("GetBatStatistics took " + watch.ElapsedMilliseconds + "ms");

            //BatStatsDataGrid.ItemsSource = BatStatisticsList;
            if (old_selection < BatStatisticsList.Count)
            {
                BatStatsDataGrid.SelectedIndex = old_selection;
            }
            //  watch.Reset();
            //  watch.Start();
            BatStatsDataGrid.Items.Refresh();
            //  watch.Stop();
            // Debug.WriteLine("DataGrid Item refresh took " + watch.ElapsedMilliseconds + "ms");
        }

        internal void SelectSession(string sessionUpdated)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Initiates the generation of a report for the selected recordings of the
        /// selected sessions of the selected bats.  If there are no selections in a
        /// panel then the report is generated to cover all the items in the panel.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void BatListReportButton_Click(object sender, RoutedEventArgs e)
        {
            List<BatStatistics> ReportBatStatsList = new List<BatStatistics>();
            List<RecordingSession> ReportSessionList = new List<RecordingSession>();
            List<Recording> ReportRecordingList = new List<Recording>();
            ReportMainWindow reportWindow = new ReportMainWindow();

            using (new WaitCursor("Generating Report..."))
            {
                if (BatStatsDataGrid.SelectedItems != null && BatStatsDataGrid.SelectedItems.Count > 0)
                {
                    foreach (var bs in BatStatsDataGrid.SelectedItems)
                    {
                        ReportBatStatsList.Add((bs as BatStatistics));
                    }
                }
                else
                {
                    ReportBatStatsList.AddRange(BatStatisticsList);
                }

                ReportSessionList = sessionsAndRecordings.GetSelectedSessions();
                ReportRecordingList = sessionsAndRecordings.GetSelectedRecordings();

                reportWindow.setReportData(ReportBatStatsList, ReportSessionList, ReportRecordingList);
            }
            reportWindow.ShowDialog();
        }

        private void BatStatsDataGrid_GotFocus(object sender, RoutedEventArgs e)
        {
        }

        /// <summary>
        /// Double clicking on the bat list adds all the images for the segments of the selected
        /// bat to the comparison window.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void BatStatsDataGrid_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            using (new WaitCursor("Adding all images to Comparison Window..."))
            {
                BulkObservableCollection<StoredImage> images = sessionsAndRecordings.RecordingImageScroller.imageList;
                ComparisonHost.Instance.AddImageRange(images);
                
            }
        }

        private void BatStatsDataGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            using (new WaitCursor("Bat selection changed"))
            {
                //DataGrid bsdg = sender as DataGrid;
                ListByBatsImageScroller.Clear();

                if (e.AddedItems == null || e.RemovedItems == null) return;
                if (e.AddedItems.Count <= 0 && e.RemovedItems.Count <= 0) return; // nothing has changed

                List<BatStatistics> selectedBatDetailsList = new List<BatStatistics>();
                foreach (var item in BatStatsDataGrid.SelectedItems)
                {
                    selectedBatDetailsList.Add(item as BatStatistics);
                }
                sessionsAndRecordings.SelectedBatDetailsList.Clear();
                sessionsAndRecordings.SelectedBatDetailsList.AddRange(selectedBatDetailsList);
                sessionsAndRecordings.SetMatchingSessionsAndRecordings();
                sessionsAndRecordings.SelectAll();

                foreach (var item in BatStatsDataGrid.SelectedItems)
                {
                    BatStatistics batstats = item as BatStatistics;
                    if (batstats.numBatImages > 0)
                    {
                        //var images = DBAccess.GetImagesForBat(batstats.bat, Tools.BlobType.BMPS);
                        var images = batstats.bat.GetImageList();
                        if (!images.IsNullOrEmpty())
                        {
                            foreach (var img in images)
                            {
                                ListByBatsImageScroller.BatImages.Add(img);
                            }
                        }
                    }
                }

                ListByBatsImageScroller.ImageScrollerDisplaysBatImages = true;
            }
        }

        private void CompareImagesButton_Click(object sender, RoutedEventArgs e)
        {
            using (new WaitCursor("Adding all images to Comparison Window..."))
            {
                BulkObservableCollection<StoredImage> images = sessionsAndRecordings.RecordingImageScroller.imageList;
                ComparisonHost.Instance.AddImageRange(images);

            }
        }
    }

    #endregion BatRecordingListDetailControl

    //==============================================================================================================================================
    //==============================================================================================================================================
    //================================== BAT STATISTICS ============================================================================================

    #region BatStatistics

    /// <summary>
    ///     </summary>
    public class BatStatistics
    {
        /// <summary>
        ///     Initializes a new instance of the <see cref="BatStatistics"/> class.
        /// </summary>
        public BatStatistics()
        {
            bat = null;

            displayable.Clear();
        }

        public BatStatistics(Bat bat)
        {
            displayable.Clear();
            this.bat = bat;
            displayable.Name = Name;
            displayable.Genus = Genus;
            displayable.Species = Species;
            displayable.Sessions = numSessions;
            displayable.Recordings = numRecordings;
            displayable.Passes = passes;
            displayable.BatImages = bat.BatPictures.Count;
            displayable.RecImages = numRecordingImages;
        }

        /// <summary>
        /// The bat to which these statistics apply.  Applies a lazy loading protocol for recordings and sessions
        /// which only get populated from bat when they are first accesses.  If they have been initialised to empty
        /// collections (or full collections) before the bat is entered then they will be updated to the correct data
        /// for the bat being entered. Similalry for the stats.
        /// </summary>
        public Bat bat
        {
            get
            {
                return (_bat);
            }
            set
            {
                Clear();
                _bat = value;
            }
        }

        public Displayable displayable { get; set; } = new Displayable();

        /// <summary>
        ///     The genus
        /// </summary>
        public String Genus
        {
            get
            {
                return (bat != null ? bat.Batgenus : "");
            }
        }

        /// <summary>
        ///     The name
        /// </summary>
        public String Name
        {
            get
            {
                return (bat != null ? bat.Name : "");
            }
        }

        /// <summary>
        /// number of images of the bat itself
        /// </summary>
        public int numBatImages
        {
            get
            {
                if (bat != null) return (bat.BatPictures.Count);
                else return (0);
            }
        }

        /// <summary>
        /// number of images associated with recordings which include this bat
        /// </summary>
        public int numRecordingImages
        {
            get
            {
                if (bat == null)
                {
                    return (0);
                }
                else
                {
                    if (_numRecordingImages < 0)
                    {
                        //_numRecordingImages = bat.BatSegmentLinks.Sum(lnk => lnk.LabelledSegment.SegmentDatas.Count);
                        _numRecordingImages = DBAccess.GetNumRecordingImagesForBat(bat.Id);
                    }
                    //var sum1 = bat.BatSegmentLinks.Select(lnk => lnk.LabelledSegment.SegmentDatas.Count).Sum();
                    return (_numRecordingImages);
                }
            }
        }

        public int numRecordings
        {
            get
            {
                if (_numRecordings < 0)
                {
                    _numRecordings = bat.BatRecordingLinks.Count;
                }
                return (_numRecordings);
            }
        }

        public int numSessions
        {
            get
            {
                if (_numSessions < 0)
                {
                    _numSessions = bat.BatSessionLinks.Count;
                }
                return (_numSessions);
            }
        }

        public int passes
        {
            get
            {
                if (_passes < 0)
                {
                    _passes = bat.BatSegmentLinks.Sum(lnk => lnk.NumberOfPasses);
                }
                return (_passes);
            }
        }

        /// <summary>
        ///     The species
        /// </summary>
        public String Species
        {
            get
            {
                return (bat != null ? bat.BatSpecies : "");
            }
        }

        public class Displayable : INotifyPropertyChanged

        {
            public Displayable()
            {
                Clear();
            }

            public event PropertyChangedEventHandler PropertyChanged;

            public int BatImages
            {
                get { return _BatImages; }
                set { _BatImages = value; pc("BatImages"); }
            }

            public String Genus
            {
                get { return (_Genus); }
                set { _Genus = value; pc("Genus"); }
            }

            public String Name
            {
                get { return (_Name); }
                set { _Name = value; pc("Name"); }
            }

            public int Passes
            {
                get { return _Passes; }
                set { _Passes = value; pc("Passes"); }
            }

            public int RecImages
            {
                get { return _RecImages; }
                set { _RecImages = value; pc("RecImages"); }
            }

            public int Recordings
            {
                get { return _Recordings; }
                set { _Recordings = value; pc("Recordings"); }
            }

            public int Sessions
            {
                get { return _Sessions; }
                set { _Sessions = value; pc("Sessions"); }
            }

            public String Species
            {
                get { return (_Species); }
                set { _Species = value; pc("Species"); }
            }

            public void Clear()
            {
                Name = "";
                Genus = "";
                Species = "";
                Sessions = 0;
                Recordings = 0;
                Passes = 0;
                BatImages = 0;
                RecImages = 0;
            }

            private int _BatImages;
            private string _Genus;
            private string _Name;
            private int _Passes;
            private int _RecImages;
            private int _Recordings;
            private int _Sessions;
            private String _Species;

            private void pc(string item)
            {
                if (PropertyChanged != null)
                {
                    PropertyChanged(this, new PropertyChangedEventArgs(item));
                }
            }
        }

        private Bat _bat;
        private int _numRecordingImages = -1;
        private int _numRecordings = -1;
        private int _numSessions = -1;
        private int _passes = -1;

        private void Clear()
        {
            _passes = -1;
            _bat = null;
            _numRecordingImages = -1;
            //_numRecordings = -1;
            _numSessions = -1;
        }
    }

    #endregion BatStatistics
}