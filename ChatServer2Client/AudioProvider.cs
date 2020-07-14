using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Collections.Concurrent;
using System.Text;
using System.Linq;
using NAudio.Wave;
using NAudio.Wave.SampleProviders;
using Concentus.Structs;
using Concentus.Enums;
using System.Threading.Tasks;

namespace ChatServer2Client
{
	public class AudioProvider
	{
		#region OutFields
		private readonly SampleToWaveProvider16 _provider;
		private readonly MixingSampleProvider _mixer;
		private readonly WaveFormat _format;
		private readonly WaveOutEvent _out;
		private readonly ConcurrentDictionary<int, BufferedWaveProvider> _providers;
		private readonly OpusDecoder _dec;
		private bool isPlaying;
		#endregion

		#region InFields
		private readonly WaveInEvent _in;
		private readonly OpusEncoder _enc;
		private bool recording;
		#endregion

		private readonly int frames;
		private readonly bool _encode;
		private readonly IPEndPoint endpoint;
		private readonly UdpClient client;
		private readonly int bufferms;
		private readonly int lenting;
		private readonly ulong voice;
		private readonly int clientid;
		private bool disposed;

		public int Clients { get; set; }

		public AudioProvider(IPEndPoint ep, WaveFormat format, bool encode, int bufferMs, ulong voiceId, int clientId)
		{
			voice = voiceId;
			clientid = clientId;

			endpoint = ep;
			_format = format;
			bufferms = bufferMs;

			_mixer = new MixingSampleProvider(WaveFormat.CreateIeeeFloatWaveFormat(_format.SampleRate, _format.Channels));
			_provider = new SampleToWaveProvider16(_mixer);
			_providers = new ConcurrentDictionary<int, BufferedWaveProvider>();
			_out = new WaveOutEvent();
			isPlaying = false;

			_in = new WaveInEvent();
			//_in.DeviceNumber
			_in.BufferMilliseconds = bufferms;
			_in.WaveFormat = _format;
			_in.DataAvailable += DataAvailable;
			recording = false;


			frames = (_format.SampleRate / 1000) * bufferMs;

			client = new UdpClient();

			if (encode)
			{
				_encode = true;
				_dec = new OpusDecoder(_format.SampleRate, _format.Channels);

				_enc = new OpusEncoder(_format.SampleRate, _format.Channels, OpusApplication.OPUS_APPLICATION_VOIP)
				{
					Complexity = 10,
					SignalType = OpusSignal.OPUS_SIGNAL_VOICE,
					ForceMode = OpusMode.MODE_HYBRID,
					ExpertFrameDuration = OpusFramesize.OPUS_FRAMESIZE_60_MS
				};
			}
			lenting = frames * _format.Channels * (_format.BitsPerSample / 8);
		}
		public IList<string> GetInputDevices()
		{
			List<string> retval = new List<string>();
			for (int n = 0; n < WaveIn.DeviceCount; n++)
			{
				var caps = WaveIn.GetCapabilities(n);
				retval.Add(caps.ProductName);
			}
			return retval;
		}
		public IList<string> GetOutputDevices()
		{
			List<string> retval = new List<string>();
			for(int n = 0; n < WaveOut.DeviceCount; n++)
			{
				var caps = WaveOut.GetCapabilities(n);
				retval.Add(caps.ProductName);
			}
			return retval;
		}
		public void SetInputDeviceId(int deviceNum = -1)
		{
			_in.DeviceNumber = deviceNum;
		}
		public void InitializeWaveOut(int deviceNum = -1)
		{
			_out.DeviceNumber = deviceNum;
			_out.Init(_provider);
		}

		private async void DataAvailable(object sender, WaveInEventArgs e)
		{
			/*
			if (Clients < 1)
				return;
			*/
			byte[] b = e.Buffer;
			
			if(_encode)
			{
				short[] buff = BytesToShorts(b, 0, b.Length);
				b = new byte[1275*3];
				int a = _enc.Encode(buff, 0, frames, b, 0, b.Length);
				b = SubArray(b, 0, a);
			}

			b = Join(BitConverter.GetBytes(voice), b);
			await client.SendAsync(b, b.Length);
		}
		private async Task HandleReceives()
		{
			while (true)
			{
				try
				{
					var dg = (await client.ReceiveAsync()).Buffer;

					int id = BitConverter.ToInt32(new byte[] { dg[0], dg[1], dg[2], dg[3] }, 0);

					///*
					if (clientid == id)
					{
						continue;
					}
					//*/

					int asdf = dg.Length;
					dg = SubArray(dg, 4, dg.Length - 4);

					var provider = _providers.GetOrAdd(id, xx =>
					{
						var temp = new BufferedWaveProvider(_format);
						_mixer.AddMixerInput(temp);
						return temp;
					});

					if (_encode)
					{
						short[] b = new short[lenting / 2];
						int dd = _dec.Decode(dg, 0, dg.Length, b, 0, frames);
						dg = ShortsTobytes(b);
					}

					if (!isPlaying)
					{
						isPlaying = true;
						_out.Play();
					}

					provider.AddSamples(dg, 0, dg.Length);
				}
				catch (Exception e)
				{
					Console.WriteLine(e);
				}
			}

		}
		public Task StartAsync()
		{
			client.Connect(endpoint);
			_ = HandleReceives();

			_in.StartRecording();
			recording = true;

			return Task.CompletedTask;
		}
		
		public static T[] SubArray<T>(T[] data, int index, int length)
		{
			T[] result = new T[length];
			Array.Copy(data, index, result, 0, length);
			return result;
		}
		private static byte[] Join(byte[] a, byte[] b)
		{
			return a.Concat(b).ToArray();
		}
		private static short[] BytesToShorts(byte[] input, int offset, int length)
		{
			short[] processedValues = new short[length / 2];
			for (int c = 0; c < processedValues.Length; c++)
			{
				processedValues[c] = (short)(((int)input[(c * 2) + offset]) << 0);
				processedValues[c] += (short)(((int)input[(c * 2) + 1 + offset]) << 8);
			}

			return processedValues;
		}
		private static byte[] ShortsTobytes(short[] source)
		{
			byte[] target = new byte[source.Length * 2];
			Buffer.BlockCopy(source, 0, target, 0, target.Length);
			return target;
		}
	}
}
