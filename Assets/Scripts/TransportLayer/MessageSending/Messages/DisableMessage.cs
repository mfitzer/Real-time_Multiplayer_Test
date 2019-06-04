using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace MessageSending
{
    [System.Serializable]
    public class DisableMessage : Message
    {
        public string objToEnable = "";

        //Disable a GameObject with the name objToEnable
        public override bool process()
        {
            GameObject obj = GameObject.Find(objToEnable);

            if (obj) //obj found
            {
                obj.SetActive(false);
                return true;
            }

            return false;
        }
    }
}
