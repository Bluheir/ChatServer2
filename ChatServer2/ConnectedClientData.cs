namespace ChatServer2
{
	public class ConnectedClientData
	{
		public int ClientId { get; set; }
		public ulong? VoiceClientData { get; set; }
		public System.Net.IPEndPoint UDPEndpoint { get; set; }
	}
}