//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE.md file in the project root for full license information.
//

using System.IO;
using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;

namespace Azure.AI.Details.Common.CLI
{
    internal static class DiagnosticsInterop
    {
        public static void diagnostics_log_trace_string(
            int level,
            string title,
            string message,
            [CallerFilePath] string? fileName = null,
            [CallerLineNumber] int lineNumber = 0)
        {
            if (message.Contains("\"key\""))
            {
                message = Regex.Replace(message, "\"key\"\\s*:\\s*\"[^\"]*\"", "\"key\":\"REDACTED\"");
            }
            if (message.Contains("\"embeddingKey\""))
            {
                message = Regex.Replace(message, "\"embeddingKey\"\\s*:\\s*\"[^\"]*\"", "\"embeddingKey\":\"REDACTED\"");
            }

            if (message.Length > 1024) message = message.Substring(0, 1024);

            using var nativeTitle = NativeUtils.ToNativeNullTerminatedUtf8String(title);
            using var nativeMessage = NativeUtils.ToNativeNullTerminatedUtf8String(message);
            using var nativeFileName = NativeUtils.ToNativeNullTerminatedUtf8String(Path.GetFileName(fileName));

            SpeechNativeMethods.diagnostics_log_trace_string(level, nativeTitle.Handle, nativeFileName.Handle, lineNumber, nativeMessage.Handle);
        }

        public const int __TRACE_LEVEL_INFO = 0x08; // Trace_Info
        public const int __TRACE_LEVEL_WARNING = 0x04; // Trace_Warning
        public const int __TRACE_LEVEL_ERROR = 0x02; // Trace_Error
        public const int __TRACE_LEVEL_VERBOSE = 0x10; // Trace_Verbose
    }
}