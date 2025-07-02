# Persona 3 Reload - BGM Selector

A custom GUI application for managing background music (BGM) in Persona 3 Reload using the BGME Framework.

## Features

- User-friendly interface with Persona-themed design
- Browse and search through all available music tracks
- Filter tracks by category
- Easily assign HCA files to specific cue IDs
- Save changes directly to the global_music.pme file

## Requirements

- Windows 10 or later
- .NET 6.0 Runtime (will be automatically installed if missing)
- Reloaded-II with BGME Framework installed

## Installation

1. Extract all files to your BGME mod folder (e.g., `C:\Users\[username]\Desktop\Reloaded-II\Mods\BGME`)
2. Run `build.bat` to build the application and copy necessary files
3. Double-click `BGMSelector.exe` in the `build` folder to launch the application

## Usage

1. Browse through the available tracks in the left panel
   - Use the search box to find specific tracks
   - Use the category dropdown to filter by category

2. Select a track you want to replace
   - The track details will appear in the right panel

3. Select an HCA file from the dropdown
   - This is the custom music file that will replace the selected track

4. Click "Assign" to create the assignment

5. Repeat steps 2-4 for any other tracks you want to replace

6. Click "Save Changes" to update the global_music.pme file

7. Launch Persona 3 Reload through Reloaded-II to hear your custom music

## Notes

- The application reads from and writes to `global_music.pme` in its directory
- Make sure your custom HCA files are placed in the `p3r` folder
- Track information is read from `music.yaml`

## Troubleshooting

If you encounter any issues:

1. Make sure all files are in the correct locations
2. Verify that your HCA files are properly formatted
3. Check that the BGME Framework is correctly installed and configured

## Credits

Created for use with the BGME Framework for Persona 3 Reload modding. 