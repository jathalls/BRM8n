using Microsoft.Maps.MapControl.WPF;
using Microsoft.VisualStudio.Language.Intellisense;
using System;
using System.Collections.Generic;
using System.Data.Linq;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Forms;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using WindowsInput;
using WindowsInput.Native;

namespace BatRecordingManager
{
    /// <summary>
    ///     Class of miscellaneous, multi access functions - all static for ease of re-use
    /// </summary>
    public static class Tools
    {
#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member

        /// <summary>
        /// BlobTypes are used to identify the type of binary data object stored in the database.
        /// The enum types are 3 or 4 char strings that are stored as string literals in the database
        /// but the enum allows simple internal handling.  The enum is converted to a string to be
        /// stored in the database and is converted back to an enum on retrieval.  enum names must be limited
        /// to 4 chars to fit into the database type field.
        /// BMP is a raw bitmap
        /// BMPS is a BitmapSource object
        /// WAV is a snippet of waveform read from a .wav file.
        /// </summary>
        public enum BlobType { NONE = 0, ANY = 1, BMP, BMPS, WAV, PNG };

#pragma warning restore CS1591 // Missing XML comment for publicly visible type or member

        /// <summary>
        ///     Formats the time span. Given a Timespan returns a formatted string as mm'ss.sss" or 23h59'58.765"
        /// </summary>
        /// <param name="time">
        ///     The time.
        /// </param>
        /// <returns>
        /// </returns>
        public static string FormattedTimeSpan(TimeSpan time)
        {
            string result = "";
            if (time != null && time.Ticks >= 0)
            {
                time = time.Duration();
                if (time.Hours > 0)
                {
                    result = result + time.Hours + "h";
                }
                if (time.Hours > 0 || time.Minutes > 0)
                {
                    result = result + time.Minutes + "'";
                }
                decimal seconds = time.Seconds + ((decimal)time.Milliseconds / 1000.0m);
                result = result + string.Format("{0:0.0#}\"", seconds);
            }

            return (result);
        }

        /// <summary>
        /// Extension method for IEnumerable(T) to check if the list is null or empty
        /// before committing to a foreach
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="list"></param>
        /// <returns></returns>
        public static bool IsNullOrEmpty<T>(this IEnumerable<T> list)
        {
            return (!(list?.Any() ?? false));
        }

        /// <summary>
        /// Clears all the children of this canvas except children of type Grid
        /// </summary>
        /// <param name="canvas"></param>
        public static void ClearExceptGrids(this Canvas canvas)
        {
            if (canvas.Children != null && canvas.Children.Count > 0)
            {
                List<UIElement> elementsToRemove = new List<UIElement>();
                foreach (var child in canvas.Children)
                {
                    if (!((child is Canvas) || (child is Grid)))
                    {
                        elementsToRemove.Add((UIElement)child);
                    }
                }
                if (!elementsToRemove.IsNullOrEmpty())
                {
                    foreach (var element in elementsToRemove)
                    {
                        canvas.Children.Remove(element);
                    }
                }
            }
        }

        /// <summary>
        /// Finds a descendant of the object
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="obj"></param>
        /// <returns></returns>
        public static T FindDescendant<T>(DependencyObject obj) where T : DependencyObject

        {
            // Check if this object is the specified type
            if (obj is T)
                return obj as T;

            // Check for children
            int childrenCount = VisualTreeHelper.GetChildrenCount(obj);
            if (childrenCount < 1)
                return null;

            // First check all the children
            for (int i = 0; i < childrenCount; i++)
            {
                DependencyObject child = VisualTreeHelper.GetChild(obj, i);
                if (child is T)
                    return child as T;
            }

            // Then check the childrens children
            for (int i = 0; i < childrenCount; i++)
            {
                DependencyObject child = FindDescendant<T>(VisualTreeHelper.GetChild(obj, i));
                if (child != null && child is T)
                    return child as T;
            }

            return null;
        }

#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member

        public static T FindParent<T>(DependencyObject child) where T : DependencyObject
#pragma warning restore CS1591 // Missing XML comment for publicly visible type or member
        {
            // get parent item
            DependencyObject parentObject = VisualTreeHelper.GetParent(child);

            //we've reached the end of the tree
            if (parentObject == null) return (null);

            //check if the parent matches the type we want
            if (parentObject is T parent)
            {
                return (parent);
            }
            else
            {
                return (FindParent<T>(parentObject));
            }
        }

        /// <summary>
        /// looks in a string for the sequence .wav and truncates the string after that
        /// then removes leading charachters up to the last \ to remove any path and pre-amble.
        /// This should leave just the filename.wav unless there was textual preamble to the filename
        /// which cannot be distinguished from part of the name.
        /// returns null if no such string is found
        /// </summary>
        /// <param name="description"></param>
        /// <returns></returns>
        internal static string ExtractWAVFilename(string description)
        {
            if (description.ToUpper().Contains(".WAV"))
            {
                string fullname = description.Substring(0, description.ToUpper().IndexOf(".WAV") + 4);
                if (fullname.Contains(@"\"))
                {
                    fullname = fullname.Substring(description.LastIndexOf(@"\")+1);
                }
                return (fullname);
            }
            else
            {
                return (null);
            }
        }

        /// <summary>
        /// Uses the recording start and end times to calculate the recording duration
        /// and if the end time is earlier than the start time adds 1 day to the end time
        /// to allow for recordings starting before midnight and ending after midnight.
        /// Does not mak allowance for multi-day recordings.
        /// </summary>
        /// <param name="recording"></param>
        /// <returns></returns>
        public static TimeSpan GetRecordingDuration(Recording recording)
        {
            TimeSpan dur = new TimeSpan();
            if (recording.RecordingEndTime != null && recording.RecordingStartTime != null)
            {
                if (recording.RecordingEndTime.Value > recording.RecordingStartTime.Value)
                {
                    dur = recording.RecordingEndTime.Value - recording.RecordingStartTime.Value;
                }
                else
                {
                    DateTime start = new DateTime() + recording.RecordingStartTime.Value;
                    DateTime end = new DateTime().AddDays(1) + recording.RecordingEndTime.Value;
                    dur = end - start;
                }
            }
            return (dur);
        }




        

        /// <summary>
        ///     To the formatted string.
        /// </summary>
        /// <param name="session">
        ///     The session.
        /// </param>
        /// <returns>
        /// </returns>
        public static String ToFormattedString(this RecordingSession session)
        {
            String result = "";
            result += session.SessionTag + "\n";
            result += session.Location + "\n";
            result += session.SessionDate.ToShortDateString() + " " +
                session.SessionStartTime ?? "" + " - " +
                (session.EndDate ?? session.SessionDate).ToShortDateString() + " " +
                session.SessionEndTime ?? "" + "\n";
            result += session.Operator ?? "" + "\n";
            result += (session.LocationGPSLatitude ?? 0.0m) + ", " + (session.LocationGPSLongitude ?? 0.0m) + "\n";
            result += session.Equipment ?? "" + "\n";
            result += session.Microphone ?? "" + "\n";
            result += session.SessionNotes ?? "" + "\n";
            result += "==================================================================\n";

            return (result);
        }

        private static bool hasErred = false;
#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member

        public static void ErrorLog(string error)
#pragma warning restore CS1591 // Missing XML comment for publicly visible type or member
        {
            string path = @"C:\BRM-Error\";
            string errorFile = path + "BRM-Error-Log.txt";
            try
            {
                var Build = System.Reflection.Assembly.GetExecutingAssembly().GetName().Version.ToString();

                if (!Directory.Exists(path))
                {
                    Directory.CreateDirectory(path);
                }
                using (var stream = new StreamWriter(errorFile, true))
                {
                    if (!hasErred)
                    {
                        stream.WriteLine("\n" + DateTime.Now.ToString() + "Bat Recording Manager v" + Build + "\n");
                    }
                    StackTrace stackTrace = new StackTrace();
                    string caller = stackTrace.GetFrame(1).GetMethod().Name;
                    stream.WriteLine(DateTime.Now.ToShortDateString() + " " + DateTime.Now.ToShortTimeString() +"["+caller+"] :- " + error);
                    Debug.WriteLine("ERROR:- in " + caller + ":- " + error);
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine("\n\n**** Error writing to error log" + ex);
                File.AppendAllText(path + "FatalError.txt", "Error writing to Log file!!!!!!!!!!!\n");
            }
        }

        /// <summary>
        /// Writes an information string to the Error Log without the additional burden of a stack trace
        /// </summary>
        /// <param name="v"></param>
        internal static void InfoLog(string error)
        {
            string path = @"C:\BRM-Error\";
            string errorFile = path + "BRM-Error-Log.txt";
            try
            {
                var Build = System.Reflection.Assembly.GetExecutingAssembly().GetName().Version.ToString();

                if (!Directory.Exists(path))
                {
                    Directory.CreateDirectory(path);
                }

                using (var stream = new StreamWriter(errorFile, true))
                {
                    stream.WriteLine(DateTime.Now.ToShortDateString() + " " + DateTime.Now.ToShortTimeString() + ":-" + error);
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine("\n\n**** Error writing to info log" + ex);
                File.AppendAllText(path + "FatalError.txt", "Error writing to Info Log file!!!!!!!!!!!\n");
            }
        }

        /// <summary>
        ///     Converts the double in seconds to time span.
        /// </summary>
        /// <param name="value">
        ///     The value.
        /// </param>
        /// <returns>
        /// </returns>
        internal static TimeSpan ConvertDoubleToTimeSpan(double? value)
        {
            if (value == null) return new TimeSpan();
            int seconds = (int)Math.Floor(value.Value);
            int millis = (int)Math.Round((value.Value - seconds) * 1000.0d);

            int minutes = Math.DivRem(seconds, 60, out seconds);
            return (new TimeSpan(0, 0, minutes, seconds, millis));
        }

        /// <summary>
        ///     Given a valid Segment, generates a formatted string in the format mm'ss.ss" - mm'ss.ss"
        ///     = mm'ss.ss" comment
        /// </summary>
        /// <param name="segment">
        ///     The segment.
        /// </param>
        /// <returns>
        /// </returns>
        /// <exception cref="System.NotImplementedException">
        /// </exception>
        internal static string FormattedSegmentLine(LabelledSegment segment)
        {
            if (segment == null) return ("");
            String result = Tools.FormattedTimeSpan(segment.StartOffset) + " - " +
                Tools.FormattedTimeSpan(segment.EndOffset) + " = " +
                Tools.FormattedTimeSpan(segment.EndOffset - segment.StartOffset) + "; " +
                segment.Comment;
            var calls = DBAccess.GetCallParametersForSegment(segment);
            if (calls != null && calls.Count() > 0)
            {
                foreach (var call in calls)
                {
                    result = result + "\n    " + "sf=" + FormattedValuePair(call.StartFrequency, call.StartFrequencyVariation) +
                        ", ef=" + FormattedValuePair(call.EndFrequency, call.EndFrequencyVariation) +
                        ", pf=" + FormattedValuePair(call.PeakFrequency, call.PeakFrequencyVariation) +
                        ", durn=" + FormattedValuePair(call.PulseDuration, call.PulseDurationVariation) +
                        ", int=" + FormattedValuePair(call.PulseInterval, call.PulseIntervalVariation);
                    if (!String.IsNullOrWhiteSpace(call.CallType)) result = result + ", type=" + call.CallType;
                    if (!String.IsNullOrWhiteSpace(call.CallFunction)) result = result + ", fnctn=" + call.CallFunction;
                    if (!String.IsNullOrWhiteSpace(call.CallNotes)) result = result + "\n    " + call.CallNotes;
                }
            }
            return (result);
        }

        internal static string FormattedValuePair(double? value, double? variation)
        {
            string result = "";
            if (value == null || value <= 0.0d)
            {
                return ("");
            }
            result = string.Format("{0:##0.0}", value);
            if (variation != null && variation >= 0.0)
            {
                result = result + "+/-" + string.Format("{0:##0.0}", variation);
            }

            return (result);
        }

        internal static string GetFormattedBatStats(BatStats value, bool showNoBats)
        {
            string result = "";
            if (value == null) return (result);

            if (value.batCommonName.ToUpper() == "NO BATS" || value.batCommonName.ToUpper() == "NOBATS")
            {
                if (showNoBats)
                {
                    return ("No Bats");
                }
                else
                {
                    return ("");
                }
            }
            if (value.passes > 0 || value.segments > 0)
            {
                result = value.batCommonName + " " + value.passes + (value.passes == 1 ? " pass in " : " passes in ") +
                    value.segments + " segment" + (value.segments != 1 ? "s" : "") +

                            " = ( " +
                            "Min=" + Tools.FormattedTimeSpan(value.minDuration) +
                            ", Max=" + Tools.FormattedTimeSpan(value.maxDuration) +
                            ", Mean=" + Tools.FormattedTimeSpan(value.meanDuration) + " )" +
                            "Total duration=" + Tools.FormattedTimeSpan(value.totalDuration);
            }
            return (result);
        }

        internal static int GetNumberOfPassesForSegment(LabelledSegment segment)
        {
            BatStats stat = new BatStats();
            stat.Add(segment.EndOffset - segment.StartOffset);
            return (stat.passes);
        }

        internal static void OpenWavFile(Recording selectedRecording)
        {
            if (selectedRecording == null) return;
            if (selectedRecording.RecordingSession == null) return;
            String folder = selectedRecording.RecordingSession.OriginalFilePath;
            if (String.IsNullOrWhiteSpace(folder)) return;
            folder = folder.Trim();

            if (!Directory.Exists(folder))
            {// try to find the folder on a different drive if necessary
                if (folder[1] == ':')
                {// then the folder name starts with a drive letter - almost definite
                    string drivelessFolder = folder.Substring(2);
                    if (drivelessFolder.StartsWith(@"\"))
                    {
                        drivelessFolder = drivelessFolder.Substring(1);
                    }
                    if (!drivelessFolder.EndsWith(@"\"))
                    {
                        drivelessFolder = drivelessFolder + @"\";
                    }

                    DriveInfo[] allDrives = DriveInfo.GetDrives();
                    foreach (var drive in allDrives)
                    {
                        if (Directory.Exists(drive.Name + drivelessFolder))
                        {
                            folder = drive.Name + drivelessFolder;
                            break;
                        }
                    }
                    if (folder[1] != ':') return; // we didn't find a drive with the folder path so give up
                }
            }
            if (selectedRecording.RecordingName.StartsWith(@"\"))
            {
                selectedRecording.RecordingName = selectedRecording.RecordingName.Substring(1);
            }
            folder = folder +@"\"+ selectedRecording.RecordingName;
            Tools.OpenWavFile(folder);
        }

        /// <summary>
        /// Given a recording Session, returns a list of strings each of which contains a summary
        /// of number and duration od passes for a specific type of bat for that session.
        /// </summary>
        /// <param name="recordingSession"></param>
        /// <returns></returns>
        internal static List<string> GetSessionSummary(RecordingSession session)
        {
            List<string> result = new List<string>();
            var statsForSession = session.GetStats();
            statsForSession = CondenseStatsList(statsForSession);
            foreach (var batStat in statsForSession)
            {
                string summary = Tools.GetFormattedBatStats(batStat, false);
                if (!string.IsNullOrWhiteSpace(summary))
                {
                    result.Add(summary);
                }
            }
            return (result);
        }

        /// <summary>
        ///     Condenses the stats list. Given a List of BatStats for a wide collection of bats and
        ///     passes, condenses it to have a single BatStat for each bat type along with the
        ///     cumulative number of passes and segments.
        /// </summary>
        /// <param name="statsForSession">
        ///     The stats for session.
        /// </param>
        /// <returns>
        ///     </returns>
        /// <exception cref="System.NotImplementedException">
        ///     </exception>
        internal static BulkObservableCollection<BatStats> CondenseStatsList(BulkObservableCollection<BatStats> statsForSession)
        {
            BulkObservableCollection<BatStats> result = new BulkObservableCollection<BatStats>();
            foreach (var stat in statsForSession)
            {
                var matchingStats = from s in result
                                    where s.batCommonName == stat.batCommonName
                                    select s;
                if (matchingStats != null && matchingStats.Count() > 0)
                {
                    var existingStat = matchingStats.First();
                    existingStat.Add(stat);
                }
                else
                {
                    result.Add(stat);
                }
            }
            return (result);
        }

        /// <summary>
        /// changes the folder icon for the specified folder to a folder symbol with a green tick
        /// The change may or may not be apparent until a reboot
        /// </summary>
        /// <param name="workingFolder"></param>
        internal static void SetFolderIconTick(string workingFolder)
        {
            setFolderIcon(workingFolder, @"C:\Windows\system32\SHELL32.dll,144", "Data Imported to Bat Recording Manager");
            return;
            
        }


        [DllImport("user32.dll")]
        private static extern bool SetForegroundWindow(IntPtr hWnd);

        internal static void ActivateApp(string processName)
        {
            Process[] p = Process.GetProcessesByName(processName);

            // Activate the first application we find with this name
            if (p.Count() > 0)
                SetForegroundWindow(p[0].MainWindowHandle);
        }

        [DllImport("Shell32.dll", CharSet = CharSet.Auto)]
        private static extern UInt32 SHGetSetFolderCustomSettings(ref LPSHFOLDERCUSTOMSETTINGS pfcs, string pszPath, UInt32 dwReadWrite);

        /// <summary>
        /// Returns the size of the overlap between the time of the labelled segment and the period defined by the Tuple.  The
        /// Tuple is defined on the basis of noon-noon, so all times have 12hrs subtracted from them to normalise to midnight to
        /// midnight for the comparisons removing any complications due to days overlapping.
        /// </summary>
        /// <param name="seg"></param>
        /// <param name="samplePeriod"></param>
        /// <returns></returns>
        internal static int SegmentOverlap(LabelledSegment seg, Tuple<DateTime, DateTime> samplePeriod)
        {
            TimeSpan tDay = new TimeSpan(12, 0, 0);

            var normalisedSegmentStart = (seg.Recording.RecordingStartTime ?? new TimeSpan()) - tDay + seg.StartOffset;
            var normalisedSegmentEnd = (seg.Recording.RecordingStartTime ?? new TimeSpan()) - tDay + seg.EndOffset;

            var normalisedSampleStart = samplePeriod.Item1.TimeOfDay - tDay;
            var normalisedSampleEnd = samplePeriod.Item2.TimeOfDay - tDay;

            var overlap = (normalisedSegmentEnd < normalisedSampleEnd ? normalisedSegmentEnd : normalisedSampleEnd) -
                (normalisedSegmentStart > normalisedSampleStart ? normalisedSegmentStart : normalisedSampleStart);
            if (overlap.TotalMinutes < 0) overlap = new TimeSpan();
            return ((int)Math.Ceiling(overlap.TotalMinutes));
        }

        /// <summary>
        /// Using Reflection to force a sort on a column of a System.Windows.Controls.DataGrid
        /// </summary>
        /// <param name="dataGrid"></param>
        /// <param name="ColumnIndex"></param>
        public static void SortColumn(System.Windows.Controls.DataGrid dataGrid, int ColumnIndex)
        {
            var performSortMethod = typeof(System.Windows.Controls.DataGrid).GetMethod("PerformSort", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
            performSortMethod?.Invoke(dataGrid, new[] { dataGrid.Columns[ColumnIndex] });
        }

        internal static void OpenWavFile(String folder)
        {
            if (String.IsNullOrWhiteSpace(folder) || !File.Exists(folder)) { return; }
            //Process externalProcess = new Process();

            //externalProcess.StartInfo.FileName = folder;
            //externalProcess.StartInfo.Arguments = folder;
            //externalProcess.StartInfo.WindowStyle = ProcessWindowStyle.Normal;
            //externalProcess.Start();
            OpenWavAndTextFile(folder);
        }

        

        internal static Process OpenWavAndTextFile(string wavFile,Process ExternalProcess=null)
        {
            Debug.WriteLine("Selected wavFile=" + wavFile);
            wavFile=wavFile.Replace(@"\\", @"\");
            Debug.WriteLine("Corrected wavFile=" + wavFile);
            if (!File.Exists(wavFile))
            {
                Debug.WriteLine("Wav file does not exist");
                return (null);
            }
            //int sleep = 1000;
            if (ExternalProcess == null)
            {
                ExternalProcess = new Process();
                ExternalProcess.Exited += ExternalProcess_Exited;
                if (ExternalProcess == null) return (null);
            }
            DialogResult result = DialogResult.Retry;
            while (result == DialogResult.Retry)
            {
                System.Diagnostics.Process[] p = System.Diagnostics.Process.GetProcessesByName("audacity");

                if (p.Length > 0)
                {
                    result = System.Windows.Forms.MessageBox.Show("Please close open copies of Audacity first.", "Audacity Already Open", MessageBoxButtons.RetryCancel);
                }
                else
                {
                    break;
                }
            }

            if (result == DialogResult.Cancel)
            {
                ExternalProcess.Close();
                return(null);
            }
            string audacityFileLocation = FindAudacity();
            if (string.IsNullOrWhiteSpace(audacityFileLocation) || !File.Exists(audacityFileLocation))
            {
                ExternalProcess.StartInfo.FileName = wavFile;
            }
            else
            {
                ExternalProcess.StartInfo.FileName = audacityFileLocation;
                ExternalProcess.StartInfo.Arguments = "\""+wavFile+"\""; // enclosed in quotes in case the path has spaces
                   
            }
            //externalProcess.StartInfo.Arguments = folder;
            ExternalProcess.StartInfo.WindowStyle = ProcessWindowStyle.Maximized;
            
            bool started = ExternalProcess.Start();
            while (!ExternalProcess.Responding)
            {
                Thread.Sleep(100);
                Debug.Write("!");
            }
            Thread.Sleep(1000);
            ExternalProcess.EnableRaisingEvents = true;
            //ExternalProcess.Exited += ExternalProcess_Exited;

            //int startSeconds = (int)startOffset.TotalSeconds;
            //int endSeconds = (int)endOffset.TotalSeconds;
            //if (endSeconds == startSeconds) endSeconds = startSeconds + 1;
            try
            {
                System.Windows.Application.Current.MainWindow.Focus();
            }
            catch (System.InvalidOperationException) { }
            while (ExternalProcess.MainWindowHandle == (IntPtr)0L)
            {
                if (ExternalProcess.HasExited)
                {
                    ExternalProcess.Close();
                    return (null);
                }
            }
            if (!WaitForIdle(ExternalProcess, "!", "Starting")) return (null);
            IntPtr epHandle = ExternalProcess.MainWindowHandle;
            SetForegroundWindow(epHandle);
                      
            try
            {
                InputSimulator ipSim = new InputSimulator();

                //s.Keyboard.ModifiedKeyStroke(VirtualKeyCode.LMENU, VirtualKeyCode.VK_S);
                //SetForegroundWindow(epHandle);
                


                //ipSim.Keyboard.ModifiedKeyStroke(VirtualKeyCode.LSHIFT, VirtualKeyCode.UP);
                Debug.WriteLine(ExternalProcess.MainWindowTitle);
                ExternalProcess.WaitForInputIdle();
                //Thread.Sleep(sleep);

                //SetForegroundWindow(epHandle);
                // CTRL-SHIFT-N - select nothing, i.e. clear current selection
                ipSim.Keyboard.ModifiedKeyStroke(new[] { VirtualKeyCode.CONTROL, VirtualKeyCode.LSHIFT }, VirtualKeyCode.VK_N);

                if (!WaitForIdle(ExternalProcess)) return null;
                //Thread.Sleep(sleep);

                //ALT-F,I,L filename<ENTER> - open the matching label track
                String textFileName = Tools.GetMatchingTextFile(wavFile);
                Debug.WriteLine("Matches {" + wavFile + "} to {" + textFileName + "}");
                
                if (!string.IsNullOrWhiteSpace(textFileName) && File.Exists(textFileName))
                {
                    if(!OpenAudacityLabelFile(ExternalProcess, ipSim, textFileName))
                    {
                        return (null);
                    }
                }
                else
                {
                    if(!CreateAudacityLabelFile(ExternalProcess, ipSim, textFileName))
                    {
                        return (null);
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine("Error trying to open audacity with a .wav and a .txt:- " + ex.Message);
                Tools.ErrorLog("Error trying to open .wav and .txt file in Audacity:-" + ex.Message);
            }
            return (ExternalProcess);
        }

        /// <summary>
        /// Given a Process in which Audacity is running, and an Input Simulator, sends keyboard commands to 
        /// Audacity to create a new label track with the name of the text file after removing the extension.
        /// CTRL-SHIFT-N    Clear selection
        /// CTRL-SHIFT-B    Create Label Track
        /// CTRL-SHIFT-M    Open Track menu
        /// N               Select Name from Menu
        /// trackname       Enter the new track namw
        /// RETURN          Accept the name and close the dialog
        /// </summary>
        /// <param name="ExternalProcess"></param>
        /// <param name="ipSim"></param>
        /// <param name="textFileName"></param>
        /// <returns></returns>
        private static bool CreateAudacityLabelFile(Process ExternalProcess, InputSimulator ipSim, string textFileName)
        {
            if(ExternalProcess==null || ipSim==null || ExternalProcess.HasExited )
            {
                return (false);
            }
            IntPtr epHandle = ExternalProcess.MainWindowHandle;
            if (epHandle == (IntPtr)0L)
            {
                return (false);
            }
            bool result = true;
            string bareFileName = "LabelTrack";
            if (String.IsNullOrWhiteSpace(textFileName))
            {
                bareFileName = "LabelTrack";
            }
            else
            {
                if (textFileName.ToUpper().EndsWith(".TXT"))
                {
                    bareFileName = textFileName.ExtractFilename(".txt");
                }
                else if (textFileName.ToUpper().EndsWith(".WAV"))
                {
                    bareFileName = textFileName.ExtractFilename(".wav");
                }
            }
               
            
            try
            {
                
                SetForegroundWindow(epHandle);
                //s.Keyboard.ModifiedKeyStroke(VirtualKeyCode.LMENU, VirtualKeyCode.VK_S);
                if (!Tools.WaitForIdle(ExternalProcess)) return false;
                // CTRL-SHIFT-N - select nothing, i.e. clear current selection
                ipSim.Keyboard.ModifiedKeyStroke(new[] { VirtualKeyCode.CONTROL, VirtualKeyCode.LSHIFT }, VirtualKeyCode.VK_N);
                //Thread.Sleep(2000);
                //SetForegroundWindow(h);
                //s.Keyboard.KeyPress(VirtualKeyCode.VK_N);
                //Thread.Sleep(2000);
                // CTRL-SHIFT-B - create new laabel track
                SetForegroundWindow(epHandle);
                if (!Tools.WaitForIdle(ExternalProcess)) return false;
                ipSim.Keyboard.ModifiedKeyStroke(new[] { VirtualKeyCode.CONTROL, VirtualKeyCode.LSHIFT }, VirtualKeyCode.VK_B);
                if (!Tools.WaitForIdle(ExternalProcess)) return false;
                //Thread.Sleep(2000);
                //Thread.Sleep(5000);
                // CTRL-SHIFT-M - Open track menu
                SetForegroundWindow(epHandle);
                ipSim.Keyboard.ModifiedKeyStroke(new[] { VirtualKeyCode.CONTROL, VirtualKeyCode.LSHIFT }, VirtualKeyCode.VK_M);
                if (!Tools.WaitForIdle(ExternalProcess)) return false;
                //Thread.Sleep(2000);
                //Thread.Sleep(5000);
                // N - select 'Name' from menu
                SetForegroundWindow(epHandle);
                ipSim.Keyboard.KeyPress(VirtualKeyCode.VK_N);
                if (!Tools.WaitForIdle(ExternalProcess)) return false;
                //Thread.Sleep(2000);
                // write filename into the Name field text box
                //SetForegroundWindow(epHandle);
                ipSim.Keyboard.TextEntry(bareFileName);
                if (!Tools.WaitForIdle(ExternalProcess)) return false;
                //Thread.Sleep(2000);
                // ENTER - to close the Name dialog
                //SetForegroundWindow(epHandle);
                ipSim.Keyboard.KeyPress(VirtualKeyCode.RETURN);
                if (!Tools.WaitForIdle(ExternalProcess)) return false;

                if (!WaitForIdle(ExternalProcess, "=", "ENTER after text file name")) return (false);
                ipSim.Keyboard.KeyPress(VirtualKeyCode.UP);
                if (!WaitForIdle(ExternalProcess)) return false;
            }
            catch(Exception ex)
            {
                Debug.WriteLine("Error trying to create audacity label file:- " + ex.Message);
                Tools.ErrorLog("Error trying to create label file in Audacity:-" + ex.Message);
                result = false;
            }
            return (result);
        }

        /// <summary>
        /// Opens an existing text file as a label file by sending keyboard commands to Audacity using the supplied
        /// InputSimulator.  Audacity is running in the provided Process.
        /// ALT-F       Open file menu
        /// I           Select Import
        /// L           Select Labels, opens a file dialog
        /// textfilename    Type in the name of the text file
        /// RETURN      Accept and close the dialog
        /// UP          Move focus back to the audio track
        /// </summary>
        /// <param name="ExternalProcess"></param>
        /// <param name="ipSim"></param>
        /// <param name="textFileName"></param>
        /// <returns></returns>
        private static bool OpenAudacityLabelFile(Process ExternalProcess, InputSimulator ipSim,string textFileName)
        {
            bool result = true;
            try
            {
                ipSim.Keyboard.ModifiedKeyStroke(new[] { VirtualKeyCode.LMENU }, VirtualKeyCode.VK_F);
                if (!WaitForIdle(ExternalProcess)) return false;
                ipSim.Keyboard.KeyPress(VirtualKeyCode.VK_I);
                if (!WaitForIdle(ExternalProcess)) return false;
                ipSim.Keyboard.KeyPress(VirtualKeyCode.VK_L);
                Thread.Sleep(1000);
                if (!WaitForIdle(ExternalProcess, "+", "Open Label Track")) return (false);

                //SetForegroundWindow(epHandle);
                //ExternalProcess.WaitForInputIdle();
                //Thread.Sleep(3000);

                //textFileName = Tools.StripPath(textFileName);
                ipSim.Keyboard.TextEntry("\""+textFileName+"\"");
                if (!WaitForIdle(ExternalProcess, "-", "Entered Text File Name")) return (false);
                //Thread.Sleep(2000);
                // ENTER - to close the Name dialog
                //SetForegroundWindow(epHandle);
                ipSim.Keyboard.KeyPress(VirtualKeyCode.RETURN);
                Debug.WriteLine(textFileName);
                Thread.Sleep(500);

                //SetForegroundWindow(epHandle);
                if (!WaitForIdle(ExternalProcess, "=", "ENTER after text file name")) return (false);
                ipSim.Keyboard.KeyPress(VirtualKeyCode.UP);
                if (!WaitForIdle(ExternalProcess)) return false;
                //Thread.Sleep(sleep);
            }catch(Exception ex)
            {
                Debug.WriteLine("Error trying to open audacity label file:- " + ex.Message);
                Tools.ErrorLog("Error trying to open label file in Audacity:-" + ex.Message);
                result = false;
            }
            return (result);
        }

        /// <summary>
        /// Given an external process, waits for the process to be responding and for Inputidle as well
        /// as a static 100ms wait at the start.  If the ExternalProcess exits during the wait then the
        /// function returns false, otherwise it returns true.  Will wait indefinitiely if the process
        /// does not exit and never becomes idle.
        /// </summary>
        /// <param name="ExternalProcess"></param>
        /// <param name="marker"></param>
        /// <param name="location"></param>
        /// <returns></returns>
        public static bool WaitForIdle(Process ExternalProcess,string marker="",string location = "")
        {
            Debug.Write(marker);
            Thread.Sleep(100);
            while (!ExternalProcess.WaitForInputIdle(500) || !ExternalProcess.Responding)
            {
                Debug.Write(marker);
                if (ExternalProcess.HasExited)
                {
                    ExternalProcess.Close();
                    Debug.Write("Process Exited at:- " + location);
                    return (false);
                }
            }
            return (true);
        }

        /// <summary>
        /// Looks for a copy of Audacity in a subfolder of the current folder and if found
        /// returns the fully qualified name of the executabe file, trimmed
        /// </summary>
        /// <returns></returns>
        private static string FindAudacity()
        {
            string result = "";
            string folder = @"C:\audacity-win-portable\"; //Audacity installation folder used by InnoScript installer
            string file = "audacity.exe";
            if (Directory.Exists(folder))
            {
                
                
                if (File.Exists(folder+file))
                {
                    return (folder+file);
                }
            }
            var subdirs=Directory.GetDirectories(System.Windows.Forms.Application.ExecutablePath.Substring(0,System.Windows.Forms.Application.ExecutablePath.LastIndexOf(@"\")));
            foreach(var rawsubdir in subdirs)
            {
                string subdir = rawsubdir.Trim();
                if (!subdir.EndsWith(@"\"))
                {
                    subdir = subdir + @"\";
                }
                Debug.WriteLine(subdir);
                if (subdir.ToUpper().Contains("AUDACITY"))
                {
                    
                    if (File.Exists(subdir + file))
                    {
                        return (subdir + file);
                        
                    }
                    
                }
            }
            return ("");
        }

        /// <summary>
        /// Opens Kaleidoscope with a folder path or a filename.
        /// If the string is a filename the file part is stripped from it to leave
        /// the bare path.  Then the oldest .wav file in the specified folder is opened
        /// in Kaleidoscope.  It is assumed that Kaleidoscope.exe is in the system path
        /// and does not need to be explicitly located in order to run.
        /// If a process is passed to the function then a callback ill be generated on that
        /// process when Kaleidoscope exits.  If no process is provided a new one will be
        /// generated.
        /// </summary>
        /// <param name="wavFile"></param>
        /// <param name="process" default="null"></param>
        public static Process OpenKaleidoscope(string wavFile,Process ExternalProcess=null)
        {
            if (string.IsNullOrWhiteSpace(wavFile)) return (null);
            string file = GetOldestFile(wavFile); // returns the fully qualified path and filename
            if (String.IsNullOrWhiteSpace(file)) return (null);

            if (ExternalProcess == null)
            {
                ExternalProcess = new Process();
                ExternalProcess.Exited += ExternalProcess_Exited;
                if (ExternalProcess == null) return (null);
            }
            DialogResult result = DialogResult.Retry;
            while (result == DialogResult.Retry)
            {
                System.Diagnostics.Process[] p = System.Diagnostics.Process.GetProcessesByName("kaleidoscope");

                if (p.Length > 0)
                {
                    result = System.Windows.Forms.MessageBox.Show("Please close open copies of Kaleidoscope first.", "Kaleidoscope Already Open", MessageBoxButtons.RetryCancel);
                }
                else
                {
                    break;
                }
            }

            if (result == DialogResult.Cancel)
            {
                ExternalProcess.Close();
                return (null);
            }
            string executable = @"C:\Program Files (x86)\Wildlife Acoustics\kaleidoscope\kaleidoscope.exe";
            ExternalProcess.StartInfo.FileName = "\"" + executable + "\"";
            ExternalProcess.StartInfo.Arguments = "\"" + file + "\""; // enclosed in quotes in case the path has spaces

            
            //externalProcess.StartInfo.Arguments = folder;
            ExternalProcess.StartInfo.WindowStyle = ProcessWindowStyle.Maximized;

            bool started = ExternalProcess.Start();
            while (!ExternalProcess.Responding)
            {
                Thread.Sleep(100);
                Debug.Write("!");
            }
            Thread.Sleep(1000);
            ExternalProcess.EnableRaisingEvents = true;
            //ExternalProcess.Exited += ExternalProcess_Exited;

            //int startSeconds = (int)startOffset.TotalSeconds;
            //int endSeconds = (int)endOffset.TotalSeconds;
            //if (endSeconds == startSeconds) endSeconds = startSeconds + 1;
            try
            {
                System.Windows.Application.Current.MainWindow.Focus();
            }
            catch (System.InvalidOperationException) { }
            while (ExternalProcess.MainWindowHandle == (IntPtr)0L)
            {
                if (ExternalProcess.HasExited)
                {
                    ExternalProcess.Close();
                    return (null);
                }
            }
            if (!WaitForIdle(ExternalProcess, "!", "Starting")) return (null);
            IntPtr epHandle = ExternalProcess.MainWindowHandle;
            SetForegroundWindow(epHandle);

            return (ExternalProcess);
        }

        /// <summary>
        /// Given a fully qualified file name, returns the fully qualified name
        /// of the oldest .wav file in the same folder, based on the last modified date.
        /// </summary>
        /// <param name="wavFile"></param>
        /// <returns></returns>
        private static string GetOldestFile(string wavFile)
        {
            string folder = GetPath(wavFile);
            if (!Directory.Exists(folder)) return (null);
            var fileList = Directory.EnumerateFiles(folder, "*.wav");
            //var FILEList= Directory.EnumerateFiles(folder, "*.WAV");
            //fileList = fileList.Concat<string>(FILEList);
            DateTime earliestDate = DateTime.Now;
            string file = "";
            foreach(var f in fileList)
            {
                DateTime thisDate = File.GetLastWriteTime(f);
                if (thisDate < earliestDate)
                {
                    file = f;
                    earliestDate = thisDate;
                }
            }
            return (file);
        }

        /// <summary>
        /// Returns the path component from the fully qualified file name
        /// </summary>
        /// <param name="wavFile"></param>
        /// <returns></returns>
        public static string GetPath(string wavFile)
        {
            
            if (string.IsNullOrWhiteSpace(wavFile))
            {
                return ("");
            }
            if (wavFile.EndsWith(@"\")) return (wavFile);
            if (!wavFile.Contains(@"\")) return ("");
            
            return (wavFile.Substring(0, wavFile.LastIndexOf(@"\")+1));
        }

        private static void ExternalProcess_Exited(object sender, EventArgs e)
        {
            Debug.WriteLine("************** Process Exited *****************");
        }

        /// <summary>
        /// Given a filename removes the path if any
        /// </summary>
        /// <param name="textFileName"></param>
        /// <returns></returns>
        public static string StripPath(string textFileName)
        {
            if (textFileName.EndsWith(@"\"))
            {
                return ("");
            }
            if (textFileName.Contains(@"\"))
            {
                textFileName = textFileName.Substring(textFileName.LastIndexOf(@"\")+1 );
            }
            //Debug.WriteLine("Open text file:-" + textFileName);
            return (textFileName);
        }

        /// <summary>
        /// Given a .wav filename (or indeed any other filename) replaces the last four characters
        /// of the name with .txt and returns that modified string.  Does not do any explicit checks to see
        /// if the string passed is indeed a filename, with or without a path.
        /// If the input string is null, empty or less than 4 characters long then the function returns
        /// an unmodified string
        /// </summary>
        /// <param name="wavFile"></param>
        /// <returns></returns>
        private static string GetMatchingTextFile(string wavFile)
        {
            if(string.IsNullOrWhiteSpace(wavFile) || wavFile.Length < 4)
            {
                return (wavFile);
            }
            wavFile = wavFile.Substring(0, wavFile.Length - 4);
            wavFile = wavFile + ".txt";
            return (wavFile);

        }

        [DllImport("user32")]
        private static extern bool ShowWindowAsync(IntPtr hwnd, int a);

        /// <summary>
        /// Opens the specified .wav file in Audacity and sends the necessarty keyboard commands to zoom in to
        /// the segment defined by the specified start and end offsets.
        /// </summary>
        /// <param name="wavFile"></param>
        /// <param name="startOffset"></param>
        /// <param name="endOffset"></param>
        internal static void OpenWavFile(string wavFile, TimeSpan startOffset, TimeSpan endOffset)
        {
            if(string.IsNullOrWhiteSpace(wavFile) || !File.Exists(wavFile))
            {
                return; // since we don't have a valid file name to work with
            }
            int startSeconds = (int)startOffset.TotalSeconds;
            int endSeconds = (int)endOffset.TotalSeconds;
            if (endSeconds == startSeconds) endSeconds = startSeconds + 1;
            Debug.WriteLine("Open Audacity from " + startOffset.TotalSeconds + " to " + endOffset.TotalSeconds);
            Process ExternalProcess = OpenWavAndTextFile(wavFile);
            if (ExternalProcess == null)
            {
                return; // since we have failed to start an external program successfully
            }
            Debug.WriteLine("Audacity running and file opened");
            IntPtr epHandle = ExternalProcess.MainWindowHandle;
            int sleep = 1000;
            
            try {
                /*
                 * J            Move to start of track
                 * ....         Move right (end-start) seconds
                 * SHIFT-J      select cursor to start of track
                 * CTRL-E       Zoom to selection
                 * J            Move to start of track and clear selection
                 * ....         Move right (end) seconds
                 * 
                 * 
                 * 
                 * 
                 * */
                InputSimulator ipSim = new InputSimulator();
                // J - move to start of track
                SetForegroundWindow(epHandle);
                
                ipSim.Keyboard.KeyPress(VirtualKeyCode.VK_J);
                if (!WaitForIdle(ExternalProcess)) return ;
                //         Thread.Sleep(400);
                //Thread.Sleep(sleep);

                // . . . . . - Cursor short jump right (by one second)
                SetForegroundWindow(epHandle);
                for (int i = 0; i < endSeconds - startSeconds; i++)
                {
                    ipSim.Keyboard.KeyPress(VirtualKeyCode.OEM_PERIOD);
                    Thread.Sleep(50);
                    //if (!WaitForIdle(ExternalProcess)) return ;
                }
                //Thread.Sleep(sleep);
                if (!WaitForIdle(ExternalProcess)) return ;
                // Zoom to size of segment
                SetForegroundWindow(epHandle);
                ipSim.Keyboard.ModifiedKeyStroke(VirtualKeyCode.LSHIFT, VirtualKeyCode.VK_J);
                if (!WaitForIdle(ExternalProcess)) return ;
                //Thread.Sleep(sleep);

                // Zoom start to cursor
                SetForegroundWindow(epHandle);
                ipSim.Keyboard.ModifiedKeyStroke(VirtualKeyCode.CONTROL, VirtualKeyCode.VK_E);
                if (!WaitForIdle(ExternalProcess)) return ;

                // Back to the start, clearing the selection
                SetForegroundWindow(epHandle);
                ipSim.Keyboard.KeyPress(VirtualKeyCode.VK_J);
                if (!WaitForIdle(ExternalProcess)) return ;
                //Thread.Sleep(sleep);

                //Move cursor to the end of the segment - scrolling the screen as it goes
                for (int i = 0; i < endSeconds; i++)
                {
                    SetForegroundWindow(epHandle);
                    ipSim.Keyboard.KeyPress(VirtualKeyCode.OEM_PERIOD);

                    Thread.Sleep(50);
                    //if (!WaitForIdle(ExternalProcess)) return ;
                }
                Debug.WriteLine("Audacity zoomed");
            }
            catch (Exception ex)
            {
                Tools.ErrorLog("Error opening and zooming Audacity:-" + ex.Message);
                Debug.WriteLine("Error opening and zooming Audacity:-" + ex.Message);
            }
        }

        /// <summary>
        /// returns a DateTime containing the date defined in a sessionTag of the format
        /// [alnum]*[-_][alnum]+[-_]20yymmdd
        /// </summary>
        /// <returns></returns>
        internal static DateTime GetDateFromTag(string tag)
        {
            DateTime result = new DateTime();

            string dateField = tag.Substring(tag.LastIndexOfAny(new char[] { '-', '_' }));
            if (dateField.Length == 9)
            {
                string year = dateField.Substring(1, 4);
                string month = dateField.Substring(5, 2);
                string day = dateField.Substring(7, 2);
                int Year = DateTime.Now.Year;
                int Month = DateTime.Now.Month;
                int Day = DateTime.Now.Day;
                int.TryParse(year, out Year);
                int.TryParse(month, out Month);
                int.TryParse(day, out Day);
                result = new DateTime(Year, Month, Day);
            }
            return (result);
        }

#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member

        public static void setFolderIcon(string path, string iconPath, string folderToolTip)
#pragma warning restore CS1591 // Missing XML comment for publicly visible type or member
        {
            /* Remove any existing desktop.ini */
            if (File.Exists(path + @"desktop.ini")) File.Delete(path + @"desktop.ini");

            /* Write the desktop.ini */
            using (StreamWriter sw = File.CreateText(path + @"desktop.ini"))
            {
                if (sw != null)
                {
                    sw.WriteLine("[.ShellClassInfo]");
                    sw.WriteLine("InfoTip=" + folderToolTip);
                    sw.WriteLine("IconResource=" + iconPath);
                    sw.WriteLine("IconIndex=0");
                    sw.Close();
                }
            }

            /* Set the desktop.ini to be hidden */
            File.SetAttributes(path + @"desktop.ini", File.GetAttributes(path + @"desktop.ini") | FileAttributes.Hidden);

            /* Set the path to system */
            File.SetAttributes(path, File.GetAttributes(path) | FileAttributes.System);
        }

        /*
        internal static SegmentAndBatList Parse(String segmentLine)
        {
            BulkObservableCollection<Bat> bats = DBAccess.GetSortedBatList();
            var result = FileProcessor.ProcessLabelledSegment(segmentLine, bats);
            return (result);
        }*/

        /// <summary>
        ///     Parses a line in the format 00'00.00 into a TimeSpan the original strting has been
        ///     matched by a Regex of the form [0-9\.\']+
        /// </summary>
        /// <param name="value">
        ///     The value.
        /// </param>
        /// <returns>
        /// </returns>
        /// <exception cref="System.NotImplementedException">
        /// </exception>
        public static TimeSpan TimeParse(string value)
        {
            String regPattern = @"([0-9]*\')?([0-9]+)[\.]?([0-9]*)";
            int minutes = 0;
            int seconds = 0;
            int millis = 0;

            var result = System.Text.RegularExpressions.Regex.Match(value, regPattern);
            if (result.Success && result.Groups.Count >= 4)
            {// we have matched and identified the fields
                if (!String.IsNullOrWhiteSpace(result.Groups[1].Value))
                {
                    string minstr = result.Groups[1].Value.Substring(0, result.Groups[1].Value.Length - 1);
                    var r1 = int.TryParse(minstr, out minutes);
                }
                if (!String.IsNullOrWhiteSpace(result.Groups[2].Value))
                {
                    var r2 = int.TryParse(result.Groups[2].Value, out seconds);
                }
                if (!String.IsNullOrWhiteSpace(result.Groups[3].Value))
                {
                    double dm = 0.0d;
                    string s = "0." + result.Groups[3].Value;
                    var r3 = double.TryParse(s, out dm);
                    millis = (int)(dm * 1000);
                }
            }
            TimeSpan ts = new TimeSpan(0, 0, minutes, seconds, millis);
            return (ts);
        }

        /*
            //bool seconds = false;
            bool minutes = false;
            TimeSpan ts = new TimeSpan();
            String[] separators = { "\'", ".", "\"" };
            value = value.Trim();
            if (value.EndsWith("\""))
            {
                //seconds = true;
                value = value.Substring(0, value.Length - 1);
            }
            if (value.EndsWith("\'"))
            {
                minutes = true;
                value = value.Substring(0, value.Length - 1);
            }
            if (value.EndsWith("."))
            {
                value = value.Substring(0, value.Length - 1);
            }
            String[] numbers = value.Split(separators, StringSplitOptions.None);
            if (numbers.Count() == 3) //dd'dd.dd"
            {
                int mins = 0;
                int.TryParse(numbers[0], out mins);
                int secs = 0;
                int.TryParse(numbers[1], out secs);
                int millis = 0;
                while (numbers[2].Length > 3)
                {
                    numbers[2] = numbers[2].Substring(0, 3);
                }
                while (numbers[2].Length < 3)
                {
                    numbers[2] = numbers[2] + "0";
                }
                int.TryParse(numbers[2], out millis);
                ts = new TimeSpan(0, 0, mins, secs, millis);
            }
            else if (numbers.Count() == 2) //dd.dd in seconds or dd'dd or dd.dd'
            {
                if (value.Contains('.') && !minutes) // dd.dd in seconds
                {
                    int secs = 0;
                    int.TryParse(numbers[0], out secs);
                    while (numbers[1].Length > 3)
                    {
                        numbers[1] = numbers[1].Substring(0, 3);
                    }
                    while (numbers[1].Length < 3)
                    {
                        numbers[1] = numbers[1] + "0";
                    }
                    int millis = 0;
                    int.TryParse(numbers[1], out millis);
                    ts = new TimeSpan(0, 0, 0, secs, millis);
                }else if (value.Contains('.')) // dd.dd' (' was truncated but caused the minutes flag to be set)
                {
                    int mins = 0;
                    int.TryParse(numbers[0], out mins);
                    double fractionalMins = 0.0d;
                    double.TryParse("0."+numbers[1], out fractionalMins);
                    int secs = (int)(60 * fractionalMins);
                    ts = new TimeSpan(0, mins, secs);
                }else // assume must be dd'dd" with a truncated or missing "
                {
                    int mins = 0;
                    int secs = 0;
                    int.TryParse(numbers[0], out mins);
                    int.TryParse(numbers[1], out secs);
                    ts = new TimeSpan(0, mins, secs);
                }
            }
            else if (numbers.Count() == 1) // dd in seconds or dd'
            {
                    int secs = 0;
                    int.TryParse(numbers[0], out secs);
                if (!minutes)
                {
                    ts = new TimeSpan(0, 0, 0, secs, 0);
                }else
                {
                    ts = new TimeSpan(0, secs, 0);
                }
            }

            return (ts);
        }*/

        /// <summary>
        /// Assumes that a filename may include the date in the format yyyymmdd
        /// preceded and followed by either - or _
        ///
        /// </summary>
        /// <param name="fileName"></param>
        /// <returns></returns>
        internal static DateTime? GetDateFromFilename(string fileName)
        {
            DateTime? result = null;
            if (string.IsNullOrWhiteSpace(fileName))
            {
                return (result);
            }

            String Pattern = @"[_-]([0-9]{4}).?([0-9]{2}).?([0-9]{2})[_-]";
            var match = Regex.Match(fileName, Pattern);
            if (match.Success)
            {
                int year = -1;
                int month = -1;
                int day = -1;

                if (match.Groups.Count > 3)
                {
                    int.TryParse(match.Groups[1].Value, out year);
                    int.TryParse(match.Groups[2].Value, out month);
                    int.TryParse(match.Groups[3].Value, out day);
                }
                if (year > 1970 && month >= 0 && month <= 12 && day >= 0 && day <= 31)
                {
                    result = new DateTime(year, month, day);

                    int hour = -1;
                    int minute = -1;
                    int secs = -1;
                    Pattern = @"[_-]([0-9]{4}).?([0-9]{2}).?([0-9]{2})[_-]([0-9]{2}).?([0-9]{2}).?([0-9]{2})";
                    match = Regex.Match(fileName, Pattern);
                    if (match.Success && match.Groups.Count > 6)
                    {
                        int.TryParse(match.Groups[4].Value, out hour);
                        int.TryParse(match.Groups[5].Value, out minute);
                        int.TryParse(match.Groups[6].Value, out secs);
                        if (hour >= 0 && hour <= 24 && minute >= 0 && minute <= 60 && secs >= 0 && secs <= 60)
                        {
                            result = new DateTime(year, month, day, hour, minute, secs);
                        }
                    }
                }
            }
            return (result);
        }

        //[System.Runtime.InteropServices.DllImport("gdi32.dll")]
        //public static extern bool DeleteObject(IntPtr hObject);
        /// <summary>
        /// Converts a System.Drawing.Bitmap to a WPF compatible BitmapImage
        /// </summary>
        /// <param name="source"></param>
        /// <returns></returns>
        public static BitmapSource ToBitmapSource(this System.Drawing.Bitmap source)
        {
            //IntPtr hBitmap = source.GetHbitmap();
            BitmapSource result;

            try
            {
                result = (BitmapSource)ImagingConverterClass.CreateBitmapSourceFromBitmap(source);
                //hBitmap,
                //IntPtr.Zero,
                //Int32Rect.Empty,
                //BitmapSizeOptions.FromEmptyOptions());
            }
            finally
            {
                // DeleteObject(hBitmap);
            }
            return (result);
            /*
            BitmapImage bitmapImage = new BitmapImage();
            using (MemoryStream memory = new MemoryStream())
            {
                source.Save(memory, System.Drawing.Imaging.ImageFormat.Png);
                memory.Position = 0;

                bitmapImage.BeginInit();
                bitmapImage.StreamSource = memory;
                bitmapImage.CacheOption = BitmapCacheOption.OnLoad;
                bitmapImage.EndInit();
            }
            return (bitmapImage);*/
        }

        /// <summary>
        ///     Valids the coordinates as GPS lat and long in text format and returns those
        ///     coordinates as a Location or null if they are not valid
        /// </summary>
        /// <param name="latit">
        ///     The latitude
        /// </param>
        /// <param name="longit">
        ///     The longitude
        /// </param>
        /// <returns>
        /// </returns>
        internal static Location ValidCoordinates(string latit, string longit)
        {
            Location result = null;
            if (!String.IsNullOrWhiteSpace(latit) && !String.IsNullOrWhiteSpace(longit))
            {
                double dLat = 200;
                double dlong = 200;
                double.TryParse(latit, out dLat);
                double.TryParse(longit, out dlong);
                result = ValidCoordinates(new Location(dLat, dlong));
            }

            return (result);
        }

        /// <summary>
        ///     Valids the coordinates in the location as valid GPS coordinates and returns the valid
        ///     Location or null if they are not valid.
        /// </summary>
        /// <param name="location">
        ///     The last selected location.
        /// </param>
        /// <returns>
        /// </returns>
        internal static Location ValidCoordinates(Location location)
        {
            Location result = null;
            if (location != null)
            {
                if (Math.Abs(location.Latitude) <= 90.0d && Math.Abs(location.Longitude) <= 180.0d)
                {
                    result = location;
                }
            }
            return (result);
        }

        /// <summary>
        /// Takes a string with two values as either mean+/-variation
        /// or as min-max, converts them tot he standard mean and
        /// variation format as two doubles and returns those two
        /// values.
        /// </summary>
        /// <param name="parameters"></param>
        /// <param name="mean"></param>
        /// <param name="variation"></param>
        /// <returns></returns>
        internal static bool GetValuesAsMeanAndVariation(string parameters, out double mean, out double variation)
        {
            mean = 0.0d;
            variation = 0.0d;
            if (string.IsNullOrWhiteSpace(parameters)) return (false);

            var match = Regex.Match(parameters, @"\s*([0-9.]+)\s*([+\-\/]*)\s*([0-9.]*)");
            /* then parse the two doubles, read the middle matched segment
             * and do the conversion as below if it is necessary,
             * then assign the values and return;
             * */

            if (match.Success)
            {
                for (int i = 1; i < match.Groups.Count; i++)
                {
                    if (i == 0)
                    {
                        continue;
                    }
                    double v = 0.0d;
                    double.TryParse(match.Groups[i].Value, out v);
                    switch (i)
                    {
                        case 1: mean = v; break;
                        case 2: break;
                        case 3: variation = v; break;
                        default: break;
                    }
                }
                if (match.Groups.Count > 2)
                {
                    string sep = match.Groups[2].Value;
                    if (!(sep as String).Contains("+/-"))
                    {
                        double temp = (mean + variation) / 2;
                        variation = Math.Max(mean, variation) - temp;
                        mean = temp;
                    }
                }
                return (true);
            }

            

            return (false);
        }

        

        /// <summary>
        /// Given a string, removes any curly brackets and replaces them around all the text following
        /// a $ if any, or around the entire string if there is no $
        /// </summary>
        /// <param name="comment"></param>
        /// <returns></returns>
        internal static string AdjustBracketedText(string comment)
        {
            comment = comment.Replace("{", " ");
            comment = comment.Replace("}", " ");
            comment = comment.Trim();
            if (comment.Contains("$"))
            {
                comment = comment.Replace("$", "${");
            }
            else
            {
                comment = "{" + comment;
            }
            comment = comment + "}";
            return (comment);
        }
    }

    /// /////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

    public static class ImagingConverterClass
    {
        [DllImport("gdi32.dll")]
        private static extern bool DeleteObject(IntPtr hObject);

        /// <summary>
        /// A static function to convert a Bitmap into a BitmapSOurce for display in a
        /// wpf Image
        /// </summary>
        /// <param name="bitmap"></param>
        /// <returns></returns>
        public static BitmapSource CreateBitmapSourceFromBitmap(Bitmap bitmap)
        {
            if (bitmap == null)
                throw new ArgumentNullException("bitmap");

            IntPtr hBitmap = bitmap.GetHbitmap();
            try
            {
                return (System.Windows.Interop.Imaging.CreateBitmapSourceFromHBitmap(
                    hBitmap,
                    IntPtr.Zero,
                    Int32Rect.Empty,
                    BitmapSizeOptions.FromEmptyOptions()));
            }
            finally
            {
                DeleteObject(hBitmap);
            }
        }
    }

    #region DoubleStringConverter (ValueConverter)

#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member

    public class DoubleStringConverter : IValueConverter
#pragma warning restore CS1591 // Missing XML comment for publicly visible type or member
    {
#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member

        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
#pragma warning restore CS1591 // Missing XML comment for publicly visible type or member
        {
            try
            {
                // Here's where you put the code do handle the value conversion.
                string str = "";
                str = String.Format("{0,5:N1}", value);

                return str;
            }
            catch
            {
                return value;
            }
        }

#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
#pragma warning restore CS1591 // Missing XML comment for publicly visible type or member
        {
            // Not implemented
            double d = -1.0d;
            double.TryParse((string)value, out d);
            if (d < 0) return (null);

            return (d);
        }
    }

    #endregion DoubleStringConverter (ValueConverter)

    #region TimeSpanDateConverter (ValueConverter)

    /// <summary>
    /// Converts a nullable Timespan into a DateTime of the same number of ticks, or a
    /// DateTime.Now if it is null
    /// </summary>
    public class TimeSpanDateConverter : IValueConverter
    {
        /// <summary>
        ///     Converts the specified value.
        /// </summary>
        /// <param name="value">
        ///     The value.
        /// </param>
        /// <param name="targetType">
        ///     Type of the target.
        /// </param>
        /// <param name="parameter">
        ///     The parameter.
        /// </param>
        /// <param name="culture">
        ///     The culture.
        /// </param>
        /// <returns>
        /// </returns>
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            try
            {
                if (value == null) return (DateTime.Now);
                TimeSpan time = (value as TimeSpan?) ?? new TimeSpan();
                DateTime result = new DateTime(time.Ticks);
                return (result);
            }
            catch
            {
                return value;
            }
        }

        /// <summary>
        ///     Converts the back.
        /// </summary>
        /// <param name="value">
        ///     The value.
        /// </param>
        /// <param name="targetType">
        ///     Type of the target.
        /// </param>
        /// <param name="parameter">
        ///     The parameter.
        /// </param>
        /// <param name="culture">
        ///     The culture.
        /// </param>
        /// <returns>
        /// </returns>
        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            TimeSpan result = new TimeSpan(((value as DateTime?) ?? DateTime.Now).Ticks);
            return (result);
        }
    }

    #endregion TimeSpanDateConverter (ValueConverter)

    #region SegmentToTextConverter (ValueConverter)

    /// <summary>
    /// Converts a LabelledSegment into a Text form as 'start - end comment'
    /// and appends an asterisk if the segnent has associated images
    /// </summary>
    public class SegmentToTextConverter : IValueConverter
    {
        /// <summary>
        ///     Converts the specified value.
        /// </summary>
        /// <param name="value">
        ///     The value.
        /// </param>
        /// <param name="targetType">
        ///     Type of the target.
        /// </param>
        /// <param name="parameter">
        ///     The parameter.
        /// </param>
        /// <param name="culture">
        ///     The culture.
        /// </param>
        /// <returns>
        /// </returns>
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            try
            {
                if (value == null) return ("");
                LabelledSegment segment = value as LabelledSegment;
                String result = Tools.FormattedTimeSpan(segment.StartOffset) + " - " + Tools.FormattedTimeSpan(segment.EndOffset) +
                    "  " + segment.Comment;
                while (result.Trim().EndsWith("*"))
                {
                    result = result.Substring(0, result.Length - 1);
                }
                String pattern = @"\(\s*[0-9]*\s*images?\s*\)";
                result = Regex.Replace(result, pattern, "");
                result = result.Trim();
                if (segment.SegmentDatas.Count > 0)
                {
                    result = result + " (" + segment.SegmentDatas.Count + " images )";
                }
                return result;
            }
            catch
            {
                return "";
            }
        }

        /// <summary>
        ///     Converts the back.
        /// </summary>
        /// <param name="value">
        ///     The value.
        /// </param>
        /// <param name="targetType">
        ///     Type of the target.
        /// </param>
        /// <param name="parameter">
        ///     The parameter.
        /// </param>
        /// <param name="culture">
        ///     The culture.
        /// </param>
        /// <returns>
        /// </returns>
        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            String text = value as String;
            LabelledSegment modifiedSegment = new LabelledSegment();
            modifiedSegment.Comment = text;

            return modifiedSegment;
        }
    }

    #endregion SegmentToTextConverter (ValueConverter)

    #region ImageConverter (ValueConverter)

    /// <summary>
    /// ImageConverter converts either a Bitmap or a BitmapImage to a BitmapSource suitable for
    /// display in a wpf Image control.  The BitmapImage is first converted to a Bitmap then to
    /// a BitmapSource.
    /// </summary>

    public class ImageConverter : IValueConverter
    {
        //[DllImport("gdi32.dll")]
        //private static extern bool DeleteObject(IntPtr hObject);

#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member

        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
#pragma warning restore CS1591 // Missing XML comment for publicly visible type or member
        {
            try
            {
                //<summary>
                // Converts System.Drawing.Bitmap to BitmapSource
                //</summary>
                //
                if (value == null) return (null);

                Bitmap bmp = new Bitmap(10, 10);
                //No conversion to be done if value is null

                if (value is BitmapImage)
                {
                    try
                    {
                        BitmapImage bmi = value as BitmapImage;
                        return (bmi);

                        /*
                        using (var stream = new MemoryStream())
                        {
                            BitmapEncoder enc = new BmpBitmapEncoder();
                            try
                            {
                                // NB Known to throw a SystemNotSupported Exception which is not an error but a WPF 'Feature'
                                enc.Frames.Add(BitmapFrame.Create(bmi));
                            }catch(Exception)
                            {
                                return (null);
                            }
                            enc.Save(stream);
                            bmp = new Bitmap(stream);
                        }
                        */
                    }
                    catch (Exception ex)
                    {
                        Tools.ErrorLog(ex.Message);
                        return (null);
                    }
                }
                else
                {
                    //Validate object being converted
                    if (value is System.Drawing.Bitmap)
                    {
                        if (value == null) value = new Bitmap(10, 10);
                        bmp = value as Bitmap;
                    }
                }
                if (bmp != null)
                {
                    //Use existing Interop functionality to perform conversion

                    return (bmp.ToBitmapSource());
                    /*IntPtr HBitmap = bmp.GetHbitmap();
                    try
                    {
                        System.Windows.Media.Imaging.BitmapSizeOptions sizeOptions =
                            System.Windows.Media.Imaging.BitmapSizeOptions.FromEmptyOptions();

                        BitmapSource bmps= System.Windows.Interop.Imaging.CreateBitmapSourceFromHBitmap(
                            HBitmap, IntPtr.Zero, Int32Rect.Empty, sizeOptions);
                        return (bmps);
                    }finally
                    {
                        DeleteObject(HBitmap);
                    }
                    */
                }
                else
                {
                    //We don't want the conversion to fail if it's not valid
                    return null;
                }
            }
            catch
            {
                return value;
            }
        }

#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
#pragma warning restore CS1591 // Missing XML comment for publicly visible type or member
        {
            // Not implemented
            return null;
        }
    }

    #endregion ImageConverter (ValueConverter)

    #region ShortDateConverter (ValueConverter)

    /// <summary>
    /// Converts a nullable DateTime to a short date string safely even for null values
    /// </summary>
    public class ShortDateConverter : IValueConverter
    {
        /// <summary>
        ///     Converts the specified value.
        /// </summary>
        /// <param name="value">
        ///     The value.
        /// </param>
        /// <param name="targetType">
        ///     Type of the target.
        /// </param>
        /// <param name="parameter">
        ///     The parameter.
        /// </param>
        /// <param name="culture">
        ///     The culture.
        /// </param>
        /// <returns>
        /// </returns>
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            try
            {
                // Here's where you put the code do handle the value conversion.
                DateTime dateToDisplay = (value as DateTime?) ?? DateTime.Now;
                return dateToDisplay.ToShortDateString();
            }
            catch
            {
                return value.ToString();
            }
        }

        /// <summary>
        ///     Converts the back.
        /// </summary>
        /// <param name="value">
        ///     The value.
        /// </param>
        /// <param name="targetType">
        ///     Type of the target.
        /// </param>
        /// <param name="parameter">
        ///     The parameter.
        /// </param>
        /// <param name="culture">
        ///     The culture.
        /// </param>
        /// <returns>
        /// </returns>
        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            string text = value as String;
            DateTime result = new DateTime();
            DateTime.TryParse(text, out result);
            return result;
        }
    }

    #endregion ShortDateConverter (ValueConverter)

    #region ShortTimeConverter (ValueConverter)

    /// <summary>
    /// Converts a nullable DateTime to a short date string safely even for null values
    /// </summary>
    public class ShortTimeConverter : IValueConverter
    {
        /// <summary>
        ///     Converts the specified value.
        /// </summary>
        /// <param name="value">
        ///     The value.
        /// </param>
        /// <param name="targetType">
        ///     Type of the target.
        /// </param>
        /// <param name="parameter">
        ///     The parameter.
        /// </param>
        /// <param name="culture">
        ///     The culture.
        /// </param>
        /// <returns>
        /// </returns>
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            try
            {
                // Here's where you put the code do handle the value conversion.

                if ((value as TimeSpan?) == null)
                {
                    return ("--:--:--");
                }

                TimeSpan timeToDisplay = (value as TimeSpan?) ?? new TimeSpan();

                return timeToDisplay.ToString(@"hh\:mm\:ss");
            }
            catch
            {
                return value.ToString();
            }
        }

        /// <summary>
        ///     Converts the back.
        /// </summary>
        /// <param name="value">
        ///     The value.
        /// </param>
        /// <param name="targetType">
        ///     Type of the target.
        /// </param>
        /// <param name="parameter">
        ///     The parameter.
        /// </param>
        /// <param name="culture">
        ///     The culture.
        /// </param>
        /// <returns>
        /// </returns>
        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            string text = value as String;
            DateTime result = new DateTime();
            DateTime.TryParse(text, out result);
            return result;
        }
    }

    #endregion ShortTimeConverter (ValueConverter)

    #region TextColourConverter (ValueConverter)

#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member

    public class TextColourConverter : IValueConverter
#pragma warning restore CS1591 // Missing XML comment for publicly visible type or member
    {
#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member

        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
#pragma warning restore CS1591 // Missing XML comment for publicly visible type or member
        {
            try
            {
                //String text = (value as LabelledSegment).Comment;
                String text = value as String;
                if (String.IsNullOrWhiteSpace(text))
                {
                    return (System.Drawing.Brushes.LightCyan);
                }
                if (text.EndsWith("H"))
                {
                    return (System.Drawing.Brushes.LightGreen);
                }
                else if (text.EndsWith("M"))
                {
                    return (System.Drawing.Brushes.LightGoldenrodYellow);
                }
                else if (text.EndsWith("L"))
                {
                    return (System.Drawing.Brushes.LightPink);
                }
                return (System.Drawing.Brushes.LightCyan);
            }
            catch
            {
                return (System.Drawing.Brushes.LightCyan);
            }
        }

#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
#pragma warning restore CS1591 // Missing XML comment for publicly visible type or member
        {
            // Not implemented
            return null;
        }
    }

    #endregion TextColourConverter (ValueConverter)

    #region DebugBreak (ValueConverter)

#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member

    public class DebugBreak : IValueConverter
#pragma warning restore CS1591 // Missing XML comment for publicly visible type or member
    {
#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member

        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
#pragma warning restore CS1591 // Missing XML comment for publicly visible type or member
        {
            try
            {
                // Here's where you put the code do handle the value conversion.
                //Debug.WriteLine("&&& DebugBreakConverter:- " + value == null ? "null" : (value.ToString()));
                return value;
            }
            catch
            {
                return value;
            }
        }

#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
#pragma warning restore CS1591 // Missing XML comment for publicly visible type or member
        {
            // Not implemented
            return null;
        }
    }

    #endregion DebugBreak (ValueConverter)

#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member

    public class WaitCursor : IDisposable
#pragma warning restore CS1591 // Missing XML comment for publicly visible type or member
    {
        private System.Windows.Input.Cursor _previousCursor;
        private string oldStatus = "null";

#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member

        public WaitCursor(String status="null")
#pragma warning restore CS1591 // Missing XML comment for publicly visible type or member
        {
            try
            {
                if (status != "null")
                {
                    //(App.Current.MainWindow as MainWindow).Dispatcher.Invoke((Action)delegate
                    System.Windows.Application.Current.Dispatcher.Invoke((Action)delegate
                    {
                        var mw = System.Windows.Application.Current.MainWindow;
                        if (mw != null && mw is MainWindow)
                        {
                            oldStatus = (mw as MainWindow).setStatusText(status);
                            //Debug.WriteLine("old Status=" + oldStatus);
                        }
                        _previousCursor = Mouse.OverrideCursor;
                        //Debug.WriteLine("old cursor saved");
                        Mouse.OverrideCursor = System.Windows.Input.Cursors.Wait;
                        //Debug.WriteLine("Wait cursor set");
                    });
                }
                else
                {
                    System.Windows.Application.Current.Dispatcher.Invoke((Action)delegate
                    {
                        _previousCursor = Mouse.OverrideCursor;
                        Mouse.OverrideCursor = System.Windows.Input.Cursors.Wait;
                    });
                }
            }
            catch (Exception ex) { Debug.WriteLine("WaitCursor failed for \""+status+"\":-" + ex.Message); }
        }

#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member

        public void Dispose()
#pragma warning restore CS1591 // Missing XML comment for publicly visible type or member
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member

        protected void Dispose(bool all)
#pragma warning restore CS1591 // Missing XML comment for publicly visible type or member
        {
            try
            {
                System.Windows.Application.Current.Dispatcher.Invoke((Action)delegate
                {


                    Mouse.OverrideCursor = _previousCursor??System.Windows.Input.Cursors.Arrow;
                });
                if (oldStatus != "null")
                {
                    //(App.Current.MainWindow as MainWindow).Dispatcher.Invoke((Action)delegate
                    System.Windows.Application.Current.Dispatcher.Invoke((Action)delegate
                    {
                        oldStatus = (App.Current.MainWindow as MainWindow).setStatusText(oldStatus);
                    });
                }
               
            }
            catch(Exception ex)
            {
                Debug.WriteLine("Error disposing of wait cursor:- " + ex.Message);
            }
        }
    }

    #region DivideConverter (ValueConverter)

#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member

    public class DivideConverter : IValueConverter
#pragma warning restore CS1591 // Missing XML comment for publicly visible type or member
    {
#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member

        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
#pragma warning restore CS1591 // Missing XML comment for publicly visible type or member
        {
            try
            {
                if (value != null && parameter != null)
                {
                    var val = (double)value;
                    var parm = (double)parameter;
                    return (val / parm);
                }
                return (double)value / 2;
            }
            catch
            {
                return value;
            }
        }

#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
#pragma warning restore CS1591 // Missing XML comment for publicly visible type or member
        {
            // Not implemented
            return null;
        }
    }

    #endregion DivideConverter (ValueConverter)

    #region Times2Converter (ValueConverter)

#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member

    public class Times2Converter : IValueConverter
#pragma warning restore CS1591 // Missing XML comment for publicly visible type or member
    {
#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member

        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
#pragma warning restore CS1591 // Missing XML comment for publicly visible type or member
        {
            try
            {
                // Here's where you put the code do handle the value conversion.
                double factor = 1.0d;
                double.TryParse(parameter as string, out factor);
                return ((double)value) * factor;
            }
            catch
            {
                return value;
            }
        }

        /// <summary>
        /// Used to set the height of a scale grid inside a canvas of variable size.
        /// The converter is passed to bound values the height of the parent canvas and a
        /// scale factor.  it returns a value of the height multiplied by the scale factor.
        /// </summary>
        public class GridScaleConverter : IMultiValueConverter
        {
            public object Convert(object[] values, Type targetType, object parameter, System.Globalization.CultureInfo culture)
            {
                if (values.Count() < 2) return (0.0d as Double?);
                if (values.Count() == 1) return values[0] as Double?;
                Double? height = values[0] as Double?;
                Double? scale = values[1] as Double?;
                return (height * scale);
            }

            public object[] ConvertBack(object value, Type[] targetTypes, object parameter, System.Globalization.CultureInfo culture)
            {
                throw new NotImplementedException();
            }
        }

#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
#pragma warning restore CS1591 // Missing XML comment for publicly visible type or member
        {
            // Not implemented
            return null;
        }
    }

    #endregion Times2Converter (ValueConverter)

    #region multiscaleConverter (ValueConverter)

    /// <summary>
    /// Converter class for scaling height or width of an image
    /// </summary>
    public class multiscaleConverter : IMultiValueConverter

    {
        /// <summary>
        /// Forward scale converter
        /// </summary>
        /// <param name="values"></param>
        /// <param name="targetType"></param>
        /// <param name="parameter"></param>
        /// <param name="culture"></param>
        /// <returns></returns>
        public object Convert(object[] values, Type targetType, object parameter, System.Globalization.CultureInfo culture)

        {
            try
            {
                // Here's where you put the code do handle the value conversion.
                if (values != null && values.Count() >= 2)
                {
                    double height = 100.0d;
                    double factor = 0.5d;

                    if (values[0] is string)
                    {
                        string strHeight = (values[0] == null ? string.Empty : values[0].ToString());
                        double.TryParse(strHeight, out height);
                    }
                    if (values[0] is double)
                    {
                        height = (double)(values[0] as Double?).Value;
                    }

                    if (values[1] is string)
                    {
                        string strFactor = (values[1] == null ? string.Empty : values[1].ToString());
                        double.TryParse(strFactor, out factor);
                    }
                    if (values[1] is double)
                    {
                        factor = (double)(values[1] as Double?).Value;
                    }

                    return (height * factor);
                }

                return (100.0d);
            }
            catch
            {
                return 100.0d;
            }
        }

        /// <summary>
        /// Reverse converter not used
        /// </summary>
        /// <param name="value"></param>
        /// <param name="targetTypes"></param>
        /// <param name="parameter"></param>
        /// <param name="culture"></param>
        /// <returns></returns>
        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, System.Globalization.CultureInfo culture)
        {
            // Not implemented
            return null;
        }
    }

    #endregion multiscaleConverter (ValueConverter)

    #region HGridLineConverter (ValueConverter)

    /// <summary>
    /// Converter class for scaling height or width of an image
    /// It is passed the location of the line in the stored image and a copy of
    /// the displayImageCanvas it is to be written and the storedImage it relates to
    /// </summary>
    public class HGridLineConverter : IMultiValueConverter

    {
        /// <summary>
        /// Forward scale converter
        /// </summary>
        /// <param name="values"></param>
        /// <param name="targetType"></param>
        /// <param name="parameter"></param>
        /// <param name="culture"></param>
        /// <returns></returns>
        public object Convert(object[] values, Type targetType, object parameter, System.Globalization.CultureInfo culture)

        {
            try
            {
                int indexToGridline = -1;
                int.TryParse(values[0] as String, out indexToGridline);
                var width = values[1] as double?;
                var height = values[2] as double?;
                
                StoredImage si = values[3] as StoredImage;
                //Debug.WriteLine("HGridLineConverter:- storedValue=" + si.HorizontalGridlines[indexToGridline]);

                if (width == null || height==null || indexToGridline < 0 || indexToGridline>=si.HorizontalGridlines.Count || si == null) return (null);

                var displayPosition = DisplayStoredImageControl.FindHScaleProportion(si.HorizontalGridlines[indexToGridline], width.Value,height.Value, si)*height;
                //Debug.WriteLine("      DisplayedPosition=" + displayPosition);

                return (displayPosition);



                
            }
            catch(Exception ex)
            {
                Debug.WriteLine("HGridLineConverter error:- " + ex.Message);
                return null;
            }
        }

        /// <summary>
        /// Reverse converter not used
        /// </summary>
        /// <param name="value"></param>
        /// <param name="targetTypes"></param>
        /// <param name="parameter"></param>
        /// <param name="culture"></param>
        /// <returns></returns>
        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, System.Globalization.CultureInfo culture)
        {
            // Not implemented
            return null;
        }
    }

    #endregion HGridLineConverter (ValueConverter)

    #region VGridLineConverter (ValueConverter)

    /// <summary>
    /// Converter class for scaling height or width of an image
    /// It is passed the location of the line in the stored image and a copy of
    /// the displayImageCanvas it is to be written and the storedImage it relates to
    /// </summary>
    public class VGridLineConverter : IMultiValueConverter

    {
        /// <summary>
        /// Forward scale converter
        /// </summary>
        /// <param name="values"></param>
        /// <param name="targetType"></param>
        /// <param name="parameter"></param>
        /// <param name="culture"></param>
        /// <returns></returns>
        public object Convert(object[] values, Type targetType, object parameter, System.Globalization.CultureInfo culture)

        {
            try
            {
                int indexToGridline = -1;
                int.TryParse(values[0] as String, out indexToGridline);
                var width = values[1] as double?;
                var height = values[2] as double?;

                StoredImage si = values[3] as StoredImage;

                if (width == null || height == null || indexToGridline < 0 || indexToGridline>=si.VerticalGridLines.Count || si == null) return (null);

                var displayPosition = DisplayStoredImageControl.FindVScaleProportion(si.VerticalGridLines[indexToGridline], width.Value, height.Value, si) * width;


                return (displayPosition);




            }
            catch
            {
                return null;
            }
        }

        /// <summary>
        /// Reverse converter not used
        /// </summary>
        /// <param name="value"></param>
        /// <param name="targetTypes"></param>
        /// <param name="parameter"></param>
        /// <param name="culture"></param>
        /// <returns></returns>
        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, System.Globalization.CultureInfo culture)
        {
            // Not implemented
            return null;
        }
    }

    #endregion VGridLineConverter (ValueConverter)

    #region LeftMarginConverter (ValueConverter)

    /// <summary>
    /// Converter class for scaling height or width of an image
    /// It is passed the location of the line in the stored image and a copy of
    /// the displayImageCanvas it is to be written and the storedImage it relates to
    /// </summary>
    public class LeftMarginConverter : IMultiValueConverter

    {
        /// <summary>
        /// Forward scale converter
        /// </summary>
        /// <param name="values"></param>
        /// <param name="targetType"></param>
        /// <param name="parameter"></param>
        /// <param name="culture"></param>
        /// <returns></returns>
        public object Convert(object[] values, Type targetType, object parameter, System.Globalization.CultureInfo culture)

        {
            try
            {
                
                var width = values[0] as double?;
                var height = values[1] as double?;

                StoredImage si = values[2] as StoredImage;

                if(width!=null && height!=null && si != null)
                {
                    //Debug.WriteLine("============================================================================================");
                    double hscale = width.Value / (double)si.image.Width;
                    double vscale = height.Value / (double)si.image.Height;
                    double actualScale = Math.Min(hscale, vscale);

                    var rightAndLeftMargins = Math.Abs(width.Value - (si.image.Width * actualScale));
                    var topAndBottomMargins = Math.Abs(height.Value - (si.image.Height * actualScale));

                    return (rightAndLeftMargins / 2);
                }
                return (0.0d);



            }
            catch
            {
                return 0.0d;
            }
        }

        /// <summary>
        /// Reverse converter not used
        /// </summary>
        /// <param name="value"></param>
        /// <param name="targetTypes"></param>
        /// <param name="parameter"></param>
        /// <param name="culture"></param>
        /// <returns></returns>
        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, System.Globalization.CultureInfo culture)
        {
            // Not implemented
            return null;
        }
    }

    #endregion LeftMarginConverter (ValueConverter)
    #region RightMarginConverter (ValueConverter)
    /// <summary>
    /// Converter class for scaling height or width of an image
    /// It is passed the location of the line in the stored image and a copy of
    /// the displayImageCanvas it is to be written and the storedImage it relates to
    /// </summary>
    public class RightMarginConverter : IMultiValueConverter

    {
        /// <summary>
        /// Forward scale converter
        /// </summary>
        /// <param name="values"></param>
        /// <param name="targetType"></param>
        /// <param name="parameter"></param>
        /// <param name="culture"></param>
        /// <returns></returns>
        public object Convert(object[] values, Type targetType, object parameter, System.Globalization.CultureInfo culture)

        {
            try
            {

                var width = values[0] as double?;
                var height = values[1] as double?;

                StoredImage si = values[2] as StoredImage;

                if (width != null && height != null && si != null)
                {
                    //Debug.WriteLine("============================================================================================");
                    double hscale = width.Value / (double)si.image.Width;
                    double vscale = height.Value / (double)si.image.Height;
                    double actualScale = Math.Min(hscale, vscale);

                    var rightAndLeftMargins = Math.Abs(width.Value - (si.image.Width * actualScale));
                    var topAndBottomMargins = Math.Abs(height.Value - (si.image.Height * actualScale));

                    return ((rightAndLeftMargins / 2)+(si.image.Width*actualScale));
                }
                return (0.0d);



            }
            catch
            {
                return 0.0d;
            }
        }

        /// <summary>
        /// Reverse converter not used
        /// </summary>
        /// <param name="value"></param>
        /// <param name="targetTypes"></param>
        /// <param name="parameter"></param>
        /// <param name="culture"></param>
        /// <returns></returns>
        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, System.Globalization.CultureInfo culture)
        {
            // Not implemented
            return null;
        }
    }

    #endregion RightMarginConverter (ValueConverter)

    #region TopMarginConverter

    /// <summary>
    /// Converter class for scaling height or width of an image
    /// It is passed the location of the line in the stored image and a copy of
    /// the displayImageCanvas it is to be written and the storedImage it relates to
    /// </summary>
    public class TopMarginConverter : IMultiValueConverter

    {
        /// <summary>
        /// Forward scale converter
        /// </summary>
        /// <param name="values"></param>
        /// <param name="targetType"></param>
        /// <param name="parameter"></param>
        /// <param name="culture"></param>
        /// <returns></returns>
        public object Convert(object[] values, Type targetType, object parameter, System.Globalization.CultureInfo culture)

        {
            try
            {

                var width = values[0] as double?;
                var height = values[1] as double?;

                StoredImage si = values[2] as StoredImage;

                if (width != null && height != null && si != null)
                {
                    //Debug.WriteLine("============================================================================================");
                    double hscale = width.Value / (double)si.image.Width;
                    double vscale = height.Value / (double)si.image.Height;
                    double actualScale = Math.Min(hscale, vscale);

                    var rightAndLeftMargins = Math.Abs(width.Value - (si.image.Width * actualScale));
                    var topAndBottomMargins = Math.Abs(height.Value - (si.image.Height * actualScale));

                    return (topAndBottomMargins / 2);
                }
                return (0.0d);



            }
            catch
            {
                return 0.0d;
            }
        }

        /// <summary>
        /// Reverse converter not used
        /// </summary>
        /// <param name="value"></param>
        /// <param name="targetTypes"></param>
        /// <param name="parameter"></param>
        /// <param name="culture"></param>
        /// <returns></returns>
        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, System.Globalization.CultureInfo culture)
        {
            // Not implemented
            return null;
        }
    }

    #endregion TopMarginConverter (ValueConverter)


    #region BottomMarginConverter (ValueConverter)
    /// <summary>
    /// Converter class for scaling height or width of an image
    /// It is passed the location of the line in the stored image and a copy of
    /// the displayImageCanvas it is to be written and the storedImage it relates to
    /// </summary>
    public class BottomMarginConverter : IMultiValueConverter

    {
        /// <summary>
        /// Forward scale converter
        /// </summary>
        /// <param name="values"></param>
        /// <param name="targetType"></param>
        /// <param name="parameter"></param>
        /// <param name="culture"></param>
        /// <returns></returns>
        public object Convert(object[] values, Type targetType, object parameter, System.Globalization.CultureInfo culture)

        {
            try
            {

                var width = values[0] as double?;
                var height = values[1] as double?;

                StoredImage si = values[2] as StoredImage;

                if (width != null && height != null && si != null)
                {
                    //Debug.WriteLine("============================================================================================");
                    double hscale = width.Value / (double)si.image.Width;
                    double vscale = height.Value / (double)si.image.Height;
                    double actualScale = Math.Min(hscale, vscale);

                    var rightAndLeftMargins = Math.Abs(width.Value - (si.image.Width * actualScale));
                    var topAndBottomMargins = Math.Abs(height.Value - (si.image.Height * actualScale));

                    return ((topAndBottomMargins / 2)+(si.image.Height*actualScale));
                }
                return (0.0d);



            }
            catch
            {
                return 0.0d;
            }
        }

        /// <summary>
        /// Reverse converter not used
        /// </summary>
        /// <param name="value"></param>
        /// <param name="targetTypes"></param>
        /// <param name="parameter"></param>
        /// <param name="culture"></param>
        /// <returns></returns>
        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, System.Globalization.CultureInfo culture)
        {
            // Not implemented
            return null;
        }
    }

#endregion BottomMarginConverter (ValueConverter)



    #region NumberOfImagesConverter (ValueConverter)

    /// <summary>
    /// Is passed an EntitySet of Recordings and calculates the total number of images
    /// associated with those recordings, returning the value as a string
    ///
    /// </summary>
    public class NumberOfImagesConverter : IValueConverter
    {
#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member

        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
#pragma warning restore CS1591 // Missing XML comment for publicly visible type or member
        {
            try
            {
                // Here's where you put the code do handle the value conversion.
                if (value == null) return ("0");
                EntitySet<Recording> recordings = value as EntitySet<Recording>;
                if (recordings.Count <= 0) return ("0");
                int imgs = 0;

                imgs = (from rec in recordings
                        from seg in rec.LabelledSegments

                        select seg.SegmentDatas.Count).Sum();
                /*
                foreach(var rec in recordings)
                {
                    if(rec.LabelledSegments!=null && rec.LabelledSegments.Count > 0)
                    {
                        foreach(var seg in rec.LabelledSegments)
                        {
                            imgs+=seg.SegmentDatas.Count;
                        }
                    }
                }*/
                return (imgs.ToString());
            }
            catch
            {
                return "0";
            }
        }

#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
#pragma warning restore CS1591 // Missing XML comment for publicly visible type or member
        {
            // Not implemented
            return null;
        }
    }

    #endregion NumberOfImagesConverter (ValueConverter)

    #region ConvertGetNumberOfImages (ValueConverter)

    /// <summary>
    /// Converter to get the number of images associated with a bat and return that value
    /// as a string for display in a DataItem Text Column
    /// </summary>
    public class ConvertGetNumberOfImages : IValueConverter
    {
#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member

        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
#pragma warning restore CS1591 // Missing XML comment for publicly visible type or member
        {
            try
            {
                // Here's where you put the code do handle the value conversion.
                if (value != null)
                {
                    Bat bat = value as Bat;
                    int cnt = 0;
                    if (bat.BatPictures != null)
                    {
                        cnt = bat.BatPictures.Count;
                    }
                    return cnt.ToString();
                }
                else
                {
                    return ("-");
                }
            }
            catch
            {
                return "-";
            }
        }

#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
#pragma warning restore CS1591 // Missing XML comment for publicly visible type or member
        {
            // Not implemented
            return null;
        }
    }

    #endregion ConvertGetNumberOfImages (ValueConverter)

    #region ImagesForAllRecordingsConverter (ValueConverter)

    /// <summary>
    /// converter getting images for all recordings
    /// </summary>
    public class ImagesForAllRecordingsConverter : IMultiValueConverter
    {
        /// <summary>
        /// converter - takes an array of 2 objects.  object[0] is a BulkObservableCollection
        /// of Recordings and  object[1] is a bat.  It returns a string representation of the
        /// number of images in all recordings that include that bat.
        /// i.e. the number of images linked to labelled segments for these recordings that
        /// include the named bat
        /// </summary>
        /// <param name="values"></param>
        /// <param name="targetType"></param>
        /// <param name="parameter"></param>
        /// <param name="culture"></param>
        /// <returns></returns>
        public object Convert(object[] values, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            try
            {
                // Here's where you put the code do handle the value conversion.
                if (values == null || values.Count() < 2) return ("-");

                int numberOfImages = 0;
                if (values[1] == null) return ("-");
                Bat bat = values[1] as Bat;
                if (values[0] == null) return ("-");
                BulkObservableCollection<Recording> recordings = values[0] as BulkObservableCollection<Recording>;
                if (recordings == null || recordings.Count <= 0)
                {
                    return ("-");
                }

                numberOfImages = (from rec in recordings.AsParallel()
                                  from seg in rec.LabelledSegments.AsParallel()
                                  from link in seg.BatSegmentLinks.AsParallel()
                                  where link.BatID == bat.Id
                                  select seg.SegmentDatas.Count).Sum();

                /*
                foreach(var rec in recordings)
                {
                    if(!rec.LabelledSegments.IsNullOrEmpty())
                    {
                        bool RecordingHasBat = false;
                        numberOfImages+=rec.GetImageCount(bat,out RecordingHasBat);
                    }
                }*/

                return numberOfImages.ToString();
            }
            catch
            {
                return "-";
            }
        }

        /// <summary>
        /// unconverter not used
        /// </summary>
        /// <param name="value"></param>
        /// <param name="targetType"></param>
        /// <param name="parameter"></param>
        /// <param name="culture"></param>
        /// <returns></returns>
        public object[] ConvertBack(object value, Type[] targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            // Not implemented
            return null;
        }
    }

    #endregion ImagesForAllRecordingsConverter (ValueConverter)

    #region ImageWithGridConverter (ValueConverter)

    /// <summary>
    /// Converter which takes a StoredImage and returns the image component overlaid with horizontal and vertical
    /// grid lines as defined in the StoredImage lists.
    /// </summary>
    public class ImageWithGridConverter : IValueConverter
    {
        /// <summary>
        /// Converter to add the grid lines to the image component and reutrn it
        /// </summary>
        /// <param name="value"></param>
        /// <param name="targetType"></param>
        /// <param name="parameter"></param>
        /// <param name="culture"></param>
        /// <returns></returns>
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            WriteableBitmap result = new WriteableBitmap(new BitmapImage());
            try
            {
                // Here's where you put the code do handle the value conversion.
                StoredImage sImage = value as StoredImage;
                result = new WriteableBitmap(sImage.image);
                return (result);
            }
            catch
            {
                return value;
            }
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            // Not implemented
            return null;
        }
    }

    #endregion ImageWithGridConverter (ValueConverter)

    #region LabelledSegmentConverter (ValueConverter)

    /// <summary>
    ///     Converts a LabelledSegment instance to an intelligible string for display
    /// </summary>
    public class LabelledSegmentConverter : IValueConverter
    {
        /// <summary>
        ///     Converts the specified value.
        /// </summary>
        /// <param name="value">
        ///     The value.
        /// </param>
        /// <param name="targetType">
        ///     Type of the target.
        /// </param>
        /// <param name="parameter">
        ///     The parameter.
        /// </param>
        /// <param name="culture">
        ///     The culture.
        /// </param>
        /// <returns>
        ///     </returns>
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            try
            {
                // Here's where you put the code do handle the value conversion.
                String result = "";
                if (value is LabelledSegment segment)
                {
                    result = Tools.FormattedSegmentLine(segment);
                    while (result.Trim().EndsWith("*"))
                    {
                        result = result.Substring(0, result.Length - 1);
                    }
                    result = result.Trim();
                    if (!result.EndsWith(")") && segment.SegmentDatas.Count > 0)
                    {
                        result = result + " (" + segment.SegmentDatas.Count + " images )";
                    }
                }

                return (result);
            }
            catch
            {
                return "";
            }
        }

        /// <summary>
        ///     ConvertBack not implemented
        /// </summary>
        /// <param name="value">
        ///     The value.
        /// </param>
        /// <param name="targetType">
        ///     Type of the target.
        /// </param>
        /// <param name="parameter">
        ///     The parameter.
        /// </param>
        /// <param name="culture">
        ///     The culture.
        /// </param>
        /// <returns>
        ///     </returns>
        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            // Not implemented
            return null;
        }
    }

    #endregion LabelledSegmentConverter (ValueConverter)

    #region BatCallConverter (ValueConverter)

    /// <summary>
    ///     Converts a LabelledSegment instance to an intelligible string for display
    /// </summary>
    public class BatCallConverter : IValueConverter
    {
        /// <summary>
        ///     Converts the specified value.
        /// </summary>
        /// <param name="value">
        ///     The value.
        /// </param>
        /// <param name="targetType">
        ///     Type of the target.
        /// </param>
        /// <param name="parameter">
        ///     The parameter.
        /// </param>
        /// <param name="culture">
        ///     The culture.
        /// </param>
        /// <returns>
        ///     </returns>
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            try
            {
                // Here's where you put the code do handle the value conversion.
                Call result = new Call();
                if (value is LabelledSegment segment)
                {
                    result = DBAccess.GetSegmentCall(segment);
                    if (result == null)
                    {
                        result = new Call();
                    }
                }

                return (result);
            }
            catch
            {
                return "";
            }
        }

        /// <summary>
        ///     ConvertBack not implemented
        /// </summary>
        /// <param name="value">
        ///     The value.
        /// </param>
        /// <param name="targetType">
        ///     Type of the target.
        /// </param>
        /// <param name="parameter">
        ///     The parameter.
        /// </param>
        /// <param name="culture">
        ///     The culture.
        /// </param>
        /// <returns>
        ///     </returns>
        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            // Not implemented
            return null;
        }
    }

    #endregion BatCallConverter (ValueConverter)

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto)]
    internal struct LPSHFOLDERCUSTOMSETTINGS
    {
        public UInt32 dwSize;
        public UInt32 dwMask;
        public IntPtr pvid;
        public string pszWebViewTemplate;
        public UInt32 cchWebViewTemplate;
        public string pszWebViewTemplateVersion;
        public string pszInfoTip;
        public UInt32 cchInfoTip;
        public IntPtr pclsid;
        public UInt32 dwFlags;
        public string pszIconFile;
        public UInt32 cchIconFile;
        public int iIconIndex;
        public string pszLogo;
        public UInt32 cchLogo;
    }

    /// <summary>
    /// A simple class to accommodate the parsed and analysed contents of the wamd
    /// metadata chunk from a .wav file.  The data in the chunk is identified by a
    /// numerical type and contents which should be a string.  The data structure
    /// holds items for each known type and getters return the contents by name or add
    /// contents by type.
    /// </summary>
    public class WAMD_Data
    {
#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member
        public string model { get; private set; }
#pragma warning restore CS1591 // Missing XML comment for publicly visible type or member
#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member
        public string version { get; private set; }
#pragma warning restore CS1591 // Missing XML comment for publicly visible type or member
#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member
        public string header { get; private set; }
#pragma warning restore CS1591 // Missing XML comment for publicly visible type or member
#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member
        public string timestamp { get; private set; }
#pragma warning restore CS1591 // Missing XML comment for publicly visible type or member
#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member
        public string source { get; private set; }
#pragma warning restore CS1591 // Missing XML comment for publicly visible type or member
#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member
        public string note { get; private set; }
#pragma warning restore CS1591 // Missing XML comment for publicly visible type or member
#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member
        public string identification { get; private set; }
#pragma warning restore CS1591 // Missing XML comment for publicly visible type or member
        public double duration { get; set; }

#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member

        public string comment
#pragma warning restore CS1591 // Missing XML comment for publicly visible type or member
        {
            get
            {
                string s = note + " " + identification;
                return (s.Trim());
            }
        }

#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member

        public Tuple<Int16, string> item
#pragma warning restore CS1591 // Missing XML comment for publicly visible type or member
        {
            set
            {
                switch (value.Item1)
                {
                    case 1:
                        model = value.Item2;
                        break;

                    case 3:
                        version = value.Item2;
                        break;

                    case 4: header = value.Item2; break;
                    case 5: timestamp = value.Item2; break;
                    case 12: identification = value.Item2; break;
                    case 10: note = value.Item2; break;
                    default: break;
                }
            }
        }

        public double? versionAsDouble { get; internal set; }

        /// <summary>
        /// initialises the data structure with empty strings throughout
        /// </summary>
        public WAMD_Data()
        {
            model = "";
            version = "";
            header = "";
            timestamp = "";
            source = "";
            note = "";
            identification = "";
        }
    }

    /*
    public static class DataGridExtensions
    {
        public static void ExportUsingReflection(this DataGridExtensions grid,ITypeLibExporterNameProvider exporter, string exportPath)
        {
            if (grid.ItemsSource == null || grid.Items.Count.Equals(0))
                throw new InvalidOperationException("You cannot export any data from an empty DataGrid.");

            IEnumerable<DataGridColumn> columns = grid.Columns.OrderBy(c => c.DisplayIndex);
            ICollectionView collectionView = CollectionViewSource.GetDefaultView(grid.ItemsSource);
            foreach (object o in collectionView)
            {
                if (o.Equals(CollectionView.NewItemPlaceholder))
                    continue;

                foreach (DataGridColumn column in columns)
                {
                    if(column is DataGridBoundColumn)
    {
                        string propertyValue = string.Empty;

                        // Get the property name from the column's binding
                        BindingBase bb = (column as DataGridBoundColumn).Binding;
                        if (bb != null)
                        {
                            Binding binding = bb as Binding;
                            if (binding != null)
                            {
                                string boundProperty = binding.Path.Path;

                                // Get the property value using reflection
                                PropertyInfo pi = o.GetType().GetProperty(boundProperty);
                                if (pi != null)
                                {
                                    object value = pi.GetValue(o);
                                    if (value != null)
                                        propertyValue = value.ToString();
                                    else if (column is DataGridCheckBoxColumn)
                                        propertyValue = "-";
                                }
                            }
                        }

                        exporter.AddColumn(propertyValue);
                    }
                }
                exporter.AddLineBreak();
            }
            // Create and open export file
            Process.Start(exporter.Export(exportPath));
        }
    }

    public interface IExporter
    {
        void AddColumn(string value);
        void AddLineBreak();
        string Export(string exportPath);
    }

    public class CsvExporter : IExporter
    {
        private readonly StringBuilder sb = new StringBuilder();
        private readonly string _delimiter;

        public CsvExporter(char delimiter)
        {
            _delimiter = delimiter.ToString();
        }

        public char Delimiter
        {
            get { return _delimiter[0]; }
        }

        public void AddColumn(string value)
        {
            sb.Append(value.Replace(_delimiter,
                string.Format("\"{0}\"", _delimiter)));
            sb.Append(_delimiter);
        }

        public void AddLineBreak()
        {
            sb.Remove(sb.Length - 1, 1); //remove trailing delimiter
            sb.AppendLine();
        }

        public string Export(string exportPath)
        {
            if (string.IsNullOrEmpty(exportPath))
            {
                Random rnd = new Random();
                exportPath = string.Format("{0}.csv", rnd.Next());
            }
            else if (!Path.GetExtension(exportPath).ToLower().Equals(".csv"))
            {
                throw new ArgumentException("Invalid file extension.", "exportPath");
            }

            File.WriteAllText(exportPath, sb.ToString().Trim());
            sb.Clear();
            return exportPath;
        }
    }*/

        /// <summary>
        /// static functions to operate on visual UI elements
        /// </summary>
    public static class UIHelper
    {
        /// <summary>
        /// Finds a parent of a given item on the visual tree.
        /// </summary>
        /// <typeparam name="T">The type of the queried item.</typeparam>
        /// <param name="child">A direct or indirect child of the queried item.</param>
        /// <returns>The first parent item that matches the submitted type parameter. 
        /// If not matching item can be found, a null reference is being returned.</returns>
        public static T FindVisualParent<T>(DependencyObject child)
          where T : DependencyObject
        {
            // get parent item
            DependencyObject parentObject = VisualTreeHelper.GetParent(child);

            // we’ve reached the end of the tree
            if (parentObject == null) return null;

            // check if the parent matches the type we’re looking for
            T parent = parentObject as T;
            if (parent != null)
            {
                return parent;
            }
            else
            {
                // use recursion to proceed with next level
                return FindVisualParent<T>(parentObject);
            }
        }
    }
}