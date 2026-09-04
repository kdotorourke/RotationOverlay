

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

77fd6649-6c87-4085-bc53-afcbb05c1fcc.png

4a4d54cb-e97d-4c21-8c18-a90fa7e5baff.png

Today 12:03 PM
d611ee87-7506-4477-8078-9281bbf1d593.png

Yep — the README’s project tree is now stale. We need to add the PNG assets that are actually part of the project rather than pretending weapon_swap_button.png is our lone survivor. 😂

Since those PNG filenames you pasted look like screenshot/generated filenames rather than necessarily the actual filenames in your assets folder, I don’t want to guess what belongs in the repository documentation.

In your Cursor PowerShell terminal, run:

Get-ChildItem .\assets -Recurse | Select-Object FullName
Paste that result here.

Then I’ll update the README’s Project Structure and any relevant asset instructions to match what is actually in the project.


2db16551-7540-4765-a859-792149352487.png
theres only one - not sure what all the others are referencing

Ahhh — mystery solved. 😂

Those filenames like:

0c480299-50a4-4654-9b86-f48fedf97d25.png
e29c2abd-ce8b-43ea-b65d-b7a10e4ee200.png
39daf404-2105-48af-bb4b-ca33dcef05a7.png
are not files in your Rotation Overlay project.

They're images/screenshots that were attached during our ChatGPT conversation. When you copied the conversation/history into your message, those attachment references came along with it. They have nothing to do with GitHub or your repo.

Your actual project contains exactly what we expected:

assets/
└── weapon_swap_button.png
So our README's project structure is already correct. ✅

One thing we do need to fix
We just published the older README to GitHub.

Remember, we created the improved concise version afterward as README_CONCISE.md, but we haven't actually replaced:

README.md
inside your project with it yet.

So that's our next move: replace your current project README.md with the concise version, then you'll get to do your first real GitHub update:

Change README
      ↓
GitHub Desktop detects it
      ↓
Commit
      ↓
Push
      ↓
GitHub README updates
Which is actually perfect timing because it'll teach you the normal workflow you'll use from now on. 😎


okay, give me the readme to replace then please

Absolutely. Here is the replacement README with the concise structure and the now-confirmed project tree showing only the one actual PNG asset.


Replace your current file
Download it, then replace:

C:\Users\k_0_1\.cursor\projects\RotationOverlay\README.md
with this one. You can rename RotationOverlay_README.md to simply:

README.md
Then open GitHub Desktop. It should immediately show README.md under Changes.

That'll be a perfect first normal GitHub update: change → review → commit → push. 😎 




Library
/
RotationOverlay_README.md


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

