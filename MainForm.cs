using BGMSelector.Models;
using BGMSelector.Services;
using System.ComponentModel;

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
                Height = 150,
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
        
        private void AssignmentsListView_SelectedIndexChanged(object? sender, EventArgs e)
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
    }
} 