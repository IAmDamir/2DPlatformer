using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public interface HealthPoints<T>
{
    public int currentHealth { get; }
    void TakeDamage(T damage);
    void Die();
}
