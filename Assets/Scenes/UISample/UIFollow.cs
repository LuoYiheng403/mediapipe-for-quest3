using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace UIManager
{
    public class UIFollow : MonoBehaviour
    {
        [SerializeField] private GameObject followObj;
        

        // Update is called once per frame
        void Update()
        {
            if (followObj != null)
            {
                this.transform.LookAt(new Vector3(followObj.transform.position.x, this.transform.position.y, followObj.transform.position.z));
                this.transform.Rotate(0, 180f, 0);

            }


        }
    }
}