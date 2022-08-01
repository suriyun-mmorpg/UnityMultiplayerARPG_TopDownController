using UnityEngine;

namespace MultiplayerARPG
{
    public sealed partial class TopDownPlayerCharacterController : PlayerCharacterController
    {
        private bool cantSetDestination;
        private bool getRMouseDown;
        private bool getRMouseUp;
        private bool getRMouse;

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

            isPointerOverUI = CacheUISceneGameplay != null && CacheUISceneGameplay.IsPointerOverUIObject();
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
            if (!getMouse || getMouseDown)
            {
                TargetEntity = null;
                didActionOnTarget = false;
            }

            tempCount = FindClickObjects(out tempVector3);
            for (int tempCounter = 0; tempCounter < tempCount; ++tempCounter)
            {
                tempTransform = physicFunctions.GetRaycastTransform(tempCounter);
                targetPosition = physicFunctions.GetRaycastPoint(tempCounter);
                /*
                if (targetPlayer != null && !targetPlayer.IsHideOrDead())
                {
                    foundTargetEntity = true;
                    if (!getMouse)
                        SelectedGameEntity = targetPlayer;
                    if (getMouseDown)
                        SetTarget(targetPlayer, TargetActionType.Attack);
                    break;
                }
                else if (targetMonster != null && !targetMonster.IsHideOrDead())
                {
                    foundTargetEntity = true;
                    if (!getMouse)
                        SelectedGameEntity = targetMonster;
                    if (getMouseDown)
                        SetTarget(targetMonster, TargetActionType.Attack);
                    break;
                }
                else if (targetNpc != null)
                {
                    foundTargetEntity = true;
                    if (!getMouse)
                        SelectedGameEntity = targetNpc;
                    if (getMouseDown)
                        SetTarget(targetNpc, TargetActionType.Activate);
                    break;
                }
                else if (targetItemDrop != null)
                {
                    foundTargetEntity = true;
                    if (!getMouse)
                        SelectedGameEntity = targetItemDrop;
                    if (getMouseDown)
                        SetTarget(targetItemDrop, TargetActionType.Activate);
                    break;
                }
                else if (targetHarvestable != null && !targetHarvestable.IsDead())
                {
                    foundTargetEntity = true;
                    if (!getMouse)
                        SelectedGameEntity = targetHarvestable;
                    if (getMouseDown)
                        SetTarget(targetHarvestable, TargetActionType.Attack);
                    break;
                }
                else if (targetVehicle != null)
                {
                    foundTargetEntity = true;
                    if (!getMouse)
                        SelectedGameEntity = targetVehicle;
                    if (getMouseDown)
                    {
                        if (targetVehicle.ShouldBeAttackTarget())
                            SetTarget(targetVehicle, TargetActionType.Attack);
                        else
                            SetTarget(targetVehicle, TargetActionType.Activate);
                    }
                    break;
                }
                else if (targetBuilding != null && !targetBuilding.IsDead())
                {
                    foundTargetEntity = true;
                    if (!getMouse)
                        SelectedGameEntity = targetBuilding;
                    if (getMouseDown && targetBuilding.Activatable)
                        SetTarget(targetBuilding, TargetActionType.Activate);
                    if (getRMouseDown)
                        SetTarget(targetBuilding, TargetActionType.ViewOptions);
                    break;
                }
                */
            }

            if (!foundTargetEntity)
                SelectedEntity = null;

            if (getMouse)
            {
                if (TargetGameEntity != null)
                {
                    // Has target so move to target not the destination
                    cantSetDestination = true;
                }
                else
                {
                    // Close NPC dialog, when target changes
                    HideNpcDialog();
                }

                // Move to target
                if (!cantSetDestination && tempCount > 0)
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
                        PlayerCharacterEntity.SetTargetEntity(null);
                        tempVector3.z = 0;
                        targetPosition = tempVector3;
                    }
                    destination = targetPosition;
                    PlayerCharacterEntity.PointClickMovement(targetPosition.Value);
                }
            }
            else
            {
                // Mouse released, reset states
                if (TargetGameEntity == null)
                    cantSetDestination = false;
            }
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
            PlayerCharacterEntity.SetTargetEntity(entity as BaseGameEntity);
        }
    }
}
