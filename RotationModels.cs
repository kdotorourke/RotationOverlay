using System.Collections.Generic;
using System.Runtime.Serialization;

namespace RotationOverlay
{
    [DataContract]
    public class RotationDefinition
    {
        [DataMember(Name = "name")]
        public string Name { get; set; }

        [DataMember(Name = "sections")]
        public List<RotationSection> Sections { get; set; }
    }

    [DataContract]
    public class RotationSection
    {
        [DataMember(Name = "title")]
        public string Title { get; set; }

        [DataMember(Name = "rows")]
        public List<List<object>> Rows { get; set; }
    }

    [DataContract]
    public class RotationInstruction
    {
        // Supported values: repeat, swap, note
        [DataMember(Name = "type")]
        public string Type { get; set; }

        [DataMember(Name = "text")]
        public string Text { get; set; }

        [DataMember(Name = "name")]
        public string Name { get; set; }
    }

    [DataContract]
    public class SkillDefinition
    {
        [DataMember(Name = "name")]
        public string Name { get; set; }

        [DataMember(Name = "skillId")]
        public int SkillId { get; set; }

        // Optional manual override/cache. If 0, the module resolves the
        // icon asset automatically from the GW2 API using SkillId.
        [DataMember(Name = "assetId")]
        public int AssetId { get; set; }

        [DataMember(Name = "icon")]
        public string Icon { get; set; }

        [DataMember(Name = "key")]
        public string Key { get; set; }
    }

    [DataContract]
    public class Gw2SkillApiRecord
    {
        [DataMember(Name = "id")]
        public int Id { get; set; }

        [DataMember(Name = "name")]
        public string Name { get; set; }

        [DataMember(Name = "icon")]
        public string Icon { get; set; }
    }

}