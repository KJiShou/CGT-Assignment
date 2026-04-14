using UnityEngine;
using System.Collections.Generic;

public class EmotionEmojiVFX : MonoBehaviour
{
    public ParticleSystem ps;
    public List<Material> emojiMaterials;

    private ParticleSystemRenderer psRenderer;

    void Awake()
    {
        psRenderer = ps.GetComponent<ParticleSystemRenderer>();
    }

    public void PlayEmoji(int index)
    {
        if (index < 0 || index >= emojiMaterials.Count) return;

        psRenderer.material = emojiMaterials[index];

        ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        ps.Play();
    }

    public void StopEmoji()
    {
        ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
    }
}