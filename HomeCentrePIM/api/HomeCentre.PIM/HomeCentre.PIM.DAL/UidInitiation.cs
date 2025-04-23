using System;

namespace HomeCentre.PIM.DAL;

public class UidInitiation : Entity
{
    public string Planogram { get; set; }

    public string FamilyName { get; set; }

    public string? Status { get; set; }

    public long? FirstLevelApprovedBy { get; set; }

    public DateTime? FirstLevelApprovedDate { get; set; }

    public long? SecondLevelApprovedBy { get; set; }

    public DateTime? SecondLevelApprovedDate { get; set; }

    public string? FirstLevelComments { get; set; }

    public string? SecondLevelComments { get; set; }

    public string? Description { get; set; }

    public string? FirstLevelAction { get; set; }

    public string? SecondLevelAction { get; set; }

    public long DepartmentId { get; set; }

    public long DivisionId { get; set; }

    public string? L1RejectDescription { get; set; }

    public string? L2RejectDescription { get; set; }

    public DateTime? RmsIntegrationDate { get; set; }

    public string? PlanoCode { get; set; }
}