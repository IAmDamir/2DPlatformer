using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DeathTrigger : MonoBehaviour
{
    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (!collision.Equals(null))
        {
            if (collision.GetComponent<HeroKnight>())
                collision.GetComponent<HeroKnight>().Die();
            else if (collision.GetComponent<Bandit>())
                collision.GetComponent<Bandit>().Die();
        }
    }
}
