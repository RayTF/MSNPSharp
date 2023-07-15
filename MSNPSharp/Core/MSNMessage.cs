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
using System.Text;
using System.Collections;
using System.Globalization;

namespace MSNPSharp.Core
{
    using MSNPSharp;

    [Serializable()]
    public class MSNMessage : NetworkMessage, ICloneable
    {
        protected int transactionID = -1;
        private string command = "MSG";
        private ArrayList commandValues;
        private const string noTransactionIDCommands = "OUT PNG QNG RNG";

        public MSNMessage()
        {
            commandValues = new ArrayList();
        }

        public MSNMessage(string command, ArrayList commandValues)
        {
            Command = command;
            CommandValues = commandValues;
        }

        public override void PrepareMessage()
        {
            base.PrepareMessage();
        }


        public int TransactionID
        {
            get
            {
                return transactionID;
            }
            set
            {
                transactionID = value;
            }
        }

        public string Command
        {
            get
            {
                return command;
            }
            set
            {
                command = value;
            }
        }

        public ArrayList CommandValues
        {
            get
            {
                return commandValues;
            }
            set
            {
                commandValues = value;
            }
        }

        public override byte[] GetBytes()
        {

            StringBuilder builder = new StringBuilder(128);
            builder.Append(Command);

            if (noTransactionIDCommands.IndexOf(Command) == -1)
            {
                if (TransactionID != -1)
                {
                    builder.Append(' ');
                    builder.Append(TransactionID.ToString(CultureInfo.InvariantCulture));
                }
            }

            foreach (string val in CommandValues)
            {
                builder.Append(' ');
                builder.Append(val);
            }



            if (InnerMessage != null)
            {
                builder.Append(' ');
                builder.Append(InnerMessage.GetBytes().Length);
                builder.Append("\r\n");
                return AppendArray(System.Text.Encoding.UTF8.GetBytes(builder.ToString()), InnerMessage.GetBytes());
            }
            else
            {
                builder.Append("\r\n");
                return System.Text.Encoding.UTF8.GetBytes(builder.ToString());
            }
        }

        public override void ParseBytes(byte[] data)
        {

            int cnt = 0;
            int bodyStart = 0;

            while (data[cnt] != '\r')
            {
                cnt++;

                // watch out for buffer overflow
                if (cnt == data.Length)
                    throw new MSNPSharpException("Parsing of incoming command message failed. No newline was detected.");
            }

            bodyStart = cnt + 1;
            while (bodyStart < data.Length && (data[bodyStart] == '\r' || data[bodyStart] == '\n'))
            {
                bodyStart++;
            }

            // get the command parameters
            Command = System.Text.Encoding.UTF8.GetString(data, 0, 3);
            string commandLine = Encoding.UTF8.GetString(data, 4, cnt - 4);
            CommandValues = new ArrayList(commandLine.Split(new char[] { ' ' }));


                //Filter those commands follow by a number but not transaction id.
            if (noTransactionIDCommands.IndexOf(Command) == -1)
            {
                if (CommandValues.Count > 0)
                {
                    if (!int.TryParse((string)CommandValues[0], out transactionID))
                    {
                        transactionID = -1;  //if there's no transid, set to -1
                    }
                    else
                    {
                        CommandValues.RemoveAt(0);
                    }
                }

            }

            // set the inner body contents, if it is available
            if (bodyStart < data.Length)
            {
                if (CommandValues.Count > 0)
                {
                    CommandValues.RemoveAt(CommandValues.Count - 1);
                }

                int startIndex = bodyStart;
                int newLength = data.Length - startIndex;
                InnerBody = new byte[newLength];
                Array.Copy(data, startIndex, InnerBody, 0, newLength);
            }


        }

        /// <summary>
        /// Get the debug string representation of the message
        /// </summary>
        /// <returns></returns>
        /// <remarks>
        /// To futher developers:
        /// You cannot simply apply Encoding.UTF8.GetString(GetByte()) in this function 
        /// since the InnerMessage of MSNMessage may contain binary data.
        /// </remarks>
        public override string ToString()
        {
            StringBuilder builder = new StringBuilder(128);
            builder.Append(Command);


            if (noTransactionIDCommands.IndexOf(Command) == -1)
            {
                if (TransactionID != -1)
                {
                    builder.Append(' ');
                    builder.Append(TransactionID.ToString(CultureInfo.InvariantCulture));
                }
            }


            foreach (string val in CommandValues)
            {
                builder.Append(' ');
                builder.Append(val);
            }


            if (InnerMessage != null)
            {
                builder.Append(' ');
                builder.Append(InnerMessage.GetBytes().Length);
            }

            //For toString, we do not return the inner message's string.
            return builder.ToString();
        }

        #region ICloneable Member

        object ICloneable.Clone()
        {
            MSNMessage messageClone = new MSNMessage();
            messageClone.ParseBytes(GetBytes());
            return messageClone;
        }

        #endregion
    }
};
