

0c480299-50a4-4654-9b86-f48fedf97d25.png

e29c2abd-ce8b-43ea-b65d-b7a10e4ee200.png

39daf404-2105-48af-bb4b-ca33dcef05a7.png

f3c6aa05-6486-4771-aadf-73727e1a4855.png

d8454f1e-b7c5-4200-bd0e-09b9bd9887f9.png

14e90d08-3556-4298-bcb2-a2e4dccd39eb.png

b5d4750e-3455-4dce-a6dd-0e79489c255a.png
I don't know how I feel about it. I really like that it's less "long" but I also don't know whether this is what I want it to look like, what do you think?

0953594b-d529-41fa-960a-5fff628c5196.png
The opener is what I want, but the other two loops should still be in one row

a8901551-ce84-4bb9-ba82-94e8f81382c1.png
better but ew, now they aren't lining up. The swap should be the same as the other icons. And thinking of that, is there a swap icon assetID that can be used?

af57033f-1261-4a1b-88f7-4911d7e60693.png

Edit


Edit


f0ec825c-475a-4327-9e88-a1a284bff0bc.png
okay, how do I put an arrow in between swap and Q

I also want to remove the SWAP text

e8b825aa-45b1-4eba-b19b-1fb81c138918.png

Edit


Edited image
cfcc2fb6-a4b5-4315-b6bd-0f79773c1a0a.png

Edit


Edited image
how about you just update the entire overlay code?

7f21f35c-c25c-4f55-81fa-150dba387a19.png

ee8d8fc7-53b2-4bb0-9edd-42ae0b06b202.png
so much cleaner!

Yep — updated to match the version we just finalized.

I added the embedded weapon-swap asset, the ` keybind, fixed 44 px icons, current settings, exact JSON row behavior, full package/install/sync workflow, arrow behavior, and some contributor notes so future-you—or a friend—doesn’t have to rediscover all our little landmines. 😂

 



Is this concise enough that people will be able to follow the instructions?

Yes, but I think we can make it more beginner-friendly and shorter.

The current README is good as developer documentation, but it’s slightly over-documented for someone who just wants to clone the repo, build it, and get the overlay working. The biggest issue is that installation instructions and development instructions are mixed together.

I’d restructure it around what the person is actually trying to do:

What is this? — 2–3 sentences + features.

Quick Start — clone/download → build → install → restart Blish.

Customize Your Rotation — explain skills.json and rotation.json, with one small example each.

Rotation Instructions — weapon swap/repeat.

Development — only for people modifying C#.

Command Cheat Sheet — “Changed X? Run Y.”

Technical Notes — embedded asset, 44 px icons, ASCII arrows, etc.

For example, this part:

“The editable rotation data is stored separately from the .bhm module. Changes to rotation.json or skills.json therefore do not require rebuilding or repackaging the module...”

is useful, but we explain essentially the same concept in How Rotation Data Works, Updating Rotation Data, Which Command Do I Need?, and the flow diagrams. That's where it starts feeling longer than necessary.

One thing I'd change beyond conciseness
The README currently says:

The current weapon-swap key label is `

That's your personal keybind, so I wouldn't bake that into public documentation as though that's how the module works. If we're putting this on Git for friends/other users, weapon swap should eventually be configurable just like skill keybinds.

Likewise, I'd separate:

For users

“Here is how you install and customize Rotation Overlay.”

from:

For contributors

“Here is how the code works and how to build/package it.”

That would make the README much easier to scan.

So: people could follow the current version, absolutely. But I think we can cut ~25–35% of it and actually make it easier to follow. I'd rather give you a polished GitHub-style README where the first screen gets someone from “what is this?” → “how do I run it?” without making them read the architecture first.


Yes lets do that

Done. This version is much more “clone it → build it → customize it” and less “here is the complete archaeological history of how Kairo and Kristyn fought Blish HUD.” 😂

I kept the important developer notes at the bottom, but the main instructions now focus on what someone actually needs to do.


One thing I deliberately changed: I removed your personal ` weapon-swap keybind from the documentation. The README now describes weapon swap generically, which is much better for GitHub/sharing. 




Library
/
README_CONCISE.md


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
You need:

Windows

Guild Wars 2

Blish HUD

.NET SDK capable of building .NET Framework 4.8 projects

2. Build
Open PowerShell in the project folder and run:

dotnet build -c Release
The compiled files will be created in:

bin\Release\net48\
3. Set Your Blish HUD Folder
Set $blishData to your Blish HUD data directory:

$blishData = "C:\Path\To\Your\Guild Wars 2\addons\blishhud"
4. Package and Install
Run:

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
Restart Blish HUD.

Customize Your Rotation
Rotation Overlay uses two files:

rotations\skills.json
rotations\rotation.json
skills.json defines your skills.

rotation.json defines the order they appear.

Add a Skill
Each skill has a local ID, name, keybind, and Guild Wars 2 skill ID.

Example:

{
  "19": {
    "name": "Tale of the Soulkeeper",
    "key": "9",
    "skillId": 76850,
    "assetId": 0,
    "icon": ""
  }
}
Here, 19 is the local ID used in your rotation.

Build a Rotation
Use those local IDs inside rotation.json:

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
Each array inside rows becomes one row on screen.

The module keeps the row layout exactly as written in the JSON.

Rotation Instructions
Rows can contain special instructions in addition to skill IDs.

Weapon Swap
{
  "type": "swap"
}
Weapon swap uses the custom icon stored at:

assets\weapon_swap_button.png
The image is embedded in the module when it is built.

Repeat
{
  "type": "repeat",
  "text": "[R]"
}
Test JSON Changes
Changes to rotation.json and skills.json do not require rebuilding the module.

Copy the updated files to your Blish HUD data folder:

Copy-Item `
  ".\rotations\rotation.json" `
  "$blishData\rotation_overlay\rotation.json" `
  -Force

Copy-Item `
  ".\rotations\skills.json" `
  "$blishData\rotation_overlay\skills.json" `
  -Force
The module watches these live files and reloads rotation data when they change.

What Do I Run?
What You Changed	What To Do
rotation.json	Copy JSON
skills.json	Copy JSON
C# source	Build → Package → Install → Restart Blish HUD
weapon_swap_button.png	Build → Package → Install → Restart Blish HUD
RotationOverlay.csproj	Build → Package → Install → Restart Blish HUD
manifest.json	Package → Install → Restart Blish HUD
Project Structure
RotationOverlay/
├── assets/
│   └── weapon_swap_button.png
├── rotations/
│   ├── rotation.json
│   └── skills.json
├── manifest.json
├── RotationModels.cs
├── RotationOverlay.csproj
├── RotationOverlayModule.cs
└── README.md
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

During testing, remember to copy JSON changes to the live rotation_overlay folder.

The weapon-swap image is embedded into RotationOverlay.dll.

Rotation arrows use > because Unicode arrows did not render reliably in Blish HUD.

Avoid using System.Reflection; in RotationOverlayModule.cs. Use System.Reflection.Assembly explicitly to avoid a name conflict with Blish_HUD.Modules.Module.

