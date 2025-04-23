using Microsoft.AspNetCore.Mvc;

namespace HomeCentre.PIM.CommonWeb
{
    /// <summary>
    /// Authorize using multiple policies
    /// </summary>
    public class MultiplePoliciesAuthorizeAttribute : TypeFilterAttribute
    {
        /// <summary>
        /// Constructor Multiple policy authorization
        /// </summary>
        /// <param name="policies">Comma separated string with policy names</param>
        /// <param name="isAnd">Should all policies match or one should match, default is false</param>
        public MultiplePoliciesAuthorizeAttribute(string policies, bool isAnd = false)
            : base(typeof(MultiplePoliciesAuthorizeFilter))
        {
            Arguments = new object[] { policies, isAnd };
        }
    }
}