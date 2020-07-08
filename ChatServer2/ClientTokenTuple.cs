using System.Net.Sockets;
using System.Threading;

namespace ChatServer2
{
	public class ClientTokenTuple
	{
		public TcpClient Client { get; }
		public CancellationTokenSource TokenSource { get; }

		public ClientTokenTuple(TcpClient client, CancellationTokenSource tokenSource = null)
		{
			Client = client;
			TokenSource = tokenSource ?? new CancellationTokenSource();
		}
	}
}
