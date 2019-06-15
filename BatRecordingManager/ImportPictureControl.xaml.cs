using System;
using System.Windows;
using System.Windows.Controls;

namespace BatRecordingManager
{
    /// <summary>
    /// Interaction logic for ImportPictureControl.xaml
    /// </summary>
    public partial class ImportPictureControl : UserControl
    {
        /// <summary>
        /// ImportPicture Control provides screen for adding images as a panel in the Import
        /// screen or as a stand-alone dialog
        /// </summary>
        public ImportPictureControl()
        {
            imageEntryScroller = Activator.CreateInstance<ImageScrollerControl>();
            ImageEntryControl = Activator.CreateInstance<ImageDialogControl>();
            InitializeComponent();
            ImageEntryControl.OKButtonClicked += ImageEntryControl_OKButtonClicked;
            imageEntryScroller.ButtonPressed += ImageEntryScroller_ButtonPressed;
        }

        /// <summary>
        /// Applies the string fileName to the caption for imported images
        /// </summary>
        /// <param name="fileName"></param>
        internal void SetCaption(string fileName)
        {
            ImageEntryControl.storedImage.caption = fileName;
        }

        private void ImageEntryControl_OKButtonClicked(object sender, EventArgs e)
        {
            StoredImage imageToSave = ImageEntryControl.GetStoredImage();
            if (imageToSave.image != null)
            {
                imageToSave = DBAccess.InsertImage(imageToSave);
                imageEntryScroller.AddImage(imageToSave);
                ImageEntryControl.Clear(false);
            }
        }

        private void ImageEntryScroller_ButtonPressed(object sender, EventArgs e)
        {
            ButtonPressedEventArgs bpArgs = e as ButtonPressedEventArgs;
            if (bpArgs.fromDatabase)
            {
                var result = MessageBox.Show("Are you sure you want to delete this image from the databse?\nThis deletion is permanent nd cannot be reversed!",
                    "Delete From Database?",
                    MessageBoxButton.YesNo);
                if (result == MessageBoxResult.Yes) {
                    
    
                    bpArgs.image.delete();
                }
            }
            imageEntryScroller.DeleteImage();
        }
    }
}