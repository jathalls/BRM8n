using Microsoft.VisualStudio.Language.Intellisense;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

using System.IO;
using System.Threading;

namespace BatRecordingManager
{
    /// <summary>
    /// Interaction logic for AudioPlayerIK.xaml
    /// </summary>
    public partial class AudioPlayer : Window
    {
        #region 

        /// <summary>
        /// Frequency Dependency Property
        /// </summary>
        public static readonly DependencyProperty FrequencyProperty =
            DependencyProperty.Register("Frequency", typeof(double), typeof(AudioPlayer),
                new FrameworkPropertyMetadata((double)50.0d));

        /// <summary>
        /// Gets or sets the Frequency property.  This dependency property 
        /// indicates ....
        /// </summary>
        public double Frequency
        {
            get { return (double)GetValue(FrequencyProperty); }
            set
            {
                if (wrapper != null)
                {
                    wrapper.Frequency = (decimal)value;
                }
                SetValue(FrequencyProperty, value);
            }
        }

        #endregion



        /// <summary>
        /// List of items that can be played if selected
        /// </summary>
        public BulkObservableCollection<PlayListItem> PlayList { get; set; } = new BulkObservableCollection<PlayListItem>();
        private NaudioWrapper wrapper;
        /// <summary>
        /// Constructor for the AudioPlayer
        /// </summary>
        public AudioPlayer()
        {
            InitializeComponent();
            DataContext = this;
            
            Closing += AudioPlayer_Closing;
            SetButtonVisibility();
            //PlayListItem pli = PlayListItem.Create(@"X:\BatRecordings\2018\Knebworth-KNB18-2_20180816\KNB18-2p_20180816\KNB18-2p_20180816_212555.wav", TimeSpan.FromSeconds(218), TimeSpan.FromSeconds(8), "Comment line");
            //PlayList.Add(pli);
        }

        /// <summary>
        /// Adds a specific labelled segment to the playlist
        /// </summary>
        /// <param name="segmentToAdd"></param>
        public int AddToList(LabelledSegment segmentToAdd)
        {
            if (PlayList == null) PlayList = new BulkObservableCollection<PlayListItem>();
            string filename = segmentToAdd.Recording.GetFileName();
            if (string.IsNullOrWhiteSpace(filename))
            {
                MessageBox.Show("No file found on this computer for this segment");
                return (PlayList.Count);
            }
            TimeSpan start = segmentToAdd.StartOffset;
            TimeSpan duration = segmentToAdd.Duration()??new TimeSpan();
            string comment = segmentToAdd.Comment;
            PlayListItem pli = PlayListItem.Create(filename, start, duration, comment);
            AddToPlayList(pli);

            
            
            return (PlayList.Count);

        }

        private void SetButtonVisibility()
        {
            if(PlayList==null || PlayList.Count <= 0)
            {
                PlayButton.IsEnabled = false;
                PlayLoopedButton.IsEnabled = false;
            }
            else 
            {
                PlayButton.IsEnabled = true;
                PlayLoopedButton.IsEnabled = true;
            }
        }

        /// <summary>
        /// event handler triggered when the window is closing
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void AudioPlayer_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            if (wrapper != null)
            {
                wrapper.Dispose();
                int i = 0;
                while (wrapper!=null && wrapper.playBackState != NAudio.Wave.PlaybackState.Stopped)
                {
                    Thread.Sleep(100);
                    if (i++ > 10)
                    {
                        wrapper = null;
                    }
                }
            }
        }

        /// <summary>
        /// constructs a playlistitem and adds it to the playlist
        /// </summary>
        /// <param name="filename"></param>
        /// <param name="start"></param>
        /// <param name="duration"></param>
        /// <param name="label"></param>
        /// <returns></returns>
        public bool AddToPlayList(string filename, TimeSpan start, TimeSpan duration, string label)
        {
            PlayListItem pli=PlayListItem.Create(filename, start, duration, label);
            if (pli != null)
            {
                return(AddToPlayList(pli));
            }
            return (false);
        }

        /// <summary>
        /// Adds a pre-constructed playlistitem to the playlist
        /// </summary>
        /// <param name="pli"></param>
        /// <returns></returns>
        public bool AddToPlayList(PlayListItem pli)
        {
            if (pli == null) return (false);
            PlayList.Add(pli);
            SetButtonVisibility();
            return (true);
        }

        private void PlayButton_Click(object sender, RoutedEventArgs e)
        {
            bool looped = false;
            if((sender as Button).Content as String == "LOOP")
            {
                looped = true;
            }
            if ((PlayButton.Content as string) == "PLAY")
            {
                
                PlayListItem itemToPlay = null;
                if (!PlayList.IsNullOrEmpty())
                {
                    if (PlayListDatagrid.SelectedItem != null)
                    {
                        itemToPlay = PlayListDatagrid.SelectedItem as PlayListItem;
                    }
                    else
                    {
                        itemToPlay = PlayList.First();
                    }
                }
                if (itemToPlay != null)
                {
                    PlayItem(itemToPlay,looped);
                    PlayButton.Content = "STOP";
                }
            }
            else
            {
                if (wrapper != null)
                {
                    wrapper.Stop();
                    if (wrapper.playBackState == NAudio.Wave.PlaybackState.Stopped)
                    {
                        wrapper.Dispose();
                        wrapper = null;
                        PlayButton.Content = "PLAY";
                    }
                }
            }
        }

        private void PlayItem(PlayListItem itemToPlay,bool playLooped)
        {
            wrapper = new NaudioWrapper();
            wrapper.Frequency = (decimal)Frequency;
            wrapper.Stopped += Wrapper_Stopped;
            if (!TunedButton.IsChecked ?? false)
            {
                decimal rate = 1.0m;

                if (tenthButton.IsChecked ?? false) rate = 0.1m;
                if (fifthButton.IsChecked ?? false) rate = 0.2m;
                if (twentiethButton.IsChecked ?? false) rate = 0.05m;
                wrapper.play(itemToPlay, rate,playLooped);
            }
            else
            {
                wrapper.Heterodyne(itemToPlay,@"X:\test.wav");
            }
        }

        private void Wrapper_Stopped(object sender, EventArgs e)
        {
            if (wrapper != null)
            {
                wrapper.Dispose();
                wrapper = null;
            }
            PlayButton.Content = "PLAY";
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            if (wrapper != null)
            {
                wrapper.Dispose();
                wrapper = null;
            }
            PlayButton.Content = "PLAY";
            this.Close();
        }

        private void FrequencySlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (wrapper != null)
            {
                wrapper.Frequency = (decimal)(sender as Slider).Value;
            }
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            if (wrapper != null)
            {
                wrapper.Dispose();
                wrapper = null;
            }
            if (PlayList != null)
            {
                PlayList.Clear();
            }
            ShowInTaskbar = true;
            WindowState = WindowState.Minimized;
            PlayButton.Content = "PLAY";
            e.Cancel = true;
        }

        internal void Stop()
        {
            if (wrapper != null)
            {
                wrapper.Stop();
            }
        }

        
    }

    /// <summary>
    /// A class to hold items to be displayed in the AudioPlayer playlist
    /// </summary>
    public class PlayListItem
    {
        /// <summary>
        /// fully qualified name of the source .wav file
        /// </summary>
        public string filename { get; set; }
        /// <summary>
        /// offset in the file for the start of the region to be played
        /// </summary>
        public TimeSpan startOffset { get; set; }
        /// <summary>
        /// duration of the segment to be played
        /// </summary>
        public TimeSpan playLength { get; set; }
        /// <summary>
        /// label of the original labelled segment or other comment for the playlist display
        /// </summary>
        public string label { get; set; }

        /// <summary>
        /// Constructor for playlist elements
        /// </summary>
        /// <param name="filename"></param>
        /// <param name="start"></param>
        /// <param name="duration"></param>
        /// <param name="label"></param>
        public static PlayListItem Create(string filename,TimeSpan start,TimeSpan duration,string label)
        {
            if (string.IsNullOrWhiteSpace(filename)) return (null);
            if (!File.Exists(filename)) return (null);
            PlayListItem result = new PlayListItem();

            result.filename = filename;
           
            result.startOffset = start;
            result.playLength = duration;
            result.label = label;
            return (result);
        }
    }
}
