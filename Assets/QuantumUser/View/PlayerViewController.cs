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

        [SerializeField] private SpriteRenderer[] paintablePlayerRenderers;
        [SerializeField] private AnimatorOverrideController[] firstPersonClassAnimators;
        [SerializeField] private Animator firstPersonAnimator;
        [SerializeField] private GameObject firstPersonView;
        [SerializeField] private GameObject DeadFirstPersonView;
        [SerializeField] private Transform cameraPivot;
        [SerializeField] private AnimatorOverrideController[] thirdPersonClassAnimators;
        [SerializeField] private Animator thirdPersonAnimator;
        [SerializeField] private GameObject thirdPersonView;
        [SerializeField] private GameObject DeadThirdPersonView;
        [SerializeField] private float bobFrequency = 7.5f;
        [SerializeField] private float bobAmplitude = 0.05f;

        float yaw;
        float pitch;
        const float speedYaw = 1.5f;
        [SerializeField] float speedPitch = 1.5f;

        [Header("Head UI - NonLocal")]
        [SerializeField] private Canvas headCanvas;
        [SerializeField] private TextMeshProUGUI nameText;
        [SerializeField] private Image healthBar;

        private float bobTimer = 0f;
        private Vector3 cameraPivotDefaultLocalPos;

        public override void OnActivate(Frame frame)
        {
            var index = 0;
            if (frame.TryGet(EntityRef, out PlayerLink playerLink))
            {
                isPlayerLocal = Game.PlayerIsLocal(playerLink.Player);
                PlayerRef = playerLink.Player;

                RuntimePlayer data = frame.GetPlayerData(playerLink.Player);
                nameText.text = data.PlayerNickname;

                UpdateCharacterVisuals(playerLink);
            }    

            cameraPivotDefaultLocalPos = cameraPivot.localPosition;
        }

        private void UpdateCharacterVisuals(PlayerLink playerLink)
        {
            firstPersonAnimator.runtimeAnimatorController = firstPersonClassAnimators[((int)playerLink.Class)];
            thirdPersonAnimator.runtimeAnimatorController = thirdPersonClassAnimators[((int)playerLink.Class)];

            foreach (var item in paintablePlayerRenderers)
            {
                item.color = Game.Configurations.Runtime.ClassColors[(int)playerLink.Class];
            }
        }

        private void UpdateCharacterView(Frame frame)
        {
            var health = frame.Unsafe.GetPointer<HealthComponent>(EntityRef);
            bool isDead = health->IsDead;

            DeadFirstPersonView.SetActive(isPlayerLocal && isDead);
            DeadThirdPersonView.SetActive(!isPlayerLocal && isDead);

            firstPersonView.gameObject.SetActive(isPlayerLocal && !isDead);
            thirdPersonView.gameObject.SetActive(!isPlayerLocal && !isDead);
            headCanvas.gameObject.SetActive(!isPlayerLocal && !isDead);

            healthBar.transform.localScale = new Vector3((health->CurrentHealth / health->MaxHealth).AsFloat, 1);
        }

        public override void OnUpdateView()
        {
            var frame = Game.Frames.Predicted;
            if (!frame.Exists(EntityRef)) return;

            UpdateCharacterView(frame);

            if (!isPlayerLocal) return;

            var character = frame.Unsafe.GetPointer<Character>(EntityRef);

            // Head bobbing logic
            bool isMoving = false;
            if (frame.TryGet(EntityRef, out PhysicsBody3D move))
            {
                // You can use a threshold to avoid bobbing on tiny movements
                isMoving = move.Velocity.Magnitude.AsFloat > 0.1f;
            }

            if (isMoving)
            {
                bobTimer += Time.deltaTime * bobFrequency;
                float bobOffset = Mathf.Sin(bobTimer) * bobAmplitude;
                cameraPivot.localPosition = cameraPivotDefaultLocalPos + new Vector3(0, bobOffset, 0);
            }
            else
            {
                bobTimer = 0f;
                cameraPivot.localPosition = cameraPivotDefaultLocalPos;
            }

            // Existing camera pitch logic
            cameraPivot.localRotation = Quaternion.Euler(character->VerticalLookPitch.AsFloat, 0, 0);
        }
    }
}