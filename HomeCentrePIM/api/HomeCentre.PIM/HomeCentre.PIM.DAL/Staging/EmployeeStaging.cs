namespace HomeCentre.PIM.DAL.Staging;

public class EmployeeStaging : StagingEntity
{
    public string FirstName { get; set; }

    public string LastName { get; set; }

    public string Email { get; set; }

    public string PhoneNo { get; set; }

    public bool IsRemoved { get; set; }
}