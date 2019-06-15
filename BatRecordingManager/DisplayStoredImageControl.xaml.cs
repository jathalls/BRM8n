using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;
using Point = System.Windows.Point;

namespace BatRecordingManager
{
    /// <summary>
    /// Interaction logic for DisplayStoredImageControl.xaml
    /// </summary>
    public partial class DisplayStoredImageControl : UserControl
    {
        #region scaleValue

        /// <summary>
        /// scaleValue Dependency Property
        /// </summary>
        public static readonly DependencyProperty scaleValueProperty =
            DependencyProperty.Register("scaleValue", typeof(string), typeof(DisplayStoredImageControl),
                new FrameworkPropertyMetadata((string)"0.5"));

        /// <summary>
        /// Gets or sets the scaleValue property.  This dependency property
        /// indicates ....
        /// </summary>
        public string scaleValue
        {
            get { return (string)GetValue(scaleValueProperty); }
            set { SetValue(scaleValueProperty, value); }
        }

        #endregion scaleValue

        #region gridScaleValue

        /// <summary>
        /// scaleValue Dependency Property
        /// </summary>
        public static readonly DependencyProperty gridScaleValueProperty =
            DependencyProperty.Register("gridScaleValue", typeof(double), typeof(DisplayStoredImageControl),
                new FrameworkPropertyMetadata(1.1d));

        /// <summary>
        /// Gets or sets the scaleValue property.  This dependency property
        /// indicates ....
        /// </summary>
        public double gridScaleValue
        {
            get { return (double)GetValue(gridScaleValueProperty); }
            set { SetValue(gridScaleValueProperty, value); }
        }

        #endregion gridScaleValue

        #region gridLeftMargin

        /// <summary>
        /// scaleValue Dependency Property
        /// </summary>
        public static readonly DependencyProperty gridLeftMarginProperty =
            DependencyProperty.Register("gridLeftMargin", typeof(double), typeof(DisplayStoredImageControl),
                new FrameworkPropertyMetadata(0.28d));

        /// <summary>
        /// Gets or sets the scaleValue property.  This dependency property
        /// indicates ....
        /// </summary>
        public double gridLeftMargin
        {
            get { return (double)GetValue(gridLeftMarginProperty); }
            set { SetValue(gridLeftMarginProperty, value); }
        }

        #endregion gridLeftMargin

        #region gridTopMargin

        /// <summary>
        /// scaleValue Dependency Property
        /// </summary>
        public static readonly DependencyProperty gridTopMarginProperty =
            DependencyProperty.Register("gridTopMargin", typeof(double), typeof(DisplayStoredImageControl),
                new FrameworkPropertyMetadata(0.154d));

        /// <summary>
        /// Gets or sets the scaleValue property.  This dependency property
        /// indicates ....
        /// </summary>
        public double gridTopMargin
        {
            get { return (double)GetValue(gridTopMarginProperty); }
            set { SetValue(gridTopMarginProperty, value); }
        }

        #endregion gridTopMargin



        

        /// Using a DependencyProperty as the backing store for storedImage.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty storedImageProperty =
            DependencyProperty.Register("storedImage", typeof(StoredImage), typeof(DisplayStoredImageControl), 
                new PropertyMetadata(new StoredImage(null,"","",-1)));


        /// <summary>
        /// The instance of a StoredImage to be displayed in this control
        /// </summary>
        public StoredImage storedImage
        {       
            get     { return ((StoredImage)GetValue(storedImageProperty)); }
                
            set
            {
                SetValue(storedImageProperty, DBAccess.GetImage(value));
                if (value.isPlayable)
                {
                    PlayButton.IsEnabled = true;
                }
                else
                {
                    PlayButton.IsEnabled = false;
                }
            }
        }

        /// <summary>
        /// Simple boolean to indicate if the storedImage has has grid lines added, deleted or changed and thus to
        /// indicate if the gridlines need to be resaved
        /// </summary>
        public bool isModified = false;

        private readonly double defaultGridTopMargin=0.154d;
        private readonly double defaultGridLeftMargin = 0.28d;
        private readonly double defaultGridScale = 0.6782d;


        /// <summary>
        /// constructor for display of control for displaying comparison images
        /// </summary>
        public DisplayStoredImageControl()
        {
            InitializeComponent();
            Loaded += DisplayStoredImageControl_Loaded;
            this.DataContext = storedImage;
            axisGrid675.DataContext = this;
            axisGrid7029A.DataContext = this;

            gridTopMargin = defaultGridTopMargin;
            gridLeftMargin = defaultGridLeftMargin;

            scaleValue = "0.5";
            gridScaleValue = defaultGridScale;
            showGrid = false;
            axisGrid7029A.Visibility = Visibility.Hidden;
            axisGrid675.Visibility = Visibility.Hidden;

            

            displayImageCanvas.Focus();
        }

        private void DisplayStoredImageControl_Loaded(object sender, RoutedEventArgs e)
        {
            if (this.IsLoaded)
            {
                FiducialsButton.IsChecked = true;
                FiducialsButton_Click(this, new RoutedEventArgs());
            }
        }

        /// <summary>
        /// Sets the state of the FIDS button according to the bool
        /// </summary>
        /// <param name="ToChecked"></param>
        internal void SetImageFids(bool? ToChecked)
        {
            FiducialsButton.IsChecked = ToChecked;
        }

        /// <summary>
        /// takes the line information from the DuplicateEvwentArgs and applies them to the current image
        /// if there are not any existing definitions
        /// </summary>
        /// <param name="duplicateEventArgs"></param>
        internal void DuplicateThis(DuplicateEventArgs duplicateEventArgs)
        {
            if (duplicateEventArgs != null)
            {
                if (gridTopMargin != defaultGridTopMargin) gridTopMargin = duplicateEventArgs.TopMargin;
                if (gridLeftMargin != defaultGridLeftMargin) gridLeftMargin = duplicateEventArgs.LeftMargin;
                if (gridScaleValue != defaultGridScale) gridScaleValue = duplicateEventArgs.Scale;
                if (storedImage.HorizontalGridlines != null && storedImage.HorizontalGridlines.Count > 0) return;
                if (storedImage.VerticalGridLines != null && storedImage.VerticalGridLines.Count > 0) return;
                //if we get here there are no horizontal or vertical gridlines defined
                storedImage.HorizontalGridlines.Clear();
                foreach(var hglProp in duplicateEventArgs.HLineProportions)
                {
                    storedImage.HorizontalGridlines.Add((int)(hglProp * storedImage.image.Height));
                }
                storedImage.VerticalGridLines.Clear();
                foreach(var vglProp in duplicateEventArgs.VLineProportions)
                {
                    storedImage.VerticalGridLines.Add((int)(vglProp * storedImage.image.Width));
                }
                
                FiducialsButton_Click(this, new RoutedEventArgs());
            }
        }



        /// <summary>
        /// Event raised after the <see cref="Text" /> property value has changed.
        /// </summary>
        public event EventHandler<EventArgs> DelButtonPressed
        {
            add
            {
                lock (DelButtonPressedEventLock)
                {
                    DelButtonPressedEvent += value;
                }
            }
            remove
            {
                lock (DelButtonPressedEventLock)
                {
                    DelButtonPressedEvent -= value;
                }
            }
        }

        /// <summary>
        /// event raised by pressing DOWN
        /// </summary>
        public event EventHandler<EventArgs> DownButtonPressed
        {
            add
            {
                lock (DownButtonPressedEventLock)
                {
                    DownButtonPressedEvent += value;
                }
            }
            remove
            {
                lock (DownButtonPressedEventLock)
                {
                    DownButtonPressedEvent -= value;
                }
            }
        }

        /// <summary>
        /// Event raised after the  property value has changed.
        /// </summary>
        public event EventHandler<EventArgs> UpButtonPressed
        {
            add
            {
                lock (UpButtonPressedEventLock)
                {
                    UpButtonPressedEvent += value;
                }
            }
            remove
            {
                lock (UpButtonPressedEventLock)
                {
                    UpButtonPressedEvent -= value;
                }
            }
        }

        

        

        /// <summary>
        /// ERROR needs to be displayed at full screen, full size for best resolution and even then the lower part of
        /// the image is lost - starts at about 30k.  Investigate taking a copy of the bitmapImage and drawing the
        /// gridlines on it directly and then exporting that rather than exporting the canvas which uses the image as a
        /// background. Corrected - see image.Save for details.
        /// </summary>
        /// <param name="folderPath"></param>
        internal string Export(string folderPath,int index,int count,bool isPng)
        {
            
            if (!folderPath.EndsWith(@"\"))
            {
                folderPath = folderPath + @"\";
            }
            string fname = folderPath + "BatCallImage";
            if (storedImage.ImageID >= 0)
            {
                fname = storedImage.GetName();
            }
            if (fname.Contains(@"\"))
            {
                fname = fname.Substring(fname.LastIndexOf('\\') + 1);
            }
            string FormatString = @"{0,1:D1} - {1}";
            if(count>=10) FormatString=@"{0,2:D2} - {1}";
            if (count >= 100) FormatString = @"{0,3:D3} - {1}";


            fname = String.Format(FormatString, index, fname);
            int i = 0;

            while (File.Exists(folderPath + fname + (i > 0 ? ("-" + i.ToString()) : "") + ".png"))
            {
                i++;
            }
            var image = storedImage;
            fname = fname + (i > 0 ? ("-" + i.ToString()) : "");
            image.Save(folderPath + fname + (isPng?".png":".jpg"), FiducialsButton.IsChecked ?? false);
            File.WriteAllText(folderPath + fname +  ".txt", storedImage.caption + ":- " + storedImage.description);
            return (fname + (isPng ? ".png" : ".jpg"));
        }

        /// <summary>
        /// Saves any fiducial lines associated with the image at the current settings, replacing any previously
        /// defined fiducial lines
        /// </summary>
        internal void Save()
        {
            if (isModified)
            {
                storedImage.Update();
            }
            isModified = false;
        }

        /// <summary>
        /// Raises the <see cref="DelButtonPressed" /> event.
        /// </summary>
        /// <param name="e"><see cref="EventArgs" /> object that provides the arguments for the event.</param>
        protected virtual void OnDelButtonPressed(EventArgs e)
        {
            EventHandler<EventArgs> handler = null;

            lock (DelButtonPressedEventLock)
            {
                handler = DelButtonPressedEvent;

                if (handler == null)
                    return;
            }

            handler(this, e);
        }

        /// <summary>
        /// Raises the <see cref="DownButtonPressed" /> event.
        /// </summary>
        /// <param name="e"><see cref="EventArgs" /> object that provides the arguments for the event.</param>
        protected virtual void OnDownButtonPressed(EventArgs e)
        {
            EventHandler<EventArgs> handler = null;

            lock (DownButtonPressedEventLock)
            {
                handler = DownButtonPressedEvent;

                if (handler == null)
                    return;
            }

            handler(this, e);
        }

        /// <summary>
        /// Event raised by pressing UP
        /// </summary>
        /// <param name="e"><see cref="EventArgs" /> object that provides the arguments for the event.</param>
        protected virtual void OnUpButtonPressed(EventArgs e)
        {
            EventHandler<EventArgs> handler = null;

            lock (UpButtonPressedEventLock)
            {
                handler = UpButtonPressedEvent;

                if (handler == null)
                    return;
            }

            handler(this, e);
        }

        

        
        private readonly object DelButtonPressedEventLock = new object();
        private readonly object DownButtonPressedEventLock = new object();
        private readonly object UpButtonPressedEventLock = new object();
        private StoredImage _storedImage = new StoredImage(null, "", "", -1);
        private Canvas axisGrid = new Canvas();

        private EventHandler<EventArgs> DelButtonPressedEvent;

        private EventHandler<EventArgs> DownButtonPressedEvent;

        

        private bool isPlacingFiducialLines = false;

        private int selectedLine = -1;

        private bool showGrid = false;

        private EventHandler<EventArgs> UpButtonPressedEvent;


        #region FullButtonRClickedEvent

        /// <summary>
        /// Event raised after the  property value has changed.
        /// </summary>
        public event EventHandler<EventArgs> FullButtonRClicked
        {
            add
            {
                lock (FullButtonRClickedEventLock)
                {
                    FullButtonRClickedEvent += value;
                }
            }
            remove
            {
                lock (FullButtonRClickedEventLock)
                {
                    FullButtonRClickedEvent -= value;
                }
            }
        }

        /// <summary>
        /// Raises the <see cref="FullButtonRClicked" /> event.
        /// </summary>
        /// <param name="e"><see cref="EventArgs" /> object that provides the arguments for the event.</param>
        protected virtual void OnFullButtonRClicked(EventArgs e)
        {
            EventHandler<EventArgs> handler = null;

            lock (FullButtonRClickedEventLock)
            {
                handler = FullButtonRClickedEvent;

                if (handler == null)
                    return;
            }

            handler(this, e);
        }

        // Add Event for adding image/s when there is no segment selected
        private readonly object FullButtonRClickedEventLock = new object();

        private EventHandler<EventArgs> FullButtonRClickedEvent;

        #endregion FullButtonRClickedEvent

        private readonly object FidsButtonRClickedEventLock = new object();
        private EventHandler<EventArgs> FidsButtonRClickedEvent;

        /// <summary>
        /// Event raised after the <see cref="Text" /> property value has changed.
        /// </summary>
        public event EventHandler<EventArgs> FidsButtonRClicked
        {
            add
            {
                lock (FidsButtonRClickedEventLock)
                {
                    FidsButtonRClickedEvent += value;
                }
            }
            remove
            {
                lock (FidsButtonRClickedEventLock)
                {
                    FidsButtonRClickedEvent -= value;
                }
            }
        }

        /// <summary>
        /// Raises the <see cref="FidsButtonRClicked" /> event.
        /// </summary>
        /// <param name="e"><see cref="EventArgs" /> object that provides the arguments for the event.</param>
        protected virtual void OnFidsButtonRClicked(object sender, EventArgs e)
        {
            EventHandler<EventArgs> handler = null;

            lock (FidsButtonRClickedEventLock)
            {
                handler = FidsButtonRClickedEvent;

                if (handler == null)
                    return;
            }

            handler(sender, e);
        }


        private enum Orientation
        { HORIZONTAL, VERTICAL };

        /// <summary>
        /// LineMap has two integers.  The key is the index of a Line in the children of displayImageCanvas
        /// the value is the index of a HorizontalGridLine List of the storedImage.
        /// The Y values of the Line are bound to the HorizontalGridLine element through a scaling
        /// converter.  The line is moved by referencing the relevant HGL via this table and bumping the
        /// value appropriately
        /// </summary>
        private Dictionary<int, int> HLineMap { get; set; } = new Dictionary<int, int>();

        /// <summary>
        /// LineMap has two integers.  The key is the index of a Line in the children of displayImageCanvas
        /// the value is the index of a VerticalGridLine List of the storedImage.
        /// The Y values of the Line are bound to the VericalGridLine element through a scaling
        /// converter.  The line is moved by referencing the relevant VGL via this table and bumping the
        /// value appropriately
        /// </summary>
        private Dictionary<int, int> VLineMap { get; set; } = new Dictionary<int, int>();

        private bool AdjustFiducials(object sender, KeyEventArgs e, int moveSize)
        {
            bool result = false;
            switch (e.Key)
            {
                case Key.Tab:
                    if (e.Key == Key.Tab && (Keyboard.Modifiers & ModifierKeys.Shift) == (ModifierKeys.Shift))
                    {
                        result = DecrementSelectedLine();
                    }
                    else
                    {
                        result = IncrementSelectedLine();
                    }
                    break;

                case Key.Up:
                    result = MoveLineUp(moveSize);
                    break;

                case Key.Down:
                    result = MoveLineDown(moveSize);
                    break;

                case Key.Right:
                    result = MoveLineRight(moveSize);
                    break;

                case Key.Left:
                    result = MoveLineLeft(moveSize);
                    break;

                case Key.Delete:
                    if (selectedLine >= 0)
                    {
                        result = DeleteGridLine();
                    }
                    break;

                default:
                    break;
            }
            
            displayImageCanvas.UpdateLayout();
            return (result);
        }

        /// <summary>
        /// A key is pressed while the FiducialGrid is Visible and the GridAdjustToggleButton is pressed.
        /// Arrow keys move the grid up, down left and right;
        /// SHIFT-arrowkeys move the top of the grid up and down compressing the row sizes
        /// CTRL-arrowkeys moves the bottom of the grid up and down  compressing/expanding the row heights
        /// LEFT-ALT-arrowkeys move the left margin of the grid right and left compressing/expanding the column widths
        /// RIGHT-ALT-arrowkeys move the right margin of the grid compressing/expanding the column widths
        /// NUMPAD-+ add a column at the right
        /// NUMPAD - delete the rightmost column
        /// NUMPAD * add a row
        ///
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void AdjustGridSize(object sender, KeyEventArgs e)
        {
        }

        /// <summary>
        /// Clears all Fiducial gridlines from the canvas and clears the H and V lineMaps.
        /// Sets the selectedLine to -1.
        /// </summary>
        private void ClearGridlines()
        {
            linesDrawn = false;
            if (displayImageCanvas != null)
            {
                if (displayImageCanvas.Children != null)
                {
                    displayImageCanvas.ClearExceptGrids();
                }
                if (HLineMap != null)
                {
                    HLineMap.Clear();
                }
                else
                {
                    HLineMap = new Dictionary<int, int>();
                }
                if (VLineMap != null)
                {
                    VLineMap.Clear();
                }
                else
                {
                    VLineMap = new Dictionary<int, int>();
                }
                selectedLine = -1;
            }
        }

        /// <summary>
        /// Clicking the COPY button
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void CpyButton_Click(object sender, RoutedEventArgs e)
        {
            /*
            System.Windows.Forms.DataFormats.Format myFormat = System.Windows.Forms.DataFormats.GetFormat("myFormat");

            /* Creates a new object and stores it in a DataObject using myFormat
             * as the type of format. *//*
            MyNewObject myObject = new MyNewObject();
            DataObject myDataObject = new DataObject(myFormat.Name, myObject);

            // Copies myObject into the clipboard.
            Clipboard.SetDataObject(myDataObject);

            // Performs some processing steps.

            // Retrieves the data from the clipboard.
            IDataObject myRetrievedObject = Clipboard.GetDataObject();

            // Converts the IDataObject type to MyNewObject type.
            MyNewObject myDereferencedObject = (MyNewObject)myRetrievedObject.GetData(myFormat.Name);

            // Prints the value of the Object in a textBox.
            //textBox1.Text = myDereferencedObject.MyObjectValue;

            */
            Debug.WriteLine("Clicked the COPY button");
            var siFormat = System.Windows.DataFormats.GetDataFormat("siFormat");
            var obj = new DataObject();

            obj.SetText("***" + storedImage.ImageID.ToString());
            obj.SetImage(storedImage.image);
            Clipboard.SetDataObject(obj);

            //Clipboard.SetImage(storedImage.image);
            //Clipboard.SetText("***"+storedImage.ImageID.ToString());
            /*bool isImage = Clipboard.ContainsImage();
            bool isText = Clipboard.ContainsText();
            var cbtext = Clipboard.GetText();
            var cbimage = Clipboard.GetImage();*/
            displayImageCanvas.Focus();
        }

        private bool DecrementSelectedLine()
        {
            bool result = false;
            selectedLine--;
            while (selectedLine >= 0 && !(displayImageCanvas.Children[selectedLine] is Line))
            {
                selectedLine--;
            }
            if (selectedLine < -1)
            {
                selectedLine = displayImageCanvas.Children.Count - 1;
            }
            HighlightSelectedLine();
            result = true;
            return result;
        }

        /// <summary>
        /// deletes the currently selected Fiducial Grid Line from the storedImage
        /// and redraws all the lines from scratch.  Selected line is preserved if possible.
        /// </summary>
        private bool DeleteGridLine()
        {
            bool result = false;
            if (selectedLine >= 0 && displayImageCanvas.Children != null && displayImageCanvas.Children.Count > selectedLine)
            {
                try
                {
                    int previouslySelectedLine = selectedLine;
                    int lineIndex = -1;
                    Line line = displayImageCanvas.Children[selectedLine] as Line;
                    if (line.VerticalAlignment == VerticalAlignment.Stretch)
                    {
                        // we have selected a vertical line
                        lineIndex = VLineMap[selectedLine];
                        if (lineIndex >= 0 && lineIndex < storedImage.VerticalGridLines.Count)
                        {
                            storedImage.VerticalGridLines.RemoveAt(lineIndex);
                        }
                    }
                    else
                    {
                        // we have selected a horizontal line
                        lineIndex = HLineMap[selectedLine];
                        if (lineIndex >= 0 && lineIndex < storedImage.HorizontalGridlines.Count)
                        {
                            storedImage.HorizontalGridlines.RemoveAt(lineIndex);
                        }
                    }
                    ClearGridlines();
                    DrawAllLines();
                    if (previouslySelectedLine > 0 && previouslySelectedLine < displayImageCanvas.Children.Count)
                    {
                        selectedLine = previouslySelectedLine;
                    }
                    else
                    {
                        selectedLine = displayImageCanvas.Children.Count - 1;
                    }
                    HighlightSelectedLine();
                    result = true;
                }catch(Exception ex)
                {
                    Debug.WriteLine("Line selection error:- " + ex.Message);
                    Tools.ErrorLog("DeleteGridLine:-" + ex.Message);
                    result = false;
                }
            }
            displayImageCanvas.Focus();
            return (result);
        }

        private void DeleteImageButton_Click(object sender, RoutedEventArgs e)
        {
            OnDelButtonPressed(new EventArgs());
            displayImageCanvas.Focus();
        }

        /// <summary>
        /// EventHander for when the window is loaded - identifies the current
        /// window and adds a handler for the PreviewKeyDown event which is used to
        /// adjust the size of the scale grid
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void DisplayImage_Loaded(object sender, RoutedEventArgs e)
        {
            var window = Window.GetWindow(this);

            displayImageCanvas.Focus();
            e.Handled = true;
        }

        /// <summary>
        /// creates a fiducial line horizontally on the image which will be dragged by the mouse
        /// and made permanent by releasing the mouse button.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void DisplayImage_MouseRightButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (!e.Handled)
            {
                e.Handled = true;
                if (FiducialsButton.IsChecked ?? false)
                {
                    var pos = e.GetPosition(displayImageCanvas);
                    Debug.WriteLine("X=" + pos.X + " Y=" + pos.Y + " dic.W=" + displayImageCanvas.ActualWidth + " dic.H=" + displayImageCanvas.ActualHeight + "\n");

                    if (pos.X >= 0 && pos.X < displayImageCanvas.ActualWidth && pos.Y >= 0 && pos.Y < displayImageCanvas.ActualHeight)
                    {
                        //Tools.InfoLog("Right Mouse Button");
                        selectedLine = -1;
                        HighlightSelectedLine();
                        bool isVertical = false;
                        int imageLineIndex = -1;

                        Line line = new Line();
                        if (Keyboard.IsKeyDown(Key.LeftShift))
                        {
                            isVertical = true;

                            if (storedImage.VerticalGridLines == null) storedImage.VerticalGridLines = new List<int>();
                            storedImage.VerticalGridLines.Add(WidthDeScale(pos.X));
                            imageLineIndex = storedImage.VerticalGridLines.Count - 1;
                            DrawLine(imageLineIndex, Orientation.VERTICAL);

                            //TOD add binding to VGL of storedImage
                        }
                        else
                        {
                            try
                            {
                                isVertical = false;

                                var newGL = HeightDeScale(pos.Y);

                                if (storedImage.HorizontalGridlines == null)
                                {
                                    storedImage.HorizontalGridlines = new List<int>();
                                }
                                storedImage.HorizontalGridlines.Add(newGL);
                                //Tools.InfoLog("At canvas=" + pos.Y + " Image=" + newGL);

                                imageLineIndex = storedImage.HorizontalGridlines.Count - 1;
                                DrawLine(imageLineIndex, Orientation.HORIZONTAL);
                            }
                            catch (Exception ex)
                            {
                                Tools.ErrorLog("££££££££££   " + ex.Message + "::" + ex.ToString());
                                ClearGridlines();
                                DrawAllLines();
                                return;
                            }
                        }

                        selectedLine = displayImageCanvas.Children.Count - 1;
                        //Tools.InfoLog("Selected line " + selectedLine);
                        if (VLineMap == null) VLineMap = new Dictionary<int, int>();
                        if (HLineMap == null) HLineMap = new Dictionary<int, int>();

                        VLineMap.Add(selectedLine, imageLineIndex);
                        HLineMap.Add(selectedLine, imageLineIndex);

                        HighlightSelectedLine();
                        displayImageCanvas.UpdateLayout();
                        isModified = true;
                    }
                }
            }
        }

        private void DisplayImage_Unloaded(object sender, RoutedEventArgs e)
        {
        }

        private void displayImageCanvas_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            //Debug.WriteLine("MouseLeftButtonDown");
            displayImageCanvas.Focus();
        }

        private void displayImageCanvas_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (!e.Handled)
            {
                e.Handled = true;
                //Debug.WriteLine("dic.PreviewKeyDown");
                bool isShiftPressed = ((Keyboard.Modifiers & ModifierKeys.Shift) > 0);
                bool isCtrlPressed = ((Keyboard.Modifiers & ModifierKeys.Control) > 0);
                var canvas = sender as Canvas;
                this.Focus();

                if ((FiducialsButton.IsChecked ?? false) && isCtrlPressed && e.Key == Key.D)
                {// CTRL-D while fiducials is active causes the fiducial lines of the current image (if any)
                 // to be duplicated to all images that do not already have some fiducial lines
                 // through an event handler so that the action is taken by the parent which has access to allthe sibling images
                    if (canvas != null && canvas.Children.Count > 2)
                    {
                        List<double> HGLProps = new List<double>();
                        List<double> VGLProps = new List<double>();
                        foreach (var hgl in storedImage.HorizontalGridlines)
                        {
                            HGLProps.Add(hgl / storedImage.image.Height);
                        }
                        foreach (var vgl in storedImage.VerticalGridLines)
                        {
                            VGLProps.Add(vgl / storedImage.image.Width);
                        }
                        OnDuplicate(new DuplicateEventArgs(HGLProps, VGLProps,
                            axisGrid == null ? DuplicateEventArgs.GRID.K675 : (axisGrid == this.axisGrid675 ? DuplicateEventArgs.GRID.K675 : DuplicateEventArgs.GRID.K7029),
                            gridLeftMargin, gridTopMargin, gridScaleValue));

                        return;
                    }

                }

                int moveSize = 5;
                double scaleSize = 1.1d;
                if (isShiftPressed)
                {
                    moveSize = 1;
                    scaleSize = 1.005d;
                }
                double moveScale = 0.002d;

                //Debug.WriteLine("wKey Previewed =" + e.Key.ToString());


                //var stackPanel = (StackPanel)(canvas.Parent as Grid).Parent);

                if (canvas != null)
                {
                    if (FiducialsButton.IsChecked ?? false)
                    {
                        //Debug.WriteLine("Adjust Fiducials");
                        if (AdjustFiducials(sender, e, moveSize))
                        {
                            e.Handled = true;
                            isModified = true;
                        }
                    }
                    else
                    {
                        try
                        {
                            //var dispImCont = cwin.ComparisonStackPanel.SelectedItem as DisplayStoredImageControl;

                            if (showGrid)
                            {
                                if (GridSelectionComboBox.SelectedIndex == 0 || GridSelectionComboBox.SelectedIndex == 1)
                                {
                                    axisGrid = this.axisGrid675;
                                }
                                else
                                {
                                    axisGrid = this.axisGrid7029A;
                                }

                                var img = this.displayImageCanvas;

                                var margin = axisGrid.Margin;

                                switch (e.Key)
                                {
                                    case Key.PageUp:
                                        gridScaleValue = gridScaleValue * scaleSize;
                                        e.Handled = true;
                                        break;

                                    case Key.PageDown:
                                        gridScaleValue = gridScaleValue * (1 / scaleSize);
                                        e.Handled = true;
                                        break;

                                    case Key.Up:

                                        gridTopMargin -= moveSize * moveScale;
                                        if (gridTopMargin < 0) gridTopMargin = 0;
                                        e.Handled = true;
                                        break;

                                    case Key.Down:

                                        gridTopMargin += moveSize * moveScale;
                                        var hgridProportion = axisGrid.ActualHeight / displayImageCanvas.ActualHeight;
                                        var hspaceProportion = 1.0d - hgridProportion;
                                        if (gridTopMargin > hspaceProportion)
                                        {
                                            gridTopMargin = hspaceProportion;
                                        }

                                        e.Handled = true;
                                        break;

                                    case Key.Right:
                                        // increase gridLeftMargin by moveSize*.005
                                        // gridLeftMargin is the proportion of displayImageCanvas.Width where the top elft corner will be

                                        gridLeftMargin += moveSize * moveScale;

                                        // the left margin proportion must be less than the space left by the grid
                                        var gridProportion = axisGrid.ActualWidth / displayImageCanvas.ActualWidth;
                                        var spaceProportion = 1.0d - gridProportion;

                                        if (gridLeftMargin > spaceProportion)
                                        {
                                            gridLeftMargin = spaceProportion;
                                        }

                                        e.Handled = true;
                                        break;

                                    case Key.Left:

                                        gridLeftMargin -= moveSize * moveScale;
                                        if (gridLeftMargin < 0) gridLeftMargin = 0;
                                        e.Handled = true;
                                        break;

                                    default:
                                        break;
                                }

                                e.Handled = true;
                            }
                        }
                        catch (NullReferenceException nre)
                        {
                            Debug.WriteLine(nre);
                        }
                    }
                }
                var oldwidth = displayImageCanvas.RenderSize.Width;
                displayImageCanvas.RenderSize = new Size(oldwidth + 1, displayImageCanvas.RenderSize.Height);
                displayImageCanvas.InvalidateVisual();
                displayImageCanvas.UpdateLayout();
                //this.InvalidateVisual();
                //this.UpdateLayout();
                displayImageCanvas.RenderSize = new Size(oldwidth, displayImageCanvas.RenderSize.Height);
                displayImageCanvas.Focus();
            }
            
        }

        

        

        private void DownImageButton_Click(object sender, RoutedEventArgs e)
        {
            if (!e.Handled)
            {
                e.Handled = true;
                OnDownButtonPressed(new EventArgs());
                displayImageCanvas.Focus();
            }
        }

        private bool linesDrawn = false;
        private void DrawAllLines()
        {
            Debug.WriteLine("DrawAllLines...");
            if (!linesDrawn)
            {
                Debug.WriteLine(".....OK");
                if (storedImage.HorizontalGridlines != null)
                {
                    int i = 0;
                    foreach (var gridline in storedImage.HorizontalGridlines)
                    {
                        DrawLine(i, Orientation.HORIZONTAL);
                        HLineMap.Add(displayImageCanvas.Children.Count - 1, i++);
                        VLineMap.Add(displayImageCanvas.Children.Count - 1, i - 1);
                    }
                }
                if (storedImage.VerticalGridLines != null)
                {
                    int i = 0;
                    foreach (var gridline in storedImage.VerticalGridLines)
                    {
                        DrawLine(i, Orientation.VERTICAL);
                        VLineMap.Add(displayImageCanvas.Children.Count - 1, i++);
                        HLineMap.Add(displayImageCanvas.Children.Count - 1, i - 1);
                    }
                }
            }
            linesDrawn = true;
            Debug.WriteLine("Number of lines is:-" + (displayImageCanvas.Children.Count - 2));
        }

        /// <summary>
        /// draws the
        /// </summary>
        /// <param name="indexToGridLine"></param>
        /// <param name="direction"></param>
        private void DrawLine(int indexToGridLine, Orientation direction)
        {
            Line line = new Line();

            line.Stroke = System.Windows.Media.Brushes.Black;
            line.StrokeThickness = 1;

            if (direction == Orientation.HORIZONTAL && indexToGridLine >= 0 && indexToGridLine <= storedImage.image.Height)
            {
                line.HorizontalAlignment = HorizontalAlignment.Stretch;
                line.VerticalAlignment = VerticalAlignment.Center;
                //line.Y1 = HeightScale(gridline);
                //line.Y2 = line.Y1;
                //line.X1 = 0;
                //Binding binding = new Binding();
                //binding.Source = displayImageCanvas;
                //binding.Path = new PropertyPath("ActualWidth");
                //BindingOperations.SetBinding(line, Line.X2Property, binding);

                MultiBinding mbXBinding = new MultiBinding();
                mbXBinding.Converter = new LeftMarginConverter();
                Binding binding = new Binding();
                
                //binding.Source = this;
                binding.RelativeSource = new RelativeSource(RelativeSourceMode.FindAncestor, typeof(Canvas), 1);
                binding.Path = new PropertyPath("ActualWidth");
                mbXBinding.Bindings.Add(binding);

                binding = new Binding();
                //binding.Source = this;
                binding.RelativeSource = new RelativeSource(RelativeSourceMode.FindAncestor, typeof(Canvas), 1);
                binding.Path = new PropertyPath("ActualHeight");
                mbXBinding.Bindings.Add(binding);

                binding = new Binding();
                binding.Source = this;
                binding.Path = new PropertyPath("storedImage");
                mbXBinding.Bindings.Add(binding);

                BindingOperations.SetBinding(line, Line.X1Property, mbXBinding);

                mbXBinding = new MultiBinding();
                mbXBinding.Converter = new RightMarginConverter();
                binding = new Binding();

                //binding.Source = this;
                binding.RelativeSource = new RelativeSource(RelativeSourceMode.FindAncestor, typeof(Canvas), 1);
                binding.Path = new PropertyPath("ActualWidth");
                mbXBinding.Bindings.Add(binding);

                binding = new Binding();
                //binding.Source = this;
                binding.RelativeSource = new RelativeSource(RelativeSourceMode.FindAncestor, typeof(Canvas), 1);
                binding.Path = new PropertyPath("ActualHeight");
                mbXBinding.Bindings.Add(binding);

                binding = new Binding();
                binding.Source = this;
                binding.Path = new PropertyPath("storedImage");
                mbXBinding.Bindings.Add(binding);

                BindingOperations.SetBinding(line, Line.X2Property, mbXBinding);


                MultiBinding mBinding = new MultiBinding();
                mBinding.Converter = new HGridLineConverter();

                binding = new Binding();
                //double proportion = FindHScaleProportion(gridline);
                binding.Source = indexToGridLine.ToString();
                mBinding.Bindings.Add(binding);

                binding = new Binding();
                //binding.Source = this;
                binding.RelativeSource = new RelativeSource(RelativeSourceMode.FindAncestor,typeof(Canvas),1);
                binding.Path = new PropertyPath("ActualWidth");
                mBinding.Bindings.Add(binding);

                binding = new Binding();
                //binding.Source = this;
                binding.RelativeSource = new RelativeSource(RelativeSourceMode.FindAncestor, typeof(Canvas), 1);
                binding.Path = new PropertyPath("ActualHeight");
                mBinding.Bindings.Add(binding);

                /*binding = new Binding();
                binding.Source = this;
                binding.Path = new PropertyPath("storedImage");*/
                binding = new Binding();
                binding.Source = storedImage;
                binding.BindsDirectlyToSource = true;
                binding.NotifyOnSourceUpdated = true;
                mBinding.Bindings.Add(binding);

                BindingOperations.SetBinding(line, Line.Y1Property, mBinding);
                BindingOperations.SetBinding(line, Line.Y2Property, mBinding);

                

            }
            else
            {
                line.VerticalAlignment = VerticalAlignment.Stretch;
                line.HorizontalAlignment = HorizontalAlignment.Center;
                //line.X1 = WidthScale(gridline);
                //line.X2 = line.X1;
                //line.Y1 = 0;
                //Binding binding = new Binding();
                //binding.Source = displayImageCanvas;
                //binding.Path = new PropertyPath("ActualHeight");
                //BindingOperations.SetBinding(line, Line.Y2Property, binding);

                MultiBinding mbXBinding = new MultiBinding();
                mbXBinding.Converter = new TopMarginConverter();
                Binding binding = new Binding();

                //binding.Source = this;
                binding.RelativeSource = new RelativeSource(RelativeSourceMode.FindAncestor, typeof(Canvas), 1);
                binding.Path = new PropertyPath("ActualWidth");
                mbXBinding.Bindings.Add(binding);

                binding = new Binding();
                //binding.Source = this;
                binding.RelativeSource = new RelativeSource(RelativeSourceMode.FindAncestor, typeof(Canvas), 1);
                binding.Path = new PropertyPath("ActualHeight");
                mbXBinding.Bindings.Add(binding);

                binding = new Binding();
                binding.Source = this;
                binding.Path = new PropertyPath("storedImage");
                mbXBinding.Bindings.Add(binding);

                BindingOperations.SetBinding(line, Line.Y1Property, mbXBinding);

                mbXBinding = new MultiBinding();
                mbXBinding.Converter = new BottomMarginConverter();
                binding = new Binding();

                //binding.Source = this;
                binding.RelativeSource = new RelativeSource(RelativeSourceMode.FindAncestor, typeof(Canvas), 1);
                binding.Path = new PropertyPath("ActualWidth");
                mbXBinding.Bindings.Add(binding);

                binding = new Binding();
                //binding.Source = this;
                binding.RelativeSource = new RelativeSource(RelativeSourceMode.FindAncestor, typeof(Canvas), 1);
                binding.Path = new PropertyPath("ActualHeight");
                mbXBinding.Bindings.Add(binding);

                binding = new Binding();
                binding.Source = this;
                binding.Path = new PropertyPath("storedImage");
                mbXBinding.Bindings.Add(binding);

                BindingOperations.SetBinding(line, Line.Y2Property, mbXBinding);


                MultiBinding mBinding = new MultiBinding();
                mBinding.Converter = new VGridLineConverter();
                binding = new Binding();
                //double proportion = FindHScaleProportion(gridline);
                binding.Source = indexToGridLine.ToString();
                mBinding.Bindings.Add(binding);

                binding = new Binding();
                //binding.Source = this;
                binding.RelativeSource = new RelativeSource(RelativeSourceMode.FindAncestor, typeof(Canvas), 1);
                binding.Path = new PropertyPath("ActualWidth");
                mBinding.Bindings.Add(binding);

                binding = new Binding();
                //binding.Source = this;
                binding.RelativeSource = new RelativeSource(RelativeSourceMode.FindAncestor, typeof(Canvas), 1);
                binding.Path = new PropertyPath("ActualHeight");
                mBinding.Bindings.Add(binding);

                binding = new Binding();
                binding.Source = this;
                binding.Path = new PropertyPath("storedImage");
                mBinding.Bindings.Add(binding);

                BindingOperations.SetBinding(line, Line.X1Property, mBinding);
                BindingOperations.SetBinding(line, Line.X2Property, mBinding);
            }
            displayImageCanvas.Children.Add(line);

            selectedLine = displayImageCanvas.Children.Count - 1;
            displayImageCanvas.UpdateLayout();
        }

        private void FiducialsButton_Checked(object sender, RoutedEventArgs e)
        {
            FiducialsButton_Click(sender, e);
        }

        /// <summary>
        /// Triggered by clicking the FIDS Toggle Button, toggles the display of
        /// fiduciary lines on the image
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void FiducialsButton_Click(object sender, RoutedEventArgs e)
        {
            
                if (this.IsLoaded)
                {
                    ClearGridlines();
                    if (FiducialsButton.IsChecked ?? false)
                    {
                        //axisGrid675.Visibility = Visibility.Hidden;
                        //axisGrid7029A.Visibility = Visibility.Hidden;

                        //GridSelectionComboBox.Visibility = Visibility.Hidden;
                        //FiducialGrid.Visibility = Visibility.Hidden;

                        //showGrid = false;

                        DrawAllLines();
                        selectedLine = -1;
                    }
                    displayImageCanvas.Focus();
                }
            
        }

        private void FiducialsButton_Unchecked(object sender, RoutedEventArgs e)
        {
            FiducialsButton_Click(sender, e);
        }

        /// <summary>
        /// Actually handles MouseLeftButtonUp to prevent action when mouse is right clicked
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void FullSizeButton_Click(object sender, RoutedEventArgs e)
        {
            Debug.WriteLine("FullSizeButtonClick");
            
                

                Button thisButton = sender as Button;

                SetImageFull((thisButton.Content as String) == "FULL");


                this.BringIntoView();
                this.Focus();
                displayImageCanvas.Focus();
            
        }

        private void FullSizeButton_MouseRightButtonUp(object sender, MouseButtonEventArgs e)
        {
            Debug.WriteLine("MouseRightButtonUp");
            if (!e.Handled)
            {
                e.Handled = true;
                OnFullButtonRClicked(e);
                this.BringIntoView();
                this.Focus();
                displayImageCanvas.Focus();
            }


        }

        private void FiducialsButton_PreviewMouseRightButtonUp(object sender, MouseButtonEventArgs e)
        {
            Debug.WriteLine("Mouse Right button uP on FIDS");
            if (!e.Handled)
            {
                e.Handled = true;
                OnFidsButtonRClicked(sender, e);
                this.BringIntoView();
                this.Focus();
                displayImageCanvas.Focus();
            }
        }

        public void SetImageFull(bool fullSize)
        {
            SetImageSize(fullSize ? 1.0d : 0.5d);
            FullSizeButton.Content = fullSize ? "HALF" : "FULL";
        }


        /// <summary>
        /// Given an X position in the displayCanvas, return the corresponding position in the
        /// original image
        /// </summary>
        /// <param name="x1"></param>
        /// <returns></returns>
        private int GetImageXPosition(double x1)
        {
            var pos = (int)((x1 / displayImageCanvas.ActualWidth) * storedImage.image.Width);
            return (pos);
        }

        /// <summary>
        /// Given a Y position within the displayed canvas, finde the corresponding Y coordinate
        /// in the original image
        /// </summary>
        /// <param name="y1"></param>
        /// <returns></returns>
        private int GetImageYPosition(double y1)
        {
            var pos = (int)((y1 / displayImageCanvas.ActualHeight) * storedImage.image.Height);
            return (pos);
        }

        private int gridToShow = 0;

        private void GridButton_Click(object sender, RoutedEventArgs e)
        {
            //FiducialsButton.IsChecked = false;
            
                if (showGrid)
                {
                    gridToShow = 0;
                    if (axisGrid675.Visibility == Visibility.Visible) gridToShow = 675;
                    if (axisGrid7029A.Visibility == Visibility.Visible) gridToShow = 7029;
                    axisGrid675.Visibility = Visibility.Hidden;
                    axisGrid7029A.Visibility = Visibility.Hidden;
                    GridSelectionComboBox.Visibility = Visibility.Hidden;
                    //FiducialGrid.Visibility = Visibility.Hidden;

                    showGrid = false;
                }
                else
                {
                    //axisGrid.Visibility = Visibility.Visible;
                    if (gridToShow == 675)
                    {
                        axisGrid675.Visibility = Visibility.Visible;
                        axisGrid = axisGrid675;
                    }
                    else axisGrid675.Visibility = Visibility.Hidden;

                    if (gridToShow == 7029)
                    {
                        axisGrid7029A.Visibility = Visibility.Visible;
                        axisGrid = axisGrid7029A;
                    }
                    else axisGrid7029A.Visibility = Visibility.Hidden;
                    GridSelectionComboBox.Visibility = Visibility.Visible;
                    GridSelectionComboBox.IsDropDownOpen = true;
                    showGrid = true;
                }
            
        }

        private void GridSelectionComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            GridSelectionComboBox.IsDropDownOpen = false;
            if (showGrid)
            {
                if (GridSelectionComboBox.SelectedIndex == 0 || GridSelectionComboBox.SelectedIndex == 1)
                {
                    axisGrid675.Visibility = Visibility.Visible;
                    axisGrid7029A.Visibility = Visibility.Hidden;
                    axisGrid = axisGrid675;
                    //gridTopMargin = 0.1d;
                    //gridLeftMargin = 0.1d;
                    gridToShow = 675;
                    //FiducialGrid.Visibility = Visibility.Hidden;
                }
                if (GridSelectionComboBox.SelectedIndex == 2 || GridSelectionComboBox.SelectedIndex == 3)
                {
                    axisGrid7029A.Visibility = Visibility.Visible;
                    axisGrid675.Visibility = Visibility.Hidden;
                    axisGrid = axisGrid7029A;
                    //gridTopMargin = 0.1d;
                    //gridLeftMargin = 0.1d;
                    gridToShow = 7029;
                    //FiducialGrid.Visibility = Visibility.Hidden;
                }
                if (GridSelectionComboBox.SelectedIndex == 4)
                {
                    axisGrid675.Visibility = Visibility.Hidden;
                    axisGrid7029A.Visibility = Visibility.Hidden;
                    gridToShow = 0;
                    //FiducialGrid.Visibility = Visibility.Visible;
                }
            }
            displayImageCanvas.Focus();
        }

        private int HeightDeScale(double y)
        {
            double hscale = (double)displayImageCanvas.ActualWidth/ (double)storedImage.image.Width;
            double vscale = (double)displayImageCanvas.ActualHeight / (double)storedImage.image.Height;
            double actualScale = Math.Min(hscale, vscale);

            var rightAndLeftMargins = Math.Abs(displayImageCanvas.ActualWidth - (storedImage.image.Width * actualScale));
            var topAndBottomMargins = Math.Abs(displayImageCanvas.ActualHeight - (storedImage.image.Height * actualScale));

            var positionInScaledImage = y - (topAndBottomMargins / 2);
            var proportionOfScaledImage = positionInScaledImage / (storedImage.image.Height * actualScale);
            var positionInImage = proportionOfScaledImage * storedImage.image.Height;
            return (int)(positionInImage);
        }

        
        /// <summary>
        /// Finds the ratio of the gridline position to the stored image height making allowance for the fit of
        /// the image in the background of the displayImageCanvas in that it might be stretched to fit
        /// vertically or horizontally but not both.  The returned value, when multiplied by the actual
        /// height of the canvas should determine the location of the horizontal gridline on the canvas.
        /// </summary>
        /// <param name="linePositionInImage"></param>
        /// <param name="displayImageCanvas"></param>
        /// <param name="storedImage"></param>
        /// 
        /// <returns></returns>
        public static double FindHScaleProportion(int linePositionInImage,double canvasWidth,double canvasHeight,StoredImage storedImage)
        {

            //Debug.WriteLine("============================================================================================");
            double hscale =  (double)canvasWidth/ (double)storedImage.image.Width;
            double vscale =  (double)canvasHeight/ (double)storedImage.image.Height;
            double actualScale = Math.Min(hscale, vscale);
            //Debug.WriteLine("Scale: H=" + hscale + " V=" + vscale + " Actual=" + actualScale);

            var linePositionAsProportionOfImage = (double)linePositionInImage / storedImage.image.Height;
            //Debug.WriteLine("Initial:- Position=" + linePositionInImage + " of Heigh=" + storedImage.image.Height+" prop="+linePositionAsProportionOfImage);

            var linePositionInScaledImage = linePositionAsProportionOfImage * (storedImage.image.Height * actualScale);
            //Debug.WriteLine("Position In scale=" + linePositionInScaledImage+" in canvas of (h/w) "+canvasHeight+"/"+canvasWidth);
            
            var rightAndLeftMargins = Math.Abs(canvasWidth - (storedImage.image.Width * actualScale));
            var topAndBottomMargins = Math.Abs(canvasHeight - (storedImage.image.Height * actualScale));
            //Debug.WriteLine("Margins:- r+l=" + rightAndLeftMargins + " t+b=" + topAndBottomMargins);
            var linePositionInCanvas = linePositionInScaledImage + (topAndBottomMargins / 2);
            
            var linePositionAsProportionOfCanvas =  linePositionInCanvas/ canvasHeight;
            //Debug.WriteLine("Pos in Canvas=" + linePositionInCanvas+" As proportion="+linePositionAsProportionOfCanvas);
            //Debug.WriteLine("----------------------------------------------------------------------------------------------");
            return (linePositionAsProportionOfCanvas);
        }

        /// <summary>
        /// Finds the ratio of the gridline position to the stored image height making allowance for the fit of
        /// the image in the background of the displayImageCanvas in that it might be stretched to fit
        /// vertically or horizontally but not both.  The returned value, when multiplied by the actual
        /// height of the canvas should determine the location of the horizontal gridline on the canvas.
        /// </summary>
        /// <param name="linePositionInImage"></param>
        /// <param name="displayImageCanvas"></param>
        /// <param name="storedImage"></param>
        /// 
        /// <returns></returns>
        public static double FindVScaleProportion(int linePositionInImage, double canvasWidth, double canvasHeight, StoredImage storedImage)
        {

            //Debug.WriteLine("============================================================================================");
            double hscale = (double)canvasWidth / (double)storedImage.image.Width;
            double vscale = (double)canvasHeight / (double)storedImage.image.Height;
            double actualScale = Math.Min(hscale, vscale);
            //Debug.WriteLine("Scale: H=" + hscale + " V=" + vscale + " Actual=" + actualScale);

            var linePositionAsProportionOfImage = (double)linePositionInImage / storedImage.image.Width;
            //Debug.WriteLine("Initial:- Position=" + linePositionInImage + " of Heigh=" + storedImage.image.Height + " prop=" + linePositionAsProportionOfImage);

            var linePositionInScaledImage = linePositionAsProportionOfImage * (storedImage.image.Width * actualScale);
            //Debug.WriteLine("Position In scale=" + linePositionInScaledImage + " in canvas of (h/w) " + canvasHeight + "/" + canvasWidth);

            var rightAndLeftMargins = Math.Abs(canvasWidth - (storedImage.image.Width * actualScale));
            var topAndBottomMargins = Math.Abs(canvasHeight - (storedImage.image.Height * actualScale));
            //Debug.WriteLine("Margins:- r+l=" + rightAndLeftMargins + " t+b=" + topAndBottomMargins);
            var linePositionInCanvas = linePositionInScaledImage + (rightAndLeftMargins / 2);

            var linePositionAsProportionOfCanvas = linePositionInCanvas / canvasWidth;
            //Debug.WriteLine("Pos in Canvas=" + linePositionInCanvas + " As proportion=" + linePositionAsProportionOfCanvas);
            //Debug.WriteLine("----------------------------------------------------------------------------------------------");
            return (linePositionAsProportionOfCanvas);
        }


        private void HighlightSelectedLine()
        {
            if (displayImageCanvas.Children != null)
            {
                foreach (var child in displayImageCanvas.Children)
                {
                    if (child is Line)
                    {
                        (child as Line).StrokeThickness = 1;
                    }
                }
                if (selectedLine >= 0)
                {
                    if (displayImageCanvas.Children[selectedLine] is Line)
                    {
                        (displayImageCanvas.Children[selectedLine] as Line).StrokeThickness = 2;
                    }
                }
            }
            displayImageCanvas.UpdateLayout();
            displayImageCanvas.Focus();
        }

        private bool IncrementSelectedLine()
        {
            Debug.Write("Incrementing from " + selectedLine);
            bool result = false;
            selectedLine++;
            while (selectedLine < displayImageCanvas.Children.Count && !(displayImageCanvas.Children[selectedLine] is Line))
            {
                selectedLine++;
            }

            if (selectedLine >= displayImageCanvas.Children.Count)
            {
                selectedLine = -1;
            }
            HighlightSelectedLine();
            Debug.WriteLine(" to " + selectedLine);
            result = true;
            return result;
        }

        

        private bool MoveLineLeft(int moveSize)
        {
            bool result = false;
            if (displayImageCanvas.Children != null && selectedLine >= 0 && displayImageCanvas.Children.Count > selectedLine)
            {
                if (displayImageCanvas.Children[selectedLine] is Line)
                {
                    Line line = displayImageCanvas.Children[selectedLine] as Line;
                    if (line.VerticalAlignment == VerticalAlignment.Stretch)
                    {
                        var xy = VLineMap[selectedLine];
                        storedImage.VerticalGridLines[xy] -= moveSize;
                        if (storedImage.VerticalGridLines[xy] < 0) storedImage.VerticalGridLines[xy] = 0;

                        //line.X1 = WidthScale(storedImage.VerticalGridLines[xy]);
                        //line.X2 = line.X1;
                        result = true;
                    }
                }
            }
            return result;
        }

        private bool MoveLineRight(int moveSize)
        {
            bool result = false;
            if (displayImageCanvas.Children != null && selectedLine >= 0 && displayImageCanvas.Children.Count > selectedLine)
            {
                if (displayImageCanvas.Children[selectedLine] is Line)
                {
                    Line line = displayImageCanvas.Children[selectedLine] as Line;
                    if (line.VerticalAlignment == VerticalAlignment.Stretch)
                    {
                        var xy = VLineMap[selectedLine];
                        storedImage.VerticalGridLines[xy] += moveSize;
                        if (storedImage.VerticalGridLines[xy] > storedImage.image.Width) storedImage.VerticalGridLines[xy] = (int)storedImage.image.Width;

                        //line.X1 = WidthScale(storedImage.VerticalGridLines[xy]);
                        //ine.X2 = line.X1;
                        result = true;
                    }
                }
            }
            return result;
        }

        private bool MoveLineDown(int moveSize)
        {
            bool result = false;
            if (displayImageCanvas.Children != null && selectedLine >= 0 && displayImageCanvas.Children.Count > selectedLine)
            {
                if (displayImageCanvas.Children[selectedLine] is Line)
                {
                    Line line = displayImageCanvas.Children[selectedLine] as Line;
                    if (line.HorizontalAlignment == HorizontalAlignment.Stretch)
                    {
                        var xy = HLineMap[selectedLine];
                        storedImage.HorizontalGridlines[xy] += moveSize;
                        if (storedImage.HorizontalGridlines[xy] > storedImage.image.Height) storedImage.HorizontalGridlines[xy] = (int)storedImage.image.Height;
                        //line.Y1 = HeightScale(storedImage.HorizontalGridlines[xy]);
                        //line.Y2 = line.Y1;
                        result = true;
                    }
                }
            }
            return result;
        }

        private bool MoveLineUp(int moveSize)
        {
            //Debug.WriteLine("--------------------------MOVELINE-UP---------------------------------");
            bool result = false;
            if (displayImageCanvas.Children != null && selectedLine >= 0 && displayImageCanvas.Children.Count > selectedLine)
            {
                if (displayImageCanvas.Children[selectedLine] is Line)
                {
                    Line line = displayImageCanvas.Children[selectedLine] as Line;
                    if (line.HorizontalAlignment == HorizontalAlignment.Stretch)
                    {
                        //Debug.WriteLine("Old Y=" + (double)line.GetValue(Line.Y1Property));

                        var xy = HLineMap[selectedLine];
                        //Debug.WriteLine("Old Gridline=" + storedImage.HorizontalGridlines[xy]);
                        storedImage.HorizontalGridlines[xy] -= moveSize;
                        if (storedImage.HorizontalGridlines[xy] < 0) storedImage.HorizontalGridlines[xy] = 0;

                        result = true;
                    }
                }
            }
            //Debug.WriteLine("-----------------------------------------" + result + "-----------------------------------------------");
            return result;
        }

        private void RotateImage90(bool clockwise)
        {
            int angle = clockwise ? 90 : -90;
            if (displayImageCanvas != null)
            {
                if (displayImageCanvas.LayoutTransform is RotateTransform Transform)
                {
                    displayImageCanvas.RenderTransformOrigin = new Point(0.5, 0.5);
                    Transform.Angle += angle;
                }
                else
                {
                    displayImageCanvas.LayoutTransform = new RotateTransform();
                    Transform = displayImageCanvas.LayoutTransform as RotateTransform;
                    displayImageCanvas.RenderTransformOrigin = new Point(0.5, 0.5);
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
            displayImageCanvas.Focus();
        }

        private void RotateImageButton_Click(object sender, RoutedEventArgs e)
        {
            RotateImage90(true);
        }

        /// <summary>
        /// sets the size of the displayImage panel by adjusting the binding
        /// converter parameter.
        /// </summary>
        /// <param name="v"></param>
        private void SetImageSize(double v)
        {
            scaleValue = v.ToString();
        }

        private void UpImageButton_Click(object sender, RoutedEventArgs e)
        {
            OnUpButtonPressed(new EventArgs());
        }

        private int WidthDeScale(double x)
        {
            double hscale = (double)displayImageCanvas.ActualWidth / (double)storedImage.image.Width;
            double vscale = (double)displayImageCanvas.ActualHeight / (double)storedImage.image.Height;
            double actualScale = Math.Min(hscale, vscale);

            var rightAndLeftMargins = Math.Abs(displayImageCanvas.ActualWidth - (storedImage.image.Width * actualScale));
            var topAndBottomMargins = Math.Abs(displayImageCanvas.ActualHeight - (storedImage.image.Height * actualScale));

            var positionInScaledImage = x - (rightAndLeftMargins / 2);
            var proportionOfScaledImage = positionInScaledImage / (storedImage.image.Width * actualScale);
            var positionInImage = proportionOfScaledImage * storedImage.image.Width;
            return (int)(positionInImage);
        }

        private readonly object DuplicateEventLock = new object();
        private EventHandler<EventArgs> DuplicateEvent;

        /// <summary>
        /// Event raised after the <see cref="Text" /> property value has changed.
        /// </summary>
        public event EventHandler<EventArgs> Duplicate
        {
            add
            {
                lock (DuplicateEventLock)
                {
                    DuplicateEvent += value;
                }
            }
            remove
            {
                lock (DuplicateEventLock)
                {
                    DuplicateEvent -= value;
                }
            }
        }

        /// <summary>
        /// Raises the <see cref="Duplicate" /> event.
        /// </summary>
        /// <param name="e"><see cref="EventArgs" /> object that provides the arguments for the event.</param>
        protected virtual void OnDuplicate(DuplicateEventArgs e)
        {
            Debug.WriteLine(e.ToString());
            EventHandler<EventArgs> handler = null;

            lock (DuplicateEventLock)
            {
                handler = DuplicateEvent;

                if (handler == null)
                    return;
            }

            handler(this, e);
        }

        /// <summary>
        /// If <see langword="abstract"/>GRID is displayed and the mouse is left-clicked, then Fiducial lines are 
        /// turned on (regardless of the previous state), all existing fiducial lines are deleted and a new set are
        /// drawn to match the locations of the lines in the displayed grid
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void FiducialsButton_PreviewMouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            if (!(Keyboard.IsKeyDown(Key.LeftCtrl) || Keyboard.IsKeyDown(Key.RightCtrl))) return; // not a CTRL-Click
            if (!showGrid) return; // GRID is not displayed so do nothing
            if (!e.Handled)
            {
                double gridWidth = 0.0d;
                double gridHeight = 0.0d;
                double gridTop = 0.0d;
                double gridLeft = 0.0d;
                int numHLines = 0;
                int numVLines = 0;
                e.Handled = true;
                ClearGridlines();
                storedImage.HorizontalGridlines.Clear();
                storedImage.VerticalGridLines.Clear();
                if (axisGrid != null)
                {
                    gridWidth = axisGrid.ActualWidth;
                    gridHeight = axisGrid.ActualHeight;
                    gridLeft = gridLeftMargin * displayImageCanvas.ActualWidth;
                    gridTop = gridTopMargin * displayImageCanvas.ActualHeight;
                    if (gridToShow == 675)
                    {
                        
                        numHLines = 7;
                        numVLines = 6;
                    }
                    else
                    {
                        numHLines = 9;
                        numVLines = 6;
                    }
                    double HLineSpacing = gridHeight / (numHLines - 1);
                    double VLineSpacing = gridWidth / (numVLines - 1);
                    double pos = gridTop;
                    for(int i = 0; i < numHLines; i++)
                    {
                        var NewGL = HeightDeScale(pos);
                        storedImage.HorizontalGridlines.Add(NewGL);
                        pos += HLineSpacing;
                    }
                    pos = gridLeft;
                    for(int i = 0; i < numVLines; i++)
                    {
                        var NewGL = WidthDeScale(pos);
                        storedImage.VerticalGridLines.Add(NewGL);
                        pos += VLineSpacing;
                    }
                    DrawAllLines();
                }

            }
        }

        private void DescriptionTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            isModified = true;
        }

        private void PlayButton_Click(object sender, RoutedEventArgs e)
        {
            if (!e.Handled)
            {
                e.Handled = true;

                if (storedImage.isPlayable)
                {

                    AudioHost.Instance.audioPlayer.Stop();
                    if (!storedImage.segmentsForImage.IsNullOrEmpty())
                    {
                        foreach (var seg in storedImage.segmentsForImage)
                        {
                            AudioHost.Instance.audioPlayer.AddToList(seg);
                        }
                    }

                }
            }
        }

        /// <summary>
        /// Opens the associated recording or segment in Audacity
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void OpenButton_Click(object sender, RoutedEventArgs e)
        {
            if (!e.Handled)
            {
                e.Handled = true;

                if (storedImage.isPlayable)
                {
                    using (new WaitCursor("Opening recording in Audacity..."))
                    {

                        storedImage.Open();
                    }

                }
            }
        }
    }

    //================================================================================================================================
    /// <summary>
    /// Provides arguments for aDuplicate Lines event.
    /// </summary>
    [Serializable]
    public class DuplicateEventArgs : EventArgs
    {
        /// <summary>
        /// default example of event args
        /// </summary>
        public new static readonly DuplicateEventArgs Empty = new DuplicateEventArgs(null,null,GRID.K675,0,0,1.0);

        #region Public Properties

        public List<double> HLineProportions { get; set; }
        public List<double> VLineProportions { get; set; }
        public enum GRID { K675, K7029 };
        public GRID GridType { get; set; }
        public double LeftMargin;
        public double TopMargin;
        public double Scale;

        #endregion Public Properties



        #region Constructors

        /// <summary>
        /// Constructs a new instance of the <see cref="DuplicateEventArgs" /> class.
        /// </summary>
        public DuplicateEventArgs(List<double> hLines, List<double> vLines, GRID gridType, double leftMargin, double topMargin, double scale)
        {
            HLineProportions = hLines;
            VLineProportions = vLines;
            GridType = gridType;
            LeftMargin = leftMargin;
            TopMargin = topMargin;
            Scale = scale;
        }

        public string ToString()
        {
            string result = "";

            if (HLineProportions != null)
            {
                result = "Horizontals at:-\n";
                foreach(var line in HLineProportions)
                {
                    result += line.ToString() + ", ";
                }
            }
            if (VLineProportions != null)
            {
                result = "\nVerticals at:-\n";
                foreach (var line in VLineProportions)
                {
                    result += line.ToString() + ", ";
                }
            }
            result += "\nGRID=" + GridType.ToString();
            result += "\nLeft Margin=" + LeftMargin;
            result += "\nTopMargin=" + TopMargin;
            result += "\nScale=" + Scale + "\n";

            return (result);
        }

        #endregion Constructors
    }
}