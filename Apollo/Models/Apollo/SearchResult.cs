namespace Apollo.Models.Apollo
{
    public class SearchResult
    {
        public SearchResult(string value, string img, string uri)
        {
            Value = value;
            Img = img;
            Uri = uri;
        }

        public string Value { get; set; }

        public string Img { get; set; }

        public string Uri { get; set; }
    }
}