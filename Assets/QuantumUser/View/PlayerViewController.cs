using Quantum;
using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace QuantumUser.View.Controllers
{
    public unsafe class PlayerViewController : QuantumEntityViewComponent
    {
        [HideInInspector] public bool isPlayerLocal;

        public PlayerRef PlayerRef { get; private set; }


        [SerializeField] private GameObject firstPersonView;
        [SerializeField] private GameObject DeadFirstPersonView;
        [SerializeField] private Transform cameraPivot;
        [SerializeField] private GameObject thirdPersonView;
        [SerializeField] private GameObject DeadThirdPersonView;

        float yaw;
        float pitch;
        const float speedYaw = 1.5f;
        [SerializeField] float speedPitch = 1.5f;

        [Header("Head UI - NonLocal")]
        [SerializeField] private Canvas headCanvas;
        [SerializeField] private TextMeshProUGUI nameText;
        [SerializeField] private Image healthBar;

        private void Awake()
        {
            QuantumEvent.Subscribe(this, (EventOnPlayerDefeated e) => OnPlayerDefeated(e));
            QuantumEvent.Subscribe(this, (EventOnPlayerHit e) => OnPlayerHit(e));
        }

        private void OnPlayerHit(EventOnPlayerHit e)
        {
            var f = e.Game.Frames.Verified;
            if (!f.TryGet(e.Target, out PlayerLink playerLink)) return;
            if (e.Game.PlayerIsLocal(playerLink.Player)) return;

            f.TryGet(e.Target, out Character character);
            healthBar.transform.localScale = new Vector3((character.CurrentHealth / character.MaxHealth).AsFloat, 1);
        }

        private void OnPlayerDefeated(EventOnPlayerDefeated e)
        {
            var f = e.Game.Frames.Verified;
            if (!f.TryGet(e.Target, out PlayerLink playerLink)) return;

            if (e.Game.PlayerIsLocal(playerLink.Player))
            {
                firstPersonView.SetActive(false);
                DeadFirstPersonView.SetActive(true);
            }
            else
            {
                thirdPersonView.SetActive(false);
                DeadThirdPersonView.SetActive(true); 
            }
            
        }

        public override void OnActivate(Frame frame)
        {
            var index = 0;
            if (frame.TryGet(EntityRef, out PlayerLink playerLink))
            {
                isPlayerLocal = Game.PlayerIsLocal(playerLink.Player);
                PlayerRef = playerLink.Player;

                RuntimePlayer data = frame.GetPlayerData(playerLink.Player);
                nameText.text = data.PlayerNickname;
            }

            firstPersonView.gameObject.SetActive(isPlayerLocal);
            thirdPersonView.gameObject.SetActive(!isPlayerLocal);
            headCanvas.gameObject.SetActive(!isPlayerLocal);
        }

        public override void OnUpdateView()
        {
            if (!isPlayerLocal) return;

            var frame = Game.Frames.Predicted;

            if (!frame.Exists(EntityRef)) return;

            //Convert fixed-point look delta back to float
            var character = frame.Unsafe.GetPointer<Character>(EntityRef);

            cameraPivot.localRotation = Quaternion.Euler(character->VerticalLookPitch.AsFloat, 0, 0);

            //Debug.Log($"[PlayerViewController] Camera pivot rotated lookdelta:{lookDelta} pitch:{pitch} rot:{cameraPivot.localRotation}");
        }
    }
}