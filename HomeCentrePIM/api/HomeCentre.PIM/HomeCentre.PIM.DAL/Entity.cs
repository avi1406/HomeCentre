using System;
using HomeCentre.PIM.Common.MetaAttributes;

namespace HomeCentre.PIM.DAL
{
    public abstract class Entity
    {
        [SystemField] 
        public long CreatedBy { get; set; }

        public DateTimeOffset DateCreated { get; set; }

        public DateTimeOffset DateModified { get; set; }

        [SystemField] 
        public long Id { get; set; }

        [SystemField] 
        public long UpdatedBy { get; set; }

        [SystemField] 
        public bool IsActive { get; set; }
    }
}