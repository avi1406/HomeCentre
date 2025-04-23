using System;

namespace HomeCentre.PIM.Common.MetaAttributes
{
    public class SearchFieldAttribute : Attribute
    {
        public SearchFieldAttribute(string[] fieldNames)
        {
            this.FieldNames = fieldNames;
        }

        public string[] FieldNames { get; }
    }
}