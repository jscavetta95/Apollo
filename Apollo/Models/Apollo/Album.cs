namespace Apollo.Models.Apollo
{
    public class Album
    {
        public Album(string name, string artist, string uri, string imageLink)
        {
            Name = name;
            Artist = artist;
            Uri = uri;
            ImageLink = imageLink;
        }

        public string Name { get; set; }

        public string Artist { get; set; }

        public string Uri { get; set; }

        public string ImageLink { get; set; }
    }
}