using Microsoft.VisualStudio.Language.Intellisense;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Controls;

namespace BatRecordingManager
{
    /// <summary>
    /// Specific case of ReportMaster for displaying and exporting reports
    /// organised on the basis of Recordings
    /// </summary>
    internal class ReportByRecordings : ReportMaster
    {
        /// <summary>
        /// BySession data list for cross reference
        /// </summary>
        //public BulkObservableCollection<ReportData> reportDataBySessionList { get; set; } = new BulkObservableCollection<ReportData>();
        /// <summary>
        /// default constructor
        /// </summary>
        public ReportByRecordings() { }

        /// <summary>
        /// main report data list for this instance
        /// </summary>
        public BulkObservableCollection<RecordingReportData> reportDataList { get; set; } = new BulkObservableCollection<RecordingReportData>();

        /// <summary>
        /// Specific header for the tab containing this report type
        /// </summary>
        public override string tabHeader { get; } = "Recordings";

        /// <summary>
        /// Specific instance of data initialization and configuration for this report type
        /// </summary>
        /// <param name="reportBatStatsList"></param>
        /// <param name="reportSessionList"></param>
        /// <param name="reportRecordingList"></param>
        public override void SetData(BulkObservableCollection<BatStatistics> reportBatStatsList,
            BulkObservableCollection<RecordingSession> reportSessionList,
            BulkObservableCollection<Recording> reportRecordingList)
        {
            //reportDataBySessionList.Clear();
            HeaderTextBox.Text = "";
            reportDataList.Clear();
            this.DataContext = this;
            int recnum = 0;
            //string lastTag = null;
            List<int> sessionList = new List<int>();
            foreach (var session in reportSessionList)
            {
                bool sessionHeaderAdded = false;
                var allStatsForSession = session.GetStats();
                if (!allStatsForSession.IsNullOrEmpty())
                {
                    foreach (var batStats in reportBatStatsList)
                    {
                        if (batStats.bat != null)
                        {
                            var thisBatStatsForSession = from bs in allStatsForSession
                                                         where bs.batCommonName == batStats.bat.Name
                                                         select bs;
                            if (!thisBatStatsForSession.IsNullOrEmpty())
                            {
                                var statsForAllSessions = new BatStats();
                                foreach (var bs in thisBatStatsForSession)
                                {
                                    statsForAllSessions.Add(bs);
                                }
                                sessionList.Add(recnum);
                                foreach (var recording in reportRecordingList)
                                {
                                    if (recording.RecordingSession.Id == session.Id)
                                    {
                                        var allSTatsForRecording = recording.GetStats();
                                        var thisBatStatsForRecording = from bs in allSTatsForRecording
                                                                       where bs.batCommonName == batStats.Name

                                                                       select bs;
                                        if (!thisBatStatsForRecording.IsNullOrEmpty())
                                        {
                                            if (statsForAllSessions.passes > 0 && thisBatStatsForRecording.First().passes > 0)
                                            {
                                                //ReportData reportData = new ReportData();
                                                RecordingReportData recordingReportData = new RecordingReportData();

                                                //reportData.bat = batStats.bat;
                                                //recordingReportData.bat = batStats.bat;
                                                //reportData.session = session;
                                                //reportData.sessionStats = statsForAllSessions;
                                                //reportData.recording = recording;
                                                //reportData.recordingStats = thisBatStatsForRecording.First();
                                                //reportDataBySessionList.Add(reportData);
                                                if (!sessionHeaderAdded)
                                                {
                                                    recordingReportData.sessionHeader = SetHeaderText(session);
                                                    sessionHeaderAdded = true;
                                                }
                                                else
                                                {
                                                    recordingReportData.sessionHeader = "";
                                                }
                                                recordingReportData.recording = recording;
                                                recordingReportData.bat = batStats.bat;
                                                recordingReportData.session = session;
                                                recordingReportData.sessionStats = statsForAllSessions;

                                                recordingReportData.recordingStats = thisBatStatsForRecording.First();
                                                reportDataList.Add(recordingReportData);

                                                recnum++;
                                            }
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
            }
            if (reportDataList != null)
            {
                BulkObservableCollection<RecordingReportData> tmpList = new BulkObservableCollection<RecordingReportData>();
                tmpList.AddRange(reportDataList.OrderBy(recrepdata => recrepdata.recording.RecordingName));
                reportDataList = tmpList;
            }

            CreateTable();

            ReportDataGrid.ItemsSource = reportDataList;
        }

        /// <summary>
        /// Creates the relevant columns in the DataGrid and assigns the relevant bindings
        /// </summary>
        private void CreateTable()
        {
            DataGridTextColumn column;
            column = CreateColumn("Session", "sessionHeader", System.Windows.Visibility.Hidden, "");
            ReportDataGrid.Columns.Add(column);
            column = CreateColumn("Recording", "recording.RecordingName", System.Windows.Visibility.Visible, "");
            ReportDataGrid.Columns.Add(column);
            column = CreateColumn("Latitude", "recording.RecordingGPSLatitude", System.Windows.Visibility.Visible, "");
            ReportDataGrid.Columns.Add(column);
            column = CreateColumn("Longitude", "recording.RecordingGPSLongitude", System.Windows.Visibility.Visible, "");
            ReportDataGrid.Columns.Add(column);
            column = CreateColumn("Bat", "bat.Name", System.Windows.Visibility.Visible, "");
            ReportDataGrid.Columns.Add(column);
            column = CreateColumn("Start Time", "recording.RecordingStartTime", System.Windows.Visibility.Visible, "ShortTime_Converter");

            ReportDataGrid.Columns.Add(column);
            column = CreateColumn("End Time", "recording.RecordingEndTime", System.Windows.Visibility.Visible, "ShortTime_Converter");
            ReportDataGrid.Columns.Add(column);
            column = CreateColumn("Passes", "recordingStats.passes", System.Windows.Visibility.Visible, "");
            ReportDataGrid.Columns.Add(column);
            column = CreateColumn("Total length", "recordingStats.totalDuration", System.Windows.Visibility.Visible, "ShortTime_Converter");
            ReportDataGrid.Columns.Add(column);
        }
    }
}