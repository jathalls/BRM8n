using System;
using System.Windows;

namespace BatRecordingManager
{
    /// <summary>
    /// Interaction logic for ImageDialog.xaml
    /// </summary>
    public partial class ImageDialog : Window
    {
        public bool? DialogResult;

        /// <summary>
        /// base level constructor, instantiates the imageDialogControl and sets up the OKbutton
        /// event handler.
        /// </summary>
        public ImageDialog()
        {
            InitializeComponent();
            if (imageDialogControl != null)
            {
                imageDialogControl.SetImageDialogControl("", "");
                imageDialogControl.OKButtonClicked += ImageDialogControl_OKButtonClicked;
            }
        }

        /// <summary>
        /// Modified constructor which sets a caption and description for the image
        /// </summary>
        /// <param name="caption"></param>
        /// <param name="description"></param>
        public ImageDialog(string caption, string description) : this()
        {
            if (imageDialogControl != null)
            {
                imageDialogControl.SetImageDialogControl(caption, description);
            }
        }

        internal StoredImage GetStoredImage()
        {
            return (imageDialogControl.storedImage);
        }

        /// <summary>
        /// Dialog OK button handler responds to event generated in the imageDialoGControl
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void ImageDialogControl_OKButtonClicked(object sender, EventArgs e)
        {
            this.Visibility = Visibility.Visible;
            base.DialogResult = true;
            this.Close();
        }
    }
}