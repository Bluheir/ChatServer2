using System;
using System.Threading.Tasks;
using System.Threading;
using System.Net;
using System.Net.Sockets;
using System.Text;

namespace ChatServer2
{
	class Program
	{
		private TCPUDPServer _server;

		private static void Main()
		=> new Program().MainAsync().GetAwaiter().GetResult();

		private async Task MainAsync()
		{
			var tcpendpoint = new IPEndPoint(
				IPAddress.Parse("127.0.0.1"),
				6969);
			var udpendpoint = new IPEndPoint(
				IPAddress.Parse("192.168.1.101"),
				0
				);
			
			_server = new TCPUDPServer(tcpendpoint, udpendpoint);

			_server.OnMessageReceivedUDP += MessageReceived;
			_server.OnMessageReceivedTCP += TCPMessageReceived;
			_server.OnConnect += ClientConnect;
			_server.OnDisconnect += Disconnect;

			await _server.StartAsync();

			while(true)
			{
				string text = Console.ReadLine();

				if(text == "sendtest")
				{
					await _server.BroadcastAsync(Encoding.ASCII.GetBytes("nigger nigger nigger nigger nigger"));
					Console.WriteLine("sent test packet");
				}
			}
		}

		private Task Disconnect(TcpClient arg)
		{
			Console.WriteLine("client disconnected");
			return Task.CompletedTask;
		}

		private Task TCPMessageReceived(TcpClient arg1, byte[] arg2)
		{
			throw new NotImplementedException();
		}

		private async Task ClientConnect(TcpClient arg)
		{
			Console.WriteLine("client connected");
			
		}

		private async Task MessageReceived(IPEndPoint arg1, byte[] arg2)
		{
			Console.WriteLine(Encoding.ASCII.GetString(arg2) + arg1.ToString());
			await _server.SendUDPAsync(Encoding.ASCII.GetBytes("YO BITCH I RECEIVED YOUR MESSAGE"), arg1);
		}
	}
}
