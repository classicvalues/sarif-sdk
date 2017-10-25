﻿// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.IO;
using FluentAssertions;
using Microsoft.CodeAnalysis.Sarif.Converters.TSLintObjectModel;
using Newtonsoft.Json.Linq;
using Xunit;

namespace Microsoft.CodeAnalysis.Sarif.Converters
{
    public class TSLintLoaderTests
    {
        [Fact]
        public void TSLintLoader_ReadLog_WhenInputIsNull_ThrowsArgumentNullException()
        {
            TSLintLoader loader = new TSLintLoader();

            Action action = () => loader.ReadLog(default(Stream));
            action.ShouldThrow<ArgumentNullException>();
        }

        [Fact]
        public void TSLintLoader_ReadLog_ProducesExpectedLog()
        {
            const string Input = @"
            [
                {
                    ""endPosition"":
                        {
                            ""character"":1,
                            ""line"":113,
                            ""position"":4429
                        },
                    ""failure"":""file should end with a newline"",
                    ""fix"":
                    {
                        ""innerStart"":4429,
                        ""innerLength"":0,
                        ""innerText"":""\r\n""
                    },
                    ""name"":""SecureApp/js/index.d.ts"",
                    ""ruleName"":""eofline"",
                    ""ruleSeverity"":""ERROR"",
                    ""startPosition"":
                    {
                        ""character"":1,
                        ""line"":113,
                        ""position"":4429
                    }
                }
            ]";

            TSLintLog expectedLog = new TSLintLog
            {
                new TSLintLogEntry
                {
                    EndPosition = new TSLintLogPosition
                    {
                        Character = 1,
                        Line = 113,
                        Position = 4429
                    },
                    Failure = "file should end with a newline",
                    Fixes = new List<TSLintLogFix>
                    {
                        new TSLintLogFix
                        {
                            InnerStart = 4429,
                            InnerLength = 0,
                            InnerText = "\r\n"
                        }
                    },
                    Name = "SecureApp/js/index.d.ts",
                    RuleName = "eofline",
                    RuleSeverity = "ERROR",
                    StartPosition = new TSLintLogPosition
                    {
                        Character = 1,
                        Line = 113,
                        Position = 4429
                    }
                }
            };

            TSLintLoader loader = new TSLintLoader();

            TSLintLog actualLog = loader.ReadLog(Input);

            CompareLogs(actualLog, expectedLog);
        }

        [Fact]
        public void TSLintLoader_NormalizeLog_WrapsSingleFixInArray()
        {
            const string Input = @"
            [
                {
                    ""endPosition"":
                        {
                            ""character"":1,
                            ""line"":113,
                            ""position"":4429
                        },
                    ""failure"":""file should end with a newline"",
                    ""fix"":
                    {
                        ""innerStart"":4429,
                        ""innerLength"":0,
                        ""innerText"":""\r\n""
                    },
                    ""name"":""SecureApp/js/index.d.ts"",
                    ""ruleName"":""eofline"",
                    ""ruleSeverity"":""ERROR"",
                    ""startPosition"":
                    {
                        ""character"":1,
                        ""line"":113,
                        ""position"":4429
                    }
                }
            ]";

            const string ExpectedOutput = @"
            [
                {
                    ""endPosition"":
                        {
                            ""character"":1,
                            ""line"":113,
                            ""position"":4429
                        },
                    ""failure"":""file should end with a newline"",
                    ""fix"":
                    [{
                        ""innerStart"":4429,
                        ""innerLength"":0,
                        ""innerText"":""\r\n""
                    }],
                    ""name"":""SecureApp/js/index.d.ts"",
                    ""ruleName"":""eofline"",
                    ""ruleSeverity"":""ERROR"",
                    ""startPosition"":
                    {
                        ""character"":1,
                        ""line"":113,
                        ""position"":4429
                    }
                }
            ]";

            JToken expectedToken = JToken.Parse(ExpectedOutput);

            JToken inputToken = JToken.Parse(Input);
            TSLintLoader loader = new TSLintLoader();
            JToken actualToken = loader.NormalizeLog(inputToken);

            JToken.DeepEquals(expectedToken, actualToken).Should().BeTrue();
        }

        private static void CompareLogs(TSLintLog actualLog, TSLintLog expectedLog)
        {
            actualLog.Count.Should().Be(expectedLog.Count);

            for (int i = 0; i < actualLog.Count; ++i)
            {
                CompareEntries(actualLog[i], expectedLog[i]);
            }
        }

        private static void CompareEntries(TSLintLogEntry actualEntry, TSLintLogEntry expectedEntry)
        {
            actualEntry.EndPosition.Character.Should().Be(expectedEntry.EndPosition.Character);
            actualEntry.EndPosition.Line.Should().Be(expectedEntry.EndPosition.Line);
            actualEntry.EndPosition.Position.Should().Be(expectedEntry.EndPosition.Position);
            actualEntry.Failure.Should().Be(expectedEntry.Failure);

            CompareFixes(actualEntry.Fixes, expectedEntry.Fixes);

            actualEntry.Name.Should().Be(expectedEntry.Name);
            actualEntry.RuleName.Should().Be(expectedEntry.RuleName);
            actualEntry.RuleSeverity.Should().Be(expectedEntry.RuleSeverity);
            actualEntry.StartPosition.Character.Should().Be(expectedEntry.StartPosition.Character);
            actualEntry.EndPosition.Line.Should().Be(expectedEntry.StartPosition.Line);
            actualEntry.EndPosition.Position.Should().Be(expectedEntry.StartPosition.Position);
        }

        private static void CompareFixes(IList<TSLintLogFix> actualFixes, IList<TSLintLogFix> expectedFixes)
        {
            (actualFixes == null).Should().Be(expectedFixes == null);

            if (actualFixes != null)
            {
                actualFixes.Count.Should().Be(expectedFixes.Count);

                for (int i = 0; i < actualFixes.Count; ++i)
                {
                    actualFixes[i].InnerStart.Should().Be(expectedFixes[i].InnerStart);
                    actualFixes[i].InnerLength.Should().Be(expectedFixes[i].InnerLength);
                    actualFixes[i].InnerText.Should().Be(expectedFixes[i].InnerText);
                }
            }
        }
    }
}
