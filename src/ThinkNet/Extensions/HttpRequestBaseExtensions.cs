using System;
using System.Web;

namespace ThinkNet
{
    /// <summary>
    /// 扩展 <see cref="HttpRequestBase" /> 类，该类包含客户端在 Web 请求中发送的 HTTP 值。
    /// </summary>
    public static class HttpRequestBaseExtensions
    {
        /// <summary>
        /// 判断当前页面是否接收到了 POST 请求
        /// </summary>
        /// <param name="request">一个包含客户端在 Web 请求中发送的 HTTP 值的对象。</param>
        /// <returns>是否接收到了POST请求。</returns>
        public static bool IsPost(this HttpRequestBase request)
        {
            if (request == null) {
                throw new ArgumentNullException("request");
            }
            return request.HttpMethod.Equals("POST", StringComparison.OrdinalIgnoreCase);
        }
        /// <summary>
        /// 判断当前页面是否接收到了 GET 请求
        /// </summary>
        /// <param name="request">一个包含客户端在 Web 请求中发送的 HTTP 值的对象。</param>
        /// <returns>是否接收到了GET请求。</returns>
        public static bool IsGet(this HttpRequestBase request)
        {
            if (request == null) {
                throw new ArgumentNullException("request");
            }
            return request.HttpMethod.Equals("GET", StringComparison.OrdinalIgnoreCase);
        }

        ///// <summary>
        ///// 判断指定的 HTTP 请求是否为 AJAX 请求。
        ///// </summary>
        ///// <param name="request">一个包含客户端在 Web 请求中发送的 HTTP 值的对象。</param>
        ///// <returns>是否接收到了AJAX请求。</returns>
        //public static bool IsAjaxRequest(this HttpRequestBase request)
        //{
        //    if (request == null) {
        //        throw new ArgumentNullException("request");
        //    }
        //    return (request["X-Requested-With"] == "XMLHttpRequest") || ((request.Headers != null) && (request.Headers["X-Requested-With"] == "XMLHttpRequest"));
        //}

        /// <summary>
        /// 获取 <see cref="HttpRequestBase.QueryString" />、<see cref="HttpRequestBase.Form" />、<see cref="HttpRequestBase.ServerVariables" /> 和 <see cref="HttpRequestBase.Cookies" /> 项的集合中具有指定键的项。
        /// </summary>
        /// <param name="request">一个包含客户端在 Web 请求中发送的 HTTP 值的对象。</param>
        /// <param name="name">键值</param>
        /// <returns>该键值对应的值。</returns>
        public static string GetString(this HttpRequestBase request, string name)
        {
            if (request == null) {
                throw new ArgumentNullException("request");
            }

            if (request.Params[name] == null) {
                return string.Empty;
            }
            return request.Params[name];
        }

        /// <summary>
        /// 获得指定表单参数的值
        /// </summary>
        /// <param name="request">一个包含客户端在 Web 请求中发送的 HTTP 值的对象。</param>
        /// <param name="name">表单参数</param>
        /// <returns>表单参数的值</returns>
        public static string GetFormString(this HttpRequestBase request, string name)
        {
            if (request == null) {
                throw new ArgumentNullException("request");
            }

            if (request.Form[name] == null) {
                return string.Empty;
            }
            return request.Form[name];
        }

        /// <summary>
        /// 获得指定Url参数的值
        /// </summary>
        /// <param name="request">一个包含客户端在 Web 请求中发送的 HTTP 值的对象。</param>
        /// <param name="name">Url参数</param>
        /// <returns>Url参数的值</returns>
        public static string GetQueryString(this HttpRequestBase request, string name)
        {
            if (request == null) {
                throw new ArgumentNullException("request");
            }

            if (request.QueryString[name] == null) {
                return string.Empty;
            }
            return request.QueryString[name];
        }

        /// <summary>
        /// 获得当前页面客户端的IP
        /// </summary>
        /// <param name="request">一个包含客户端在 Web 请求中发送的 HTTP 值的对象。</param>
        /// <returns>当前页面客户端的IP</returns>
        public static string GetIP(this HttpRequestBase request)
        {
            if (request == null) {
                throw new ArgumentNullException("request");
            }

            string result = request.ServerVariables["HTTP_X_FORWARDED_FOR"];
            if (string.IsNullOrEmpty(result)) {
                result = request.ServerVariables["REMOTE_ADDR"];
            }

            if (string.IsNullOrEmpty(result)) {
                result = request.UserHostAddress;
            }

            if (string.IsNullOrEmpty(result)) {
                result = "0.0.0.0";
            }

            return result;
        }
    }
}
