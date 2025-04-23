using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HomeCentre.PIM.DAL
{
    public class Studio : Entity
    {
        public string City { get; set; }

        public long DivisionId { get; set; }

        public string Name { get; set; }

        public long Code { get; set; }

        public long Week1Capacity { get; set; }

        public long Week2Capacity { get; set; }

        public long Week3Capacity { get; set; }

        public long Week4Capacity { get; set; }

        public long? Week5Capacity { get; set; }

        public long TotalCapacity { get; set; }

        public long RemainingCapacity { get; set; }

        public string Priority { get; set; }

        public DateTime? EffectivefromDate { get; set; }
    }
}
