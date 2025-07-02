using BGMSelector.Models;
using System.Text;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace BGMSelector.Services
{
    public class MusicService
    {
        private readonly string _yamlPath;
        private readonly string _pmePath;
        private readonly string _hcaFolderPath;

        public MusicService(string yamlPath, string pmePath, string hcaFolderPath)
        {
            _yamlPath = yamlPath;
            _pmePath = pmePath;
            _hcaFolderPath = hcaFolderPath;
        }

        public MusicConfig LoadMusicConfig()
        {
            try
            {
                var yamlText = File.ReadAllText(_yamlPath);
                var deserializer = new DeserializerBuilder()
                    .WithNamingConvention(UnderscoredNamingConvention.Instance)
                    .Build();

                return deserializer.Deserialize<MusicConfig>(yamlText);
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
                return Directory.GetFiles(_hcaFolderPath, "*.hca")
                    .Select(Path.GetFileName)
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

                foreach (var assignment in cueAssignments)
                {
                    // Extract just the numeric part from the HCA filename (e.g., "10500" from "10500.hca")
                    string hcaValue = Path.GetFileNameWithoutExtension(assignment.Value);
                    
                    sb.AppendLine($"global_bgm[{assignment.Key}]:");
                    sb.AppendLine($"  music = {hcaValue}");
                    sb.AppendLine("end");
                    sb.AppendLine();
                }

                File.WriteAllText(_pmePath, sb.ToString());
            }
            catch (Exception ex)
            {
                throw new Exception($"Failed to update PME file: {ex.Message}", ex);
            }
        }

        public Dictionary<int, string> LoadCurrentPmeAssignments()
        {
            try
            {
                var assignments = new Dictionary<int, string>();
                
                if (!File.Exists(_pmePath))
                    return assignments;

                var lines = File.ReadAllLines(_pmePath);
                
                for (int i = 0; i < lines.Length; i++)
                {
                    var line = lines[i].Trim();
                    if (line.StartsWith("global_bgm[") && line.EndsWith("]:"))
                    {
                        // Extract cue ID
                        var cueIdStr = line.Substring(11, line.Length - 13);
                        if (int.TryParse(cueIdStr, out int cueId))
                        {
                            // Get the music value from the next line
                            if (i + 1 < lines.Length)
                            {
                                var musicLine = lines[i + 1].Trim();
                                if (musicLine.StartsWith("music = "))
                                {
                                    var musicValue = musicLine.Substring(8);
                                    assignments[cueId] = $"{musicValue}.hca";
                                }
                            }
                        }
                    }
                }

                return assignments;
            }
            catch (Exception ex)
            {
                throw new Exception($"Failed to load current PME assignments: {ex.Message}", ex);
            }
        }
    }
} 