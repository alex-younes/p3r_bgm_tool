using BGMSelector.Models;
using BGMSelector.Services;
using System.ComponentModel;
using Microsoft.VisualBasic.FileIO; // For RecycleBin operations

namespace BGMSelector
{
    public partial class MainForm : Form
    {
        private readonly MusicService _musicService;
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
        private Button bulkAssignButton;
        private Button bulkRemoveButton;
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
            
            _musicService = new MusicService(yamlPath, pmePath, _hcaFolderPath);
            
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

            actionsMenuItem.DropDownItems.Add(modifyHcaMenuItem);
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
            
            mainContainer.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50F));
            mainContainer.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50F));
            
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
            
            Panel assignmentControlsPanel = new Panel
            {
                Dock = DockStyle.Bottom,
                Height = 180, // Increased height for multi-select controls
                Padding = new Padding(10)
            };
            
            // Create drop panel for HCA files - positioned prominently
            Panel dropPanel = new Panel
            {
                Location = new Point(10, 10),
                Size = new Size(250, 40),
                BackColor = Color.FromArgb(35, 35, 35),
                BorderStyle = BorderStyle.FixedSingle
            };

            Label dropLabel = new Label
            {
                Text = "Drop HCA files here",
                ForeColor = Color.Gray,
                TextAlign = ContentAlignment.MiddleCenter,
                Dock = DockStyle.Fill,
                Font = new Font("Arial", 10, FontStyle.Italic)
            };
            
            dropPanel.Controls.Add(dropLabel);
            
            // Setup drag & drop for the panel
            dropPanel.AllowDrop = true;
            dropPanel.DragEnter += DropPanel_DragEnter;
            dropPanel.DragDrop += DropPanel_DragDrop;
            
            // Refresh HCA files button - next to drop panel
            Button refreshHcaButton = new Button
            {
                Text = "↻",
                BackColor = Color.FromArgb(60, 60, 60),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Location = new Point(270, 10),
                Size = new Size(40, 40),
                Font = new Font("Arial", 12, FontStyle.Bold)
            };
            
            refreshHcaButton.Click += RefreshHcaButton_Click;
            
            Label selectedTrackLabel = new Label
            {
                Text = "Selected Track:",
                ForeColor = _lightText,
                Location = new Point(10, 60),
                Size = new Size(100, 20)
            };
            
            Label selectedTrackValue = new Label
            {
                Text = "None",
                ForeColor = _lightText,
                Location = new Point(120, 60),
                Size = new Size(300, 20),
                AutoEllipsis = true
            };
            
            Label hcaFileLabel = new Label
            {
                Text = "HCA File:",
                ForeColor = _lightText,
                Location = new Point(10, 90),
                Size = new Size(100, 20)
            };

            TextBox hcaSearchBox = new TextBox
            {
                Location = new Point(120, 90),
                Size = new Size(200, 25),
                BackColor = Color.FromArgb(50, 50, 50),
                ForeColor = _lightText,
                BorderStyle = BorderStyle.FixedSingle,
                PlaceholderText = "Search HCA files..."
            };
            
            ComboBox hcaFileComboBox = new ComboBox
            {
                Location = new Point(120, 120),
                Size = new Size(200, 25),
                BackColor = Color.FromArgb(50, 50, 50),
                ForeColor = _lightText,
                FlatStyle = FlatStyle.Flat,
                DropDownStyle = ComboBoxStyle.DropDownList
            };
            
            Button assignButton = new Button
            {
                Text = "Assign",
                BackColor = _personaBlue,
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Location = new Point(330, 120),
                Size = new Size(80, 25)
            };
            
            Button saveButton = new Button
            {
                Text = "Save Changes",
                BackColor = _personaBlue,
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Location = new Point(330, 10),
                Size = new Size(120, 30)
            };
            
            Button removeButton = new Button
            {
                Text = "Remove",
                BackColor = Color.FromArgb(70, 70, 70),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Location = new Point(330, 50),
                Size = new Size(120, 30)
            };
            
            // Multi-select checkbox
            CheckBox enableMultiSelectCheckbox = new CheckBox
            {
                Text = "Enable Multi-Select",
                ForeColor = _lightText,
                Location = new Point(10, 150),
                Size = new Size(150, 20),
                BackColor = Color.Transparent,
                Checked = false
            };
            
            enableMultiSelectCheckbox.CheckedChanged += EnableMultiSelectCheckbox_CheckedChanged;
            
            // Bulk operation buttons
            Button bulkAssignButton = new Button
            {
                Text = "Bulk Assign",
                BackColor = _personaBlue,
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Location = new Point(170, 150),
                Size = new Size(100, 25),
                Enabled = false
            };
            
            Button bulkRemoveButton = new Button
            {
                Text = "Bulk Remove",
                BackColor = Color.FromArgb(70, 70, 70),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Location = new Point(280, 150),
                Size = new Size(100, 25),
                Enabled = false
            };
            
            bulkAssignButton.Click += BulkAssignButton_Click;
            bulkRemoveButton.Click += BulkRemoveButton_Click;
            
            assignmentControlsPanel.Controls.Add(enableMultiSelectCheckbox);
            assignmentControlsPanel.Controls.Add(bulkAssignButton);
            assignmentControlsPanel.Controls.Add(bulkRemoveButton);
            assignmentControlsPanel.Controls.Add(dropPanel);
            assignmentControlsPanel.Controls.Add(refreshHcaButton);
            assignmentControlsPanel.Controls.Add(removeButton);
            assignmentControlsPanel.Controls.Add(saveButton);
            assignmentControlsPanel.Controls.Add(assignButton);
            assignmentControlsPanel.Controls.Add(hcaFileComboBox);
            assignmentControlsPanel.Controls.Add(hcaSearchBox);
            assignmentControlsPanel.Controls.Add(hcaFileLabel);
            assignmentControlsPanel.Controls.Add(selectedTrackValue);
            assignmentControlsPanel.Controls.Add(selectedTrackLabel);
            
            rightPanel.Controls.Add(assignmentsListView);
            rightPanel.Controls.Add(assignmentControlsPanel);
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
            this.bulkAssignButton = bulkAssignButton;
            this.bulkRemoveButton = bulkRemoveButton;
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
                    hcaFileComboBox.Items.Add(file);
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
                item.SubItems.Add(assignment.Value);
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
                    string hcaFile = selectedItem.SubItems[2].Text;
                    
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
                    
                    // Select HCA file in combo box
                    for (int i = 0; i < hcaFileComboBox.Items.Count; i++)
                    {
                        if (hcaFileComboBox.Items[i].ToString() == hcaFile)
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
            if (string.IsNullOrEmpty(searchText))
            {
                // Restore all HCA files
                PopulateHcaFiles();
                return;
            }

            hcaFileComboBox.BeginUpdate();
            hcaFileComboBox.Items.Clear();

            try
            {
                var allHcaFiles = _musicService.GetAvailableHcaFiles();
                foreach (var file in allHcaFiles)
                {
                    if (file.ToLower().Contains(searchText))
                    {
                        hcaFileComboBox.Items.Add(file);
                    }
                }

                if (hcaFileComboBox.Items.Count > 0)
                    hcaFileComboBox.SelectedIndex = 0;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error filtering HCA files: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }

            hcaFileComboBox.EndUpdate();
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
            if (selectedTrackValue.Tag is MusicTrack track && hcaFileComboBox.SelectedItem != null)
            {
                string hcaFile = hcaFileComboBox.SelectedItem.ToString() ?? "";
                
                _currentAssignments[track.CueId] = hcaFile;
                UpdateAssignmentsList();
            }
            else
            {
                MessageBox.Show("Please select both a track and an HCA file.", "Missing Selection", MessageBoxButtons.OK, MessageBoxIcon.Warning);
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
            if (assignmentsListView.SelectedItems.Count > 0)
            {
                var selectedItem = assignmentsListView.SelectedItems[0];
                int cueId = (int)selectedItem.Tag;
                
                _currentAssignments.Remove(cueId);
                UpdateAssignmentsList();
            }
            else
            {
                MessageBox.Show("Please select an assignment to remove.", "No Selection", MessageBoxButtons.OK, MessageBoxIcon.Warning);
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
            
            // Enable/disable bulk operation buttons
            bulkAssignButton.Enabled = multiSelectEnabled;
            bulkRemoveButton.Enabled = multiSelectEnabled;
            
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
        
        private void BulkAssignButton_Click(object? sender, EventArgs e)
        {
            if (hcaFileComboBox.SelectedItem == null)
            {
                MessageBox.Show("Please select an HCA file to assign.", "Missing Selection", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            
            string hcaFile = hcaFileComboBox.SelectedItem.ToString() ?? "";
            int assignedCount = 0;
            
            // Check if tracks are selected from the track list
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
            // Check if assignments are selected from the assignments list
            else if (assignmentsListView.SelectedItems.Count > 0)
            {
                foreach (ListViewItem item in assignmentsListView.SelectedItems)
                {
                    int cueId = (int)item.Tag;
                    _currentAssignments[cueId] = hcaFile;
                    assignedCount++;
                }
            }
            else
            {
                MessageBox.Show("Please select tracks or assignments to reassign.", "No Selection", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            
            UpdateAssignmentsList();
            
            // Show feedback
            string message = assignedCount == 1 
                ? "1 track assigned successfully." 
                : $"{assignedCount} tracks assigned successfully.";
                
            MessageBox.Show(message, "Bulk Assign", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }
        
        private void BulkRemoveButton_Click(object? sender, EventArgs e)
        {
            if (assignmentsListView.SelectedItems.Count > 0)
            {
                int removedCount = 0;
                
                // Create a list of items to remove
                List<int> cueIdsToRemove = new List<int>();
                
                foreach (ListViewItem item in assignmentsListView.SelectedItems)
                {
                    int cueId = (int)item.Tag;
                    cueIdsToRemove.Add(cueId);
                }
                
                // Remove the assignments
                foreach (int cueId in cueIdsToRemove)
                {
                    if (_currentAssignments.Remove(cueId))
                    {
                        removedCount++;
                    }
                }
                
                UpdateAssignmentsList();
                
                // Show feedback
                string message = removedCount == 1 
                    ? "1 assignment removed successfully." 
                    : $"{removedCount} assignments removed successfully.";
                    
                MessageBox.Show(message, "Bulk Remove", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            else
            {
                MessageBox.Show("Please select assignment(s) to remove.", "No Selection", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }

        private void ModifyHcaMenuItem_Click(object? sender, EventArgs e)
        {
            if (hcaFileComboBox.SelectedItem == null)
            {
                MessageBox.Show("Please select an HCA file to modify.", "No Selection", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            
            string hcaFile = hcaFileComboBox.SelectedItem.ToString() ?? "";
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