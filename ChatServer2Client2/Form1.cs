using ChatMessages;
using NAudio.Wave;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace ChatServer2Client2
{
	public partial class Form1 : Form
	{
		private readonly TcpClient tcp;

		private IPEndPoint endpoint;
		private AudioProvider prov;
		private NetworkStream stream;
		private int id;
		private ulong voiceId;
		private bool stopReading;
		private bool connected;

		private int? outs = null;
		private int? inps = null;

		public Form1()
		{
			InitializeComponent();
			tcp = new TcpClient();

			
			prov = new AudioProvider(endpoint, new WaveFormat(48000, 16, 2), true, 60);

			this.input.KeyPress += EnterPress;
			this.sendButton.Click += SendButtonClick;

			WriteLine("Please type in the ip address and port of the server.");
		}

		private async void SendButtonClick(object sender, EventArgs e)
		{
			await OnInput();
		}

		private async void EnterPress(object sender, KeyPressEventArgs e)
		{
			if (e.KeyChar != (char)Keys.Enter)
				return;

			await OnInput();
		}
		private async Task OnInput()
		{
			string text = this.input.Text;
			input.Text = "";

			if (!connected)
			{
				bool i = IPEndPoint.TryParse(text, out endpoint);
				if (!i)
				{
					WriteLine("This IP address is invalid.");
					return;
				}

				try
				{
					await tcp.ConnectAsync(endpoint.Address, endpoint.Port);
				}
				catch(SocketException)
				{
					WriteLine("No server is hosted on that ip address.");
				}
				stream = tcp.GetStream();
				connected = true;
				HandleReceivesTCP(null);
				prov.EndPoint = endpoint;
				return;
			}

			if(stopReading && outs == null)
			{
				var output = Convert.ToInt32(text);
				prov.InitializeWaveOut(output - 1);


				var ins = prov.GetInputDevices();
				WriteLine($"There have been {ins.Count} input devices detected. Type the number next to the device you want to use.");
				WriteLine("0 : Auto");
				for (int i = 0; i < ins.Count; i++)
				{
					WriteLine($"{i + 1} : {ins[i]}");
				}
				outs = output;
				return;
			}
			if(outs != null && stopReading)
			{
				inps = Convert.ToInt32(text);
				prov.SetInputDeviceId(inps.GetValueOrDefault() - 1);
				stopReading = false;

				var msg = new BaseMessage() { MessageType = "VOICE_CONNECT" };
				var json = JsonConvert.SerializeObject(msg);
				var bytes = Encoding.UTF8.GetBytes(json);

				await stream.WriteAsync(bytes, 0, bytes.Length);
				return;
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
				if (voiceId != 0)
					return;
				var outs = prov.GetOutputDevices();
				WriteLine($"There have been {outs.Count} output devices detected. Type the number next to the device you want to use.");
				WriteLine("0 : Auto");
				for (int i = 0; i < outs.Count; i++)
				{
					WriteLine($"{i + 1} : {outs[i]}");
				}
				stopReading = true;
			}

		}
		private async void HandleReceivesTCP(object a)
		{

			while (true)
			{
				byte[] bytes = new byte[2048];
				await stream.ReadAsync(bytes, 0, bytes.Length);

				var json = Encoding.UTF8.GetString(bytes).TrimEnd('\0');
				if (string.IsNullOrWhiteSpace(json))
				{
					continue;
				}
				var msg = JsonConvert.DeserializeObject<BaseMessage>(json);

				var type = msg.MessageType;
				if (type == "TEXT")
				{
					var tm = JsonConvert.DeserializeObject<TextMessage>(json);
					WriteLine($"<{tm.Sender}>: {tm.Contents}");
				}
				else if (type == "VOICE_CONNECT")
				{
					var dId = Convert.ToInt32(msg.Value);
					if (dId == id)
					{
						var tm = JsonConvert.DeserializeObject<AudioAccept>(json);
						voiceId = tm.VoiceId;

						prov.ClientId = id;
						prov.VoiceId = voiceId;

					
						await prov.StartAsync();
						WriteLine("AYYYYY OOOOO");
					}
					else
					{
						WriteLine($"Client with id {msg.Value} connected to voice chat.");
					}
				}
				else if (type == "WELCOME")
				{
					id = Convert.ToInt32(msg.Value);
					WriteLine($"You connected with id {id}.");
				}

			}
		}
		public void WriteLine(object value)
		{
			log.Text += value + "\n";
		}

	}
}
