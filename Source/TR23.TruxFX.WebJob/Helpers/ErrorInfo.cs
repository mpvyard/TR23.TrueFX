// Copyright (C) SquidEyes, LLC. - All Rights Reserved
// Unauthorized copying of this file, via any medium is strictly prohibited
// Proprietary and Confidential
// Written by Louis S. Berman <louis@squideyes.com>, 7/19/2016

using System;

namespace TR23.TrueFX.WebJob.Helpers
{
    public class ErrorInfo
    {
        public string ErrorType { get; set; }
        public string Message { get; set; }
        public string Source { get; set; }
        public string TargetSite { get; set; }
        public string StackTrace { get; set; }
        public ErrorInfo InnerErrorInfo { get; set; }

        internal static ErrorInfo Create(Exception error)
        {
            ErrorInfo result;

            var errorInfo = result = GetErrorInfo(error);

            while (error != null)
            {
                if (error.InnerException != null)
                {
                    errorInfo.InnerErrorInfo = GetErrorInfo(error.InnerException);

                    errorInfo = errorInfo.InnerErrorInfo;
                }

                error = error.InnerException;
            }

            return result;
        }

        private static ErrorInfo GetErrorInfo(Exception error)
        {
            return new ErrorInfo()
            {
                Message = error.Message,
                ErrorType = error.GetType().ToString(),
                Source = error.Source,
                StackTrace = error.StackTrace,
                TargetSite = error.TargetSite.GetInterface()
            };
        }
    }
}
