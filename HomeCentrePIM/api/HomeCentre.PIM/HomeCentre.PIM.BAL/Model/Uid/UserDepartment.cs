namespace HomeCentre.PIM.BAL.Model.Uid
{
    public class UserDepartment
    {
        public long DepartmentId { get; set; }

        public string DepartmentName { get; set; }

        public string DivisionName { get; set; }

        public bool IsPlanogram { get; set; }

        public long? DivisionId { get; set; }
    }
}