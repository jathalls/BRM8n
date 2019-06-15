using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data.Linq;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using ImageMagick;


namespace BatRecordingManager
{
    /// <summary>
    /// Class to hold an image and its associated metadata
    /// </summary>
    [Serializable]
    public class StoredImage : INotifyPropertyChanged

    {
        public event PropertyChangedEventHandler PropertyChanged;

        /// <summary>
        /// The binary image as a windows bitmap
        /// </summary>
        //public BitmapImage image { get; set; } = null;
        public BitmapSource image
        {
            get
            {
                return (_image);
            }
            set
            {
                _image = value;
                if (PropertyChanged != null)
                {
                    PropertyChanged(this, new PropertyChangedEventArgs("image"));
                }
            }
        }

        private BitmapSource _image;

        /// <summary>
        /// The caption for the image - usually the filename or the bat name
        /// </summary>
        public string caption
        {
            get { return (_caption); }
            set
            {
                _caption = value;
                if (PropertyChanged != null)
                {
                    PropertyChanged(this, new PropertyChangedEventArgs("caption"));
                }
            }
        }

        private string _caption;

        /// <summary>
        /// A longer description of what the image is all about
        /// </summary>
        public string description
        {
            get { return (_description); }
            set
            {
                _description = value;
                if (PropertyChanged != null)
                {
                    PropertyChanged(this, new PropertyChangedEventArgs("description"));
                }
            }
        }

        public void SetPropertyChanged()
        {
            PropertyChanged(this, new PropertyChangedEventArgs("HorizontalGridlines"));
        }

        private string _description;

        /// <summary>
        /// The index ID of the image in the database
        /// </summary>
        public int ImageID { get; set; } = -1;

        /// <summary>
        /// The URI used for encoding and decoding the image
        /// </summary>
        public String Uri { get; set; } = "";

        /// <summary>
        /// A list of values for horzontal grid lines to be overlaid on the image in pixels
        /// between 0 and the height of the image
        /// </summary>
        public List<int> HorizontalGridlines { get; set; } = new List<int>();

        /// <summary>
        /// A list of vertical grid lines to be overlaid on the image in pixels from 0 to
        /// the width of the image
        /// </summary>
        public List<int> VerticalGridLines { get; set; } = new List<int>();

        /// <summary>
        /// Not sure if this name field is used
        /// </summary>
        public string Name { get; set; } = "";

        /// <summary>
        /// returns true if there is a labelled segment associated with this image.
        /// Also causes the segmentsForImage to be populated if it has not been previously.
        /// </summary>
        public bool isPlayable
        {
            get
            {

                if (segmentsForImage == null)
                {
                    segmentsForImage = DBAccess.GetSegmentsForImage(this.ImageID);
                }
                if (segmentsForImage.Count == 0)
                {
                    return (false);
                }
                else
                {
                    string fileName = Tools.ExtractWAVFilename(this.caption);
                    if (!string.IsNullOrWhiteSpace(fileName))
                    {
                        string path = segmentsForImage[0].Recording.GetFileName();
                        if (!String.IsNullOrWhiteSpace(path))
                        {
                            return (true);
                        }
                    }
                    return (false);
                }
                
            }
        }

        public List<LabelledSegment> segmentsForImage { get; set; } = null;

        /// <summary>
        /// default constructor for the StoredImage class
        /// </summary>
        /// <param name="image"></param>
        /// <param name="caption"></param>
        /// <param name="description"></param>
        /// <param name="ImageID"></param>
        //public StoredImage(BitmapImage image, string caption, string description, int ImageID)
        public StoredImage(BitmapSource image, string caption, string description, int imageID)
        {
            this.image = image;
            this.caption = caption;
            this.description = description;

            this.ImageID = imageID;
        }

        /// <summary>
        /// Creates an instance of a StoredImage from a BinaryData such as is stored in the database;
        /// Converts from PNG or generic types of blob and if the type is PNG restores any gridline data.
        /// </summary>
        /// <param name="blob"></param>
        /// <returns></returns>
        public static StoredImage CreateFromBinary(BinaryData blob)
        {
            StoredImage si;
            if (blob.BinaryDataType.ToUpper().Contains("PNG"))
            {
                List<int> hgls = new List<int>();
                List<int> vgls = new List<int>();
                si = new StoredImage(StoredImage.ConvertBinaryPngToBitmapImage(blob.BinaryData1, out hgls, out vgls), "", "", blob.Id);
                si.HorizontalGridlines = hgls;
                si.VerticalGridLines = vgls;
            }
            else
            {
                si = new StoredImage(StoredImage.ConvertBinaryToBitmapImage(blob.BinaryData1), "", "", blob.Id);
            }
            si.setCaptionAndDescription(blob.Description);
            si.SetUri(blob);
            return (si);
        }

        /// <summary>
        /// Takes combined description as stored in the database in which the caption and description
        /// parts are separatedd by $ signs, and stores the extracted components in them relative member
        /// variables of StoredImage.
        /// </summary>
        /// <param name="combined_caption_description"></param>
        public void setCaptionAndDescription(string combined_caption_description)
        {
            if (string.IsNullOrWhiteSpace(combined_caption_description))
            {
                caption = "";
                description = "";
            }
            else
            {
                caption = "";
                description = "";
                if (combined_caption_description.Contains("$"))
                {
                    // we have a properly combined string with a colon separator
                    var parts = combined_caption_description.Split('$');
                    // first part is the caption
                    if (parts.Any())
                    {
                        caption = parts[0].Trim();
                        caption = caption.Replace("££", "$");
                    }
                    description = "";
                    // second part is the description
                    if (parts.Count() > 1)
                    {
                        description = parts[1].Trim() + "\n";
                    }
                    description = description.Trim();
                    description = description.Replace("££", "$");

                    //third part is the encoded grid-lines
                    string metadata = "";
                    if (parts.Count() > 2)
                    {
                        if (!String.IsNullOrWhiteSpace(parts[2]))
                        {
                            List<int> HGLs = new List<int>();
                            List<int> VGLs = new List<int>();
                            DecodeMetadata(parts[2].Trim(), out HGLs, out VGLs);
                            if (!HGLs.IsNullOrEmpty())
                            {
                                HorizontalGridlines = HGLs;
                            }
                            if (!VGLs.IsNullOrEmpty())
                            {
                                VerticalGridLines = VGLs;
                            }
                        }
                    }
                }
                else
                {
                    if (combined_caption_description.Length < 144)
                    {
                        caption = combined_caption_description.Trim();
                    }
                    else
                    {
                        description = combined_caption_description.Trim();
                    }
                }
            }
        }

        /// <summary>
        /// combines the StoredImage member variable caption and description into a multiplexed
        /// string separated by $ which is no longer truncated to 250 chars to fit the database
        /// </summary>
        /// <returns></returns>
        public string getCombinedText()
        {
            string s1, s2;
            if (String.IsNullOrWhiteSpace(caption)) caption = " ";
            s1 = caption.Replace("$", "££");
            if (String.IsNullOrWhiteSpace(description)) description = " ";
            s2 = description.Replace("$", "££");
            string result = s1 + "$" + s2;
            string metadata = encodeMetadata(HorizontalGridlines, VerticalGridLines);
            if (!String.IsNullOrWhiteSpace(metadata))
            {
                result = result + "$" + metadata;
            }
            return (result);
        }

        /// <summary>
        /// Given a BinaryData from the database which includes various fields, converts the blob to a BitmapImage which is
        /// stored in the member variable 'image' and SetCaptionAndDescription is used to separate the caption and description
        /// which are merged together in the BinaryData into separate memberVariables in StoredImage.
        /// </summary>
        /// <param name="blob"></param>
        public void setBinaryData(BinaryData blob)
        {
            var HGLs = new List<int>();
            var VGLs = new List<int>();
            if (blob == null)
            {
                image = null;
                caption = "";
                description = "";

                ImageID = -1;
            }
            else
            {
                if (blob.BinaryDataType != Tools.BlobType.BMP.ToString() && blob.BinaryDataType != Tools.BlobType.BMPS.ToString())
                {
                    return;
                }
                if (blob.BinaryDataType == Tools.BlobType.BMP.ToString())
                {
                    BitmapImage bmps;

                    bmps = ConvertBinaryToBitmapImage(blob.BinaryData1);
                    image = bmps;
                }
                else if (blob.BinaryDataType == Tools.BlobType.BMPS.ToString())
                {
                    BitmapImage bmps;

                    bmps = ConvertBinaryToBitmapImage(blob.BinaryData1);
                    image = bmps;
                }
                else if (blob.BinaryDataType == Tools.BlobType.PNG.ToString())
                {
                    //BitmapImage bmps;
                    BitmapSource bmps;
                    bmps = ConvertBinaryPngToBitmapImage(blob.BinaryData1, out HGLs, out VGLs);
                    HorizontalGridlines = HGLs;
                    VerticalGridLines = VGLs;
                    image = bmps;
                }
                setCaptionAndDescription(blob.Description);
                ImageID = blob.Id;
            }
            HorizontalGridlines = HGLs;
            VerticalGridLines = VGLs;
        }

        /// <summary>
        /// returns a string containing a name for the image.  If the image links to a segment or recording
        /// the name will be the recording filename with the .wav removed and either _nn appended where nn
        /// is either 00 for a recording or the number of seconds to the start of the segment.
        /// If the image relates to a bat or call then the name returned is the image caption .trimmed.
        /// </summary>
        /// <returns></returns>
        public String GetName()
        {
            String name = caption;
            if (name.ToUpper().Contains(".WAV"))
            {
                name = name.Substring(0, name.ToUpper().LastIndexOf(".WAV"));
            }
            name = name + "_00";
            var blob = DBAccess.GetBlob(ImageID);
            if (!blob.SegmentDatas.IsNullOrEmpty())
            {
                LabelledSegment segment = blob.SegmentDatas.First().LabelledSegment;
                name = segment.Recording.RecordingName;
                if (name.ToUpper().Contains(".WAV"))
                {
                    name = name.Substring(0, name.ToUpper().LastIndexOf(".WAV"));
                }
                name = name + "_" + segment.StartOffset.TotalSeconds;
            }
            if (name.Contains(@"\"))
            {
                name = name.Substring(name.LastIndexOf('\\') + 1);
            }
            return (name);
        }

        /// <summary>
        /// Clears the contents, replacing with a zero sized bitmap and no strings or gridlines
        /// </summary>
        internal void Clear(bool clearCaption)
        {
            image = null;
            if (clearCaption)
            {
                caption = "";
                Uri = "";
            }
            description = "";
            HorizontalGridlines = new List<int>();
            VerticalGridLines = new List<int>();
            ImageID = -1;
            
        }

        internal void Clear()
        {
            this.Clear(true);
        }

        /// <summary>
        /// Updates this stored image in the database.  Assumes that this image is already in the database
        /// and has a valid ID
        /// </summary>
        internal void Update()
        {
            DBAccess.UpdateImage(this);
        }

        /// <summary>
        /// sets the Uri field of the stored image if there is an appropriate one
        /// available.  For images linked to a segment, this will be the fully qualified
        /// file name of the associated recording .wav file.  The system may be extended in the
        /// future to include additional types of Uri's.  If there are more than one associated
        /// files, then the names will be concatenated, separated by ';'.
        ///
        /// </summary>
        /// <param name="blob"></param>
        internal void SetUri(BinaryData blob)
        {
            String uri = "";

            if (blob != null && !blob.SegmentDatas.IsNullOrEmpty())
            {
                var files = (from sd in blob.SegmentDatas

                             select sd.LabelledSegment.Recording.RecordingSession.OriginalFilePath.Trim() + @"\" + sd.LabelledSegment.Recording.RecordingName)
                                .Aggregate((i, j) => i + ";" + j);
                /*if (!files.IsNullOrEmpty())
                {
                    foreach(var file in files)
                    {
                        if (!String.IsNullOrWhiteSpace(uri))
                        {
                            uri = uri + ";";
                        }
                        uri = uri + file;
                    }
                }*/
                while (uri.Contains(@"\\"))
                {
                    uri = uri.Replace(@"\\", @"\");
                }

                Uri = uri;
            }
        }

        /// <summary>
        /// Converts a binary blob representing a .PNG image into a BitmapImage
        /// </summary>
        /// <param name="rawImage"></param>
        /// <param name="binaryData1"></param>
        /// <returns></returns>
        public static BitmapSource ConvertBinaryPngToBitmapImage(Binary rawImage, out List<int> HorizontalGridLines, out List<int> VerticalGridLines)
        {
            //BitmapImage bmpImage = null;
            //bmpImage = new BitmapImage();
            BitmapSource bmps = null;

            HorizontalGridLines = new List<int>();
            VerticalGridLines = new List<int>();

            if (rawImage != null)
            {
                byte[] array = rawImage.ToArray();

                if (array.Length > 0)
                {
                    bmps = (BitmapSource)new ImageSourceConverter().ConvertFrom(array);
                    //using (MemoryStream stream = new MemoryStream(array))
                    //{
                    // PngBitmapDecoder decoder = new PngBitmapDecoder(stream, BitmapCreateOptions.None, BitmapCacheOption.Default);

                    //BitmapSource bmps = decoder.Frames[0];

                    BitmapMetadata md = (BitmapMetadata)bmps.Metadata;
                    if (md != null)
                    {
                        var res1 = md.GetQuery("/tEXt/{str=Comment}");
                        string resStr = "";
                        if (res1 != null)
                        {
                            resStr = res1.ToString();
                        }
                        if (!String.IsNullOrWhiteSpace(resStr))
                        {
                            DecodeMetadata(resStr, out HorizontalGridLines, out VerticalGridLines);
                        }
                    }

                    //bmpImage = ConvertBitmapSourceToBitmapImage(bmps);

                    //}
                }
            }

            return (bmps);
        }

        /// <summary>
        /// Decodes the metadata stored in a Png image Text field with the label of
        /// /Text/Fiducials into teo ararys of horizontal and vertical pixel values.
        /// The metadata has the form Hnnn,Hnnn,...,Vnnn,Vnnn where H or V indicate if the
        /// gridline is to be horizontal or vertical and nnn is the pixel value of the location
        /// of the line when overlaid on the image.
        /// </summary>
        /// <param name="pngTextMetadata"></param>
        private static void DecodeMetadata(string pngTextMetadata, out List<int> HorizontalgridLines, out List<int> VerticalGridLines)
        {
            List<int> HGLs = new List<int>();
            List<int> VGLs = new List<int>();

            if (!String.IsNullOrWhiteSpace(pngTextMetadata))
            {
                var items = pngTextMetadata.Split(',');
                if (!items.IsNullOrEmpty())
                {
                    foreach (var item in items)
                    {
                        int val = 0;
                        string direction = item.Trim().Substring(0, 1);
                        int.TryParse(item.Trim().Substring(1), out val);
                        if (direction.ToUpper() == "H")
                        {
                            HGLs.Add(val);
                        }
                        else if (direction.ToUpper() == "V")
                        {
                            VGLs.Add(val);
                        }
                    }
                }
            }
            HorizontalgridLines = HGLs;
            VerticalGridLines = VGLs;
        }

        private static string encodeMetadata(List<int> HorizontalGridLines, List<int> VerticalGridLines)
        {
            string result = "";
            if (HorizontalGridLines != null && HorizontalGridLines.Count > 0)
            {
                foreach (var line in HorizontalGridLines)
                {
                    result = result + "H" + line + ",";
                }
            }
            if (VerticalGridLines != null && VerticalGridLines.Count > 0)
            {
                foreach (var line in VerticalGridLines)
                {
                    result = result + "V" + line + ",";
                }
            }
            if (result.EndsWith(","))
            {
                result = result.Substring(0, result.Length - 1);
            }
            return (result);
        }

        /// <summary>
        /// Loads a .png image from a file and uses PropertyTags to set the caption and description
        /// </summary>
        /// <param name="file"></param>
        /// <returns></returns>
        internal static StoredImage Load(string file)
        {
            StoredImage si = null;
            String caption = "";
            String description = "";

            if (!String.IsNullOrWhiteSpace(file) && File.Exists(file))
            {
                Image img = Image.FromFile(file);
                try
                {
                    PropertyItem piCaption = img.GetPropertyItem(0x0320); //PropertyTagImageTitle
                    if (piCaption != null)
                    {
                        caption = System.Text.Encoding.UTF8.GetString(piCaption.Value);
                        if (caption.Last() == '\0')
                        {
                            caption = caption.Substring(0, caption.Length - 1);
                        }
                    }
                }
                catch (Exception ex)
                {
                    Tools.ErrorLog(ex.Message);
                    caption = "";
                }

                try
                {
                    PropertyItem piDescription = img.GetPropertyItem(0x010E);
                    if (piDescription != null)
                    {
                        description = System.Text.Encoding.UTF8.GetString(piDescription.Value);
                        if (description.Last() == '\0')
                        {
                            description = description.Substring(0, description.Length - 1);
                        }
                    }
                }
                catch (Exception ex)
                {
                    Tools.ErrorLog(ex.Message);
                    description = "";
                }

                var sdBmp = img as Bitmap;
                MemoryStream ms = new MemoryStream();
                sdBmp.Save(ms, System.Drawing.Imaging.ImageFormat.Png);
                System.Windows.Media.Imaging.BitmapImage miBmpImage = new BitmapImage();
                miBmpImage.BeginInit();
                miBmpImage.StreamSource = new MemoryStream(ms.ToArray());
                miBmpImage.EndInit();
                si = new StoredImage(miBmpImage, caption, description, -1);
            }
            return (si);
        }

        /// <summary>
        /// Converts a binary blob representing a bitmap or bitmapsource into a BitmapImage
        /// </summary>
        /// <param name="rawImage"></param>
        /// <returns></returns>
        public static BitmapImage ConvertBinaryToBitmapImage(Binary rawImage)
        {
            BitmapImage bmpImage = null;
            bmpImage = new BitmapImage();
            if (rawImage != null)
            {
                byte[] array = rawImage.ToArray();
                if (array.Length > 0)
                {
                    using (MemoryStream stream = new MemoryStream(array))
                    {
                        bmpImage.BeginInit();
                        bmpImage.CacheOption = BitmapCacheOption.OnLoad;
                        bmpImage.StreamSource = stream;
                        bmpImage.EndInit();
                        bmpImage.Freeze();
                    }
                }
            }

            return bmpImage;
        }

        /// <summary>
        /// Saves the current image with the caption and description as Properties
        /// </summary>
        /// <param name="folderPath"></param>
        internal void Save(string folderPath, bool withFuducialLines)
        {
            Image img = null;
            try
            {
                
                
                PngBitmapEncoder encoder = new PngBitmapEncoder();
                encoder.Frames.Add(BitmapFrame.Create(image));
                using (MemoryStream ms = new MemoryStream())
                {
                    encoder.Save(ms);

                    img = Image.FromStream(ms);
                }

                MagickImage mi = new MagickImage();
                using (var graphics = Graphics.FromImage(img))
                {
                    using (var blackPen = new System.Drawing.Pen(System.Drawing.Color.Black, 1))
                    {
                        if (HorizontalGridlines != null && HorizontalGridlines.Count > 0 && withFuducialLines)
                        {
                            foreach (var HLine in HorizontalGridlines)
                            {
                                graphics.DrawLine(blackPen, (float)0.0f, (float)HLine, (float)img.Width, (float)HLine);
                            }
                        }
                        if (VerticalGridLines != null && VerticalGridLines.Count > 0 && withFuducialLines)
                        {
                            foreach (var VLine in VerticalGridLines)
                            {
                                graphics.DrawLine(blackPen, (float)VLine, (float)0.0f, (float)VLine, (float)img.Height);
                            }
                        }
                        
                    }

                    if (img != null)
                    {
                        if (folderPath.EndsWith(".jpg"))
                        {


                            using (MemoryStream ms = new MemoryStream())
                            {
                                ms.Seek(0L, SeekOrigin.Begin);
                                img.SavePng(ms, 100);
                                ms.Seek(0L, SeekOrigin.Begin);
                                mi.Read(ms);
                            }
                            mi.Format = MagickFormat.Jpg;
                            var pr = new IptcProfile();
                            pr.SetValue(IptcTag.Caption, description);
                           
                            
                            
                            pr.SetValue(IptcTag.Title, caption);
                            mi.AddProfile(pr);
                            
                            
                            mi.Write(folderPath);
                        }
                        else
                        {
                            img.SavePng(folderPath, 100L, caption, description);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Tools.ErrorLog(ex.Message);
                Debug.WriteLine("Failed to save image {" + folderPath + "}:- " + ex.Message);
            }
        }

        private byte[] GetBytes(string v)
        {
            byte[] bytes = new byte[v.Length * sizeof(char)];
            System.Buffer.BlockCopy(v.ToCharArray(), 0, bytes, 0, bytes.Length);
            return (bytes);
        }

        /// <summary>
        /// Given a BitmapImage converts it into a PNG binary for storage in the database
        /// </summary>
        /// <param name="bmpImage"></param>
        /// <param name="HorizontalGridLines"></param>
        /// <param name="VerticalGridLines"></param>
        /// <returns></returns>
        //public static System.Data.Linq.Binary ConvertBitmapImageToPngBinary(BitmapImage bmpImage,List<int> HorizontalGridLines,List<int> VerticalGridLines)
        public static System.Data.Linq.Binary ConvertBitmapImageToPngBinary(BitmapSource bmpImage, List<int> HorizontalGridLines, List<int> VerticalGridLines)
        {
            if (bmpImage != null)
            {
                try
                {
                    byte[] rawImage;

                    ///ERROR IN HERE

                    string pngTextMetadata = encodeMetadata(HorizontalGridLines, VerticalGridLines);

                    PngBitmapEncoder encoder = new PngBitmapEncoder();

                    encoder.Frames.Add(BitmapFrame.Create(bmpImage));

                    BitmapMetadata tmpMeta = new BitmapMetadata("png");

                    tmpMeta.SetQuery("/tEXt/{str=Comment}", pngTextMetadata);

                    BitmapFrame newBMF = BitmapFrame.Create((BitmapSource)encoder.Frames[0],
                        (BitmapSource)encoder.Frames[0].Thumbnail,
                        tmpMeta,
                        encoder.Frames[0].ColorContexts);

                    encoder = new PngBitmapEncoder();
                    encoder.Frames.Add(newBMF);

                    using (MemoryStream ms = new MemoryStream())
                    {
                        encoder.Save(ms);
                        rawImage = ms.ToArray();
                        Binary binary = new Binary(rawImage);
                        return (binary);
                    }
                }
                catch (System.InvalidOperationException iex)
                {
                    Tools.ErrorLog(iex.Message + " Invalid Bitmap");
                    Debug.WriteLine("**** Invalid operation, invalid bitmap:- " + iex.ToString());
                }
            }

            return (null);
        }


        /// <summary>
        /// returns the image stored in the clipboard if any
        /// </summary>
        /// <returns></returns>
        public static BitmapSource GetClipboardImageAsBitmapImage()
        {
            //BitmapImage bmi = new BitmapImage();
            BitmapSource bmps = null;

            if (Clipboard.ContainsImage())
            {
                bmps = (BitmapSource)Clipboard.GetImage();
                //bmi = StoredImage.ConvertBitmapSourceToBitmapImage(bmps);
            }

            return (bmps);
        }

        
        /// <summary>
        /// converts the image member variable which is a BitmapImage into a .PNG binary for storage
        /// </summary>
        /// <returns></returns>
        public BinaryData GetAsBinaryData()
        {
            BinaryData blob = new BinaryData();
            blob.BinaryData1 = ConvertBitmapImageToPngBinary(image, HorizontalGridlines, VerticalGridLines);
            blob.BinaryDataType = Tools.BlobType.PNG.ToString();
            blob.Description = getCombinedText();
            blob.Id = ImageID;
            return (blob);
        }

        /// <summary>
        /// If the storedImage has a .wav file and/or segments associated with it, then the file is opened
        /// with Audacity (if it is the default program for .wav files) and zooms to the segment if there is a
        /// single segment associated with the image.
        /// </summary>
        internal void Open()
        {
            if (isPlayable)
            {
                if(segmentsForImage!=null && segmentsForImage.Count == 1)
                {
                    Tools.OpenWavFile(segmentsForImage[0].Recording.GetFileName(), segmentsForImage[0].StartOffset, segmentsForImage[0].EndOffset);
                }
                else
                {
                    if (segmentsForImage.Count > 1)
                    {
                        TimeSpan start = new TimeSpan();
                        TimeSpan end = new TimeSpan();
                        if (GetOffsetsFromCaption(out start, out end))
                        {
                            Tools.OpenWavFile(segmentsForImage[0].Recording.GetFileName(), start, end);
                        }
                        else
                        {
                            Tools.OpenWavFile(segmentsForImage[0].Recording.GetFileName());
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Checks the caption to see if it has one or more time fields after the filename.  If so
        /// extracts them as TimeSpans and returns true, otherwise returns false
        /// </summary>
        /// <param name="start"></param>
        /// <param name="end"></param>
        /// <returns></returns>
        private bool GetOffsetsFromCaption(out TimeSpan start, out TimeSpan end)
        {
            bool result = false;
            start = new TimeSpan();
            end = new TimeSpan();
            String pattern = @".*\.[wW][aA][vV]\s*([0-9]+)\s*-?\s*([0-9]*)";
            if (!String.IsNullOrWhiteSpace(caption))
            {
                var match = Regex.Match(caption, pattern);
                if (match.Success)
                {
                    if (match.Groups.Count > 1)
                    {
                        int startSecs = 0;
                        if(int.TryParse(match.Groups[1].Value, out startSecs))
                        {
                            start = TimeSpan.FromSeconds((double)startSecs);
                        }
                        if (match.Groups.Count > 2)
                        {
                            int endSecs = 0;
                            if (int.TryParse(match.Groups[2].Value, out endSecs))
                            {
                                end = TimeSpan.FromSeconds((double)endSecs);
                            }
                        }
                        else
                        {
                            end = start + TimeSpan.FromSeconds(5.0d);
                        }

                        result = true;
                    }
                    
                    
                }
            }


            return (result);
        }
    }

#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member

    public static class ImageExtensions
#pragma warning restore CS1591 // Missing XML comment for publicly visible type or member
    {
#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member

        public static void SaveJpeg(this Image img, string filePath, long quality)
#pragma warning restore CS1591 // Missing XML comment for publicly visible type or member
        {
            var encoderParameters = new EncoderParameters(1);
            encoderParameters.Param[0] = new EncoderParameter(System.Drawing.Imaging.Encoder.Quality, quality);
            img.Save(filePath, GetEncoder(ImageFormat.Jpeg), encoderParameters);
        }

#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member

        public static void SavePng(this Image img, string filePath, long quality)
#pragma warning restore CS1591 // Missing XML comment for publicly visible type or member
        {
            var encoderParameters = new EncoderParameters(1);
            encoderParameters.Param[0] = new EncoderParameter(System.Drawing.Imaging.Encoder.Quality, quality);
            img.Save(filePath, GetEncoder(ImageFormat.Png), encoderParameters);
        }

#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member

        public static void SavePng(this Image img, string filePath, long quality, string caption, string description)
#pragma warning restore CS1591 // Missing XML comment for publicly visible type or member
        {
            PropertyItem pi = null;

            var list = img.PropertyItems;
            if (list != null && list.Length > 0)
            {
                pi = list[0];
            }

            if (!String.IsNullOrWhiteSpace(caption))
            {
                //pi = img.GetPropertyItem(0x010D);// Id=PropertyTagDocumentName
                pi.Id = 0x0320; // PropertyTagImageTitle
                pi.Type = 2;
                pi.Value = Encoding.UTF8.GetBytes(caption+'\0');
                pi.Len = pi.Value.Length;
                img.SetPropertyItem(pi);
            }

            if (!String.IsNullOrWhiteSpace(description))
            {
                //pi = null;
                //pi = img.GetPropertyItem(0x010E);// Id=PropertyTagImageDescription
                pi.Id = 0x010E;
                pi.Type = 2;

                pi.Value = Encoding.UTF8.GetBytes(description+'\0');
                pi.Len = pi.Value.Length;
                img.SetPropertyItem(pi);
            }

            var encoderParameters = new EncoderParameters(1);
            encoderParameters.Param[0] = new EncoderParameter(System.Drawing.Imaging.Encoder.Quality, quality);
            if (File.Exists(filePath))
            {
                string backfile = filePath.Substring(0, filePath.Length - 3) + "bak";
                if (File.Exists(backfile))
                {
                    File.Delete(backfile);
                }

                File.Move(filePath, backfile);
            }
            img.Save(filePath, GetEncoder(ImageFormat.Png), encoderParameters);
        }

#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member

        public static void SaveJpeg(this Image img, Stream stream, long quality)
#pragma warning restore CS1591 // Missing XML comment for publicly visible type or member
        {
            var encoderParameters = new EncoderParameters(1);
            encoderParameters.Param[0] = new EncoderParameter(System.Drawing.Imaging.Encoder.Quality, quality);
            img.Save(stream, GetEncoder(ImageFormat.Jpeg), encoderParameters);
        }

#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member

        public static void SavePng(this Image img, Stream stream, long quality)
#pragma warning restore CS1591 // Missing XML comment for publicly visible type or member
        {
            var encoderParameters = new EncoderParameters(1);
            encoderParameters.Param[0] = new EncoderParameter(System.Drawing.Imaging.Encoder.Quality, quality);
            img.Save(stream, GetEncoder(ImageFormat.Png), encoderParameters);
        }

        private static ImageCodecInfo GetEncoder(ImageFormat format)
        {
            ImageCodecInfo[] codecs = ImageCodecInfo.GetImageDecoders();
            return codecs.Single(codec => codec.FormatID == format.Guid);
        }

        private static Action EmptyDelegate = delegate () { };

        public static void Refresh(this UIElement uiElement)
        {
            uiElement.Dispatcher.Invoke(System.Windows.Threading.DispatcherPriority.Render, EmptyDelegate);
        }
    }
}