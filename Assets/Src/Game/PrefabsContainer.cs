using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Data contaienr with available prefabs to be instantiated by object pool
/// </summary>
public class PrefabsContainer : MonoBehaviour
{
    // create prefab
    public Creature Creature;

    // bullet prefab
    public Bullet Bullet;

    // particle system prefab
    public ParticleEmitter ParticleSystem;
}
