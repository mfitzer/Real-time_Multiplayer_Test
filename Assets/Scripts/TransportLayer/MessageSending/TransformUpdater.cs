using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using MessageSending;

namespace MessageSending
{
    public class TransformUpdater : MonoBehaviour
    {
        public ServerBehaviour server;

        [Tooltip("Max number of times the transform will refresh in one second.")]
        public int maxRefreshRate = 30;

        //Number of times the transform has refreshed in the current second
        int refreshesThisSecond = 0;

        //Timer that goes from 0 to 1, tracking each second as it goes by
        float secondTimer = 0f;

        Vector3 previousPosition = Vector3.zero;
        Vector3 previousRotation = Vector3.zero;
        Vector3 previousScale = Vector3.one;

        // Start is called before the first frame update
        void Start()
        {
            refreshTransformData();
        }

        //Updates secondTimer to track each second as it goes by
        void trackSecondsElapsed()
        {
            secondTimer += Time.deltaTime;
            if (secondTimer > 1)
            {
                secondTimer = 0;
                refreshesThisSecond = 0;
            }
        }

        //Check if transform data has changed since the last frame
        void refreshTransformData()
        {
            if (previousPosition != transform.position)
            {
                previousPosition = transform.position;
                updateNetwork();
            }
            else if (previousRotation != transform.eulerAngles)
            {
                previousRotation = transform.eulerAngles;
                updateNetwork();
            }
            else if (previousScale != transform.localScale)
            {
                previousScale = transform.localScale;
                updateNetwork();
            }
        }

        //Updates the network with the new transform data
        void updateNetwork()
        {
            if (refreshesThisSecond < maxRefreshRate)
            {
                //TransformMessage transformMsg = MessageBuilder.createTransformMessage(transform);
                TransformMessage transformMsg = new TransformMessage(transform);
                server.sendMessage(transformMsg);

                refreshesThisSecond++;
            }
        }

        // Update is called once per frame
        void Update()
        {
            trackSecondsElapsed();
            refreshTransformData();
        }
    }
}
