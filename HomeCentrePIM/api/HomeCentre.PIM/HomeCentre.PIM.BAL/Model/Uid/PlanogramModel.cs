using System;
using HomeCentre.PIM.Common.MetaAttributes;
using HomeCentre.PIM.DAL;

namespace HomeCentre.PIM.BAL.Model.Uid
{
    public class PlanagromRecortList
    {
        public string ActionedbyL1 { get; set; }

        public string? L1ActionDate { get; set; }

        public string PlanoOrFamilyNameCreationStatusL1 { get; set; }

        public string FirstLevelComments { get; set; }

        public string SecondLevelComments { get; set; }

        public string ActionedbyL2 { get; set; }

        public string PlanoOrFamilyNameCreationStatusL2 { get; set; }

        public string? L2ActionDate { get; set; }

        public long Id { get; set; }

        public long RecordId { get; set; }

        public string PlanoOrFamilyName { get; set; }

        public string RequestType { get; set; }

        public string Division { get; set; }

        public string Description { get; set; }

        public string PlanoOrFamilyCreatedBy { get; set; }

        public string PlanoOrFamilyCreatedDate { get; set; }

        public long TaskStepInstanceId { get; set; }

        public string PlanoOrFamilyCode { get; set; }

        public string PlanoOrFamilyCodeUpdatedDate { get; set; }

        public string PlanoOrFamilyCodeStatus { get; set; }

        public string? L1RejectDescription { get; set; }

        public string? L2RejectDescription { get; set; }

        public string Datecreated { get; set; }

        public string TaskStepInstanceName { get; set; }
    }
}