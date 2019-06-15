using Microsoft.VisualStudio.Language.Intellisense;
using System;
using System.Diagnostics;
using System.Linq;
using System.Windows.Controls;

namespace BatRecordingManager
{
    /// <summary>
    /// Derived class tailoring the report as a report by frequency
    /// while using the ReportMaster TextBox/DataGrid layout and support
    /// functions.
    /// </summary>
    public class ReportByFrequency : ReportMaster
    {
        /// <summary>
        /// default constructor
        /// </summary>
        public ReportByFrequency()
        {
        }

        /// <summary>
        /// BOC to hold the formatted Frequency data for display and export
        /// </summary>
        public BulkObservableCollection<FrequencyData> reportDataList { get; set; } = new BulkObservableCollection<FrequencyData>();

        /// <summary>
        /// Read only string for the label in the tab to identify this report type
        /// </summary>
        public override string tabHeader { get; } = "Frequencies";

        /// <summary>
        /// SetData is passed the full set of report data in the form of three lists and uses whatever is necessary
        /// to format and populate this particular datagrid.  It overrides the abstract function in reportMaster.
        /// </summary>
        /// <param name="reportBatStatsList"></param>
        /// <param name="reportSessionList"></param>
        /// <param name="reportRecordingList"></param>
        override public void SetData(BulkObservableCollection<BatStatistics> reportBatStatsList, BulkObservableCollection<RecordingSession> reportSessionList, BulkObservableCollection<Recording> reportRecordingList)
        {
            ReportDataGrid.DataContext = this;
            //var binding = new Binding("reportDataList");

            //binding.Source = new FrequencyData(10,new Bat(),new BulkObservableCollection<int>());
            //ReportDataGrid.SetBinding(DataGrid.ItemsSourceProperty, binding);

            int AggregationPeriod = 10;
            reportDataList = SetFrequencyData(AggregationPeriod, reportSessionList);
            CreateFrequencyTable();
            ReportDataGrid.ItemsSource = reportDataList;
        }

        /// <summary>
        /// Creates a single column in the reportDataGridByFrequency Datagrid which will hold the list of bats
        /// in the reportDataGridByFrequencyList to which the grid is already bound.
        /// Plus an invisible first column which will hold the session tags and notes
        /// </summary>
        /// <returns></returns>
        private DataGridColumn CreateBatColumn()
        {
            DataGridTextColumn headerColumn = new DataGridTextColumn();
            headerColumn.Header = "Sessions";
            headerColumn.Binding = new System.Windows.Data.Binding("sessionHeader");
            headerColumn.Visibility = System.Windows.Visibility.Hidden;
            ReportDataGrid.Columns.Add(headerColumn);

            DataGridTextColumn batColumn = new DataGridTextColumn();
            //var a=reportDataByFrequencyList.First().OccurrencesPerPeriod
            batColumn.Header = "Bat";
            batColumn.Binding = new System.Windows.Data.Binding("bat.Name");
            return (batColumn);
        }

        /// <summary>
        /// creates the datagrid for the frequency of occurrence data stored in reportDataByFrequencyList.
        /// The table has a column for bat species and a column for each aggregation period in 24 hours running
        /// from noon to noon.  Cells are bound to the reportDataByFrequencyList.
        /// </summary>
        private void CreateFrequencyTable()
        {
            ReportDataGrid.Columns.Clear();
            if (reportDataList == null || reportDataList.Count <= 0)
            {
                return;
            }
            int AggregationPeriod = reportDataList.FirstOrDefault().AggregationPeriod;
            TimeSpan day = new TimeSpan(24, 0, 0);
            int minutesInDay = (int)day.TotalMinutes;

            ReportDataGrid.Columns.Add(CreateBatColumn());

            for (int i = 0; i < reportDataList.FirstOrDefault().OccurrencesPerPeriod.Count; i++)
            {
                ReportDataGrid.Columns.Add(CreateOccurrencesColumn(AggregationPeriod, i));
            }
        }

        /// <summary>
        /// Generates each column in the occurrences array list
        /// </summary>
        /// <param name="i"></param>
        /// <returns></returns>
        private DataGridColumn CreateOccurrencesColumn(int Aggregationperiod, int i)
        {
            TimeSpan time = new TimeSpan(12, Aggregationperiod * i, 0);
            string strTime = string.Format("{0}:{1}", time.Hours, time.Minutes);
            DataGridTextColumn valueColumn = new DataGridTextColumn();
            valueColumn.Header = strTime;

            valueColumn.Binding = new System.Windows.Data.Binding("OccurrencesPerPeriod[" + i + "]");

            return (valueColumn);
        }

        /// <summary>
        /// Assuming aggregation in Aggregationperiods from midday to midday, get the indeces into the array of aggreagtion
        /// values for the start abd stop times in the recordingperiod.
        /// </summary>
        /// <param name="aggregationPeriod"></param>
        /// <param name="recordingPeriod"></param>
        /// <returns></returns>
        private Tuple<int, int> GetAggregationIndeces(int aggregationPeriod, Tuple<DateTime, DateTime> recordingPeriod)
        {
            int startIndex = 0;
            int periods = 1440 / aggregationPeriod;
            startIndex = GetAggregationIndex(aggregationPeriod, recordingPeriod.Item1);
            periods = (int)(((recordingPeriod.Item2 - recordingPeriod.Item1).TotalMinutes) / aggregationPeriod);
            if (periods <= 0) periods = 1;
            return (new Tuple<int, int>(startIndex, periods));
        }

        /// <summary>
        /// Return the index into a 144 element array corresponding to the time given
        /// </summary>
        /// <param name="aggregationPeriod"></param>
        /// <param name="recordingTime"></param>
        /// <returns></returns>
        private int GetAggregationIndex(int aggregationPeriod, DateTime recordingTime)
        {
            //TimeSpan day = new TimeSpan(24, 0, 0);
            //int minutesInDay = (int)day.TotalMinutes;
            int recordingTimeInMinutes = (int)(recordingTime.TimeOfDay.TotalMinutes - 720);
            if (recordingTimeInMinutes < 0) recordingTimeInMinutes += 1440;
            int index = (int)Math.Floor((decimal)recordingTimeInMinutes / (decimal)aggregationPeriod);
            return (index);
        }

        /// <summary>
        /// Accumulates frequency data per bat from the speified session into the list of FrequencyData
        /// </summary>
        /// <param name="session"></param>
        /// <param name="result"></param>
        private BulkObservableCollection<FrequencyData> GetFrequencyDataForSession(RecordingSession session, BulkObservableCollection<FrequencyData> result)
        { // example data for LGL18-2am_20180620
            Debug.WriteLine("GetFrequencyData for " + session.SessionTag);
            if (result == null || !(result.Count > 0)) return (result);
            Tuple<DateTime, DateTime> recordingPeriod;
            int AggregationPeriod = result.FirstOrDefault().AggregationPeriod;
            TimeSpan AggregationTimeSpan = new TimeSpan(0, AggregationPeriod, 0);

            recordingPeriod = DBAccess.GetRecordingPeriod(session); //20/6/2018 22:04 - 21/6/2018 06:07:28
            if (recordingPeriod.Item2 < recordingPeriod.Item1)
            {
                Tuple<DateTime, DateTime> reversed = new Tuple<DateTime, DateTime>(recordingPeriod.Item2, recordingPeriod.Item1);
                recordingPeriod = reversed;
                // since the recording session has a negative or zero length

            }
            recordingPeriod = FrequencyData.NormalizePeriod(AggregationPeriod, recordingPeriod); // 20/6/2018 22:00 - 21/6/2018 06:10:00

            Tuple<int, int> indexForRecordingStartAndNumberOfPeriods = GetAggregationIndeces(AggregationPeriod, recordingPeriod); //60,49
            DateTime SampleStart;
            int i = 0;
            int periodsPerDay = result[0].OccurrencesPerPeriod.Count;
            for (SampleStart = recordingPeriod.Item1;
                i <= indexForRecordingStartAndNumberOfPeriods.Item2;
                i++, SampleStart = SampleStart + AggregationTimeSpan)// 21:50-22:00, 22:00-22:10, 22:10-22:20
            {
                Debug.WriteLine("Period " + SampleStart.ToShortTimeString());
                //foreach(var batData in result)
                for (int j = 0; j < result.Count; j++)
                {
                    Debug.WriteLine("Bat=" + result[j].bat.Name);
                    try
                    {
                        result[j].OccurrencesPerPeriod[(i + indexForRecordingStartAndNumberOfPeriods.Item1) % periodsPerDay] += DBAccess.GetOccurrencesInWindow(session, result[j].bat, SampleStart,AggregationPeriod);
                        //0; 3,2,1 - CP
                        //1, 0,2,0 - P50
                        //2, 0,3,1 - SP
                        //3, 0,0,2 - DB
                    }
                    catch (IndexOutOfRangeException iorex)
                    {
                        Debug.WriteLine(iorex.Message);
                        Tools.ErrorLog("From GetFrequencyDataForSession - Index out of range error [" + (i + indexForRecordingStartAndNumberOfPeriods.Item1) % periodsPerDay + "] period-" + AggregationPeriod + " :-");
                    }
                }
            }
            return (result);
        }

        /// <summary>
        /// Creates a frequency of occurrence data set for a number of specified recording sessions
        /// FrequencyData is a row in the report, corresponding to one type of bat.  It has a bat, a
        /// sessionHeader and an array of Occurrences/Period
        /// </summary>
        /// <param name="reportSessionList"></param>
        /// <returns></returns>
        private BulkObservableCollection<FrequencyData> SetFrequencyData(int AggregationPeriod, BulkObservableCollection<RecordingSession> reportSessionList)
        {
            TimeSpan day = new TimeSpan(24, 0, 0);
            int minutesInDay = (int)day.TotalMinutes;
            int numberOfAggregationPeriods = (int)Math.Floor((decimal)minutesInDay / (decimal)AggregationPeriod);
            BulkObservableCollection<FrequencyData> result = new BulkObservableCollection<FrequencyData>();

            if (reportSessionList != null && !reportSessionList.IsNullOrEmpty())
            {
                BulkObservableCollection<Bat> batList = DBAccess.GetBatsForTheseSessions(reportSessionList);
                if (batList != null)
                {
                    // First establishes a row for each bat and fills every Occurrences/Period across the row with 0
                    foreach (var bat in batList)
                    {
                        FrequencyData data = new FrequencyData(AggregationPeriod, bat, null);
                        data.OccurrencesPerPeriod.Clear();
                        for (int i = 0; i < numberOfAggregationPeriods; i++)
                        {
                            data.OccurrencesPerPeriod.Add(0);
                        }
                        result.Add(data);
                    }
                }
                foreach (var session in reportSessionList)
                {
                    // GetFrequencyDataForSession accumulates the number of occurrences into the pre-created result table
                    result = GetFrequencyDataForSession(session, result);
                }
            }
            HeaderTextBox.Text = "";
            foreach (var session in reportSessionList)
            {
                FrequencyData data = new FrequencyData(1, null, null);
                data.sessionHeader = SetHeaderText(session);
                result.Add(data);
            }
            return (result);
        }
    }
}