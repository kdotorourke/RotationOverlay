Rotation Overlay

A custom Blish HUD module for displaying Guild Wars 2 skill rotations.

Rotation Overlay renders real Guild Wars 2 skill icons, keybinds, section headings, arrows, and special rotation instructions directly in Blish HUD. Rotations are controlled with JSON, so you can change a rotation without rebuilding the module.

Features

Real Guild Wars 2 skill icons

Keybind labels

Multiple rotation sections and rows

Weapon-swap and repeat instructions

Movable and lockable overlay

Adjustable opacity

Optional keybind display

Automatic reload when rotation JSON changes

Quick Start

1. Requirements

Windows

Guild Wars 2

Blish HUD

.NET SDK capable of building .NET Framework 4.8 projects

2. Build

Open PowerShell in the project folder:

dotnet build -c Release

The compiled files are created in:

bin\Release\net48\

3. Set Your Blish HUD Folder

Set $blishData to your Blish HUD data directory:

$blishData = "C:\Path\To\Your\Guild Wars 2\addons\blishhud"

4. Package and Install

$release = ".\bin\Release\net48"

Remove-Item ".\RotationOverlay.bhm" -ErrorAction Ignore
Remove-Item ".\RotationOverlay.zip" -ErrorAction Ignore

Compress-Archive `
  -Path "$release\RotationOverlay.dll",
        "$release\manifest.json" `
  -DestinationPath ".\RotationOverlay.zip" `
  -Force

Rename-Item ".\RotationOverlay.zip" "RotationOverlay.bhm"

Copy-Item `
  ".\RotationOverlay.bhm" `
  "$blishData\modules\RotationOverlay.bhm" `
  -Force

Restart Blish HUD after replacing the module.

Customize Your Rotation

Rotation Overlay uses two editable JSON files:

rotations\skills.json
rotations\rotation.json

skills.json defines the skills and keybinds.

rotation.json defines the order and layout in which they appear.

Add a Skill

Each skill has a local ID, name, keybind, and Guild Wars 2 skill ID.

{
  "19": {
    "name": "Tale of the Soulkeeper",
    "key": "9",
    "skillId": 76850,
    "assetId": 0,
    "icon": ""
  }
}

Here, 19 is the local ID used in rotation.json.

Build a Rotation

Use local skill IDs inside the rotation:

{
  "name": "Example Rotation",
  "sections": [
    {
      "title": "OPENER",
      "rows": [
        [19, 8, 10, 7, 4],
        [1, 12, 14, 2]
      ]
    }
  ]
}

Each array inside rows becomes one visual row. The module keeps the row layout exactly as written in the JSON.

Rotation Instructions

Rows can contain special instructions in addition to skill IDs.

Weapon Swap

{
  "type": "swap"
}

Weapon swap uses the custom icon stored at:

assets\weapon_swap_button.png

The image is embedded into the module when it is built.

Repeat

{
  "type": "repeat",
  "text": "[R]"
}

Test JSON Changes

Changes to rotation.json and skills.json do not require rebuilding the module.

Copy the updated files to the Blish HUD data folder:

Copy-Item `
  ".\rotations\rotation.json" `
  "$blishData\rotation_overlay\rotation.json" `
  -Force

Copy-Item `
  ".\rotations\skills.json" `
  "$blishData\rotation_overlay\skills.json" `
  -Force

The module watches the live JSON files and reloads rotation data when they change.

What Do I Run?

What You Changed

What To Do

rotation.json

Copy JSON

skills.json

Copy JSON

C# source

Build → Package → Install → Restart Blish HUD

weapon_swap_button.png

Build → Package → Install → Restart Blish HUD

RotationOverlay.csproj

Build → Package → Install → Restart Blish HUD

manifest.json

Package → Install → Restart Blish HUD

Project Structure

RotationOverlay/
├── assets/
│   └── weapon_swap_button.png
├── rotations/
│   ├── rotation.json
│   └── skills.json
├── .gitignore
├── manifest.json
├── README.md
├── RotationModels.cs
├── RotationOverlay.csproj
└── RotationOverlayModule.cs

Module Settings

Rotation Overlay currently provides:

Show Overlay — show or hide the overlay

Opacity — adjust overlay transparency

Lock Position — lock the overlay and allow mouse input to pass through

Show Keybind Labels — show or hide keybinds

Skill icons use a fixed size of 44 px for a consistent layout.

Developer Notes

Keep personal Windows paths out of committed files.

Use $blishData for local Blish HUD paths.

Files in rotations\ are the project/master copies.

Copy JSON changes to the live rotation_overlay folder while testing.

weapon_swap_button.png is embedded into RotationOverlay.dll; it does not need to be copied separately.

Rotation arrows use > because Unicode arrows did not render reliably in Blish HUD.

Avoid using System.Reflection; in RotationOverlayModule.cs. Use System.Reflection.Assembly explicitly to avoid a name conflict with Blish_HUD.Modules.Module.