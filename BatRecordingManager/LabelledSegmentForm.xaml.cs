using System.Windows;

namespace BatRecordingManager
{
    /// <summary>
    ///     Interaction logic for LabelledSegmentForm.xaml
    /// </summary>
    public partial class LabelledSegmentForm : Window
    {
        #region labelledSegment

        /// <summary>
        ///     labelledSegment Dependency Property
        /// </summary>
        public static readonly DependencyProperty labelledSegmentProperty =
            DependencyProperty.Register("labelledSegment", typeof(LabelledSegment), typeof(LabelledSegmentForm),
                new FrameworkPropertyMetadata((LabelledSegment)new LabelledSegment()));

        /// <summary>
        ///     Gets or sets the labelledSegment property. This dependency property indicates ....
        /// </summary>
        public LabelledSegment labelledSegment
        {
            get
            {
                LabelledSegment result = (LabelledSegment)GetValue(labelledSegmentProperty);
                result.StartOffset = Tools.ConvertDoubleToTimeSpan(StartOffsetDoubleUpDown.Value);
                result.EndOffset = Tools.ConvertDoubleToTimeSpan(EndOffsetDoubleUpDown.Value);
                result.Comment = CommentTextBox.Text;
                return (result);
            }
            set
            {
                SetValue(labelledSegmentProperty, value);
                StartOffsetDoubleUpDown.Value = value.StartOffset.TotalSeconds;
                EndOffsetDoubleUpDown.Value = value.EndOffset.TotalSeconds;
                CommentTextBox.Text = value.Comment;
            }
        }

        #endregion labelledSegment

        /// <summary>
        ///     Initializes a new instance of the <see cref="LabelledSegmentForm"/> class.
        /// </summary>
        public LabelledSegmentForm()
        {
            InitializeComponent();
        }

        private void OKButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = true;
            this.Close();
        }
    }
}