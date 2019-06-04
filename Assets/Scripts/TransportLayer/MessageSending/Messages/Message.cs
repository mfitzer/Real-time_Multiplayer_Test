using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace MessageSending
{
    [System.Serializable]
    public abstract class Message
    {
        //Process message data
        public abstract bool process();

        //Returns a byte representation of the object
        public byte[] toBytes()
        {
            return Helpers.objectToByteArray(this);
        }
    }
}
