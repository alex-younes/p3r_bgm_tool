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
        private readonly MusicService _musicService;
        private readonly HcaNameService _hcaNameService;
        private readonly DefaultTagsService _defaultTagsService;
        private readonly string _battleMusicPmePath;
        private readonly string _bossMusicPmePath;
        private List<string> _allHcaFiles;

        private readonly Color _personaBlue = Color.FromArgb(43, 87, 151);
        private readonly Color _darkBackground = Color.FromArgb(30, 30, 30);
        private readonly Color _lightText = Color.FromArgb(240, 240, 240);

        // UI Controls
        private GroupBox grpNormal, grpAdvantage, grpDisadvantage, grpBoss, grpVictory;
        private RadioButton radNormalSingle, radNormalRandom, radAdvantageSingle, radAdvantageRandom, radDisadvantageSingle, radDisadvantageRandom, radBossSingle, radBossRandom, radVictorySingle, radVictoryRandom;
        private CheckedListBox lstNormal, lstAdvantage, lstDisadvantage, lstBoss, lstVictory;
        private Button btnSave, btnCancel;

        public BattleMusicForm(MusicService musicService, HcaNameService hcaNameService, DefaultTagsService defaultTagsService, string battleMusicPmePath)
        {
            _musicService = musicService;
            _hcaNameService = hcaNameService;
            _defaultTagsService = defaultTagsService;
            _battleMusicPmePath = battleMusicPmePath;
            _bossMusicPmePath = Path.Combine(Path.GetDirectoryName(battleMusicPmePath) ?? string.Empty, "boss_music.pme");

            InitializeComponent();
            PopulateHcaLists();
            LoadConfig();
        }

        private void InitializeComponent()
        {
            this.SuspendLayout();
            // 
            // BattleMusicForm
            // 
            this.ClientSize = new System.Drawing.Size(1200, 800); // Increased window size
            this.Name = "BattleMusicForm";
            this.Text = "Battle Music Editor";
            this.Icon = new System.Drawing.Icon("Resources/persona_icon.ico");
            this.BackColor = _darkBackground;
            this.ForeColor = _lightText;
            this.Padding = new Padding(10); // Add padding around the form content
            this.ResumeLayout(false);

            var mainLayout = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 2,
                RowCount = 4, // 3 rows for groups + 1 for buttons
            };
            mainLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50F));
            mainLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50F));
            mainLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 33F)); // Normal & Advantage
            mainLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 33F)); // Disadvantage & Boss
            mainLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 34F)); // Victory (spans columns)
            mainLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 50F)); // Buttons

            // Create sections
            grpNormal = CreateMusicGroup("Normal Battle", out lstNormal, out radNormalSingle, out radNormalRandom);
            grpAdvantage = CreateMusicGroup("Advantage Battle", out lstAdvantage, out radAdvantageSingle, out radAdvantageRandom);
            grpDisadvantage = CreateMusicGroup("Disadvantage Battle", out lstDisadvantage, out radDisadvantageSingle, out radDisadvantageRandom);
            grpBoss = CreateMusicGroup("Boss Battle", out lstBoss, out radBossSingle, out radBossRandom);
            grpVictory = CreateMusicGroup("Victory", out lstVictory, out radVictorySingle, out radVictoryRandom);

            // Arrange the five groups in the grid
            mainLayout.Controls.Add(grpNormal, 0, 0);
            mainLayout.Controls.Add(grpAdvantage, 1, 0);
            mainLayout.Controls.Add(grpDisadvantage, 0, 1);
            mainLayout.Controls.Add(grpBoss, 1, 1);

            // Victory spans the full width on its own row
            mainLayout.Controls.Add(grpVictory, 0, 2);
            mainLayout.SetColumnSpan(grpVictory, 2);

            // Button Panel
            var buttonPanel = new Panel { Dock = DockStyle.Fill, Padding = new Padding(5) };
            btnSave = new Button { Text = "Save & Close", Dock = DockStyle.Right, Width = 120, BackColor = _personaBlue, ForeColor = Color.White, FlatStyle = FlatStyle.Flat };
            btnCancel = new Button { Text = "Cancel", Dock = DockStyle.Right, Width = 100, BackColor = Color.FromArgb(70, 70, 70), ForeColor = Color.White, FlatStyle = FlatStyle.Flat };
            
            btnSave.Click += (s, e) => SaveConfig();
            btnCancel.Click += (s, e) => this.Close();

            buttonPanel.Controls.Add(btnSave);
            buttonPanel.Controls.Add(btnCancel);
            
            mainLayout.Controls.Add(buttonPanel, 0, 3);
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

            // Main layout: 1 column, 2 rows (options, list)
            var layout = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                RowCount = 2,
                ColumnCount = 1,
            };
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 40)); // Increased height for options row
            layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100)); // List takes remaining space

            // Panel for top-row options: 1 row, 3 columns (single, random, clear)
            var optionsPanel = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 3,
                RowCount = 1,
                Margin = new Padding(0, 0, 0, 6) // Add space below the options row
            };
            optionsPanel.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
            optionsPanel.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
            optionsPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));

            var createdSingle = new RadioButton { Text = "Single Track", Checked = true, ForeColor = _lightText, AutoSize = true };
            createdSingle.Margin = new Padding(3, 3, 15, 3); // Add space to the right

            var createdRandom = new RadioButton { Text = "Random", ForeColor = _lightText, AutoSize = true };
            
            var btnClear = new Button 
            { 
                Text = "Clear", 
                Dock = DockStyle.Right,
                Size = new Size(90, 30),
                BackColor = Color.FromArgb(70, 70, 70), 
                ForeColor = _lightText, 
                FlatStyle = FlatStyle.Flat,
                Margin = new Padding(15, 0, 0, 0) // Gap before button
            };
            btnClear.FlatAppearance.BorderSize = 1;
            btnClear.FlatAppearance.BorderColor = Color.FromArgb(100, 100, 100);

            // List must be created before Clear button so the lambda can reference it safely
            var createdList = new CheckedListBox
            {
                Dock = DockStyle.Fill,
                BackColor = Color.FromArgb(40, 40, 40),
                ForeColor = _lightText,
                BorderStyle = BorderStyle.FixedSingle,
                CheckOnClick = true,
                SelectionMode = SelectionMode.One,
                IntegralHeight = false // Prevents weird height changes
            };

            // Clear button logic – uncheck everything in the list
            btnClear.Click += (s, e) => {
                for (int i = 0; i < createdList.Items.Count; i++)
                {
                    createdList.SetItemChecked(i, false);
                }
            };

            // Add controls to the options panel
            optionsPanel.Controls.Add(createdSingle, 0, 0);
            optionsPanel.Controls.Add(createdRandom, 1, 0);
            optionsPanel.Controls.Add(btnClear, 2, 0);

            // Add panels to the main layout
            layout.Controls.Add(optionsPanel, 0, 0);
            layout.Controls.Add(createdList, 0, 1);
            group.Controls.Add(layout);

            // Event Handlers for single-select mode
            createdSingle.CheckedChanged += (s, e) =>
            {
                if (createdSingle.Checked)
                {
                    int? firstChecked = null;
                    for (int i = 0; i < createdList.Items.Count; i++)
                    {
                        if (createdList.GetItemChecked(i))
                        {
                            if (firstChecked == null) firstChecked = i;
                            else createdList.SetItemChecked(i, false);
                        }
                    }
                }
            };

            createdList.ItemCheck += (s, e) => {
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
            var defaultTags = _defaultTagsService.GetConfig();

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
            lstBoss.Items.Clear();
            lstVictory.Items.Clear();

            // Add all items to the lists - DEFAULT ID TRACKS FIRST
            if (defaultBattleIdItems.Length > 0)
            {
                lstNormal.Items.AddRange(defaultBattleIdItems);
                lstAdvantage.Items.AddRange(defaultBattleIdItems);
                lstDisadvantage.Items.AddRange(defaultBattleIdItems);
                lstBoss.Items.AddRange(defaultBattleIdItems);
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
            lstBoss.Items.AddRange(displayItems);
            lstVictory.Items.AddRange(displayItems);
        }

        private void LoadConfig()
        {
            if (!File.Exists(_battleMusicPmePath) && !File.Exists(_bossMusicPmePath)) return;

            var contentBuilder = new StringBuilder();
            if (File.Exists(_battleMusicPmePath))
            {
                contentBuilder.AppendLine(File.ReadAllText(_battleMusicPmePath));
            }
            if (File.Exists(_bossMusicPmePath))
            {
                contentBuilder.AppendLine(File.ReadAllText(_bossMusicPmePath));
            }

            var content = contentBuilder.ToString();
            if (string.IsNullOrWhiteSpace(content)) return;

            var parsedConsts = new Dictionary<string, List<string>>();
            string normalVal = "default", advantageVal = "default", disadvantageVal = "default", bossVal = "default", victoryVal = "default";

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
                    var normalMusicRegex = new Regex(@"\bmusic\b\s*=\s*(?<normal>[^=\r\n]+)");
                    var normalMatch = normalMusicRegex.Match(blockContent);
                    if (normalMatch.Success)
                    {
                        normalVal = NormalizeValue(normalMatch.Groups["normal"].Value.Trim());
                        System.Diagnostics.Debug.WriteLine($"[BGME] Parsed normal music: {normalVal}");
                    }

                    // Parse normal_bgm (explicit normal context)
                    var normalBgmRegex = new Regex(@"\bnormal_bgm\b\s*=\s*(?<normal>[^=\r\n]+)");
                    var normalBgmMatch = normalBgmRegex.Match(blockContent);
                    if (normalBgmMatch.Success)
                    {
                        normalVal = NormalizeValue(normalBgmMatch.Groups["normal"].Value.Trim());
                        System.Diagnostics.Debug.WriteLine($"[BGME] Parsed normal_bgm music: {normalVal}");
                    }
                    // Parse advantage_bgm
                    var advantageRegex = new Regex(@"\badvantage_bgm\b\s*=\s*(?<advantage>[^=\r\n]+)");
                    var advantageMatch = advantageRegex.Match(blockContent);
                    if (advantageMatch.Success)
                    {
                        advantageVal = NormalizeValue(advantageMatch.Groups["advantage"].Value.Trim());
                        System.Diagnostics.Debug.WriteLine($"[BGME] Parsed advantage music: {advantageVal}");
                    }

                    // Parse disadvantage_bgm
                    var disadvantageRegex = new Regex(@"\bdisadvantage_bgm\b\s*=\s*(?<disadvantage>[^=\r\n]+)");
                    var disadvantageMatch = disadvantageRegex.Match(blockContent);
                    if (disadvantageMatch.Success)
                    {
                        disadvantageVal = NormalizeValue(disadvantageMatch.Groups["disadvantage"].Value.Trim());
                        System.Diagnostics.Debug.WriteLine($"[BGME] Parsed disadvantage music: {disadvantageVal}");
                    }
                }

                // Parse victory_music
                var victoryRegex = new Regex(@"\bvictory_music\b\s*=\s*(?<victory>[^=\r\n]+)");
                var victoryMatch = victoryRegex.Match(blockContent);
                if (victoryMatch.Success)
                {
                    victoryVal = NormalizeValue(victoryMatch.Groups["victory"].Value.Trim());
                    System.Diagnostics.Debug.WriteLine($"[BGME] Parsed victory music: {victoryVal}");
                }
            }

            // Parse Boss block
            var bossBlockRegex = new Regex(@"encounter\[""Special Battles""\]:(?<block>.*?)(end|\z)", RegexOptions.Singleline);
            var bossMatch = bossBlockRegex.Match(content);
            if (bossMatch.Success)
            {
                var bossBlock = bossMatch.Groups["block"].Value;
                var musicMatch = Regex.Match(bossBlock, @"music\s*=\s*(.+)");
                if (musicMatch.Success)
                {
                    bossVal = musicMatch.Groups[1].Value.Trim();
                }
            }

            // Debug output to verify parsed values
            System.Diagnostics.Debug.WriteLine($"[BGME] Parsed values - Normal: {normalVal}, Advantage: {advantageVal}, Disadvantage: {disadvantageVal}, Victory: {victoryVal}");

            // Update UI
            UpdateGroupUI(normalVal, lstNormal, radNormalSingle, radNormalRandom, parsedConsts);
            UpdateGroupUI(advantageVal, lstAdvantage, radAdvantageSingle, radAdvantageRandom, parsedConsts);
            UpdateGroupUI(disadvantageVal, lstDisadvantage, radDisadvantageSingle, radDisadvantageRandom, parsedConsts);
            UpdateGroupUI(bossVal, lstBoss, radBossSingle, radBossRandom, parsedConsts);
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
            string bossValue = ProcessSection(lstBoss, radBossRandom, "bossBgm", consts);
            if (bossValue == "default") bossValue = null;
            string victoryValue = ProcessSection(lstVictory, radVictoryRandom, "victoryBgm", consts);

            // Boss encounter block - moved earlier to handle deletion even when all other sections are default
            if (bossValue != null)
            {
                var bossScript = new StringBuilder();

                // copy consts to boss file as well
                foreach (var c in consts)
                {
                    bossScript.AppendLine($"const {c.Key} = random_song([{string.Join(", ", c.Value)}])");
                }
                if (consts.Any()) bossScript.AppendLine();

                bossScript.AppendLine("encounter[\"Special Battles\"]:");
                bossScript.AppendLine($"  music = {bossValue}");
                bossScript.AppendLine("end");

                try
                {
                    File.WriteAllText(_bossMusicPmePath, bossScript.ToString());
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Error saving boss settings: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
            else
            {
                // If previously saved but now cleared, wipe the boss_music.pme file so it's effectively empty
                try
                {
                    if (File.Exists(_bossMusicPmePath))
                    {
                        File.WriteAllText(_bossMusicPmePath, string.Empty);
                    }
                    else
                    {
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Error clearing boss settings: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }

            // Count how many are set
            int setCount = 0;
            if (normalValue != null) setCount++;
            if (advantageValue != null) setCount++;
            if (disadvantageValue != null) setCount++;
            // Note: We no longer count bossValue here since it's handled separately
            // if (bossValue != null) setCount++;

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
                // Normal and Advantage set, separate properties so Advantage doesn't force Normal context usage
                properties.Add($"normal_bgm = {normalValue}");
                properties.Add($"advantage_bgm = {advantageValue}");
            }
            else if (normalValue != null && advantageValue == null && disadvantageValue == null)
            {
                // Only Normal set
                properties.Add($"music = {normalValue}");
            }
            else
            {
                // For any that are set, but not covered above, use separate lines
                if (normalValue != null)
                {
                    // Use normal_bgm when any other context is set to avoid overriding logic
                    if (advantageValue != null || disadvantageValue != null)
                        properties.Add($"normal_bgm = {normalValue}");
                    else
                        properties.Add($"music = {normalValue}");
                }
                if (advantageValue != null)
                    properties.Add($"advantage_bgm = {advantageValue}");
                if (disadvantageValue != null)
                    properties.Add($"disadvantage_bgm = {disadvantageValue}");
            }

            // Boss value handled in separate encounter block below

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