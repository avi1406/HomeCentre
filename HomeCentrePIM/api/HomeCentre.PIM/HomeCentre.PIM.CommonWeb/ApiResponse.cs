namespace HomeCentre.PIM.CommonWeb
{
    /// <summary>
    /// Api Response Model
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class ApiResponse<T>
    {
        /// <summary>
        /// gets or sets http error codes
        /// </summary>
        public int? Code { get; set; }

        /// <summary>
        /// gets or sets http request key
        /// </summary>
        public string RequestKey { get; set; }

        /// <summary>
        /// Get or sets Message
        /// </summary>
        public string Message { get; set; }

        /// <summary>
        /// get or sets detail
        /// </summary>
        public string Detail { get; set; }

        /// <summary>
        /// get or sets Result
        /// </summary>
        public T Result { get; set; }

        /// <summary>
        /// get or sets haserror
        /// </summary>
        public bool HasError { get; set; }
    }

    /// <summary>
    /// Api response 
    /// </summary>
    public class ApiResponse : ApiResponse<object>
    {
    }
}