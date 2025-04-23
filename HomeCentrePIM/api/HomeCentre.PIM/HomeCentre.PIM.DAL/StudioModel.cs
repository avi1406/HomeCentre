using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HomeCentre.PIM.DAL
{
    public class StudioModel
    {
        public long Id { get; set; }

        public string City { get; set; }

        public string StudioName { get; set; }

        public long Code { get; set; }

        public long TotalCapacity { get; set; }

        public long RemainingCapacity { get; set; }

        public long Assigned { get; set; }

        public long Week1Capacity { get; set; }

        public long Week2Capacity { get; set; }

        public long Week3Capacity { get; set; }

        public long Week4Capacity { get; set; }

        public long? Week5Capacity { get; set; }

        public string Priority { get; set; }

        public bool IsActive { get; set; }

        public string Effectivefromdate { get; set; }

        public string DivisionName { get; set; }

        public long DivisionId { get; set; }
    }
}
