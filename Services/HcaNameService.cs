using System.Text.Json;

namespace BGMSelector.Services
{
    public class HcaNameService
    {
        private readonly string _filePath;
        private Dictionary<string, string> _hcaNames;

        public HcaNameService(string filePath)
        {
            _filePath = filePath;
            _hcaNames = LoadHcaNames();
        }

        private Dictionary<string, string> LoadHcaNames()
        {
            if (!File.Exists(_filePath))
            {
                return new Dictionary<string, string>();
            }

            try
            {
                string json = File.ReadAllText(_filePath);
                return JsonSerializer.Deserialize<Dictionary<string, string>>(json) ?? new Dictionary<string, string>();
            }
            catch
            {
                // If deserialization fails, return an empty dictionary
                return new Dictionary<string, string>();
            }
        }

        public void SaveHcaNames()
        {
            try
            {
                string json = JsonSerializer.Serialize(_hcaNames, new JsonSerializerOptions { WriteIndented = true });
                File.WriteAllText(_filePath, json);
            }
            catch (Exception ex)
            {
                // Handle exceptions (e.g., show a message to the user)
                MessageBox.Show($"Error saving HCA names: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        public string GetName(string hcaFile)
        {
            return _hcaNames.TryGetValue(hcaFile, out var name) ? name : hcaFile;
        }

        public void SetName(string hcaFile, string name)
        {
            _hcaNames[hcaFile] = name;
            SaveHcaNames();
        }

        public void RemoveName(string hcaFile)
        {
            if (_hcaNames.ContainsKey(hcaFile))
            {
                _hcaNames.Remove(hcaFile);
                SaveHcaNames();
            }
        }

        public void RenameHcaFile(string oldName, string newName)
        {
            if (_hcaNames.ContainsKey(oldName))
            {
                _hcaNames[newName] = _hcaNames[oldName];
                _hcaNames.Remove(oldName);
                SaveHcaNames();
            }
        }
    }
} 