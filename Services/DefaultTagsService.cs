using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace BGMSelector.Services
{
    public class DefaultTrackInfo
    {
        [JsonPropertyName("file")]
        public string File { get; set; }
        [JsonPropertyName("displayName")]
        public string DisplayName { get; set; }
        [JsonPropertyName("id")]
        public int? Id { get; set; }
    }

    public class DefaultTagsConfig
    {
        [JsonPropertyName("defaultBattle")]
        public List<DefaultTrackInfo> DefaultBattle { get; set; } = new List<DefaultTrackInfo>();
        [JsonPropertyName("defaultVictory")]
        public List<DefaultTrackInfo> DefaultVictory { get; set; } = new List<DefaultTrackInfo>();
        [JsonPropertyName("customBattle")]
        public List<DefaultTrackInfo> CustomBattle { get; set; } = new List<DefaultTrackInfo>();
        [JsonPropertyName("customVictory")]
        public List<DefaultTrackInfo> CustomVictory { get; set; } = new List<DefaultTrackInfo>();
    }

    public class DefaultTagsService
    {
        private readonly DefaultTagsConfig _config;
        private readonly Dictionary<string, string> _fileToDisplayNameMap = new Dictionary<string, string>();

        public DefaultTagsService(string defaultTagsPath)
        {
            if (File.Exists(defaultTagsPath))
            {
                var json = File.ReadAllText(defaultTagsPath);
                _config = JsonSerializer.Deserialize<DefaultTagsConfig>(json) ?? new DefaultTagsConfig();
            }
            else
            {
                _config = new DefaultTagsConfig();
            }

            var allFileTracks = _config.DefaultBattle
                .Concat(_config.DefaultVictory)
                .Concat(_config.CustomBattle)
                .Concat(_config.CustomVictory)
                .Where(t => !string.IsNullOrEmpty(t.File));

            foreach (var track in allFileTracks)
            {
                if (!string.IsNullOrEmpty(track.File) && !string.IsNullOrEmpty(track.DisplayName))
                {
                    _fileToDisplayNameMap[track.File.ToLower()] = track.DisplayName;
                }
            }
        }

        public string GetDisplayName(string hcaFileName)
        {
            if (_fileToDisplayNameMap.TryGetValue(hcaFileName.ToLower(), out var displayName))
            {
                return displayName;
            }
            return hcaFileName;
        }
        
        public DefaultTagsConfig GetConfig()
        {
            return _config;
        }
    }
} 