using System;
using System.Net;
using System.Text;
using System.Net.Sockets;
using System.Threading.Tasks;


namespace ChatServer2Client
{
	class Program
	{
		private UdpClient client;
		private static void Main()
		=> new Program().MainAsync().GetAwaiter().GetResult();

		private async Task MainAsync()
		{
			client = new UdpClient()
			{
				EnableBroadcast = true
			};

			client.Connect("192.168.1.101", 0);

			HandleReceives();
			HandleSends();

			await Task.Delay(-1);
		}
		private async void HandleReceives()
		{
			while (true)
			{
				//Console.WriteLine("very very very very gay");
				var dg = await client.ReceiveAsync();
				string text = Encoding.ASCII.GetString(dg.Buffer);
				Console.WriteLine(text);
			}
		}
		private async void HandleSends()
		{
			while(true)
			{
				string text = Console.ReadLine();
				byte[] bytes = Encoding.ASCII.GetBytes(text);
				await client.SendAsync(bytes, bytes.Length);
			}
		}
	}
}
