using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Egghead : MonoBehaviour
{

    private Animator anim;
    private int idleLoops;
    private bool lookingUp;
    // Start is called before the first frame update
    void Start()
    {
        anim = GetComponent<Animator>();

        lookingUp = true;                               //olhar pra cima
        idleLoops = Random.RandomRange(2, 4);           //calcular qtd de loops
        anim.SetInteger("loop_count", 0
            );               //zerar o contador;

    }

    private void Update()
    {
        EggmanRandomIdle();
    }
    void EggmanRandomIdle() //troca a animação quando fizer os loops pre estabelecidos
    {
       
        if (idleLoops == anim.GetInteger("loop_count")) //fez todos os loops que devia?
        {
            idleLoops = Random.RandomRange(2, 4); //calcular nova qtd
            anim.SetInteger("loop_count", 0); //resetar o contador
            lookingUp = !lookingUp; //inverter o bool determinante da animaçao
            anim.SetBool("looking_up", lookingUp); //setar animaçao
        }


    }

    public void CountOneLoop() // acontece smp ao final do idle
    {
        anim.SetInteger("loop_count", anim.GetInteger("loop_count")+1);
    }


}
