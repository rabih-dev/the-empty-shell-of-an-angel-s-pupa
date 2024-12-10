using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Enemy : MonoBehaviour
{
    [SerializeField] private int maxHealth;
    private int currentHealth;
    private SpriteRenderer sp;
    // Start is called before the first frame update
    void Start()
    {
        currentHealth = maxHealth;
        sp = GetComponent<SpriteRenderer>();   
    }

    public void TakeDmg(int dmg)
    {
        StartCoroutine(hurtAnim());
        currentHealth -= dmg;
        if (currentHealth <= 0)
        {
            this.gameObject.SetActive(false);
        }


    }

    IEnumerator hurtAnim()
    {
        sp.color = Color.red;
        yield return new WaitForSeconds(0.5f);
        sp.color = Color.white;
    }
}
