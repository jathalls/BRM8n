using System;
using System.Windows;

namespace BatRecordingManager
{
    /// <summary>
    /// Interaction logic for ImportPictureDialog.xaml
    /// </summary>
    public partial class ImportPictureDialog : Window
    {
        /// <summary>
        /// Allows the import picture control to be used in a stand-alone dialog mode
        /// </summary>
        public ImportPictureDialog()
        {
            importPictureControl = Activator.CreateInstance<ImportPictureControl>();
            InitializeComponent();
        }

        /// <summary>
        ///
        /// </summary>
        /// <param name="fileName"></param>
        internal void setCaption(string fileName)
        {
            importPictureControl.SetCaption(fileName);
        }

        /// <summary>
        ///
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void UpdateAndCloseButton_Click(object sender, RoutedEventArgs e)
        {
            DBAccess.ResolveOrphanImages();
            /*
             * NB this code only required if the window is to stay open and the content needs to
             * be refreshed after the update to the database.
             *
            importPictureControl.imageEntryScroller.Clear();
            BulkObservableCollection<StoredImage> orphanImages = DBAccess.GetOrphanImages(null);
            if (!orphanImages.IsNullOrEmpty())
            {
                foreach (var image in orphanImages)
                {
                    importPictureControl.imageEntryScroller.AddImage(image);
                }
            }*/
            this.Close();
        }
    }
}