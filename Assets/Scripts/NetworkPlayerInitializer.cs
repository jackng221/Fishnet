using FishNet.Managing;
using FishNet.Object;
using SUPERCharacter;
using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

public class NetworkPlayerInitializer : NetworkBehaviour
{
    public override void OnStartClient()
    {
        base.OnStartClient();
        if (!base.Owner.IsLocalClient)
        {
            GetComponent<SUPERCharacterAIO>().enabled = false;
            GetComponentInChildren<Camera>().gameObject.SetActive(false);
            this.enabled = false;
        }
    }
}
