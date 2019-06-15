using Microsoft.VisualStudio.Language.Intellisense;
using System;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;

namespace BatRecordingManager
{
    /// <summary>
    ///     Interaction logic for FolderSelectionDialog.xaml
    /// </summary>
    public partial class FolderSelectionDialog : Window
    {
        #region FolderList

        /// <summary>
        ///     FolderList Dependency Property
        /// </summary>
        public static readonly DependencyProperty FolderListProperty =
            DependencyProperty.Register("FolderList", typeof(BulkObservableCollection<String>), typeof(FolderSelectionDialog),
                new FrameworkPropertyMetadata((BulkObservableCollection<String>)new BulkObservableCollection<String>()));

        /// <summary>
        ///     Gets or sets the FolderList property. This dependency property indicates ....
        /// </summary>
        public BulkObservableCollection<String> FolderList
        {
            get
            {
                return (BulkObservableCollection<String>)GetValue(FolderListProperty);
                /*if (list != null)
                {
                    for(int i = 0; i < list.Count; i++)
                    {
                        list[i] = list[i].Replace('#', ' ').Trim();
                    }
                }*/
                //return (list);
            }
            set
            {
                if (!value.IsNullOrEmpty())
                {
                    for (int i = 0; i < value.Count; i++)
                    {
                        var folder = value[i];
                        if (!folder.Contains("###") && DBAccess.FolderExists(folder))
                        {
                            value[i] = folder + " ###";
                        }
                    }
                }
                SetValue(FolderListProperty, value);
            }
        }

        #endregion FolderList

        /// <summary>
        ///     Initializes a new instance of the <see cref="FolderSelectionDialog"/> class.
        /// </summary>
        public FolderSelectionDialog()
        {
            InitializeComponent();
            DataContext = this;
        }

        private void AddFolderButton_Click(object sender, RoutedEventArgs e)
        {
            FileBrowser browser = new FileBrowser();
            browser.SelectHeaderTextFile();
            if (browser.WorkingFolder != null && !String.IsNullOrWhiteSpace(browser.WorkingFolder))
            {
                if (Directory.Exists(browser.WorkingFolder))
                {
                    FolderList.Add(browser.WorkingFolder);
                }
            }
        }

        private void AddFolderTreeButton_Click(object sender, RoutedEventArgs e)
        {
            FileBrowser browser = new FileBrowser();
            browser.SelectRootFolder();
            if (!browser.wavFileFolders.IsNullOrEmpty())
            {
                var combinedList = ((FolderList.Concat(browser.wavFileFolders)).Distinct());
                FolderList = (BulkObservableCollection<String>)combinedList;
            }
        }

        private void ButtonDeleteFolder_Click(object sender, RoutedEventArgs e)
        {
            Button thisButton = sender as Button;
            String ItemToDelete = ((thisButton.Parent as Grid).Children[1] as TextBox).Text;
            FolderList.Remove(ItemToDelete);

            ICollectionView view = CollectionViewSource.GetDefaultView(FolderListView.ItemsSource);
            view.Refresh();
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            this.Close();
        }

        private void OKButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = true;
            this.Close();
        }

        private void OnListViewItemFocused(object sender, RoutedEventArgs e)
        {
            ListViewItem lvi = sender as ListViewItem;
            lvi.IsSelected = true;
        }

        private void OnTextBoxFocused(object sender, RoutedEventArgs e)
        {
            TextBox segmentTextBox = sender as TextBox;
            Button myDelButton = ((segmentTextBox.Parent as Grid).Children[0] as Button);
            myDelButton.Visibility = Visibility.Visible;
        }
    }
}