using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Video;
public class StreamVideo : MonoBehaviour
{
    public RawImage raw;
    public VideoPlayer vp;
    public AudioSource audioSource;

    // Start is called before the first frame update
    void Start()
    {
        StartCoroutine(PlayVideo());
    }

    IEnumerator PlayVideo()
    {
        vp.Prepare();
        WaitForSeconds wait = new WaitForSeconds(1);
        while (!vp.isPrepared)
        {
            yield return wait;
            break;
        }
        raw.texture = vp.texture;
        vp.Play();
    }
}
