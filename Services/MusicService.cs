using BGMSelector.Models;
using System.Text;
using System.Text.RegularExpressions;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace BGMSelector.Services
{
    public class MusicService
    {
        private readonly string _yamlPath;
        private readonly string _pmePath;
        private readonly string _hcaFolderPath;
        private Dictionary<int, RandomizedTrack> _randomizedTracks = new Dictionary<int, RandomizedTrack>();
        private MusicConfig? _musicConfigCache;

        public MusicService(string yamlPath, string pmePath, string hcaFolderPath)
        {
            _yamlPath = yamlPath;
            _pmePath = pmePath;
            _hcaFolderPath = hcaFolderPath;
        }

        public MusicConfig LoadMusicConfig()
        {
            if (_musicConfigCache != null)
            {
                return _musicConfigCache;
            }
            try
            {
                var yamlText = File.ReadAllText(_yamlPath);
                var deserializer = new DeserializerBuilder()
                    .WithNamingConvention(UnderscoredNamingConvention.Instance)
                    .Build();

                _musicConfigCache = deserializer.Deserialize<MusicConfig>(yamlText);
                return _musicConfigCache;
            }
            catch (Exception ex)
            {
                throw new Exception($"Failed to load music configuration: {ex.Message}", ex);
            }
        }

        public List<string> GetAvailableHcaFiles()
        {
            try
            {
                if (!Directory.Exists(_hcaFolderPath))
                {
                    return new List<string>();
                }
                return Directory.GetFiles(_hcaFolderPath, "*.hca")
                    .Select(Path.GetFileName)
                    .Where(f => f != null)
                    .Select(f => f!)
                    .ToList();
            }
            catch (Exception ex)
            {
                throw new Exception($"Failed to get HCA files: {ex.Message}", ex);
            }
        }

        public void UpdatePmeFile(Dictionary<int, string> cueAssignments)
        {
            try
            {
                var sb = new StringBuilder();

                var allCueIds = cueAssignments.Keys.Union(_randomizedTracks.Keys).Distinct();

                foreach (var cueId in allCueIds)
                {
                    if (_randomizedTracks.TryGetValue(cueId, out var randomTrack) && randomTrack.IsRandomized)
                    {
                        var trackIds = randomTrack.HcaFiles
                            .Select(Path.GetFileNameWithoutExtension)
                            .ToList();

                        if (randomTrack.IncludeDefault)
                        {
                            string? originalId = GetOriginalTrackId(cueId);
                            if (originalId != null)
                            {
                                trackIds.Add(originalId);
                            }
                        }
                        
                        // BGME requires at least two tracks for randomization
                        if (trackIds.Count > 1)
                        {
                            string trackList = string.Join(", ", trackIds);
                            sb.AppendLine($"global_bgm[{cueId}]:");
                            sb.AppendLine($"  music = random_song([{trackList}])");
                            sb.AppendLine("end");
                            sb.AppendLine();
                        }
                        // If it's not actually random (e.g. 1 custom track, no default), treat as regular assignment
                        else if (trackIds.Count == 1)
                        {
                             sb.AppendLine($"global_bgm[{cueId}]:");
                             sb.AppendLine($"  music = {trackIds.First()}");
                             sb.AppendLine("end");
                             sb.AppendLine();
                        }
                    }
                    else if (cueAssignments.TryGetValue(cueId, out var hcaFile))
                    {
                        string hcaValue = Path.GetFileNameWithoutExtension(hcaFile);
                        sb.AppendLine($"global_bgm[{cueId}]:");
                        sb.AppendLine($"  music = {hcaValue}");
                        sb.AppendLine("end");
                        sb.AppendLine();
                    }
                }

                File.WriteAllText(_pmePath, sb.ToString());
            }
            catch (Exception ex)
            {
                throw new Exception($"Failed to update PME file: {ex.Message}", ex);
            }
        }
        
        private string? GetOriginalTrackId(int cueId)
        {
            try
            {
                var config = LoadMusicConfig();
                var track = config.Tracks?.FirstOrDefault(t => t.CueId == cueId);
                
                if (track != null && !string.IsNullOrEmpty(track.OutputPath))
                {
                    string filename = Path.GetFileNameWithoutExtension(track.OutputPath);
                    
                    // Extracts number from strings like "link_101" or "Sound_01"
                    var match = Regex.Match(filename, @"\d+$");
                    if (match.Success)
                    {
                        return match.Value;
                    }
                }
                
                return cueId.ToString();
            }
            catch
            {
                return cueId.ToString();
            }
        }

        public Dictionary<int, string> LoadCurrentPmeAssignments()
        {
            var assignments = new Dictionary<int, string>();
            _randomizedTracks.Clear();

            if (!File.Exists(_pmePath))
                return assignments;
                
            try
            {
                var pmeContent = File.ReadAllText(_pmePath);
                
                // Regex to find all global_bgm blocks
                var blockRegex = new Regex(@"global_bgm\[(\d+)\]:\s*([\s\S]*?)\s*end", RegexOptions.Multiline);
                var matches = blockRegex.Matches(pmeContent);

                foreach (Match match in matches)
                {
                    if (!int.TryParse(match.Groups[1].Value, out int cueId)) continue;
                    
                    string blockContent = match.Groups[2].Value.Trim();
                    var musicLineMatch = Regex.Match(blockContent, @"music\s*=\s*(.+)");
                    
                    if (!musicLineMatch.Success) continue;
                    
                    string musicValue = musicLineMatch.Groups[1].Value.Trim();
                    
                    // Check for randomization
                    var randomMatch = Regex.Match(musicValue, @"random_song\(\[([\d\s,]+)\]\)");
                    if (randomMatch.Success)
                    {
                        var ids = randomMatch.Groups[1].Value.Split(',')
                            .Select(s => s.Trim())
                            .ToList();
                            
                        string? originalId = GetOriginalTrackId(cueId);
                        var randomTrack = new RandomizedTrack(cueId);
                        randomTrack.IncludeDefault = originalId != null && ids.Contains(originalId);
                        
                        randomTrack.HcaFiles = ids
                            .Where(id => id != originalId)
                            .Select(id => $"{id}.hca")
                            .ToList();
                        
                        _randomizedTracks[cueId] = randomTrack;
                        
                        // For display purposes, show the first custom HCA, or an empty string if only default is used
                        assignments[cueId] = randomTrack.HcaFiles.FirstOrDefault() ?? "";
                    }
                    else
                    {
                        assignments[cueId] = $"{musicValue}.hca";
                    }
                }
                return assignments;
            }
            catch (Exception ex)
            {
                throw new Exception($"Failed to load current PME assignments: {ex.Message}", ex);
            }
        }
        
        public void SetRandomizedTrack(int cueId, List<string> hcaFiles, bool includeDefault)
        {
            _randomizedTracks[cueId] = new RandomizedTrack(cueId, hcaFiles, includeDefault);
        }
        
        public RandomizedTrack GetRandomizedTrack(int cueId)
        {
            return _randomizedTracks.TryGetValue(cueId, out var track) ? track : new RandomizedTrack(cueId);
        }
        
        public bool IsRandomized(int cueId)
        {
            return _randomizedTracks.TryGetValue(cueId, out var track) && track.IsRandomized;
        }
        
        public void RemoveRandomization(int cueId)
        {
            _randomizedTracks.Remove(cueId);
        }
        
        public IEnumerable<int> GetAllRandomizedCueIds()
        {
            return _randomizedTracks.Keys;
        }
    }
} 