using Hangfire.Dashboard;

namespace HomeCentre.PIM.CommonWeb;

public class MyAuthorizationFilter : IDashboardAuthorizationFilter
{
    public bool Authorize(DashboardContext context)
    {
        return true;
    }
}