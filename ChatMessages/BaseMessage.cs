using System;
using Newtonsoft.Json;

namespace ChatMessages
{
	public class BaseMessage
	{
		[JsonProperty("message_type")]
		public string MessageType { get; set; }

		[JsonProperty("value")]
		public object Value { get; set; }
	}
}