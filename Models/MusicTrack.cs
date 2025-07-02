namespace BGMSelector.Models
{
    public class MusicTrack
    {
        public string? Name { get; set; }
        public string? Category { get; set; }
        public List<string>? Tags { get; set; }
        public int CueId { get; set; }
        public string? OutputPath { get; set; }
        public string? Encoder { get; set; }

        public override string ToString()
        {
            return $"{Name} (ID: {CueId})";
        }
    }

    public class MusicConfig
    {
        public string? Name { get; set; }
        public List<string>? Tags { get; set; }
        public string? DefaultOutputPath { get; set; }
        public string? DefaultEncoder { get; set; }
        public bool DefaultLoopState { get; set; }
        public List<MusicTrack>? Tracks { get; set; }
    }
} 