using Cinemachine;
using FishNet.Managing;
using FishNet.Object;
using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

public class NetworkPlayerInitializer : NetworkBehaviour
{
    [SerializeField] List<GameObject> disableObjects;

    public override void OnStartClient()
    {
        base.OnStartClient();
        if (base.Owner.IsLocalClient == false)
        {
            //GetComponentInChildren<Camera>().gameObject.SetActive(false);
            //GetComponentInChildren<EventSystem>().gameObject.SetActive(false);
            //GetComponent<CinemachineVirtualCamera>().gameObject.SetActive(false);

            foreach (GameObject obj in disableObjects)
            {
                obj.SetActive(false);
            }
            GetComponent<PlayerMainScript>().enabled = false;
            GetComponent<CharacterController>().enabled = false;
            GetComponent<PlayerInput>().enabled = false;
            this.enabled = false;
        }
    }
}
