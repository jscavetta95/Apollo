namespace Apollo.Models.Spurify
{
    using System;

    public class Track
    {
        public Track(string name, string artist, string uri)
        {
            Name = name;
            Artist = artist;
            Uri = uri;
        }

        public string Name { get; set; }

        public string Artist { get; set; }

        public string Uri { get; set; }

        public int PlayCount { get; set; }

        public override int GetHashCode()
        {
            return (Name.ToLower() + Artist.ToLower()).GetHashCode();
        }

        public override bool Equals(object obj)
        {
            return (obj is Track) && Name.Equals(((Track)obj).Name) && Artist.Equals(((Track)obj).Artist);
        }

        public override string ToString()
        {
            return Name;
        }
    }
}