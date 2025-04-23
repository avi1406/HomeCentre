namespace HomeCentre.PIM.BAL.Implementations.B2C.Graph.Helpers
{
    internal class B2CCustomAttributeHelper
    {
        private readonly string b2CExtensionAppClientId;

        internal B2CCustomAttributeHelper(string b2CExtensionAppClientId)
        {
            this.b2CExtensionAppClientId = b2CExtensionAppClientId.Replace("-", string.Empty);
        }

        internal string GetCompleteAttributeName(string attributeName)
        {
            if (string.IsNullOrWhiteSpace(attributeName))
            {
                throw new System.ArgumentException("Parameter cannot be null", nameof(attributeName));
            }

            return $"extension_{b2CExtensionAppClientId}_{attributeName}";
        }
    }
}
