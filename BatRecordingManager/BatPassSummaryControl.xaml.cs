using System;
using System.Windows.Controls;

namespace BatRecordingManager
{
    /// <summary>
    ///     Interaction logic for BatPassSummaryControl.xaml
    /// </summary>
    public partial class BatPassSummaryControl : UserControl
    {
        /// <summary>
        ///     Initializes a new instance of the <see cref="BatPassSummaryControl"/> class.
        /// </summary>
        public BatPassSummaryControl()
        {
            InitializeComponent();
        }

        /// <summary>
        ///     Gets or sets the PassSummary property. This dependency property indicates ....
        /// </summary>
        public BatStats PassSummary
        {
            get
            {
                return (_PassSummary);
            }
            set
            {
                _PassSummary = value;

                SummaryStackpanel.Children.Clear();

                String statstring = Tools.GetFormattedBatStats(value, false);
                Label statLabel = new Label();
                statLabel.Content = statstring;
                SummaryStackpanel.Children.Add(statLabel);
                InvalidateArrange();
                UpdateLayout();
            }
        }

        private BatStats _PassSummary;
    }
}