// Copyright (C) SquidEyes, LLC. - All Rights Reserved
// Unauthorized copying of this file, via any medium is strictly prohibited
// Proprietary and Confidential
// Written by Louis S. Berman <louis@squideyes.com>, 7/19/2016

using System.Reflection;
using System.Text;

namespace TR23.TrueFX.WebJob.Helpers
{
    public static class MethodBaseExtenders
    {
        public static string GetInterface(this MethodBase methodBase)
        {
            var sb = new StringBuilder();

            if (methodBase is MethodInfo)
            {
                var methodInfo = (MethodInfo)methodBase;

                if (methodInfo.ReturnParameter == null)
                {
                    sb.Append("System.Void");
                }
                else
                {
                    sb.Append(methodInfo.ReturnParameter.
                        ParameterType.ToString());
                }

                sb.Append(' ');
            }

            if (methodBase.DeclaringType == null)
                sb.Append("{EmittedType}");
            else
                sb.Append(methodBase.DeclaringType.ToString());

            if (methodBase is MethodInfo)
            {
                sb.Append('.');

                sb.Append(methodBase.Name);
            }

            sb.Append('(');

            int count = 0;

            foreach (var param in methodBase.GetParameters())
            {
                count += 1;

                if (count > 1)
                    sb.Append(", ");

                sb.Append(param.ParameterType.ToString().Replace('+', '.'));

                if (!string.IsNullOrWhiteSpace(param.Name))
                {
                    sb.Append(' ');

                    sb.Append(param.Name);
                }
            }

            sb.Append(')');

            return sb.ToString();
        }
    }
}
