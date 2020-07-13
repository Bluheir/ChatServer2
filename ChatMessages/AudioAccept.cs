using Newtonsoft.Json;

namespace ChatMessages
{
	public class AudioAccept : BaseMessage
	{
		[JsonProperty("voiceid")]
		public ulong VoiceId { get; set; }
	}
}
