using UnityEngine;

namespace MultiplayerARPG
{
    public sealed partial class TopDownPlayerCharacterController : PlayerCharacterController
    {
        private bool cannotSetDestination;
        private bool getRMouseDown;
        private bool getRMouseUp;
        private bool getRMouse;
        private bool lastFrameIsAiming;

        protected override void Update()
        {
            pointClickSetTargetImmediately = true;
            controllerMode = PlayerCharacterControllerMode.PointClick;
            isFollowingTarget = true;
            base.Update();
        }

        public override void UpdatePointClickInput()
        {
            // If it's building something, not allow point click movement
            if (ConstructingBuildingEntity != null)
                return;

            isPointerOverUI = UISceneGameplay != null && UISceneGameplay.IsPointerOverUIObject();
            if (isPointerOverUI)
                return;

            // Temp mouse input value
            getMouseDown = Input.GetMouseButtonDown(0);
            getMouseUp = Input.GetMouseButtonUp(0);
            getMouse = Input.GetMouseButton(0);
            getRMouseDown = Input.GetMouseButtonDown(1);
            getRMouseUp = Input.GetMouseButtonUp(1);
            getRMouse = Input.GetMouseButton(1);

            // Prepare temp variables
            bool foundTargetEntity = false;
            Transform tempTransform;
            Vector3 tempVector3;
            int tempCount;

            // Clear target
            if (getMouseDown)
                didActionOnTarget = false;

            tempCount = FindClickObjects(out tempVector3);
            for (int tempCounter = 0; tempCounter < tempCount; ++tempCounter)
            {
                tempTransform = physicFunctions.GetRaycastTransform(tempCounter);
                targetPosition = physicFunctions.GetRaycastPoint(tempCounter);
                ITargetableEntity targetable = tempTransform.GetComponent<ITargetableEntity>();
                IActivatableEntity clickActivatable = targetable as IActivatableEntity;
                IHoldActivatableEntity rightClickActivatable = targetable as IHoldActivatableEntity;
                IDamageableEntity damageable = targetable as IDamageableEntity;
                if (!targetable.IsNull())
                {
                    if (!getMouse)
                    {
                        if (damageable.IsNull() || !damageable.IsHideOrDead())
                        {
                            // Mouse cursor hover on entity
                            foundTargetEntity = true;
                            if (!TargetEntity.IsNull())
                                SelectedEntity = TargetEntity;
                            else
                                SelectedEntity = targetable;
                        }
                    }
                    if (getMouseDown)
                    {
                        if (!clickActivatable.IsNull() && clickActivatable.CanActivate())
                        {
                            // Clicked on entity
                            foundTargetEntity = true;
                            if (clickActivatable.ShouldBeAttackTarget())
                                SetTarget(clickActivatable, TargetActionType.Attack);
                            else
                                SetTarget(clickActivatable, TargetActionType.ClickActivate);
                        }
                        else if (!damageable.IsNull() && !damageable.IsHideOrDead() && damageable.CanReceiveDamageFrom(PlayingCharacterEntity.GetInfo()))
                        {
                            // Clicked on entity
                            foundTargetEntity = true;
                            SetTarget(damageable, TargetActionType.Attack);
                        }
                    }
                    if (getRMouseDown)
                    {
                        if (!rightClickActivatable.IsNull() && rightClickActivatable.CanHoldActivate())
                        {
                            // Right-clicked on entity
                            foundTargetEntity = true;
                            SetTarget(rightClickActivatable, TargetActionType.HoldClickActivate);
                        }
                    }
                }
                if (foundTargetEntity)
                    break;
            }

            if (getMouseUp && TargetEntity == null)
            {
                // Mouse release while cursor hover on ground
                SelectedEntity = null;
            }

            if (!getMouse && !foundTargetEntity)
            {
                // Mouse cursor not hover on entity
                SelectedEntity = null;
            }


            if (getMouse)
            {
                if (TargetGameEntity != null)
                {
                    // Has target so move to target not the destination
                    cannotSetDestination = true;
                }
                else
                {
                    // Close NPC dialog, when target changes
                    HideNpcDialog();
                }

                if (lastFrameIsAiming)
                    cannotSetDestination = true;

                // Move to target
                if (!cannotSetDestination && tempCount > 0)
                {
                    // When moving, find target position which mouse click on
                    targetPosition = physicFunctions.GetRaycastPoint(0);
                    // When clicked on map (any non-collider position)
                    // tempVector3 is come from FindClickObjects()
                    // - Clear character target to make character stop doing actions
                    // - Clear selected target to hide selected entity UIs
                    // - Set target position to position where mouse clicked
                    if (CurrentGameInstance.DimensionType == DimensionType.Dimension2D)
                    {
                        PlayingCharacterEntity.SetTargetEntity(null);
                        tempVector3.z = 0;
                        targetPosition = tempVector3;
                    }
                    destination = targetPosition;
                    PlayingCharacterEntity.PointClickMovement(targetPosition.Value);
                }
            }
            else
            {
                // Mouse released, reset states
                if (TargetGameEntity == null)
                    cannotSetDestination = false;
            }

            lastFrameIsAiming = AreaSkillAimController.IsAiming;
        }

        protected override void OnDoActionOnEntity()
        {
            if (!getMouse && !getRMouse)
            {
                // Clear target when player release mouse button
                ClearTarget(true);
            }
        }

        protected override void OnAttackOnEntity()
        {
            if (!getMouse && !getRMouse)
            {
                // Clear target when player release mouse button
                ClearTarget(true);
            }
        }

        protected override void OnUseSkillOnEntity()
        {
            if (!getMouse && !getRMouse)
            {
                // Clear target when player release mouse button
                ClearTarget(true);
            }
        }

        protected override void SetTarget(ITargetableEntity entity, TargetActionType targetActionType, bool checkControllerMode = true)
        {
            this.targetActionType = targetActionType;
            destination = null;
            TargetEntity = entity;
            if (entity is IGameEntity)
                PlayingCharacterEntity.SetTargetEntity((entity as IGameEntity).Entity);
        }
    }
}
