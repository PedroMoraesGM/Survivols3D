using DG.Tweening;
using Quantum;
using System;
using TMPro;
using UnityEngine;


namespace QuantumUser.View.Controllers
{
    public unsafe class EnemyViewController : QuantumEntityViewComponent
    {
        [SerializeField] GameObject hitDamageTextEffect;
        [SerializeField] GameObject destroyEffect;
        [SerializeField] GameObject hitEffect;
        [SerializeField] SpriteRenderer enemyRenderer;
        [SerializeField] float hitBlinkDuration = 1f;

        private void OnEnable()
        {
            QuantumEvent.Subscribe(this, (EventOnDefeated sig) => OnEnemyDefeated(sig));
            QuantumEvent.Subscribe(this, (EventOnHit sig) => OnEnemyHit(sig));
        }

        private void OnEnemyDefeated(EventOnDefeated sig)
        {
            if (sig.Target != EntityRef) return;

            // Instantiate destroy effect
            if(destroyEffect) Instantiate(destroyEffect, transform.position, Quaternion.identity);
        }

        private void OnEnemyHit(EventOnHit sig)
        {
            if (sig.Target != EntityRef) return;

            if(hitEffect) Instantiate(hitEffect, transform.position, Quaternion.identity);
            if (hitDamageTextEffect) Instantiate(hitDamageTextEffect, transform.position, Quaternion.identity).GetComponentInChildren<TextMeshProUGUI>().text = sig.Damage.ToString(); 

            // Reset color 
            enemyRenderer.DOKill();
            var col = Color.red;
            enemyRenderer.color = col;

            // Fade alpha to 0 and back to 1 over hitBlinkDuration
            // half the duration to fade out, half to fade back in,
            // with 2 loops in Yoyo mode = out>in.
            enemyRenderer
                .DOColor(Color.white, hitBlinkDuration)
                .SetLoops(2, LoopType.Yoyo)
                .SetEase(Ease.Linear)
                .SetLink(gameObject);
        }

        public override void OnActivate(Frame frame)
        {

        }

        public override void OnUpdateView()
        {

        }
    }
}