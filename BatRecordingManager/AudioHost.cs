using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BatRecordingManager
{
    /// <summary>
    /// Host class for the AudioPlayer window
    /// </summary>
    public sealed class AudioHost
    {
        private static readonly AudioHost audioHostInstance = new AudioHost();

        private AudioPlayer _audioPlayer=null;

        /// <summary>
        /// public accessor to the AudioPlayer for this inatsance
        /// </summary>
        public AudioPlayer audioPlayer
        {
            get
            {
                if (_audioPlayer == null)
                {
                    _audioPlayer = new AudioPlayer();
                    _audioPlayer.Show();
                }
                if (_audioPlayer.WindowState == System.Windows.WindowState.Minimized)
                {
                    _audioPlayer.WindowState = System.Windows.WindowState.Normal;
                    
                }
                return (_audioPlayer);
            }
        }

        static AudioHost() { }

        private AudioHost() { }

        /// <summary>
        /// returns an instance of the AudioHost holding an AudioPlayer
        /// </summary>
        public static AudioHost Instance
        {
            get
            {
                return audioHostInstance;
            }
        }

        internal void Close()
        {
            if (_audioPlayer != null)
            {
                _audioPlayer.Close();
                _audioPlayer = null;
            }
        }
    }
}
