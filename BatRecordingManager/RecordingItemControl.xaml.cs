using Microsoft.VisualStudio.Language.Intellisense;
using System;
using System.Windows;
using System.Windows.Controls;

namespace BatRecordingManager
{
    /// <summary>
    ///     Interaction logic for RecordingItemControl.xaml
    /// </summary>
    public partial class RecordingItemControl : UserControl
    {
        #region recordingItem

        /// <summary>
        ///     recordingItem Dependency Property
        /// </summary>
        public static readonly DependencyProperty recordingItemProperty =
            DependencyProperty.Register("recordingItem", typeof(Recording), typeof(RecordingItemControl),
                new FrameworkPropertyMetadata((Recording)new Recording()));

        /// <summary>
        ///     Gets or sets the recordingItem property. This dependency property indicates ....
        /// </summary>
        /// <value>
        ///     The recording item.
        /// </value>
        public Recording RecordingItem
        {
            get
            {
                return (Recording)GetValue(recordingItemProperty);
            }
            set
            {
                SetValue(recordingItemProperty, value);
                summary = value.GetStats();
                DateTime? date = value.RecordingDate;
                if (date == null)
                {
                    date = value.RecordingSession.SessionDate;
                }
                string strDate = "";
                if (date != null)
                {
                    strDate = date.Value.ToShortDateString();
                }

                RecordingNameLabel.Content = value.RecordingName + " " + Tools.GetRecordingDuration(value).ToString();
                if (!String.IsNullOrWhiteSpace(value.RecordingGPSLatitude) && !String.IsNullOrWhiteSpace(value.RecordingGPSLongitude))
                {
                    GPSLabel.Content = value.RecordingGPSLatitude + ", " + value.RecordingGPSLongitude;
                }
                else
                {
                    GPSLabel.Content = strDate + value.RecordingStartTime.ToString() + " - " + value.RecordingEndTime.ToString();
                }
                if (!String.IsNullOrWhiteSpace(value.RecordingNotes))
                {
                    RecordingNotesLabel.Content = value.RecordingNotes;
                }
                else
                {
                    RecordingNotesLabel.Content = "";
                }

                BatPassSummaryStackPanel.Children.Clear();
                if (summary != null && summary.Count > 0)
                {
                    foreach (var batType in summary)
                    {
                        BatPassSummaryControl batPassControl = new BatPassSummaryControl();
                        batPassControl.PassSummary = batType;
                        BatPassSummaryStackPanel.Children.Add(batPassControl);
                    }
                }

                LabelledSegmentListView.Items.Clear();
                foreach (var segment in value.LabelledSegments)
                {
                    LabelledSegmentControl labelledSegmentControl = new LabelledSegmentControl();
                    labelledSegmentControl.labelledSegment = segment;
                    LabelledSegmentListView.Items.Add(labelledSegmentControl);
                }
                InvalidateArrange();
                UpdateLayout();
            }
        }

        #endregion recordingItem

        /// <summary>
        ///     Initializes a new instance of the <see cref="RecordingItemControl"/> class.
        /// </summary>
        public RecordingItemControl()
        {
            InitializeComponent();
            this.DataContext = RecordingItem;
            GPSLabel.MouseDoubleClick += GPSLabel_MouseDoubleClick;
        }

        /// <summary>
        ///     Deletes the recording.
        /// </summary>
        internal void DeleteRecording()
        {
            if (RecordingItem != null)
            {
                String err = DBAccess.DeleteRecording(RecordingItem);
                if (!String.IsNullOrWhiteSpace(err))
                {
                    MessageBox.Show(err, "Delete Recording Failed");
                }
            }
        }

        /// <summary>
        ///     The summary
        /// </summary>
        private BulkObservableCollection<BatStats> summary;

        private void GPSLabel_MouseDoubleClick(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            Clipboard.Clear();
            Clipboard.SetText(GPSLabel.Content as string);
        }
    }
}