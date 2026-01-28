using System.Collections.Generic;
using UnityEngine;

public class PlayerBloquer : MonoBehaviour
{
    [SerializeField] PlayerController player = null;
    Collider2D collider = null;
    
    void Start()
    {
        collider = GetComponent<Collider2D>();

        if (player == null || collider == null) return;

        var playerControlers = FindObjectsByType<PlayerController>(FindObjectsSortMode.None);
        foreach (PlayerController player in playerControlers)
        {
            if (player == this.player)
            {
                foreach(PlayerController.Bone bone in player.bones)
                {
                    Physics2D.IgnoreCollision(bone.collider2D, collider, true);
                }
            }
        }
    }
}
