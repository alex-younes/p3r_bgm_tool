using BGMSelector.Models;
using BGMSelector.Services;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Windows.Forms;

namespace BGMSelector
{
    public class BattleMusicForm : Form
    {
        private class DefaultTrackInfo
        {
            [System.Text.Json.Serialization.JsonPropertyName("file")]
            public string File { get; set; }
            [System.Text.Json.Serialization.JsonPropertyName("displayName")]
            public string DisplayName { get; set; }
            [System.Text.Json.Serialization.JsonPropertyName("id")]
            public int? Id { get; set; }
        }

        private class DefaultTagsConfig
        {
            [System.Text.Json.Serialization.JsonPropertyName("defaultBattle")]
            public List<DefaultTrackInfo> DefaultBattle { get; set; }
            [System.Text.Json.Serialization.JsonPropertyName("defaultVictory")]
            public List<DefaultTrackInfo> DefaultVictory { get; set; }
            [System.Text.Json.Serialization.JsonPropertyName("customBattle")]
            public List<DefaultTrackInfo> CustomBattle { get; set; }
            [System.Text.Json.Serialization.JsonPropertyName("customVictory")]
            public List<DefaultTrackInfo> CustomVictory { get; set; }
        }

        private readonly MusicService _musicService;
        private readonly HcaNameService _hcaNameService;
        private readonly string _battleMusicPmePath;
        private readonly string _defaultTagsPath;
        private List<string> _allHcaFiles;

        private readonly Color _personaBlue = Color.FromArgb(43, 87, 151);
        private readonly Color _darkBackground = Color.FromArgb(30, 30, 30);
        private readonly Color _lightText = Color.FromArgb(240, 240, 240);

        // UI Controls
        private GroupBox grpNormal, grpAdvantage, grpDisadvantage, grpVictory;
        private RadioButton radNormalSingle, radNormalRandom, radAdvantageSingle, radAdvantageRandom, radDisadvantageSingle, radDisadvantageRandom, radVictorySingle, radVictoryRandom;
        private CheckedListBox lstNormal, lstAdvantage, lstDisadvantage, lstVictory;
        private Button btnSave, btnCancel;

        public BattleMusicForm(MusicService musicService, HcaNameService hcaNameService, string battleMusicPmePath, string defaultTagsPath = null)
        {
            _musicService = musicService;
            _hcaNameService = hcaNameService;
            _battleMusicPmePath = battleMusicPmePath;
            _defaultTagsPath = defaultTagsPath;

            InitializeComponent();
            PopulateHcaLists();
            LoadConfig();
        }

        private void InitializeComponent()
        {
            this.Text = "Custom Battle Music Editor";
            this.ClientSize = new Size(800, 600);
            this.StartPosition = FormStartPosition.CenterParent;
            this.BackColor = _darkBackground;
            this.ForeColor = _lightText;
            this.Padding = new Padding(10);
            this.Font = new Font("Arial", 9F, FontStyle.Regular);

            var mainLayout = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 2,
                RowCount = 3,
            };
            mainLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50F));
            mainLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50F));
            mainLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 45F));
            mainLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 45F));
            mainLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 50F));

            // Create sections
            grpNormal = CreateMusicGroup("Normal Battle", out lstNormal, out radNormalSingle, out radNormalRandom);
            grpAdvantage = CreateMusicGroup("Advantage Battle", out lstAdvantage, out radAdvantageSingle, out radAdvantageRandom);
            grpDisadvantage = CreateMusicGroup("Disadvantage Battle", out lstDisadvantage, out radDisadvantageSingle, out radDisadvantageRandom);
            grpVictory = CreateMusicGroup("Victory", out lstVictory, out radVictorySingle, out radVictoryRandom);

            mainLayout.Controls.Add(grpNormal, 0, 0);
            mainLayout.Controls.Add(grpAdvantage, 1, 0);
            mainLayout.Controls.Add(grpDisadvantage, 0, 1);
            mainLayout.Controls.Add(grpVictory, 1, 1);

            // Button Panel
            var buttonPanel = new Panel { Dock = DockStyle.Fill, Padding = new Padding(5) };
            btnSave = new Button { Text = "Save & Close", Dock = DockStyle.Right, Width = 120, BackColor = _personaBlue, ForeColor = Color.White, FlatStyle = FlatStyle.Flat };
            btnCancel = new Button { Text = "Cancel", Dock = DockStyle.Right, Width = 100, BackColor = Color.FromArgb(70, 70, 70), ForeColor = Color.White, FlatStyle = FlatStyle.Flat };
            
            btnSave.Click += (s, e) => SaveConfig();
            btnCancel.Click += (s, e) => this.Close();

            buttonPanel.Controls.Add(btnSave);
            buttonPanel.Controls.Add(btnCancel);
            
            mainLayout.Controls.Add(buttonPanel, 0, 2);
            mainLayout.SetColumnSpan(buttonPanel, 2);

            this.Controls.Add(mainLayout);
        }
        
        private GroupBox CreateMusicGroup(string title, out CheckedListBox list, out RadioButton single, out RadioButton random)
        {
            var group = new GroupBox
            {
                Text = title,
                Dock = DockStyle.Fill,
                ForeColor = _lightText,
                Padding = new Padding(10),
                Margin = new Padding(5)
            };

            var layout = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 1, RowCount = 2 };
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 30F));
            layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));

            var optionsPanel = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 2 };
            optionsPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50F));
            optionsPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50F));

            var createdSingle = new RadioButton { Text = "Single Track", Checked = true, Dock = DockStyle.Fill, ForeColor = _lightText };
            var createdRandom = new RadioButton { Text = "Random Tracks", Dock = DockStyle.Fill, ForeColor = _lightText };
            optionsPanel.Controls.Add(createdSingle, 0, 0);
            optionsPanel.Controls.Add(createdRandom, 1, 0);

            var createdList = new CheckedListBox
            {
                Dock = DockStyle.Fill,
                BackColor = Color.FromArgb(40, 40, 40),
                ForeColor = _lightText,
                BorderStyle = BorderStyle.FixedSingle,
                CheckOnClick = true,
                SelectionMode = SelectionMode.One
            };
            
            layout.Controls.Add(optionsPanel, 0, 0);
            layout.Controls.Add(createdList, 0, 1);
            group.Controls.Add(layout);

            // Event Handlers
            createdSingle.CheckedChanged += (s, e) =>
            {
                if (createdSingle.Checked)
                {
                    // When switching to single-select, ensure only one item is checked.
                    int? firstChecked = null;
                    for (int i = 0; i < createdList.Items.Count; i++)
                    {
                        if (createdList.GetItemChecked(i))
                        {
                            if (firstChecked == null)
                            {
                                firstChecked = i;
                            }
                            else
                            {
                                createdList.SetItemChecked(i, false);
                            }
                        }
                    }
                }
            };

            createdList.ItemCheck += (s, e) => {
                // If in single-select mode and a new item is checked, uncheck others.
                if (createdSingle.Checked && e.NewValue == CheckState.Checked)
                {
                    for (int i = 0; i < createdList.Items.Count; i++)
                    {
                        if (i != e.Index) createdList.SetItemChecked(i, false);
                    }
                }
            };
            
            // Assign to out parameters
            list = createdList;
            single = createdSingle;
            random = createdRandom;
            
            return group;
        }

        private void PopulateHcaLists()
        {
            var defaultTags = new DefaultTagsConfig 
            { 
                DefaultBattle = new List<DefaultTrackInfo>(), 
                DefaultVictory = new List<DefaultTrackInfo>(),
                CustomBattle = new List<DefaultTrackInfo>(),
                CustomVictory = new List<DefaultTrackInfo>()
            };

            try
            {
                // Try multiple locations for default_tags.json
                string baseDir = Path.GetDirectoryName(Application.ExecutablePath) ?? "";
                Console.WriteLine($"Base directory: {baseDir}");
                
                if (Path.GetFileName(baseDir) == "build")
                {
                    baseDir = Path.GetDirectoryName(baseDir) ?? "";
                    Console.WriteLine($"Adjusted base directory: {baseDir}");
                }
                
                // Try multiple possible paths
                string[] possiblePaths = {
                    Path.Combine(baseDir, "BGME", "default_tags.json"),
                    Path.Combine(baseDir, "default_tags.json"),
                    Path.Combine(baseDir, "..", "BGME", "default_tags.json")
                };
                
                string tagsPath = null;
                foreach (var path in possiblePaths)
                {
                    Console.WriteLine($"Checking path: {path}");
                    if (File.Exists(path))
                    {
                        tagsPath = path;
                        Console.WriteLine($"Found default_tags.json at: {tagsPath}");
                        break;
                    }
                }

                if (tagsPath != null && File.Exists(tagsPath))
                {
                    var json = File.ReadAllText(tagsPath);
                    Console.WriteLine($"JSON content: {json}");
                    
                    defaultTags = JsonSerializer.Deserialize<DefaultTagsConfig>(json);
                    Console.WriteLine($"Loaded default tags: {defaultTags.DefaultBattle.Count} battle tracks, {defaultTags.DefaultVictory.Count} victory tracks");
                    Console.WriteLine($"Loaded custom tags: {defaultTags.CustomBattle?.Count ?? 0} battle tracks, {defaultTags.CustomVictory?.Count ?? 0} victory tracks");
                    
                    // Debug output to verify default tags are loaded correctly
                    foreach (var item in defaultTags.DefaultBattle)
                    {
                        if (item.Id.HasValue)
                            Console.WriteLine($"Default Battle ID: {item.Id}, Name: {item.DisplayName}");
                        else
                            Console.WriteLine($"Default Battle File: {item.File}, Name: {item.DisplayName}");
                    }
                }
                else
                {
                    Console.WriteLine("Default tags file not found in any location");
                    MessageBox.Show("Default tags file not found. Please make sure default_tags.json exists in the BGME folder.", "File Not Found", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Could not load default_tags.json: {ex.Message}");
                MessageBox.Show($"Error loading default_tags.json: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }

            // Separate default tracks with files from those with just IDs
            var defaultBattleFiles = defaultTags.DefaultBattle
                .Where(x => !string.IsNullOrEmpty(x.File))
                .ToDictionary(x => x.File.ToLower(), x => x.DisplayName);
            
            var defaultVictoryFiles = defaultTags.DefaultVictory
                .Where(x => !string.IsNullOrEmpty(x.File))
                .ToDictionary(x => x.File.ToLower(), x => x.DisplayName);

            // Get custom tracks with files
            var customBattleFiles = defaultTags.CustomBattle?
                .Where(x => !string.IsNullOrEmpty(x.File))
                .ToDictionary(x => x.File.ToLower(), x => x.DisplayName) ?? new Dictionary<string, string>();
            
            var customVictoryFiles = defaultTags.CustomVictory?
                .Where(x => !string.IsNullOrEmpty(x.File))
                .ToDictionary(x => x.File.ToLower(), x => x.DisplayName) ?? new Dictionary<string, string>();

            // Get default tracks with just IDs
            var defaultBattleIds = defaultTags.DefaultBattle
                .Where(x => x.Id.HasValue)
                .ToList();
                
            var defaultVictoryIds = defaultTags.DefaultVictory
                .Where(x => x.Id.HasValue)
                .ToList();

            Console.WriteLine($"Default battle IDs count: {defaultBattleIds.Count}");
            Console.WriteLine($"Default victory IDs count: {defaultVictoryIds.Count}");
            Console.WriteLine($"Custom battle files count: {customBattleFiles.Count}");
            Console.WriteLine($"Custom victory files count: {customVictoryFiles.Count}");

            _allHcaFiles = _musicService.GetAvailableHcaFiles()
                .OrderBy(f => {
                    string fileName = Path.GetFileName(f).ToLower();
                    if (defaultVictoryFiles.ContainsKey(fileName)) return 0;
                    if (defaultBattleFiles.ContainsKey(fileName)) return 1;
                    if (customVictoryFiles.ContainsKey(fileName)) return 2;
                    if (customBattleFiles.ContainsKey(fileName)) return 3;
                    return 4;
                })
                .ThenBy(f => f)
                .ToList();

            // Create display items for HCA files
            var displayItems = _allHcaFiles.Select(file => {
                string fileName = Path.GetFileName(file).ToLower();
                string customName = _hcaNameService.GetName(file);
                
                string baseName = (customName != file && !string.IsNullOrEmpty(customName))
                    ? $"{customName} ({Path.GetFileName(file)})"
                    : Path.GetFileName(file);

                if (defaultVictoryFiles.TryGetValue(fileName, out string victoryName))
                {
                    return $"{victoryName} ({Path.GetFileName(file)}) [Default Victory]";
                }
                if (defaultBattleFiles.TryGetValue(fileName, out string battleName))
                {
                    return $"{battleName} ({Path.GetFileName(file)}) [Default Battle]";
                }
                if (customVictoryFiles.TryGetValue(fileName, out string customVictoryName))
                {
                    return $"{customVictoryName} ({Path.GetFileName(file)}) [Custom Victory]";
                }
                if (customBattleFiles.TryGetValue(fileName, out string customBattleName))
                {
                    return $"{customBattleName} ({Path.GetFileName(file)}) [Custom Battle]";
                }
                return baseName;
            }).ToArray();

            // Add items for default tracks with just IDs
            var defaultBattleIdItems = defaultBattleIds
                .Select(item => $"{item.DisplayName} (ID: {item.Id}) [Default Battle]")
                .ToArray();
                
            var defaultVictoryIdItems = defaultVictoryIds
                .Select(item => $"{item.DisplayName} (ID: {item.Id}) [Default Victory]")
                .ToArray();

            // Clear existing items
            lstNormal.Items.Clear();
            lstAdvantage.Items.Clear();
            lstDisadvantage.Items.Clear();
            lstVictory.Items.Clear();

            // Add all items to the lists - DEFAULT ID TRACKS FIRST
            if (defaultBattleIdItems.Length > 0)
            {
                lstNormal.Items.AddRange(defaultBattleIdItems);
                lstAdvantage.Items.AddRange(defaultBattleIdItems);
                lstDisadvantage.Items.AddRange(defaultBattleIdItems);
            }
            else
            {
                Console.WriteLine("WARNING: No default battle ID tracks found!");
            }
            
            if (defaultVictoryIdItems.Length > 0)
            {
                lstVictory.Items.AddRange(defaultVictoryIdItems);
            }
            else
            {
                Console.WriteLine("WARNING: No default victory ID tracks found!");
            }
            
            // Then add the HCA files
            lstNormal.Items.AddRange(displayItems);
            lstAdvantage.Items.AddRange(displayItems);
            lstDisadvantage.Items.AddRange(displayItems);
            lstVictory.Items.AddRange(displayItems);
        }

        private void LoadConfig()
        {
            if (!File.Exists(_battleMusicPmePath)) return;

            var content = File.ReadAllText(_battleMusicPmePath);
            if (string.IsNullOrWhiteSpace(content)) return;

            var parsedConsts = new Dictionary<string, List<string>>();
            string normalVal = "default", advantageVal = "default", disadvantageVal = "default", victoryVal = "default";

            var songRegex = new Regex(@"""(?<name>.*?)""|(?<name>\w+)");

            // Parse consts
            var constRegex = new Regex(@"const\s+(?<name>\w+)\s*=\s*random_song\(\[(?<songs>.*?)\]\)");
            foreach (Match match in constRegex.Matches(content))
            {
                var name = match.Groups["name"].Value;
                var songs = songRegex.Matches(match.Groups["songs"].Value)
                                     .Cast<Match>()
                                     .Select(m => m.Groups["name"].Value.Trim())
                                     .ToList();
                parsedConsts[name] = songs;
            }

            // Parse encounter block
            var encounterBlockRegex = new Regex(@"encounter\[""Normal Battles""\]:(?<block>.*?)(end|\z)", RegexOptions.Singleline);
            var encounterMatch = encounterBlockRegex.Match(content);
            if (encounterMatch.Success)
            {
                var blockContent = encounterMatch.Groups["block"].Value;

                // Parse battle_bgm function
                var bgmRegex = new Regex(@"music\s*=\s*battle_bgm\((?<normal>.*?),\s*(?<advantage>.*?),\s*(?<disadvantage>.*?)\)");
                var bgmMatch = bgmRegex.Match(blockContent);
                if (bgmMatch.Success)
                {
                    normalVal = NormalizeValue(bgmMatch.Groups["normal"].Value.Trim());
                    advantageVal = NormalizeValue(bgmMatch.Groups["advantage"].Value.Trim());
                    disadvantageVal = NormalizeValue(bgmMatch.Groups["disadvantage"].Value.Trim());
                }
                else
                {
                    // Parse individual music assignments if not using battle_bgm
                    // Parse normal music (music = X)
                    var normalMusicRegex = new Regex(@"music\s*=\s*(?<normal>[^=\r\n]+)");
                    var normalMatch = normalMusicRegex.Match(blockContent);
                    if (normalMatch.Success)
                    {
                        normalVal = NormalizeValue(normalMatch.Groups["normal"].Value.Trim());
                        System.Diagnostics.Debug.WriteLine($"[BGME] Parsed normal music: {normalVal}");
                    }

                    // Parse advantage_bgm
                    var advantageRegex = new Regex(@"advantage_bgm\s*=\s*(?<advantage>[^=\r\n]+)");
                    var advantageMatch = advantageRegex.Match(blockContent);
                    if (advantageMatch.Success)
                    {
                        advantageVal = NormalizeValue(advantageMatch.Groups["advantage"].Value.Trim());
                        System.Diagnostics.Debug.WriteLine($"[BGME] Parsed advantage music: {advantageVal}");
                    }

                    // Parse disadvantage_bgm
                    var disadvantageRegex = new Regex(@"disadvantage_bgm\s*=\s*(?<disadvantage>[^=\r\n]+)");
                    var disadvantageMatch = disadvantageRegex.Match(blockContent);
                    if (disadvantageMatch.Success)
                    {
                        disadvantageVal = NormalizeValue(disadvantageMatch.Groups["disadvantage"].Value.Trim());
                        System.Diagnostics.Debug.WriteLine($"[BGME] Parsed disadvantage music: {disadvantageVal}");
                    }
                }

                // Parse victory_music
                var victoryRegex = new Regex(@"victory_music\s*=\s*(?<victory>[^=\r\n]+)");
                var victoryMatch = victoryRegex.Match(blockContent);
                if (victoryMatch.Success)
                {
                    victoryVal = NormalizeValue(victoryMatch.Groups["victory"].Value.Trim());
                    System.Diagnostics.Debug.WriteLine($"[BGME] Parsed victory music: {victoryVal}");
                }
            }

            // Debug output to verify parsed values
            System.Diagnostics.Debug.WriteLine($"[BGME] Parsed values - Normal: {normalVal}, Advantage: {advantageVal}, Disadvantage: {disadvantageVal}, Victory: {victoryVal}");

            // Update UI
            UpdateGroupUI(normalVal, lstNormal, radNormalSingle, radNormalRandom, parsedConsts);
            UpdateGroupUI(advantageVal, lstAdvantage, radAdvantageSingle, radAdvantageRandom, parsedConsts);
            UpdateGroupUI(disadvantageVal, lstDisadvantage, radDisadvantageSingle, radDisadvantageRandom, parsedConsts);
            UpdateGroupUI(victoryVal, lstVictory, radVictorySingle, radVictoryRandom, parsedConsts);
        }
        
        private void UpdateGroupUI(string value, CheckedListBox list, RadioButton radSingle, RadioButton radRandom, Dictionary<string, List<string>> parsedConsts)
        {
            for (int i = 0; i < list.Items.Count; i++) list.SetItemChecked(i, false);
            
            string unquotedValue = value.Replace("\"", "");

            if (unquotedValue == "default" || string.IsNullOrEmpty(unquotedValue))
            {
                radSingle.Checked = true;
                return; // Nothing selected is default
            }

            if (parsedConsts.TryGetValue(unquotedValue, out var trackList))
            {
                radRandom.Checked = true;
                foreach (var track in trackList)
                {
                    string trackToFind = track;
                    bool found = false;
                    // Check if this is a numeric ID (default track OR HCA file)
                    if (int.TryParse(track, out int trackId))
                    {
                        // 1. First, try to match by (ID: X) for default tracks
                        string trackToFindById = $"(ID: {trackId})";
                        for (int i = 0; i < list.Items.Count; i++)
                        {
                            if (list.Items[i].ToString().Contains(trackToFindById))
                            {
                                list.SetItemChecked(i, true);
                                found = true;
                                break;
                            }
                        }

                        // 2. If not found, try to match by (X.hca) for HCA files
                        if (!found)
                        {
                            string trackToFindByHca = $"({trackId}.hca)";
                            for (int i = 0; i < list.Items.Count; i++)
                            {
                                if (list.Items[i].ToString().Contains(trackToFindByHca))
                                {
                                    list.SetItemChecked(i, true);
                                    found = true;
                                    break;
                                }
                            }
                        }
                        
                        if (!found)
                        {
                            // Add as temporary item
                            string temp = $"Unknown Track (ID: {trackId}) [Unmatched]";
                            int idx = list.Items.Add(temp);
                            list.SetItemChecked(idx, true);
                            System.Diagnostics.Debug.WriteLine($"[BGME] Added unmatched ID track for random list: {trackId}");
                        }
                        continue;
                    }
                    
                    // Handle HCA file references
                    string fileNameWithoutExt = Path.GetFileNameWithoutExtension(track);
                    trackToFind = $"({fileNameWithoutExt}.hca)";
                    for (int i = 0; i < list.Items.Count; i++)
                    {
                        string itemText = list.Items[i].ToString();
                        if (itemText.Contains(trackToFind) || 
                            itemText.Contains(fileNameWithoutExt) || 
                            Path.GetFileNameWithoutExtension(itemText).Equals(track, StringComparison.OrdinalIgnoreCase))
                        {
                            list.SetItemChecked(i, true);
                            found = true;
                            break;
                        }
                    }
                    if (!found)
                    {
                        // Add as temporary item
                        string temp = $"Unknown Track ({fileNameWithoutExt}.hca) [Unmatched]";
                        int idx = list.Items.Add(temp);
                        list.SetItemChecked(idx, true);
                        System.Diagnostics.Debug.WriteLine($"[BGME] Added unmatched HCA track: {fileNameWithoutExt}");
                    }
                }
            }
            else 
            {
                radSingle.Checked = true;
                bool found = false;
                // Check if this is a numeric ID (default track OR HCA filename)
                if (int.TryParse(unquotedValue, out int trackId))
                {
                    // 1. First, try to match by (ID: X) for default tracks
                    string trackToFindById = $"(ID: {trackId})";
                    for (int i = 0; i < list.Items.Count; i++)
                    {
                        if (list.Items[i].ToString().Contains(trackToFindById))
                        {
                            list.SetItemChecked(i, true);
                            found = true;
                            break;
                        }
                    }

                    // 2. If not found, try to match by (X.hca) for HCA files
                    if (!found)
                    {
                        string trackToFindByHca = $"({trackId}.hca)";
                        for (int i = 0; i < list.Items.Count; i++)
                        {
                            if (list.Items[i].ToString().Contains(trackToFindByHca))
                            {
                                list.SetItemChecked(i, true);
                                found = true;
                                break;
                            }
                        }
                    }
                    
                    if (!found)
                    {
                        // Add as temporary item only if truly not found
                        string temp = $"Unknown Track (ID: {trackId}) [Unmatched]";
                        int idx = list.Items.Add(temp);
                        list.SetItemChecked(idx, true);
                        System.Diagnostics.Debug.WriteLine($"[BGME] Added unmatched ID track: {trackId}");
                    }
                }
                else
                {
                    // It's an HCA file name or something else
                    string fileNameWithoutExt = Path.GetFileNameWithoutExtension(unquotedValue);
                    string trackToFind = $"({fileNameWithoutExt}.hca)";
                    for (int i = 0; i < list.Items.Count; i++)
                    {
                        string itemText = list.Items[i].ToString();
                        if (itemText.Contains(trackToFind) || 
                            itemText.EndsWith(fileNameWithoutExt + ".hca") || 
                            Path.GetFileNameWithoutExtension(itemText).Equals(unquotedValue, StringComparison.OrdinalIgnoreCase))
                        {
                            list.SetItemChecked(i, true);
                            found = true;
                            break;
                        }
                    }
                    if (!found)
                    {
                        // Add as temporary item
                        string temp = $"Unknown Track ({fileNameWithoutExt}.hca) [Unmatched]";
                        int idx = list.Items.Add(temp);
                        list.SetItemChecked(idx, true);
                        System.Diagnostics.Debug.WriteLine($"[BGME] Added unmatched HCA track: {fileNameWithoutExt}");
                    }
                }
            }
        }

        private void SaveConfig()
        {
            var consts = new Dictionary<string, List<string>>();
            var properties = new List<string>();

            string normalValue = ProcessSection(lstNormal, radNormalRandom, "normalBgm", consts);
            string advantageValue = ProcessSection(lstAdvantage, radAdvantageRandom, "advantageBgm", consts);
            string disadvantageValue = ProcessSection(lstDisadvantage, radDisadvantageRandom, "disadvantageBgm", consts);
            string victoryValue = ProcessSection(lstVictory, radVictoryRandom, "victoryBgm", consts);

            // Count how many are set
            int setCount = 0;
            if (normalValue != null) setCount++;
            if (advantageValue != null) setCount++;
            if (disadvantageValue != null) setCount++;

            // If nothing is set, save an empty file
            if (setCount == 0 && victoryValue == null)
            {
                try
                {
                    File.WriteAllText(_battleMusicPmePath, "");
                    MessageBox.Show("All battle music set to default. Saved an empty configuration.", "Settings Cleared", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    this.DialogResult = DialogResult.OK;
                    this.Close();
                    return;
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Error saving empty settings: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }
            }

            // Compose music lines according to wiki logic
            if (normalValue != null && advantageValue != null && disadvantageValue != null)
            {
                properties.Add($"music = battle_bgm({normalValue}, {advantageValue}, {disadvantageValue})");
            }
            else if (normalValue != null && advantageValue != null && disadvantageValue == null)
            {
                properties.Add($"music = battle_bgm({normalValue}, {advantageValue})");
            }
            else if (normalValue != null && advantageValue == null && disadvantageValue == null)
            {
                properties.Add($"music = {normalValue}");
            }
            else
            {
                // For any that are set, but not covered above, use separate lines
                if (normalValue != null)
                    properties.Add($"music = {normalValue}");
                if (advantageValue != null)
                    properties.Add($"advantage_bgm = {advantageValue}");
                if (disadvantageValue != null)
                    properties.Add($"disadvantage_bgm = {disadvantageValue}");
            }

            if (victoryValue != null)
            {
                properties.Add($"victory_music = {victoryValue}");
            }

            var finalScript = new StringBuilder();

            foreach(var c in consts)
            {
                finalScript.AppendLine($"const {c.Key} = random_song([{string.Join(", ", c.Value)}])");
            }
            if (consts.Any()) finalScript.AppendLine();

            finalScript.AppendLine("encounter[\"Normal Battles\"]:");
            foreach(var prop in properties)
            {
                finalScript.AppendLine($"  {prop}");
            }
            finalScript.AppendLine("end");

            try
            {
                File.WriteAllText(_battleMusicPmePath, finalScript.ToString());
                MessageBox.Show("Battle music settings saved successfully!", "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);
                this.DialogResult = DialogResult.OK;
                this.Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error saving settings: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
        
        private string ProcessSection(CheckedListBox list, RadioButton randomRadio, string constName, Dictionary<string, List<string>> consts)
        {
            var selectedIndices = list.CheckedIndices.Cast<int>().ToList();

            if (!selectedIndices.Any()) return null;
            
            var selectedValues = selectedIndices.Select(i => {
                string item = list.Items[i].ToString();
                string finalValue = null;

                // 1. Extract ID from default tracks with ID: e.g., "Mass Destruction (Default) (ID: 26)"
                var idMatch = Regex.Match(item, @"\(ID: (\d+)\)");
                if (idMatch.Success)
                {
                    finalValue = idMatch.Groups[1].Value;
                }
                else 
                {
                    // 2. Extract filename from items with HCA files: e.g., "Way To Go (PQ) (30789.hca)"
                    string hcaFileName = null;
                    var fileMatch = Regex.Match(item, @"\(([^)]+\.hca)\)");
                    if (fileMatch.Success)
                    {
                        hcaFileName = Path.GetFileNameWithoutExtension(fileMatch.Groups[1].Value);
                    }
                    else if (item.EndsWith(".hca"))
                    {
                        hcaFileName = Path.GetFileNameWithoutExtension(item);
                    }
                    else
                    {
                        // Fallback for items without parentheses: e.g., item text contains "30456.hca"
                        string foundHca = _allHcaFiles.FirstOrDefault(hca => item.Contains(Path.GetFileName(hca)));
                        if(foundHca != null)
                        {
                            hcaFileName = Path.GetFileNameWithoutExtension(foundHca);
                        }
                    }
                    finalValue = hcaFileName;
                }

                if (finalValue == null) return null;

                // 3. The core rule: if it's a number, don't quote it. Otherwise, quote it.
                if (int.TryParse(finalValue, out _))
                {
                    return finalValue;
                }
                
                return $"\"{finalValue}\"";

            }).Where(s => s != null).ToList();

            if (randomRadio.Checked && selectedValues.Count > 1)
            {
                // Check if all values are of the same type (either all IDs or all strings)
                bool allIds = selectedValues.All(v => !v.StartsWith("\"") && !v.EndsWith("\""));
                bool allStrings = selectedValues.All(v => v.StartsWith("\"") && v.EndsWith("\""));
                
                if (!allIds && !allStrings)
                {
                    // Mixed types - show warning and use only the first value
                    MessageBox.Show(
                        "Cannot mix numeric IDs and string filenames in the same random selection. " +
                        "Only the first selected item will be used.", 
                        "Mixed Types Warning", 
                        MessageBoxButtons.OK, 
                        MessageBoxIcon.Warning);
                    
                    return selectedValues.FirstOrDefault() ?? "default";
                }
                
                consts[constName] = selectedValues;
                return constName;
            }

            return selectedValues.FirstOrDefault() ?? "default";
        }
        
        private int GetDefaultItemCountForList(CheckedListBox list)
        {
            return list.Items.Cast<string>().Count(item => item.Contains("(ID:"));
        }

        // Helper method to normalize values - removes quotes from numeric values
        private string NormalizeValue(string value)
        {
            if (string.IsNullOrWhiteSpace(value) || value == "default")
                return value;

            // Remove quotes if present
            string unquoted = value.Trim('"');
            
            // If it's a number, return without quotes
            if (int.TryParse(unquoted, out _))
                return unquoted;
                
            // Otherwise, keep the original format
            return value;
        }
    }
} 