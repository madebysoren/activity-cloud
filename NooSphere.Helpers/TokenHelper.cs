﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Web;

namespace NooSphere.Helpers
{
    public class TokenHelper
    {
        public static Dictionary<string, string> GetNameValues(string token)
        {
            if (string.IsNullOrEmpty(token))
                throw new ArgumentException();

            return
                token
                .Split('&')
                .Aggregate(
                new Dictionary<string, string>(),
                (dict, rawNameValue) =>
                {
                    if (rawNameValue == string.Empty)
                        return dict;

                    string[] nameValue = rawNameValue.Split('=');

                    if (nameValue.Length != 2)
                        throw new ArgumentException("Invalid formEncodedstring - contains a name/value pair missing an = character");

                    if (dict.ContainsKey(nameValue[0]) == true)
                        throw new ArgumentException("Repeated name/value pair in form");

                    dict.Add(HttpUtility.UrlDecode(nameValue[0]), HttpUtility.UrlDecode(nameValue[1]));
                    return dict;
                });
        }

    }
}
