using HomeCentre.PIM.Common;
using Microsoft.AspNetCore.Mvc;

namespace HomeCentre.PIM.CommonWeb
{
    public class BaseApiController : Controller
    {
        /// <summary>
        /// Success
        /// </summary>
        /// <returns></returns>
        [ApiExplorerSettings(IgnoreApi = true)]
        [IgnoreForSwagger]
        public new ApiResponse<bool> Ok()
        {
            return new ApiResponse<bool> { HasError = false, Code = 200 };
        }

        /// <summary>
        /// Set success/error
        /// </summary>
        /// <param name="status"></param>
        /// <returns></returns>
        [ApiExplorerSettings(IgnoreApi = true)]
        [IgnoreForSwagger]
        public new ApiResponse<bool> Ok(bool status)
        {
            return new ApiResponse<bool>
            {
                HasError = !status,
                Code = status ? 200 : 500
            };
        }

        /// <summary>
        /// Succuss with code
        /// </summary>
        /// <param name="code"></param>
        /// <returns></returns>
        [ApiExplorerSettings(IgnoreApi = true)]
        [IgnoreForSwagger]
        public new ApiResponse<bool> Ok(int code)
        {
            return new ApiResponse<bool> { HasError = false, Code = code };
        }

        /// <summary>
        /// Success/Error with code
        /// </summary>
        /// <param name="status"></param>
        /// <param name="code"></param>
        /// <returns></returns>
        [ApiExplorerSettings(IgnoreApi = true)]
        [IgnoreForSwagger]
        public new ApiResponse<bool> Ok(bool status, int code)
        {
            return new ApiResponse<bool> { HasError = !status, Code = code };
        }

        /// <summary>
        /// Success with data
        /// </summary>
        /// <typeparam name="TValue"></typeparam>
        /// <param name="data"></param>
        /// <returns></returns>
        [ApiExplorerSettings(IgnoreApi = true)]
        [IgnoreForSwagger]
        public new ApiResponse<TValue> Ok<TValue>(TValue data)
        {
            return new ApiResponse<TValue> { Result = data, HasError = false, Code = 200 };
        }

        /// <summary>
        /// Success with data and code
        /// </summary>
        /// <typeparam name="TValue"></typeparam>
        /// <param name="data"></param>
        /// <param name="code"></param>
        /// <returns></returns>
        [ApiExplorerSettings(IgnoreApi = true)]
        [IgnoreForSwagger]
        public new ApiResponse<TValue> Ok<TValue>(bool status, TValue data, int code)
        {
            return new ApiResponse<TValue> { Result = data, HasError = !status, Code = code };
        }

        /// <summary>
        /// Success with data and code
        /// </summary>
        /// <typeparam name="TValue"></typeparam>
        /// <param name="data"></param>
        /// <param name="code"></param>
        /// <returns></returns>
        [ApiExplorerSettings(IgnoreApi = true)]
        [IgnoreForSwagger]
        public new ApiResponse<TValue> Ok<TValue>(TValue data, bool status, int code)
        {
            return new ApiResponse<TValue> { Result = data, HasError = !status, Code = code };
        }

        /// <summary>
        /// Success with data and code
        /// </summary>
        /// <typeparam name="TValue"></typeparam>
        /// <param name="data"></param>
        /// <param name="code"></param>
        /// <returns></returns>
        [ApiExplorerSettings(IgnoreApi = true)]
        [IgnoreForSwagger]
        public new ApiResponse<TValue> Ok<TValue>(TValue data, int code)
        {
            return new ApiResponse<TValue> { Result = data, HasError = false, Code = code };
        }

        /// <summary>
        /// Success with data and code
        /// </summary>
        /// <typeparam name="TValue"></typeparam>
        /// <param name="code"></param>
        /// <param name="data"></param>
        /// <returns></returns>
        [ApiExplorerSettings(IgnoreApi = true)]
        [IgnoreForSwagger]
        public ApiResponse<TValue> Ok<TValue>(int code, TValue data)
        {
            return new ApiResponse<TValue> { Result = data, HasError = false, Code = code };
        }
    }
}