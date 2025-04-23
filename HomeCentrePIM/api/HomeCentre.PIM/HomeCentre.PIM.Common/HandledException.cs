using System;
using System.Net;

namespace HomeCentre.PIM.Common
{
    public class HandledException : Exception
    {
        public HandledException(int code)
            : base()
        {
            Data["code"] = code;
            Data["message"] = Message;
            Data["httpCode"] = HttpStatusCode.InternalServerError;
        }

        public HandledException(string message)
            : base()
        {
            Data["message"] = message;
            Data["code"] = 500;
            Data["httpCode"] = HttpStatusCode.InternalServerError;
        }

        public HandledException(string message, int code)
        {
            Data["code"] = code;
            Data["message"] = message;
            Data["httpCode"] = HttpStatusCode.InternalServerError;
        }

        public HandledException(HttpStatusCode httpCode)
        {
            Data["httpCode"] = (int)httpCode;
            Data["code"] = 500;
            Data["message"] = Message;
        }

        public HandledException(int code, HttpStatusCode httpCode)
        {
            Data["httpCode"] = (int)httpCode;
            Data["code"] = code;
            Data["message"] = Message;
        }

        public HandledException(string message, int code, HttpStatusCode httpCode)
        {
            Data["httpCode"] = (int)httpCode;
            Data["code"] = code;
            Data["message"] = message;
        }

        public HandledException(string message, HttpStatusCode code)
        {
            Data["httpCode"] = HttpStatusCode.InternalServerError;
            Data["code"] = (int)code;
            Data["message"] = message;
        }
    }
}