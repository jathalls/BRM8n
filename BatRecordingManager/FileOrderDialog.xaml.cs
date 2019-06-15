using Microsoft.VisualStudio.Language.Intellisense;
using System;
using System.Windows;

namespace BatRecordingManager
{
    /// <summary>
    ///     Interaction logic for FileOrderDialog.xaml
    /// </summary>
    public partial class FileOrderDialog : Window
    {
        //private BulkObservableCollection<String> fileList;

        #region fileList

        /// <summary>
        ///     fileList Dependency Property
        /// </summary>
        public static readonly DependencyProperty fileListProperty =
            DependencyProperty.Register("fileList", typeof(BulkObservableCollection<String>), typeof(FileOrderDialog),
                new FrameworkPropertyMetadata((new BulkObservableCollection<String>())));

        /// <summary>
        ///     Gets or sets the fileList property. This dependency property indicates ....
        /// </summary>
        public BulkObservableCollection<String> fileList
        {
            get { return (BulkObservableCollection<String>)GetValue(fileListProperty); }
            set { SetValue(fileListProperty, value); }
        }

        #endregion fileList

        /// <summary>
        ///     Default Constructor
        /// </summary>
        public FileOrderDialog()
        {
            InitializeComponent();
            DataContext = this;
            fileList = new BulkObservableCollection<String>();
            FileListBox.ItemsSource = new BulkObservableCollection<String>();
        }

        /// <summary>
        ///     Returns the list of strings displayed in the dialog
        /// </summary>
        /// <returns>
        ///     </returns>
        internal BulkObservableCollection<String> GetFileList()
        {
            return (fileList);
        }

        /// <summary>
        ///     Populates the list box with the supplied list of strings
        /// </summary>
        /// <param name="list">
        ///     </param>
        internal void Populate(BulkObservableCollection<String> list)
        {
            fileList = list;
        }

        /// <summary>
        ///     Allows the user to select additional files to add to the existing file list
        /// </summary>
        /// <param name="sender">
        ///     </param>
        /// <param name="e">
        ///     </param>
        private void ADDButton_Click(object sender, RoutedEventArgs e)
        {
            FileBrowser additionalFileBrowser = new FileBrowser();
            additionalFileBrowser.SelectLogFiles();
            if (!additionalFileBrowser.TextFileNames.IsNullOrEmpty())
            {
                // some additional names have been chosen
                foreach (var file in additionalFileBrowser.TextFileNames)
                {
                    if (!fileList.Contains(file))
                    {
                        fileList.Add(file);
                    }
                }
            }
        }

        /// <summary>
        ///     Causes the selected file name to be deleted from the file list
        /// </summary>
        /// <param name="sender">
        ///     </param>
        /// <param name="e">
        ///     </param>
        private void DELButton_Click(object sender, RoutedEventArgs e)
        {
            if (FileListBox.SelectedIndex >= 0 && FileListBox.SelectedItem != null)
            {
                if (fileList.Contains((String)FileListBox.SelectedItem))
                {
                    fileList.Remove((String)FileListBox.SelectedItem);
                }
            }
            FileListBox.Items.Refresh();
        }

        /// <summary>
        ///     Moves the selected item down one place in the list
        /// </summary>
        /// <param name="sender">
        ///     </param>
        /// <param name="e">
        ///     </param>
        private void DOWNButton_Click(object sender, RoutedEventArgs e)
        {
            if (FileListBox.SelectedIndex >= 0 && FileListBox.SelectedItem != null)
            {
                if (FileListBox.SelectedIndex >= 0 && FileListBox.SelectedIndex < FileListBox.Items.Count - 1)
                {
                    int selectedIndex = FileListBox.SelectedIndex;
                    String temp = fileList[selectedIndex];
                    fileList[selectedIndex] = fileList[selectedIndex + 1];
                    fileList[selectedIndex + 1] = temp;
                    FileListBox.Items.Refresh();
                    FileListBox.SelectedIndex = selectedIndex + 1;
                }
            }
        }

        /// <summary>
        ///     Responds to the OK button by closing the dialog and returning true
        /// </summary>
        /// <param name="sender">
        ///     </param>
        /// <param name="e">
        ///     </param>
        private void OKButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = true;
            this.Close();
        }

        /// <summary>
        ///     Moves the selected item one place up in the list
        /// </summary>
        /// <param name="sender">
        ///     </param>
        /// <param name="e">
        ///     </param>
        private void UPButton_Click(object sender, RoutedEventArgs e)
        {
            if (FileListBox.SelectedIndex >= 0 && FileListBox.SelectedItem != null)
            {
                if (FileListBox.SelectedIndex > 0 && FileListBox.SelectedIndex < fileList.Count)
                {
                    int selectedIndex = FileListBox.SelectedIndex;
                    String temp = fileList[selectedIndex];
                    fileList[selectedIndex] = fileList[selectedIndex - 1];
                    fileList[selectedIndex - 1] = temp;
                    FileListBox.Items.Refresh();
                    FileListBox.SelectedIndex = selectedIndex - 1;
                }
            }
        }
    }
}