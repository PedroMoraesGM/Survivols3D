using Quantum;
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
        [SerializeField] private Transform cameraPivot;
        [SerializeField] private GameObject thirdPersonView;

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