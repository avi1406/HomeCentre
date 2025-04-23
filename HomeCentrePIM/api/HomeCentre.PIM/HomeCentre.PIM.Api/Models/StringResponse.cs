namespace HomeCentre.PIM.Api.Models
{
    public class StringResponse
    {
        public StringResponse(string content)
        {
            Content = content;
        }

        public string Content { get; }
    }
}