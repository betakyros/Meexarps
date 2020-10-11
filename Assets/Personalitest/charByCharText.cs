using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class charByCharText : MonoBehaviour
{

    public float letterPause = 0.05f;
    public AudioClip typeSound1;
    public AudioClip typeSound2;

    string message;
    TMPro.TextMeshProUGUI textComp;

    public IEnumerator<WaitForSeconds> WriteText(string s)
    {
        textComp = GetComponent<TMPro.TextMeshProUGUI>();
        message = s;
        StartCoroutine(TypeText());
        yield return new WaitForSeconds(((float)message.Length) * letterPause);
    }

    IEnumerator<WaitForSeconds> TypeText()
    {
        foreach (char letter in message.ToCharArray())
        {
            textComp.text += letter;
            //if (typeSound1 && typeSound2)
            //    SoundManager.instance.RandomizeSfx(typeSound1, typeSound2);
            yield return new WaitForSeconds(letterPause);
        }
    }
}
