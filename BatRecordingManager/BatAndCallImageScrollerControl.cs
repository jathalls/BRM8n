using Microsoft.VisualStudio.Language.Intellisense;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Windows;

namespace BatRecordingManager
{
    /// <summary>
    /// Class derived from the ImageScrollerControl which is specifically
    /// designed to handle the images associated with a specific bat
    /// in the form of a list of bat images and a several lists of images
    /// each associated with a specific call type of that bat.  The bat
    /// images are storedin a list called BatImages and there is a
    /// list of lists of images for the associated call images.
    /// The constructor is parameterless and the spcific bat instance
    /// should be set usig the CurrentBat property.  Image lists and handling
    /// are then handeled internally and the parent class only needs to
    /// ensure that the CurrentBat is kept up to date.  The parent class
    /// is responsible for saving the bat and its images to the database
    /// if required.
    ///
    /// More generally the control is used widely and simply contains two sets of images.
    /// BatImages are displayed if ImageScrollerDisplaysBatImages is true
    /// CallImageList is displayed if ImageScrollerDisplaysBatImages is false
    ///
    /// The control may be set to be readonly in which case the add/edit/delete
    /// buttons will not be visible.
    /// </summary>
    public class BatAndCallImageScrollerControl : ImageScrollerControl
    {
        #region CurrentBat

        /// <summary>
        /// CurrentBat Dependency Property.  This property must be updated by the
        /// parent with the currently selected bat.
        /// </summary>
        public static readonly System.Windows.DependencyProperty CurrentBatProperty =
            DependencyProperty.Register("CurrentBat", typeof(Bat), typeof(BatAndCallImageScrollerControl),
                new FrameworkPropertyMetadata((Bat)null));

        /// <summary>
        /// Gets or sets the CurrentBat property.  This dependency property
        /// indicates ....
        /// </summary>
        public Bat CurrentBat
        {
            get { return (Bat)GetValue(CurrentBatProperty); }
            set
            {
                // First clear out the image source lists
                ImageScrollerDisplaysBatImages = true;
                //BatImages.Clear();
                //foreach (var list in CallListofImageLists)
                //{
                //    list.Clear();
                //}
                //CallListofImageLists.Clear();
                // then clear the working images in the underlying base class
                Clear();
                //CallList.Clear();

                // then get any new images for the new bat
                if (value != null)
                {
                    //var batImagesFromDB = DBAccess.GetImagesForBat(value, Tools.BlobType.BMPS);
                    var batImagesFromDB = value.GetImageList();

                    //foreach (var image in batImagesFromDB) BatImages.Add(image);
                    BatImages.AddRange(batImagesFromDB);

                    //ImageList.Clear();
                    if (!BatImages.IsNullOrEmpty())
                    {
                        //ImageList.AddRange(rawImageList);
                        //}
                        foreach (var storedImage in BatImages)
                        {
                            AddImage(storedImage);
                        }
                    }

                    foreach (var batCallLink in value.BatCalls)
                    {
                        CallList.Add(batCallLink.Call);
                        var imageListFromDB = batCallLink.Call.GetImageList();
                        if (imageListFromDB == null) imageListFromDB = new BulkObservableCollection<StoredImage>();
                        ListofCallImageLists.Add(imageListFromDB);
                    }

                    imageIndex = imageList.Any() ? 0 : -1;

                    SetValue(CurrentBatProperty, value);
                    SelectedCallIndex = CallList.Any() ? 0 : -1;
                    SetImportAllowed(true);
                }
                else
                {
                    SetImportAllowed(false);
                }
            }
        }

        #endregion CurrentBat

        /// <summary>
        /// default caption to be displayed if no other is available
        /// </summary>
        public string defaultCaption = "";

        /// <summary>
        /// default description to be displayed if no other is available
        /// </summary>
        public string defaultDescription = "";

        /// <summary>
        /// BatAndCallImageScrollerControl (constructor) is a user defined wmf control which
        /// displays a set of images.  These may be either a set of images for a
        /// specific bat, or a set of images of a specific call type related to that bat.
        /// Either set of images is displayed in the same space with the same set of
        /// operation buttons.
        /// </summary>
        public BatAndCallImageScrollerControl()
        {
            base.ButtonPressed += BatAndCallImageScrollerControl_ButtonPressed;
            this.DataContext = base.selectedImage;
            Title = "Bat and Call Images";
        }

        /// <summary>
        /// The set of images to be displayed if ImageScrollerDisplaysBatImages is true
        /// </summary>
        public BulkObservableCollection<StoredImage> BatImages { get; } = new BulkObservableCollection<StoredImage>();

        /// <summary>
        /// The list of images to be displayed if ImageScrollerDisplaysBatImages is false.  The master list is held in
        /// ListOfCallImageLists and the index into that list in SelectedCallIndex
        /// </summary>
        public BulkObservableCollection<StoredImage> CallImageList { get; } = new BulkObservableCollection<StoredImage>();

        /// <summary>
        /// The list of defined call types for this bat
        /// </summary>
        public BulkObservableCollection<Call> CallList { get; } = new BulkObservableCollection<Call>();

        /// <summary>
        /// getters and setters for a flag indicating whether it is permissible to add new images
        /// Controls the visibility of the AddImageButton
        /// </summary>
        public bool CanAdd

        {
            get
            {
                return (_CanAdd);
            }

            internal set
            {
                _CanAdd = value;
                if (value)
                {
                    AddImageButton.IsEnabled = true;
                    AddImageButton.Visibility = Visibility.Visible;
                }
                else
                {
                    AddImageButton.IsEnabled = false;
                }
            }
        }

        /// <summary>
        /// getters and setters for flag to indicate whether to use the BatImages or the CallImageList selected
        /// from the ListOfCallImageLists by Selected CallList
        /// </summary>
        public bool ImageScrollerDisplaysBatImages
        {
            get
            {
                return (_ImageScrollerDisplaysBatImages);
            }
            set
            {
                _ImageScrollerDisplaysBatImages = value;
                base.Clear();
                SetImportAllowed(true);
                if (value)
                {
                    // clear the image list and re-populate from the BatImage List

                    if (!BatImages.IsNullOrEmpty())
                    {
                        foreach (var im in BatImages)
                        {
                            AddImage(im);
                        }
                        Title = "Bat Images";
                    }
                    else
                    {
                        Title = "No Bat Images to Display";
                    }
                }
                else
                {
                    // clear the imageList and populate from the currently selected call image list
                    
                    if (ListofCallImageLists != null && SelectedCallIndex >= 0 && SelectedCallIndex < ListofCallImageLists.Count && ListofCallImageLists[SelectedCallIndex] != null)
                    {
                        CallImageList.Clear();
                        foreach (var im in ListofCallImageLists[SelectedCallIndex])
                        {
                            AddImage(im);
                            CallImageList.Add(im);
                        }
                        Title = "Call Images";
                    }
                    else
                    {
                        Title = "No Call Images to Display";
                    }
                    SetImportAllowed(true);
                }
                if (imageList.Any())
                {
                    imageIndex = 0;
                }
            }
        }

        /// <summary>
        /// The list of lists of images for all call types.  Each call type has an image list even if it is empty
        /// </summary>
        public BulkObservableCollection<BulkObservableCollection<StoredImage>> ListofCallImageLists { get; } = new BulkObservableCollection<BulkObservableCollection<StoredImage>>();

        /// <summary>
        /// List of captions and descriptions tomatch the list of calls
        /// </summary>
        public Dictionary<string, string> ListOfCaptionAndDescription { get; } = new Dictionary<string, string>();

        /// <summary>
        /// SelectedCallIndex is the index to the specific call which is to be displayed in detail
        /// in the Call detail control and whose images are to be displayed in the ImageScroller control
        /// if the ImageScrollerDisplaysBatImages flag is cleared (i.e. the Image scroller is displaying
        /// call images insted of bat images.
        /// The Setter is responsible for updating the ImageScroller display and the Bat Call Detail display,
        /// as well as setting the states of the Prev and Next  Call Buttons
        ///
        /// </summary>
        public int SelectedCallIndex
        {
            get
            {
                return (_SelectedCallIndex);
            }
            set
            {
                if (imageIndex >= 0 && imageIndex < imageList.Count())
                {
                    imageList[imageIndex] = selectedImage;// retrieve the possiblt edited current image
                }
                if (!ImageScrollerDisplaysBatImages)
                { // for call images...
                    //Copy the current image set into the current CallImageList
                    CallImageList.Clear();
                    CallImageList.AddRange(imageList);
                    Title = "Call Images";
                    //foreach (var image in imageList) CallImageList.Add(image);

                    if (_SelectedCallIndex < ListofCallImageLists.Count && _SelectedCallIndex >= 0)
                    {
                        ListofCallImageLists[_SelectedCallIndex] = new BulkObservableCollection<StoredImage>();
                        ListofCallImageLists[_SelectedCallIndex].AddRange(CallImageList);
                    }
                    else if (_SelectedCallIndex >= 0) // therefore index >=Count
                    {
                        var boc = new BulkObservableCollection<StoredImage>();
                        boc.AddRange(CallImageList);
                        ListofCallImageLists.Add(boc);
                    }
                }
                // we have now updated our lists to the previously displayed list
                _SelectedCallIndex = value;

                // so now we set up the new image sets...
                if (value >= 0 && value < ListofCallImageLists.Count)
                {
                    SetImportAllowed(true);
                    try
                    {
                        // copy the images from the list of lists, to the current CallImageList
                        CallImageList.Clear();
                        foreach (var im in ListofCallImageLists[_SelectedCallIndex]) CallImageList.Add(im);
                    }
                    catch (ArgumentOutOfRangeException iore)
                    {
                        Debug.WriteLine(iore.ToString());
                        CallImageList.Clear();
                        Tools.ErrorLog(iore.Message);
                    }

                    // if we are displaying call images, copy them to the current display set
                    if (!ImageScrollerDisplaysBatImages)
                    {
                        base.enableAllButtons(); // before the Clear so that clear can reset some
                        base.Clear();

                        foreach (var im in CallImageList)
                        {
                            AddImage(im);
                        }
                        if (imageList.Any())
                        {
                            imageIndex = 0;
                        }
                        else
                        {
                            imageIndex = -1;
                        }
                    }
                }
                else
                {
                    SetImportAllowed(true);
                    // value is out of range (assumed to be -1)
                    if (!ImageScrollerDisplaysBatImages)
                    {
                        // but we are displaying the call list
                        base.Clear(); // clear the display since there is nothing to display
                        base.disableAllButtons(); // after the clear to override any enabled by Clear
                    }
                }
            }
        }

        /// <summary>
        /// Clears the control of all image lists and clears the base ImageScrollerControl
        /// </summary>
        public new void Clear()

        {
            foreach (var list in ListofCallImageLists)
            {
                list.Clear();
            }
            ListofCallImageLists.Clear();
            CallImageList.Clear();
            SelectedCallIndex = -1;
            BatImages.Clear();
            base.Clear();
            Title = "Bat and Call Images";
        }

        /// <summary>
        /// Updates the base ImageScroller and uses its imageList to repopulate
        /// either BatImages or the currently selected element of ListOfCallImageLists with
        /// the base ImageScroller's imageList.
        /// </summary>
        public new void Update()
        {
            base.Update();
            if (ImageScrollerDisplaysBatImages)
            {
                BatImages.Clear();
                BatImages.AddRange(imageList);
                Title = "Bat Images";                //foreach (var im in imageList)
                //{
                //    BatImages.Add(im);
                //}
            }
            else
            {
                if (SelectedCallIndex >= 0 && SelectedCallIndex < ListofCallImageLists.Count)
                {
                    ListofCallImageLists[SelectedCallIndex].Clear();
                    ListofCallImageLists[SelectedCallIndex].AddRange(imageList);
                    Title = "Call Images";
                    //foreach (var im in imageList)
                    //{
                    //   ListofCallImageLists[SelectedCallIndex].Add(im);
                    //}
                }
            }
        }

        /// <summary>
        /// Returns the currently displayed list of images
        /// </summary>
        /// <returns></returns>
        internal BulkObservableCollection<StoredImage> GetCurrentImageList()
        {
            if (SelectedCallIndex >= 0 && SelectedCallIndex < ListofCallImageLists.Count)
            {
                return (ListofCallImageLists[SelectedCallIndex]);
            }
            return CallImageList;
        }

        private bool _CanAdd;

        private bool _ImageScrollerDisplaysBatImages;

        /// <summary>
        /// index to the selected call type indicates which image list in ListofCallImageLists to use
        /// </summary>
        private int _SelectedCallIndex;

        /// <summary>
        /// Shows a dialog to add a new image to the current image list.
        /// if the dialog result is OK then the image is added to the lists
        /// and the display updated.  The image is not added to the database
        /// until the Parent decides to do it.
        /// </summary>
        private void AddImage()
        {
            string defaultCaption = this.defaultCaption;
            string defaultDescription = this.defaultDescription;
            if (ImageScrollerDisplaysBatImages)
            {
                if (CurrentBat != null)
                {
                    defaultCaption = CurrentBat.Name;
                    defaultDescription = CurrentBat.Notes;
                }
            }
            else
            {
                if (CallList != null && SelectedCallIndex >= 0 && CallList.Count > SelectedCallIndex && CallList[SelectedCallIndex] != null)
                {
                    if (CurrentBat != null)
                    {
                        defaultCaption = CurrentBat.Name;
                        defaultDescription = CallList[SelectedCallIndex].CallType;
                    }
                    else
                    {
                        defaultCaption = CallList[SelectedCallIndex].CallType;
                        defaultDescription = CallList[SelectedCallIndex].CallNotes;
                    }
                }
                else
                {
                    if (ListOfCaptionAndDescription != null && SelectedCallIndex >= 0 && ListOfCaptionAndDescription.Count > SelectedCallIndex)
                    {
                        defaultCaption = ListOfCaptionAndDescription.ElementAt(SelectedCallIndex).Key;
                        defaultDescription = ListOfCaptionAndDescription.ElementAt(SelectedCallIndex).Value;
                    }
                }
            }
            ImageDialog dialog = new ImageDialog(defaultCaption, defaultDescription);

            dialog.ShowDialog();
            var result = dialog.DialogResult;
            StoredImage image = dialog.GetStoredImage();
            if (image != null && image.image != null)
            {
                base.AddImage(image);
                if (ImageScrollerDisplaysBatImages)
                {
                    BatImages.Add(image);
                }
                else
                {
                    CallImageList.Add(image);
                    if (SelectedCallIndex >= 0 && SelectedCallIndex < ListofCallImageLists.Count)
                    {
                        ListofCallImageLists[SelectedCallIndex].Add(image);
                    }
                    else if (SelectedCallIndex < 0)
                    {
                        // SelectedCallIndex not valid so no LabelledSegment was selected prior to adding the image
                        // Therefore raise an event with the parent to create a new 0-time segment and associate
                        // this image/images with it by selecting it
                        OnUnassociatedImage(new EventArgs());
                    }
                }
            }
        }

        /// <summary>
        /// Handler for the ImageScrollerControl button pressed event, which will deal with
        /// the pressing of the ADD or DEL buttons.  If the ADD button is pressed then the
        /// ImageDialog is displayed and if the result is OK then the image is added to the
        /// appropriate image list within this control and the ImageScroller is updated to
        /// display the new image.  If the DEL button is pressed then the currently displayed
        /// image is removed from the list that contains it and the currently displayed image
        /// is changed to the nearest available image for the call or bat.
        /// Also deals with the Import button which has already added the new image to the displayed
        /// image list but needs it to be linked to the current source object in the database.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void BatAndCallImageScrollerControl_ButtonPressed(object sender, EventArgs e)
        {
            if (e == null || !(e is ButtonPressedEventArgs)) return;
            ButtonPressedEventArgs bpe = e as ButtonPressedEventArgs;
            if (bpe.PressedButton == null) return;

            if (bpe.PressedButton.Content as String == "DEL")
            {
                if (bpe.image == null) return;
                DeleteImageFromLists(bpe.image);
            }
            else if (bpe.PressedButton.Content as string == "ADD")
            {
                AddImage();
            }else if(bpe.PressedButton.Content as string == "IMPORT")
            {
                if (bpe.image == null) return;
                if (bpe.image.ImageID < 0) return; // dont try linking if the image is not in the database yet
                if (ImageScrollerDisplaysBatImages)
                {
                    LinkImageToBat(bpe.image);
                }
                else
                {
                    LinkImageToCall(bpe.image);
                }
            }
            Update();
        }

        /// <summary>
        /// adds a link in the database linking the supplied StoredImage (which should be in the
        /// database) to the currently selected call definition.
        /// </summary>
        /// <param name="image"></param>
        private void LinkImageToCall(StoredImage image)
        {

            if (SelectedCallIndex >= 0)
            {
                Call selectedCall = CallList[SelectedCallIndex];
                if (!CallImageList.Contains(image)) {
                    CallImageList.Add(image);
                }
                if (!ListofCallImageLists[SelectedCallIndex].Contains(image))
                {
                    ListofCallImageLists[SelectedCallIndex].Add(image);
                }
                if(selectedCall!=null && selectedCall.Id >= 0)
                {
                    selectedCall.AddImage(image.GetAsBinaryData());
                }
            }
        }

        /// <summary>
        /// adds a link in the database linking the supplied StoredImage (which should be in the
        /// database) to the currently selected bat.
        /// </summary>
        /// <param name="image"></param>
        private void LinkImageToBat(StoredImage image)
        {
            BatImages.Add(image);
            if (CurrentBat != null)
            {
                CurrentBat.AddImage(image.GetAsBinaryData());
            }

        }

        /// <summary>
        /// Removes the currently displayed image from the list that contains it.
        /// The parent is responsible for updating the database.
        /// </summary>
        /// <param name="imageToDelete"></param>
        ///
        private void DeleteImageFromLists(StoredImage imageToDelete)
        {
            if (imageToDelete == null || imageToDelete.ImageID < 0) return;
            //var result = MessageBox.Show("This will permanently delete the image from the Database - Are You Sure?", "Permanent Deletion Warning", MessageBoxButton.YesNo);
            //if (result == MessageBoxResult.Yes)
            //{
            //    DBAccess.DeleteBinaryData(imageToDelete.ImageID);
            //}

            if (ImageScrollerDisplaysBatImages)
            {
                BatImages.Remove(imageToDelete);
            }
            else
            {
                ListofCallImageLists[SelectedCallIndex].Remove(imageToDelete);
                CallImageList.Remove(imageToDelete);
            }

            base.DeleteImage();
        }

        #region UnAssociatedImageEvent

        /// <summary>
        /// Event raised after the  property value has changed.
        /// </summary>
        public event EventHandler<EventArgs> UnassociatedImage
        {
            add
            {
                lock (UnassociatedImageEventLock)
                {
                    UnassociatedImageEvent += value;
                }
            }
            remove
            {
                lock (UnassociatedImageEventLock)
                {
                    UnassociatedImageEvent -= value;
                }
            }
        }

        /// <summary>
        /// Raises the <see cref="UnassociatedImage" /> event.
        /// </summary>
        /// <param name="e"><see cref="EventArgs" /> object that provides the arguments for the event.</param>
        protected virtual void OnUnassociatedImage(EventArgs e)
        {
            EventHandler<EventArgs> handler = null;

            lock (UnassociatedImageEventLock)
            {
                handler = UnassociatedImageEvent;

                if (handler == null)
                    return;
            }

            handler(this, e);
        }

        // Add Event for adding image/s when there is no segment selected
        private readonly object UnassociatedImageEventLock = new object();

        private EventHandler<EventArgs> UnassociatedImageEvent;

        #endregion UnAssociatedImageEvent
    }
}