using System.Collections;
using System.Collections.Generic;
using System;
using Unity.Networking.Transport;
using UnityEngine;

namespace MessageSending
{
    public static class MessageParser
    {
        /// <summary>
        /// Parses data from streamReader according to the CommandType in the Message Header
        /// </summary>
        public static void parse(DataStreamReader streamReader)
        {
            //Tracks where in the data stream you are and how much you've read
            var readerContext = default(DataStreamReader.Context);

            //Attempt to read Message byte array from streamReader
            byte[] msgBytes = streamReader.ReadBytesAsArray(ref readerContext, streamReader.Length);

            Debug.Log(msgBytes.Length);

            //Convert msgBytes to object and attempt to cast as a Message
            Message msgRecieved = (Message) Helpers.byteArrayToObject(msgBytes);

            if (msgRecieved != null) //Message object was recieved
            {
                msgRecieved.process(); //Process the message data
            }
        }
    }
}
