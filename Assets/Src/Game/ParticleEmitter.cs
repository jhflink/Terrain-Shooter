using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ParticleEmitter : MonoBehaviour, IPoolableObject
{
    // A dictionary to store a particle systems settings by reflection
    // can then be used to apply the setting of one particle system to another
    public static Dictionary<string, (System.Reflection.PropertyInfo[] propertyList, ParticleSystem system)> PrefabSettings = new Dictionary<string, (System.Reflection.PropertyInfo[], ParticleSystem)>();

    /// <summary>
    /// Copy settings from one particle system to another using reflection (use with care)
    /// </summary>
    /// <param name="properties"></param>
    /// <param name="fromSystem"></param>
    /// <param name="toSystem"></param>
    public static void CopyParticleSettings(System.Reflection.PropertyInfo[] properties, ParticleSystem fromSystem, ParticleSystem toSystem)
    {
        foreach (System.Reflection.PropertyInfo property in properties)
        {
            if (property.CanWrite)
                property.SetValue(toSystem, property.GetValue(fromSystem));
        }
    }

    /// <summary>
    /// Instantiate a new emitter
    /// </summary>
    /// <returns></returns>
    public static IPoolableObject InstantiateEmitter()
    {
        return Instantiate<ParticleEmitter>(GameManager.Instance.Prefabs.ParticleSystem);
    }

    /// <summary>
    /// Is object available for pooling
    /// </summary>
    /// <returns></returns>
    public bool IsAvailable() => !gameObject.activeSelf;

    /// <summary>
    /// Called when getting ready to bee pooled again
    /// </summary>
    public void OnResetCallback() { }

    /// <summary>
    /// Called when a spawn point tries to spawn the obejct
    /// </summary>
    public void OnSpawnCallback(Vector3 position) { }

    // actual particle system
    public ParticleSystem System;

    // fire particles at position
    public void Fire(Vector3 position)
    {
        if (System == null)
            System = GetComponent<ParticleSystem>();

        System.transform.position = position;
        System.Play();
    }

    // check if firing is done or not
    public bool IsDone()
    {
        return !System.isPlaying;
    }
}
