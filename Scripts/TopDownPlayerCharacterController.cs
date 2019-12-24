using LiteNetLibManager;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace MultiplayerARPG
{
    public sealed partial class TopDownPlayerCharacterController : PlayerCharacterController
    {
        private bool cantSetDestination;

        protected override void Update()
        {
            pointClickSetTargetImmediately = true;
            controllerMode = PlayerCharacterControllerMode.PointClick;
            base.Update();
        }

        public override void UpdatePointClickInput()
        {
            // If it's building something, not allow point click movement
            if (CurrentBuildingEntity != null)
                return;

            isPointerOverUI = CacheUISceneGameplay != null && CacheUISceneGameplay.IsPointerOverUIObject();
            if (isPointerOverUI)
                return;

            // Temp mouse input value
            getMouseDown = Input.GetMouseButtonDown(0);
            getMouseUp = Input.GetMouseButtonUp(0);
            getMouse = Input.GetMouseButton(0);

            if (getMouseUp)
            {
                // Clear target when player release mouse button
                ClearTarget(true);
                return;
            }

            // Prepare temp variables
            Transform tempTransform;
            Vector3 tempVector3;
            int tempCount;

            // Clear target
            if (!getMouse)
                SelectedEntity = null;

            tempCount = FindClickObjects(out tempVector3);
            for (int tempCounter = 0; tempCounter < tempCount; ++tempCounter)
            {
                tempTransform = GetRaycastTransform(tempCounter);
                targetPlayer = tempTransform.GetComponent<BasePlayerCharacterEntity>();
                targetMonster = tempTransform.GetComponent<BaseMonsterCharacterEntity>();
                targetNpc = tempTransform.GetComponent<NpcEntity>();
                targetItemDrop = tempTransform.GetComponent<ItemDropEntity>();
                targetHarvestable = tempTransform.GetComponent<HarvestableEntity>();
                BuildingMaterial buildingMaterial = tempTransform.GetComponent<BuildingMaterial>();
                targetPosition = GetRaycastPoint(tempCounter);
                PlayerCharacterEntity.SetTargetEntity(null);
                lastNpcObjectId = 0;
                if (targetPlayer != null && !targetPlayer.GetCaches().IsHide)
                {
                    if (!getMouse)
                        SelectedEntity = targetPlayer;
                    if (getMouseDown)
                        SetTarget(targetPlayer, TargetActionType.Attack);
                    break;
                }
                else if (targetMonster != null && !targetMonster.GetCaches().IsHide)
                {
                    if (!getMouse)
                        SelectedEntity = targetMonster;
                    if (getMouseDown)
                        SetTarget(targetMonster, TargetActionType.Attack);
                    break;
                }
                else if (targetNpc != null)
                {
                    if (!getMouse)
                        SelectedEntity = targetNpc;
                    if (getMouseDown)
                        SetTarget(targetNpc, TargetActionType.Undefined);
                    break;
                }
                else if (targetItemDrop != null)
                {
                    if (!getMouse)
                        SelectedEntity = targetItemDrop;
                    if (getMouseDown)
                        SetTarget(targetItemDrop, TargetActionType.Undefined);
                    break;
                }
                else if (targetHarvestable != null && !targetHarvestable.IsDead())
                {
                    if (!getMouse)
                        SelectedEntity = targetHarvestable;
                    if (getMouseDown)
                        SetTarget(targetHarvestable, TargetActionType.Undefined);
                    break;
                }
                else if (buildingMaterial != null && buildingMaterial.buildingEntity != null && !buildingMaterial.buildingEntity.IsDead())
                {
                    if (!getMouse)
                        SelectedEntity = buildingMaterial.buildingEntity;
                    if (getMouseDown)
                        SetTarget(buildingMaterial.buildingEntity, TargetActionType.Undefined);
                    break;
                }
            }

            if (getMouse)
            {
                // Close NPC dialog, when target changes
                if (CacheUISceneGameplay != null && CacheUISceneGameplay.uiNpcDialog != null)
                    CacheUISceneGameplay.uiNpcDialog.Hide();

                if (TargetEntity != null)
                    cantSetDestination = true;

                // Move to target
                if (!cantSetDestination)
                {
                    // When moving, find target position which mouse click on
                    targetPosition = GetRaycastPoint(0);
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
                if (TargetEntity == null)
                    cantSetDestination = false;
            }
        }

        protected override void SetTarget(BaseGameEntity entity, TargetActionType targetActionType, bool checkControllerMode = true)
        {
            this.targetActionType = targetActionType;
            destination = null;
            TargetEntity = entity;
            PlayerCharacterEntity.SetTargetEntity(entity);
        }
    }
}
