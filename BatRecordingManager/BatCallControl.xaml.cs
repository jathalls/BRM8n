using Microsoft.VisualStudio.Language.Intellisense;
using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace BatRecordingManager
{
    /// <summary>
    ///     Interaction logic for BatCallControl.xaml
    /// </summary>
    public partial class BatCallControl : UserControl
    {
        #region BatCall

        /// <summary>
        ///     BatCall Dependency Property
        /// </summary>
        public static readonly DependencyProperty BatCallProperty =
            DependencyProperty.Register("BatCall", typeof(Call), typeof(BatCallControl),
                new FrameworkPropertyMetadata((Call)new Call()));

        /// <summary>
        ///     Gets or sets the BatCall property. This dependency property indicates ....
        /// </summary>
        public Call BatCall
        {
            get
            {
                Call result = (Call)GetValue(BatCallProperty);

                result.CallPictures.Clear();
                if (CallImageList != null)
                {
                    foreach (var storedImage in CallImageList)
                    {
                        CallPicture callPicture = new CallPicture();
                        callPicture.BinaryData = storedImage.GetAsBinaryData();
                        result.CallPictures.Add(callPicture);
                    }
                }

                if (result != null)
                {
                    result.CallFunction = CallFunctionTextBox.Text;
                    result.CallNotes = CallTypeNotesBox.Text;
                    result.CallType = CallTypeTextBox.Text;
                    if (StartFrequencySeparatorLabel.Content as string == "+/-")
                    {
                        result.StartFrequency = StartFreqUpDown.Value;
                        result.StartFrequencyVariation = StartFreqVariationTextBox.Value;
                    }
                    else
                    {
                        double hi = StartFreqUpDown.Value ?? 0.0;
                        double lo = StartFreqVariationTextBox.Value ?? 0.0;

                        result.StartFrequency = (hi + lo) / 2.0d;
                        result.StartFrequencyVariation = Math.Abs((hi - lo) / 2.0d);
                    }
                    if (EndFrequencySeparatorLabel.Content as string == "+/-")
                    {
                        result.EndFrequency = EndFreqTextBox.Value;
                        result.EndFrequencyVariation = EndFreqVariationTextBox.Value;
                    }
                    else
                    {
                        double hi = EndFreqTextBox.Value ?? 0.0;
                        double lo = EndFreqVariationTextBox.Value ?? 0.0;
                        result.EndFrequency = (hi + lo) / 2.0d;
                        result.EndFrequencyVariation = Math.Abs((hi - lo) / 2.0d);
                    }

                    if (PeakFrequencySeparatorLabel.Content as string == "+/-")
                    {
                        result.PeakFrequency = PeakFreqTextBox.Value;
                        result.PeakFrequencyVariation = PeakFreqVariationTextBox.Value;
                    }
                    else
                    {
                        double hi = PeakFreqTextBox.Value ?? 0.0;
                        double lo = PeakFreqVariationTextBox.Value ?? 0.0;
                        result.PeakFrequency = (hi + lo) / 2.0d;
                        result.PeakFrequencyVariation = Math.Abs((hi - lo) / 2.0d);
                    }

                    if (DurationSeparatorLabel.Content as string == "+/-")
                    {
                        result.PulseDuration = PulseDurationTextBox.Value;
                        result.PulseDurationVariation = PulseDurationVariationTextBox.Value;
                    }
                    else
                    {
                        double hi = PulseDurationTextBox.Value ?? 0.0;
                        double lo = PulseDurationVariationTextBox.Value ?? 0.0;
                        result.PulseDuration = (hi + lo) / 2.0d;
                        result.PulseDurationVariation = Math.Abs((hi - lo) / 2.0d);
                    }

                    if (IntervalSeparatorLabel.Content as string == "+/-")
                    {
                        result.PulseInterval = PulseIntervalTextBox.Value;
                        result.PulseIntervalVariation = PulseIntervalVariationTextBox.Value;
                    }
                    else
                    {
                        double hi = PulseIntervalTextBox.Value ?? 0.0;
                        double lo = PulseIntervalVariationTextBox.Value ?? 0.0;
                        result.PulseInterval = (hi + lo) / 2.0d;
                        result.PulseIntervalVariation = Math.Abs((hi - lo) / 2.0d);
                    }
                }

                return (result);
            }
            set
            {
                SetValue(BatCallProperty, value);
                if (value != null)
                {
                    ShowImageButton.IsEnabled = true;
                    CallImageList.Clear();
                    if (!value.CallPictures.IsNullOrEmpty())
                    {
                        foreach (var callPicture in value.CallPictures)
                        {
                            StoredImage callImage = new StoredImage(null, "", "", -1);
                            if (callPicture.BinaryData.BinaryDataType == "BMPS" || callPicture.BinaryData.BinaryDataType == "BMP" || callPicture.BinaryData.BinaryDataType.Trim() == "PNG")
                            {
                                callImage.setBinaryData(callPicture.BinaryData);
                                CallImageList.Add(callImage);
                            }
                        }
                    }

                    if (StartFrequencySeparatorLabel.Content as string == "+/-")
                    {
                        StartFreqUpDown.Value = value.StartFrequency;
                        StartFreqVariationTextBox.Value = value.StartFrequencyVariation;
                    }
                    else
                    {
                        double mid = value.StartFrequency ?? 0.0;
                        double var = value.StartFrequencyVariation ?? 0.0;
                        StartFreqUpDown.Value = mid + var;
                        StartFreqVariationTextBox.Value = mid - var;
                    }

                    if (EndFrequencySeparatorLabel.Content as string == "+/-")
                    {
                        EndFreqTextBox.Value = value.EndFrequency;
                        EndFreqVariationTextBox.Value = value.EndFrequencyVariation;
                    }
                    else
                    {
                        double mid = value.EndFrequency ?? 0.0;
                        double var = value.EndFrequencyVariation ?? 0.0;
                        EndFreqTextBox.Value = mid + var;
                        EndFreqVariationTextBox.Value = mid - var;
                    }

                    if (PeakFrequencySeparatorLabel.Content as string == "+/-")
                    {
                        PeakFreqTextBox.Value = value.PeakFrequency;
                        PeakFreqVariationTextBox.Value = value.PeakFrequencyVariation;
                    }
                    else
                    {
                        double mid = value.PeakFrequency ?? 0.0;
                        double var = value.PeakFrequencyVariation ?? 0.0;
                        PeakFreqTextBox.Value = mid + var;
                        PeakFreqVariationTextBox.Value = mid - var;
                    }

                    if (DurationSeparatorLabel.Content as string == "+/-")
                    {
                        PulseDurationTextBox.Value = value.PulseDuration;
                        PulseDurationVariationTextBox.Value = value.PulseDurationVariation;
                    }
                    else
                    {
                        double mid = value.PulseDuration ?? 0.0;
                        double var = value.PulseDurationVariation ?? 0.0;
                        PulseDurationTextBox.Value = mid + var;
                        PulseDurationVariationTextBox.Value = mid - var;
                    }

                    if (IntervalSeparatorLabel.Content as string == "+/-")
                    {
                        PulseIntervalTextBox.Value = value.PulseInterval;
                        PulseIntervalVariationTextBox.Value = value.PulseIntervalVariation;
                    }
                    else
                    {
                        double mid = value.PulseInterval ?? 0.0;
                        double var = value.PulseIntervalVariation ?? 0.0;
                        PulseIntervalTextBox.Value = mid + var;
                        PulseIntervalVariationTextBox.Value = mid - var;
                    }

                    CallTypeNotesBox.Text = value.CallNotes;
                    CallTypeTextBox.Text = value.CallType;
                    CallFunctionTextBox.Text = value.CallFunction;
                }
                else
                {
                    SetReadOnly(true);
                }
            }
        }

        #endregion BatCall

#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member
        public readonly BulkObservableCollection<StoredImage> CallImageList = new BulkObservableCollection<StoredImage>();
#pragma warning restore CS1591 // Missing XML comment for publicly visible type or member

#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member

        public BatCallControl()
#pragma warning restore CS1591 // Missing XML comment for publicly visible type or member
        {
            InitializeComponent();
            this.DataContext = BatCall;
            SetReadOnly(true);
        }

        /// <summary>
        ///     Initializes a new instance of the <see cref="BatCallControl"/> class.
        /// </summary>
        /// <param name="isReadOnly">
        ///     if set to <c>true</c> [is read only].
        /// </param>
        public BatCallControl(bool isReadOnly)
        {
            if (BatCall == null)
            {
                BatCall = new Call();
            }
            InitializeComponent();
            this.DataContext = BatCall;
            SetReadOnly(isReadOnly);
        }

        /// <summary>
        ///     Sets the read only.
        /// </summary>
        /// <param name="isReadOnly">
        ///     if set to <c>true</c> [is read only].
        /// </param>
        public void SetReadOnly(bool isReadOnly)
        {
            StartFreqUpDown.IsReadOnly = isReadOnly;
            StartFreqVariationTextBox.IsReadOnly = isReadOnly;
            EndFreqTextBox.IsReadOnly = isReadOnly;
            EndFreqVariationTextBox.IsReadOnly = isReadOnly;
            PeakFreqTextBox.IsReadOnly = isReadOnly;
            PeakFreqVariationTextBox.IsReadOnly = isReadOnly;
            PulseDurationTextBox.IsReadOnly = isReadOnly;
            PulseDurationVariationTextBox.IsReadOnly = isReadOnly;
            PulseIntervalTextBox.IsReadOnly = isReadOnly;
            PulseIntervalVariationTextBox.IsReadOnly = isReadOnly;
            CallTypeTextBox.IsReadOnly = isReadOnly;
            CallFunctionTextBox.IsReadOnly = isReadOnly;
            CallTypeNotesBox.IsReadOnly = isReadOnly;
        }

        private Brush defaultBrush = Brushes.Cornsilk;

        private void ShowImageButton_Click(object sender, RoutedEventArgs e)
        {
            if (!(sender is Button)) return;
            Button button = sender as Button;
            if (button.Background != Brushes.Coral)
            {
                defaultBrush = button.Background;
                button.Background = Brushes.Coral;
            }
            else
            {
                button.Background = defaultBrush;
            }
            OnShowImageButtonPressed(new EventArgs());
        }

        /// <summary>
        /// toggles the state of the showimage button
        /// </summary>
        internal void Reset()
        {
            if (ShowImageButton.Background == Brushes.Coral)
            {
                ShowImageButton_Click(ShowImageButton, new RoutedEventArgs());
            }
        }

        private void StartFrequencySeparatorLabel_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (!(sender is Label)) return;
            Call call = BatCall;
            Label sl = sender as Label;
            if (sl.Content as String == "+/-")
            {
                sl.Content = " - ";
            }
            else
            {
                sl.Content = "+/-";
            }
            BatCall = call;
        }

        private readonly object ShowImageButtonPressedEventLock = new object();
        private EventHandler<EventArgs> ShowImageButtonPressedEvent;

        /// <summary>
        /// Event raised when the Image button is pressed to tell a parent class to display
        /// the list of images for this call in its own ImageScrollerControl.
        /// </summary>
        public event EventHandler<EventArgs> ShowImageButtonPressed
        {
            add
            {
                lock (ShowImageButtonPressedEventLock)
                {
                    ShowImageButtonPressedEvent += value;
                }
            }
            remove
            {
                lock (ShowImageButtonPressedEventLock)
                {
                    ShowImageButtonPressedEvent -= value;
                }
            }
        }

        /// <summary>
        /// Raises the <see cref="ShowImageButtonPressed" /> event.
        /// </summary>
        /// <param name="e"><see cref="EventArgs" /> object that provides the arguments for the event.</param>
        protected virtual void OnShowImageButtonPressed(EventArgs e)
        {
            EventHandler<EventArgs> handler = null;

            lock (ShowImageButtonPressedEventLock)
            {
                handler = ShowImageButtonPressedEvent;

                if (handler == null)
                    return;
            }

            handler(this, e);
        }
    }
}