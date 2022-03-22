using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Resources;
using System.Text;

namespace CodeKicker.BBCode.Core
{
    static class MessagesHelper
    {
        static readonly ResourceManager resMgr;

        static MessagesHelper()
        {
            resMgr = new ResourceManager(typeof(Messages));
        }

        public static string? GetString(string key)
        {
            return resMgr.GetString(key);
        }
        public static string? GetString(string key, params string[] parameters)
        {
            var format = resMgr.GetString(key);
            return string.IsNullOrWhiteSpace(format) ? format : string.Format(format, parameters);
        }
    }

    /// <summary>
    /// reflection-only use
    /// </summary>
    static class Messages
    {
    }
}
