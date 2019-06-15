
using DataVirtualizationLibrary;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace BatRecordingManager
{
    public partial class DatabaseTableControl : UserControl
    {
        

        public DatabaseTableControl()
        {
            InitializeComponent();
        }

        private void DatabaseTableDataGrid_Sorting(object sender, DataGridSortingEventArgs e)
        {
            string columnName = (e.Column.Header as string);
            if(e.Column.SortDirection!=null && e.Column.SortDirection.Value == System.ComponentModel.ListSortDirection.Descending)
            {
                columnName = columnName + " descending";
            }
            SortByColumn(columnName);
            
        }
        public void SortByColumn(string columnName) { }
    }


    public partial class RecordingSessionTableControl : DatabaseTableControl
    {
        public AsyncVirtualizingCollection<RecordingSession> VirtualizedCollectionOfRecordingSession { get; set; } = new AsyncVirtualizingCollection<RecordingSession>(new RecordingSessionProvider(), 25, 100);

        /// <summary>
        /// string to be used in Linq sortby query
        /// </summary>
        public void SortByColumn(string name)
        {
           
                VirtualizedCollectionOfRecordingSession.sortColumn = name;
            
        }

        /// <summary>
        /// default constructor
        /// </summary>
        public RecordingSessionTableControl() : base()
        {





            DatabaseTableDataGrid.DataContext = VirtualizedCollectionOfRecordingSession;

            
        }
    }

    public partial class RecordingTableControl : DatabaseTableControl
    {

        public AsyncVirtualizingCollection<Recording> VirtualizedCollectionOfRecording { get; set; } = new AsyncVirtualizingCollection<Recording>(new RecordingProvider(), 100, 100);

        public RecordingTableControl() : base()
        {

            //RecordingProvider recordingProvider = new RecordingProvider(100, 0);
            //if (recordingProvider != null)
            //{
            //    VirtualizedCollectionOfRecording = new AsyncVirtualizingCollection<Recording>(recordingProvider, 100, 0);
            //}
            //Debug.WriteLine(VirtualizedCollectionOfRecording.Count+" elements in List of Recording");




            this.DataContext = VirtualizedCollectionOfRecording;
            //Debug.WriteLine("Data Context for Recordings set");
            //VirtualizedCollectionOfRecording = new AsyncVirtualizingCollection<Recording>(recordingProvider, 100, 0);
            //Debug.WriteLine(VirtualizedCollectionOfRecording.Count + " elements in List of Recording after setting conext");
            //Debug.WriteLine(VirtualizedCollectionOfRecording[0].RecordingName);
        }
    }
}
