using Microsoft.VisualStudio.Language.Intellisense;
using Mm.ExportableDataGrid;
using System;
using System.Windows.Controls;

namespace BatRecordingManager
{
    /// <summary>
    /// Interaction logic for ReportMaster.xaml
    /// This class is the base class for specific report types.
    /// </summary>
    public abstract partial class ReportMaster : UserControl
    {
        /// <summary>
        /// default constructor
        /// </summary>
        public ReportMaster()
        {
            InitializeComponent();
            ReportDataGrid.EnableColumnVirtualization = true;
            ReportDataGrid.EnableRowVirtualization = true;
        }

        /// <summary>
        /// abstract generic list for the specific type of report data
        /// </summary>
        public abstract string tabHeader { get; }

        /// <summary>
        /// Generic SetData for each tabbed instance of ReportMaster
        /// </summary>
        /// <param name="reportBatStatsList"></param>
        /// <param name="reportSessionList"></param>
        /// <param name="reportRecordingList"></param>
        public abstract void SetData(BulkObservableCollection<BatStatistics> reportBatStatsList, BulkObservableCollection<RecordingSession> reportSessionList, BulkObservableCollection<Recording> reportRecordingList);

        internal void Export(CsvExporter exporter, string fileName)
        {
            ReportDataGrid.ExportUsingRefection(exporter, fileName);
        }

        /// <summary>
        /// Generic column creation for versatile DataGrids
        /// </summary>
        /// <param name="header"></param>
        /// <param name="bindingSource"></param>
        /// <param name="visibility"></param>
        /// <param name="converter"></param>
        /// <returns></returns>
        protected DataGridTextColumn CreateColumn(string header, string bindingSource, System.Windows.Visibility visibility, string converter)
        {
            DataGridTextColumn column = new DataGridTextColumn();
            column.Header = header;
            System.Windows.Data.Binding bind = new System.Windows.Data.Binding(bindingSource);
            if (!String.IsNullOrWhiteSpace(converter))
            {
                if (converter == "ShortTime_Converter")
                    bind.Converter = new ShortTimeConverter();
                else if (converter == "ShortDate_Converter")
                    bind.Converter = new ShortDateConverter();
            }
            column.Binding = bind;
            column.Visibility = visibility;
            return (column);
        }

        /// <summary>
        /// Sets the header text into the headerTextBox and returns it to be put into the
        /// DataGrid.  Adds bat passes summary for the session.
        /// </summary>
        /// <param name="session"></param>
        /// <returns></returns>
        protected string SetHeaderText(RecordingSession session)
        {
            string Footnote = @"
* NB Grid references marked with * are session locations, others are for the start of the recording
";
            string result = session.SessionTag + "\n" + session.SessionNotes.Replace(',', ';') + "\n";

            var summary = Tools.GetSessionSummary(session);
            foreach (var item in summary)
            {
                result += item.Replace(',', ';') + "\n";
            }
            HeaderTextBox.Text += result +Footnote+ "***************************************\n";

            return (result);
        }

        //protected BulkObservableCollection<BatStatistics> ReportBatStatsList { get; set; } = new BulkObservableCollection<BatStatistics>();
        //protected BulkObservableCollection<RecordingSession> ReportSessionList { get; set; } = new BulkObservableCollection<RecordingSession>();
        //protected BulkObservableCollection<Recording> ReportRecordingList { get; set; } = new BulkObservableCollection<Recording>();
        private void ReportDataGrid_ColumnReordered(object sender, DataGridColumnEventArgs e)
        {
        }
    }
}