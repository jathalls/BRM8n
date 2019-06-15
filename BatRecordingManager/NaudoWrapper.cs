using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NAudio.Dsp;
using NAudio.Utils;
using NAudio.Wave;
using NAudio.Wave.SampleProviders;

namespace BatRecordingManager
{
    /// <summary>
    /// A class to hold all interface functions between the application and the Naudio system
    /// </summary>
    public class NaudioWrapper : IDisposable
    {
        private AudioFileReader wave = null;
        private WaveOut player = null;
        private WaveFileReader reader = null;
        private MemoryStream ms = null;
        private bool doLoop = false;
        private WaveFormatConversionProvider converter = null;
        private PlayListItem currentItem { get; set; } = null;
        private decimal currentSpeed { get; set; } = 0.0m;
        private MediaFoundationResampler resampler = null;

        public decimal Frequency { get; set; } = 50.0m;

        public PlaybackState playBackState
        {
            get
            {
                if (player != null)
                {
                    return (player.PlaybackState);
                }
                else
                {
                    return (PlaybackState.Stopped);
                }
            }
        }

        private void CleanUp()
        {
            if (player != null)
            {
                player.Dispose();
                player = null;

            }
            if (reader != null && !doLoop)
            {
                reader.Dispose();
                reader = null;
            }
            if (ms != null && !doLoop)
            {
                ms.Dispose();
                ms = null;
            }
            if (converter != null)
            {
                converter.Dispose();
                converter = null;
            }
            if (wave != null)
            {
                wave.Dispose();
                wave = null;
            }
            if (resampler != null)
            {
                resampler.Dispose();
                resampler = null;
            }
        }

        /// <summary>
        /// Wrapper class to encapsulate a the Audio interface using the Naudio API
        /// </summary>
        public NaudioWrapper()
        {

        }

        public void Loop(PlayListItem itemToPlay,decimal speedFactor)
        {
            
            play(itemToPlay, speedFactor,true);
        }

        /// <summary>
        /// Plays an item in heterodyned format.
        /// If in debug mode and filename is specified also saves a copy of the output to a file.
        /// </summary>
        /// <param name="itemToPlay"></param>
        /// <param name="fileName"></param>
        public void Heterodyne(PlayListItem itemToPlay, String fileName = "")
        {

            CleanUp();
            currentItem = itemToPlay;
            currentSpeed = 0.0m;
            doLoop = true;
            GetSampleReader(itemToPlay, 1.0m, true);
            if (reader == null)
            {
                OnStopped(new EventArgs());
                return;
            }
            reader.Position = 0;

            var outFormat = new WaveFormat(21000, reader.WaveFormat.Channels);
            resampler = new MediaFoundationResampler(reader, outFormat);






#if DEBUG
            if (!String.IsNullOrWhiteSpace(fileName))
            {
                WaveFileWriter.CreateWaveFile(fileName, resampler);
                
                
            }
#endif
            reader.Position = 0;
            resampler.Reposition();
            
            player = new WaveOut();
            if (player == null)
            {
                CleanUp();
                OnStopped(new EventArgs());
                return;
            }
            player.PlaybackStopped += Player_PlaybackStopped;

            //reader = new WaveFileReader(converter);
            //player.Init(converter);
            player.Init(resampler);
            player.Play();


        }

        public void play(PlayListItem itemToPlay,decimal speedFactor,bool playInLoop)
        {
            CleanUp();
            doLoop = playInLoop;
            currentItem = itemToPlay;
            currentSpeed = speedFactor;
            GetSampleReader(itemToPlay, speedFactor);
            if (reader == null)
            {
                CleanUp();
                OnStopped(new EventArgs());
                return;
            }
            player = new WaveOut();
            if (player == null)
            {
                CleanUp();
                OnStopped(new EventArgs());
                return;
            }
            player.PlaybackStopped += Player_PlaybackStopped;
            player.Init(reader);
            player.Play();

            

        }

        private void GetSampleReader(PlayListItem itemToPlay, decimal speedFactor,bool doHeterodyne=false)
        {
            if(reader!=null && ms != null && doLoop && !doHeterodyne)
            {
                ms.Position = 0;
                return;
            }
            if (player != null && player.PlaybackState != PlaybackState.Stopped) return;
            if (itemToPlay == null) return;
            if (string.IsNullOrWhiteSpace(itemToPlay.filename)) return;
            if (!File.Exists(itemToPlay.filename)) return;


            using (var afr = new AudioFileReader(itemToPlay.filename))
            {

                if (afr != null)
                {
                    afr.Skip((int)itemToPlay.startOffset.TotalSeconds);


                    if (itemToPlay.playLength.TotalSeconds < 1)
                    {
                        if (itemToPlay.playLength.TotalMilliseconds > 0)
                        {
                            itemToPlay.playLength = new TimeSpan(0, 0, 1);
                        }
                        else
                        {
                            itemToPlay.playLength = afr.TotalTime - itemToPlay.startOffset;
                        }
                    }
                    var takenb = afr.Take(itemToPlay.playLength);
                    int bufferLength = 0;
                    if (doHeterodyne)
                    {
                        bufferLength = takenb.WaveFormat.SampleRate;
                    }
                    float[] sineBuffer = new float[bufferLength];
                    sineBuffer = Fill(sineBuffer,Frequency*1000);
                    
                    if (takenb != null)
                    {
                        //afr.Dispose();
                        WaveFormat wf = new WaveFormat((int)(takenb.WaveFormat.SampleRate * speedFactor), takenb.WaveFormat.BitsPerSample, takenb.WaveFormat.Channels);
                        wf = WaveFormat.CreateIeeeFloatWaveFormat((int)(takenb.WaveFormat.SampleRate * speedFactor), takenb.WaveFormat.Channels);
                        ms = new MemoryStream();
                        if (wf != null && ms != null)
                        {

                            using (WaveFileWriter wfw = new WaveFileWriter(new IgnoreDisposeStream(ms), wf))
                            {
                                if (wfw != null)
                                {

                                    //byte[] bytes = new byte[takenb.WaveFormat.AverageBytesPerSecond];
                                    float[] floats = new float[takenb.WaveFormat.SampleRate];
                                    int read = -1;
                                    var filter = BiQuadFilter.LowPassFilter(wfw.WaveFormat.SampleRate, 5000, 2.0f);
                                    // 6s @ 384ksps = 2,304,000 = 4,608,000 bytes
                                    while ((read = takenb.Read(floats, 0, floats.Length)) > 0)
                                    {
                                        if (doHeterodyne)
                                        {
                                            for(int i = 0; i < read ; i++)
                                            {
                                                floats[i] = floats[i] * sineBuffer[i];

                                                floats[i] = filter.Transform(floats[i]);
                                            }


                                            wfw.WriteSamples(floats, 0, read);
                                        }
                                        else
                                        {
                                            wfw.WriteSamples(floats, 0, read);
                                        }
                                        
                                        
                                        wfw.Flush();
                                    }



                                    wfw.Flush();



                                    afr.Dispose();
                                }
                            }
                        }
                    }
                }
            }
            if (ms != null)
            {
                ms.Position = 0;
                //player = new WaveOut();
                //player.PlaybackStopped += Player_PlaybackStopped;
                //if (doHeterodyne)
                //{
                //    MyBiQuadFilter filter = new MyBiQuadFilter(ms);
                //    filter.setValues(5000);
                //    reader = filter;
                //}
                //else
                //{

                    reader = new WaveFileReader(ms);
                //}
            }

        }

        /// <summary>
        /// Fills an array of floats with sinewaves at the specified frequency
        /// </summary>
        /// <param name="sineBuffer"></param>
        /// <param name="frequency"></param>
        /// <returns></returns>
        private float[] Fill(float[] sineBuffer, decimal frequency)
        {
            
            for(int i = 0; i < sineBuffer.Length; i++)
            {
                sineBuffer[i] = (float)Math.Sin((2.0d * Math.PI * (double)i * (double)frequency) / (double)sineBuffer.Length);
            }
            return (sineBuffer);
        }

        /// <summary>
        /// stops the player if it is playing
        /// </summary>
        /// <returns></returns>
        public bool Stop()
        {
            doLoop = false;
            if(player!=null && player.PlaybackState != PlaybackState.Stopped)
            {
                player.Stop();
                return (true);
            }
            else
            {
                return (false);
            }
        }

        

        private void Player_PlaybackStopped(object sender, StoppedEventArgs e)
        {
            CleanUp();
            if (doLoop)
            {
                if (currentSpeed == 0.0m)
                {
                    
                    Heterodyne(currentItem);
                }
                else
                {
                    play(currentItem, currentSpeed,doLoop);
                }
            }
            else
            {
                if (reader != null)
                {
                    reader.Dispose();
                    reader = null;
                }
                if (player != null)
                {
                    player.Dispose();
                    player = null;
                }
                if (ms != null)
                {
                    ms.Dispose();
                    ms = null;
                }

                OnStopped(new EventArgs());
            }
            
        }

        private bool isDisposing = false;
        /// <summary>
        /// Tidy up before disposal
        /// </summary>
        public void Dispose()
        {
            if (!this.isDisposing)
            {
                doLoop = false;
                isDisposing = true;
                if (player != null && player.PlaybackState == PlaybackState.Playing)
                {
                    if (!Stop())
                    {
                        player.Dispose();
                        player = null;
                    }
                }
                else
                {
                    if (reader != null)
                    {
                        reader.Dispose();
                        reader = null;
                    }
                    if (ms != null)
                    {
                        ms.Dispose();
                        ms = null;
                    }
                }
                CleanUp();
            }
           
        }

        /////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        ///
        private readonly object StoppedEventLock = new object();
        private EventHandler StoppedEvent;

        /// <summary>
        /// Event raised after the <see cref="Text" /> property value has changed.
        /// </summary>
        public event EventHandler Stopped
        {
            add
            {
                lock (StoppedEventLock)
                {
                    StoppedEvent += value;
                }
            }
            remove
            {
                lock (StoppedEventLock)
                {
                    StoppedEvent -= value;
                }
            }
        }

        /// <summary>
        /// Raises the <see cref="Stopped" /> event.
        /// </summary>
        /// <param name="e"><see cref="EventArgs" /> object that provides the arguments for the event.</param>
        protected virtual void OnStopped(EventArgs e)
        {
            EventHandler handler = null;

            lock (StoppedEventLock)
            {
                handler = StoppedEvent;

                if (handler == null)
                    return;
            }

            handler(this, e);
        }
    }

    public class MyBiQuadFilter : WaveFileReader
    {
        
        private int cutOffFreq = 0;
        private string fileName = "";
        private int channels;
        private BiQuadFilter[] filters;
        private Stream inputStream;


        /// <summary>
        /// An implementation of a BiQuad low pass filter
        /// </summary>
        public MyBiQuadFilter(Stream inputStream) : base(inputStream)
        {
            this.inputStream = inputStream;
        }

        public MyBiQuadFilter(String fileName) : base(fileName) {
            this.fileName = fileName;
        }

        public void setValues(int cutOffFreq)
        {
            
            this.cutOffFreq = cutOffFreq;

            filter_LowPass();
        }

        private void filter_LowPass()
        {
            channels = base.WaveFormat.Channels;
            filters = new BiQuadFilter[channels];

            for (int n = 0; n < channels; n++)
                if (filters[n] == null)
                    filters[n] = BiQuadFilter.LowPassFilter(WaveFormat.SampleRate, cutOffFreq, 1);
                else
                    filters[n].SetLowPassFilter(WaveFormat.SampleRate, cutOffFreq, 1);
        }

        

        public new int Read(float[] buffer, int offset, int count)
        {
            var sampleProvider = this.ToSampleProvider();
            int samplesRead = sampleProvider.Read(buffer, offset, count);

            for (int i = 0; i < samplesRead; i++)
                buffer[offset + i] = filters[(i % channels)].Transform(buffer[offset + i]);

            return samplesRead;
        }
    }
}
