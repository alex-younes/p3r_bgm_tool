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
    
    public class RandomizedTrack
    {
        public int CueId { get; set; }
        public List<string> HcaFiles { get; set; } = new List<string>();
        public bool IncludeDefault { get; set; }
        
        public bool IsRandomized => HcaFiles.Count > 1 || (HcaFiles.Count > 0 && IncludeDefault);
        
        public RandomizedTrack(int cueId)
        {
            CueId = cueId;
        }
        
        public RandomizedTrack(int cueId, List<string> hcaFiles, bool includeDefault = false)
        {
            CueId = cueId;
            HcaFiles = hcaFiles;
            IncludeDefault = includeDefault;
        }
        
        public override string ToString()
        {
            int totalTracks = HcaFiles.Count + (IncludeDefault ? 1 : 0);
            return $"Random ({totalTracks} tracks)";
        }
    }
} 