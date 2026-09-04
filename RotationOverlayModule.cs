using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.IO;
using System.Net;
using System.Runtime.Serialization.Json;
using System.Text;
using Newtonsoft.Json.Linq;

using Blish_HUD;
using Blish_HUD.Content;
using Blish_HUD.Controls;
using Blish_HUD.Input;
using Blish_HUD.Modules;
using Blish_HUD.Modules.Managers;
using Blish_HUD.Settings;

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace RotationOverlay
{
    [Export(typeof(Module))]
    public class RotationOverlayModule : Module
    {
        private const int OuterPadding = 8;
        private const int FixedIconSize = 44;
        private const int SectionSpacing = 5;
        private const int RowSpacing = 3;
        private const int TitleHeight = 18;
        private const int ArrowWidth = 16;
        private const int KeyHeight = 14;
        private const int InstructionWidth = 30;
        private const int SwapWidth = 42;
        private const int MaxStepsPerDisplayRow = 14;

        private OverlayPanel overlayPanel;

        private bool isDragging;
        private Point dragStartMouse;
        private Point dragStartPanel;

        private string rotationDirectory;
        private string rotationFile;
        private string skillsFile;
        private string iconsDirectory;

        private DateTime lastRotationWriteTime = DateTime.MinValue;
        private DateTime lastSkillsWriteTime = DateTime.MinValue;
        private double reloadTimer;

        private readonly List<Texture2D> localTextures =
            new List<Texture2D>();

        private Dictionary<int, SkillDefinition> skillDefinitions =
            new Dictionary<int, SkillDefinition>();

        // Settings
        private SettingEntry<int> opacitySetting;
        private SettingEntry<bool> showSetting;
        private SettingEntry<bool> lockSetting;
        private SettingEntry<bool> showKeysSetting;

        // Hidden saved position
        private SettingEntry<int> positionXSetting;
        private SettingEntry<int> positionYSetting;

        private DirectoriesManager DirectoriesManager =>
            this.ModuleParameters.DirectoriesManager;

        [ImportingConstructor]
        public RotationOverlayModule(
            [Import("ModuleParameters")] ModuleParameters moduleParameters
        ) : base(moduleParameters)
        {
        }

        protected override void DefineSettings(
            SettingCollection settings)
        {

            opacitySetting = settings.DefineSetting(
                "OverlayOpacity",
                100,
                () => "Opacity",
                () => "Changes the transparency of the entire overlay."
            );

            opacitySetting.SetRange(20, 100);

            showSetting = settings.DefineSetting(
                "ShowOverlay",
                true,
                () => "Show Overlay",
                () => "Shows or hides the rotation overlay."
            );

            lockSetting = settings.DefineSetting(
                "LockPosition",
                false,
                () => "Lock Position",
                () => "Locks the overlay and allows mouse clicks to pass through it."
            );

            showKeysSetting = settings.DefineSetting(
                "ShowSkillKeys",
                true,
                () => "Show Keybind Labels",
                () => "Shows the keybind text beneath each skill when provided in rotation.json."
            );

            SettingCollection savedPosition =
                settings.AddSubCollection(
                    "SavedPosition",
                    false
                );

            positionXSetting =
                savedPosition.DefineSetting(
                    "X",
                    -1
                );

            positionYSetting =
                savedPosition.DefineSetting(
                    "Y",
                    40
                );

            opacitySetting.SettingChanged +=
                OpacitySetting_SettingChanged;

            showSetting.SettingChanged +=
                ShowSetting_SettingChanged;

            lockSetting.SettingChanged +=
                LockSetting_SettingChanged;

            showKeysSetting.SettingChanged +=
                ShowKeysSetting_SettingChanged;
        }

        protected override void Initialize()
        {
            PrepareRotationDirectory();

            CreateOverlay();

            LoadRotation();

            GameService.Input.Mouse.MouseMoved +=
                Mouse_MouseMoved;

            GameService.Input.Mouse.LeftMouseButtonReleased +=
                Mouse_LeftMouseButtonReleased;
        }

        private void PrepareRotationDirectory()
        {
            rotationDirectory =
                DirectoriesManager.GetFullDirectoryPath(
                    "rotation_overlay"
                );

            if (string.IsNullOrWhiteSpace(rotationDirectory))
            {
                throw new InvalidOperationException(
                    "Rotation Overlay data directory could not be created."
                );
            }

            iconsDirectory =
                Path.Combine(
                    rotationDirectory,
                    "icons"
                );

            rotationFile =
                Path.Combine(
                    rotationDirectory,
                    "rotation.json"
                );

            skillsFile =
                Path.Combine(
                    rotationDirectory,
                    "skills.json"
                );

            Directory.CreateDirectory(
                rotationDirectory
            );

            Directory.CreateDirectory(
                iconsDirectory
            );

            if (!File.Exists(rotationFile))
            {
                File.WriteAllText(
                    rotationFile,
                    GetStarterRotationJson()
                );
            }

            if (!File.Exists(skillsFile))
            {
                File.WriteAllText(
                    skillsFile,
                    GetStarterSkillsJson()
                );
            }
        }

        private void CreateOverlay()
        {
            overlayPanel =
                new OverlayPanel
                {
                    Parent =
                        GameService.Graphics.SpriteScreen,

                    ZIndex = 9999,

                    BackgroundColor =
                        Color.FromNonPremultiplied(
                            15,
                            15,
                            18,
                            225
                        ),

                    Interactive =
                        !lockSetting.Value
                };

            overlayPanel.LeftMouseButtonPressed +=
                OverlayPanel_LeftMouseButtonPressed;

            ApplyVisibility();
            ApplyOpacity();

            if (positionXSetting.Value >= 0)
            {
                overlayPanel.Left =
                    positionXSetting.Value;

                overlayPanel.Top =
                    positionYSetting.Value;
            }
            else
            {
                overlayPanel.Top = 40;
            }
        }

        private void LoadRotation()
        {
            try
            {
                skillDefinitions =
                    ReadSkillsFile();

                ResolveSkillAssetIds();

                RotationDefinition rotation =
                    ReadRotationFile();

                if (rotation == null)
                    return;

                BuildOverlay(rotation);

                lastRotationWriteTime =
                    File.GetLastWriteTimeUtc(
                        rotationFile
                    );

                lastSkillsWriteTime =
                    File.GetLastWriteTimeUtc(
                        skillsFile
                    );
            }
            catch (Exception ex)
            {
                BuildErrorOverlay(
                    "Unable to load rotation data\n" +
                    ex.Message
                );
            }
        }

        private RotationDefinition ReadRotationFile()
        {
            string json =
                File.ReadAllText(
                    rotationFile
                );

            JObject root =
                JObject.Parse(
                    json
                );

            RotationDefinition rotation =
                new RotationDefinition
                {
                    Name =
                        (string)root["name"] ?? "",

                    Sections =
                        new List<RotationSection>()
                };

            JArray sections =
                root["sections"]
                as JArray;

            if (sections == null)
            {
                return rotation;
            }

            foreach (
                JToken sectionToken
                in sections)
            {
                JObject sectionObject =
                    sectionToken
                    as JObject;

                if (sectionObject == null)
                    continue;

                RotationSection section =
                    new RotationSection
                    {
                        Title =
                            (string)sectionObject["title"]
                            ?? "",

                        Rows =
                            new List<List<object>>()
                    };

                JArray rows =
                    sectionObject["rows"]
                    as JArray;

                if (rows != null)
                {
                    foreach (
                        JToken rowToken
                        in rows)
                    {
                        JArray rowArray =
                            rowToken
                            as JArray;

                        if (rowArray == null)
                            continue;

                        List<object> row =
                            new List<object>();

                        foreach (
                            JToken stepToken
                            in rowArray)
                        {
                            if (
                                stepToken.Type ==
                                JTokenType.Integer
                            )
                            {
                                row.Add(
                                    stepToken.Value<int>()
                                );

                                continue;
                            }

                            if (
                                stepToken.Type ==
                                JTokenType.Object
                            )
                            {
                                JObject instructionObject =
                                    (JObject)stepToken;

                                row.Add(
                                    new RotationInstruction
                                    {
                                        Type =
                                            (string)instructionObject["type"]
                                            ?? "",

                                        Text =
                                            (string)instructionObject["text"]
                                            ?? "",

                                        Name =
                                            (string)instructionObject["name"]
                                            ?? ""
                                    }
                                );

                                continue;
                            }

                            if (
                                stepToken.Type ==
                                JTokenType.String
                            )
                            {
                                int parsedSkillId;

                                if (
                                    int.TryParse(
                                        stepToken.Value<string>(),
                                        out parsedSkillId
                                    )
                                )
                                {
                                    row.Add(
                                        parsedSkillId
                                    );
                                }
                            }
                        }

                        section.Rows.Add(
                            row
                        );
                    }
                }

                rotation.Sections.Add(
                    section
                );
            }

            return rotation;
        }

        private string GetDictionaryString(
            Dictionary<string, object> dictionary,
            string key)
        {
            object value;

            if (
                dictionary != null &&
                dictionary.TryGetValue(
                    key,
                    out value
                ) &&
                value != null
            )
            {
                return value.ToString();
            }

            return "";
        }

        private Dictionary<int, SkillDefinition> ReadSkillsFile()
        {
            string json =
                File.ReadAllText(
                    skillsFile
                );

            JObject root =
                JObject.Parse(
                    json
                );

            Dictionary<int, SkillDefinition> result =
                new Dictionary<int, SkillDefinition>();

            foreach (
                JProperty property
                in root.Properties())
            {
                int localId;

                if (
                    !int.TryParse(
                        property.Name,
                        out localId
                    )
                )
                {
                    continue;
                }

                JObject skillObject =
                    property.Value
                    as JObject;

                if (skillObject == null)
                    continue;

                SkillDefinition skill =
                    new SkillDefinition
                    {
                        Name =
                            (string)skillObject["name"]
                            ?? "",

                        Key =
                            (string)skillObject["key"]
                            ?? "",

                        SkillId =
                            (int?)skillObject["skillId"]
                            ?? 0,

                        AssetId =
                            (int?)skillObject["assetId"]
                            ?? 0,

                        Icon =
                            (string)skillObject["icon"]
                            ?? ""
                    };

                result[localId] =
                    skill;
            }

            return result;
        }

        private void ResolveSkillAssetIds()
        {
            List<int> unresolvedSkillIds =
                new List<int>();

            foreach (
                KeyValuePair<int, SkillDefinition> pair
                in skillDefinitions)
            {
                SkillDefinition skill =
                    pair.Value;

                if (
                    skill == null ||
                    skill.SkillId <= 0 ||
                    skill.AssetId > 0
                )
                {
                    continue;
                }

                if (
                    !unresolvedSkillIds.Contains(
                        skill.SkillId
                    )
                )
                {
                    unresolvedSkillIds.Add(
                        skill.SkillId
                    );
                }
            }

            if (unresolvedSkillIds.Count == 0)
                return;

            try
            {
                string url =
                    "https://api.guildwars2.com/v2/skills?ids=" +
                    string.Join(
                        ",",
                        unresolvedSkillIds
                    );

                string json;

                using (
                    WebClient client =
                        new WebClient()
                )
                {
                    client.Headers[
                        HttpRequestHeader.UserAgent
                    ] =
                        "RotationOverlay/1.2";

                    json =
                        client.DownloadString(
                            url
                        );
                }

                List<Gw2SkillApiRecord> apiSkills =
                    DeserializeGw2Skills(
                        json
                    );

                foreach (
                    Gw2SkillApiRecord apiSkill
                    in apiSkills)
                {
                    if (apiSkill == null)
                        continue;

                    int assetId;

                    if (
                        !TryGetAssetIdFromIconUrl(
                            apiSkill.Icon,
                            out assetId
                        )
                    )
                    {
                        continue;
                    }

                    foreach (
                        KeyValuePair<
                            int,
                            SkillDefinition
                        > pair
                        in skillDefinitions)
                    {
                        SkillDefinition skill =
                            pair.Value;

                        if (
                            skill == null ||
                            skill.SkillId != apiSkill.Id
                        )
                        {
                            continue;
                        }

                        skill.AssetId =
                            assetId;

                        if (
                            string.IsNullOrWhiteSpace(
                                skill.Name
                            )
                        )
                        {
                            skill.Name =
                                apiSkill.Name;
                        }
                    }
                }
            }
            catch
            {
                // If the GW2 API is unavailable, the overlay still loads.
                // Skills can fall back to a manually supplied assetId or icon.
            }
        }

        private List<Gw2SkillApiRecord>
            DeserializeGw2Skills(
                string json)
        {
            DataContractJsonSerializer serializer =
                new DataContractJsonSerializer(
                    typeof(
                        List<Gw2SkillApiRecord>
                    )
                );

            using (
                MemoryStream stream =
                    new MemoryStream(
                        Encoding.UTF8.GetBytes(
                            json
                        )
                    )
            )
            {
                return
                    serializer.ReadObject(stream)
                    as List<Gw2SkillApiRecord>
                    ?? new List<
                        Gw2SkillApiRecord
                    >();
            }
        }

        private bool TryGetAssetIdFromIconUrl(
            string iconUrl,
            out int assetId)
        {
            assetId =
                0;

            if (
                string.IsNullOrWhiteSpace(
                    iconUrl
                )
            )
            {
                return false;
            }

            Uri iconUri;

            if (
                !Uri.TryCreate(
                    iconUrl,
                    UriKind.Absolute,
                    out iconUri
                )
            )
            {
                return false;
            }

            string fileName =
                Path.GetFileNameWithoutExtension(
                    iconUri.AbsolutePath
                );

            return
                int.TryParse(
                    fileName,
                    out assetId
                );
        }

        private void BuildOverlay(
            RotationDefinition rotation)
        {
            ClearOverlay();

            int iconSize =
                FixedIconSize;

            int skillCellHeight =
                iconSize +
                (showKeysSetting.Value
                    ? KeyHeight
                    : 0);

            int y =
                OuterPadding;

            int widestContent =
                250;

            if (rotation.Sections == null)
            {
                rotation.Sections =
                    new List<RotationSection>();
            }

            foreach (
                RotationSection section
                in rotation.Sections)
            {
                if (section == null)
                    continue;

                PassiveLabel title =
                    new PassiveLabel
                    {
                        Parent =
                            overlayPanel,

                        Text =
                            section.Title ?? "",

                        Left =
                            OuterPadding,

                        Top =
                            y,

                        Width =
                            500,

                        Height =
                            TitleHeight,

                        TextColor =
                            Color.FromNonPremultiplied(
                                205,
                                140,
                                255,
                                255
                            ),

                        ShowShadow =
                            true
                    };

                y +=
                    TitleHeight;

                List<List<object>> rows =
                    section.Rows
                    ?? new List<List<object>>();

                foreach (
                    List<object> row
                    in rows)
                {
                    if (row == null)
                        continue;

                    int x =
                        OuterPadding;

                    for (
                        int i = 0;
                        i < row.Count;
                        i++)
                    {
                        object step =
                            row[i];

                        int stepWidth =
                            CreateResolvedStepControl(
                                step,
                                x,
                                y,
                                iconSize
                            );

                        x +=
                            stepWidth;

                        if (
                            ShouldDrawArrowAfter(
                                row,
                                i
                            )
                        )
                        {
                            CreateArrow(
                                x,
                                y,
                                iconSize
                            );

                            x +=
                                ArrowWidth;
                        }
                    }

                    widestContent =
                        Math.Max(
                            widestContent,
                            x + OuterPadding
                        );

                    y +=
                        skillCellHeight +
                        RowSpacing;
                }

                y +=
                    SectionSpacing;
            }

            int panelHeight =
                y +
                OuterPadding -
                SectionSpacing;

            overlayPanel.Size =
                new Point(
                    widestContent,
                    Math.Max(
                        panelHeight,
                        60
                    )
                );

            if (positionXSetting.Value < 0)
            {
                overlayPanel.Left =
                    (
                        GameService.Graphics
                            .SpriteScreen.Width
                        -
                        overlayPanel.Width
                    )
                    / 2;

                overlayPanel.Top =
                    40;

                SavePosition();
            }

            ApplyOpacity();
            ApplyVisibility();
        }

        private int CreateResolvedStepControl(
            object step,
            int x,
            int y,
            int iconSize)
        {
            int skillId;

            if (
                TryGetSkillId(
                    step,
                    out skillId
                )
            )
            {
                SkillDefinition skill;

                if (
                    skillDefinitions.TryGetValue(
                        skillId,
                        out skill
                    )
                )
                {
                    CreateSkillControl(
                        skill,
                        x,
                        y,
                        iconSize
                    );
                }
                else
                {
                    CreateMissingSkillControl(
                        skillId,
                        x,
                        y,
                        iconSize
                    );
                }

                return iconSize;
            }

            RotationInstruction instruction =
                ConvertToInstruction(
                    step
                );

            if (instruction == null)
            {
                CreateUnknownStepControl(
                    x,
                    y,
                    iconSize
                );

                return InstructionWidth;
            }

            string type =
                string.IsNullOrWhiteSpace(
                    instruction.Type
                )
                ? "note"
                : instruction.Type
                    .Trim()
                    .ToLowerInvariant();

            switch (type)
            {
                case "repeat":
                    CreateInstructionControl(
                        new RotationInstruction
                        {
                            Type = instruction.Type,
                            Text = "[R]",
                            Name = string.IsNullOrWhiteSpace(instruction.Name)
                                ? "Repeat"
                                : instruction.Name
                        },
                        x,
                        y,
                        iconSize,
                        "[R]"
                    );

                    return InstructionWidth;

                case "swap":
                    CreateSwapControl(
                        instruction,
                        x,
                        y,
                        iconSize
                    );

                    return iconSize;

                case "note":
                default:
                    return CreateNoteControl(
                        instruction,
                        x,
                        y,
                        iconSize
                    );
            }
        }

        private bool TryGetSkillId(
            object step,
            out int skillId)
        {
            skillId =
                0;

            if (step == null)
                return false;

            if (step is int)
            {
                skillId =
                    (int)step;

                return true;
            }

            if (step is long)
            {
                skillId =
                    (int)(long)step;

                return true;
            }

            if (step is decimal)
            {
                skillId =
                    (int)(decimal)step;

                return true;
            }

            string text =
                step.ToString();

            return
                int.TryParse(
                    text,
                    out skillId
                );
        }

        private RotationInstruction ConvertToInstruction(
            object step)
        {
            if (step == null)
                return null;

            RotationInstruction direct =
                step as RotationInstruction;

            if (direct != null)
                return direct;

            Dictionary<string, object> dictionary =
                step
                as Dictionary<string, object>;

            if (dictionary != null)
            {
                return
                    new RotationInstruction
                    {
                        Type =
                            GetDictionaryString(
                                dictionary,
                                "type"
                            ),

                        Text =
                            GetDictionaryString(
                                dictionary,
                                "text"
                            ),

                        Name =
                            GetDictionaryString(
                                dictionary,
                                "name"
                            )
                    };
            }

            return null;
        }

        private void CreateSkillControl(
            SkillDefinition skill,
            int x,
            int y,
            int iconSize)
        {
            if (skill == null)
            {
                skill =
                    new SkillDefinition();
            }

            PassivePanel card =
                new PassivePanel
                {
                    Parent =
                        overlayPanel,

                    Left =
                        x,

                    Top =
                        y,

                    Width =
                        iconSize,

                    Height =
                        iconSize +
                        (showKeysSetting.Value
                            ? KeyHeight
                            : 0),

                    BackgroundColor =
                        Color.FromNonPremultiplied(
                            35,
                            35,
                            42,
                            220
                        ),

                    CanScroll =
                        false
                };

            AsyncTexture2D texture =
                LoadSkillTexture(
                    skill
                );

            if (texture != null)
            {
                PassiveImage image =
                    new PassiveImage
                    {
                        Parent =
                            card,

                        Texture =
                            texture,

                        Left =
                            2,

                        Top =
                            2,

                        Width =
                            iconSize - 4,

                        Height =
                            iconSize - 4,

                        BasicTooltipText =
                            skill.Name
                    };
            }
            else
            {
                PassiveLabel missing =
                    new PassiveLabel
                    {
                        Parent =
                            card,

                        Text =
                            "?",

                        Left =
                            2,

                        Top =
                            2,

                        Width =
                            iconSize - 4,

                        Height =
                            iconSize - 4,

                        BackgroundColor =
                            Color.FromNonPremultiplied(
                                50,
                                50,
                                58,
                                255
                            ),

                        HorizontalAlignment =
                            HorizontalAlignment.Center,

                        VerticalAlignment =
                            VerticalAlignment.Middle,

                        TextColor =
                            Color.FromNonPremultiplied(
                                210,
                                210,
                                210,
                                255
                            ),

                        BasicTooltipText =
                            skill.Name
                    };
            }

            if (showKeysSetting.Value)
            {
                PassiveLabel keyLabel =
                    new PassiveLabel
                    {
                        Parent =
                            card,

                        Text =
                            string.IsNullOrWhiteSpace(
                                skill.Key
                            )
                            ? ""
                            : skill.Key,

                        Left =
                            0,

                        Top =
                            iconSize,

                        Width =
                            iconSize,

                        Height =
                            KeyHeight,

                        BackgroundColor =
                            Color.FromNonPremultiplied(
                                20,
                                20,
                                25,
                                235
                            ),

                        HorizontalAlignment =
                            HorizontalAlignment.Center,

                        VerticalAlignment =
                            VerticalAlignment.Middle,

                        TextColor =
                            Color.White,

                        ShowShadow =
                            true
                    };
            }
        }

        private void CreateMissingSkillControl(
            int skillId,
            int x,
            int y,
            int iconSize)
        {
            PassiveLabel missing =
                new PassiveLabel
                {
                    Parent =
                        overlayPanel,

                    Text =
                        skillId.ToString(),

                    Left =
                        x,

                    Top =
                        y,

                    Width =
                        iconSize,

                    Height =
                        iconSize,

                    BackgroundColor =
                        Color.FromNonPremultiplied(
                            80,
                            35,
                            35,
                            230
                        ),

                    HorizontalAlignment =
                        HorizontalAlignment.Center,

                    VerticalAlignment =
                        VerticalAlignment.Middle,

                    TextColor =
                        Color.White,

                    BasicTooltipText =
                        "Skill ID " +
                        skillId +
                        " is not defined in skills.json."
                };
        }

        private void CreateUnknownStepControl(
            int x,
            int y,
            int iconSize)
        {
            PassiveLabel unknown =
                new PassiveLabel
                {
                    Parent =
                        overlayPanel,

                    Text =
                        "?",

                    Left =
                        x,

                    Top =
                        y,

                    Width =
                        InstructionWidth,

                    Height =
                        iconSize,

                    HorizontalAlignment =
                        HorizontalAlignment.Center,

                    VerticalAlignment =
                        VerticalAlignment.Middle,

                    TextColor =
                        Color.White
                };
        }

        private void CreateInstructionControl(
            RotationInstruction instruction,
            int x,
            int y,
            int iconSize,
            string defaultText)
        {
            PassiveLabel label =
                new PassiveLabel
                {
                    Parent =
                        overlayPanel,

                    Text =
                        string.IsNullOrWhiteSpace(
                            instruction.Text
                        )
                        ? defaultText
                        : instruction.Text,

                    Left =
                        x,

                    Top =
                        y,

                    Width =
                        InstructionWidth,

                    Height =
                        iconSize,

                    HorizontalAlignment =
                        HorizontalAlignment.Center,

                    VerticalAlignment =
                        VerticalAlignment.Middle,

                    TextColor =
                        Color.FromNonPremultiplied(
                            205,
                            140,
                            255,
                            255
                        ),

                    ShowShadow =
                        true,

                    BasicTooltipText =
                        instruction.Name
                };
        }

        private void CreateSwapControl(
            RotationInstruction instruction,
            int x,
            int y,
            int iconSize)
        {
            PassivePanel card =
                new PassivePanel
                {
                    Parent =
                        overlayPanel,

                    Left =
                        x,

                    Top =
                        y,

                    Width =
                        iconSize,

                    Height =
                        iconSize +
                        (showKeysSetting.Value
                            ? KeyHeight
                            : 0),

                    BackgroundColor =
                        Color.FromNonPremultiplied(
                            35,
                            35,
                            42,
                            220
                        ),

                    CanScroll =
                        false
                };

            AsyncTexture2D texture =
                LoadEmbeddedSwapTexture();

            if (texture != null)
            {
                PassiveImage image =
                    new PassiveImage
                    {
                        Parent =
                            card,

                        Texture =
                            texture,

                        Left =
                            2,

                        Top =
                            2,

                        Width =
                            iconSize - 4,

                        Height =
                            iconSize - 4,

                        BasicTooltipText =
                            string.IsNullOrWhiteSpace(
                                instruction.Name
                            )
                            ? "Weapon Swap"
                            : instruction.Name
                    };
            }
            else
            {
                PassiveLabel fallback =
                    new PassiveLabel
                    {
                        Parent =
                            card,

                        Text =
                            "SW",

                        Left =
                            2,

                        Top =
                            2,

                        Width =
                            iconSize - 4,

                        Height =
                            iconSize - 4,

                        HorizontalAlignment =
                            HorizontalAlignment.Center,

                        VerticalAlignment =
                            VerticalAlignment.Middle,

                        TextColor =
                            Color.White,

                        ShowShadow =
                            true,

                        BasicTooltipText =
                            "Weapon Swap"
                    };
            }
        
            if (showKeysSetting.Value)
            {
                PassiveLabel keyLabel =
                    new PassiveLabel
                    {
                        Parent =
                            card,

                        Text =
                            "`",

                        Left =
                            0,

                        Top =
                            iconSize,

                        Width =
                            iconSize,

                        Height =
                            KeyHeight,

                        BackgroundColor =
                            Color.FromNonPremultiplied(
                                20,
                                20,
                                25,
                                235
                            ),

                        HorizontalAlignment =
                            HorizontalAlignment.Center,

                        VerticalAlignment =
                            VerticalAlignment.Middle,

                        TextColor =
                            Color.White,

                        ShowShadow =
                            true
                    };
            }
}

        private AsyncTexture2D LoadEmbeddedSwapTexture()
        {
            System.Reflection.Assembly assembly =
                typeof(RotationOverlayModule)
                    .Assembly;

            string resourceName =
                "RotationOverlay.assets.weapon_swap_button.png";

            using (
                Stream stream =
                    assembly.GetManifestResourceStream(
                        resourceName
                    )
            )
            {
                if (stream == null)
                    return null;

                Texture2D texture =
                    TextureUtil
                        .FromStreamPremultiplied(
                            stream
                        );

                localTextures.Add(
                    texture
                );

                return texture;
            }
        }

        private int CreateNoteControl(
            RotationInstruction instruction,
            int x,
            int y,
            int iconSize)
        {
            int noteWidth =
                90;

            PassiveLabel note =
                new PassiveLabel
                {
                    Parent =
                        overlayPanel,

                    Text =
                        instruction.Text ?? "",

                    Left =
                        x,

                    Top =
                        y,

                    Width =
                        noteWidth,

                    Height =
                        iconSize,

                    HorizontalAlignment =
                        HorizontalAlignment.Center,

                    VerticalAlignment =
                        VerticalAlignment.Middle,

                    TextColor =
                        Color.White,

                    ShowShadow =
                        true,

                    BasicTooltipText =
                        instruction.Name
                };

            return noteWidth;
        }

        private void CreateArrow(
            int x,
            int y,
            int iconSize)
        {
            PassiveLabel arrow =
                new PassiveLabel
                {
                    Parent =
                        overlayPanel,

                    Text =
                        ">",

                    Left =
                        x,

                    Top =
                        y,

                    Width =
                        ArrowWidth,

                    Height =
                        iconSize,

                    HorizontalAlignment =
                        HorizontalAlignment.Center,

                    VerticalAlignment =
                        VerticalAlignment.Middle,

                    TextColor =
                        Color.FromNonPremultiplied(
                            230,
                            230,
                            235,
                            255
                        ),

                    BackgroundColor =
                        Color.FromNonPremultiplied(
                            0,
                            0,
                            0,
                            1
                        ),

                    ShowShadow =
                        true,

                    ZIndex =
                        100
                };
        }

        private bool ShouldDrawArrowAfter(
            List<object> row,
            int index)
        {
            if (
                row == null ||
                index < 0 ||
                index >= row.Count - 1
            )
            {
                return false;
            }

            object current =
                row[index];

            object next =
                row[index + 1];

            int ignoredSkillId;

            bool currentIsSkill =
                TryGetSkillId(
                    current,
                    out ignoredSkillId
                );

            bool nextIsSkill =
                TryGetSkillId(
                    next,
                    out ignoredSkillId
                );

            RotationInstruction currentInstruction =
                current as RotationInstruction;

            bool currentIsSwap =
                currentInstruction != null &&
                string.Equals(
                    currentInstruction.Type,
                    "swap",
                    StringComparison.OrdinalIgnoreCase
                );

            return
                (currentIsSkill || currentIsSwap) &&
                nextIsSkill;
        }

        private AsyncTexture2D LoadSkillTexture(
            SkillDefinition skill)
        {
            if (skill.AssetId > 0)
            {
                return
                    AsyncTexture2D.FromAssetId(
                        skill.AssetId
                    );
            }

            if (
                !string.IsNullOrWhiteSpace(
                    skill.Icon
                )
            )
            {
                string path =
                    Path.Combine(
                        rotationDirectory,
                        skill.Icon
                    );

                if (File.Exists(path))
                {
                    using (
                        FileStream stream =
                            File.OpenRead(path)
                    )
                    {
                        Texture2D texture =
                            TextureUtil
                                .FromStreamPremultiplied(
                                    stream
                                );

                        localTextures.Add(
                            texture
                        );

                        return texture;
                    }
                }
            }

            return null;
        }

        private void ClearOverlay()
        {
            DisposeLocalTextures();

            if (overlayPanel == null)
                return;

            List<Control> children =
                new List<Control>(
                    overlayPanel.Children
                );

            foreach (
                Control control
                in children)
            {
                control.Dispose();
            }
        }

        private void BuildErrorOverlay(
            string message)
        {
            ClearOverlay();

            overlayPanel.Size =
                new Point(
                    500,
                    90
                );

            PassiveLabel error =
                new PassiveLabel
                {
                    Parent =
                        overlayPanel,

                    Text =
                        message,

                    Left =
                        12,

                    Top =
                        12,

                    Width =
                        476,

                    Height =
                        66,

                    WrapText =
                        true,

                    TextColor =
                        Color.FromNonPremultiplied(
                            255,
                            120,
                            120,
                            255
                        )
                };
        }

        protected override void Update(
            GameTime gameTime)
        {
            

            // Check the JSON roughly once per second.
            reloadTimer +=
                gameTime.ElapsedGameTime.TotalSeconds;

            if (reloadTimer < 1.0)
                return;

            reloadTimer = 0;

            if (
                !File.Exists(rotationFile) ||
                !File.Exists(skillsFile)
            )
            {
                return;
            }

            DateTime rotationWriteTime =
                File.GetLastWriteTimeUtc(
                    rotationFile
                );

            DateTime skillsWriteTime =
                File.GetLastWriteTimeUtc(
                    skillsFile
                );

            if (
                rotationWriteTime !=
                    lastRotationWriteTime ||
                skillsWriteTime !=
                    lastSkillsWriteTime
            )
            {
                LoadRotation();
            }
        }

        private void OverlayPanel_LeftMouseButtonPressed(
            object sender,
            MouseEventArgs e)
        {
            if (lockSetting.Value)
                return;

            isDragging =
                true;

            dragStartMouse =
                GameService.Input.Mouse.Position;

            dragStartPanel =
                new Point(
                    overlayPanel.Left,
                    overlayPanel.Top
                );
        }

        private void Mouse_MouseMoved(
            object sender,
            MouseEventArgs e)
        {
            if (!isDragging)
                return;

            if (lockSetting.Value)
            {
                isDragging =
                    false;

                return;
            }

            Point currentMouse =
                GameService.Input.Mouse.Position;

            int deltaX =
                currentMouse.X
                -
                dragStartMouse.X;

            int deltaY =
                currentMouse.Y
                -
                dragStartMouse.Y;

            overlayPanel.Left =
                dragStartPanel.X +
                deltaX;

            overlayPanel.Top =
                dragStartPanel.Y +
                deltaY;
        }

        private void Mouse_LeftMouseButtonReleased(
            object sender,
            MouseEventArgs e)
        {
            if (!isDragging)
                return;

            isDragging =
                false;

            SavePosition();
        }

        private void SavePosition()
        {
            if (overlayPanel == null)
                return;

            positionXSetting.Value =
                overlayPanel.Left;

            positionYSetting.Value =
                overlayPanel.Top;
        }

        private void ApplyOpacity()
        {
            if (overlayPanel == null)
                return;

            overlayPanel.Opacity =
                opacitySetting.Value /
                100f;
        }

        private void ApplyVisibility()
        {
            if (overlayPanel == null)
                return;

            overlayPanel.Visible =
                showSetting.Value;
        }



        private void OpacitySetting_SettingChanged(
            object sender,
            ValueChangedEventArgs<int> e)
        {
            ApplyOpacity();
        }

        private void ShowSetting_SettingChanged(
            object sender,
            ValueChangedEventArgs<bool> e)
        {
            ApplyVisibility();
        }

        private void ShowKeysSetting_SettingChanged(
            object sender,
            ValueChangedEventArgs<bool> e)
        {
            LoadRotation();
        }

        private void LockSetting_SettingChanged(
            object sender,
            ValueChangedEventArgs<bool> e)
        {
            if (overlayPanel != null)
            {
                overlayPanel.Interactive =
                    !e.NewValue;
            }

            if (e.NewValue)
            {
                isDragging =
                    false;
            }
        }

        private void DisposeLocalTextures()
        {
            foreach (
                Texture2D texture
                in localTextures)
            {
                texture?.Dispose();
            }

            localTextures.Clear();
        }

        protected override void Unload()
        {
            SavePosition();

            opacitySetting.SettingChanged -=
                OpacitySetting_SettingChanged;

            showSetting.SettingChanged -=
                ShowSetting_SettingChanged;

            lockSetting.SettingChanged -=
                LockSetting_SettingChanged;

            showKeysSetting.SettingChanged -=
                ShowKeysSetting_SettingChanged;

            GameService.Input.Mouse.MouseMoved -=
                Mouse_MouseMoved;

            GameService.Input.Mouse.LeftMouseButtonReleased -=
                Mouse_LeftMouseButtonReleased;

            if (overlayPanel != null)
            {
                overlayPanel.LeftMouseButtonPressed -=
                    OverlayPanel_LeftMouseButtonPressed;

                overlayPanel.Dispose();

                overlayPanel =
                    null;
            }

            DisposeLocalTextures();
        }

        private string GetStarterRotationJson()
        {
            return
@"{
  ""name"": ""My Rotation"",
  ""sections"": [
    {
      ""title"": ""OPENER"",
      ""rows"": [
        [1, 2, 3]
      ]
    }
  ]
}";
        }

        private string GetStarterSkillsJson()
        {
            return
@"{
  ""1"": {
    ""name"": ""Skill 1"",
    ""key"": ""1"",
    ""skillId"": 0,
    ""assetId"": 0,
    ""icon"": """"
  },
  ""2"": {
    ""name"": ""Skill 2"",
    ""key"": ""2"",
    ""skillId"": 0,
    ""assetId"": 0,
    ""icon"": """"
  },
  ""3"": {
    ""name"": ""Skill 3"",
    ""key"": ""3"",
    ""skillId"": 0,
    ""assetId"": 0,
    ""icon"": """"
  }
}";
        }
    }

    // ============================================
    // CONTROLS
    // ============================================

    public class OverlayPanel : Panel
    {
        public bool Interactive { get; set; }

        protected override CaptureType CapturesInput()
        {
            if (!Interactive)
            {
                return CaptureType.None;
            }

            return CaptureType.Mouse;
        }
    }

    public class PassivePanel : Panel
    {
        protected override CaptureType CapturesInput()
        {
            return CaptureType.None;
        }
    }

    // Children don't steal mouse input from
    // the draggable parent panel.
    public class PassiveImage : Image
    {
        protected override CaptureType CapturesInput()
        {
            return CaptureType.None;
        }
    }

    public class PassiveLabel : Label
    {
        protected override CaptureType CapturesInput()
        {
            return CaptureType.None;
        }
    }
}