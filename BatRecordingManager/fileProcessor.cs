using Microsoft.VisualStudio.Language.Intellisense;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Windows;

namespace BatRecordingManager
{
    /// <summary>
    ///     Class to hold details of a specific LabelledSegment, and a List of Bats that were present
    ///     during this segment.
    /// </summary>
    public class SegmentAndBatList
    {
        /// <summary>
        ///     The List of Bats present during the segment
        /// </summary>
        public BulkObservableCollection<Bat> batList = new BulkObservableCollection<Bat>();

        /// <summary>
        ///     The Labelled Segment
        /// </summary>
        public LabelledSegment segment = new LabelledSegment();

#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member
        public bool updated;
#pragma warning restore CS1591 // Missing XML comment for publicly visible type or member

        /// <summary>
        ///     Initializes a new instance of the <see cref="SegmentAndBatList"/> class.
        /// </summary>
        public SegmentAndBatList()
        {
            segment = new LabelledSegment();
            batList = new BulkObservableCollection<Bat>();
            updated = false;
        }

        /// <summary>
        /// Creates a SegmentAndBatList item using the provided labelledSegment.
        /// The SegmentAndBatList contains the provided segment and a list of all
        /// the bats referenced by the segment comment.
        /// </summary>
        /// <param name="segment"></param>
        /// <returns></returns>
        internal static SegmentAndBatList Create(LabelledSegment segment)
        {
            SegmentAndBatList segBatList = new SegmentAndBatList();
            segBatList.segment = segment;

            segBatList.batList = DBAccess.GetDescribedBats(segment.Comment);

            //DBAccess.InsertParamsFromComment(segment.Comment, null);
            BulkObservableCollection<StoredImage> ListOfSegmentImages = segment.GetImageList();
            DBAccess.UpdateLabelledSegment(segBatList, segment.RecordingID, ListOfSegmentImages, null);
            return (segBatList);
        }

        /// <summary>
        ///     Processes the labelled segment. Accepts a processed segment comment line consisting
        ///     of a start offset, end offset, duration and comment string and generates a new
        ///     Labelled segment instance and BatSegmentLink instances for each bat represented in
        ///     the Labelled segment. The instances are merged into a single instance of
        ///     CombinedSegmentAndBatPasses to be returned. If the line to be processed is not in the
        ///     correct format then an instance containing an empty LabelledSegment instance and an
        ///     empty List of ExtendedBatPasses. The comment section is checked for the presence of a
        ///     call parameter string and if present new Call is created and populated.
        /// </summary>
        /// <param name="processedLine">
        ///     The processed line.
        /// </param>
        /// <param name="bats">
        ///     The bats.
        /// </param>
        /// <returns>
        /// </returns>
        /// <exception cref="System.NotImplementedException">
        /// </exception>
        public static SegmentAndBatList ProcessLabelledSegment(string processedLine, BulkObservableCollection<Bat> bats)
        {
            LabelledSegment segment = new LabelledSegment();
            SegmentAndBatList result = new SegmentAndBatList();
            var match = Regex.Match(processedLine, "([0-9\\.\\']+)[\\\"]?\\s*-?\\s*([0-9\\.\\']+)[\\\"]?\\s*=\\s*([0-9\\.\']+)[\\\"]?\\s*(.+)");
            //e.g. (123'12.3)" - (123'12.3)" = (123'12.3)" (other text)
            if (match.Success)
            {
                //int passes = 1;
                // The line structure matches a labelled segment
                if (match.Groups.Count > 3)
                {
                    segment.Comment = match.Groups[4].Value;

                    TimeSpan ts = Tools.TimeParse(match.Groups[2].Value);
                    segment.EndOffset = ts;
                    ts = Tools.TimeParse(match.Groups[1].Value);
                    segment.StartOffset = ts;
                    result.segment = segment;
                    result.batList = bats;
                    //ts = TimeParse(match.Groups[3].Value);
                    //passes = new BatStats(ts).passes;
                }
                // result.batPasses = IdentifyBatPasses(passes, bats);
            }

            return (result);
        }
    }

    /////////////////////////////////////////////////////////////////////////////////////////////////////////////
    ///////////////////////////////// FILE PROCESSOR CLASS  /////////////////////////////////////////////////////
    /////////////////////////////////////////////////////////////////////////////////////////////////////////////

    /// <summary>
    ///     This class handles the data processing for a single file, whether a manually generated
    ///     composite file or a label file created by Audacity.
    /// </summary>
    internal class FileProcessor
    {
        /// <summary>
        ///     The bats found
        /// </summary>
        public Dictionary<string, BatStats> BatsFound = new Dictionary<string, BatStats>();

        private BulkObservableCollection<String> linesToMerge = null;

        /// <summary>
        ///     The m bat summary
        /// </summary>
        //private BatSummary mBatSummary;

        private MODE mode = MODE.PROCESS;

        /// <summary>
        ///     The output string
        /// </summary>
        private string OutputString = "";

        /// <summary>
        ///     Initializes a new instance of the <see cref="FileProcessor"/> class.
        /// </summary>
        public FileProcessor()
        {
        }

        private enum MODE
        { PROCESS, SKIP, COPY, MERGE };

        /// <summary>
        ///     Determines whether [is label file line] [the specified line].
        /// </summary>
        /// <param name="line">
        ///     The line.
        /// </param>
        /// <param name="startStr">
        ///     The start string.
        /// </param>
        /// <param name="endStr">
        ///     The end string.
        /// </param>
        /// <param name="comment">
        ///     The comment.
        /// </param>
        /// <returns>
        /// </returns>
        public static bool IsLabelFileLine(string line, out string startStr, out string endStr, out string comment)
        {
            startStr = "";
            endStr = "";
            comment = "";
            //string regexLabelFileLine = "\\A\\s*(\\d*\\.?\\d*)\\\"?\\s+-?\\s*(\\d*\\.?\\d*)\\\"?\\s*(.*)";
            string regexLabelFileLine = "([0-9\\.\\'\\\"]+)\\s*-?\\s*([0-9\\.\\'\\\"]+)\\s*(.*)";
            // e.g. (groups in brackets) <start> (nnn.nnn)" - (nnn.nnn)" (other text)
            // (startTime)[ ][-][ ](endTime)[ ]([text][{text}])
            Match match = Regex.Match(line, regexLabelFileLine);
            if (match.Success)
            {
                startStr = match.Groups[1].Value;
                if (startStr.Contains("'"))
                {
                    TimeSpan ts = GetTimeOffset(startStr);
                    startStr = ts.TotalSeconds.ToString();
                }
                endStr = match.Groups[2].Value;
                if (endStr.Contains("'"))
                {
                    TimeSpan ts = GetTimeOffset(endStr);
                    endStr = ts.TotalSeconds.ToString();
                }
                comment = match.Groups[3].Value;
                string moddedComment = comment;
                DBAccess.GetDescribedBats(comment, out moddedComment);
                comment = moddedComment;
                return (true);
            }
            return (false);
        }

        /// <summary>
        /// Identifies a line of text as AboutScreen valid line from a label file and returns the comment section
        /// and time fields in out parameters and true or false depending on whether it is a valid line
        /// </summary>
        /// <param name="line"></param>
        /// <param name="start"></param>
        /// <param name="end"></param>
        /// <param name="comment"></param>
        /// <returns></returns>
        public static bool IsLabelFileLine(string line, out TimeSpan start, out TimeSpan end, out string comment)
        {
            string startStr = "";
            string endStr = "";
            if (!IsLabelFileLine(line, out startStr, out endStr, out comment))
            {
                start = new TimeSpan();
                end = new TimeSpan();
                return (false);
            }

            start = Tools.TimeParse(startStr);
            end = Tools.TimeParse(endStr);

            return (true);
        }

        /// <summary>
        ///     Adds to bat summary.
        /// </summary>
        /// <param name="line">
        ///     The line.
        /// </param>
        /// <param name="NewDuration">
        ///     The new duration.
        /// </param>
        public static BulkObservableCollection<Bat> AddToBatSummary(string line, TimeSpan NewDuration, ref Dictionary<string, BatStats> BatsFound)
        {
            BulkObservableCollection<Bat> bats = DBAccess.GetDescribedBats(line);
            if (!bats.IsNullOrEmpty())
            {
                foreach (var bat in bats)
                {
                    string batname = bat.Name;
                    if (!string.IsNullOrWhiteSpace(batname))
                    {
                        if (BatsFound.ContainsKey(batname))
                        {
                            BatsFound[batname].Add(NewDuration);
                        }
                        else
                        {
                            BatsFound.Add(batname, new BatStats(NewDuration));
                        }
                    }
                }
            }
            return (bats);
        }

        /// <summary>
        ///     Processes the file using ProcessLabelOrManualFile.
        ///
        /// The file may be a .txt file which is a comment/log file made with
        /// Audacity or a .wav file with embedded 'wamd' metadata
        /// </summary>
        /// <param name="batSummary">
        ///     The bat summary.
        /// </param>
        /// <param name="fileName">
        ///     Name of the file.
        /// </param>
        /// <param name="gpxHandler">
        ///     The GPX handler.
        /// </param>
        /// <param name="CurrentRecordingSessionId">
        ///     The current recording session identifier.
        /// </param>
        /// <returns>
        /// </returns>
        public static String ProcessFile(string fileName, GpxHandler gpxHandler, int CurrentRecordingSessionId, ref Dictionary<string, BatStats> BatsFound)
        {
            //mBatSummary = batSummary;
            string OutputString = "";
            if (fileName.ToUpper().EndsWith(".TXT") || fileName.ToUpper().EndsWith(".WAV"))
            {
                OutputString = ProcessLabelOrManualFile(fileName, gpxHandler, CurrentRecordingSessionId, ref BatsFound);
            }
            return (OutputString);
        }

        /// <summary>
        ///     Processes the manual file line.
        /// </summary>
        /// <param name="match">
        ///     The match.
        /// </param>
        /// <param name="bats">
        ///     The bats.
        /// </param>
        /// <returns>
        /// </returns>
        public static string ProcessManualFileLine(Match match, out BulkObservableCollection<Bat> bats, ref Dictionary<string, BatStats> BatsFound)
        {
            string comment = "";
            bats = new BulkObservableCollection<Bat>();

            if (match.Groups.Count >= 5)
            {
                string strStartOffset = match.Groups[1].Value;
                TimeSpan startTime = GetTimeOffset(strStartOffset);
                comment = comment + Tools.FormattedTimeSpan(startTime) + " - ";
                string strEndOffset = match.Groups[3].Value;
                TimeSpan endTime = GetTimeOffset(strEndOffset);
                comment = comment + Tools.FormattedTimeSpan(endTime) + " = ";
                TimeSpan thisDuration = endTime - startTime;
                comment = comment + Tools.FormattedTimeSpan(endTime - startTime) + " \t";
                for (int i = 4; i < match.Groups.Count; i++)
                {
                    comment = comment + match.Groups[i];
                }
                bats = AddToBatSummary(comment, thisDuration, ref BatsFound);
            }
            return (comment + "\n");
        }

        /// <summary>
        ///     Gets the time offset.
        /// </summary>
        /// <param name="strTime">
        ///     The string time.
        /// </param>
        /// <returns>
        /// </returns>
        private static TimeSpan GetTimeOffset(String strTime)
        {
            int Minutes = 0;
            int Seconds = 0;
            int Milliseconds = 0;
            TimeSpan result = new TimeSpan();

            if (strTime.ToUpper().Contains("START") || strTime.ToUpper().Contains("END"))
            {
                strTime = "0.0";
            }

            String NumberRegex = @"[0-9]+";
            Regex regex = new Regex(NumberRegex);
            MatchCollection allMatches = regex.Matches(strTime);
            if (allMatches != null)
            {
                if (allMatches.Count == 3)
                {
                    int.TryParse(allMatches[0].Value, out Minutes);
                    int.TryParse(allMatches[1].Value, out Seconds);
                    int.TryParse(allMatches[2].Value, out Milliseconds);
                }
                else if (allMatches.Count == 2)
                {
                    if (strTime.Contains(@"'"))
                    {
                        int.TryParse(allMatches[0].Value, out Minutes);
                        int.TryParse(allMatches[1].Value, out Seconds);
                    }
                    else
                    {
                        int.TryParse(allMatches[0].Value, out Seconds);
                        int.TryParse(allMatches[1].Value, out Milliseconds);
                    }
                }
                else if (allMatches.Count == 1)
                {
                    if (strTime.Contains(@"'"))
                    {
                        int.TryParse(allMatches[0].Value, out Minutes);
                    }
                    else
                    {
                        int.TryParse(allMatches[0].Value, out Seconds);
                    }
                }
                result = new TimeSpan(0, 0, Minutes, Seconds, Milliseconds);
            }
            return (result);
        }

        /// <summary>
        ///     Gets the duration of the file. (NB would be improved by using various Regex to parse the
        ///     filename into dates and times
        /// </summary>
        /// <param name="fileName">
        ///     Name of the file.
        /// </param>
        /// <param name="wavfile">
        ///     The wavfile.
        /// </param>
        /// <param name="fileStart">
        ///     The file start.
        /// </param>
        /// <param name="fileEnd">
        ///     The file end.
        /// </param>
        /// <returns>
        /// </returns>
        private static TimeSpan GetFileDuration(string fileName, out string wavfile, out DateTime fileStart, out DateTime fileEnd)
        {
            DateTime CreationTime;
            fileStart = new DateTime();
            fileEnd = new DateTime();

            TimeSpan duration = new TimeSpan(0L);
            wavfile = "";
            try
            {
                string wavfilename = fileName.Substring(0, fileName.Length - 4);
                wavfilename = wavfilename + ".wav";
                if (File.Exists(wavfilename))
                {
                    var Info = new FileInfo(wavfilename);
                    wavfile = wavfilename;
                    var fa = File.GetAttributes(wavfile);

                    String RecordingTime = wavfilename.Substring(Math.Max(fileName.LastIndexOf('_'), fileName.LastIndexOf('-')) + 1, 6);

                    DateTime recordingDateTime;
                    CreationTime = File.GetLastWriteTime(wavfilename);
                    if (String.IsNullOrWhiteSpace(RecordingTime))
                    {
                        RecordingTime = CreationTime.Hour.ToString() + CreationTime.Minute.ToString() + CreationTime.Second.ToString();
                    }

                    if (RecordingTime.Length == 6)
                    {
                        int hour;
                        int minute;
                        int second;
                        if (!int.TryParse(RecordingTime.Substring(0, 2), out hour))
                        {
                            hour = -1;
                        }
                        if (!int.TryParse(RecordingTime.Substring(2, 2), out minute))
                        {
                            minute = -1;
                        }
                        if (!int.TryParse(RecordingTime.Substring(4, 2), out second))
                        {
                            second = -1;
                        }
                        if (hour >= 0 && minute >= 0 && second >= 0)
                        {
                            recordingDateTime = new DateTime(CreationTime.Year, CreationTime.Month, CreationTime.Day, hour, minute, second);
                            duration = CreationTime - recordingDateTime;
                            if (duration < new TimeSpan())
                            {
                                duration = duration.Add(new TimeSpan(24, 0, 0));
                            }
                            fileStart = recordingDateTime;
                            fileEnd = CreationTime;
                        }
                    }
                    else
                    {
                        if (CreationTime != null)
                        {
                            fileStart = CreationTime;
                            fileEnd = CreationTime;
                            duration = new TimeSpan();
                        }
                    }
                }
            }
            catch (Exception ex) { Tools.ErrorLog(ex.Message); Debug.WriteLine(ex); }

            return (duration);
        }

        /*
        private BulkObservableCollection<BatSegmentLink> IdentifyBatPasses(int passes, BulkObservableCollection<Bat> bats)
        {
            BulkObservableCollection<BatSegmentLink> passList = new BulkObservableCollection<BatSegmentLink>();
            foreach (var bat in bats)
            {
                BatSegmentLink pass = new BatSegmentLink();
                pass.Bat = bat;
                pass.NumberOfPasses = passes;
                passList.Add(pass);
            }
            return (passList);
        }*/

        /// <summary>
        ///     Determines whether [is manual file line] [the specified line].
        /// </summary>
        /// <param name="line">
        ///     The line.
        /// </param>
        /// <returns>
        /// </returns>
        private static Match IsManualFileLine(string line)
        {
            //string regexLabelFileLine = @"\A((\d*'?\s*\d*\.?\d*)|START)\s*-\s*((\d*'?\s*\d*\.?\d*)|END)\s*.*";
            string regexLabelFileLine = "([0-9.'\"]+)([\\s\t-]+)([0-9.'\"]+)\\s+(.*)";
            Match match = Regex.Match(line, regexLabelFileLine);
            if (match == null || match.Groups.Count < 5)
            {
                match = null;
            }

            if (match != null && match.Success) { return (match); }
            return (null);
        }

        /// <summary>
        ///     Processes the label file line.
        /// </summary>
        /// <param name="line">
        ///     The line.
        /// </param>
        /// <param name="startStr">
        ///     The start string.
        /// </param>
        /// <param name="endStr">
        ///     The end string.
        /// </param>
        /// <param name="comment">
        ///     The comment.
        /// </param>
        /// <param name="bats">
        ///     The bats.
        /// </param>
        /// <returns>
        /// </returns>
        private static string ProcessLabelFileLine(string line, string startStr, string endStr, string comment, out BulkObservableCollection<Bat> bats, ref Dictionary<string, BatStats> BatsFound)
        {
            string result = "";
            TimeSpan NewDuration;
            bats = new BulkObservableCollection<Bat>();

            if (!string.IsNullOrWhiteSpace(line) && char.IsDigit(line[0]))
            {
                result = ProcessLabelLine(line, startStr, endStr, comment, out NewDuration) + "\n";
                bats = AddToBatSummary(line, NewDuration, ref BatsFound);
            }
            else
            {
                result = line + "\n";
            }

            return (result);
        }

        /// <summary>
        ///     Processes the label line.
        /// </summary>
        /// <param name="line">
        ///     The line.
        /// </param>
        /// <param name="startStr">
        ///     The start string.
        /// </param>
        /// <param name="endStr">
        ///     The end string.
        /// </param>
        /// <param name="comment">
        ///     The comment.
        /// </param>
        /// <param name="NewDuration">
        ///     The new duration.
        /// </param>
        /// <returns>
        /// </returns>
        private static string ProcessLabelLine(string line, string startStr, string endStr, string comment, out TimeSpan NewDuration)
        {
            NewDuration = new TimeSpan(0L);
            line = line.Trim();
            if (!Char.IsDigit(line[0]))
            {
                return (line);
            }
            String outLine = "";
            TimeSpan StartTime;
            TimeSpan EndTime;
            TimeSpan duration;
            string shortened = line;

            /*Regex regexSeconds = new Regex(@"([0-9]+\.[0-9]+)\s*-*\s*([0-9]+\.[0-9]+)\s*(.*)");
            Match match = Regex.Match(line, @"([0-9]+\.[0-9]+)\s*-*\s*([0-9]+\.[0-9]+)\s*(.*)");
            //MatchCollection allMatches = regexSeconds.Matches(line);
            if (match.Success)
            {*/
            double startTimeSeconds;
            double endTimeSeconds;
            Double.TryParse(startStr, out startTimeSeconds);
            Double.TryParse(endStr, out endTimeSeconds);

            int Minutes = (int)Math.Floor(startTimeSeconds / 60);
            int Seconds = (int)Math.Floor(startTimeSeconds - (Minutes * 60));
            int Milliseconds = (int)Math.Floor(1000 * (startTimeSeconds - Math.Floor(startTimeSeconds)));
            StartTime = new TimeSpan(0, 0, Minutes, Seconds, Milliseconds);
            Minutes = (int)Math.Floor(endTimeSeconds / 60);
            Seconds = (int)Math.Floor(endTimeSeconds - (Minutes * 60));
            Milliseconds = (int)Math.Floor(1000 * (endTimeSeconds - Math.Floor(endTimeSeconds)));
            EndTime = new TimeSpan(0, 0, Minutes, Seconds, Milliseconds);

            duration = EndTime - StartTime;
            NewDuration = duration;
            shortened = comment;

            outLine = Tools.FormattedTimeSpan(StartTime) + " - " + Tools.FormattedTimeSpan(EndTime) + " = " +
                Tools.FormattedTimeSpan(duration) + "\t" + shortened;
            //outLine = String.Format("{0:00}\'{1:00}.{2:0##} - {3:00}\'{4:00}.{5:0##} = {6:00}\'{7:00}.{8:0##}\t{9}",
            //StartTime.Minutes, StartTime.Seconds, StartTime.Milliseconds,
            //EndTime.Minutes, EndTime.Seconds, EndTime.Milliseconds,
            //duration.Minutes, duration.Seconds, duration.Milliseconds, shortened);
            /*
            outLine = StartTime.Minutes + @"'" + StartTime.Seconds + "." + StartTime.Milliseconds +
            " - " + EndTime.Minutes + @"'" + EndTime.Seconds + "." + EndTime.Milliseconds +
            " = " + duration.Minutes + @"'" + duration.Seconds + "." + duration.Milliseconds +
            "\t" + shortened;*/
            /* }
             else
             {
                 StartTime = new TimeSpan();
                 EndTime = new TimeSpan();
                 outLine = line;
             }*/

            return (outLine);
        }

        /// <summary>
        /// Re-processes the specified label file, updating the Labelled segments in the
        /// database with new ones derived from the specified file.
        /// </summary>
        /// <param name="recording"></param>
        /// <param name="labelFileName"></param>
        internal void UpdateRecording(Recording recording, string labelFileName)
        {
            Dictionary<string, BatStats> BatsFound = new Dictionary<string, BatStats>();
            ProcessLabelOrManualFile(labelFileName, new GpxHandler(recording.RecordingSession.Location), recording.RecordingSession.Id, ref BatsFound);
        }

        /// <summary>
        ///     Processes a text file with a simple .txt extension that has been generated as an
        ///     Audacity LabelTrack. The fileName will be added to the output at the start of the OutputString.
        ///     Mod 22/3/2017 allow the use of txt files from Audacity 2.1.3 which may include spectral info
        ///     in the label on a second line starting with a '\'
        /// </summary>
        /// <param name="fileName">
        ///     Name of the file.
        /// </param>
        /// <param name="gpxHandler">
        ///     The GPX handler.
        /// </param>
        /// <param name="CurrentRecordingSessionId">
        ///     The current recording session identifier.
        /// </param>
        /// <returns>
        /// </returns>
        private static String ProcessLabelOrManualFile(string fileName, GpxHandler gpxHandler, int CurrentRecordingSessionId, ref Dictionary<string, BatStats> BatsFound)
        {
            BatsFound = new Dictionary<string, BatStats>();
            Recording recording = new Recording();
            if (CurrentRecordingSessionId <= 0)
            {
                MessageBox.Show("No session identified for these recordings", "ProcessLabelOrManualFile");
                Tools.ErrorLog("No session defined for recordings");
                return ("");
            }
            recording.RecordingSessionId = CurrentRecordingSessionId;

            return (ProcessLabelOrManualFile(fileName, gpxHandler, CurrentRecordingSessionId, recording, ref BatsFound));
        }

        private static string ProcessLabelOrManualFile(string fileName, GpxHandler gpxHandler, int CurrentRecordingSessionId, Recording recording, ref Dictionary<string, BatStats> BatsFound)
        {
            BulkObservableCollection<SegmentAndBatList> ListOfsegmentAndBatLists = new BulkObservableCollection<SegmentAndBatList>();
            string OutputString = "";

            MODE mode = MODE.PROCESS;
            TimeSpan duration = new TimeSpan();

            BatsFound = new Dictionary<string, BatStats>();

            DateTime fileStart;
            DateTime fileEnd;
            WavFileMetaData wfmd = null;

            try
            {
                if (File.Exists(fileName))
                {
                    string wavfile = fileName.Substring(0, fileName.Length - 4) + ".wav";
                    duration = GetFileDuration(fileName, out wavfile, out fileStart, out fileEnd);
                    if (File.Exists(wavfile))
                    {
                        wfmd = new WavFileMetaData(wavfile);
                        //Guano GuanoData=new Guano(Guano.GetGuanoData(CurrentRecordingSessionId, wavfile));
                        if (wfmd != null && wfmd.m_Duration != null)
                        {
                            duration = wfmd.m_Duration.Value;
                            if (fileEnd > fileStart + duration)
                            {
                                fileStart = fileEnd - duration;
                            }
                            else
                            {
                                fileEnd = fileStart + duration;
                            }

                            recording.RecordingNotes = wfmd.FormattedText();

                        }
                    }
                    recording.RecordingStartTime = fileStart.TimeOfDay;
                    recording.RecordingEndTime = fileEnd.TimeOfDay;
                    recording.RecordingDate = Tools.GetDateFromFilename(fileName);
                    OutputString = fileName;
                    if (!string.IsNullOrWhiteSpace(wavfile))
                    {
                        OutputString = wavfile;
                        recording.RecordingName = wavfile.Substring(wavfile.LastIndexOf('\\'));
                    }
                    if (duration.Ticks > 0L)
                    {
                        OutputString = OutputString + " \t" + duration.Minutes + "m" + duration.Seconds + "s";
                    }
                    OutputString = OutputString + "\n";
                    BulkObservableCollection<decimal> gpsLocation = gpxHandler.GetLocation(fileStart);
                    if (gpsLocation != null && gpsLocation.Count() == 2)
                    {
                        OutputString = OutputString + gpsLocation[0] + ", " + gpsLocation[1];
                        recording.RecordingGPSLatitude = gpsLocation[0].ToString();
                        recording.RecordingGPSLongitude = gpsLocation[1].ToString();
                        if (recording.RecordingSession != null)
                        {
                            if (recording.RecordingSession.LocationGPSLatitude == null || recording.RecordingSession.LocationGPSLatitude < 5.0m)
                            {
                                recording.RecordingSession.LocationGPSLatitude = gpsLocation[0];
                                recording.RecordingSession.LocationGPSLongitude = gpsLocation[1];
                            }
                        }
                    }
                    gpsLocation = gpxHandler.GetLocation(fileEnd);
                    if (gpsLocation != null && gpsLocation.Count() == 2)
                    {
                        OutputString = OutputString + " => " + gpsLocation[0] + ", " + gpsLocation[1] + "\n";
                    }
                    if (String.IsNullOrWhiteSpace(recording.RecordingGPSLatitude))
                    {
                        try
                        {
                            if (wfmd != null && wfmd.m_Location != null && wfmd.m_Location.m_Latitude<200.0d && wfmd.m_Location.m_Longitude<200.0d)
                            {

                                var location = new Tuple<double, double>(wfmd.m_Location.m_Latitude, wfmd.m_Location.m_Longitude);
                                recording.RecordingGPSLatitude = location.Item1.ToString();
                                recording.RecordingGPSLongitude = location.Item2.ToString();
                                if (recording.RecordingSession != null &&
                                    (recording.RecordingSession.LocationGPSLatitude == null || recording.RecordingSession.LocationGPSLatitude < 5.0m))
                                {
                                    recording.RecordingSession.LocationGPSLatitude = (Decimal)location.Item1;
                                    recording.RecordingSession.LocationGPSLongitude = (Decimal)location.Item2;
                                }
                            }
                        }
                        catch (NullReferenceException nex)
                        {
                            Tools.ErrorLog(nex.Message);
                            Debug.WriteLine("*** ProcessLabelOrManualFile:-LocationData:-" + nex.Message);
                        }
                    }

                    if (fileName.ToUpper().EndsWith(".TXT"))
                    {
                        OutputString = OutputString + ProcessTextFile(fileName, duration, ref ListOfsegmentAndBatLists, mode, ref BatsFound);
                    }
                    else if (fileName.ToUpper().EndsWith(".WAV"))
                    {
                        string comment = ProcessWavFile(fileName, duration, ref ListOfsegmentAndBatLists, mode, ref BatsFound);
                        recording.RecordingNotes = recording.RecordingNotes + " " + comment;
                        OutputString = OutputString + comment;
                    }
                }
            }
            catch (Exception ex)
            {
                Tools.ErrorLog(ex.Message);
                Debug.WriteLine("Error Processing File <" + fileName + ">: " + ex.Message);
            }

            if (!String.IsNullOrWhiteSpace(OutputString) && !BatsFound.IsNullOrEmpty())
            {
                foreach (var bat in BatsFound)
                {
                    bat.Value.batCommonName = bat.Key;
                    OutputString = OutputString + "\n" + Tools.GetFormattedBatStats(bat.Value, true);
                }
            }

            if (!ListOfsegmentAndBatLists.IsNullOrEmpty())
            {
                for (int i = ListOfsegmentAndBatLists.Count - 1; i >= 0; i--)
                {
                    if (String.IsNullOrWhiteSpace(ListOfsegmentAndBatLists[i].segment.Comment))
                    {
                        ListOfsegmentAndBatLists.RemoveAt(i);
                    }
                }
                DBAccess.UpdateRecording(recording, ListOfsegmentAndBatLists, null);
            }

            return (OutputString);
        }

        private static string ProcessTextFile(string fileName, TimeSpan duration, ref BulkObservableCollection<SegmentAndBatList> ListOfsegmentAndBatLists, MODE mode, ref Dictionary<string, BatStats> BatsFound)
        {
            string[] allLines = new string[1];
            String OutputString = "";

            try
            {
                if (fileName.ToUpper().EndsWith(".TXT"))
                {
                    allLines = File.ReadAllLines(fileName);
                    if (allLines.Count() == 0 || String.IsNullOrWhiteSpace(allLines[0]))
                    {
                        allLines = new[] { "Start - End \t No Bats" };
                    }
                }
            }
            catch (Exception ex) { Debug.WriteLine(ex); }
            OutputString = ProcessText(allLines, duration, ref ListOfsegmentAndBatLists, mode, ref BatsFound);
            return (OutputString);
        }

        private static string ProcessWavFile(string fileName, TimeSpan duration, ref BulkObservableCollection<SegmentAndBatList> ListOfsegmentAndBatLists, MODE mode, ref Dictionary<string, BatStats> BatsFound)
        {
            string[] allLines = new string[1];
            String OutputString = "";
            WavFileMetaData wfmd;
            string line = "";

            try
            {
                wfmd = new WavFileMetaData(fileName);
                if (wfmd != null)
                {
                    
                    if (wfmd.m_Duration != null)
                    {
                        line = "0 - " + wfmd.m_Duration.Value.TotalSeconds;
                        duration = wfmd.m_Duration.Value;
                    }
                    if (!String.IsNullOrWhiteSpace(wfmd.m_ManualID))
                    {
                        line += " " + wfmd.m_ManualID;
                    }
                    if (!String.IsNullOrWhiteSpace(wfmd.m_AutoID))
                    {
                        line += ", " + wfmd.m_AutoID;
                    }
                    if (!String.IsNullOrWhiteSpace(wfmd.m_Note))
                    {
                        line += ", " + wfmd.m_Note;
                    }
                }
                allLines[0] = line;
            }
            catch (Exception ex) { Tools.ErrorLog(ex.Message); Debug.WriteLine(ex); }
            OutputString = ProcessText(allLines, duration, ref ListOfsegmentAndBatLists, mode, ref BatsFound);
            return (OutputString);
        }

        private static string ProcessText(string[] allLines, TimeSpan duration, ref BulkObservableCollection<SegmentAndBatList> ListOfsegmentAndBatLists, MODE mode, ref Dictionary<string, BatStats> BatsFound)
        {
            String OutputString = "";
            BulkObservableCollection<string> linesToMerge = new BulkObservableCollection<string>();

            Match match = null;

            if (allLines.Count() > 1 && allLines[0].StartsWith("["))
            {
                if (allLines[0].ToUpper().StartsWith("[SKIP]") || allLines[0].ToUpper().StartsWith("[LOG]"))
                {
                    mode = MODE.COPY;
                    return ("");
                }
                if (allLines[0].ToUpper().StartsWith("[COPY]"))
                {
                    mode = MODE.COPY;
                    OutputString = "";
                    foreach (var line in allLines)
                    {
                        if (line.Contains("[MERGE]"))
                        {
                            mode = MODE.MERGE;

                            linesToMerge = new BulkObservableCollection<String>();
                        }
                        if (!line.Contains("[COPY]") && !line.Contains("[MERGE]"))
                        {
                            if (mode == MODE.MERGE)
                            {
                                linesToMerge.Add(line);
                            }
                            else
                            {
                                OutputString = OutputString + line + "\n";
                            }
                        }
                    }
                    return (OutputString);
                }
            }
            if (!allLines.IsNullOrEmpty())
            {
                if (!linesToMerge.IsNullOrEmpty())
                {
                    OutputString = OutputString + linesToMerge[0] + "\n";
                    linesToMerge.Remove(linesToMerge[0]);
                }
                for (int ln = 0; ln < allLines.Count(); ln++)
                {
                    string line = allLines[ln];
                    if ((ln + 1) < allLines.Count() && allLines[ln + 1].StartsWith(@"\"))
                    {
                        string spectralParams = allLines[ln + 1];
                        ln++;

                        line = AddSpectralParametersToLine(line, spectralParams);
                    }
                    if (!String.IsNullOrWhiteSpace(line))
                    {
                        string modline = Regex.Replace(line, @"[Ss][Tt][Aa][Rr][Tt]", "0.0");
                        modline = Regex.Replace(modline, @"[Ee][Nn][Dd]", ((decimal)(duration.TotalSeconds)).ToString());
                        string processedLine = "";
                        BulkObservableCollection<Bat> bats = new BulkObservableCollection<Bat>();
                        string startStr, endStr, comment;
                        if (FileProcessor.IsLabelFileLine(modline, out startStr, out endStr, out comment))
                        {
                            processedLine = ProcessLabelFileLine(modline, startStr, endStr, comment, out bats, ref BatsFound);
                        }
                        else if ((match = IsManualFileLine(modline)) != null)
                        {
                            processedLine = ProcessManualFileLine(match, out bats, ref BatsFound);
                        }
                        else
                        {
                            processedLine = line + "\n";
                        }
                        ListOfsegmentAndBatLists.Add(SegmentAndBatList.ProcessLabelledSegment(processedLine, bats) ?? new SegmentAndBatList());
                        // one added for each line that is processed as a segment label
                        OutputString = OutputString + processedLine;
                    }
                }
            }
            return (OutputString);
        }

        /// <summary>
        /// When reading a line from an Audacity Label file, since Audacity 2.1.3
        /// the label may include a second line starting with a '\' containing the
        /// upper and lower frequencies of the selection when the label was added.
        /// This function is passed as parameters the label text line and the second line
        /// starting with the '\'.
        /// If the label includes the string "{}" the selection parameters are ignored.
        /// If the label does not include a parameter section in {} then the frequency
        /// parameters are added as start and end frequencies.
        /// If the label includes a parameters section which includes s= or e= then the
        /// selection parameters are ignored.
        /// If the label includes a parameters section which starts with a number and includes
        /// a comma then it is assumed to be an implicit parameter section and the selection
        /// parameters are ignored.
        /// Otherwise the selection parameters are trimmed to two decimal places and inserted
        /// as {s=high,end=low}
        ///
        /// </summary>
        /// <param name="line"></param>
        /// <param name="spectralParams"></param>
        /// <returns></returns>
        private static string AddSpectralParametersToLine(string line, string spectralParams)
        {
            if (line.Contains(@"{}"))
            {
                return (line.Replace("{}", ""));
            }

            double fmax = -1.0d;
            double fmin = -1.0d;
            if (spectralParams.StartsWith(@"\"))
            {
                spectralParams = spectralParams.Substring(1);
                var freqs = spectralParams.Split('\t');
                int maxparam = 0;

                if (freqs.Count() > 2)
                {
                    maxparam = 2;
                }
                else if (freqs.Count() > 1)
                {
                    maxparam = 1;
                }
                if (maxparam > 0)
                {
                    double.TryParse(freqs[maxparam - 1], out fmin);
                    double.TryParse(freqs[maxparam], out fmax);
                    if (fmax < fmin)
                    {
                        double temp = fmin;
                        fmin = fmax;
                        fmax = temp;
                    }
                }
            }
            if (fmin < 0.0d)
            {
                return (line);
            }

            if (line.Contains("{"))
            {
                var parts = line.Split('{');
                if (parts.Count() > 1)
                {
                    if (parts[1].StartsWith("{"))
                    {
                        parts[1] = parts[1].Substring(1);
                    }

                    if (parts[1].Contains("s=") || parts[1].Contains("e="))
                    {
                        return (line);
                    }

                    if (char.IsDigit(parts[1].Trim()[0]) && parts[1].Contains(","))
                    {
                        return (line);
                    }

                    line = String.Format("{0}s={1:F2},e={2:F2},{3}", parts[0] + "{", fmax, fmin, parts[1]);
                }
            }
            else
            {
                line = string.Format("{0}s={1:F2},e={2:F2}{3}", line + " {", fmax, fmin, "}");
            }

            return (line);
        }

        /*       /// <summary>
               /// using a string that matches the regex @"[0-9]+\.[0-9]+" or a string that matches
               /// the regex @"[0-9]+'?[0-9]*\.?[0-9]+" extracts one to three numeric portions and
               /// converts them to a timespan. 3 number represent minute,seconds,fraction 2 numbers
               /// represent seconds,fraction or minutes,seconds 1 number represents minutes or
               /// seconds </summary> <param name="match">The match.</param> <returns></returns>
               private static TimeSpan GetTimeOffset(Match match)
               {
                   return (FileProcessor.GetTimeOffset(match.Value));
               }*/
    }
}