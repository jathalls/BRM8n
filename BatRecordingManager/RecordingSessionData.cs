using System;
using System.ComponentModel;

namespace BatRecordingManager
{
    /// <summary>
    /// Specialised class to hold the data displayed by RecordingSessionListDetailControl.
    /// Contains just the necessary items to display to allow for fast loading
    /// </summary>
    public class RecordingSessionData : INotifyPropertyChanged
    {
        public RecordingSessionData(int ID, string tag, string loc, DateTime startDate, TimeSpan? startTime, int numImages, int numRecordings)
        {
            this.Id = ID;
            this.SessionTag = tag;
            this.Location = loc;
            this.SessionStartDate = startDate;
            this.StartTime = startTime;
            this.NumberOfRecordingImages = numImages;
            this.NumberOfRecordings = numRecordings;
        }
        public RecordingSessionData()
        {
            this.Id = -1;
            this.SessionTag = "";
            this.Location = "";
            this.SessionStartDate = new DateTime();
            this.StartTime = null;
            this.NumberOfRecordingImages = 0;
            this.NumberOfRecordings = 0;
        }

        public event PropertyChangedEventHandler PropertyChanged;

        public int Id
        {
            get { return _Id; }
            set { _Id = value; pc("Id"); }
        }

        /// <summary>
        /// Location of the session
        /// </summary>
        public string Location
        {
            get { return _Location; }
            set { _Location = value; pc("Location"); }
        }

        /// <summary>
        /// The number of images associated with recordings of this session
        /// </summary>
        public int? NumberOfRecordingImages
        {
            get { return _NumberOfRecordingImages; }
            set { _NumberOfRecordingImages = value; pc("NumberOfRecordingImages"); }
        }

        /// <summary>
        /// The number of recordings that are part of this session
        /// </summary>
        public int NumberOfRecordings
        {
            get { return _NumberOfRecordings; }
            set { _NumberOfRecordings = value; pc("NumberOfRecordings"); }
        }

        /// <summary>
        /// Date and time at which the session started
        /// </summary>
        public DateTime SessionStartDate
        {
            get { return _SessionStartDate; }
            set { _SessionStartDate = value; pc("SessionStartDate"); }
        }

        /// <summary>
        /// Tag from the RecordingSession
        /// </summary>
        public String SessionTag
        {
            get { return _SessionTag; }
            set { _SessionTag = value; pc("SessionTag"); }
        }         /// <summary>

                  /// The optional time of the start of the session
                  /// </summary>
        public TimeSpan? StartTime
        {
            get { return _StartTime; }
            set { _StartTime = value; pc("StartTime"); }
        }

        private int _Id = -1;
        private string _Location;
        private int? _NumberOfRecordingImages;
        private int _NumberOfRecordings;
        private DateTime _SessionStartDate;
        private String _SessionTag;
        private TimeSpan? _StartTime;

        private void pc(String property)
        {
            if (PropertyChanged != null)
            {
                PropertyChanged(this, new PropertyChangedEventArgs(property));
            }
        }
    }
}