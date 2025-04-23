export class FirstLevelApprovalModel {
    Id: Number | undefined;
    PlanogramOrFamilyName: string | undefined;
    Type: string | undefined;
    Division: string | undefined;
    Description: string | undefined;
    CreatedBy: string | undefined;
    DateCreated: Date | undefined;
    TaskStepInstanceId: Number | undefined;
    FirstLevelApprover: string | undefined;
    FirstLevelActionDate: Date | undefined;
    FirstLevelAction: string | undefined;
    SecondLevelApprover: string | undefined;
    SecondLevelAction: string | undefined;
    SecondLevelActionDate: Date | undefined;
    FirstLevelComment: string | undefined;
    SecondLevelComment: string | undefined;
}