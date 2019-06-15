using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace BatRecordingManager
{
    /// <summary>
    /// Interaction logic for ImageDialog.xaml
    /// </summary>
    public partial class ImageDialogControl : UserControl
    {
        #region storedImage

        /// <summary>
        /// storedImage Dependency Property
        /// </summary>
        public static readonly DependencyProperty storedImageProperty =
            DependencyProperty.Register("storedImage", typeof(StoredImage), typeof(ImageDialogControl),
                new FrameworkPropertyMetadata(new StoredImage(null, "", "", -1)));

        /// <summary>
        /// storedImage with metadata - used as the DataContext
        /// </summary>
        public StoredImage storedImage
        {
            get
            {
                return (StoredImage)GetValue(storedImageProperty);
            }
            set
            {
                SetValue(storedImageProperty, value);
            }
        }

        #endregion storedImage

        /// <summary>
        /// An instance of StoredImage to hold the image and its related metadata
        /// </summary>
        //public StoredImage storedImage { get; set; } = new StoredImage(null, "", "", -1);

        /// <summary>
        /// Basic constructor for the ImageDialogControl
        /// </summary>
        public ImageDialogControl()
        {
            InitializeComponent();
            this.DataContext = this.storedImage;
        }

        /// <summary>
        /// Event raised after the OK Button is clicked.
        /// </summary>
        public event EventHandler<EventArgs> OKButtonClicked
        {
            add
            {
                lock (OKButtonClickedEventLock)
                {
                    OKButtonClickedEvent += value;
                }
            }
            remove
            {
                lock (OKButtonClickedEventLock)
                {
                    OKButtonClickedEvent -= value;
                }
            }
        }

        /// <summary>
        /// Grabs the screen within the given rectangle
        /// </summary>
        /// <param name="rect"></param>
        public System.Drawing.Bitmap GrabRect(System.Drawing.Rectangle rect)
        {
            int rectWidth = rect.Width - rect.Left;
            int rectHeight = rect.Height - rect.Top;
            Bitmap bm = new Bitmap(rect.Width, rect.Height, System.Drawing.Imaging.PixelFormat.Format32bppArgb);
            Graphics g = Graphics.FromImage(bm);
            g.CopyFromScreen(rect.Left, rect.Top, 0, 0, bm.Size, CopyPixelOperation.SourceCopy);
            //DrawMousePointer(g, Cursor.Position.X - rect.Left, Cursor.Position.Y - rect.Top);
            //this.screengrab.Image = bm;
            return (bm);
        }

        /// <summary>
        /// Inserts current and default caption and description for the displayedImage
        /// </summary>
        /// <param name="caption"></param>
        /// <param name="description"></param>
        public void SetImageDialogControl(string caption, string description)
        {
            //this.caption = caption;
            this.defaultCaption = caption;
            //this.description = description;
            this.defaultDescription = description;
            if (storedImage == null)
            {
                storedImage = new StoredImage(null, "", "", -1);
            }
            this.storedImage.caption = caption;
            this.storedImage.description = description;
        }

        /// <summary>
        /// Clears the currently diaplyed image, the caption and the description
        /// The caption is retained for re-use with later images so that a long filename
        /// does not have to be retyped over and over but can be left or modified for
        /// successive images
        ///
        /// </summary>
        internal void Clear(bool clearCaption)
        {
            //displayedImage = null;
            //caption = "";
            //description = "";
            storedImage.Clear(clearCaption);
        }

        internal void Clear()
        {
            this.Clear(true);
        }

        /// <summary>
        /// If there is a displayed image returns it as a StoredImage using the current
        /// caption and description and an ID of -1.
        /// </summary>
        /// <returns></returns>
        internal StoredImage GetStoredImage()
        {
            return (storedImage);
        }

        /// <summary>
        /// Raises the <see cref="OKButtonClicked" /> event.
        /// </summary>
        /// <param name="e"><see cref="EventArgs" /> object that provides the arguments for the event.</param>
        protected virtual void OnOKButtonClicked(EventArgs e)
        {
            EventHandler<EventArgs> handler = null;

            lock (OKButtonClickedEventLock)
            {
                handler = OKButtonClickedEvent;

                if (handler == null)
                    return;
            }

            handler(this, e);
        }

        private readonly object OKButtonClickedEventLock = new object();
        private string defaultCaption = "";
        private string defaultDescription = "";
        private EventHandler<EventArgs> OKButtonClickedEvent;

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            storedImage.Clear();
            storedImage.description = "";
            storedImage.caption = "";
            OnOKButtonClicked(new EventArgs());
        }

        private void CCWImageButton_Click(object sender, RoutedEventArgs e)
        {
            RotateImage90(false);
        }

        private void ClearImageButton_Click(object sender, RoutedEventArgs e)
        {
            //displayedImage = null;
            //caption = defaultCaption;
            //description = defaultDescription;

            storedImage.Clear();
            this.DataContext = this.storedImage;
            this.Refresh();
        }

        private void CWImageButton_Click(object sender, RoutedEventArgs e)
        {
            RotateImage90(true);
        }

        private void OKButton_Click(object sender, RoutedEventArgs e)
        {
            if (storedImage != null && (!string.IsNullOrWhiteSpace(storedImage.caption) || !string.IsNullOrWhiteSpace(storedImage.description)))
            {
                OnOKButtonClicked(EventArgs.Empty);
                return;
            }
        }

        /// <summary>
        /// Button handler for OPEN
        /// Displays a file dialog to allow the user to select a suitable
        /// image file.  Converts the file into a BitmapSource and displays it in
        /// the image window.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void OpenFileButton_Click(object sender, RoutedEventArgs e)
        {
            List<String> selectedFiles = FileBrowser.SelectFile("Select Image File", null, "Image file (*.bmp,*.jpg,*.png)|*.bmp;*.jpg;*.png|All files (*.*)|*.*", false);
            if (!selectedFiles.IsNullOrEmpty())
            {
                Uri uri = new Uri(selectedFiles[0], UriKind.Absolute);
                BitmapImage bmps = new BitmapImage(uri);
                storedImage.Clear();
                storedImage.image = bmps;
                storedImage.caption = uri.ToString();
                storedImage.description = "";
            }
        }

        private void PasteImageButton_Click(object sender, RoutedEventArgs e)
        {
            if (Keyboard.IsKeyDown(Key.LeftShift) || Keyboard.IsKeyDown(Key.RightShift))
            {
                GrabRegionForm grf = new GrabRegionForm();
                DependencyObject parent = this.Parent;
                int avoidInfiniteLoop = 0;
                // Search up the visual tree to find the first parent window.
                while ((parent is Window) == false)
                {
                    parent = VisualTreeHelper.GetParent(parent);
                    avoidInfiniteLoop++;
                    if (avoidInfiniteLoop == 1000)
                    {
                        // Something is wrong - we could not find the parent window.
                        Debug.WriteLine("Failed to find a parent of type Window");
                        return;
                    }
                }
                var window = parent as Window;

                //window.Visibility = Visibility.Hidden;
                window.WindowState = WindowState.Minimized;

                grf.ShowDialog();
                System.Drawing.Rectangle rect = grf.rect;
                grf.Close();

                System.Drawing.Bitmap bm = GrabRect(rect);

                storedImage = new StoredImage(bm.ToBitmapSource(),
                            storedImage != null ? storedImage.caption : "", storedImage != null ? storedImage.description : "", -1);

                //window.Visibility = Visibility.Visible;
                window.WindowState = WindowState.Normal;
                this.Visibility = Visibility.Visible;
                this.InvalidateVisual();
                this.Refresh();
                window.InvalidateVisual();
                window.Refresh();
                Thread.Sleep(500);
            }
            else
            {
                if (Clipboard.ContainsText() && Clipboard.ContainsImage())
                {
                    int id;
                    // then we may have been passed an imageID to be linked to the current
                    //object that wants the image
                    string inboundText = Clipboard.GetText();
                    if (inboundText.StartsWith("***"))
                    {
                        inboundText = inboundText.Substring(3);
                        id = -1;
                        if (int.TryParse(inboundText, out id))
                        {
                            if (DBAccess.ImageExists(id))
                            {
                                storedImage = DBAccess.GetImage(id);
                            }
                        }
                    }
                }
                else
                {
                    if (Clipboard.ContainsImage())
                    {
                        //storedImage.Clear();
                        //storedImage.image = StoredImage.GetClipboardImageAsBitmapImage();
                        storedImage = new StoredImage(StoredImage.GetClipboardImageAsBitmapImage(),
                            storedImage != null ? storedImage.caption : "", storedImage != null ? storedImage.description : "", -1);

                        if (Clipboard.ContainsText())

                        {
                            try
                            {
                                //var image = StoredImage.ConvertBitmapSourceToBitmapImage(Clipboard.GetImage());
                                var text = Clipboard.GetText();

                                //StoredImage si = new StoredImage(displayedImage, "", "", -1);
                                storedImage.setCaptionAndDescription(text);
                                //displayedImage = si.image;
                            }
                            catch (Exception ex)
                            {
                                Debug.WriteLine("Error pasting from clipboard:- " + ex.Message);
                            }
                        }
                    }
                }
            }

            this.DataContext = this.storedImage;
            this.Refresh();
        }

        private void RotateImage90(bool clockwise)
        {
            int angle = clockwise ? 90 : -90;
            if (storedImage.image != null)
            {
                if (displayImageCanvas.LayoutTransform is RotateTransform Transform)
                {
                    displayImageCanvas.RenderTransformOrigin = new System.Windows.Point(0.5, 0.5);
                    Transform.Angle += angle;
                }
                else
                {
                    displayImageCanvas.LayoutTransform = new RotateTransform();
                    Transform = displayImageCanvas.LayoutTransform as RotateTransform;
                    displayImageCanvas.RenderTransformOrigin = new System.Windows.Point(0.5, 0.5);
                    Transform.Angle += angle;
                }
                while (Transform.Angle < -180)
                {
                    Transform.Angle += 360;
                }

                while (Transform.Angle > 180)
                {
                    Transform.Angle -= 360;
                }
            }
        }
    }
}