using DG.Tweening;
using Quantum;
using System;
using TMPro;
using UnityEngine;

namespace QuantumUser.View.Controllers
{
    public unsafe class AreaWeaponViewController : QuantumEntityViewComponent
    {
        [SerializeField] private ParticleSystem[] areaParticleEffects;
        [SerializeField] private Renderer areaMeshModel;

        // You can tweak this factor to control how emission scales with radius
        [SerializeField] private float emissionRatePerUnitRadius = 10f;

        public override void OnActivate(Frame frame)
        {
            // Get the collider component (assuming PhysicsCollider3D is used)
            var collider = frame.Unsafe.GetPointer<PhysicsCollider3D>(EntityRef);
            if (collider == null) return;

            float radius = collider->Shape.Capsule.Radius.AsFloat; // Adjust if using a different shape

            // Resize mesh model
            if (areaMeshModel != null)
            {
                // Assuming the mesh is a unit sphere, scale uniformly
                areaMeshModel.transform.localScale = Vector3.one * radius * 2f;
            }

            // Resize particle effects and update emission
            foreach (var ps in areaParticleEffects)
            {
                if (ps == null) continue;

                // Update shape radius
                var shape = ps.shape;
                shape.radius = radius;

                // Update emission rate proportionally to radius
                var emission = ps.emission;
                emission.rateOverTime = emissionRatePerUnitRadius * radius;
            }
        }

        public override void OnUpdateView()
        {
            // Optionally, update the radius and emission dynamically if it can change during gameplay
        }
    }
}