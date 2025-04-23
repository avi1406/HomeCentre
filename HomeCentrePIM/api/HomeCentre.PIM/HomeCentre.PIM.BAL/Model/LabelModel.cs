using HomeCentre.PIM.Common.MetaAttributes;

namespace HomeCentre.PIM.BAL.Model
{
    [SearchField(new[] { "Name", "Description" })]
    public class LabelModel
    {
        public long Id { get; set; }

        public string Name { get; set; }

        public string Description { get; set; }

        public string Language { get; set; }

        public long LanguageId { get; set; }

        public bool IsActive { get; set; }
    }
}