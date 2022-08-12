namespace Core.Api
{
    public class WebApiOptions
    {
        public WebApiOptions()
        {
            AggregationIdHeader = "X-Request-ID";
        }
        public string AggregationIdHeader { get; set; }
    }
}