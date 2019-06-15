using CSCore;
using CSCore.Codecs;
using CSCore.CoreAudioAPI;
using CSCore.SoundOut;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BatRecordingManager
{
    /// <summary>
    /// a class to carry the implementation od CScore audio components
    /// </summary>
    class CscoreWrapper
    {
        private ISoundOut _soundOut;
        private IWaveSource _waveSource;

        public event EventHandler<PlaybackStoppedEventArgs> PlaybackStopped;
        private MMDevice device = null;


        public CscoreWrapper()
        {
            using (var mmdeviceEnumerator = new MMDeviceEnumerator())
            {
                using (var mmdeviceCollection = mmdeviceEnumerator.EnumAudioEndpoints(DataFlow.Render, DeviceState.Active))
                {
                    foreach(var dev in mmdeviceCollection)
                    {
                        Debug.WriteLine(dev.DeviceID + ":-" + dev.FriendlyName);
                        if (device == null) device = dev;
                    }
                }
            }
        }

        public void play(PlayListItem itemToPlay,double speed)
        {
            if (itemToPlay == null) return;
            if (string.IsNullOrWhiteSpace(itemToPlay.filename)) return;
            if (!File.Exists(itemToPlay.filename)) return;

            Open(itemToPlay, device);
            _waveSource.SetPosition(itemToPlay.startOffset);
            _waveSource.ChangeSampleRate(_waveSource.WaveFormat.SampleRate / 10);
            TimeSpan end = itemToPlay.startOffset + itemToPlay.playLength;
            Debug.WriteLine("Play from " + itemToPlay.startOffset.ToString() + " to " + end.ToString());
            Debug.WriteLine("Starting at:-" + DateTime.Now.TimeOfDay.ToString());

            _soundOut = new WasapiOut() { Latency = 100, Device = device };
            _soundOut.Initialize(_waveSource);
             if (PlaybackStopped != null) _soundOut.Stopped += PlaybackStopped;
            Play();
            while (PlaybackState != PlaybackState.Stopped)
            {
                TimeSpan pos = Position;
                if (pos > end)
                {
                    Stop();
                    Debug.WriteLine("Stopped at end:-" + DateTime.Now.TimeOfDay.ToString()+" position="+pos.ToString());
                }
            }
            Debug.WriteLine("Play ended:-" + DateTime.Now.TimeOfDay.ToString());

        }

        public PlaybackState PlaybackState
        {
            get
            {
                if (_soundOut != null)
                    return _soundOut.PlaybackState;
                return PlaybackState.Stopped;
            }
        }

        public TimeSpan Position
        {
            get
            {
                if (_waveSource != null)
                    return _waveSource.GetPosition();
                return TimeSpan.Zero;
            }
            set
            {
                if (_waveSource != null)
                    _waveSource.SetPosition(value);
            }
        }

        public TimeSpan Length
        {
            get
            {
                if (_waveSource != null)
                    return _waveSource.GetLength();
                return TimeSpan.Zero;
            }
        }

        public int Volume
        {
            get
            {
                if (_soundOut != null)
                    return Math.Min(100, Math.Max((int)(_soundOut.Volume * 100), 0));
                return 100;
            }
            set
            {
                if (_soundOut != null)
                {
                    _soundOut.Volume = Math.Min(1.0f, Math.Max(value / 100f, 0f));
                }
            }
        }

        public void Open(PlayListItem itemToPlay, MMDevice device)
        {
            CleanupPlayback();

            var codec = CodecFactory.Instance.GetCodec(itemToPlay.filename);
            var source = codec.ToSampleSource();
            
            var mono = source.ToMono();
            _waveSource = mono.ToWaveSource();
            using (MemoryStream ms = new MemoryStream())
            {
                _waveSource.WriteToStream(ms);
                WaveFormat wf = _waveSource.WaveFormat;
                CSCore.SoundIn.WaveIn wi = new CSCore.SoundIn.WaveIn(new WaveFormat(wf.SampleRate / 10, wf.BitsPerSample, wf.Channels));
                
                

            }
            

            

            
            //_soundOut = new WasapiOut() { Latency = 100, Device = device };
           // _soundOut.Initialize(_waveSource);
           // if (PlaybackStopped != null) _soundOut.Stopped += PlaybackStopped;
        }

        public void Play()
        {
            if (_soundOut != null)
                _soundOut.Play();
        }

        public void Pause()
        {
            if (_soundOut != null)
                _soundOut.Pause();
        }

        public void Stop()
        {
            if (_soundOut != null)
                _soundOut.Stop();
        }

        private void CleanupPlayback()
        {
            if (_soundOut != null)
            {
                _soundOut.Dispose();
                _soundOut = null;
            }
            if (_waveSource != null)
            {
                _waveSource.Dispose();
                _waveSource = null;
            }
        }

        protected void Dispose(bool disposing)
        {
            //base.Dispose(disposing);
            CleanupPlayback();
        }
    }
}
