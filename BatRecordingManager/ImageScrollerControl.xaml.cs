using Microsoft.VisualStudio.Language.Intellisense;
using System;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace BatRecordingManager
{
    /// <summary>
    /// Interaction logic for ImageScrollerControl.xaml
    /// is used as a base class by BatAndCallImageScroller
    /// </summary>
    public partial class ImageScrollerControl : UserControl
    {
        #region selectedImage

        /// <summary>
        /// selectedImageProperty Dependency Property
        /// </summary>
        /// 
        public static readonly DependencyProperty selectedImageProperty =
            DependencyProperty.Register("selectedImageProperty", typeof(StoredImage), typeof(ImageScrollerControl),
                new FrameworkPropertyMetadata(null));

        
        /// <summary>
        /// Gets or sets the selectedImageProperty property.  This dependency property
        /// getters and setters for the currently displayed image
        /// indicates ....
        /// </summary>
        public StoredImage selectedImage
        {
            get
            {
                StoredImage img = (StoredImage)GetValue(selectedImageProperty);
             
                if (img != null && !locked)
                {
                    img.caption = CaptionTextBox.Text;
                    img.description = DescriptionTextBox.Text;
                }
                return img;
            }
            set
            {
                locked = true; // since get reads the text boxes, make sure it can't while things are changing
                try
                {
                    if (value != null && CaptionTextBox != null && DescriptionTextBox != null)
                    {
                        //DataContext = value;
                        SetValue(selectedImageProperty, value);
                        
                        CaptionTextBox.Text = value.caption;
                        DescriptionTextBox.Text = value.description;
                        
                        
                    }
                    else
                    {
                        SetValue(selectedImageProperty, value);
                        
                    }
                }
                finally
                {
                    
                    locked = false;
                    this.DataContext = this.selectedImage;
                    
                }
            }
        }

        private bool locked = false;

        #endregion selectedImage

        /// <summary>
        /// protected read-only list of images to be displayed - the contents of the list may be changed
        /// </summary>
        public BulkObservableCollection<StoredImage> imageList { get; } = new BulkObservableCollection<StoredImage>();

        #region IsReadOnly

        /// <summary>
        /// IsReadOnly Dependency Property
        /// </summary>
        public static readonly DependencyProperty IsReadOnlyProperty =
            DependencyProperty.Register("IsReadOnly", typeof(bool), typeof(ImageScrollerControl),
                new FrameworkPropertyMetadata((bool)false));

        /// <summary>
        /// Gets or sets the IsReadOnly property.  This dependency property
        /// indicates whether it is permissible to add, edit or delete images in
        /// the collection.
        /// </summary>
        public bool IsReadOnly
        {
            get { return (bool)GetValue(IsReadOnlyProperty); }
            set
            {
                SetValue(IsReadOnlyProperty, value);
                if (value)
                {
                    AddImageButton.Visibility = Visibility.Hidden;
                    DelImageButton.Visibility = Visibility.Hidden;
                    //EditImageButton.Visibility = Visibility.Hidden;
                    IsEditable = false;
                    CaptionTextBox.IsReadOnly = true;
                    DescriptionTextBox.IsReadOnly = true;
                }
                else
                {
                    AddImageButton.Visibility = Visibility.Visible;
                    DelImageButton.Visibility = Visibility.Visible;
                    //EditImageButton.Visibility = Visibility.Hidden;
                    IsEditable = true;
                    CaptionTextBox.IsReadOnly = false;
                    DescriptionTextBox.IsReadOnly = false;
                }
            }
        }

        #endregion IsReadOnly

        private bool _IsEditable;

        /// <summary>
        /// getters and setters for a flag that controls the visibility of the ADD and EDIT buttons
        /// </summary>
        public bool IsEditable

        {
            get
            {
                return (_IsEditable);
            }
            set
            {
                if (value)
                {
                    EditImageButton.Visibility = Visibility.Visible;
                    AddImageButton.Visibility = Visibility.Visible;

                    AddImageButton.IsEnabled = true;
                    DelImageButton.Visibility = Visibility.Visible;
                    DelImageButton.IsEnabled = true;
                    ImportImageButton.Visibility = Visibility.Visible;
                    ImportImageButton.IsEnabled = true;
                }
                else
                {
                    EditImageButton.Visibility = Visibility.Hidden;
                }
            }
        }

        private string _Title = "";
        public string Title
        {
            get
            {
                return (_Title);
            }

            set
            {
                _Title = value;
                TitleTextBox.Text = value;
                if(value.Contains("Bat") || value.Contains("Call"))
                {
                    ImportImageButton.Visibility = Visibility.Visible;
                    ImportImageButton.IsEnabled = true;

                }
                else
                {
                    ImportImageButton.Visibility = Visibility.Hidden;
                    ImportImageButton.IsEnabled = false;
                }
            }
        }

        /// <summary>
        /// default constructor for the class.  Clears the imageList and sets the
        /// DataContext to the selectedImage which is null at this point
        /// </summary>
        public ImageScrollerControl()
        {
            imageList.Clear();

            InitializeComponent();
            this.DataContext = this.selectedImage;

            imageIndex = -1;
        }

        private int _imageIndex = -1;

        /// <summary>
        /// getters and setters for an index into the imageList to the selected item in the list
        /// sets the selectedImage as it changes and sets the visibility of navigation and editing
        /// buttons as appropriate
        /// </summary>
        protected int imageIndex
        {
            get { return (_imageIndex); }
            set
            {
                // if moving in the list, make sure the image in the list is updated to the selectedImage
                if (_imageIndex >= 0 && _imageIndex < imageList.Count && selectedImage != null)
                {
                    imageList[_imageIndex] = selectedImage;
                }

                // then change the index and associated details...
                _imageIndex = value;
                if (value < 0)
                {
                    selectedImage = null;
                    disableAllButtons();
                    AddImageButton.IsEnabled = true;
                }
                else
                {
                    if (value < imageList.Count)
                    {
                        selectedImage = imageList[value];
                    }
                    else
                    {
                        selectedImage = null;
                    }
                    enableAllButtons();
                }

                if (value == 0)
                {
                    FarLeftButton.IsEnabled = false;
                    OneLeftButton.IsEnabled = false;
                }
                if (value >= imageList.Count - 1 || imageIndex < 0)
                {
                    FarRightButton.IsEnabled = false;
                    OneRightButton.IsEnabled = false;
                }
                else
                {
                    FarRightButton.IsEnabled = true;
                    OneRightButton.IsEnabled = true;
                }
                if (imageIndex >= 0)
                {
                    ImageNumberLabel.Content = (imageIndex + 1) + " of " + imageList.Count;
                }
                else
                {
                    ImageNumberLabel.Content = "";
                }
            }
        }

        /// <summary>
        /// Adds a given image to the imageList
        /// </summary>
        /// <param name="newImage"></param>
        public void AddImage(StoredImage newImage)
        {
            if (imageList != null)
            {
                imageList.Add(newImage);

                imageIndex = imageList.Count - 1;
                selectedImage = imageList[imageIndex];
            }

        }

        /// <summary>
        /// resets the selectedImage to the item in the imageList pointed to by the
        /// imageIndex, or null if the index does not point to a valid entry
        /// </summary>
        public void Update()
        {
            if (imageIndex >= 0 && imageIndex < imageList.Count)
            {
                imageList[imageIndex] = selectedImage;
            }
            else
            {
                selectedImage = null;
            }
        }

        /// <summary>
        /// deletes the currently selected image from the list but does not change the database
        /// </summary>
        public void DeleteImage()
        {
            if (!imageList.IsNullOrEmpty() && imageIndex >= 0 && imageIndex < imageList.Count)
            {
                int DeletedImageID = (selectedImage as StoredImage).ImageID;
                imageList.Remove(selectedImage);
                selectedImage = null;
                CaptionTextBox.Text = "";
                DescriptionTextBox.Text = "";
                imageIndex--;
                if (imageIndex < 0 && imageList.Count > 0)
                {
                    imageIndex = 0;
                }
                while (imageIndex > imageList.Count - 1)
                {
                    imageIndex--;
                }

                OnImageDeleted(new ImageDeletedEventArgs(DeletedImageID));
            }
        }

        /// <summary>
        /// disables all the controls buttons when there is no parent list that can be added to and no
        /// images displayed to modify
        /// </summary>
        internal void enableAllButtons()
        {
            FarLeftButton.IsEnabled = true;
            FarRightButton.IsEnabled = true;
            OneLeftButton.IsEnabled = true;
            OneRightButton.IsEnabled = true;
            AddImageButton.IsEnabled = true;
            DelImageButton.IsEnabled = true;
            EditImageButton.IsEnabled = true;
            FullScreenButton.IsEnabled = true;
            ImportImageButton.IsEnabled = true;
            
        }

        /// <summary>
        /// Clears the list of currently displayed images but not the source lists from which
        /// it gets populated.  The imageIndex is set to -1 and the selecetdImage to null.
        /// </summary>
        public void Clear()
        {
            imageList.Clear();
            imageIndex = -1;
            CaptionTextBox.Text = "";
            DescriptionTextBox.Text = "";

            AddImageButton.IsEnabled = true;
        }

        private void FarLeftButton_Click(object sender, RoutedEventArgs e)
        {
            if (imageList != null && imageList.Count > 0)
            {
                imageIndex = 0;
            }
        }

        /// <summary>
        /// Disables all the buttons in the scrollers button bar
        /// </summary>
        internal void disableAllButtons()
        {
            FarLeftButton.IsEnabled = false;
            FarRightButton.IsEnabled = false;
            OneLeftButton.IsEnabled = false;
            OneRightButton.IsEnabled = false;
            AddImageButton.IsEnabled = false;
            DelImageButton.IsEnabled = false;
            EditImageButton.IsEnabled = false;
            FullScreenButton.IsEnabled = false;
            ImportImageButton.IsEnabled = false;
           
        }

        internal void DisableAddButton()
        {
            AddImageButton.IsEnabled = false;
        }

        private void OneLeftButton_Click(object sender, RoutedEventArgs e)
        {
            if (imageList != null && imageList.Count > 1 && imageIndex > 0)
            {
                imageIndex--;
            }
        }

        private void OneRightButton_Click(object sender, RoutedEventArgs e)
        {
            if (imageList != null && imageList.Count > 1 && imageIndex >= 0 && imageIndex < imageList.Count - 1)
            {
                imageIndex++;
            }
        }

        private void FarRightButton_Click(object sender, RoutedEventArgs e)
        {
            if (imageList != null && imageList.Count > 1)
            {
                imageIndex = imageList.Count - 1;
            }
        }

        /// <summary>
        /// Button to display the selected image full screen
        /// A misnomer - adds the image to the comparisonwindow
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void FullScreenButton_Click(object sender, RoutedEventArgs e)
        {
            if (selectedImage != null)
            {
                ComparisonHost.Instance.AddImage(selectedImage);
            }
        }

        private readonly object ButtonPressedEventLock = new object();
        private EventHandler<EventArgs> ButtonPressedEvent;

        /// <summary>
        /// Event raised after one of the handled buttons has been pressed.
        /// </summary>
        public event EventHandler<EventArgs> ButtonPressed
        {
            add
            {
                lock (ButtonPressedEventLock)
                {
                    ButtonPressedEvent += value;
                }
            }
            remove
            {
                lock (ButtonPressedEventLock)
                {
                    ButtonPressedEvent -= value;
                }
            }
        }

        /// <summary>
        /// Raises the <see cref="ButtonPressed" /> event.
        /// </summary>
        /// <param name="e"><see cref="EventArgs" /> object that provides the arguments for the event.</param>
        protected virtual void OnButtonPressed(ButtonPressedEventArgs e)
        {
            EventHandler<EventArgs> handler = null;

            lock (ButtonPressedEventLock)
            {
                handler = ButtonPressedEvent;

                if (handler == null)
                    return;
            }

            handler(this, e);
        }

        private void AddImageButton_Click(object sender, RoutedEventArgs e)
        {
            if (imageIndex >= 0 && imageIndex < imageList.Count())
            {
                OnButtonPressed(new ButtonPressedEventArgs(sender as Button, imageList[imageIndex],false));
            }
            else
            {
                OnButtonPressed(new ButtonPressedEventArgs(sender as Button, null,false));
            }
        }

        /*
        private void PasteCheckBox_Checked(object sender, RoutedEventArgs e)
        {
            AddImageButton.Content = "PASTE";
        }

        private void PasteCheckBox_Unchecked(object sender, RoutedEventArgs e)
        {
            AddImageButton.Content = "ADD";
        }*/

        private void EditImageButton_Click(object sender, RoutedEventArgs e)
        {
            if ((EditImageButton.Content as string) == "EDIT")
            {
                EditImageButton.Content = "SAVE";
                CaptionTextBox.IsReadOnly = false;
                DescriptionTextBox.IsReadOnly = false;
            }
            else
            {
                if (selectedImage.ImageID >= 0)
                {
                    DBAccess.UpdateImage(selectedImage);
                    if (imageIndex >= 0 && imageIndex < imageList.Count)
                    {
                        imageList[imageIndex] = selectedImage;
                    }
                }
                EditImageButton.Content = "EDIT";
                CaptionTextBox.IsReadOnly = true;
                DescriptionTextBox.IsReadOnly = true;
            }
        }

        private void DelImageButton_Click(object sender, RoutedEventArgs e)
        {
            if (imageIndex >= 0 && imageIndex < imageList.Count())
            {
                bool fromDatabase = (Keyboard.IsKeyDown(Key.LeftShift) || Keyboard.IsKeyDown(Key.RightShift));
                ButtonPressedEventArgs args = new ButtonPressedEventArgs(sender as Button, imageList[imageIndex],fromDatabase);
                OnButtonPressed(args);
            }
            else
            {
                OnButtonPressed(new ButtonPressedEventArgs(sender as Button, null,false));
            }
        }

        private void Currentimage_MouseWheel(object sender, MouseWheelEventArgs e)
        {
            int move = e.Delta;
            if (move > 0)
            {
                OneRightButton_Click(sender, new RoutedEventArgs());
            }
            else
            {
                OneLeftButton_Click(sender, new RoutedEventArgs());
            }
        }

        /// <summary>
        /// Parses the StoredImage Uri for a collection of file names and opens those that have
        /// existing files.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void OpenFileButton_Click(object sender, RoutedEventArgs e)
        {
            /*
            if (!String.IsNullOrWhiteSpace(selectedImage.Uri))
            {
                var fileNameSet = selectedImage.Uri.Split(';');
                foreach (var fn in fileNameSet)
                {
                    Tools.OpenWavFile(fn);
                }
            }*/
            if (selectedImage!=null && selectedImage.isPlayable)
            {
                selectedImage.Open();
            }
        }

        internal void SetImportAllowed(bool allowed)
        {
            if (allowed)
            {
                ImportImageButton.Visibility = Visibility.Visible;
                ImportImageButton.IsEnabled = true;
            }
            else
            {
                ImportImageButton.Visibility = Visibility.Hidden;
                ImportImageButton.IsEnabled = false;
            }
        }

        /// <summary>
        /// Sets up the image scroller control for view only mode, basically by disabling the
        /// ADD button for when it is used in the image entry mode in which the add dialog is
        /// permanently displayed and copies images across into the scroller as they are
        /// created.
        /// </summary>
        internal void SetViewOnly(bool onoff)
        {
            if (onoff)
            {
                AddImageButton.Visibility = Visibility.Hidden;
            }
            else
            {
                AddImageButton.Visibility = Visibility.Visible;
            }
        }

        private readonly object ImageDeletedEventLock = new object();
        private EventHandler<EventArgs> ImageDeletedEvent;

        /// <summary>
        /// Event raised after the <see cref="Text" /> property value has changed.
        /// </summary>
        public event EventHandler<EventArgs> ImageDeleted
        {
            add
            {
                lock (ImageDeletedEventLock)
                {
                    ImageDeletedEvent += value;
                }
            }
            remove
            {
                lock (ImageDeletedEventLock)
                {
                    ImageDeletedEvent -= value;
                }
            }
        }

        /// <summary>
        /// Raises the <see cref="ImageDeleted" /> event.
        /// </summary>
        /// <param name="e"><see cref="EventArgs" /> object that provides the arguments for the event.</param>
        protected virtual void OnImageDeleted(EventArgs e)
        {
            EventHandler<EventArgs> handler = null;

            lock (ImageDeletedEventLock)
            {
                handler = ImageDeletedEvent;

                if (handler == null)
                    return;
            }

            handler(this, e);
        }

        /// <summary>
        /// Get the currently selecetd image in the comparison window (if any) and
        /// add it to the displayed list and also link it to the source of the currently displayed
        /// list.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void ImportImageButton_Click(object sender, RoutedEventArgs e)
        {
            if (ComparisonHost.Instance != null)
            {
                var newImage = ComparisonHost.Instance.GetSelectedImage();
                if(newImage!=null && !imageList.Contains(newImage))
                {
                    AddImage(newImage);
                    OnButtonPressed(new ButtonPressedEventArgs(sender as Button, newImage, false));
                }
            }
        }
    }/// end class ImageScrollerControl

    /// <summary>
    /// Arguments fort a ButtonPressed Event Handler
    /// </summary>
    [Serializable]
    public class ButtonPressedEventArgs : EventArgs
    {
#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member
        public new static readonly ButtonPressedEventArgs Empty = new ButtonPressedEventArgs(null, null,false);
#pragma warning restore CS1591 // Missing XML comment for publicly visible type or member

        #region Public Properties

#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member
        public Button PressedButton { get; set; }
#pragma warning restore CS1591 // Missing XML comment for publicly visible type or member

#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member
        public StoredImage image;
#pragma warning restore CS1591 // Missing XML comment for publicly visible type or member
        public bool fromDatabase { get; set; } = false;

        #endregion Public Properties



        #region Constructors

        /// <summary>
        /// Constructs a new instance of the <see cref="ButtonPressedEventArgs" /> class.
        /// </summary>
        public ButtonPressedEventArgs(Button senderButton, StoredImage image, bool fromDatabase)
        {
            PressedButton = senderButton;
            this.image = image;
            this.fromDatabase = fromDatabase;
        }

        #endregion Constructors
    }// end class ButtonPressedEventArgs

    /// <summary>
    /// ImageDeletedEventArgs
    /// </summary>
    [Serializable]
    public class ImageDeletedEventArgs : EventArgs
    {
        #region Public

        public new static readonly ImageDeletedEventArgs Empty = new ImageDeletedEventArgs(-1);

        /// <summary>
        /// ID of the image that has been removed from the list
        /// </summary>
        public int imageID { get; } = -1;

        #endregion Public



        #region Constructors

        /// <summary>
        /// Constructs a new instance of the <see cref="ImageDeletedEventArgs" /> class.
        /// </summary>
        public ImageDeletedEventArgs(int ID)
        {
            imageID = ID;
        }

        #endregion Constructors
    }
}// end namespace