#region Copyright (c) 2002-2011, Bas Geertsema, Xih Solutions (http://www.xihsolutions.net), Thiago.Sayao, Pang Wu, Ethem Evlice
/*
Copyright (c) 2002-2011, Bas Geertsema, Xih Solutions
(http://www.xihsolutions.net), Thiago.Sayao, Pang Wu, Ethem Evlice.
All rights reserved. http://code.google.com/p/msnp-sharp/

Redistribution and use in source and binary forms, with or without
modification, are permitted provided that the following conditions are met:

* Redistributions of source code must retain the above copyright notice,
  this list of conditions and the following disclaimer.
* Redistributions in binary form must reproduce the above copyright notice,
  this list of conditions and the following disclaimer in the documentation
  and/or other materials provided with the distribution.
* Neither the names of Bas Geertsema or Xih Solutions nor the names of its
  contributors may be used to endorse or promote products derived from this
  software without specific prior written permission.

THIS SOFTWARE IS PROVIDED BY THE COPYRIGHT HOLDERS AND CONTRIBUTORS 'AS IS'
AND ANY EXPRESS OR IMPLIED WARRANTIES, INCLUDING, BUT NOT LIMITED TO, THE
IMPLIED WARRANTIES OF MERCHANTABILITY AND FITNESS FOR A PARTICULAR PURPOSE
ARE DISCLAIMED. IN NO EVENT SHALL THE COPYRIGHT OWNER OR CONTRIBUTORS BE
LIABLE FOR ANY DIRECT, INDIRECT, INCIDENTAL, SPECIAL, EXEMPLARY, OR
CONSEQUENTIAL DAMAGES (INCLUDING, BUT NOT LIMITED TO, PROCUREMENT OF
SUBSTITUTE GOODS OR SERVICES; LOSS OF USE, DATA, OR PROFITS; OR BUSINESS
INTERRUPTION) HOWEVER CAUSED AND ON ANY THEORY OF LIABILITY, WHETHER IN
CONTRACT, STRICT LIABILITY, OR TORT (INCLUDING NEGLIGENCE OR OTHERWISE)
ARISING IN ANY WAY OUT OF THE USE OF THIS SOFTWARE, EVEN IF ADVISED OF
THE POSSIBILITY OF SUCH DAMAGE. 
*/
#endregion

using System;
using System.IO;
using System.Collections;
using System.Diagnostics;
using System.Collections.Generic;

namespace MSNPSharp.DataTransfer
{
    using MSNPSharp;
    using MSNPSharp.Core;

    /// <summary>
    /// Buffers incompleted P2PMessage SLP messages.
    /// </summary>
    public class P2PMessagePool
    {
        private Dictionary<uint, P2PMessage> incompletedP2PV1Messages = new Dictionary<uint, P2PMessage>();
        private Dictionary<uint, P2PMessage> incompletedP2PV2Messages = new Dictionary<uint, P2PMessage>();

        /// <summary>
        /// Buffers incompleted P2PMessage SLP messages. Ignores data and control messages. 
        /// </summary>
        /// <param name="p2pMessage"></param>
        /// <returns>
        /// true if the P2PMessage is buffering (not completed) or invalid packet received;
        /// false if the p2p message fully buffered or no need to buffer.
        /// </returns>
        public bool BufferMessage(ref P2PMessage p2pMessage)
        {
            // P2PV1 and P2PV2 check
            if (p2pMessage.Header.MessageSize == 0 || // Ack message or Unsplitted
                p2pMessage.Header.SessionId > 0) // Data message
            {
                return false; // No need to buffer
            }

            // P2PV2 pooling
            if (p2pMessage.Version == P2PVersion.P2PV2)
            {
                if ((p2pMessage.V2Header.TFCombination == TFCombination.First && p2pMessage.V2Header.DataRemaining == 0) || // Unsplitted SLP message or data preparation message
                    (p2pMessage.V2Header.TFCombination > TFCombination.First)) // Data message
                {
                    return false; // No need to buffer
                }

                // First splitted SLP message.
                if (p2pMessage.V2Header.TFCombination == TFCombination.First &&
                    p2pMessage.V2Header.DataRemaining > 0)
                {
                    P2PMessage totalMessage = new P2PMessage(p2pMessage); // Copy it
                    ulong totalSize = (ulong)(p2pMessage.V2Header.MessageSize - p2pMessage.V2Header.DataPacketHeaderLength) +
                        p2pMessage.V2Header.DataRemaining;

                    totalMessage.InnerBody = new byte[totalSize]; // Allocate buffer as needed
                    Array.Copy(p2pMessage.InnerBody, 0, totalMessage.InnerBody, (long)0, (long)p2pMessage.InnerBody.Length);

                    lock (incompletedP2PV2Messages)
                        incompletedP2PV2Messages[p2pMessage.V2Header.Identifier + p2pMessage.V2Header.MessageSize] = totalMessage;

                    return true; // Buffering
                }

                // Other splitted SLP messages
                if (p2pMessage.V2Header.TFCombination == TFCombination.None)
                {
                    lock (incompletedP2PV2Messages)
                    {
                        if (incompletedP2PV2Messages.ContainsKey(p2pMessage.V2Header.Identifier))
                        {
                            if (incompletedP2PV2Messages[p2pMessage.V2Header.Identifier].V2Header.PackageNumber == p2pMessage.V2Header.PackageNumber)
                            {
                                P2PMessage totalMessage = incompletedP2PV2Messages[p2pMessage.V2Header.Identifier];
                                ulong dataSize = Math.Min(((ulong)(p2pMessage.V2Header.MessageSize - p2pMessage.V2Header.DataPacketHeaderLength)), totalMessage.V2Header.DataRemaining);
                                ulong offSet = ((ulong)totalMessage.InnerBody.LongLength) - totalMessage.V2Header.DataRemaining;

                                // Check range and buffer overflow...
                                if (((p2pMessage.V2Header.DataRemaining + (ulong)dataSize) == totalMessage.V2Header.DataRemaining) &&
                                    (ulong)(dataSize + offSet + p2pMessage.V2Header.DataRemaining) == (ulong)totalMessage.InnerBody.LongLength)
                                {
                                    Array.Copy(p2pMessage.InnerBody, 0, totalMessage.InnerBody, (long)offSet, (long)dataSize);
                                    uint originalIdentifier = p2pMessage.V2Header.Identifier;
                                    uint newIdentifier = p2pMessage.V2Header.Identifier + p2pMessage.V2Header.MessageSize;

                                    totalMessage.V2Header.DataRemaining = p2pMessage.V2Header.DataRemaining;
                                    totalMessage.V2Header.Identifier = newIdentifier;

                                    if (originalIdentifier != newIdentifier)
                                    {
                                        incompletedP2PV2Messages.Remove(originalIdentifier);
                                    }

                                    if (p2pMessage.V2Header.DataRemaining > 0)
                                    {
                                        incompletedP2PV2Messages[newIdentifier] = totalMessage;

                                        // Don't debug p2p packet here. Because it hasn't completed yet and SLPMessage.Parse() fails...
                                        Trace.WriteLineIf(Settings.TraceSwitch.TraceInfo, "Buffering splitted messages and hasn't completed yet! DataRemaining:" + totalMessage.V2Header.DataRemaining);
                                        return true; // Buffering
                                    }
                                    else // Last part
                                    {
                                        totalMessage.InnerBody = totalMessage.InnerBody; // Refresh... DataRemaining=0 deletes data headers.
                                        totalMessage.V2Header.Identifier = newIdentifier - totalMessage.Header.MessageSize;

                                        Trace.WriteLineIf(Settings.TraceSwitch.TraceInfo,
                                            "A splitted message was combined :\r\n" +
                                            totalMessage.ToDebugString());

                                        p2pMessage = totalMessage;
                                        return false; // We have the whole message
                                    }
                                }
                            }

                            // Invalid packet received!!! Ignore and delete it...
                            incompletedP2PV2Messages.Remove(p2pMessage.V2Header.Identifier);

                            Trace.WriteLineIf(Settings.TraceSwitch.TraceWarning,
                                "INVALID P2PV2 PACKET received!!! Ignored and deleted:\r\n" +
                                p2pMessage.ToDebugString());
                        }
                    }
                }
            }
            else // P2PV1 pooling
            {
                if ((p2pMessage.V1Header.MessageSize == p2pMessage.V1Header.TotalSize) || // Whole data
                    ((p2pMessage.V1Header.Flags & P2PFlag.Data) == P2PFlag.Data)) // Data message
                {
                    return false; // No need to buffer
                }

                lock (incompletedP2PV1Messages)
                {
                    if (false == incompletedP2PV1Messages.ContainsKey(p2pMessage.Header.Identifier))
                    {
                        byte[] totalPayload = new byte[p2pMessage.V1Header.TotalSize];
                        Array.Copy(p2pMessage.InnerBody, 0, totalPayload, (long)p2pMessage.V1Header.Offset, (long)p2pMessage.V1Header.MessageSize);
                        P2PMessage copyMessage = new P2PMessage(p2pMessage);

                        copyMessage.InnerBody = totalPayload;
                        copyMessage.V1Header.Offset = p2pMessage.V1Header.Offset + p2pMessage.V1Header.MessageSize;

                        incompletedP2PV1Messages[p2pMessage.Header.Identifier] = copyMessage;
                        return true; // Buffering
                    }

                    P2PMessage totalMessage = incompletedP2PV1Messages[p2pMessage.Header.Identifier];
                    if (p2pMessage.V1Header.TotalSize == totalMessage.V1Header.TotalSize &&
                        (p2pMessage.V1Header.Offset + p2pMessage.V1Header.MessageSize) <= totalMessage.Header.TotalSize)
                    {
                        Array.Copy(p2pMessage.InnerBody, 0, totalMessage.InnerBody, (long)p2pMessage.V1Header.Offset, (long)p2pMessage.V1Header.MessageSize);
                        totalMessage.V1Header.Offset = p2pMessage.V1Header.Offset + p2pMessage.V1Header.MessageSize;

                        // Last packet
                        if (totalMessage.V1Header.Offset == p2pMessage.V1Header.TotalSize)
                        {
                            totalMessage.V1Header.Offset = 0;
                            incompletedP2PV1Messages.Remove(p2pMessage.Header.Identifier);

                            p2pMessage = totalMessage;
                            return false; // We have the whole message
                        }

                        return true; // Buffering
                    }

                    // Invalid packet received!!! Ignore and delete it...
                    incompletedP2PV1Messages.Remove(p2pMessage.Header.Identifier);

                    Trace.WriteLineIf(Settings.TraceSwitch.TraceWarning,
                        "INVALID P2PV1 PACKET received!!! Ignored and deleted:\r\n" +
                        p2pMessage.ToDebugString());
                }
            }

            return true; // Invalid packet, don't kill me.
        }
    }
};
