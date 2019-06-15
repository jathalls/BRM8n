using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;

namespace BatRecordingManager
{
    /// <summary>
    /// A class to represent a Guano metadata chunk included in a .wav file.
    /// The guano data should be extracted with Tools.Get_WAV_MetaData(wavfilename)
    /// and the string can be parsed with ParseGuanoChunk(string chunk) which will
    /// return an instance of this class.
    /// The class copes with a number of known fields for GUANO data according to
    /// specification 1.0 but is intended to work with GUANO data embedded by
    /// the Android App BatRecorder.
    /// </summary>
    public class Guano_deprecated
    {
        private WAMD_Data wamd_data = null;

        private byte[] metdata = null;

        /// <summary>
        /// Constructor for the Guano class.  May be initialized with either the name
        /// and path of a .wav file which has a Guano metadata section, or with the text of a
        /// Guano Metadata chunk as returned by Tools.Get_WAV_MetaData(wavfile)
        /// </summary>
        /// <param name="GuanoText"></param>
        public Guano_deprecated(string GuanoText)
        {
            byte[] metadata = null;
            this.wamd_data = null;
            this.rawText = "";
            
        }

        /// <summary>
        /// reads and loads the metadata from a string
        /// </summary>
        /// <param name="GuanoText"></param>
        public void SetMetaData(string GuanoText)
        {
            byte[] metadata = null;
            this.wamd_data = null;
            this.rawText = "";
            if (GuanoText.Trim().ToUpper().EndsWith(".WAV"))
            {
                WAMD_Data wamd_data = null;
                
                Get_WAVFile_MetaData(GuanoText, out metadata, out wamd_data);
                this.wamd_data = wamd_data;
            }
            else
            {
                rawText = GuanoText;
            }
            if (!string.IsNullOrWhiteSpace(rawText))
            {
                lines = rawText.Split('\n');
            }
        }

        /// <summary>
        /// Reads the wamd metadata chunk from a .wav file and converts it into the
        /// equivalent of an array of lines read from an Audacity comment file.
        /// </summary>
        /// <param name="fileName"></param>
        /// <param name="wamd_data"></param>
        /// <returns></returns>
        internal  string[] ReadMetadata(string fileName, out WAMD_Data wamd_data)
        {
            byte[] metadata = null;
            //WAMD_Data wamd_data = null;
            string comment = "start - end " + Get_WAVFile_MetaData(fileName, out metadata, out wamd_data);
            string[] result = new string[1];
            result[0] = comment;

            return (result);
        }

        /// <summary>
        /// Retrieves the metadata sections from a .wav file for either WAMD or GUANO formatted data.
        /// The file from which to extract the data is wavFilename and the metadata chunk itself is returned as
        /// a byte[] called metdata.  Formatted versions of the data are returned in the out parameters wamd_data
        /// and guano_data.  If not present in that format the classes will be returned empty.
        /// The function returns a string comprising the metadate note section followed by a ; followed by the manual
        /// species identification string and an optional auto-identification string in brackets.
        /// the data out parameters will be null if not found.
        /// </summary>
        /// <param name="WavFilename"></param>
        /// <param name="metadata"></param>
        /// <param name="wamd_data"></param>
        /// <param name="guano_data"></param>
        /// <returns></returns>
        public  string Get_WAVFile_MetaData(string WavFilename, out byte[] metadata, out WAMD_Data wamd_data)
        {
            metadata = null;
            string result = "";
            wamd_data = new WAMD_Data();
            
            if (String.IsNullOrWhiteSpace(WavFilename)) return (result);
            if (!WavFilename.Trim().ToUpper().EndsWith(".WAV")) return (result);
            try
            {
                using (FileStream fs = File.Open(WavFilename, FileMode.Open))
                {
                    BinaryReader reader = new BinaryReader(fs);

                    // chunk 0
                    int chunkID = reader.ReadInt32(); //RIFF
                    int fileSize = reader.ReadInt32(); // 4 bytes of size
                    int riffType = reader.ReadInt32(); //WAVE

                    // chunk 1
                    int fmtID = reader.ReadInt32(); //fmt_
                    int fmtSize = reader.ReadInt32(); // bytes for this chunk typically 16
                    int fmtCode = reader.ReadInt16(); // typically 1
                    int channels = reader.ReadInt16(); // 1 or 2
                    int sampleRate = reader.ReadInt32(); //
                    int byteRate = reader.ReadInt32();
                    int fmtBlockAlign = reader.ReadInt16();// 4
                    int bitDepth = reader.ReadInt16();//16

                    if (fmtSize == 18) // not expected for .wav files
                    {
                        // Read any extra values
                        int fmtExtraSize = reader.ReadInt16();
                        reader.ReadBytes(fmtExtraSize);
                    }
                    byte[] header = new byte[4];
                    byte[] data;
                    int dataBytes = 0;
                    // WAMD_Data wamd_data = new WAMD_Data();
                    result = "";
                    try
                    {
                        metadata = null;
                        wamd_data = null;
                        
                        do
                        {
                            header = reader.ReadBytes(4);
                            if (header == null || header.Length != 4) break;
                            int size = reader.ReadInt32();
                            data = reader.ReadBytes(size);
                            String strHeader = System.Text.Encoding.UTF8.GetString(header);
                            if (strHeader == "data")
                            {
                                dataBytes = size;
                            }
                            if (strHeader == "wamd")
                            {
                                metadata = data;
                                wamd_data = decode_wamd_data(metadata);
                                result = (wamd_data.note + "; " + wamd_data.identification).Trim();
                                break;
                            }
                            if (strHeader == "guan" && data != null)
                            {
                                metadata = data;
                                result = System.Text.Encoding.UTF8.GetString(data);
                                //decodeGuanoData(result);
                                result = this.note.Trim() + "; " + this.speciesManualID.Trim() + (String.IsNullOrWhiteSpace(this.speciesAutoID) ? "(" + this.speciesAutoID.Trim() + ")" : "");
                                break;
                            }
                        } while (reader.BaseStream.Position != reader.BaseStream.Length);
                    }
                    catch (IOException iox)
                    {
                        Tools.ErrorLog(iox.Message);
                        Debug.WriteLine("Error reading wav file:- " + iox.Message);
                    }

                    double durationInSecs = 0.0d;
                    if (byteRate > 0 && channels > 0 && dataBytes > 0)
                    {
                        durationInSecs = ((double)dataBytes / byteRate);
                        this.duration = TimeSpan.FromSeconds(durationInSecs);
                        if (wamd_data != null) wamd_data.duration = durationInSecs;
                        result = result + @"
Duration: " + new TimeSpan((long)(durationInSecs * 10000000L)).ToString(@"hh\:mm\:ss\.ff");
                    }
                }
            }
            catch (Exception ex)
            {
                Tools.ErrorLog(ex.Message);
                Debug.WriteLine(ex);
            }

            return (result);
        }

        /// <summary>
        /// Given a 'chunk' of metadata from a wav file wamd chunk
        /// which is everything after the wamd header and size attribute,
        /// extracts the Name and Note fields and assembles them into a
        /// 'pseudo'Audacity comment label field using start and end for
        /// the time parameters.
        /// </summary>
        /// <param name="metadata"></param>
        /// <returns></returns>
        private static WAMD_Data decode_wamd_data(byte[] metadata)
        {
            List<Tuple<Int16, string>> entries = new List<Tuple<Int16, string>>();
            WAMD_Data result = new WAMD_Data();
            BinaryReader bReader = new BinaryReader(new MemoryStream(metadata));

            while (bReader.BaseStream.Position < bReader.BaseStream.Length)
            {
                Int16 type = bReader.ReadInt16();// 01 00
                Int32 size = bReader.ReadInt32(); // 03 00 00 00
                byte[] bData = bReader.ReadBytes(size);
                if (type > 0)
                {
                    try
                    {
                        string data = System.Text.Encoding.UTF8.GetString(bData);
                        entries.Add(new Tuple<Int16, string>(type, data));
                    }
                    catch (Exception ex)
                    {
                        Tools.ErrorLog(ex.Message);
                        Debug.WriteLine(ex);
                    }
                }
            }

            WAMD_Data wamd_data = new WAMD_Data();

            foreach (var entry in entries)
            {
                wamd_data.item = entry;
            }

            result = wamd_data;

            return (result);
        }

        public TimeSpan? duration
        {
            get
            {
                bool found = false;
                if (_duration == null)
                {
                    if (lines != null && lines.Count() > 0)
                    {
                        foreach (var line in lines)
                        {
                            if (line.StartsWith("Duration:"))
                            {
                                var parts = line.Split(' ');
                                if (parts != null && parts.Count() > 1)
                                {
                                    TimeSpan ts;
                                    if (TimeSpan.TryParse(parts[1].Trim(), out ts))
                                    {
                                        _duration = ts;
                                        found = true;
                                    }
                                }
                            }
                        }
                    }
                }
                else
                {
                    found = true;
                }
                

                return (_duration);
            }
            set
            {
                _duration = value;
            }
        }

        private TimeSpan? _duration = null;

#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member
        public string rawText;
#pragma warning restore CS1591 // Missing XML comment for publicly visible type or member
        private string[] lines;

        /// <summary>
        /// MAKER|Version:
        /// </summary>
        public double? version
        {
            get
            {
                bool found = false;
                if (_version == null)
                {
                    if (lines != null && lines.Count() > 0)
                    {
                        foreach (var line in lines)
                        {
                            if (line.StartsWith("GUANO|Version:"))
                            {
                                var parts = line.Split(':');
                                if (parts != null && parts.Count() > 1)
                                {
                                    double v;
                                    if (double.TryParse(parts[1].Trim(), out v))
                                    {
                                        _version = v;
                                        found = true;
                                    }
                                }
                            }
                        }
                    }
                }
                else { found = true; }

                if(!found && wamd_data != null)
                {
                    _version = wamd_data.versionAsDouble;
                }
                return (_version);
            }
        }

        internal  string GetGuanoData(int currentRecordingSessionId, string wavfile)
        {
            if (currentRecordingSessionId < 0) return ("");
            if (string.IsNullOrWhiteSpace(wavfile)) return ("");
            String SessionNotes = DBAccess.GetRecordingSessionNotes(currentRecordingSessionId);
            if (String.IsNullOrWhiteSpace(SessionNotes)) return ("");
            if (!SessionNotes.Contains("[GUANO]"))
            {
                return ("");
            }
            if (!File.Exists(wavfile)) return ("");
            byte[] metadata = null;
            WAMD_Data wamd_data = null;
            String guanoData = Get_WAVFile_MetaData(wavfile, out metadata, out wamd_data);
            return (guanoData);
        }

        private double? _version = null;

        /// <summary>
        /// Timestamp:
        /// </summary>
        public DateTime? timestamp
        {
            get
            {
                if (_timestamp == null)
                {
                    if (lines != null && lines.Count() > 0)
                    {
                        foreach (var line in lines)
                        {
                            if (line.Contains("Timestamp:"))
                            {
                                var parts = line.Split(':');
                                if (parts != null && parts.Count() > 0)
                                {
                                    DateTime dt = new DateTime();
                                    DateTime.TryParse(parts[1], out dt);
                                    if (dt.Year > 1950) _timestamp = dt;
                                }
                            }
                        }
                    }
                }
                return (_timestamp);
            }
        }

        private DateTime? _timestamp = null;

        /// <summary>
        /// Tags: a,b,c
        /// </summary>
        public String[] tags
        {
            get
            {
                if (_tags == null)
                {
                    if (!lines.IsNullOrEmpty())
                    {
                        foreach (var line in lines)
                        {
                            if (line.Contains("Tags:"))
                            {
                                var parts = line.Split(':');
                                if (parts != null && parts.Count() > 1)
                                {
                                    _tags = parts[1].Trim().Split(',');
                                }
                            }
                        }
                    }
                }
                return (_tags);
            }
        }

        private String[] _tags = null;

        /// <summary>
        /// Note: text/nsecond line/nthird line
        /// </summary>
        public String note
        {
            get
            {
                if (_note == null)
                {
                    if (!lines.IsNullOrEmpty())
                    {
                        foreach (var line in lines)
                        {
                            if (line.StartsWith("Note:"))
                            {
                                var parts = line.Split(':');
                                if (parts != null && parts.Count() > 1)
                                {
                                    _note = parts[1];
                                }
                            }
                        }
                    }
                }
                return (_note);
            }
        }

        private string _note = null;

        /// <summary>
        /// Samplerate:
        /// </summary>
        public int? samplerate
        {
            get
            {
                if (_samplerate == null)
                {
                    if (!lines.IsNullOrEmpty())
                    {
                        foreach (var line in lines)
                        {
                            if (line.StartsWith("Samplerate:"))
                            {
                                var parts = line.Split(':');
                                if (parts != null && parts.Count() > 1)
                                {
                                    int s;
                                    if (int.TryParse(parts[1].Trim(), out s))
                                    {
                                        _samplerate = s;
                                    }
                                }
                            }
                        }
                    }
                }
                return (_samplerate);
            }
        }

        private int? _samplerate = null;

        /// <summary>
        /// Filter HP: (in kHz)
        /// </summary>
        public double? filterHP
        {
            get
            {
                if (_filterHP == null)
                {
                    if (!lines.IsNullOrEmpty())
                    {
                        foreach (var line in lines)
                        {
                            if (line.StartsWith("Filter HP:"))
                            {
                                var parts = line.Split(':');
                                if (parts != null && parts.Count() > 1)
                                {
                                    double f;
                                    if (double.TryParse(parts[1].Trim(), out f))
                                    {
                                        _filterHP = f;
                                    }
                                }
                            }
                        }
                    }
                }
                return (_filterHP);
            }
        }

        private double? _filterHP = null;

        /// <summary>
        /// Filter LP: (in kHz)
        /// </summary>
        public double? filterLP
        {
            get
            {
                if (_filterLP == null)
                {
                    if (!lines.IsNullOrEmpty())
                    {
                        foreach (var line in lines)
                        {
                            if (line.StartsWith("GUANO|Version:"))
                            {
                                var parts = line.Split(':');
                                if (parts != null && parts.Count() > 1)
                                {
                                    double f;
                                    if (double.TryParse(parts[1].Trim(), out f))
                                    {
                                        _filterLP = f;
                                    }
                                }
                            }
                        }
                    }
                }
                return (_filterLP);
            }
        }


        private double? _filterLP = null;

        /// <summary>
        /// MAKER|Version:
        /// </summary>
        public string firmwareVersion
        {
            get
            {
                if (_firmwareVersion == null)
                {
                    if (!lines.IsNullOrEmpty())
                    {
                        foreach (var line in lines)
                        {
                            if (line.StartsWith("GUANO|Version:"))
                            {
                                var parts = line.Split(':');
                                if (parts != null && parts.Count() > 1)
                                {
                                    _firmwareVersion = parts[1];
                                }
                            }
                        }
                    }
                }
                return (_firmwareVersion);
            }
        }

        private string _firmwareVersion = null;

        /// <summary>
        /// MAKER|Version:
        /// </summary>
        public string hardwareVersion
        {
            get
            {
                if (_hardwareVersion == null)
                {
                    if (!lines.IsNullOrEmpty())
                    {
                        foreach (var line in lines)
                        {
                            if (line.Contains("|Version:") && !line.Trim().StartsWith("GUANO"))
                            {
                                var parts = line.Split(':');
                                if (parts != null && parts.Count() > 1)
                                {
                                    _hardwareVersion = parts[1];
                                }
                            }
                        }
                    }
                }
                return (_hardwareVersion);
            }
        }

        private string _hardwareVersion = null;

        /// <summary>
        /// Humidity: (0.0 - 100.0)
        /// </summary>
        public double? humidity
        {
            get
            {
                if (_humidity == null)
                {
                    if (!lines.IsNullOrEmpty())
                    {
                        foreach (var line in lines)
                        {
                            if (line.StartsWith("Humidity:"))
                            {
                                var parts = line.Split(':');
                                if (parts != null && parts.Count() > 1)
                                {
                                    double v;
                                    if (double.TryParse(parts[1].Trim(), out v))
                                    {
                                        _humidity = v;
                                    }
                                }
                            }
                        }
                    }
                }
                return (_humidity);
            }
        }

        private double? _humidity = null;

        /// <summary>
        /// Length: (in secs)
        /// </summary>
        public double? length
        {
            get
            {
                if (_length == null)
                {
                    if (!lines.IsNullOrEmpty())
                    {
                        foreach (var line in lines)
                        {
                            if (line.StartsWith("Length:"))
                            {
                                var parts = line.Split(':');
                                if (parts != null && parts.Count() > 1)
                                {
                                    double v;
                                    if (double.TryParse(parts[1].Trim(), out v))
                                    {
                                        _length = v;
                                    }
                                }
                            }
                        }
                    }
                }
                return (_length);
            }
        }

        private double? _length = null;

        /// <summary>
        /// Loc Accuracy:
        /// </summary>
        public double? locationAccuracy
        {
            get
            {
                if (_locationAccuracy == null)
                {
                    if (!lines.IsNullOrEmpty())
                    {
                        foreach (var line in lines)
                        {
                            if (line.StartsWith("Loc Accuracy:"))
                            {
                                var parts = line.Split(':');
                                if (parts != null && parts.Count() > 1)
                                {
                                    double v;
                                    if (double.TryParse(parts[1].Trim(), out v))
                                    {
                                        _locationAccuracy = v;
                                    }
                                }
                            }
                        }
                    }
                }
                return (_locationAccuracy);
            }
        }

        private double? _locationAccuracy = null;

        /// <summary>
        /// Loc Elevation:
        /// </summary>
        public double? locationElevation
        {
            get
            {
                if (_locationElevation == null)
                {
                    if (!lines.IsNullOrEmpty())
                    {
                        foreach (var line in lines)
                        {
                            if (line.StartsWith("Loc Elevation:"))
                            {
                                var parts = line.Split(':');
                                if (parts != null && parts.Count() > 1)
                                {
                                    double v;
                                    if (double.TryParse(parts[1].Trim(), out v))
                                    {
                                        _locationElevation = v;
                                    }
                                }
                            }
                        }
                    }
                }
                return (_locationElevation);
            }
        }

        private double? _locationElevation = null;

        /// <summary>
        /// Loc Position: (32.1878016 -86.1057312)
        /// </summary>
        public Tuple<double, double> location
        {
            get
            {
                if (_location == null)
                {
                    if (!lines.IsNullOrEmpty())
                    {
                        foreach (var line in lines)
                        {
                            if (line.StartsWith("Loc Position:"))
                            {
                                var parts = line.Split(':');
                                if (parts != null && parts.Count() > 1)
                                {
                                    var locarray = parts[1].Trim().Split(' ');
                                    if (locarray != null && locarray.Count() > 1)
                                    {
                                        double lat;
                                        if (double.TryParse(locarray[0].Trim(), out lat))
                                        {
                                            double longit;
                                            if (double.TryParse(locarray[1].Trim(), out longit))
                                            {
                                                _location = new Tuple<double, double>(lat, longit);
                                            }
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
                return (_location);
            }
        }

        private Tuple<double, double> _location = null;

        /// <summary>
        /// MAKER|Make:
        /// </summary>
        public string make
        {
            get
            {
                if (_make == null)
                {
                    if (!lines.IsNullOrEmpty())
                    {
                        foreach (var line in lines)
                        {
                            if (line.Contains("|Make:"))
                            {
                                var parts = line.Split(':');
                                if (parts != null && parts.Count() > 1)
                                {
                                    _make = parts[1].Trim();
                                }
                            }
                        }
                    }
                }
                return (_make);
            }
        }

        private string _make = null;

        /// <summary>
        /// MAKER|Model:
        /// </summary>
        public string model
        {
            get
            {
                if (_model == null)
                {
                    if (!lines.IsNullOrEmpty())
                    {
                        foreach (var line in lines)
                        {
                            if (line.Contains("|model:"))
                            {
                                var parts = line.Split(':');
                                if (parts != null && parts.Count() > 1)
                                {
                                    _model = parts[1].Trim();
                                }
                            }
                        }
                    }
                }
                return (_model);
            }
        }

        private string _model = null;

        /// <summary>
        /// Species Auto ID: a,b,c
        /// </summary>
        public string speciesAutoID
        {
            get
            {
                if (_speciesAutoID == null)
                {
                    if (!lines.IsNullOrEmpty())
                    {
                        foreach (var line in lines)
                        {
                            if (line.StartsWith("Species Auto ID:"))
                            {
                                var parts = line.Split(':');
                                if (parts != null && parts.Count() > 1)
                                {
                                    _speciesAutoID = parts[1].Trim();
                                }
                            }
                        }
                    }
                }
                return (_speciesAutoID);
            }
        }

        private string _speciesAutoID = null;

        /// <summary>
        /// Species Manual ID: a,b,c
        /// </summary>
        public string speciesManualID
        {
            get
            {
                if (_speciesManualID == null)
                {
                    if (!lines.IsNullOrEmpty())
                    {
                        foreach (var line in lines)
                        {
                            if (line.StartsWith("Species Manual ID:"))
                            {
                                var parts = line.Split(':');
                                if (parts != null && parts.Count() > 1)
                                {
                                    _speciesManualID = parts[1].Trim();
                                }
                            }
                        }
                    }
                }
                return (_speciesManualID);
            }
        }

        private string _speciesManualID = null;

        /// <summary>
        /// TE:
        /// </summary>
        public int? expansion
        {
            get
            {
                if (_expansion == null)
                {
                    if (!lines.IsNullOrEmpty())
                    {
                        foreach (var line in lines)
                        {
                            if (line.StartsWith("TE:"))
                            {
                                var parts = line.Split(':');
                                if (parts != null && parts.Count() > 1)
                                {
                                    int v;
                                    if (int.TryParse(parts[1].Trim(), out v))
                                    {
                                        _expansion = v;
                                    }
                                }
                            }
                        }
                    }
                }
                return (_expansion);
            }
        }

        private int? _expansion = null;

        /// <summary>
        /// Temperature Ext:
        /// Temperature Int:
        /// </summary>
        public double? temperature
        {
            get
            {
                if (_temperature == null)
                {
                    if (!lines.IsNullOrEmpty())
                    {
                        foreach (var line in lines)
                        {
                            if (line.StartsWith("Temperature Ext:"))
                            {
                                var parts = line.Split(':');
                                if (parts != null && parts.Count() > 1)
                                {
                                    double v;
                                    if (double.TryParse(parts[1].Trim(), out v))
                                    {
                                        _temperature = v;
                                    }
                                }
                            }
                        }
                    }
                }
                return (_temperature);
            }
        }

        private double? _temperature = null;

        /// <summary>
        /// BATREC|Version:
        /// </summary>
        public string deviceVersion
        {
            get
            {
                if (_deviceVersion == null)
                {
                    if (!lines.IsNullOrEmpty())
                    {
                        foreach (var line in lines)
                        {
                            if (line.StartsWith("BATREC|Version:"))
                            {
                                var parts = line.Split(':');
                                if (parts != null && parts.Count() > 1)
                                {
                                    _deviceVersion = parts[1];
                                }
                            }
                        }
                    }
                }
                return (_deviceVersion);
            }
        }

        private string _deviceVersion = null;

        /// <summary>
        /// BATREC|Host Device:
        /// </summary>
        public string hostDevice
        {
            get
            {
                if (_hostDevice == null)
                {
                    if (!lines.IsNullOrEmpty())
                    {
                        foreach (var line in lines)
                        {
                            if (line.StartsWith("BATREC|Host Device:"))
                            {
                                var parts = line.Split(':');
                                if (parts != null && parts.Count() > 1)
                                {
                                    _hostDevice = parts[1];
                                }
                            }
                        }
                    }
                }
                return (_hostDevice);
            }
        }

        private string _hostDevice = null;

        /// <summary>
        /// BATREC|Host OS:
        /// </summary>
        public string hostOS
        {
            get
            {
                if (_hostOS == null)
                {
                    if (!lines.IsNullOrEmpty())
                    {
                        foreach (var line in lines)
                        {
                            if (line.StartsWith("BATREC|Host OS:"))
                            {
                                var parts = line.Split(':');
                                if (parts != null && parts.Count() > 1)
                                {
                                    _hostOS = parts[1];
                                }
                            }
                        }
                    }
                }
                return (_hostOS);
            }
        }

        private string _hostOS = null;
    }
}