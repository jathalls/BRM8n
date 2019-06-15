using Microsoft.VisualStudio.Language.Intellisense;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;

namespace BatRecordingManager
{
    /// <summary>
    ///     Interaction logic for ImportControl.xaml
    /// </summary>
    public partial class ImportControl : UserControl
    {
        /// <summary>
        ///     The current session identifier
        /// </summary>
        public int CurrentSessionId = -1;

        /// <summary>
        ///     The current session tag
        /// </summary>
        public string CurrentSessionTag = "";

        /// <summary>
        ///     Initializes a new instance of the <see cref="ImportControl"/> class.
        /// </summary>
        public ImportControl()
        {
            fileBrowser = new FileBrowser();
            importPictureControl = Activator.CreateInstance<ImportPictureControl>();
            InitializeComponent();
            //fileBrowser = new FileBrowser();
            //DBAccess.InitializeDatabase();
            fileProcessor = new FileProcessor();
            importPictureControl.Visibility = Visibility.Hidden;
            OutputWindowScrollViewer.Visibility = Visibility.Visible;

            UpdateRecordingButton.ToolTip = "Update a specific Recording by selecting a single .wav file";
        }

        /// <summary>
        ///     Processes the files. fileBrowser.TextFileNames contains a list of .txt files in the
        ///     folder that is to be processed. The .txt files are label files or at least in a
        ///     compatible format similar to that produced by Audacity. There may also be a header
        ///     file which contains information about the recording session which will be generated
        ///     from this file set. The header file should start with the tag [COPY].
        ///     fileProcessor.ProcessFile does the work on each file in turn.
        /// </summary>
        public bool ProcessFiles()
        {
            bool result = false;
            tbkOutputText.Text = "[LOG]\n";

            if (!ProcessWavFiles)
            {
                Dictionary<String, BatStats> TotalBatsFound = new Dictionary<string, BatStats>();

                // process the files one by one
                try
                {
                    if (fileBrowser.TextFileNames.Count > 0)
                    {
                        if (sessionForFolder != null && sessionForFolder.Id > 0)
                        {
                            tbkOutputText.Text = sessionForFolder.ToFormattedString();
                            foreach (var rec in sessionForFolder.Recordings)
                            {
                                DBAccess.DeleteRecording(rec);//so that we can recreate them from scrathch using the file data
                            }
                        }
                        foreach (var filename in fileBrowser.TextFileNames)
                        {
                            if (!String.IsNullOrWhiteSpace(fileBrowser.HeaderFileName) && filename == fileBrowser.HeaderFileName)
                            {
                                // skip this file if it has been identified as the header data file, since
                                // the information should have been included as the session record header
                                // and this would be a duplicate.
                            }
                            else
                            {
                                tbkOutputText.Text = tbkOutputText.Text + "***\n\n" + FileProcessor.ProcessFile(filename, gpxHandler, CurrentSessionId, ref fileProcessor.BatsFound) + "\n";
                                TotalBatsFound = BatsConcatenate(TotalBatsFound, fileProcessor.BatsFound);
                            }
                        }
                        tbkOutputText.Text = tbkOutputText.Text + "\n#########\n\n";
                        if (TotalBatsFound != null && TotalBatsFound.Count > 0)
                        {
                            foreach (var bat in TotalBatsFound)
                            {
                                bat.Value.batCommonName = bat.Key;
                                tbkOutputText.Text += Tools.GetFormattedBatStats(bat.Value, false) + "\n";

                                //tbkOutputText.Text = tbkOutputText.Text +
                                // FileProcessor.FormattedBatStats(bat) + "\n";
                            }
                        }
                    }
                    if (!String.IsNullOrWhiteSpace(tbkOutputText.Text))
                    {
                        SaveOutputFile();
                        result = true;
                    }
                }
                catch (Exception ex)
                {
                    Tools.ErrorLog(ex.Message);
                    Debug.WriteLine("Processing of Recording Files failed:- " + ex.Message);
                    result = false;
                }
            }
            else
            {
                result = ProcessWavMetadata();
            }
            return (result);
        }

        /// <summary>
        ///     Reads all the files selected through a File Open Dialog. File names are contained a
        ///     fileBrowser instance which was used to select the files. Adds all the file names to
        ///     a combobox and also loads the contents into a stack of Text Boxes in the left pane
        ///     of the screen.
        /// </summary>
        public String ReadSelectedFiles()
        {
            String outputLocation = "";
            if (fileBrowser.TextFileNames != null && fileBrowser.TextFileNames.Count > 0)
            {
                Debug.WriteLine("ReadSelectedFiles:- first=:- " + fileBrowser.TextFileNames[0]);
                //File.Create(fileBrowser.OutputLogFileName);
                if (dpMMultiWindowPanel.Children.Count > 0)
                {
                    foreach (var child in dpMMultiWindowPanel.Children)
                    {
                        (child as TextBox).Clear();
                    }
                    dpMMultiWindowPanel.Children.Clear();
                }
                BulkObservableCollection<TextBox> textFiles = new BulkObservableCollection<TextBox>();
                foreach (var file in fileBrowser.TextFileNames)
                {
                    TextBox tb = new TextBox();
                    tb.AcceptsReturn = true; ;
                    tb.AcceptsTab = true;
                    tb.VerticalScrollBarVisibility = ScrollBarVisibility.Auto;
                    if (File.Exists(file))
                    {
                        using (StreamReader sr = File.OpenText(file))
                        {
                            try
                            {
                                if (sr != null)
                                {
                                    string firstline = sr.ReadLine();
                                    if (string.IsNullOrWhiteSpace(firstline))
                                    {
                                        firstline = "start - end\tNo Bats";
                                    }
                                    //sr.Close();
                                    if (firstline != null)
                                    {
                                        if (!(firstline.Contains("[LOG]") || firstline.Contains("***")))
                                        {
                                            //if (!file.EndsWith(".log.txt"))
                                            //{
                                            tb.Text = file + @"
    " + sr.ReadToEnd();
                                            dpMMultiWindowPanel.Children.Add(tb);
                                        }
                                        else
                                        {
                                            tbkOutputText.Text = sr.ReadToEnd();
                                        }
                                    }
                                }
                            }
                            catch (Exception ex)
                            {
                                Tools.ErrorLog(ex.Message);
                                Debug.WriteLine(ex);
                            }
                            finally
                            {
                                if (sr != null)
                                {
                                    sr.Close();
                                }
                            }
                        }
                    }
                }
                if (!String.IsNullOrWhiteSpace(fileBrowser.OutputLogFileName))
                {
                    outputLocation = "Output File:- " + fileBrowser.OutputLogFileName;
                }
                else
                {
                    outputLocation = "Output to:- " + fileBrowser.WorkingFolder;
                }
            }
            else
            {
                outputLocation = "";
            }
            if (String.IsNullOrWhiteSpace(tbkOutputText.Text))
            {
                tbkOutputText.Text = "[LOG]\n";
            }
            return (outputLocation);
        }

        /// <summary>
        ///     Saves the output file.
        /// </summary>
        public bool SaveOutputFile()
        {
            bool isSaved = false;
            String ofn = fileBrowser.OutputLogFileName;
            if (!String.IsNullOrWhiteSpace(tbkOutputText.Text))
            {
                //if (MessageBox.Show("Save Output File?", "Save", MessageBoxButton.YesNo) == MessageBoxResult.Yes)
                //{
                try
                {
                    if (File.Exists(fileBrowser.OutputLogFileName))
                    {
                        if (System.Windows.MessageBox.Show
                            ("Overwrite existing\n" + fileBrowser.OutputLogFileName +
                            "?", "Overwrite File", MessageBoxButton.YesNo) == MessageBoxResult.No)
                        {
                            int index = 1;
                            ofn = fileBrowser.OutputLogFileName.Substring(0, fileBrowser.OutputLogFileName.Length - 4) + "." + index;

                            while (File.Exists(ofn + ".txt"))
                            {
                                index++;
                                ofn = ofn.Substring(0, ofn.LastIndexOf('.'));
                                ofn = ofn + "." + (index);
                            }
                        }
                        else
                        {
                            File.Delete(fileBrowser.OutputLogFileName);
                            ofn = fileBrowser.OutputLogFileName;
                        }
                    }
                    else
                    {
                        ofn = fileBrowser.OutputLogFileName;
                    }
                    File.WriteAllText(ofn, tbkOutputText.Text);
                    ofn = ofn.Substring(0, ofn.Length - 8) + ".manifest";

                    File.WriteAllLines(ofn, fileBrowser.TextFileNames);
                    isSaved = true;
                    //}
                }
                catch (Exception ex)
                {
                    Tools.ErrorLog(ex.Message);
                    System.Windows.MessageBox.Show(ex.Message, "Unable to write Log File");
                }
            }
            return (isSaved);
        }

        internal void ReadFolder()

        {
            try
            {
                if (fileBrowser != null && !String.IsNullOrWhiteSpace(fileBrowser.WorkingFolder))
                {
                    Debug.WriteLine("ReadFolder on:- " + fileBrowser.WorkingFolder);
                    ReadSelectedFiles();
                    gpxHandler = new GpxHandler(fileBrowser.WorkingFolder);
                    //sessionForFolder = GetNewRecordingSession(fileBrowser);
                    sessionForFolder = SessionManager.CreateSession(fileBrowser.WorkingFolder, SessionManager.GetSessionTag(fileBrowser), gpxHandler);
                    //sessionForFolder.OriginalFilePath = fileBrowser.WorkingFolder;
                    /*
                    RecordingSessionForm sessionForm = new RecordingSessionForm();

                    sessionForm.SetRecordingSession(sessionForFolder);
                    if (sessionForm.ShowDialog() ?? false)
                    {
                        sessionForFolder = sessionForm.GetRecordingSession();
                        //DBAccess.UpdateRecordingSession(sessionForFolder);
                        CurrentSessionTag = sessionForFolder.SessionTag;
                        var existingSession = DBAccess.GetRecordingSession(CurrentSessionTag);
                        if (existingSession != null)
                        {
                            //DBAccess.DeleteSession(existingSession);
                            CurrentSessionId = existingSession.Id;
                        }
                        else
                        {
                            CurrentSessionId = 0;
                        }
                    }*/
                }
                if (sessionForFolder != null)
                {
                    DBAccess.UpdateRecordingSession(sessionForFolder);
                    var existingSession = DBAccess.GetRecordingSession(sessionForFolder.SessionTag);
                    if (existingSession != null)
                    {
                        CurrentSessionId = existingSession.Id;
                    }
                    else
                    {
                        CurrentSessionId = 0;
                    }
                }
                // Tools.SetFolderIconTick(fileBrowser.WorkingFolder);
            }
            catch (Exception ex)
            {
                Tools.ErrorLog(ex.Message);
                Debug.WriteLine("Import-ReadFolder Failed:-" + ex.Message);
            }
        }

        internal void SortFileOrder()
        {
            if (fileBrowser != null && fileBrowser.TextFileNames.Count > 1)
            {
                FileOrderDialog fod = new FileOrderDialog();
                fod.Populate(fileBrowser.TextFileNames);
                bool? result = fod.ShowDialog();
                if (result != null && result.Value)
                {
                    fileBrowser.TextFileNames = fod.GetFileList();

                    ReadSelectedFiles();
                }
            }
        }

        /// <summary>
        ///     The file browser
        /// </summary>
        private FileBrowser fileBrowser;

        /// <summary>
        ///     The file processor
        /// </summary>
        private FileProcessor fileProcessor;

        /// <summary>
        ///     The GPX handler
        /// </summary>
        private GpxHandler gpxHandler;

        /// <summary>
        /// indicates if the selected folder is to be processed as a set of
        /// Audacity text files (false) or as a set of wav files with Kaleidoscope
        /// metadata (true).
        /// </summary>
        private bool ProcessWavFiles = false;

        /// <summary>
        ///     The session for folder
        /// </summary>
        private RecordingSession sessionForFolder;

        /// <summary>
        ///     Batses the concatenate.
        /// </summary>
        /// <param name="TotalBatsFound">
        ///     The total bats found.
        /// </param>
        /// <param name="NewBatsFound">
        ///     The new bats found.
        /// </param>
        /// <returns>
        ///     </returns>
        private Dictionary<string, BatStats> BatsConcatenate(Dictionary<string, BatStats> TotalBatsFound, Dictionary<string, BatStats> NewBatsFound)
        {
            if (TotalBatsFound == null || NewBatsFound == null) return (TotalBatsFound);
            if (NewBatsFound.Count > 0)
            {
                foreach (var bat in NewBatsFound)
                {
                    if (TotalBatsFound.ContainsKey(bat.Key))
                    {
                        TotalBatsFound[bat.Key].Add(bat.Value);
                    }
                    else
                    {
                        TotalBatsFound.Add(bat.Key, bat.Value);
                    }
                }
            }
            return (TotalBatsFound);
        }

        private RecordingSession GetNewRecordingSession(FileBrowser fileBrowser)
        {
            RecordingSession newSession = new RecordingSession();
            newSession.LocationGPSLatitude = null;
            newSession.LocationGPSLongitude = null;

            newSession = SessionManager.PopulateSession(newSession, fileBrowser);
            return (newSession);
        }

        /// <summary>
        ///     Handles the Click event of the ImportFolderButton control. User selects a new
        ///     folder, the Next button is enabled, and auto-magically clicked.
        /// </summary>
        /// <param name="sender">
        ///     The source of the event.
        /// </param>
        /// <param name="e">
        ///     The <see cref="RoutedEventArgs"/> instance containing the event data.
        /// </param>
        private void ImportFolderButton_Click(object sender, RoutedEventArgs e)
        {
            importPictureControl.Visibility = Visibility.Hidden;
            OutputWindowScrollViewer.Visibility = Visibility.Visible;
            stackPanelScroller.Visibility = Visibility.Visible;
            UpdateRecordingButton.ToolTip = "Update a specific Recording by selecting a single .wav file";
            ProcessWavFiles = false;
            fileBrowser = new FileBrowser();
            fileBrowser.SelectRootFolder();
            NextFolderButton.IsEnabled = true;

            NextFolderButton_Click(sender, e);
        }

        /// <summary>
        /// Set import pictures mode.  Sets up a window with an image editor
        /// to allow images to be pasted in without being linked to a segment or call
        /// or bat.  The caption to the picture should allow it to be allocated
        /// appropriately.  A bat tag will associate the image with a Bat.
        /// A .wav filename will attach the image to that recording either
        /// now or when the recording eventually gets imported.  If the description
        /// field is populated and matches LabelledSegment, now or later, then the
        /// image will be associated with that labelledsegment.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void ImportPicturesButton_Click(object sender, RoutedEventArgs e)
        {
            stackPanelScroller.Visibility = Visibility.Hidden;
            OutputWindowScrollViewer.Visibility = Visibility.Hidden;
            importPictureControl.Visibility = Visibility.Visible;
            UpdateRecordingButton.ToolTip = "Find possible links for orphaned images";
            importPictureControl.imageEntryScroller.SetViewOnly(true);
            importPictureControl.imageEntryScroller.Clear();
            BulkObservableCollection<StoredImage> orphanImages = DBAccess.GetOrphanImages(null);
            if (!orphanImages.IsNullOrEmpty())
            {
                foreach (var image in orphanImages)
                {
                    importPictureControl.imageEntryScroller.AddImage(image);
                }
            }
        }

        /// <summary>
        /// Imports data using the metdata contined in a set of wav files which have been
        /// annotated using Kaleidoscope rather than Audacity.  The annotations may be
        /// encapsulated in either the Name or the Notes tag of the Kaleidoscope (wamd)
        /// metadata.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void ImportWavFilesButton_Click(object sender, RoutedEventArgs e)
        {
            importPictureControl.Visibility = Visibility.Hidden;
            OutputWindowScrollViewer.Visibility = Visibility.Visible;
            stackPanelScroller.Visibility = Visibility.Visible;
            fileBrowser = new FileBrowser();
            fileBrowser.SelectRootFolder();
            NextFolderButton.IsEnabled = true;
            UpdateRecordingButton.ToolTip = "Update a specific Recording by selecting a single .wav file";

            NextFolderButton_Click(sender, e);
            var fileList = Directory.EnumerateFiles(fileBrowser.rootFolder, "*.wav");
            //var FILEList= Directory.EnumerateFiles(fileBrowser.rootFolder, "*.WAV");
            //fileList = fileList.Concat<string>(FILEList);

            List<String> wavfiles = new List<string>(fileList);
            if (wavfiles == null || wavfiles.Count == 0)
            {
                ProcessFilesButton.IsEnabled = false;
                ProcessWavFiles = false;
            }
            else
            {
                ProcessFilesButton.IsEnabled = true;
                ProcessWavFiles = true;
            }
        }

        /// <summary>
        ///     Handles the Click event of the NextFolderButton control. Pops the next folder off
        ///     the fileBrowser folder queue, has fileBrowser Process the folder, then calls
        ///     ReadFolder() to load the files into the display. Enables buttons to allow further processing.
        /// </summary>
        /// <param name="sender">
        ///     The source of the event.
        /// </param>
        /// <param name="e">
        ///     The <see cref="RoutedEventArgs"/> instance containing the event data.
        /// </param>
        private void NextFolderButton_Click(object sender, RoutedEventArgs e)
        {
            if (fileBrowser.wavFileFolders != null && fileBrowser.wavFileFolders.Count > 0)
            {
                dpMMultiWindowPanel.Children.Clear();
                tbkOutputText.Text = "";
                fileBrowser.ProcessFolder(fileBrowser.PopWavFolder());
                ReadFolder();
                SortFileOrderButton.IsEnabled = true;
                ProcessFilesButton.IsEnabled = true;
                FilesToProcessLabel.Content = fileBrowser.wavFileFolders.Count + " Folders to Process";
                if (fileBrowser.wavFileFolders.Count <= 1)
                {
                    SelectFoldersButton.IsEnabled = false;
                }
                else
                {
                    SelectFoldersButton.IsEnabled = true;
                }
                if (fileBrowser.wavFileFolders.Count > 0)
                {
                    NextFolderButton.IsEnabled = true;
                }
                else
                {
                    NextFolderButton.IsEnabled = false;
                }
            }
            else
            {
                SortFileOrderButton.IsEnabled = false;
                ProcessFilesButton.IsEnabled = false;
                SelectFoldersButton.IsEnabled = false;
                NextFolderButton.IsEnabled = false;
            }
        }

        private void ProcessFilesButton_Click(object sender, RoutedEventArgs e)
        {
            if (ProcessFiles())
            {
                Tools.SetFolderIconTick(fileBrowser.WorkingFolder);
            }
        }

        /// <summary>
        /// Processes the wav files in the selected folder, extracting Kaleidoscope metadata
        /// stored in the wamd chunk.  Looks for a header text file or requests one, and allows
        /// manual data entry for the session data if necessary.
        /// </summary>
        private bool ProcessWavMetadata()
        {
            bool result = false;
            Dictionary<String, BatStats> TotalBatsFound = new Dictionary<string, BatStats>();

            try
            {
                var wavFiles = Directory.EnumerateFiles(fileBrowser.WorkingFolder, "*.wav");
                //var WAVFilesEnum = Directory.EnumerateFiles(fileBrowser.WorkingFolder, "*.WAV");
                //var wavFiles = wavFilesEnum.Concat<string>(WAVFilesEnum).ToList<string>();
                if (wavFiles != null && wavFiles.Count() > 0)
                {
                    if (sessionForFolder != null && sessionForFolder.Id > 0)
                    {
                        tbkOutputText.Text = sessionForFolder.ToFormattedString();
                        foreach (var rec in sessionForFolder.Recordings)
                        {
                            DBAccess.DeleteRecording(rec);//so that we can recreate them from scrathch using the file data
                        }
                    }

                    foreach (var filename in wavFiles)
                    {
                        if (!String.IsNullOrWhiteSpace(fileBrowser.HeaderFileName) && filename == fileBrowser.HeaderFileName)
                        {
                            // skip this file if it has been identified as the header data file, since
                            // the information should have been included as the session record header
                            // and this would be a duplicate.
                        }
                        else
                        {
                            tbkOutputText.Text = tbkOutputText.Text + "***\n\n" + FileProcessor.ProcessFile(filename, gpxHandler, CurrentSessionId, ref fileProcessor.BatsFound) + "\n";
                            TotalBatsFound = BatsConcatenate(TotalBatsFound, fileProcessor.BatsFound);
                        }
                    }

                    tbkOutputText.Text = tbkOutputText.Text + "\n#########\n\n";
                    if (TotalBatsFound != null && TotalBatsFound.Count > 0)
                    {
                        foreach (var bat in TotalBatsFound)
                        {
                            bat.Value.batCommonName = bat.Key;
                            tbkOutputText.Text += Tools.GetFormattedBatStats(bat.Value, false) + "\n";

                            //tbkOutputText.Text = tbkOutputText.Text +
                            // FileProcessor.FormattedBatStats(bat) + "\n";
                        }
                    }
                }

                if (!String.IsNullOrWhiteSpace(tbkOutputText.Text))
                {
                    SaveOutputFile();
                    result = true;
                }
            }
            catch (Exception ex)
            {
                Tools.ErrorLog(ex.Message);
                Debug.WriteLine("Processing of .wav files failed:- " + ex.Message);
                result = false;
            }
            return (result);
        }

        /*
        /// <summary>
        ///     Opens the folder.
        /// </summary>
        internal void OpenFolder()
        {
            if (!String.IsNullOrWhiteSpace(fileBrowser.SelectFolder()))
            {
                ReadFolder();
            }
        }*/

        private void SelectFoldersButton_Click(object sender, RoutedEventArgs e)
        {
            if (fileBrowser != null && fileBrowser.wavFileFolders != null && fileBrowser.wavFileFolders.Count > 1)
            {
                FolderSelectionDialog fsd = new FolderSelectionDialog();
                fsd.FolderList = fileBrowser.wavFileFolders;
                fsd.ShowDialog();
                if (fsd.DialogResult ?? false)
                {
                    fileBrowser.wavFileFolders = fsd.FolderList;
                    for (int i = 0; i < fileBrowser.wavFileFolders.Count; i++)
                    {
                        fileBrowser.wavFileFolders[i] = fileBrowser.wavFileFolders[i].Replace('#', ' ').Trim();
                    }

                    //fileBrowser.wavFileFolders = fsd.FolderList;
                    FilesToProcessLabel.Content = fileBrowser.wavFileFolders.Count + " Folders to Process";
                }
            }
        }

        private void SortFileOrderButton_Click(object sender, RoutedEventArgs e)
        {
            SortFileOrder();
        }

        /// <summary>
        /// Allows the user to select a .wav file, then finds the corresponding label
        /// file and updates the existing Recording.  If the label file or recording
        /// do not exist simply returns without doig anything further.  NB could display a
        /// message.  Sets a WaitCursor during processing.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void UpdateRecordingButton_Click(object sender, RoutedEventArgs e)
        {
            if (!(importPictureControl.Visibility == Visibility.Visible))
            {// in normal entry mode, so do Updat Recording
                if (fileBrowser == null)
                {
                    fileBrowser = new FileBrowser();
                }
                string filename = fileBrowser.SelectWavFile();
                if (!String.IsNullOrWhiteSpace(filename))
                {
                    using (new WaitCursor("Updating Recording"))
                    {
                        Recording recording = DBAccess.GetRecordingForWavFile(filename);
                        string labelFileName = fileBrowser.GetLabelFileForRecording(recording);
                        if (!String.IsNullOrWhiteSpace(labelFileName))
                        {
                            if (fileProcessor == null)
                            {
                                fileProcessor = new FileProcessor();
                                fileProcessor.UpdateRecording(recording, labelFileName);
                            }
                        }
                    }
                }
            }
            else
            {// in image entry mode so try to de-orphanise orphan images
                DBAccess.ResolveOrphanImages();
                importPictureControl.imageEntryScroller.Clear();
                BulkObservableCollection<StoredImage> orphanImages = DBAccess.GetOrphanImages(null);
                if (!orphanImages.IsNullOrEmpty())
                {
                    foreach (var image in orphanImages)
                    {
                        importPictureControl.imageEntryScroller.AddImage(image);
                    }
                }
            }
        }
    }
}