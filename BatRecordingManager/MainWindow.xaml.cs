using System;
using System.Diagnostics;
using System.IO;
using System.Windows;
using System.Windows.Input;

namespace BatRecordingManager
{
    /// <summary>
    ///     Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        /// <summary>
        ///     The build
        /// </summary>
        private String Build;

        /// <summary>
        ///     The is saved
        /// </summary>
        //private bool isSaved = true;

        /// <summary>
        ///     The window title
        /// </summary>
        private String windowTitle = "Bat Log Manager - v";

        #region statusText

        /// <summary>
        /// statusText Dependency Property
        /// </summary>
        public static readonly DependencyProperty statusTextProperty =
            DependencyProperty.Register("statusText", typeof(String), typeof(MainWindow),
                new FrameworkPropertyMetadata((String)""));

        /// <summary>
        /// Gets or sets the statusText property.  This dependency property 
        /// indicates ....
        /// </summary>
        public String statusText
        {
            get { return (String)GetValue(statusTextProperty); }
            set { SetValue(statusTextProperty, value); }
        }

        #endregion





        private ImportControl importControl { get; } = new ImportControl();
        private BatListControl batListControl { get; } = new BatListControl();

        private RecordingSessionListDetailControl recordingSessionListControl { get; } = new RecordingSessionListDetailControl();
        private BatRecordingsListDetailControl BatRecordingListDetailControl { get; } = new BatRecordingsListDetailControl();

        /// <summary>
        /// Flag to indicate if the database listing item in help is enabled
        /// </summary>
        public bool ShowDatabase{get;set;}=false;

        /// <summary>
        ///     Initializes a new instance of the <see cref="MainWindow"/> class.
        /// </summary>
        public MainWindow()
        {
            try
            {
                try
                {
                    System.Windows.Application.Current.ShutdownMode = ShutdownMode.OnMainWindowClose;
                    DBAccess.InitializeDatabase();
                }catch(Exception ex)
                {
                    Debug.WriteLine("Error in Main Window prior to Initialisation");
                    Tools.ErrorLog("Error in Main Window prior to Initialisation "+ex.Message);
                }
                InitializeComponent();
                try
                {
                    this.ShowDatabase = App.ShowDatabase;
                    this.DataContext = this;
                    statusText = "Starting Up";

                    Build = System.Reflection.Assembly.GetExecutingAssembly().GetName().Version.ToString();
                    DateTime buildDateTime = new DateTime(2000, 1, 1);
                    var buildParts = Build.Split('.');
                    if (buildParts.Length >= 4)
                    {
                        int days = 0;
                        int seconds = 0;
                        int.TryParse(buildParts[2], out days);
                        int.TryParse(buildParts[3], out seconds);
                        if (days > 0)
                        {
                            buildDateTime = buildDateTime.AddDays(days);
                        }
                        if (seconds > 0)
                        {
                            buildDateTime = buildDateTime.AddSeconds(seconds * 2);
                        }
                    }
                    if (buildDateTime.Ticks > 0L)
                    {
                        Build = Build + " (" + buildDateTime.Date.ToShortDateString() + " " + buildDateTime.TimeOfDay.ToString() + ")";
                    }
                    //windowTitle = "Bat Log File Processor " + Build;
                    this.Title = windowTitle + " " + Build;

                    this.InvalidateArrange();
                    //DBAccess.InitializeDatabase();

                    BatRecordingListDetailControl.sessionsAndRecordings.SessionAction += SessionsAndRecordings_SessionAction;
                    miRecordingSearch_Click(this, new RoutedEventArgs());
                    this.statusText = "";
                }catch(Exception ex)
                {
                    Debug.WriteLine("Error in Main Window following Initialization");
                    Tools.ErrorLog("Error in Main Window following Initialization "+ex.Message);
                }
                try
                {
                    if (!MainWindowPaneGrid.Children.Contains(recordingSessionListControl))
                    {
                        MainWindowPaneGrid.Children.Add(recordingSessionListControl);
                    }
                    recordingSessionListControl.Visibility = Visibility.Visible;
                    recordingSessionListControl.RefreshData();
                }catch(Exception ex)
                {
                    Debug.WriteLine("Error in Main Window showing SessionsPane");
                    Tools.ErrorLog("Error in Main Window showing sessions pane " + ex.Message);
                }
            }
            catch (Exception ex)
            {
                Tools.ErrorLog(ex + "\n" + ex.Message);
            }
        }

        /// <summary>
        ///     Display the About box
        /// </summary>
        /// <param name="sender">
        ///     The source of the event.
        /// </param>
        /// <param name="e">
        ///     The <see cref="RoutedEventArgs"/> instance containing the event data.
        /// </param>
        private void miAbout_Click(object sender, RoutedEventArgs e)
        {
            AboutScreen about = new AboutScreen();
            about.version.Content = "v " + Build;
            about.ShowDialog();
        }

        public String setStatusText(String newStatusText)
        {
            /*
            string result = statusText;
            statusText = newStatusText;
            return (result);*/
            string result = StatusText.Text;
            StatusText.Text = newStatusText;
            this.InvalidateArrange();
           this.UpdateLayout();
           
            Debug.WriteLine("========== set status to \"" + newStatusText + "\"");
            return (result);
        }

        /// <summary>
        ///     Handles the Click event of the miBatReference control.
        /// </summary>
        /// <param name="sender">
        ///     The source of the event.
        /// </param>
        /// <param name="e">
        ///     The <see cref="RoutedEventArgs"/> instance containing the event data.
        /// </param>
        private void miBatReference_Click(object sender, RoutedEventArgs e)
        {
            using (new WaitCursor("Switch to Bat Reference Pane"))
            {
                HideAllControlPanes();
                if (!MainWindowPaneGrid.Children.Contains(batListControl))
                {
                    MainWindowPaneGrid.Children.Add(batListControl);
                }

                batListControl.Visibility = Visibility.Visible;
                batListControl.RefreshData();
                this.InvalidateArrange();
            }
        }

        private void HideAllControlPanes()
        {
            BatRecordingListDetailControl.Visibility = Visibility.Hidden;
            recordingSessionListControl.Visibility = Visibility.Hidden;
            importControl.Visibility = Visibility.Hidden;
            batListControl.Visibility = Visibility.Hidden;
        }

#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member

        public void GetSelectedItems(out RecordingSession session, out Recording recording, out Bat bat)
#pragma warning restore CS1591 // Missing XML comment for publicly visible type or member
        {
            session = null;
            recording = null;
            bat = null;
            if (recordingSessionListControl != null)
            {
                session = recordingSessionListControl.GetSelectedSession();
                recording = recordingSessionListControl.GetSelectedRecording();
            }
            if (batListControl != null)
            {
                bat = batListControl.GetSelectedBat();
            }
        }

        /// <summary>
        ///     Handles the Click event of the miBatSearch control.
        /// </summary>
        /// <param name="sender">
        ///     The source of the event.
        /// </param>
        /// <param name="e">
        ///     The <see cref="RoutedEventArgs"/> instance containing the event data.
        /// </param>
        private void miBatSearch_Click(object sender, RoutedEventArgs e)
        {
            using (new WaitCursor())
            {
                HideAllControlPanes();
                if (!MainWindowPaneGrid.Children.Contains(BatRecordingListDetailControl))
                {
                    MainWindowPaneGrid.Children.Add(BatRecordingListDetailControl);
                }

                BatRecordingListDetailControl.Visibility = Visibility.Visible;

                BatRecordingListDetailControl.RefreshData();

                //this.InvalidateArrange();
                //this.UpdateLayout();
            }
        }

        private void miCreateDatabase_Click(object sender, RoutedEventArgs e)
        {
            System.Windows.Forms.SaveFileDialog dialog = new System.Windows.Forms.SaveFileDialog();
            dialog.InitialDirectory = DBAccess.GetWorkingDatabaseLocation();
            dialog.FileName = "_BatReferenceDB.mdf";
            dialog.DefaultExt = ".mdf";
            var result = dialog.ShowDialog();
            if (result == System.Windows.Forms.DialogResult.OK)
            {
                int index = 1;
                while (File.Exists(dialog.FileName))
                {
                    dialog.FileName = dialog.FileName.Substring(0, dialog.FileName.Length - 4) + index.ToString() + ".mdf";
                }
                using (new WaitCursor("Creating new empty database"))
                {
                    try
                    {
                        string err = DBAccess.CreateDatabase(dialog.FileName);
                        if (!String.IsNullOrWhiteSpace(err))
                        {
                            MessageBox.Show(err, "Unable to create database");
                        }
                        else
                        {
                            err = DBAccess.SetDatabase(dialog.FileName);
                            if (!String.IsNullOrWhiteSpace(err))
                            {
                                MessageBox.Show(err, "Unable to set new DataContext for selected Database");
                            }
                            using (new WaitCursor("Refreshing the display"))
                            {
                                RefreshAll();
                                miRecordingSearch_Click(sender, e);
                            }
                        }
                    }
                    catch (Exception ex) { Tools.ErrorLog(ex.Message); }
                }
            }
        }

        /// <summary>
        ///     Handles the Click event of the miDatabase control. Allows the user to select an
        ///     alternative .mdf database file with the name BatReferenceDB but in an alternative
        ///     location. Selection of a different filename will be rejected in case the database
        ///     structure is different. The location of the selected file will be stired in the
        ///     global static App.dbFileLocation variable whence it can be referenced by the
        ///     DBAccess static functions.
        /// </summary>
        /// <param name="sender">
        ///     The source of the event.
        /// </param>
        /// <param name="e">
        ///     The <see cref="RoutedEventArgs"/> instance containing the event data.
        /// </param>
        private void miDatabase_Click(object sender, RoutedEventArgs e)
        {
            string WorkingFolder = DBAccess.GetWorkingDatabaseLocation();
            if (String.IsNullOrWhiteSpace(WorkingFolder) || !Directory.Exists(WorkingFolder))
            {
                App.dbFileLocation = "";
                WorkingFolder = DBAccess.GetWorkingDatabaseLocation();
            }
            using (System.Windows.Forms.OpenFileDialog dialog = new System.Windows.Forms.OpenFileDialog())
            {
                if (!String.IsNullOrWhiteSpace(WorkingFolder))
                {
                    dialog.InitialDirectory = WorkingFolder;
                }
                else
                {
                    WorkingFolder = Directory.GetCurrentDirectory();
                    dialog.InitialDirectory = WorkingFolder;
                }
                dialog.Filter = "mdf files|*.mdf";

                dialog.Multiselect = false;
                dialog.Title = "Select An Alternative BatReferenceDB.mdf database file";
                dialog.DefaultExt = ".mdf";

                dialog.FileName = "*.mdf";
                if (dialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
                {
                    string valid = DBAccess.ValidateDatabase(dialog.FileName);
                    if (valid == "bad")
                    {
                        System.Windows.MessageBox.Show(@"The selected file is not a valid BatRecordingManager Database.
                            Please reselect");
                    }
                    else
                    {
                        if (valid == "old")
                        {
                            var mbResult = System.Windows.MessageBox.Show(@"The elected file is an earlier version database.
Do you wish to update that database to the latest specification?", "Out of Date Database", MessageBoxButton.YesNo);
                            if (mbResult == MessageBoxResult.Yes)
                            {
                                using (new WaitCursor("Opening new database..."))
                                {
                                    DBAccess.SetDatabase(dialog.FileName);
                                    RefreshAll();
                                    miRecordingSearch_Click(sender, e);
                                }
                            }
                        }
                        else
                        {
                            using (new WaitCursor("Opening new database..."))
                            {
                                DBAccess.SetDatabase(dialog.FileName);
                                RefreshAll();
                                miRecordingSearch_Click(sender, e);
                            }
                        }
                    }
                }
            }
        }

#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member

        public void RefreshAll()
#pragma warning restore CS1591 // Missing XML comment for publicly visible type or member
        {
            using (new WaitCursor("Refreshing Data"))
            {
                recordingSessionListControl.RefreshData();
                BatRecordingListDetailControl.RefreshData();
                batListControl.RefreshData();
            }
        }

        /// <summary>
        ///     Quits the program
        /// </summary>
        /// <param name="sender">
        ///     The source of the event.
        /// </param>
        /// <param name="e">
        ///     The <see cref="RoutedEventArgs"/> instance containing the event data.
        /// </param>
        private void miExit_Click(object sender, RoutedEventArgs e)
        {
            this.Close();

            //App.Current.Shutdown();
            //Environment.Exit(0);
        }

        /// <summary>
        ///     Handles the Click event of the miHelp control.
        /// </summary>
        /// <param name="sender">
        ///     The source of the event.
        /// </param>
        /// <param name="e">
        ///     The <see cref="RoutedEventArgs"/> instance containing the event data.
        /// </param>
        private void miHelp_Click(object sender, RoutedEventArgs e)
        {
            string helpfile = @"Bat Recording Manager.chm";
            if (File.Exists(helpfile))
            {
                System.Windows.Forms.Help.ShowHelp(null, helpfile);
            }
            /*
            HelpScreen help = new HelpScreen();
            help.ShowDialog();*/
        }

        /// <summary>
        ///     Handles the Click event of the miNewLogFile control.
        /// </summary>
        /// <param name="sender">
        ///     The source of the event.
        /// </param>
        /// <param name="e">
        ///     The <see cref="RoutedEventArgs"/> instance containing the event data.
        /// </param>
        private void miNewLogFile_Click(object sender, RoutedEventArgs e)
        {
            using (new WaitCursor("Switching to Import View"))
            {
                HideAllControlPanes();

                if (!MainWindowPaneGrid.Children.Contains(importControl))
                {
                    MainWindowPaneGrid.Children.Add(importControl);
                }

                importControl.Visibility = Visibility.Visible;
                this.InvalidateArrange();
                this.UpdateLayout();
            }
        }

        /// <summary>
        ///     Handles the Click event of the miRecordingSearch control.
        /// </summary>
        /// <param name="sender">
        ///     The source of the event.
        /// </param>
        /// <param name="e">
        ///     The <see cref="RoutedEventArgs"/> instance containing the event data.
        /// </param>
        private void miRecordingSearch_Click(object sender, RoutedEventArgs e)
        {
            using (new WaitCursor("Switching to Sessions View..."))
            {
                HideAllControlPanes();
                recordingSessionListControl.Visibility = Visibility.Visible;
                recordingSessionListControl.RefreshData();
                this.InvalidateArrange();
                this.UpdateLayout();
            }
        }

        private void miSetToDefaultDatabase_Click(object sender, RoutedEventArgs e)
        {
            using (new WaitCursor("Switching to default database"))
            {
                DBAccess.SetDatabase(null);
                RefreshAll();
                miRecordingSearch_Click(sender, e);
            }
        }

        /// <summary>
        ///     Handles the SessionAction event of the SessionsAndRecordings control.
        /// </summary>
        /// <param name="sender">
        ///     The source of the event.
        /// </param>
        /// <param name="e">
        ///     The <see cref="SessionActionEventArgs"/> instance containing the event data.
        /// </param>
        private void SessionsAndRecordings_SessionAction(object sender, SessionActionEventArgs e)
        {
            miRecordingSearch_Click(this, new RoutedEventArgs());
            recordingSessionListControl.Select(e.RecordingSessionId);
        }

        /// <summary>
        ///     Handles the Closing event of the Window control.
        /// </summary>
        /// <param name="sender">
        ///     The source of the event.
        /// </param>
        /// <param name="e">
        ///     The <see cref="System.ComponentModel.CancelEventArgs"/> instance containing the
        ///     event data.
        /// </param>
        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            DBAccess.CloseDatabase();
            AudioHost.Instance.Close();
            ComparisonHost.Instance.Close();
            
        }

        /// <summary>
        /// Instance holder for the Analyze and Import class.  If null, then a new folder
        /// needs to be selectedd, otherwise analyses the next .wav file in the currently
        /// selected folder.
        /// </summary>
        private AnalyseAndImportClass analyseAndImport = null;

        private ImportPictureDialog importPictureDialog = null;

        private bool runKaleidoscope = false;
        /// <summary>
        /// Menu Item to analyse a folder full of .wav files using Audacity
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void miAnalyseFiles_Click(object sender, RoutedEventArgs e)
        {
            runKaleidoscope = false;
            if(Keyboard.IsKeyDown(Key.LeftCtrl) || Keyboard.IsKeyDown(Key.RightCtrl))
            {
                runKaleidoscope = true;
            }
            if (analyseAndImport == null)
            {
                importPictureDialog = new ImportPictureDialog();

                analyseAndImport = new AnalyseAndImportClass();
                analyseAndImport.Analysing += AnalyseAndImport_Analysing;
                analyseAndImport.DataUpdated += AnalyseAndImport_DataUpdated;
                analyseAndImport.AnalysingFinished += AnalyseAndImport_AnalysingFinished;
                if (analyseAndImport == null || !analyseAndImport.folderSelected)
                {
                    MessageBox.Show("No Folder selected for analysis");
                    analyseAndImport = null;
                    if (importPictureDialog != null)
                    {
                        importPictureDialog.Close();
                        importPictureDialog = null;
                    }
                    return;
                }
                importPictureDialog.Show();
                using (new WaitCursor("Import from Audacity or Kaleidoscope"))
                {
                    if (runKaleidoscope)
                    {
                        importPictureDialog.GotFocus += ImportPictureDialog_GotFocus;
                        analyseAndImport.ImportFromKaleidoscope();
                    }
                    else
                    {
                        analyseAndImport.AnalyseNextFile();
                    }
                }
            }
        }
        /// <summary>
        /// For Kaleidoscope, generates an event when the import picture dialog gets the focus.
        /// Causes the title of the current file to be placed in the image caption
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void ImportPictureDialog_GotFocus(object sender, RoutedEventArgs e)
        {
            var windows = OpenWindowGetter.GetOpenWindows();
           
            foreach(var win in windows)
            {
                if((win.Value as String).ToUpper().EndsWith(".WAV"))
                {
                    SetImportImageCaption((win.Value as string));
                    return;
                }
            }

        }

        /// <summary>
        /// responds to the Analysing event from AnalyseAndImport - supplies the name of
        /// the file currently being analysed
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void AnalyseAndImport_Analysing(object sender, EventArgs e)
        {
            AnalysingEventArgs aea = e as AnalysingEventArgs;
            string fileName = aea.FileName;
            SetImportImageCaption(fileName);
        }

        private void SetImportImageCaption(string caption) { 


            if (importPictureDialog != null)
            {
                importPictureDialog.Dispatcher.BeginInvoke(System.Windows.Threading.DispatcherPriority.Background,
                new Action(() =>
                {
                    importPictureDialog.setCaption(caption);
                }));
            }
        }

        /// <summary>
        /// Event handler when there are no more files to analyse or Audacity was closed without
        /// producing a matching text file.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void AnalyseAndImport_AnalysingFinished(object sender, EventArgs e)
        {
            if (analyseAndImport != null)
            {
                analyseAndImport.Close();

                analyseAndImport = null;
            }
            if (importPictureDialog != null)
            {
                importPictureDialog.Dispatcher.BeginInvoke(System.Windows.Threading.DispatcherPriority.Background,
                new Action(() =>
                {
                    importPictureDialog.Close();
                    importPictureDialog = null;
                }));
            }
        }

        /// <summary>
        /// event raised when the database has been updated by AnalyseAndImport
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void AnalyseAndImport_DataUpdated(object sender, EventArgs e)
        {
            if (sender != null)
            {
                
                if (recordingSessionListControl != null)
                {
                    recordingSessionListControl.RefreshData();
                }
                string sessionUpdated = (sender as AnalyseAndImportClass).SessionTag;
                if (!string.IsNullOrWhiteSpace(sessionUpdated) && runKaleidoscope)
                {
                    var mbResult=MessageBox.Show("Do you wish to Generate a report for this dataset?", "Generate Report?", MessageBoxButton.YesNo);
                    if (mbResult == MessageBoxResult.Yes)
                    {
                        recordingSessionListControl.Dispatcher.BeginInvoke(System.Windows.Threading.DispatcherPriority.Background,
                            //Application.Current.Dispatcher.BeginInvoke(System.Windows.Threading.DispatcherPriority.Background,
                            new Action(() =>
                            {
                                //recordingSessionListControl.RefreshData();
                                recordingSessionListControl.SelectSession(sessionUpdated);

                                recordingSessionListControl.ReportSessionDataButton_Click(sender, new RoutedEventArgs());
                            }));
                    }
                }
            }
            if (analyseAndImport != null)
            {
                analyseAndImport.Close();

                analyseAndImport = null;
            }

        }

        private void miImportBatData_Click(object sender, RoutedEventArgs e)
        {
            using (System.Windows.Forms.OpenFileDialog fileDialog = new System.Windows.Forms.OpenFileDialog())
            {
                fileDialog.AddExtension = true;
                fileDialog.CheckFileExists = true;
                fileDialog.DefaultExt = ".xml";
                fileDialog.Multiselect = false;
                fileDialog.Title = "Import Bat Data from XML file";
                fileDialog.Filter = "XML files (*.xml)|*.xml|All Files (*.*)|*.*";

                if (fileDialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
                {
                    string selectedFile = fileDialog.FileName;
                    if (File.Exists(selectedFile))
                    {
                        using (new WaitCursor("Importing new bat reference data"))
                        {
                            DBAccess.CopyXMLDataToDatabase(selectedFile);
                        }
                    }
                }
            }
        }

        private bool doingOnClosed = false;
        private void Window_Closed(object sender, EventArgs e)
        {
            if (!doingOnClosed)
            {
                doingOnClosed = true;
                base.OnClosed(e);
                Application.Current.Shutdown();
            }
        }

        private void MiDatabaseDisplay_Click(object sender, RoutedEventArgs e)
        {
            Database display = null;
            try
            {
                display = new Database();
            }catch(NullReferenceException nre)
            {
                Trace.WriteLine("Database display creation null reference exception:- " + nre.StackTrace);
            }
            try
            {
                if (display != null)
                {
                    display.Show();
                }
            }catch(NullReferenceException nre)
            {
                Trace.WriteLine("display show null reference exception: " + nre.StackTrace);
            }
        }
    }
}