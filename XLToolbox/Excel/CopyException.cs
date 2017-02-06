﻿/* CopyException.cs
 * part of Daniel's XL Toolbox NG
 * 
 * Copyright 2014-2017 Daniel Kraus
 * 
 * Licensed under the Apache License, Version 2.0 (the "License");
 * you may not use this file except in compliance with the License.
 * You may obtain a copy of the License at
 * 
 *     http://www.apache.org/licenses/LICENSE-2.0
 * 
 * Unless required by applicable law or agreed to in writing, software
 * distributed under the License is distributed on an "AS IS" BASIS,
 * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 * See the License for the specific language governing permissions and
 * limitations under the License.
 */
using System;
using System.Runtime.Serialization;

namespace XLToolbox.Excel
{
    [Serializable]
    class CopyException : Exception
    {
        public CopyException() { }
        public CopyException(string message) : base(message) { }
        public CopyException(string message,
            Exception innerException)
            : base(message, innerException) { }
        public CopyException(SerializationInfo info,
            StreamingContext context)
            : base(info, context) { }
    }
}