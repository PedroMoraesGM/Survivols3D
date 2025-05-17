using UnityEngine;
using UnityEngine.Scripting;
using Photon;
using Photon.Deterministic;
using Quantum;
using Quantum.Collections;

using Input = Quantum.Input;
using static UnityEngine.EventSystems.EventTrigger;

namespace Tomorrow.Quantum
{
    [Preserve]
    public unsafe class MoveSystem : SystemMainThreadFilter<MoveSystem.Filter>
    {
        public struct Filter
        {
            public EntityRef Entity;
            public Transform3D* Transform;
            public PhysicsBody3D* Body;
            public Character* Character;
            public CharacterController3D* Controller;
            public PlayerLink* PlayerLink;
        }

        public override void Update(Frame f, ref Filter filter)
        {
            // 1) Pull in the player input
            Input input = default;
            if (f.Unsafe.TryGetPointer(filter.Entity, out PlayerLink* link))
            {
                input = *f.GetPlayerInput(link->Player);
            }

            filter.Character->LookDelta = input.LookDelta; // Saves LookDelta value to be used on View

            FP lookX = input.LookDelta.X;
            if (lookX != FP._0)
            {
                FP yawDeg = lookX * filter.Character->HorizontalTurnSpeedDegrees * f.DeltaTime;
                // Convert degrees to radians for FPQuaternion.Euler
                FP yawRad = yawDeg * FP.Deg2Rad;
                // Build a quaternion rotating around the Y axis
                FPQuaternion yawRot = FPQuaternion.Euler(FP._0, yawRad, FP._0);
                // Apply it: newRot = yawRot * oldRot
                filter.Transform->Rotation = yawRot * filter.Transform->Rotation;
            }

            FP lookY = input.LookDelta.Y;
            if (lookY != FP._0)
            {
                var lookDelta = filter.Character->LookDelta;
                var vPitch = filter.Character->VerticalLookPitch - lookDelta.Y * filter.Character->VerticalTurnSpeedDegrees;

                if (vPitch < filter.Character->MinVerticalLook)                
                    vPitch = filter.Character->MinVerticalLook;                
                else if (vPitch > filter.Character->MaxVerticalLook)
                    vPitch = filter.Character->MaxVerticalLook;

                filter.Character->VerticalLookPitch = vPitch;
            }

            FPVector2 moveAxis = input.MoveAxis;  // X = strafe, Y = forward/back

            FPVector3 dir =
              filter.Transform->Forward * moveAxis.Y +
              filter.Transform->Right * moveAxis.X;

            if (dir == FPVector3.Zero)
            {
                return;
            }

            // 3) Normalize and scale
            dir = dir.Normalized;

            // 4) Predict next position
            FPVector3 currentPos = filter.Transform->Position;  // current world-space pos
            FPVector3 predictedPos = (currentPos + dir);           // next position if unclamped

            // 5) Clamp Y against MinHeightLimit
            //    (uses Mathf.Clamp concept for floats :contentReference[oaicite:0]{index=0},
            //     and applies the same logic to FPVector3.Y :contentReference[oaicite:1]{index=1})
            FP minY = filter.Character->MinHeightLimit;
            if (predictedPos.Y != minY)
            {
                predictedPos.Y = minY;
            }

            // 6) Compute the actual move vector and issue the move
            FPVector3 clampedDir = predictedPos - currentPos;
            //filter.Controller->Move(f, filter.Entity, clampedDir);
            filter.Body->AddForceAtPosition(clampedDir * filter.Character->MoveSpeed, filter.Transform->Position, filter.Transform);
            //filter.Body->Velocity = clampedDir * filter.Character->MoveSpeed;
            //filter.Body->ClearForce();
            //filter.Body->AddForce(clampedDir * filter.Character->MoveSpeed);
        }
    }
}

