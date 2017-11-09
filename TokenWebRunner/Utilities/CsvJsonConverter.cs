using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace TokenWebRequster.Utilities
{
    public static class CsvJsonConverter
    {
        public static IEnumerable<string> ConvertToJson(StreamReader fileStream)
        {
            var header = fileStream.ReadLine();
            var headers = header.SplitQuotedLine();
            String line;
            while ((line = fileStream.ReadLine()) != null)
            {
                yield return ConvertToJson(headers, line);
            }
        }
        private static string ConvertToJson(string[] headers, string row)
        {
            var rows = row.SplitQuotedLine();
            var newDiction = new Dictionary<string, object>(1);
            for (int i = 0; i < rows.Length; i++)
            {
                var obj = GetValidJson(rows[i]);
                newDiction.Add(headers[i], obj);
            }
            var result = JsonConvert.SerializeObject(newDiction);
            return result;
        }
        private static object GetValidJson(string strInput)
        {
            var strVal = strInput.Trim();
            if ((strVal.StartsWith("{") && strVal.EndsWith("}")) || //For object
                (strVal.StartsWith("[") && strVal.EndsWith("]"))) //For array
            {
                try
                {
                    var obj = JToken.Parse(strVal);
                    return obj;
                }
                catch { }
            }
            return strVal;
        }
        private static string[] SplitQuotedLine(this string value)
        {
            var regex = new Regex("(?:^|,)(\"(?:[^\"]+|\"\")*\"|[^,]*)", RegexOptions.Compiled);
            var list = new List<string>(1);
            foreach (Match match in regex.Matches(value))
            {
                var val = match.Value;
                if (val.Length == 0)
                {
                    list.Add("");
                }
                list.Add(val.TrimStart(',').Trim('"').Replace("\"\"", "\""));
            }
            return list.ToArray();
        }
    }
}
