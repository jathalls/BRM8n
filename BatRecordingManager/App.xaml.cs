using System.Windows;

namespace BatRecordingManager
{
    /// <summary>
    ///     Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        private static string _dbFileLocation;

        private static string _dbFileName;

        private static bool _showDatabase = false;

        public static bool ShowDatabase
        {
            get
            {
                return (_showDatabase);
            }

            set
            {
                _showDatabase = value;
            }
        }

#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member

        public static string dbFileLocation
#pragma warning restore CS1591 // Missing XML comment for publicly visible type or member
        {
            get
            {
                return (_dbFileLocation);
            }
            set
            {
                _dbFileLocation = value;
            }
        }

#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member

        public static string dbFileName
#pragma warning restore CS1591 // Missing XML comment for publicly visible type or member
        {
            get
            {
                return (_dbFileName);
            }
            set
            {
                _dbFileName = value;
            }
        }

        protected override void OnStartup(StartupEventArgs e)
        {
            foreach(string arg in e.Args)
            {
                if (arg.Contains("debug"))
                {
                    ShowDatabase = true;
                }

                


            }
#if DEBUG
            ShowDatabase = true;
#endif
            foreach(var arg in e.Args)
            {
                if (arg.Contains("nodebug") || arg.Contains("undebug"))
                {
                    ShowDatabase = false;
                }
            }
            base.OnStartup(e);
        }
    }
}