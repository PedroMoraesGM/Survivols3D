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
        }

        private void UpdateCharacterView(Frame frame)
        {
            var character = frame.Unsafe.GetPointer<Character>(EntityRef);
            bool isDead = character->IsDead;

            DeadFirstPersonView.SetActive(isPlayerLocal && isDead);
            DeadThirdPersonView.SetActive(!isPlayerLocal && isDead);

            firstPersonView.gameObject.SetActive(isPlayerLocal && !isDead);
            thirdPersonView.gameObject.SetActive(!isPlayerLocal && !isDead);
            headCanvas.gameObject.SetActive(!isPlayerLocal && !isDead);

            healthBar.transform.localScale = new Vector3((character->CurrentHealth / character->MaxHealth).AsFloat, 1);
        }

        public override void OnUpdateView()
        {
            var frame = Game.Frames.Predicted;
            if (!frame.Exists(EntityRef)) return;

            UpdateCharacterView(frame);

            if (!isPlayerLocal) return;

            //Convert fixed-point look delta back to float
            var character = frame.Unsafe.GetPointer<Character>(EntityRef);

            cameraPivot.localRotation = Quaternion.Euler(character->VerticalLookPitch.AsFloat, 0, 0);
        }
    }
}