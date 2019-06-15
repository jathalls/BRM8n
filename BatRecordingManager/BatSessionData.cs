using System;

namespace BatRecordingManager
{
    /// <summary>
    /// Data to display in the SessionsAndRecordingsControl Sessions data grid
    /// </summary>
    public class BatSessionData
    {
        /// <summary>
        /// Creator and data initializer
        /// </summary>
        /// <param name="sessionId"></param>
        /// <param name="sessiontag"></param>
        /// <param name="location"></param>
        /// <param name="sessionDate"></param>
        /// <param name="startTime"></param>
        /// <param name="BatId"></param>
        /// <param name="batName"></param>
        /// <param name="numImages"></param>
        /// <param name="numrecordings"></param>
        public BatSessionData(int sessionId, string sessiontag, string location, DateTime sessionDate,
                    TimeSpan? startTime, int BatId, string batName, int numImages, int numrecordings)
        {
            this.id = sessionId;
            this.SessionTag = sessiontag;
            this.Location = location;
            this.SessionDate = sessionDate;
            this.SessionStartTime = startTime;
            this.BatId = BatId;
            this.BatName = batName;
            this.ImageCount = numImages;
            this.BatRecordingsCount = numrecordings;
        }

        /// <summary>
        /// Id of the parent window's bat that this session relates to
        /// </summary>
        public int BatId { get; set; }

        /// <summary>
        /// Name of the bat that this session relates to
        /// </summary>
        public string BatName { get; set; }

        /// <summary>
        /// The number of recordings that include this bat in this session
        /// </summary>
        public int BatRecordingsCount { get; set; }

        /// <summary>
        /// ID of the session
        /// </summary>
        public int id { get; set; } = -1;

        /// <summary>
        /// Number of recording images associated with this bat in this session
        /// </summary>
        public int ImageCount { get; set; }

        /// <summary>
        /// Location of the session
        /// </summary>
        public string Location { get; set; }

        /// <summary>
        /// Date of the session
        /// </summary>
        public DateTime SessionDate { get; set; }

        /// <summary>
        /// Start time of the session
        /// </summary>
        public TimeSpan? SessionStartTime { get; set; }

        /// <summary>
        /// tag of the session
        /// </summary>
        public string SessionTag { get; set; }
    }

    /***************************************************************************************************************/
    /**************************************************************************************************************/

    /// <summary>
    /// Class to hold display specific data for the RecordingsDataGrid in SessionsAndRecordings control
    /// </summary>
    public class BatSessionRecordingData
    {
        /// <summary>
        /// Default creator and data initializer
        /// </summary>
        public BatSessionRecordingData(int? sessionId, int recordingId, int batId, string recordingName, DateTime? startDate,
            TimeSpan? startTime, int segments, int images)
        {
            this.SessionId = sessionId;
            this.RecordingId = recordingId;
            this.BatId = batId;
            this.RecordingName = recordingName;
            this.RecordingDate = startDate;
            this.RecordingStartTime = startTime;
            this.SegmentCountForBat = segments;
            this.ImageCount = images;
        }

        public BatSessionRecordingData() { }

        /// <summary>
        /// The bat to which this data relates
        /// </summary>
        public int BatId { get; set; } = -1;

        /// <summary>
        /// The number of images in this recording which relate to segments featuring this bat
        /// or which relate to 0-time segments of this recording - i.e. the recording as a whole
        /// </summary>
        public int ImageCount { get; set; }

        /// <summary>
        /// The date on which the recording was made
        /// </summary>
        public DateTime? RecordingDate { get; set; }

        /// <summary>
        /// The specific recording to which this inastance relates
        /// </summary>
        public int? RecordingId { get; set; } = -1;

        /// <summary>
        /// The name of the recording
        /// </summary>
        public string RecordingName { get; set; }

        /// <summary>
        /// Time of the start of this recording
        /// </summary>
        public TimeSpan? RecordingStartTime { get; set; }

        /// <summary>
        /// The number of segments inn this recording which feature this bat
        /// </summary>
        public int SegmentCountForBat { get; set; }

        /// <summary>
        /// The Session to which this data relates
        /// </summary>
        public int? SessionId { get; set; } = -1;
    }
}