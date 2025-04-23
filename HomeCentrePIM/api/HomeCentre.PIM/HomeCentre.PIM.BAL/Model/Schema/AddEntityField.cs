using HomeCentre.PIM.Common.MetaAttributes;

namespace HomeCentre.PIM.BAL.Model.Schema
{
    public class AddEntityField
    {
        public long Id { get; set; }

        public string Caption { get; set; }

        public bool IsSearchField { get; set; }

        public bool IsRequired { get; set; }

        public long RelatedEntityId { get; set; }

        public long? MaxRangeWholeNumber { get; set; }

        public long? MinRangeWholeNumber { get; set; }

        public decimal? MaxRangeFloatingPoint { get; set; }

        public decimal? MinRangeFloatingPoint { get; set; }

        public string Name { get; set; }

        public EntityFieldType EntityFieldType { get; set; }

        public long EntitySchemaId { get; set; }

        public int? Length { get; set; }

        public bool IsActive { get; set; }
    }
}