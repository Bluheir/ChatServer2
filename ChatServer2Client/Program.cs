using System;
using System.Net;
using System.Text;
using System.Net.Sockets;
using System.Threading.Tasks;
using Newtonsoft.Json;
using ChatMessages;
using System.Threading;
using NAudio.Wave;
using Newtonsoft.Json.Linq;

namespace ChatServer2Client
{
	class Program
	{
		private readonly TcpClient tcp;
		private CancellationTokenSource src;
		private AudioProvider prov;
		private readonly IPEndPoint endpoint;
		private NetworkStream stream;
		private int id;
		private ulong voiceId;
		private bool stopReading;

		private Program()
		{
			endpoint = new IPEndPoint(IPAddress.Parse("192.168.1.102"), 6878);
			src = new CancellationTokenSource();
			tcp = new TcpClient();
		}

		private static void Main()
		=> new Program().MainAsync().GetAwaiter().GetResult();

		private async Task MainAsync()
		{
			await tcp.ConnectAsync(endpoint.Address, endpoint.Port);
			stream = tcp.GetStream();

			Thread receives = new Thread(HandleReceivesTCP);
			Thread sends = new Thread(HandleSendsTCP);

			receives.Start();
			sends.Start();

			await Task.Delay(-1);
		}
		private async void HandleReceivesTCP(object s)
		{
			
			while (true)
			{
				byte[] bytes = new byte[2048];
				await stream.ReadAsync(bytes, 0, bytes.Length);

				var json = Encoding.UTF8.GetString(bytes).TrimEnd('\0');
				var msg = JsonConvert.DeserializeObject<BaseMessage>(json);

				var type = msg.MessageType;
				if (type == "TEXT")
				{
					var tm = JsonConvert.DeserializeObject<TextMessage>(json);
					Console.WriteLine($"<{tm.Sender}>: {tm.Contents}");
				}
				else if (type == "VOICE_CONNECT")
				{
					var dId = Convert.ToInt32(msg.Value);
					if(dId == id)
					{
						var tm = JsonConvert.DeserializeObject<AudioAccept>(json);
						voiceId = tm.VoiceId;
						prov = new AudioProvider(endpoint, new WaveFormat(48000, 16, 2), true, 60, voiceId, id);

						stopReading = true;
						src.Cancel();
						var outs = prov.GetOutputDevices();
						Console.WriteLine($"There have been {outs.Count} output devices detected. Type the number next to the device you want to use.");
						Console.WriteLine("0 : Auto");
						for(int i = 0; i < outs.Count; i++)
						{
							Console.WriteLine($"{i + 1} : {outs[i]}");
						}
						var output = Convert.ToInt32(Console.ReadLine());
						prov.InitializeWaveOut(output - 1);

						var ins = prov.GetInputDevices();
						Console.WriteLine($"There have been {ins.Count} input devices detected. Type the number next to the device you want to use.");
						Console.WriteLine("0 : Auto");
						for (int i = 0; i < ins.Count; i++)
						{
							Console.WriteLine($"{i + 1} : {ins[i]}");
						}
						var input = Convert.ToInt32(Console.ReadLine());
						prov.SetInputDeviceId(input - 1);
						
						stopReading = false;
						src = new CancellationTokenSource();

						await prov.StartAsync();
					}
					else
					{
						prov.Clients++;
						Console.WriteLine($"Client with id {msg.Value} connected to voice chat.");
					}
				}
				else if (type == "WELCOME")
				{
					id = Convert.ToInt32(msg.Value);
					Console.WriteLine($"You connected with id {id}.");
				}

			}
		}
		private async void HandleSendsTCP(object s)
		{
			
			while(true)
			{
				if (stopReading)
					continue;

				var text = ConsoleReader.CancellableReadLine(src.Token);
				
				if(stopReading)
				{
					continue;
				}
				
				if(text != "/connect")
				{
					var msg = new BaseMessage() { MessageType = "TEXT", Value = text };
					var json = JsonConvert.SerializeObject(msg);
					var bytes = Encoding.UTF8.GetBytes(json);

					await stream.WriteAsync(bytes, 0, bytes.Length);
				}
				else
				{
					if(voiceId != 0)
					{
						continue;
					}
					var msg = new BaseMessage() { MessageType = "VOICE_CONNECT" };
					var json = JsonConvert.SerializeObject(msg);
					var bytes = Encoding.UTF8.GetBytes(json);

					await stream.WriteAsync(bytes, 0, bytes.Length);
				}
			}
		}
		/*
		private static Task<string> ReadLineAsync()
		=> Task.Run(Console.ReadLine);
		*/
	}
}