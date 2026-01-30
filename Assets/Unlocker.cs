using UnityEngine;

public class Unlocker : MonoBehaviour
{
    [SerializeField] Unlock unlock;

    enum Unlock
    {
        Jump,
        Shoot
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.gameObject.tag == "Bone")
        {
            if (collision.transform.parent == null) return;
            PlayerController player = null;
            if(collision.transform.parent.TryGetComponent<PlayerController>(out player))
            {
                switch(unlock)
                {
                    case Unlock.Shoot:
                        player.shootUnlocked = true;
                        break;
                }

            }

        }
    }
}
