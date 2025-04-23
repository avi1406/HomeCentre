using System;

namespace HomeCentre.PIM.Common.MetaAttributes
{
    public class MasterFieldAttribute : Attribute
    {
        public MasterFieldAttribute(string name)
        {
            this.Name = name;
        }

        public string Name { get; }
    }
}