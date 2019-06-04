using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace MessageSending
{
    [System.Serializable]
    public class TransformMessage : Message
    {
        string transformName = "";

        //Position vector
        float positionX = 0f;
        float positionY = 0f;
        float positionZ = 0f;

        //Rotation vector
        float rotationX = 0f;
        float rotationY = 0f;
        float rotationZ = 0f;

        //Scale vector
        float scaleX = 0f;
        float scaleY = 0f;
        float scaleZ = 0f;

        public TransformMessage(Transform transform)
        {
            transformName = transform.name;

            positionX = transform.position.x;
            positionY = transform.position.y;
            positionZ = transform.position.z;

            rotationX = transform.eulerAngles.x;
            rotationY = transform.eulerAngles.y;
            rotationZ = transform.eulerAngles.z;

            scaleX = transform.localScale.x;
            scaleY = transform.localScale.y;
            scaleZ = transform.localScale.z;
        }

        //Update transform values of a transform with the name, transformName
        public override bool process()
        {
            GameObject obj = GameObject.Find(transformName);

            if (obj) //obj found
            {
                //Update obj transform values
                Transform transform = obj.transform;
                transform.position = new Vector3(positionX, positionY, positionZ);
                transform.rotation = Quaternion.Euler(new Vector3(rotationX, rotationY, rotationZ));
                transform.localScale = new Vector3(scaleX, scaleY, scaleZ);

                return true;
            }

            return false;
        }
    }
}

