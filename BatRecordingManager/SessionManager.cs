using Microsoft.VisualStudio.Language.Intellisense;
using System;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;

namespace BatRecordingManager
{
    /// <summary>
    ///     </summary>
    public static class SessionManager
    {
        internal static string GetSessionTag(FileBrowser fileBrowser)
        {
            String result = "";
            if (!String.IsNullOrWhiteSpace(fileBrowser.WorkingFolder))
            {
                Regex tagRegex = new Regex("-[a-zA-Z]+[0-9]+-{1}[0-9]+[a-zA-Z]*_+[0-9]{8}.*");
                Match match = tagRegex.Match(fileBrowser.WorkingFolder);
                if (match.Success)
                {
                    result = match.Value.Substring(1); // remove the surplus leading hyphen
                    if (result.EndsWith("\\")) // remove any trailing backslash
                    {
                        result = result.Substring(0, result.Length - 1);
                    }
                    while (result.Contains(@"\")) // tag may include parent folders as well as the lowest level folder so this removes leading folder names
                    {
                        result = result.Substring(result.IndexOf(@"\") + 1);
                    }
                }
            }
            return (result);
        }

        /// <summary>
        /// Uses the supplied gpxhandler to fill in the GPX co-ordinates for the supplied session
        /// </summary>
        /// <param name="session"></param>
        /// <param name="gpxHandler"></param>
        /// <returns></returns>
        internal static RecordingSession SetGPSCoordinates(RecordingSession session, GpxHandler gpxHandler)
        {
            if (session.LocationGPSLatitude == null || session.LocationGPSLatitude < 5.0m)
            {
                if (gpxHandler != null)
                {
                    var gpxLoc = gpxHandler.GetLocation(session.SessionDate);
                    if (gpxLoc != null && gpxLoc.Count == 2)
                    {
                        session.LocationGPSLatitude = gpxLoc[0];
                        session.LocationGPSLongitude = gpxLoc[1];
                    }
                }
            }
            return (session);
        }

        internal static RecordingSession PopulateSession(RecordingSession newSession, String headerFile, String SessionTag, BulkObservableCollection<String> wavFileFolders)
        {
            newSession.SessionTag = SessionTag;

            if (string.IsNullOrWhiteSpace(headerFile) || !File.Exists(headerFile))
            {
                return (new RecordingSession());
            }

            string workingFolder = headerFile.Substring(0, headerFile.LastIndexOf(@"\") + 1);
            newSession.OriginalFilePath = workingFolder;
            if (wavFileFolders.IsNullOrEmpty())
            {
                wavFileFolders = new BulkObservableCollection<string>
                {
                    workingFolder
                };
            }

            RecordingSession existingSession = DBAccess.GetRecordingSession(newSession.SessionTag);
            if (existingSession != null)
            {
                return (existingSession);
            }
            else
            {
                String[] headerFileLines = File.ReadAllLines(headerFile);
                if (headerFileLines != null)
                {
                    newSession = SessionManager.ExtractHeaderData(workingFolder, newSession.SessionTag, headerFileLines);
                    if (newSession.SessionDate.Year < 1950)
                    {
                        String dateRegex = @".*[-0-9]*(20[0-9]{6})[-0-9]*.*";
                        String folder = workingFolder;

                        var match = Regex.Match(folder, dateRegex);
                        if (match.Success)
                        {
                            newSession.SessionDate = getCompressedDate(match.Groups[1].Value);
                            newSession.EndDate = newSession.SessionDate;
                        }
                        else
                        {
                            if (!wavFileFolders.IsNullOrEmpty())
                            {
                                foreach (var wavfolder in wavFileFolders)
                                {
                                    match = Regex.Match(wavfolder, dateRegex);
                                    if (match.Success)
                                    {
                                        newSession.SessionDate = getCompressedDate(match.Groups[1].Value);
                                        newSession.EndDate = newSession.SessionDate;
                                        break;
                                    }
                                }
                            }
                            if (newSession.SessionDate.Year < 1950)
                            {
                                if (Directory.Exists(workingFolder))
                                {
                                    newSession.SessionDate = Directory.GetCreationTime(workingFolder);
                                    newSession.EndDate = newSession.SessionDate;
                                }
                            }
                        }
                    }
                }
                else
                {
                    // we can't get a header file so we need to fill in some fundamental defaults
                    // for the blank session.
                    if (!String.IsNullOrWhiteSpace(workingFolder) && Directory.Exists(workingFolder))
                    {
                        newSession.SessionDate = Directory.GetCreationTime(workingFolder);
                        newSession.EndDate = newSession.SessionDate;
                    }
                    newSession.SessionStartTime = new TimeSpan(18, 0, 0);
                    newSession.SessionEndTime = new TimeSpan(23, 0, 0);
                }
            }

            return (newSession);
        }

        /// <summary>
        /// Presents a RecordingSessionDialog to the user for filling in or amending and returns
        /// the amended session.
        /// </summary>
        /// <param name="newSession"></param>
        /// <param name="sessionTag"></param>
        /// <param name="folderPath"></param>
        /// <returns></returns>
        public static RecordingSession EditSession(RecordingSession newSession, string sessionTag, string folderPath)
        {
            if (string.IsNullOrWhiteSpace(newSession.SessionTag))
            {
                newSession.SessionTag = sessionTag;
            }
            if (newSession.SessionDate == null || newSession.SessionDate.Year < 2000)
            {
                newSession.SessionDate = SessionManager.GetDateFromTag(sessionTag);
                newSession.SessionStartTime = new TimeSpan(18, 0, 0);
            }
            if (newSession.EndDate == null || newSession.EndDate.Value.Year < 2000)
            {
                newSession.EndDate = newSession.SessionDate;
                newSession.SessionEndTime = new TimeSpan(23, 59, 59);
            }
            if (string.IsNullOrWhiteSpace(newSession.OriginalFilePath))
            {
                newSession.OriginalFilePath = folderPath;
            }
            TimeSpan start;
            TimeSpan end;
            if(SessionManager.GetTimesFromFiles(folderPath,sessionTag, out start, out end))
            {
                newSession.SessionStartTime = start;
                newSession.SessionEndTime = end;
            }

            RecordingSessionForm sessionForm = new RecordingSessionForm();

            sessionForm.SetRecordingSession(newSession);
            if (sessionForm.ShowDialog() ?? false)
            {
                newSession = sessionForm.GetRecordingSession();
                //DBAccess.UpdateRecordingSession(sessionForFolder);
                sessionTag = newSession.SessionTag;
                var existingSession = DBAccess.GetRecordingSession(sessionTag);
                if (existingSession != null)
                {
                    //DBAccess.DeleteSession(existingSession);
                    newSession.Id = existingSession.Id;
                }
                else
                {
                    newSession.Id = 0;
                }
            }
            else
            {
                newSession = null;// we hit Cancel in the form so nullify the entire process
            }
            return (newSession);
        }

        /// <summary>
        /// Tries to find a header file and if found tries to populate a new RecordingSession
        /// based on it.  Whether or no, then displays a RecordingSession Dialog for the user to
        /// populate and/or amend as required.  The RecordingSession is then saved to the
        /// database and the RecordingSessiontag is used to replace the current Sessiontag
        /// </summary>
        /// <returns></returns>
        internal static RecordingSession CreateSession(string folderPath, string sessionTag, GpxHandler gpxHandler)
        {
            if (gpxHandler == null)
            {
                gpxHandler = new GpxHandler(folderPath);
            }
            BulkObservableCollection<String> folderList = new BulkObservableCollection<string>();
            folderList.Add(folderPath);
            RecordingSession newSession = new RecordingSession();
            String headerFile = SessionManager.GetHeaderFile(folderPath);
            if (string.IsNullOrWhiteSpace(sessionTag))
            {
                sessionTag = SessionManager.CreateTag(folderPath);
            }
            newSession = SessionManager.FillSessionFromHeader(headerFile, sessionTag,folderList);
            newSession.OriginalFilePath = folderPath;
            newSession = SessionManager.SetGPSCoordinates(newSession, gpxHandler);
            newSession = SessionManager.EditSession(newSession, sessionTag, folderPath);
            if (newSession == null) return (null);
            newSession = SessionManager.SaveSession(newSession);
            sessionTag = newSession.SessionTag;



            return (newSession);
        }

        private static string CreateTag(string folderPath)
        {
            string result = Guid.NewGuid().ToString();
            if (!string.IsNullOrWhiteSpace(folderPath))
            {
                string path = Tools.GetPath(folderPath);
                if (path.Trim().EndsWith(@"\")) {
                    path = path.Substring(0, path.Length - 1);
                }
                if (path.Contains(@"\"))
                {
                    path = path.Substring(path.LastIndexOf('\\'));
                    if (path.StartsWith(("\\")))
                    {
                        path = path.Substring(1);
                    }
                }
                int i = 0;
                result = path;
                while(DBAccess.SessionTagExists(path))
                {
                    path = result +"-"+ (i++).ToString();
                }
                result = path;

            }
            return (result);
        }

        /// <summary>
        /// Saves the recordingSession to the database
        /// </summary>
        /// <param name="newSession"></param>
        /// <returns></returns>
        public static RecordingSession SaveSession(RecordingSession newSession)
        {
            DBAccess.UpdateRecordingSession(newSession);
            DBAccess.ResolveOrphanImages();

            return (DBAccess.GetRecordingSession(newSession.SessionTag));
        }

        /// <summary>
        /// Looks for a header text file in the selected folder which starts with a [COPY]
        /// directive.
        /// </summary>
        /// <returns></returns>
        public static String GetHeaderFile(string folderPath)
        {
            var listOfTxtFiles = Directory.EnumerateFiles(folderPath, "*.txt", SearchOption.TopDirectoryOnly);
            //var listOfTXTFiles= Directory.EnumerateFiles(folderPath, "*.TXT", SearchOption.TopDirectoryOnly);
            //listOfTxtFiles = listOfTxtFiles.Concat<string>(listOfTXTFiles);
            if (!listOfTxtFiles.IsNullOrEmpty())
            {
                foreach (var file in listOfTxtFiles)
                {
                    if (File.Exists(file))
                    {
                        var lines = File.ReadLines(file);
                        if (!lines.IsNullOrEmpty())
                        {
                            var line = lines.First();
                            if (line.Contains("[COPY]"))
                            {
                                return (file);
                            }
                        }
                    }
                }
            }
            return (null);
        }

        /// <summary>
        /// Uses the header file to try and populate a new recordingSession,
        /// otherwise returns a new RecordingSession;
        /// </summary>
        /// <param name="headerFile"></param>
        /// <returns></returns>
        public static RecordingSession FillSessionFromHeader(string headerFile, string sessionTag,BulkObservableCollection<String> wavFileFolders=null)
        {
            
            RecordingSession recordingSession = new RecordingSession();
            recordingSession.SessionTag = sessionTag;
            if (!String.IsNullOrWhiteSpace(headerFile) && File.Exists(headerFile))
            {
                recordingSession = SessionManager.PopulateSession(recordingSession, headerFile, sessionTag, wavFileFolders);
            }

            return (recordingSession);
        }

        /// <summary>
        ///     Populates the session.
        /// </summary>
        /// <param name="newSession">
        ///     The new session.
        /// </param>
        /// <param name="fileBrowser">
        ///     The file browser.
        /// </param>
        /// <returns>
        ///     </returns>
        internal static RecordingSession PopulateSession(RecordingSession newSession, FileBrowser fileBrowser)
        {
            string SessionTag = SessionManager.GetSessionTag(fileBrowser);
            string headerFile = fileBrowser.HeaderFileName;

            return (PopulateSession(newSession, headerFile, SessionTag, fileBrowser.wavFileFolders));
        }

        /// <summary>
        ///     Extracts the header data. Makes a best guess attempt to populate a RecordingSession
        ///     instance from a header file.
        /// </summary>
        /// <param name="folder"></param>
        /// <param name="sessionTag">
        ///     The session tag.
        /// </param>
        /// <param name="headerFile">
        ///     The header file.
        /// </param>
        /// <returns>
        ///     </returns>
        /// <exception cref="System.NotImplementedException">
        ///     </exception>
        private static RecordingSession ExtractHeaderData(string folder, string sessionTag, string[] headerFile)
        {
            string wavFile = "";
            if (Directory.Exists(folder))
            {
                var wavFiles = Directory.EnumerateFiles(folder, "*.wav");
                //var WAVFiles= Directory.EnumerateFiles(folder, "*.WAV");
                //wavFiles = wavFiles.Concat<string>(WAVFiles);
                
                if(wavFiles!=null && wavFiles.Count() > 0)
                {
                    wavFile = wavFiles.First();
                }
            }

            WavFileMetaData wfmd = null;

            if (!String.IsNullOrWhiteSpace(wavFile))
            {
                if (File.Exists(wavFile))
                {
                    wfmd = new WavFileMetaData(wavFile);
                }
            }

            RecordingSession session = new RecordingSession();
            session.SessionTag = sessionTag;
            //Tuple<DateTime, DateTime?> sessionDatesAndTimes = SessionManager.GetDateAndTimes(headerFile, sessionTag);
            //session.SessionDate = SessionManager.GetDate(headerFile, sessionTag);
            TimeSpan StartTime = new TimeSpan();
            TimeSpan EndTime = new TimeSpan();
            TimeSpan Sunset = new TimeSpan();
            DateTime StartDateTime = new DateTime();
            DateTime EndDateTime = new DateTime();
            SessionManager.GetTimes(folder, sessionTag, headerFile,wfmd, out StartDateTime, out EndDateTime, out Sunset);
            session.SessionStartTime = StartDateTime.TimeOfDay;
            session.SessionEndTime = EndDateTime.TimeOfDay;
            session.SessionDate = StartDateTime;
            session.EndDate = EndDateTime;
            session.Sunset = Sunset;
            session.Temp = SessionManager.GetTemp(headerFile,wfmd);
            session.Equipment = SessionManager.GetEquipment(headerFile,wfmd);
            session.Microphone = SessionManager.GetMicrophone(headerFile,wfmd);
            session.Operator = SessionManager.GetOperator(headerFile);
            session.Location = SessionManager.GetLocation(headerFile);
            decimal? Longitude = null;
            decimal? Latitude = null;
            if (SessionManager.GetGPSCoOrdinates(headerFile,wfmd, out Latitude, out Longitude))
            {
                session.LocationGPSLongitude = Longitude;
                session.LocationGPSLatitude = Latitude;
                if (Sunset.Hours == 0 && Longitude != null && Latitude != null)
                {
                    session.Sunset = CalculateSunset(session.SessionDate, Latitude, Longitude);
                }
            }
            session.SessionNotes = "";

            foreach (var line in headerFile)
            {
                session.SessionNotes = session.SessionNotes + line + "\n";
            }
            if (wfmd != null)
            {
                session.SessionNotes += wfmd.FormattedText();
            }
            return (session);
        }

#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member

        public static TimeSpan? CalculateSunset(DateTime sessionDate, decimal? latitude, decimal? longitude)
#pragma warning restore CS1591 // Missing XML comment for publicly visible type or member
        {
            TimeSpan? Sunset = new TimeSpan();
            DateTime dtSunrise = new DateTime();
            DateTime dtSunset = new DateTime();
            bool isSunrise = false;
            bool isSunset = false;

            if (latitude == null || longitude == null || Math.Abs((double)latitude) > 90.0 || Math.Abs((double)longitude) > 180.0 || sessionDate.Year < 1900)
            {
                return (Sunset);
            }

            if (SunTimes.Instance.CalculateSunRiseSetTimes((double)latitude.Value, (double)longitude.Value, sessionDate, ref dtSunrise, ref dtSunset, ref isSunrise, ref isSunset))
            {
                if (isSunset)
                {/*
                    if(dtSunset.IsDaylightSavingTime()){
                        dtSunset = dtSunset.AddHours(1);
                    }*/
                    Sunset = dtSunset.TimeOfDay;
                }
            }
            else
            {
                Tools.ErrorLog("Failed to calculate Sunset");
            }
            return (Sunset);
        }

        /// <summary>
        ///     Gets the compressed date. Given a date in the format yyyymmdd returns the
        ///     corresponding DateTime
        /// </summary>
        /// <param name="group">
        ///     The group.
        /// </param>
        /// <returns>
        ///     </returns>
        /// <exception cref="System.NotImplementedException">
        ///     </exception>
        private static DateTime getCompressedDate(String group)
        {
            if (String.IsNullOrWhiteSpace(group))
            {
                return (new DateTime());
            }
            if (group.Length != 8)
            {
                return (new DateTime());
            }
            int year = 0;
            int month = 0;
            int day = 0;
            int.TryParse(group.Substring(0, 4), out year);
            int.TryParse(group.Substring(4, 2), out month);
            int.TryParse(group.Substring(6, 2), out day);
            if (year < DateTime.Now.Year && month > 0 && month <= 12 && day > 0 && day <= 31)
            {
                return (new DateTime(year, month, day));
            }
            return (new DateTime());
        }

        /// <summary>
        ///     Gets the date.
        /// </summary>
        /// <param name="headerFile">
        ///     The header file.
        /// </param>
        /// <param name="sessionTag">
        ///     The session tag.
        /// </param>
        /// <returns>
        ///     </returns>
        private static DateTime GetDate(string[] headerFile, string sessionTag)
        {
            if (!String.IsNullOrWhiteSpace(sessionTag))
            {
                return (GetDateFromTag(sessionTag));
            }
            DateTime result = new DateTime();
            string pattern = @"[0-9]+\s*[a-zA-Z]+\s*(20){0,1}[0-9]{2}";
            foreach (var line in headerFile)
            {
                var match = Regex.Match(line, pattern);
                if (match.Success)
                {
                    DateTime.TryParse(match.Value, out result);
                    break;
                }
            }
            return (result);
        }

        /// <summary>
        ///     Gets the date from tag.
        /// </summary>
        /// <param name="sessionTag">
        ///     The session tag.
        /// </param>
        /// <returns>
        ///     </returns>
        private static DateTime GetDateFromTag(string sessionTag)
        {
            Regex tagRegex = new Regex("([a-zA-Z0-9-]+)(_+)([0-9]{4}).?([0-9]{2}).?([0-9]{2})");
            DateTime result = new DateTime();
            Match match = tagRegex.Match(sessionTag);
            if (match.Success)
            {
                if (match.Groups.Count == 6)
                {
                    int day;
                    int month;
                    int year;

                    int.TryParse(match.Groups[5].Value, out day);
                    int.TryParse(match.Groups[4].Value, out month);
                    int.TryParse(match.Groups[3].Value, out year);
                    result = new DateTime(year, month, day);
                }
            }

            return (result);
        }

        /// <summary>
        ///     Gets the equipment.
        /// </summary>
        /// <param name="headerFile">
        ///     The header file.
        /// </param>
        /// <returns>
        ///     </returns>
        private static string GetEquipment(string[] headerFile,WavFileMetaData wfmd=null)
        {
            if(wfmd!=null && wfmd.m_Device != null)
            {
                return (wfmd.m_Device);
            }
            if (headerFile == null || headerFile.Count() <= 0) return ("");
            BulkObservableCollection<String> knownEquipment = DBAccess.GetEquipmentList();
            if (knownEquipment == null || knownEquipment.Count <= 0) return ("");
            // get a line in the text containing a known operator
            var matchingEquipment = headerFile.Where(line => knownEquipment.Any(txt => line.ToUpper().Contains(txt == null ? "none" : txt.ToUpper())));
            if (!matchingEquipment.IsNullOrEmpty())
            {
                return (matchingEquipment.First());
            }
            return ("");
        }

        /// <summary>
        ///     Gets the GPS co ordinates.
        /// </summary>
        /// <param name="headerFile">
        ///     The header file.
        /// </param>
        /// <param name="latitude">
        ///     The latitude.
        /// </param>
        /// <param name="longitude">
        ///     The longitude.
        /// </param>
        private static bool GetGPSCoOrdinates(string[] headerFile,WavFileMetaData wfmd, out decimal? latitude, out decimal? longitude)
        {
            if(wfmd!=null && wfmd.m_Location != null)
            {
                latitude = (decimal)(wfmd.m_Location.m_Latitude);
                longitude = (decimal)(wfmd.m_Location.m_Longitude);
                if(latitude<200.0m && longitude < 200.0m)
                {
                    return (true);
                }
            }
            //Regex gpsRegex = new Regex();
            bool result = false;
            if (headerFile == null)
            {
                latitude = null;
                longitude = null;
                return (result);
            }
            latitude = null;
            longitude = null;
            string pattern = @"(-?[0-9]{1,}\.[0-9]{1,})\s*,\s*(-?[0-9]{1,2}\.[0-9]{1,})";
            foreach (var line in headerFile)
            {
                var match = Regex.Match(line, pattern);
                if (match.Success && match.Groups.Count > 2)
                {
                    decimal value = 0.0m;
                    if (decimal.TryParse(match.Groups[1].Value, out value))
                    {
                        latitude = value;
                    }

                    value = 0.0m;
                    if (decimal.TryParse(match.Groups[2].Value, out value))
                    {
                        longitude = value;
                    }
                    break;
                }
            }
            if (latitude != null && longitude != null)
            {
                result = true;
            }
            return (result);
        }

        /// <summary>
        ///     Gets the location.
        /// </summary>
        /// <param name="headerFile">
        ///     The header file.
        /// </param>
        /// <returns>
        ///     </returns>
        private static string GetLocation(string[] headerFile)
        {
            if (headerFile == null) return ("");
            if (headerFile.Count() > 1)
            {
                return (headerFile[1]);
            }
            return ("");
        }

        /// <summary>
        ///     Gets the microphone.
        /// </summary>
        /// <param name="headerFile">
        ///     The header file.
        /// </param>
        /// <returns>
        ///     </returns>
        private static string GetMicrophone(string[] headerFile,WavFileMetaData wfmd=null)
        {
            if(wfmd!=null && wfmd.m_Microphone != null)
            {
                return (wfmd.m_Microphone);
            }
            if (headerFile == null || headerFile.Count() <= 0) return ("");
            BulkObservableCollection<String> knownMicrophones = DBAccess.GetMicrophoneList();
            if (knownMicrophones == null || knownMicrophones.Count <= 0) return ("");
            // get a line in the text containing a known operator
            var mm = from line in headerFile
                     join mic in knownMicrophones on line equals mic
                     select mic;

            var matchingMicrophones = headerFile.Where(line => knownMicrophones.Any(txt => line.ToUpper().Contains(txt == null ? "none" : txt.ToUpper())));
            if (!matchingMicrophones.IsNullOrEmpty())
            {
                return (matchingMicrophones.First());
            }
            return ("");
        }

        /// <summary>
        ///     Gets the operator.
        /// </summary>
        /// <param name="headerFile">
        ///     The header file.
        /// </param>
        /// <returns>
        ///     </returns>
        private static string GetOperator(string[] headerFile)
        {
            if (headerFile == null || headerFile.Count() <= 0) return ("");
            BulkObservableCollection<String> knownOperators = DBAccess.GetOperators();
            if (knownOperators == null || knownOperators.Count <= 0) return ("");
            // get a line in the text containing a known operator
            var matchingOperators = headerFile.Where(line => knownOperators.Any(txt => line.ToUpper().Contains(txt == null ? "none" : txt.ToUpper())));
            if (!matchingOperators.IsNullOrEmpty())
            {
                return (matchingOperators.First());
            }
            return ("");
        }

        /// <summary>
        ///     Gets the temporary.
        /// </summary>
        /// <param name="headerFile">
        ///     The header file.
        /// </param>
        /// <returns>
        ///     </returns>
        private static short? GetTemp(string[] headerFile,WavFileMetaData wfmd=null)
        {
            short temp = 0;
            if(wfmd!=null && wfmd.m_Temperature != null)
            {
                if (short.TryParse(wfmd.m_Temperature,out temp))
                {
                    return (temp);
                }
            }
            if (headerFile == null) return (0);
            
            string pattern = @"([0-9]{1,2})\s*[C\u00B0]{1}";
            foreach (var line in headerFile)
            {
                var match = Regex.Match(line, pattern);
                if (match.Success)
                {
                    short.TryParse(match.Groups[1].Value, out temp);
                    break;
                }
            }
            return (temp);
        }

        /// <summary>
        ///     Revised to cope with either a line with a date and start
        ///     and end times, or a line with two date-time pairs for the
        ///     start and end of the session
        ///     Also checks for a line containing Sunset and a time
        /// </summary>
        /// <param name="folder"></param>
        /// <param name="sessionTag"></param>
        /// <param name="headerFile">
        ///     The header file.
        /// </param>
        /// <param name="wfmd"></param>
        /// <param name="startTime">
        ///     The start time.
        /// </param>
        /// <param name="endTime">
        ///     The end time.
        /// </param>
        /// <param name="sunset"></param>
        private static void GetTimes(string folder, string sessionTag, string[] headerFile,WavFileMetaData wfmd, out DateTime startTime, out DateTime endTime, out TimeSpan sunset)
        {
            startTime = new DateTime();
            endTime = new DateTime();

            if(wfmd!=null && wfmd.m_Start != null)
            {
                startTime = wfmd.m_Start.Value;
            }
            if(wfmd!=null && wfmd.m_End != null)
            {
                endTime = wfmd.m_End.Value;
            }
            sunset = new TimeSpan();
            if (headerFile == null) return;
            BulkObservableCollection<TimeSpan> times = new BulkObservableCollection<TimeSpan>();
            if (times != null)
            {
                string FormattedTimePattern = @"\d{1,2}:\d{2}:{0,1}\d{0,2}";
                foreach (var line in headerFile)
                {
                    if (line.ToUpper().Contains("SUNSET"))
                    {
                        string timepart = line.Substring(line.ToUpper().IndexOf("SUNSET") + 6);
                        var match = Regex.Match(timepart, FormattedTimePattern);
                        if (match.Success)
                        {
                            TimeSpan ts = new TimeSpan();
                            if (TimeSpan.TryParse(match.Value, out ts))
                            {
                                sunset = ts;
                            }
                        }
                    }
                    else
                    {
                        // we have a line which is not a sunset line
                        var matchingTimes = Regex.Matches(line, FormattedTimePattern);
                        if (matchingTimes.Count == 2)
                        {
                            // we have at two time fields in the line
                            var segments = line.Split('-');
                            if (segments.Count() == 2)
                            {
                                // we have a start hyphen end format
                                DateTime date = new DateTime();
                                TimeSpan time = new TimeSpan();
                                if (!DateTime.TryParse(segments[0].Trim(), out date))
                                {
                                    date = SessionManager.GetDateFromFileName(folder);
                                }

                                if (TimeSpan.TryParse(matchingTimes[0].Value, out time))
                                {
                                    startTime = date.Date + time;
                                }

                                date = date.Date;
                                DateTime.TryParse(segments[1].Trim(), out date);
                                if (TimeSpan.TryParse(matchingTimes[1].Value, out time))
                                {
                                    if (date.Date == DateTime.Now.Date)
                                    {
                                        endTime = startTime.Date + time;
                                    }
                                    else
                                    {
                                        endTime = date.Date + time;
                                    }
                                }
                            }
                        }
                    }
                }
            }

            
            
        }

        private static DateTime GetDateFromFileName(string folder)
        {
            if (String.IsNullOrWhiteSpace(folder)) return (new DateTime());
            String DatePattern = @"(%d{4}).?(%d{2}).?(%d{2})";
            var match = Regex.Match(folder, DatePattern);
            if (!match.Success)
            {
                if (Directory.Exists(folder))
                {
                    var files = Directory.EnumerateFiles(folder);
                    if (files != null && files.Any())
                    {
                        foreach (var file in files)
                        {
                            match = Regex.Match(file, DatePattern);
                            if (match.Success) break;
                        }
                    }
                }
            }
            if (match.Success)
            {
                int year = -1;
                int month = -1;
                int day = -1;
                int.TryParse(match.Groups[1].Value, out year);
                int.TryParse(match.Groups[2].Value, out month);
                int.TryParse(match.Groups[3].Value, out day);
                return (new DateTime(year, month, day));
            }
            return (new DateTime());
        }

        /// <summary>
        /// uses the times of files in the specified folder to guess at session
        /// start and end times.  Looks for .wav files with the same date as the
        /// date included in the tag, then assumes a start at 4 minutes before the
        /// time of the earlieast wav file and an end of the time of the last wav file.
        /// </summary>
        /// <param name="folder"></param>
        /// <param name="sessionTag"></param>
        /// <param name="startTime"></param>
        /// <param name="endTime"></param>
        private static bool GetTimesFromFiles(string folder, string sessionTag, out TimeSpan startTime, out TimeSpan endTime)
        {
            startTime = new TimeSpan();
            endTime = startTime;
            if (string.IsNullOrWhiteSpace(folder)) return (false);
            DateTime sessiondate = SessionManager.GetDateFromTag(sessionTag);

            try
            {
                var wavFiles = Directory.EnumerateFiles(folder, @"*.wav");
                //var WAVFiles = Directory.EnumerateFiles(folder, "*.WAV");


               // wavFiles = wavFiles.Concat(WAVFiles);



            if (sessiondate != null && sessiondate.Year > 2000)
                {
                    wavFiles = from file in wavFiles
                               where File.GetLastWriteTime(file).Date == sessiondate.Date
                               select file;
                }
                wavFiles = from file in wavFiles
                           orderby File.GetLastWriteTime(file)
                           select file;
                startTime = File.GetLastWriteTime(wavFiles.First()).TimeOfDay - new TimeSpan(0, 4, 0);
                endTime = File.GetLastWriteTime(wavFiles.Last()).TimeOfDay;
            }
            catch (Exception)
            {
                return (false);
            }
            return (true);
        }
    }
}