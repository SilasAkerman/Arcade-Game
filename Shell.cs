using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Shell : MonoBehaviour
{
    public Rigidbody myRigidbody;
    public float forceMin;
    public float forceMax;

    float lifeTime = 4;
    float fadeTime = 2;

    private void Start()
    {
        float force = Random.Range(forceMin, forceMax);
        myRigidbody.AddForce(transform.right * force); // Assumes the shell goes to the right
        myRigidbody.AddTorque(Random.insideUnitSphere * force);

        StartCoroutine(Fade());
    }

    IEnumerator Fade()
    {
        yield return new WaitForSeconds(lifeTime);

        float fadePercent = 0;
        float fadeSpeed = 1 / fadeTime;
        Material mat = GetComponent<Renderer>().material;
        Color initialColor = mat.color;

        while (fadePercent < 1)
        {
            fadePercent += Time.deltaTime * fadeSpeed;
            mat.color = Color.Lerp(initialColor, Color.clear, fadePercent);
            yield return null;
        }

        Destroy(gameObject);
    }
}
