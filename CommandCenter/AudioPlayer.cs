//using Ownaudio;
//using Ownaudio.Engines;
//using Ownaudio.Sources;
using NAudio;
using NAudio.Wasapi;
using NAudio.FileFormats;
using NAudio.CoreAudioApi;
using NAudio.Codecs;
using NAudio.Dmo;
using NAudio.Dsp;
using NAudio.MediaFoundation;
using NAudio.Mixer;
using NAudio.Wave;

namespace CommandCenter
{
    public class AudioPlayer
    {
        //public SourceManager Player { get; set; }

        public AudioPlayer()//int frameBuffer = 1024)
        {
            //OwnAudio.Initialize("C:\\ffmpeg\\bin", OwnAudioEngine.EngineHostType.ASIO);
            //Player = SourceManager.Instance;
            //SourceManager.OutputEngineOptions = new AudioEngineOutputOptions
            //    (
            //        device: OwnAudio.DefaultOutputDevice,
            //        channels: OwnAudioEngine.EngineChannels.Stereo,
            //        sampleRate: OwnAudio.DefaultOutputDevice.DefaultSampleRate,
            //        latency: OwnAudio.DefaultOutputDevice.DefaultHighOutputLatency
            //    );
            //
            //SourceManager.InputEngineOptions = new AudioEngineInputOptions
            //(
            //    device: OwnAudio.DefaultInputDevice,
            //    channels: OwnAudioEngine.EngineChannels.Mono,
            //    sampleRate: OwnAudio.DefaultInputDevice.DefaultSampleRate,
            //    latency: OwnAudio.DefaultInputDevice.DefaultLowInputLatency
            //);
            //
            //SourceManager.EngineFramesPerBuffer = frameBuffer;            
            //Player.Volume = 100;
        }

        public async Task Play(string file)
        {
            using (var audioFile = new AudioFileReader(file))
            using (var outputDevice = new WaveOutEvent())
            {
                outputDevice.Init(audioFile);
                outputDevice.Play();
                while (outputDevice.PlaybackState == PlaybackState.Playing)
                {
                    await Task.Delay(1000);
                }
            }
        }
    }
}
