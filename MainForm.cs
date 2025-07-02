using BGMSelector.Models;
using BGMSelector.Services;
using System.ComponentModel;
using Microsoft.VisualBasic.FileIO; // For RecycleBin operations

namespace BGMSelector
{
    public partial class MainForm : Form
    {
        private readonly MusicService _musicService;
        private readonly HcaNameService _hcaNameService;
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
            _hcaFolderPath = Path.Combine(baseDir, "p3r");
            string hcaNamesPath = Path.Combine(baseDir, "hca_names.json");
            
            _musicService = new MusicService(yamlPath, pmePath, _hcaFolderPath);
            _hcaNameService = new HcaNameService(hcaNamesPath);
            
            // Apply Persona theme
            ApplyPersonaTheme();
        }

        private void InitializeComponent()
        {
            this.SuspendLayout();
            
            this.AutoScaleDimensions = new System.Drawing.SizeF(7F, 15F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(1000, 600);
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

            ToolStripMenuItem actionsMenuItem = new ToolStripMenuItem("Actions");
            actionsMenuItem.ForeColor = _lightText;

            ToolStripMenuItem modifyHcaMenuItem = new ToolStripMenuItem("Modify Selected HCA File...");
            modifyHcaMenuItem.BackColor = _darkBackground;
            modifyHcaMenuItem.ForeColor = _lightText;
            modifyHcaMenuItem.Click += ModifyHcaMenuItem_Click;

            ToolStripMenuItem editHcaNameMenuItem = new ToolStripMenuItem("Edit HCA Name...");
            editHcaNameMenuItem.BackColor = _darkBackground;
            editHcaNameMenuItem.ForeColor = _lightText;
            editHcaNameMenuItem.Click += EditHcaNameMenuItem_Click;

            actionsMenuItem.DropDownItems.Add(modifyHcaMenuItem);
            actionsMenuItem.DropDownItems.Add(editHcaNameMenuItem);
            menuStrip.Items.Add(actionsMenuItem);

            this.Controls.Add(menuStrip);
            
            // Create main container
            TableLayoutPanel mainContainer = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 2,
                RowCount = 1,
                Padding = new Padding(10),
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
                Height = 190, // Increased height slightly
                Padding = new Padding(10),
                BackColor = _darkBackground,
                ColumnCount = 3, // Labels | Inputs | Buttons
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
                RowCount = 3,
                Margin = new Padding(10, 0, 0, 0)
            };
            actionButtonsPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50F));
            actionButtonsPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50F));
            actionButtonsPanel.RowStyles.Add(new RowStyle(SizeType.Absolute, 35F));
            actionButtonsPanel.RowStyles.Add(new RowStyle(SizeType.Absolute, 35F));
            actionButtonsPanel.RowStyles.Add(new RowStyle(SizeType.Absolute, 35F));

            var saveButton = new Button { Text = "Save", Dock = DockStyle.Fill, BackColor = _personaBlue, ForeColor = Color.White, FlatStyle = FlatStyle.Flat };
            var refreshHcaButton = new Button { Text = "↻", Dock = DockStyle.Fill, BackColor = Color.FromArgb(60, 60, 60), ForeColor = Color.White, FlatStyle = FlatStyle.Flat, Font = new Font("Arial", 10, FontStyle.Bold) };
            var assignButton = new Button { Text = "Assign", Dock = DockStyle.Fill, BackColor = _personaBlue, ForeColor = Color.White, FlatStyle = FlatStyle.Flat };
            var removeButton = new Button { Text = "Remove", Dock = DockStyle.Fill, BackColor = Color.FromArgb(70, 70, 70), ForeColor = Color.White, FlatStyle = FlatStyle.Flat };
            
            actionButtonsPanel.Controls.Add(saveButton, 0, 0);
            actionButtonsPanel.Controls.Add(refreshHcaButton, 1, 0);
            actionButtonsPanel.Controls.Add(assignButton, 0, 1);
            actionButtonsPanel.SetColumnSpan(assignButton, 2);
            actionButtonsPanel.Controls.Add(removeButton, 0, 2);
            actionButtonsPanel.SetColumnSpan(removeButton, 2);

            // --- Arrange Main Controls in Layout ---
            assignmentControlsLayout.Controls.Add(selectedTrackLabel, 0, 0);
            assignmentControlsLayout.Controls.Add(selectedTrackValue, 1, 0);
            
            assignmentControlsLayout.Controls.Add(hcaFileLabel, 0, 1);
            assignmentControlsLayout.SetRowSpan(hcaFileLabel, 2);
            assignmentControlsLayout.Controls.Add(hcaSearchBox, 1, 1);
            assignmentControlsLayout.Controls.Add(hcaFileComboBox, 1, 2);
            
            assignmentControlsLayout.Controls.Add(actionButtonsPanel, 2, 0);
            assignmentControlsLayout.SetRowSpan(actionButtonsPanel, 3);
            
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
            
            foreach (var assignment in _currentAssignments)
            {
                var item = new ListViewItem(assignment.Key.ToString());
                
                // Find track name by cue ID
                string trackName = "Unknown";
                if (_musicConfig?.Tracks != null)
                {
                    var track = _musicConfig.Tracks.FirstOrDefault(t => t.CueId == assignment.Key);
                    if (track != null)
                        trackName = track.Name ?? "Unknown";
                }
                
                item.SubItems.Add(trackName);

                string hcaFileName = assignment.Value;
                string hcaDisplayName = _hcaNameService.GetName(hcaFileName);
                
                if (hcaDisplayName != hcaFileName)
                {
                    item.SubItems.Add($"{hcaDisplayName} ({hcaFileName})");
                }
                else
                {
                    item.SubItems.Add(hcaFileName);
                }
                
                item.Tag = assignment.Key;
                
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
                inputForm.Text = "Edit HCA Name";
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
    }
} 