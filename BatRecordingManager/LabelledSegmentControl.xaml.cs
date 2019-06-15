using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;

namespace BatRecordingManager
{
    /// <summary>
    ///     Interaction logic for LabelledSegmentControl.xaml
    /// </summary>
    public partial class LabelledSegmentControl : UserControl
    {
        #region labelledSegment

        /// <summary>
        ///     labelledSegment Dependency Property
        /// </summary>
        public static readonly DependencyProperty labelledSegmentProperty =
            DependencyProperty.Register("labelledSegment", typeof(LabelledSegment), typeof(LabelledSegmentControl),
                new FrameworkPropertyMetadata((LabelledSegment)new LabelledSegment()));

        /// <summary>
        ///     Gets or sets the labelledSegment property. This dependency property indicates ....
        /// </summary>
        public LabelledSegment labelledSegment
        {
            get
            {
                return (LabelledSegment)GetValue(labelledSegmentProperty);
            }
            set
            {
                SetValue(labelledSegmentProperty, value);
                startTime = value.StartOffset;
                endTime = value.EndOffset;
                duration = endTime - startTime;
                comment = value.Comment;
                if (value.SegmentCalls != null && value.SegmentCalls.Count > 0)
                {
                    callParametersLabel.Visibility = Visibility.Hidden;
                }
                else
                {
                    callParametersLabel.Visibility = Visibility.Visible;
                }
            }
        }

        #endregion labelledSegment

        #region startTime

        /// <summary>
        ///     startTime Dependency Property
        /// </summary>
        public static readonly DependencyProperty startTimeProperty =
            DependencyProperty.Register("startTime", typeof(TimeSpan), typeof(LabelledSegmentControl),
                new FrameworkPropertyMetadata((TimeSpan)new TimeSpan()));

        /// <summary>
        ///     Gets or sets the startTime property. This dependency property indicates ....
        /// </summary>
        public TimeSpan startTime
        {
            get { return (TimeSpan)GetValue(startTimeProperty); }
            set { SetValue(startTimeProperty, value); }
        }

        #endregion startTime

        #region endTime

        /// <summary>
        ///     endTime Dependency Property
        /// </summary>
        public static readonly DependencyProperty endTimeProperty =
            DependencyProperty.Register("endTime", typeof(TimeSpan), typeof(LabelledSegmentControl),
                new FrameworkPropertyMetadata((TimeSpan)new TimeSpan()));

        /// <summary>
        ///     Gets or sets the endTime property. This dependency property indicates ....
        /// </summary>
        public TimeSpan endTime
        {
            get { return (TimeSpan)GetValue(endTimeProperty); }
            set { SetValue(endTimeProperty, value); }
        }

        #endregion endTime

        #region duration

        /// <summary>
        ///     duration Dependency Property
        /// </summary>
        public static readonly DependencyProperty durationProperty =
            DependencyProperty.Register("duration", typeof(TimeSpan), typeof(LabelledSegmentControl),
                new FrameworkPropertyMetadata((TimeSpan)new TimeSpan()));

        /// <summary>
        ///     Gets or sets the duration property. This dependency property indicates ....
        /// </summary>
        public TimeSpan duration
        {
            get { return (TimeSpan)GetValue(durationProperty); }
            set { SetValue(durationProperty, value); }
        }

        #endregion duration

        #region comment

        /// <summary>
        ///     comment Dependency Property
        /// </summary>
        public static readonly DependencyProperty commentProperty =
            DependencyProperty.Register("comment", typeof(String), typeof(LabelledSegmentControl),
                new FrameworkPropertyMetadata((String)""));

        /// <summary>
        ///     Gets or sets the comment property. This dependency property indicates ....
        /// </summary>
        public String comment
        {
            get { return (String)GetValue(commentProperty); }
            set { SetValue(commentProperty, value); }
        }

        #endregion comment

        /// <summary>
        ///     Initializes a new instance of the <see cref="LabelledSegmentControl"/> class.
        /// </summary>
        public LabelledSegmentControl()
        {
            InitializeComponent();
            this.DataContext = this;
        }
    }

    #region TimeSpanConverter (ValueConverter)

    /// <summary>
    ///     Converter class for displaying a timespan object as a string
    /// </summary>
    public class TimeSpanConverter : IValueConverter
    {
        /// <summary>
        ///     Converts a Timespan object into a formatted string
        /// </summary>
        /// <param name="value">
        ///     The value.
        /// </param>
        /// <param name="targetType">
        ///     Type of the target.
        /// </param>
        /// <param name="parameter">
        ///     The parameter.
        /// </param>
        /// <param name="culture">
        ///     The culture.
        /// </param>
        /// <returns>
        ///     </returns>
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            try
            {
                // Here's where you put the code do handle the value conversion.
                TimeSpan? ts = value as TimeSpan?;
                if (ts == null) return ("");
                String result = Tools.FormattedTimeSpan(ts.Value);

                return (result);
            }
            catch
            {
                return value;
            }
        }

        /// <summary>
        ///     Converts the back.
        /// </summary>
        /// <param name="value">
        ///     The value.
        /// </param>
        /// <param name="targetType">
        ///     Type of the target.
        /// </param>
        /// <param name="parameter">
        ///     The parameter.
        /// </param>
        /// <param name="culture">
        ///     The culture.
        /// </param>
        /// <returns>
        ///     </returns>
        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            // Not implemented
            return null;
        }
    }

    #endregion TimeSpanConverter (ValueConverter)

    #region CallParametersConverter (ValueConverter)

#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member

    public class CallParametersConverter : IValueConverter
#pragma warning restore CS1591 // Missing XML comment for publicly visible type or member
    {
#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member

        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
#pragma warning restore CS1591 // Missing XML comment for publicly visible type or member
        {
            try
            {
                // Here's where you put the code do handle the value conversion.
                string summary = "";
                LabelledSegment segment = value as LabelledSegment;
                if (segment.SegmentCalls != null && segment.SegmentCalls.Count > 0)
                {
                    foreach (var call in segment.SegmentCalls)
                    {
                        if (!string.IsNullOrWhiteSpace(summary))
                        {
                            summary = summary + @"
";
                        }
                        else
                        {
                            summary = "";
                        }
                        summary = summary + string.Format("{0,5:##0.0},{1,5:##0.0},{2,5:##0.0}kHz {3,5:##0.0},{4,5:##0.0}mS",
                            call.Call.StartFrequency, call.Call.EndFrequency, call.Call.PeakFrequency,
                            call.Call.PulseDuration, call.Call.PulseInterval);
                    }
                }
                return (summary);
            }
            catch
            {
                return value;
            }
        }

#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
#pragma warning restore CS1591 // Missing XML comment for publicly visible type or member
        {
            // Not implemented
            return null;
        }
    }

    #endregion CallParametersConverter (ValueConverter)
}