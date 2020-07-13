using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;

namespace ChatServer2
{
	public class TCPUDPServer : IAsyncDisposable
	{
		private readonly UdpClient udpServer;
		private readonly TcpListener tcpServer;
		private readonly IPEndPoint endpoint;
		private readonly IPEndPoint udpendpoint;
		private readonly CancellationTokenSource tcpSource;
		private readonly CancellationTokenSource udpSource;
		private readonly ConcurrentDictionary<TcpClient, ClientTokenTuple> clientTuples;
		private bool disposed;
		private bool hasStarted;

		private Func<TcpClient, Task> _onConnect;
		private Func<TcpClient, Task> _onDisconnect;
		private Func<TcpClient, byte[], Task> _onMessageReceivedTCP;
		private Func<IPEndPoint, byte[], Task> _onMessageReceivedUDP;

		public event Func<TcpClient, Task> OnConnect 
		{
			add 
			{
				_onConnect = value;
			}
			remove
			{
				_onConnect = null;
			}
		}
		public event Func<TcpClient, byte[], Task> OnMessageReceivedTCP
		{
			add
			{
				_onMessageReceivedTCP = value;
			}
			remove
			{
				_onMessageReceivedTCP = null;
			}
		}
		public event Func<TcpClient, Task> OnDisconnect
		{ 
			add
			{
				_onDisconnect = value;
			} 
			remove 
			{
				_onDisconnect = null;
			} 
		}
		public event Func<IPEndPoint, byte[], Task> OnMessageReceivedUDP 
		{
			add 
			{
				_onMessageReceivedUDP = value;
			} 
			remove 
			{
				_onMessageReceivedUDP = null;
			} 
		}

		public IPEndPoint IPEndpoint => endpoint;
		public IPEndPoint UDPEndpoint => udpendpoint;
		public int TCPPacketSize { get; set; }
		public int UDPPacketSize { get; set; }
		public IReadOnlyDictionary<TcpClient, ClientTokenTuple> ClientTuples => clientTuples;

		public TCPUDPServer(IPEndPoint endpoint, IPEndPoint udpendpoint)
		{
			this.endpoint = endpoint;
			this.udpendpoint = udpendpoint;

			udpServer = new UdpClient(udpendpoint);
			tcpServer = new TcpListener(endpoint);
			tcpSource = new CancellationTokenSource();
			udpSource = new CancellationTokenSource();
			clientTuples = new ConcurrentDictionary<TcpClient, ClientTokenTuple>();

			udpServer.EnableBroadcast = true;
		}

		public Task StartAsync()
		{
			if (disposed)
				return Task.CompletedTask;

			tcpServer.Start();
			_ = HandleTCP();
			HandleUDP();

			return Task.CompletedTask;
		}
		public async Task<int> SendUDPAsync(byte[] bytes, IPEndPoint endpoint)
		{
			if (disposed)
				return 0;
			return await udpServer.SendAsync(bytes, bytes.Length, endpoint);
		}
		public async Task<int> BroadcastAsync(byte[] bytes)
		{
			if (disposed)
				return 0;
			return await udpServer.SendAsync(bytes, bytes.Length, "255.255.255.255", udpendpoint.Port);
		}
		public Task DisconnectClientAsync(TcpClient client)
		{
			if (disposed)
				return Task.CompletedTask;


			if (!clientTuples.TryRemove(client, out var ctuple))
				return Task.CompletedTask;
			ctuple.TokenSource.Cancel();
			
			client.GetStream().Close();
			client.Close();

			return Task.CompletedTask;
		}

		private async Task HandleTCP()
		{
			await Task.Run(async () =>	
			{
				while(true)
				{
					if(tcpSource.IsCancellationRequested)
					{
						break;
					}

					var client = await tcpServer.AcceptTcpClientAsync();
					var tuple = new ClientTokenTuple(client);

					clientTuples.GetOrAdd(client, tuple);
					if (_onConnect != null)
					{
						await _onConnect(client);
					}
					_ = HandleClient(tuple);
				}
			}, tcpSource.Token);
		}
		private async void HandleUDP()
		{
			await Task.Run(async () =>
			{
				try
				{
					while (true)
					{
						if (udpSource.IsCancellationRequested)
						{
							break;
						}

						var result = await udpServer.ReceiveAsync();

						if (_onMessageReceivedUDP != null)
							await _onMessageReceivedUDP(result.RemoteEndPoint, result.Buffer);
					}
				}
				catch(Exception e)
				{
					Console.WriteLine(e);
				}

			}, udpSource.Token);
		}
		private async Task HandleClient(ClientTokenTuple tuple)
		{
			var stream = tuple.Client.GetStream();
			try
			{
				await Task.Run(async () =>
				{
					while(true)
					{
						if(tuple.TokenSource.IsCancellationRequested)
						{
							break;
						}
						
						var bytes = new byte[TCPPacketSize];
						await stream.ReadAsync(bytes, 0, bytes.Length);


						if(_onMessageReceivedTCP != null)
							await _onMessageReceivedTCP(tuple.Client, bytes);
					}
				}, tuple.TokenSource.Token);
			}
			catch
			{
				stream.Close();
				tuple.Client.Close();
				clientTuples.TryRemove(tuple.Client, out _);
				if (_onDisconnect != null)
					await _onDisconnect(tuple.Client);
			}

		}
		public async ValueTask DisposeAsync()
		{
			if (disposed)
				return;
			if (hasStarted)
			{
				foreach (var client in clientTuples)
					await DisconnectClientAsync(client.Key);
				tcpSource.Cancel();
				udpSource.Cancel();

				tcpServer.Stop();
			}

			udpServer.Dispose();

			tcpSource.Dispose();
			udpSource.Dispose();

			disposed = true;
		}
		~ TCPUDPServer()
		{
			if (!disposed)
				DisposeAsync().GetAwaiter().GetResult();
		}
	}
}
