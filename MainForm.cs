using BGMSelector.Models;
using BGMSelector.Services;
using System.ComponentModel;
using Microsoft.VisualBasic.FileIO; // For RecycleBin operations
using System.Diagnostics; // For Process
using System.Text;

namespace BGMSelector
{
    public partial class MainForm : Form
    {
        private readonly MusicService _musicService;
        private readonly HcaNameService _hcaNameService;
        private readonly DefaultTagsService _defaultTagsService;
        private MusicConfig? _musicConfig;
        private Dictionary<int, string> _currentAssignments = new Dictionary<int, string>();
        private readonly Color _personaBlue = Color.FromArgb(43, 87, 151);
        private readonly Color _darkBackground = Color.FromArgb(30, 30, 30);
        private readonly Color _lightText = Color.FromArgb(240, 240, 240);
        private string _hcaFolderPath;

        // Control fields
        private ListView trackListView;
        private ListView assignmentsListView;
        private ComboBox hcaFileComboBox;
        private TextBox hcaSearchBox;
        private Label selectedTrackValue;
        private TextBox searchBox;
        private ComboBox categoryFilter;
        private Button assignButton;
        private Button saveButton;
        private Button removeButton;
        private Button refreshHcaButton;
        private Panel dropPanel;
        private CheckBox enableMultiSelectCheckbox;
        private Button randomizeButton;

        public MainForm()
        {
            InitializeComponent();
            
            // Use paths relative to the mod root directory
            string baseDir = Path.GetDirectoryName(Application.ExecutablePath) ?? "";
            // Go up one directory if running from build folder
            if (Path.GetFileName(baseDir) == "build")
            {
                baseDir = Path.GetDirectoryName(baseDir) ?? "";
            }
            
            string yamlPath = Path.Combine(baseDir, "music.yaml");
            string pmePath = Path.Combine(baseDir, "global_music.pme");
            string battleMusicPmePath = Path.Combine(baseDir, "battle_music.pme");
            _hcaFolderPath = Path.Combine(baseDir, "p3r");
            string hcaNamesPath = Path.Combine(baseDir, "hca_names.json");
            string defaultTagsPath = Path.Combine(baseDir, "BGME", "default_tags.json");

            if (!File.Exists(defaultTagsPath))
            {
                string[] possiblePaths = {
                    Path.Combine(baseDir, "default_tags.json"),
                    Path.Combine(baseDir, "..", "BGME", "default_tags.json")
                };
                
                foreach (var path in possiblePaths)
                {
                    if (File.Exists(path))
                    {
                        defaultTagsPath = path;
                        break;
                    }
                }
            }

            _musicService = new MusicService(yamlPath, pmePath, _hcaFolderPath);
            _hcaNameService = new HcaNameService(hcaNamesPath);
            _defaultTagsService = new DefaultTagsService(defaultTagsPath);
            
            // Create empty battle_music.pme if it doesn't exist
            if (!File.Exists(battleMusicPmePath))
            {
                try
                {
                    // Create an empty file without comments
                    File.WriteAllText(battleMusicPmePath, "");
                }
                catch
                {
                    // Ignore error if we can't create the file
                }
            }
            
            // Apply Persona theme
            ApplyPersonaTheme();
        }

        private void InitializeComponent()
        {
            this.SuspendLayout();
            
            this.AutoScaleDimensions = new System.Drawing.SizeF(7F, 15F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(1100, 650);
            this.Name = "MainForm";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            this.Text = "Persona 3 Reload - BGM Selector";
            this.Load += new System.EventHandler(this.MainForm_Load);
            
            this.ResumeLayout(false);
        }

        private void MainForm_Load(object sender, EventArgs e)
        {
            try
            {
                // Load music configuration
                _musicConfig = _musicService.LoadMusicConfig();
                
                // Load current assignments
                _currentAssignments = _musicService.LoadCurrentPmeAssignments();
                
                // Populate track list
                PopulateTrackList();
                
                // Populate HCA files
                PopulateHcaFiles();
                
                // Update assignments list
                UpdateAssignmentsList();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error loading data: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
        
        private void ApplyPersonaTheme()
        {
            // Main form
            this.BackColor = _darkBackground;
            this.ForeColor = _lightText;
            this.Font = new Font("Arial", 10F, FontStyle.Regular);
            this.Text = "Persona 3 Reload - BGM Selector";
            
            // Initialize components with Persona theme
            InitializePersonaComponents();
        }
        
        private void InitializePersonaComponents()
        {
            this.SuspendLayout();
            
            // Create MenuStrip
            MenuStrip menuStrip = new MenuStrip
            {
                BackColor = Color.FromArgb(45, 45, 48),
                ForeColor = _lightText,
                Font = new Font("Arial", 9F),
                Padding = new Padding(3, 2, 0, 2),
                Dock = DockStyle.Top
            };

            ToolStripMenuItem modifyHcaMenuItem = new ToolStripMenuItem("Modify Selected HCA File");
            modifyHcaMenuItem.BackColor = _darkBackground;
            modifyHcaMenuItem.ForeColor = _lightText;
            modifyHcaMenuItem.Click += ModifyHcaMenuItem_Click;

            ToolStripMenuItem editHcaNameMenuItem = new ToolStripMenuItem("Edit HCA Custom Name");
            editHcaNameMenuItem.BackColor = _darkBackground;
            editHcaNameMenuItem.ForeColor = _lightText;
            editHcaNameMenuItem.Click += EditHcaNameMenuItem_Click;

            ToolStripMenuItem converterMenuItem = new ToolStripMenuItem("WAV to HCA Converter");
            converterMenuItem.BackColor = _darkBackground;
            converterMenuItem.ForeColor = _lightText;
            converterMenuItem.Click += ConverterMenuItem_Click;

            // Add Battle Music menu item
            ToolStripMenuItem battleMusicMenuItem = new ToolStripMenuItem("Battle Music Settings");
            battleMusicMenuItem.BackColor = _darkBackground;
            battleMusicMenuItem.ForeColor = _lightText;
            battleMusicMenuItem.Click += BattleMusicMenuItem_Click;

            menuStrip.Items.Add(modifyHcaMenuItem);
            menuStrip.Items.Add(editHcaNameMenuItem);
            menuStrip.Items.Add(converterMenuItem);
            menuStrip.Items.Add(battleMusicMenuItem);

            this.Controls.Add(menuStrip);
            
            // Create main container with top padding to create space after menustrip
            TableLayoutPanel mainContainer = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 2,
                RowCount = 1,
                Padding = new Padding(10, 15, 10, 10), // Added top padding
                BackColor = _darkBackground
            };
            
            // Adjust column ratio to give more space to the right panel
            mainContainer.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 40F)); // Left panel (smaller)
            mainContainer.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 60F)); // Right panel (larger)
            
            // Left panel - Track selection
            Panel leftPanel = new Panel
            {
                Dock = DockStyle.Fill,
                Padding = new Padding(10)
            };
            
            Label tracksLabel = new Label
            {
                Text = "Available Tracks",
                Font = new Font("Arial", 12F, FontStyle.Bold),
                ForeColor = _lightText,
                Dock = DockStyle.Top,
                Height = 30
            };
            
            TextBox searchBox = new TextBox
            {
                Dock = DockStyle.Top,
                Height = 30,
                BackColor = Color.FromArgb(50, 50, 50),
                ForeColor = _lightText,
                BorderStyle = BorderStyle.FixedSingle,
                PlaceholderText = "Search tracks..."
            };
            
            ComboBox categoryFilter = new ComboBox
            {
                Dock = DockStyle.Top,
                Height = 30,
                BackColor = Color.FromArgb(50, 50, 50),
                ForeColor = _lightText,
                FlatStyle = FlatStyle.Flat,
                DropDownStyle = ComboBoxStyle.DropDownList
            };
            
            ListView trackListView = new ListView
            {
                Dock = DockStyle.Fill,
                BackColor = Color.FromArgb(40, 40, 40),
                ForeColor = _lightText,
                BorderStyle = BorderStyle.FixedSingle,
                FullRowSelect = true,
                View = View.Details,
                HideSelection = false
            };
            
            trackListView.Columns.Add("Name", 200);
            trackListView.Columns.Add("Category", 100);
            trackListView.Columns.Add("Cue ID", 70);
            
            leftPanel.Controls.Add(trackListView);
            leftPanel.Controls.Add(categoryFilter);
            leftPanel.Controls.Add(searchBox);
            leftPanel.Controls.Add(tracksLabel);
            
            // Right panel - Assignments
            Panel rightPanel = new Panel
            {
                Dock = DockStyle.Fill,
                Padding = new Padding(10)
            };
            
            Label assignmentsLabel = new Label
            {
                Text = "Current Assignments",
                Font = new Font("Arial", 12F, FontStyle.Bold),
                ForeColor = _lightText,
                Dock = DockStyle.Top,
                Height = 30
            };
            
            ListView assignmentsListView = new ListView
            {
                Dock = DockStyle.Fill,
                BackColor = Color.FromArgb(40, 40, 40),
                ForeColor = _lightText,
                BorderStyle = BorderStyle.FixedSingle,
                FullRowSelect = true,
                View = View.Details,
                HideSelection = false
            };
            
            assignmentsListView.Columns.Add("Cue ID", 70);
            assignmentsListView.Columns.Add("Track Name", 200);
            assignmentsListView.Columns.Add("HCA File", 100);
            
            // --- New Layout for Assignment Controls ---
            var assignmentControlsLayout = new TableLayoutPanel
            {
                Dock = DockStyle.Bottom,
                Height = 190, 
                Padding = new Padding(10),
                BackColor = _darkBackground,
                ColumnCount = 3, 
                RowCount = 4 
            };

            // Define column styles
            assignmentControlsLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 110F));
            assignmentControlsLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            assignmentControlsLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 160F));

            // Define row styles
            assignmentControlsLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize)); // Selected Track
            assignmentControlsLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize)); // HCA Search
            assignmentControlsLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize)); // HCA ComboBox
            assignmentControlsLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 50F)); // Drop Panel / Checkbox

            // --- Create All Controls with full properties---
            var selectedTrackLabel = new Label { Text = "Selected Track:", Anchor = AnchorStyles.Left, AutoSize = true, ForeColor = _lightText, TextAlign = ContentAlignment.MiddleLeft };
            var selectedTrackValue = new Label { Text = "None", Dock = DockStyle.Fill, AutoEllipsis = true, ForeColor = _lightText, TextAlign = ContentAlignment.MiddleLeft };
            var hcaFileLabel = new Label { Text = "HCA File:", Anchor = AnchorStyles.Left, AutoSize = true, ForeColor = _lightText, TextAlign = ContentAlignment.MiddleLeft };
            var hcaSearchBox = new TextBox { Dock = DockStyle.Fill, PlaceholderText = "Search HCA files...", BackColor = Color.FromArgb(50, 50, 50), ForeColor = _lightText, BorderStyle = BorderStyle.FixedSingle };
            var hcaFileComboBox = new ComboBox { Dock = DockStyle.Fill, BackColor = Color.FromArgb(50, 50, 50), ForeColor = _lightText, FlatStyle = FlatStyle.Flat, DropDownStyle = ComboBoxStyle.DropDownList };
            
            var dropPanel = new Panel { Dock = DockStyle.Fill, AllowDrop = true, BackColor = Color.FromArgb(35, 35, 35), BorderStyle = BorderStyle.FixedSingle, Margin = new Padding(0, 10, 0, 0) };
            var dropLabel = new Label { Text = "Drop HCA files here", ForeColor = Color.Gray, TextAlign = ContentAlignment.MiddleCenter, Dock = DockStyle.Fill, Font = new Font("Arial", 8, FontStyle.Italic) };
            dropPanel.Controls.Add(dropLabel);
            
            var enableMultiSelectCheckbox = new CheckBox 
            { 
                Text = "Enable Multi-Select", 
                Font = new Font("Arial", 8F), // Smaller font
                AutoSize = true, 
                ForeColor = _lightText, 
                BackColor = Color.Transparent,
                Anchor = AnchorStyles.None // Center the checkbox in its cell
            };

            // --- Create Action Buttons Panel ---
            var actionButtonsPanel = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 2,
                RowCount = 4, // Added row for randomize button
                Margin = new Padding(10, 0, 0, 0)
            };
            actionButtonsPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50F));
            actionButtonsPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50F));
            actionButtonsPanel.RowStyles.Add(new RowStyle(SizeType.Absolute, 35F));
            actionButtonsPanel.RowStyles.Add(new RowStyle(SizeType.Absolute, 35F));
            actionButtonsPanel.RowStyles.Add(new RowStyle(SizeType.Absolute, 35F));
            actionButtonsPanel.RowStyles.Add(new RowStyle(SizeType.Absolute, 35F));

            var saveButton = new Button { Text = "Save", Dock = DockStyle.Fill, BackColor = _personaBlue, ForeColor = Color.White, FlatStyle = FlatStyle.Flat };
            var refreshHcaButton = new Button { Text = "↻", Dock = DockStyle.Fill, BackColor = Color.FromArgb(60, 60, 60), ForeColor = Color.White, FlatStyle = FlatStyle.Flat, Font = new Font("Arial", 10, FontStyle.Bold) };
            var assignButton = new Button { Text = "Assign", Dock = DockStyle.Fill, BackColor = _personaBlue, ForeColor = Color.White, FlatStyle = FlatStyle.Flat };
            var removeButton = new Button { Text = "Remove", Dock = DockStyle.Fill, BackColor = Color.FromArgb(70, 70, 70), ForeColor = Color.White, FlatStyle = FlatStyle.Flat };
            var randomizeButton = new Button { Text = "Randomize...", Dock = DockStyle.Fill, BackColor = Color.FromArgb(60, 100, 60), ForeColor = Color.White, FlatStyle = FlatStyle.Flat };
            
            actionButtonsPanel.Controls.Add(saveButton, 0, 0);
            actionButtonsPanel.Controls.Add(refreshHcaButton, 1, 0);
            actionButtonsPanel.Controls.Add(assignButton, 0, 1);
            actionButtonsPanel.SetColumnSpan(assignButton, 2);
            actionButtonsPanel.Controls.Add(removeButton, 0, 2);
            actionButtonsPanel.SetColumnSpan(removeButton, 2);
            actionButtonsPanel.Controls.Add(randomizeButton, 0, 3);
            actionButtonsPanel.SetColumnSpan(randomizeButton, 2);

            // --- Arrange Main Controls in Layout ---
            assignmentControlsLayout.Controls.Add(selectedTrackLabel, 0, 0);
            assignmentControlsLayout.Controls.Add(selectedTrackValue, 1, 0);
            
            assignmentControlsLayout.Controls.Add(hcaFileLabel, 0, 1);
            assignmentControlsLayout.SetRowSpan(hcaFileLabel, 2);
            assignmentControlsLayout.Controls.Add(hcaSearchBox, 1, 1);
            assignmentControlsLayout.Controls.Add(hcaFileComboBox, 1, 2);
            
            assignmentControlsLayout.Controls.Add(actionButtonsPanel, 2, 0);
            assignmentControlsLayout.SetRowSpan(actionButtonsPanel, 4); // Set RowSpan to 4 to include randomize button
            
            assignmentControlsLayout.Controls.Add(dropPanel, 0, 3);
            assignmentControlsLayout.SetColumnSpan(dropPanel, 2);
            
            assignmentControlsLayout.Controls.Add(enableMultiSelectCheckbox, 2, 3);
            
            rightPanel.Controls.Add(assignmentsListView);
            rightPanel.Controls.Add(assignmentControlsLayout);
            rightPanel.Controls.Add(assignmentsLabel);
            
            // Add panels to main container
            mainContainer.Controls.Add(leftPanel, 0, 0);
            mainContainer.Controls.Add(rightPanel, 1, 0);
            
            // Add main container to form
            this.Controls.Add(mainContainer);
            
            // Store controls as class members
            this.trackListView = trackListView;
            this.assignmentsListView = assignmentsListView;
            this.hcaFileComboBox = hcaFileComboBox;
            this.hcaSearchBox = hcaSearchBox;
            this.selectedTrackValue = selectedTrackValue;
            this.searchBox = searchBox;
            this.categoryFilter = categoryFilter;
            this.assignButton = assignButton;
            this.saveButton = saveButton;
            this.removeButton = removeButton;
            this.refreshHcaButton = refreshHcaButton;
            this.dropPanel = dropPanel;
            this.enableMultiSelectCheckbox = enableMultiSelectCheckbox;
            this.randomizeButton = randomizeButton;
            
            // Wire up events
            trackListView.SelectedIndexChanged += TrackListView_SelectedIndexChanged;
            assignmentsListView.SelectedIndexChanged += AssignmentsListView_SelectedIndexChanged;
            searchBox.TextChanged += SearchBox_TextChanged;
            hcaSearchBox.TextChanged += HcaSearchBox_TextChanged;
            categoryFilter.SelectedIndexChanged += CategoryFilter_SelectedIndexChanged;
            assignButton.Click += AssignButton_Click;
            saveButton.Click += SaveButton_Click;
            removeButton.Click += RemoveButton_Click;
            refreshHcaButton.Click += RefreshHcaButton_Click;
            enableMultiSelectCheckbox.CheckedChanged += EnableMultiSelectCheckbox_CheckedChanged;
            randomizeButton.Click += RandomizeButton_Click;
            
            // Wire up drop panel events
            dropPanel.DragEnter += DropPanel_DragEnter;
            dropPanel.DragDrop += DropPanel_DragDrop;
            dropPanel.DragLeave += DropPanel_DragLeave;
            
            this.ResumeLayout(false);
        }
        
        private void PopulateTrackList()
        {
            trackListView.Items.Clear();
            categoryFilter.Items.Clear();
            
            if (_musicConfig == null || _musicConfig.Tracks == null)
                return;
                
            // Add "All" category
            categoryFilter.Items.Add("All Categories");
            
            // Collect unique categories
            HashSet<string> categories = new HashSet<string>();
            
            foreach (var track in _musicConfig.Tracks)
            {
                if (!string.IsNullOrEmpty(track.Category))
                    categories.Add(track.Category);
                    
                var item = new ListViewItem(track.Name ?? "Unknown");
                item.SubItems.Add(track.Category ?? "");
                item.SubItems.Add(track.CueId.ToString());
                item.Tag = track;
                
                trackListView.Items.Add(item);
            }
            
            // Add categories to filter
            foreach (var category in categories.OrderBy(c => c))
            {
                categoryFilter.Items.Add(category);
            }
            
            // Select "All" by default
            categoryFilter.SelectedIndex = 0;
        }
        
        private void PopulateHcaFiles()
        {
            hcaFileComboBox.Items.Clear();
            
            try
            {
                var hcaFiles = _musicService.GetAvailableHcaFiles();
                foreach (var file in hcaFiles)
                {
                    string displayName = _hcaNameService.GetName(file);
                    if (displayName == file)
                    {
                        displayName = _defaultTagsService.GetDisplayName(file);
                    }

                    if (displayName != file)
                    {
                        hcaFileComboBox.Items.Add($"{displayName} ({file})");
                    }
                    else
                    {
                        hcaFileComboBox.Items.Add(file);
                    }
                }
                
                if (hcaFileComboBox.Items.Count > 0)
                    hcaFileComboBox.SelectedIndex = 0;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error loading HCA files: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
        
        private void UpdateAssignmentsList()
        {
            assignmentsListView.Items.Clear();
            
            // Get all unique cue IDs from both dictionaries
            var allCueIds = _currentAssignments.Keys.Union(_musicService.GetAllRandomizedCueIds()).Distinct().OrderBy(id => id);

            foreach (var cueId in allCueIds)
            {
                var item = new ListViewItem(cueId.ToString());
                
                string trackName = "Unknown";
                if (_musicConfig?.Tracks != null)
                {
                    var track = _musicConfig.Tracks.FirstOrDefault(t => t.CueId == cueId);
                    if (track != null)
                        trackName = track.Name ?? "Unknown";
                }
                item.SubItems.Add(trackName);

                if (_musicService.IsRandomized(cueId))
                {
                    var randomTrack = _musicService.GetRandomizedTrack(cueId);
                    item.SubItems.Add(randomTrack.ToString());
                    item.BackColor = Color.FromArgb(35, 50, 35); 
                }
                else if (_currentAssignments.TryGetValue(cueId, out var hcaFileName) && !string.IsNullOrEmpty(hcaFileName))
                {
                    string hcaDisplayName = _hcaNameService.GetName(hcaFileName);
                    item.SubItems.Add(hcaDisplayName != hcaFileName ? $"{hcaDisplayName} ({hcaFileName})" : hcaFileName);
                }
                else
                {
                    item.SubItems.Add(""); // No assignment
                }
                
                item.Tag = cueId;
                assignmentsListView.Items.Add(item);
            }
        }
        
        private void TrackListView_SelectedIndexChanged(object? sender, EventArgs e)
        {
            if (!enableMultiSelectCheckbox.Checked)
            {
                if (trackListView.SelectedItems.Count > 0)
                {
                    var selectedItem = trackListView.SelectedItems[0];
                    var track = selectedItem.Tag as MusicTrack;
                    
                    if (track != null)
                    {
                        selectedTrackValue.Text = $"{track.Name} (ID: {track.CueId})";
                        selectedTrackValue.Tag = track;
                    }
                }
            }
            else
            {
                // Update selection text for multi-select
                int selectedCount = trackListView.SelectedItems.Count;
                selectedTrackValue.Text = $"{selectedCount} track(s) selected";
            }
        }
        
        private void AssignmentsListView_SelectedIndexChanged(object? sender, EventArgs e)
        {
            if (!enableMultiSelectCheckbox.Checked)
            {
                if (assignmentsListView.SelectedItems.Count > 0)
                {
                    var selectedItem = assignmentsListView.SelectedItems[0];
                    int cueId = (int)selectedItem.Tag;

                    // Get the actual HCA filename from the assignment dictionary
                    string hcaFile = _currentAssignments.ContainsKey(cueId) ? _currentAssignments[cueId] : null;

                    if (hcaFile == null) return;

                    // Find track by cue ID
                    if (_musicConfig?.Tracks != null)
                    {
                        var track = _musicConfig.Tracks.FirstOrDefault(t => t.CueId == cueId);
                        if (track != null)
                        {
                            selectedTrackValue.Text = $"{track.Name} (ID: {track.CueId})";
                            selectedTrackValue.Tag = track;
                        }
                    }
                    
                    // Select HCA file in combo box by parsing the real filename
                    for (int i = 0; i < hcaFileComboBox.Items.Count; i++)
                    {
                        string itemText = hcaFileComboBox.Items[i].ToString();
                        string itemFileName = GetHcaFileNameFromComboBoxItem(itemText);

                        if (itemFileName == hcaFile)
                        {
                            hcaFileComboBox.SelectedIndex = i;
                            break;
                        }
                    }
                }
            }
            else
            {
                // Update selection text for multi-select
                int selectedCount = assignmentsListView.SelectedItems.Count;
                selectedTrackValue.Text = $"{selectedCount} assignment(s) selected";
            }
        }
        
        private void SearchBox_TextChanged(object? sender, EventArgs e)
        {
            FilterTracks();
        }
        
        private void CategoryFilter_SelectedIndexChanged(object? sender, EventArgs e)
        {
            FilterTracks();
        }

        private void HcaSearchBox_TextChanged(object? sender, EventArgs e)
        {
            FilterHcaFiles();
        }

        private void FilterHcaFiles()
        {
            string searchText = hcaSearchBox.Text.ToLower();
            string previouslySelectedFile = GetSelectedHcaFileName();

            hcaFileComboBox.BeginUpdate();
            hcaFileComboBox.Items.Clear();

            try
            {
                var allHcaFiles = _musicService.GetAvailableHcaFiles();
                
                foreach (var file in allHcaFiles)
                {
                    string customName = _hcaNameService.GetName(file);

                    // If search text is empty, or if it matches the filename or custom name, add it
                    if (string.IsNullOrEmpty(searchText) || 
                        file.ToLower().Contains(searchText) ||
                        (customName != file && customName.ToLower().Contains(searchText)))
                    {
                        if (customName != file)
                        {
                            hcaFileComboBox.Items.Add($"{customName} ({file})");
                        }
                        else
                        {
                            hcaFileComboBox.Items.Add(file);
                        }
                    }
                }
                
                // Restore previous selection if possible
                bool selectionRestored = false;
                if (previouslySelectedFile != null)
                {
                    for (int i = 0; i < hcaFileComboBox.Items.Count; i++)
                    {
                        if (GetHcaFileNameFromComboBoxItem(hcaFileComboBox.Items[i].ToString()) == previouslySelectedFile)
                        {
                            hcaFileComboBox.SelectedIndex = i;
                            selectionRestored = true;
                            break;
                        }
                    }
                }

                // If no selection was restored, select the first item.
                if (!selectionRestored && hcaFileComboBox.Items.Count > 0)
                {
                    hcaFileComboBox.SelectedIndex = 0;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error filtering HCA files: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                hcaFileComboBox.EndUpdate();
            }
        }
        
        private void FilterTracks()
        {
            if (_musicConfig?.Tracks == null)
                return;
                
            string searchText = searchBox.Text.ToLower();
            string selectedCategory = categoryFilter.SelectedIndex > 0 ? categoryFilter.SelectedItem.ToString() : null;
            
            trackListView.BeginUpdate();
            trackListView.Items.Clear();
            
            foreach (var track in _musicConfig.Tracks)
            {
                bool matchesSearch = string.IsNullOrEmpty(searchText) || 
                                     (track.Name?.ToLower().Contains(searchText) ?? false);
                                     
                bool matchesCategory = selectedCategory == null || 
                                       track.Category == selectedCategory;
                                       
                if (matchesSearch && matchesCategory)
                {
                    var item = new ListViewItem(track.Name ?? "Unknown");
                    item.SubItems.Add(track.Category ?? "");
                    item.SubItems.Add(track.CueId.ToString());
                    item.Tag = track;
                    
                    trackListView.Items.Add(item);
                }
            }
            
            trackListView.EndUpdate();
        }
        
        private void AssignButton_Click(object? sender, EventArgs e)
        {
            string hcaFile = GetSelectedHcaFileName();
            if (string.IsNullOrEmpty(hcaFile))
            {
                MessageBox.Show("Please select an HCA file to assign.", "Missing Selection", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            int assignedCount = 0;

            if (enableMultiSelectCheckbox.Checked)
            {
                // BULK MODE
                if (trackListView.SelectedItems.Count > 0)
                {
                    foreach (ListViewItem item in trackListView.SelectedItems)
                    {
                        if (item.Tag is MusicTrack track)
                        {
                            _currentAssignments[track.CueId] = hcaFile;
                            _musicService.RemoveRandomization(track.CueId); // Ensure it's not randomized
                            assignedCount++;
                        }
                    }
                }
                else if (assignmentsListView.SelectedItems.Count > 0)
                {
                    foreach (ListViewItem item in assignmentsListView.SelectedItems)
                    {
                        int cueId = (int)item.Tag;
                        _currentAssignments[cueId] = hcaFile;
                        _musicService.RemoveRandomization(cueId);
                        assignedCount++;
                    }
                }
            }
            else
            {
                // SINGLE MODE
                if (selectedTrackValue.Tag is MusicTrack track)
                {
                    _currentAssignments[track.CueId] = hcaFile;
                    _musicService.RemoveRandomization(track.CueId); // Ensure it's not randomized
                    assignedCount = 1;
                }
            }

            if (assignedCount > 0)
            {
                UpdateAssignmentsList();
                string message = assignedCount == 1 
                    ? "1 assignment updated." 
                    : $"{assignedCount} assignments updated.";
                MessageBox.Show(message, "Assign", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            else
            {
                MessageBox.Show("Please select a track or assignment(s) to process.", "No Selection", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }
        
        private void SaveButton_Click(object? sender, EventArgs e)
        {
            try
            {
                _musicService.UpdatePmeFile(_currentAssignments);
                
                // Change button text and color
                string originalText = saveButton.Text;
                Color originalForeColor = saveButton.ForeColor;
                Color originalBackColor = saveButton.BackColor;
                
                saveButton.Text = "Saved";
                saveButton.ForeColor = Color.LightGreen;
                
                // Reset button after 2 seconds
                System.Windows.Forms.Timer resetButtonTimer = new System.Windows.Forms.Timer();
                resetButtonTimer.Interval = 2000;
                resetButtonTimer.Tick += (s, args) => {
                    saveButton.Text = originalText;
                    saveButton.ForeColor = originalForeColor;
                    resetButtonTimer.Stop();
                    resetButtonTimer.Dispose();
                };
                resetButtonTimer.Start();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error saving changes: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
        
        private void RemoveButton_Click(object? sender, EventArgs e)
        {
            var cueIdsToRemove = new List<int>();

            if (enableMultiSelectCheckbox.Checked)
            {
                // BULK MODE
                if (assignmentsListView.SelectedItems.Count > 0)
                {
                    foreach (ListViewItem item in assignmentsListView.SelectedItems)
                    {
                        cueIdsToRemove.Add((int)item.Tag);
                    }
                }
            }
            else
            {
                // SINGLE MODE
                if (assignmentsListView.SelectedItems.Count > 0)
                {
                    cueIdsToRemove.Add((int)assignmentsListView.SelectedItems[0].Tag);
                }
            }

            if (cueIdsToRemove.Any())
            {
                int removedCount = 0;
                foreach (int cueId in cueIdsToRemove)
                {
                    if (_currentAssignments.Remove(cueId))
                    {
                        removedCount++;
                    }
                    _musicService.RemoveRandomization(cueId);
                }
                UpdateAssignmentsList();
                
                string message = removedCount == 1 
                    ? "1 assignment removed." 
                    : $"{removedCount} assignments removed.";
                MessageBox.Show(message, "Remove", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            else
            {
                MessageBox.Show("Please select assignment(s) to remove.", "No Selection", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }
        
        private void DropPanel_DragEnter(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                string[] files = (string[])e.Data.GetData(DataFormats.FileDrop);
                if (files.All(file => Path.GetExtension(file).ToLower() == ".hca"))
                {
                    e.Effect = DragDropEffects.Copy;
                    dropPanel.BackColor = Color.FromArgb(50, 80, 50); // Highlight when valid files
                }
                else
                {
                    e.Effect = DragDropEffects.None;
                    dropPanel.BackColor = Color.FromArgb(80, 50, 50); // Red when invalid files
                }
            }
        }
        
        private void DropPanel_DragDrop(object sender, DragEventArgs e)
        {
            // Reset panel color
            dropPanel.BackColor = Color.FromArgb(35, 35, 35);
            
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                string[] files = (string[])e.Data.GetData(DataFormats.FileDrop);
                
                // Filter for only HCA files
                var hcaFiles = files.Where(file => Path.GetExtension(file).ToLower() == ".hca").ToArray();
                
                if (hcaFiles.Length > 0)
                {
                    try
                    {
                        // Copy files to the p3r directory
                        foreach (string file in hcaFiles)
                        {
                            string fileName = Path.GetFileName(file);
                            string destPath = Path.Combine(_hcaFolderPath, fileName);
                            
                            // Check if file already exists
                            if (File.Exists(destPath))
                            {
                                DialogResult result = MessageBox.Show(
                                    $"File {fileName} already exists. Do you want to replace it?",
                                    "File Exists",
                                    MessageBoxButtons.YesNo,
                                    MessageBoxIcon.Question);
                                    
                                if (result == DialogResult.No)
                                    continue;
                            }
                            
                            // Copy the file
                            File.Copy(file, destPath, true);
                        }
                        
                        // Refresh the HCA file list
                        PopulateHcaFiles();
                        
                        // Show success message
                        string message = hcaFiles.Length == 1 
                            ? "1 file was added successfully." 
                            : $"{hcaFiles.Length} files were added successfully.";
                            
                        // Change button text to show success
                        string originalText = refreshHcaButton.Text;
                        Color originalForeColor = refreshHcaButton.ForeColor;
                        
                        refreshHcaButton.Text = "✓";
                        refreshHcaButton.ForeColor = Color.LightGreen;
                        
                        // Reset button after 2 seconds
                        System.Windows.Forms.Timer resetButtonTimer = new System.Windows.Forms.Timer();
                        resetButtonTimer.Interval = 2000;
                        resetButtonTimer.Tick += (s, args) => {
                            refreshHcaButton.Text = originalText;
                            refreshHcaButton.ForeColor = originalForeColor;
                            resetButtonTimer.Stop();
                            resetButtonTimer.Dispose();
                        };
                        resetButtonTimer.Start();
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show($"Error adding files: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                }
            }
        }
        
        private void DropPanel_DragLeave(object sender, EventArgs e)
        {
            // Reset panel color
            dropPanel.BackColor = Color.FromArgb(35, 35, 35);
        }
        
        private void RefreshHcaButton_Click(object sender, EventArgs e)
        {
            try
            {
                PopulateHcaFiles();
                
                // Visual feedback
                string originalText = refreshHcaButton.Text;
                refreshHcaButton.Text = "✓";
                refreshHcaButton.ForeColor = Color.LightGreen;
                
                // Reset button after 1 second
                System.Windows.Forms.Timer resetButtonTimer = new System.Windows.Forms.Timer();
                resetButtonTimer.Interval = 1000;
                resetButtonTimer.Tick += (s, args) => {
                    refreshHcaButton.Text = originalText;
                    refreshHcaButton.ForeColor = Color.White;
                    resetButtonTimer.Stop();
                    resetButtonTimer.Dispose();
                };
                resetButtonTimer.Start();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error refreshing HCA files: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
        
        private void EnableMultiSelectCheckbox_CheckedChanged(object? sender, EventArgs e)
        {
            bool multiSelectEnabled = enableMultiSelectCheckbox.Checked;
            
            // Update selection mode for both list views
            trackListView.MultiSelect = multiSelectEnabled;
            assignmentsListView.MultiSelect = multiSelectEnabled;
            
            // Update button text to reflect the mode
            assignButton.Text = multiSelectEnabled ? "Assign Selected" : "Assign";
            removeButton.Text = multiSelectEnabled ? "Remove Selected" : "Remove";

            // Update selection status text
            if (!multiSelectEnabled)
            {
                selectedTrackValue.Text = "None";
                selectedTrackValue.Tag = null;
                
                // Clear selections when disabling multi-select
                trackListView.SelectedItems.Clear();
                assignmentsListView.SelectedItems.Clear();
            }
            else
            {
                selectedTrackValue.Text = "Multiple selection enabled";
            }
        }

        private void RandomizeButton_Click(object? sender, EventArgs e)
        {
            if (trackListView.SelectedItems.Count == 0 && assignmentsListView.SelectedItems.Count == 0)
            {
                MessageBox.Show("Please select a track or an existing assignment to randomize.", "No Selection", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            int cueId;
            if (trackListView.SelectedItems.Count > 0) 
            {
                 var track = trackListView.SelectedItems[0].Tag as MusicTrack;
                 if (track == null) return;
                 cueId = track.CueId;
            }
            else
            {
                cueId = (int)assignmentsListView.SelectedItems[0].Tag;
            }

            var randomTrack = _musicService.GetRandomizedTrack(cueId);
            
            using (var dialog = new Form())
            {
                dialog.Text = "Set Random BGM";
                dialog.ClientSize = new Size(400, 450);
                dialog.StartPosition = FormStartPosition.CenterParent;
                dialog.BackColor = _darkBackground;
                dialog.ForeColor = _lightText;
                dialog.Padding = new Padding(10);

                var mainLayout = new TableLayoutPanel
                {
                    Dock = DockStyle.Fill,
                    RowCount = 3,
                    ColumnCount = 1,
                };
                mainLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
                mainLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
                mainLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 40F));

                var list = new CheckedListBox 
                { 
                    Dock = DockStyle.Fill, 
                    BackColor = _darkBackground, 
                    ForeColor = _lightText,
                    BorderStyle = BorderStyle.FixedSingle,
                    Margin = new Padding(0, 5, 0, 5)
                };

                var allHcaFiles = _musicService.GetAvailableHcaFiles().OrderBy(f => f).ToList();
                foreach(var file in allHcaFiles)
                {
                    string displayName = _hcaNameService.GetName(file);
                    list.Items.Add(displayName == file ? file : $"{displayName} ({file})", randomTrack.HcaFiles.Contains(file));
                }

                var includeDefault = new CheckBox 
                { 
                    Text = "Include Default Game BGM", 
                    Checked = randomTrack.IncludeDefault, 
                    AutoSize = true,
                    Dock = DockStyle.Fill
                };

                var okButton = new Button { Text = "OK", DialogResult = DialogResult.OK, Dock = DockStyle.Right, Width = 80 };
                var cancelButton = new Button { Text = "Cancel", DialogResult = DialogResult.Cancel, Dock = DockStyle.Right, Width = 80 };
                
                var buttonPanel = new Panel { Dock = DockStyle.Fill };
                buttonPanel.Controls.Add(okButton);
                buttonPanel.Controls.Add(cancelButton);

                mainLayout.Controls.Add(includeDefault, 0, 0);
                mainLayout.Controls.Add(list, 0, 1);
                mainLayout.Controls.Add(buttonPanel, 0, 2);

                dialog.Controls.Add(mainLayout);

                if (dialog.ShowDialog(this) == DialogResult.OK)
                {
                    var selectedHcaFiles = new List<string>();
                    for(int i = 0; i < list.Items.Count; i++)
                    {
                        if(list.GetItemChecked(i))
                        {
                            selectedHcaFiles.Add(allHcaFiles[i]);
                        }
                    }

                    _musicService.SetRandomizedTrack(cueId, selectedHcaFiles, includeDefault.Checked);
                    
                    // If the assignment becomes non-random, update the main dictionary, otherwise clear it
                    var newRandomTrack = _musicService.GetRandomizedTrack(cueId);
                    if (!newRandomTrack.IsRandomized)
                    {
                        _musicService.RemoveRandomization(cueId);
                        _currentAssignments[cueId] = selectedHcaFiles.FirstOrDefault() ?? "";
                    }
                    else
                    {
                         _currentAssignments.Remove(cueId);
                    }
                    
                    UpdateAssignmentsList();
                }
            }
        }

        private void ModifyHcaMenuItem_Click(object? sender, EventArgs e)
        {
            string hcaFile = GetSelectedHcaFileName();
            if (string.IsNullOrEmpty(hcaFile))
            {
                MessageBox.Show("Please select an HCA file to modify.", "No Selection", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            
            string fullPath = Path.Combine(_hcaFolderPath, hcaFile);
            
            if (!File.Exists(fullPath))
            {
                MessageBox.Show($"File not found: {hcaFile}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }
            
            // Create and show the modify HCA dialog
            using (var modifyForm = new Form())
            {
                modifyForm.Width = 300;
                modifyForm.Height = 200;
                modifyForm.FormBorderStyle = FormBorderStyle.FixedDialog;
                modifyForm.Text = "Modify HCA File";
                modifyForm.StartPosition = FormStartPosition.CenterParent;
                modifyForm.BackColor = _darkBackground;
                modifyForm.ForeColor = _lightText;
                modifyForm.MaximizeBox = false;
                modifyForm.MinimizeBox = false;
                
                var fileLabel = new Label
                {
                    Text = "Selected File:",
                    Left = 20,
                    Top = 20,
                    Width = 100,
                    ForeColor = _lightText
                };
                
                var fileNameLabel = new Label
                {
                    Text = hcaFile,
                    Left = 120,
                    Top = 20,
                    Width = 160,
                    ForeColor = _lightText
                };
                
                var renameButton = new Button
                {
                    Text = "Rename",
                    Left = 20,
                    Top = 60,
                    Width = 120,
                    Height = 40,
                    BackColor = _personaBlue,
                    ForeColor = Color.White,
                    FlatStyle = FlatStyle.Flat
                };
                
                var deleteButton = new Button
                {
                    Text = "Delete",
                    Left = 160,
                    Top = 60,
                    Width = 120,
                    Height = 40,
                    BackColor = Color.FromArgb(180, 60, 60),
                    ForeColor = Color.White,
                    FlatStyle = FlatStyle.Flat
                };
                
                var closeButton = new Button
                {
                    Text = "Close",
                    Left = 100,
                    Top = 120,
                    Width = 100,
                    Height = 30,
                    BackColor = Color.FromArgb(60, 60, 60),
                    ForeColor = Color.White,
                    FlatStyle = FlatStyle.Flat,
                    DialogResult = DialogResult.Cancel
                };
                
                renameButton.Click += (s, args) => RenameHcaFile(hcaFile, fullPath, modifyForm);
                deleteButton.Click += (s, args) => DeleteHcaFile(hcaFile, fullPath, modifyForm);
                
                modifyForm.Controls.Add(fileLabel);
                modifyForm.Controls.Add(fileNameLabel);
                modifyForm.Controls.Add(renameButton);
                modifyForm.Controls.Add(deleteButton);
                modifyForm.Controls.Add(closeButton);
                
                modifyForm.CancelButton = closeButton;
                
                modifyForm.ShowDialog();
            }
        }
        
        private void DeleteHcaFile(string hcaFile, string fullPath, Form parentForm)
        {
            // Confirm deletion
            DialogResult result = MessageBox.Show(
                $"Are you sure you want to delete {hcaFile}?\n\nThis will also remove all assignments using this file.",
                "Confirm Delete",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Warning);
                
            if (result == DialogResult.Yes)
            {
                try
                {
                    // Find all assignments using this HCA file
                    var assignmentsToRemove = _currentAssignments
                        .Where(a => a.Value == hcaFile)
                        .Select(a => a.Key)
                        .ToList();
                    
                    // Remove the assignments
                    foreach (int cueId in assignmentsToRemove)
                    {
                        _currentAssignments.Remove(cueId);
                    }
                    
                    // Move the file to recycle bin
                    FileSystem.DeleteFile(fullPath, UIOption.OnlyErrorDialogs, RecycleOption.SendToRecycleBin);
                    
                    // Update the UI
                    PopulateHcaFiles();
                    UpdateAssignmentsList();
                    
                    // Show feedback
                    string message = assignmentsToRemove.Count == 0
                        ? $"File {hcaFile} deleted successfully."
                        : $"File {hcaFile} deleted and {assignmentsToRemove.Count} assignment(s) removed.";
                        
                    MessageBox.Show(message, "Delete Successful", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    
                    // Close the parent form
                    parentForm.DialogResult = DialogResult.OK;
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Error deleting file: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
        }
        
        private void RenameHcaFile(string currentHcaFile, string fullPath, Form parentForm)
        {
            // Show input dialog for new name
            using (var inputForm = new Form())
            {
                inputForm.Width = 400;
                inputForm.Height = 150;
                inputForm.FormBorderStyle = FormBorderStyle.FixedDialog;
                inputForm.Text = "Rename HCA File";
                inputForm.StartPosition = FormStartPosition.CenterParent;
                inputForm.BackColor = _darkBackground;
                inputForm.ForeColor = _lightText;
                inputForm.MaximizeBox = false;
                inputForm.MinimizeBox = false;
                
                var label = new Label
                {
                    Text = "Enter new filename (with .hca extension):",
                    Left = 20,
                    Top = 20,
                    Width = 360,
                    ForeColor = _lightText
                };
                
                var textBox = new TextBox
                {
                    Text = currentHcaFile,
                    Left = 20,
                    Top = 50,
                    Width = 360,
                    BackColor = Color.FromArgb(50, 50, 50),
                    ForeColor = _lightText
                };
                
                var okButton = new Button
                {
                    Text = "OK",
                    Left = 200,
                    Top = 80,
                    Width = 80,
                    BackColor = _personaBlue,
                    ForeColor = Color.White,
                    FlatStyle = FlatStyle.Flat,
                    DialogResult = DialogResult.OK
                };
                
                var cancelButton = new Button
                {
                    Text = "Cancel",
                    Left = 300,
                    Top = 80,
                    Width = 80,
                    BackColor = Color.FromArgb(70, 70, 70),
                    ForeColor = Color.White,
                    FlatStyle = FlatStyle.Flat,
                    DialogResult = DialogResult.Cancel
                };
                
                inputForm.Controls.Add(label);
                inputForm.Controls.Add(textBox);
                inputForm.Controls.Add(okButton);
                inputForm.Controls.Add(cancelButton);
                
                inputForm.AcceptButton = okButton;
                inputForm.CancelButton = cancelButton;
                
                var result = inputForm.ShowDialog();
                
                if (result == DialogResult.OK)
                {
                    string newFileName = textBox.Text.Trim();
                    
                    // Validate new filename
                    if (string.IsNullOrEmpty(newFileName))
                    {
                        MessageBox.Show("Filename cannot be empty.", "Invalid Filename", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                        return;
                    }
                    
                    if (!newFileName.EndsWith(".hca", StringComparison.OrdinalIgnoreCase))
                    {
                        newFileName += ".hca";
                    }
                    
                    if (newFileName == currentHcaFile)
                    {
                        return; // No change
                    }
                    
                    string newPath = Path.Combine(_hcaFolderPath, newFileName);
                    
                    // Check if target file already exists
                    if (File.Exists(newPath))
                    {
                        MessageBox.Show($"A file named '{newFileName}' already exists.", "File Exists", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                        return;
                    }
                    
                    try
                    {
                        // Rename the file
                        File.Move(fullPath, newPath);
                        
                        // Update all assignments using this file
                        foreach (var key in _currentAssignments.Keys.ToList())
                        {
                            if (_currentAssignments[key] == currentHcaFile)
                            {
                                _currentAssignments[key] = newFileName;
                            }
                        }
                        
                        // Update the UI
                        PopulateHcaFiles();
                        UpdateAssignmentsList();
                        
                        // Select the renamed file in the combo box
                        for (int i = 0; i < hcaFileComboBox.Items.Count; i++)
                        {
                            if (hcaFileComboBox.Items[i].ToString() == newFileName)
                            {
                                hcaFileComboBox.SelectedIndex = i;
                                break;
                            }
                        }
                        
                        MessageBox.Show($"File renamed successfully to {newFileName}", "Rename Successful", MessageBoxButtons.OK, MessageBoxIcon.Information);
                        
                        // Close the parent form
                        parentForm.DialogResult = DialogResult.OK;
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show($"Error renaming file: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                }
            }
        }

        private void EditHcaNameMenuItem_Click(object? sender, EventArgs e)
        {
            string hcaFile = GetSelectedHcaFileName();
            if (string.IsNullOrEmpty(hcaFile))
            {
                MessageBox.Show("Please select an HCA file to edit its name.", "No Selection", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            string currentName = _hcaNameService.GetName(hcaFile);
            if (currentName == hcaFile) currentName = ""; // Show empty if no custom name

            // Show input dialog for new name
            using (var inputForm = new Form())
            {
                inputForm.Width = 400;
                inputForm.Height = 150;
                inputForm.FormBorderStyle = FormBorderStyle.FixedDialog;
                inputForm.Text = "Edit HCA Custom Name";
                inputForm.StartPosition = FormStartPosition.CenterParent;
                inputForm.BackColor = _darkBackground;
                inputForm.ForeColor = _lightText;
                inputForm.MaximizeBox = false;
                inputForm.MinimizeBox = false;
                
                var label = new Label
                {
                    Text = $"Enter custom name for {hcaFile}:",
                    Left = 20,
                    Top = 20,
                    Width = 360,
                    ForeColor = _lightText
                };
                
                var textBox = new TextBox
                {
                    Text = currentName,
                    Left = 20,
                    Top = 50,
                    Width = 360,
                    BackColor = Color.FromArgb(50, 50, 50),
                    ForeColor = _lightText
                };
                
                var okButton = new Button
                {
                    Text = "OK",
                    Left = 200,
                    Top = 80,
                    Width = 80,
                    BackColor = _personaBlue,
                    ForeColor = Color.White,
                    FlatStyle = FlatStyle.Flat,
                    DialogResult = DialogResult.OK
                };
                
                var cancelButton = new Button
                {
                    Text = "Cancel",
                    Left = 300,
                    Top = 80,
                    Width = 80,
                    BackColor = Color.FromArgb(70, 70, 70),
                    ForeColor = Color.White,
                    FlatStyle = FlatStyle.Flat,
                    DialogResult = DialogResult.Cancel
                };
                
                inputForm.Controls.Add(label);
                inputForm.Controls.Add(textBox);
                inputForm.Controls.Add(okButton);
                inputForm.Controls.Add(cancelButton);
                
                inputForm.AcceptButton = okButton;
                inputForm.CancelButton = cancelButton;
                
                var result = inputForm.ShowDialog();
                
                if (result == DialogResult.OK)
                {
                    string newCustomName = textBox.Text.Trim();
                    if (!string.IsNullOrEmpty(newCustomName))
                    {
                        _hcaNameService.SetName(hcaFile, newCustomName);
                    }
                    else
                    {
                        _hcaNameService.RemoveName(hcaFile);
                    }
                    PopulateHcaFiles();
                    UpdateAssignmentsList();
                    inputForm.DialogResult = DialogResult.OK;
                }
            }
        }

        private void ConverterMenuItem_Click(object? sender, EventArgs e)
        {
            // Create and show the converter dialog
            using (var converterForm = new Form())
            {
                converterForm.Width = 700;
                converterForm.Height = 500;
                converterForm.FormBorderStyle = FormBorderStyle.Sizable;
                converterForm.Text = "WAV to HCA Converter";
                converterForm.StartPosition = FormStartPosition.CenterParent;
                converterForm.BackColor = _darkBackground;
                converterForm.ForeColor = _lightText;
                converterForm.MinimizeBox = true;
                converterForm.MaximizeBox = true;
                converterForm.Icon = this.Icon;

                // Create layout
                TableLayoutPanel mainLayout = new TableLayoutPanel
                {
                    Dock = DockStyle.Fill,
                    ColumnCount = 1,
                    RowCount = 3,
                    Padding = new Padding(10),
                    BackColor = _darkBackground
                };

                mainLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 100));
                mainLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
                mainLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 50));

                // Drop panel for WAV files
                Panel dropPanel = new Panel
                {
                    Dock = DockStyle.Fill,
                    AllowDrop = true,
                    BackColor = Color.FromArgb(35, 35, 35),
                    BorderStyle = BorderStyle.FixedSingle
                };

                Label dropLabel = new Label
                {
                    Text = "Drop WAV files here or click to select files",
                    ForeColor = Color.Gray,
                    TextAlign = ContentAlignment.MiddleCenter,
                    Dock = DockStyle.Fill,
                    Font = new Font("Arial", 12, FontStyle.Regular)
                };

                dropPanel.Controls.Add(dropLabel);

                // ListView for files
                ListView fileListView = new ListView
                {
                    Dock = DockStyle.Fill,
                    BackColor = Color.FromArgb(40, 40, 40),
                    ForeColor = _lightText,
                    BorderStyle = BorderStyle.FixedSingle,
                    FullRowSelect = true,
                    CheckBoxes = true,
                    View = View.Details,
                    HideSelection = false
                };

                fileListView.Columns.Add("File Name", 250);
                fileListView.Columns.Add("Status", 100);
                fileListView.Columns.Add("Output HCA", 150);
                fileListView.Columns.Add("Size", 80);

                // Buttons panel
                TableLayoutPanel buttonsPanel = new TableLayoutPanel
                {
                    Dock = DockStyle.Fill,
                    ColumnCount = 4,
                    RowCount = 1
                };

                buttonsPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 25));
                buttonsPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 25));
                buttonsPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 25));
                buttonsPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 25));

                Button selectAllButton = new Button
                {
                    Text = "Select All",
                    Dock = DockStyle.Fill,
                    BackColor = Color.FromArgb(60, 60, 60),
                    ForeColor = Color.White,
                    FlatStyle = FlatStyle.Flat
                };

                Button deselectAllButton = new Button
                {
                    Text = "Deselect All",
                    Dock = DockStyle.Fill,
                    BackColor = Color.FromArgb(60, 60, 60),
                    ForeColor = Color.White,
                    FlatStyle = FlatStyle.Flat
                };

                Button convertButton = new Button
                {
                    Text = "Convert Selected",
                    Dock = DockStyle.Fill,
                    BackColor = _personaBlue,
                    ForeColor = Color.White,
                    FlatStyle = FlatStyle.Flat
                };

                Button importButton = new Button
                {
                    Text = "Import Selected to P3R",
                    Dock = DockStyle.Fill,
                    BackColor = _personaBlue,
                    ForeColor = Color.White,
                    FlatStyle = FlatStyle.Flat,
                    Enabled = false // Initially disabled until files are converted
                };

                buttonsPanel.Controls.Add(selectAllButton, 0, 0);
                buttonsPanel.Controls.Add(deselectAllButton, 1, 0);
                buttonsPanel.Controls.Add(convertButton, 2, 0);
                buttonsPanel.Controls.Add(importButton, 3, 0);

                // Add controls to main layout
                mainLayout.Controls.Add(dropPanel, 0, 0);
                mainLayout.Controls.Add(fileListView, 0, 1);
                mainLayout.Controls.Add(buttonsPanel, 0, 2);

                converterForm.Controls.Add(mainLayout);

                // Dictionary to track converted files
                Dictionary<string, string> convertedFiles = new Dictionary<string, string>();

                // Event handlers
                dropPanel.DragEnter += (s, e) => {
                    if (e.Data.GetDataPresent(DataFormats.FileDrop))
                    {
                        string[] files = (string[])e.Data.GetData(DataFormats.FileDrop);
                        if (files.All(file => Path.GetExtension(file).ToLower() == ".wav"))
                        {
                            e.Effect = DragDropEffects.Copy;
                            dropPanel.BackColor = Color.FromArgb(50, 80, 50);
                        }
                        else
                        {
                            e.Effect = DragDropEffects.None;
                            dropPanel.BackColor = Color.FromArgb(80, 50, 50);
                        }
                    }
                };

                dropPanel.DragLeave += (s, e) => {
                    dropPanel.BackColor = Color.FromArgb(35, 35, 35);
                };

                dropPanel.DragDrop += (s, e) => {
                    dropPanel.BackColor = Color.FromArgb(35, 35, 35);
                    
                    if (e.Data.GetDataPresent(DataFormats.FileDrop))
                    {
                        string[] files = (string[])e.Data.GetData(DataFormats.FileDrop);
                        var wavFiles = files.Where(file => Path.GetExtension(file).ToLower() == ".wav").ToArray();
                        
                        if (wavFiles.Length > 0)
                        {
                            AddFilesToList(wavFiles);
                        }
                    }
                };

                dropPanel.Click += (s, e) => {
                    using (OpenFileDialog openFileDialog = new OpenFileDialog())
                    {
                        openFileDialog.Filter = "WAV files (*.wav)|*.wav|All files (*.*)|*.*";
                        openFileDialog.Multiselect = true;

                        if (openFileDialog.ShowDialog() == DialogResult.OK)
                        {
                            AddFilesToList(openFileDialog.FileNames);
                        }
                    }
                };

                selectAllButton.Click += (s, e) => {
                    foreach (ListViewItem item in fileListView.Items)
                    {
                        item.Checked = true;
                    }
                };

                deselectAllButton.Click += (s, e) => {
                    foreach (ListViewItem item in fileListView.Items)
                    {
                        item.Checked = false;
                    }
                };

                convertButton.Click += (s, e) => {
                    List<ListViewItem> itemsToConvert = new List<ListViewItem>();
                    
                    foreach (ListViewItem item in fileListView.Items)
                    {
                        if (item.Checked && item.SubItems[1].Text != "Converted")
                        {
                            itemsToConvert.Add(item);
                        }
                    }
                    
                    if (itemsToConvert.Count == 0)
                    {
                        MessageBox.Show("Please select files to convert.", "No Selection", MessageBoxButtons.OK, MessageBoxIcon.Information);
                        return;
                    }
                    
                    // Find VGAudioCli.exe
                    string baseDir = Path.GetDirectoryName(Application.ExecutablePath) ?? "";
                    string vgAudioPath = Path.Combine(baseDir, "VGAudioCli.exe");
                    
                    if (!File.Exists(vgAudioPath))
                    {
                        // Try to find it in common locations
                        string[] possiblePaths = {
                            Path.Combine(baseDir, "tools", "VGAudioCli.exe"),
                            Path.Combine(baseDir, "..", "tools", "VGAudioCli.exe"),
                            Path.Combine(baseDir, "converter", "VGAudioCli.exe")
                        };
                        
                        foreach (string path in possiblePaths)
                        {
                            if (File.Exists(path))
                            {
                                vgAudioPath = path;
                                break;
                            }
                        }
                        
                        if (!File.Exists(vgAudioPath))
                        {
                            // Ask user to locate VGAudioCli.exe
                            using (OpenFileDialog dialog = new OpenFileDialog())
                            {
                                dialog.Title = "Locate VGAudioCli.exe";
                                dialog.Filter = "VGAudioCli.exe|VGAudioCli.exe";
                                
                                if (dialog.ShowDialog() == DialogResult.OK)
                                {
                                    vgAudioPath = dialog.FileName;
                                }
                                else
                                {
                                    MessageBox.Show("VGAudioCli.exe is required for conversion.", "Tool Not Found", MessageBoxButtons.OK, MessageBoxIcon.Error);
                                    return;
                                }
                            }
                        }
                    }
                    
                    // Create temp directory for output
                    string tempDir = Path.Combine(Path.GetTempPath(), "BGMEConverter");
                    if (!Directory.Exists(tempDir))
                    {
                        Directory.CreateDirectory(tempDir);
                    }
                    
                    // Process each file
                    foreach (ListViewItem item in itemsToConvert)
                    {
                        string inputFile = item.Tag.ToString();
                        string outputFileName = Path.GetFileNameWithoutExtension(inputFile) + ".hca";
                        string outputFile = Path.Combine(tempDir, outputFileName);
                        
                        // Update status
                        item.SubItems[1].Text = "Converting...";
                        item.SubItems[2].Text = outputFileName;
                        fileListView.Update();
                        
                        try
                        {
                            // Run VGAudioCli
                            ProcessStartInfo psi = new ProcessStartInfo
                            {
                                FileName = vgAudioPath,
                                Arguments = $"\"{inputFile}\" -o \"{outputFile}\"",
                                UseShellExecute = false,
                                CreateNoWindow = true,
                                RedirectStandardOutput = true,
                                RedirectStandardError = true
                            };
                            
                            using (Process process = Process.Start(psi))
                            {
                                process.WaitForExit();
                                
                                if (process.ExitCode == 0 && File.Exists(outputFile))
                                {
                                    // Update status and file info
                                    item.SubItems[1].Text = "Converted";
                                    
                                    // Get file size
                                    long fileSize = new FileInfo(outputFile).Length;
                                    string sizeText = fileSize < 1024 * 1024 
                                        ? $"{fileSize / 1024} KB" 
                                        : $"{fileSize / (1024 * 1024)} MB";
                                    
                                    item.SubItems[3].Text = sizeText;
                                    
                                    // Store converted file path
                                    convertedFiles[inputFile] = outputFile;
                                }
                                else
                                {
                                    item.SubItems[1].Text = "Failed";
                                }
                            }
                        }
                        catch (Exception ex)
                        {
                            item.SubItems[1].Text = "Error";
                            MessageBox.Show($"Error converting {Path.GetFileName(inputFile)}: {ex.Message}", "Conversion Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        }
                    }
                    
                    // Enable import button if any files were converted
                    importButton.Enabled = convertedFiles.Count > 0;
                };

                importButton.Click += (s, e) => {
                    List<string> filesToImport = new List<string>();
                    
                    foreach (ListViewItem item in fileListView.Items)
                    {
                        string inputFile = item.Tag.ToString();
                        
                        if (item.Checked && convertedFiles.ContainsKey(inputFile))
                        {
                            filesToImport.Add(convertedFiles[inputFile]);
                        }
                    }
                    
                    if (filesToImport.Count == 0)
                    {
                        MessageBox.Show("Please select converted files to import.", "No Selection", MessageBoxButtons.OK, MessageBoxIcon.Information);
                        return;
                    }
                    
                    // Copy files to p3r directory
                    int importedCount = 0;
                    
                    foreach (string file in filesToImport)
                    {
                        string fileName = Path.GetFileName(file);
                        string destPath = Path.Combine(_hcaFolderPath, fileName);
                        
                        try
                        {
                            // Check if file already exists
                            if (File.Exists(destPath))
                            {
                                DialogResult result = MessageBox.Show(
                                    $"File {fileName} already exists. Do you want to replace it?",
                                    "File Exists",
                                    MessageBoxButtons.YesNo,
                                    MessageBoxIcon.Question);
                                    
                                if (result == DialogResult.No)
                                    continue;
                            }
                            
                            // Copy the file
                            File.Copy(file, destPath, true);
                            importedCount++;
                        }
                        catch (Exception ex)
                        {
                            MessageBox.Show($"Error importing {fileName}: {ex.Message}", "Import Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        }
                    }
                    
                    // Show success message and refresh HCA files
                    if (importedCount > 0)
                    {
                        string message = importedCount == 1 
                            ? "1 file was imported successfully." 
                            : $"{importedCount} files were imported successfully.";
                            
                        MessageBox.Show(message, "Import Successful", MessageBoxButtons.OK, MessageBoxIcon.Information);
                        
                        // Refresh HCA files in the main form
                        PopulateHcaFiles();
                    }
                };

                // Helper function to add files to the list
                void AddFilesToList(string[] files)
                {
                    foreach (string file in files)
                    {
                        // Check if file is already in the list
                        bool alreadyExists = false;
                        foreach (ListViewItem existingItem in fileListView.Items)
                        {
                            if (existingItem.Tag.ToString() == file)
                            {
                                alreadyExists = true;
                                break;
                            }
                        }
                        
                        if (!alreadyExists)
                        {
                            ListViewItem item = new ListViewItem(Path.GetFileName(file));
                            item.SubItems.Add("Ready");
                            item.SubItems.Add("");
                            item.SubItems.Add("");
                            item.Tag = file;
                            item.Checked = true;
                            
                            fileListView.Items.Add(item);
                        }
                    }
                }

                converterForm.FormClosing += (s, e) => {
                    // Clean up temp directory
                    try
                    {
                        string tempDir = Path.Combine(Path.GetTempPath(), "BGMEConverter");
                        if (Directory.Exists(tempDir))
                        {
                            Directory.Delete(tempDir, true);
                        }
                    }
                    catch
                    {
                        // Ignore cleanup errors
                    }
                };

                converterForm.ShowDialog();
            }
        }

        private string GetHcaFileNameFromComboBoxItem(string itemText)
        {
            if (string.IsNullOrEmpty(itemText)) return null;

            if (itemText.Contains('(') && itemText.EndsWith(")"))
            {
                int startIndex = itemText.LastIndexOf('(') + 1;
                return itemText.Substring(startIndex, itemText.Length - startIndex - 1);
            }
            return itemText;
        }

        private string GetSelectedHcaFileName()
        {
            if (hcaFileComboBox.SelectedItem == null)
                return null;
            
            return GetHcaFileNameFromComboBoxItem(hcaFileComboBox.SelectedItem.ToString());
        }

        protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
        {
            // Check if Enter key is pressed
            if (keyData == Keys.Enter)
            {
                // Trigger save operation
                SaveButton_Click(this, EventArgs.Empty);
                return true;
            }
            
            return base.ProcessCmdKey(ref msg, keyData);
        }

        private void BattleMusicMenuItem_Click(object? sender, EventArgs e)
        {
            // Get the path to the battle_music.pme file
            string baseDir = Path.GetDirectoryName(Application.ExecutablePath) ?? "";
            // Go up one directory if running from build folder
            if (Path.GetFileName(baseDir) == "build")
            {
                baseDir = Path.GetDirectoryName(baseDir) ?? "";
            }
            string battleMusicPmePath = Path.Combine(baseDir, "battle_music.pme");
            
            // Create and show the battle music form
            using (var battleMusicForm = new BattleMusicForm(_musicService, _hcaNameService, _defaultTagsService, battleMusicPmePath))
            {
                battleMusicForm.ShowDialog(this);
            }
        }
    }
} 