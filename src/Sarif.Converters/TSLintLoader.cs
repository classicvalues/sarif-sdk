﻿// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Json;
using System.Text;
using Microsoft.CodeAnalysis.Sarif.Converters.TSLintObjectModel;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Microsoft.CodeAnalysis.Sarif.Converters
{
    public class TSLintLoader : ITSLintLoader
    {
        private readonly XmlObjectSerializer Serializer;

        public TSLintLoader()
        {
            Serializer = new DataContractJsonSerializer(typeof(TSLintLog));
        }

        /// <summary>
        /// A constructor used for test purposes (to allow mocking the serializer)
        /// </summary>
        /// <param name="serializer"></param>
        internal TSLintLoader(XmlObjectSerializer serializer)
        {
            Serializer = serializer ?? throw new ArgumentNullException(nameof(serializer));
        }

        public TSLintLog ReadLog(string input)
        {
            return ReadLog(input, Encoding.UTF8);
        }

        public TSLintLog ReadLog(string input, Encoding encoding)
        {
            return ReadLog(new MemoryStream(encoding.GetBytes(input)));
        }

        public TSLintLog ReadLog(Stream input)
        {
            input = input ?? throw new ArgumentNullException(nameof(input));

            using (TextReader streamReader = new StreamReader(input))
            using (JsonReader reader = new JsonTextReader(streamReader))
            {
                JToken rootToken = JToken.ReadFrom(reader);
                rootToken = NormalizeLog(rootToken);
                string normalizedLogContents = rootToken.ToString();
                using (Stream normalizedLogStream = new MemoryStream(Encoding.UTF8.GetBytes(normalizedLogContents)))
                {
                    return (TSLintLog)Serializer.ReadObject(normalizedLogStream);
                }
            }
        }

        // This method transforms all "fix" properties in the input to a standard form
        // by wrapping the property value in an array if it is not already an array.
        //
        // The input is a JSON token representing the entire TSLint log file. The method
        // modifies the input token in place.
        //
        // This method returns the same input value that it modified in place.
        //
        // This is necessary because the TSLint JSON contains multiple patterns for fix, i.e.:
        //
        // "fix":{"innerStart":4429,"innerLength":0,"innerText":"\r\n"}
        // "fix":[{"innerStart":4429,"innerLength":0,"innerText":"\r\n"}]
        // "fix":[{"innerStart":4429,"innerLength":0,"innerText":"\r\n"},{"innerStart":4429,"innerLength":0,"innerText":"\r\n"}]
        //
        // This method is marked internal rather than private for the sake of unit tests.
        internal JToken NormalizeLog(JToken rootToken)
        {
            if (rootToken is JArray entries)
            {
                NormalizeEntries(entries);
            }
            else
            {
                throw new Exception(
                    string.Format(
                        CultureInfo.InvariantCulture,
                        "The root JSON value should be a JArray, but is a {1}.",
                        rootToken.GetType().Name));
            }

            return rootToken;
        }

        private void NormalizeEntries(JArray entries)
        {
            foreach (JToken entryToken in entries)
            {
                if (entryToken is JObject entry)
                {
                    NormalizeEntry(entry);
                }
                else
                {
                    var lineInfo = entryToken as IJsonLineInfo;
                    throw new Exception(
                        string.Format(
                            CultureInfo.InvariantCulture,
                            "({0}, {1}): The JSON value should be a JObject, but is a {2}.",
                            lineInfo.LineNumber,
                            lineInfo.LinePosition,
                            entryToken.GetType().Name));
                }
            }
        }

        private void NormalizeEntry(JObject entry)
        {
            JProperty fixProperty = entry.Properties().SingleOrDefault(p => p.Name.Equals("fix"));
            if (fixProperty != null)
            {
                NormalizeFixProperty(fixProperty);
            }
        }

        private static void NormalizeFixProperty(JProperty fixProperty)
        {
            var fixValueToken = fixProperty.Value;

            // If the property value isn't already an array...
            var fixValueArray = fixValueToken as JArray;
            if (fixValueArray == null)
            {
                var fixValueObject = fixValueToken as JObject;
                if (fixValueObject == null)
                {
                    var lineInfo = fixValueToken as IJsonLineInfo;
                    throw new Exception(
                        string.Format(
                            CultureInfo.InvariantCulture,
                           "({0}, {1}): The value of the 'fix' property should be either a JObject or a JArray, but is a {2}.",
                           lineInfo.LineNumber,
                           lineInfo.LinePosition,
                           fixValueToken.GetType().Name));
                }

                // ... then wrap it in an array.
                fixProperty.Value = new JArray(fixValueToken);
            }
        }
    }
}
