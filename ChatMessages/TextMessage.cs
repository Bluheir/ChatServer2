using Newtonsoft.Json;

namespace ChatMessages
{
	public class TextMessage : BaseMessage
	{
		[JsonProperty("contents")]
		public string Contents { get; set; }

		[JsonProperty("sender")]
		public int? Sender { get; set; }
	}
}