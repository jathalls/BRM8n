using System.Windows;

namespace BatRecordingManager
{
    /// <summary>
    /// Interaction logic for AboutScreen.xaml
    /// </summary>
    public partial class AboutScreen : Window
    {
        #region AssemblyVersion

        /// <summary>
        /// AssemblyVersion Dependency Property
        /// </summary>
        public static readonly DependencyProperty AssemblyVersionProperty =
            DependencyProperty.Register("AssemblyVersion", typeof(string), typeof(AboutScreen),
                new FrameworkPropertyMetadata((string)""));

        /// <summary>
        /// Gets or sets the AssemblyVersion property.  This dependency property
        /// indicates ....
        /// </summary>
        public string AssemblyVersion
        {
            get { return (string)GetValue(AssemblyVersionProperty); }
            set { SetValue(AssemblyVersionProperty, value); }
        }

        #endregion AssemblyVersion

        /// <summary>
        /// Initializes a new instance of the <see cref="AboutScreen"/> class.
        /// </summary>
        public AboutScreen()
        {
            var Build = System.Reflection.Assembly.GetExecutingAssembly().GetName().Version.ToString();
            AssemblyVersion = "Build " + Build;
            InitializeComponent();
            DataContext = this;
            version.Content = "v 6.2 (" + Build + ")";
            dbVer.Content = "    Database Version " + DBAccess.GetDatabaseVersion() + " named:- " + DBAccess.GetWorkingDatabaseName(DBAccess.GetWorkingDatabaseLocation());
        }
    }
}