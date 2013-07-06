using Newtonsoft.Json;

namespace ArcGisServerPermissionsProxy.Api.Models.Response
{
    /// <summary>
    ///     Generic ResultContainer for passing result data
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class ResponseContainer<T> : ResponseContainer where T : class
    {
        public ResponseContainer(int status, string message, T result) : base(status, message)
        {
            Result = result;
        }

        [JsonProperty(PropertyName = "result")]
        public T Result { get; set; }
    }

    /// <summary>
    ///     A container class for returning api call results with status messages.
    /// </summary>
    public class ResponseContainer
    {
        public ResponseContainer(int status, string message)
        {
            Status = status;
            Message = message;
        }

        [JsonProperty(PropertyName = "status")]
        public int Status { get; set; }

        [JsonProperty(PropertyName = "message")]
        public string Message { get; set; }

        public bool ShouldSerializeMessage()
        {
            return !string.IsNullOrEmpty(Message);
        }
    }
}