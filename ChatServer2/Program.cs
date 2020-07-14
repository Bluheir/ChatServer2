using System;
using System.Threading.Tasks;
using System.Threading;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Collections.Generic;
using System.Collections.Concurrent;
using ChatMessages;
using System.Linq;
using Newtonsoft.Json;

namespace ChatServer2
{
	class Program
	{
		private readonly TCPUDPServer _server;
		private readonly ConcurrentDictionary<TcpClient, ConnectedClientData> _clientData;
        private readonly Dictionary<int, TcpClient> cidToTcp;
		private readonly Dictionary<ulong, TcpClient> idToTcp;

		private Program()
		{
			IPEndPoint a = new IPEndPoint(IPAddress.Parse("192.168.1.112"), 6878);
			IPEndPoint b = new IPEndPoint(IPAddress.Parse("192.168.1.112"), 6878);

			_server = new TCPUDPServer(a,b);
			idToTcp = new Dictionary<ulong, TcpClient>();
            cidToTcp = new Dictionary<int, TcpClient>();
			_clientData = new ConcurrentDictionary<TcpClient, ConnectedClientData>();
		}

		private static void Main()
		=> new Program().MainAsync().GetAwaiter().GetResult();

		private async Task MainAsync()
		{
			_server.OnConnect += OnClientConnect;
			_server.OnDisconnect += OnClientDisconnect;
			_server.OnMessageReceivedTCP += OnMessageReceivedTCP;
			_server.OnMessageReceivedUDP += OnMessageReceivedUDP;

			_server.TCPPacketSize = 32767;

			await _server.StartAsync();
			await Task.Delay(-1);
		}



        private async Task OnClientDisconnect(TcpClient client)
		{
			_clientData.TryRemove(client, out var a);
            cidToTcp.Remove(a.ClientId);
			
			if(a.VoiceClientData != null)
			{
				idToTcp.Remove(a.VoiceClientData.GetValueOrDefault());
			}

			var msg = new BaseMessage() { MessageType = "DISCONNECT", Value = a.ClientId };
			var buffer = Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(msg));
			
			foreach(var c in _clientData.Keys)
			{
				await c.GetStream().WriteAsync(buffer, 0, buffer.Length);
			}
			Console.WriteLine($"Client {a.ClientId} disconnected with voice client id {(a.VoiceClientData as object) ?? "null"}");
		}

		private async Task OnMessageReceivedUDP(IPEndPoint endpoint, byte[] bytes)
		{
			//Console.WriteLine("Received sometin");
			ArraySegment<byte> parse = new ArraySegment<byte>(bytes);
			ulong id = BitConverter.ToUInt64(parse.Slice(0, 8));
			
			if (!idToTcp.ContainsKey(id))
				return;
			var clientdata = _clientData[idToTcp[id]];
			clientdata.UDPEndpoint = endpoint;

			var bb = Join(BitConverter.GetBytes(clientdata.ClientId), SubArray(bytes, 8, bytes.Length - 8));

			foreach (var c in idToTcp.Values)
			{
				var eps = _clientData[c];
				var ep = eps.UDPEndpoint;
				if (ep == null || eps.ClientId == _clientData[idToTcp[id]].ClientId)
					continue;
				await _server.SendUDPAsync(bb, ep);
			}
		}

		private async Task OnMessageReceivedTCP(TcpClient client, byte[] bytes)
		{
			var json = Encoding.UTF8.GetString(bytes).TrimEnd('\0');
			var msg = JsonConvert.DeserializeObject<BaseMessage>(json);
			
			var type = msg.MessageType;
			
			if(type == "TEXT")
			{
				var tm = ToTextMessage(msg.Value.ToString(), _clientData[client].ClientId);

				foreach (var c in _clientData)
				{
					await SendMessageAsync(tm, c.Key);
				}
				Console.WriteLine($"<{tm.Sender}>: {(tm.Contents)}");
			}
			else if(type == "VOICE_CONNECT")
			{
				var cl = _clientData.GetOrAdd(client, (x) => new ConnectedClientData());

				if (cl.UDPEndpoint != null)
					return;


				cl.VoiceClientData = RandomUlong(0, ulong.MaxValue);
				
				idToTcp.Add(cl.VoiceClientData.GetValueOrDefault(), client);
				foreach(var c in _clientData)
				{
					if(c.Key == client)
					{
						await SendMessageAsync(new AudioAccept() { MessageType = "VOICE_CONNECT", Value = c.Value.ClientId, VoiceId = c.Value.VoiceClientData.GetValueOrDefault() }, c.Key);
						continue;
					}
					if(c.Value.VoiceClientData != null)
					{
						await SendMessageAsync(ToNotifyVoiceConnect(cl.ClientId), c.Key);
					}
				}
			}
		}

		private async Task OnClientConnect(TcpClient client)
		{
            int id = _clientData.Count;
            while (true)
            {
                if (cidToTcp.ContainsKey(id))
                {
                    id++;
                }
                else
                    break;
            }
            cidToTcp.Add(id, client);
            var cdata = _clientData.GetOrAdd(client, x => new ConnectedClientData() { ClientId = id });
           
			foreach(var c in _clientData)
			{
				if(c.Key == client)
				{
					await SendMessageAsync(ToNotifyWelcome(id), client);
					continue;
				}
				await SendMessageAsync(ToNotifyConnect(id), c.Key);
			}
			Console.WriteLine($"Client connected with id {id}");
		}

		public static async Task SendMessageAsync(BaseMessage message, TcpClient client)
		{
			if (message == null)
				return;
			var json = JsonConvert.SerializeObject(message);
			var bytes = Encoding.UTF8.GetBytes(json);

			await client.GetStream().WriteAsync(bytes, 0, bytes.Length);
		}
		public static TextMessage ToTextMessage(string text, int sender)
		{
			return new TextMessage() { MessageType = "TEXT", Sender = sender, Contents = text };
		}
		public static ulong RandomUlong(ulong min, ulong max, Random rand = null)
		{
			rand = rand ?? new Random();

			byte[] b = new byte[8];
			rand.NextBytes(b);

			return BitConverter.ToUInt64(b, 0);
		}

		public static BaseMessage ToNotifyConnect(int sender)
		{
			return new BaseMessage() { MessageType = "CONNECT", Value = sender };
		}
		public static BaseMessage ToNotifyDisconnect(int sender)
		{
			return new BaseMessage { MessageType = "DISCONNECT", Value = sender };
		}
		public static BaseMessage ToNotifyWelcome(int sender)
		{
			return new BaseMessage() { MessageType = "WELCOME", Value = sender };
		}
		public static BaseMessage ToNotifyVoiceConnect(int sender)
		{
			return new BaseMessage() { MessageType = "VOICE_CONNECT", Value = sender };
		}
		public static byte[] Join(byte[] arr1, byte[] arr2)
		{
			return arr1.Concat(arr2).ToArray();
		}
		public static T[] SubArray<T>(T[] data, int index, int length)
		{
			T[] result = new T[length];
			Array.Copy(data, index, result, 0, length);
			return result;
		}
	}
}