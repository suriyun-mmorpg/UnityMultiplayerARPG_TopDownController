using LiteNetLibManager;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace MultiplayerARPG
{
    public sealed partial class TopDownPlayerCharacterController : PlayerCharacterController
    {
        protected override void Update()
        {
            pointClickSetTargetImmediately = true;
            SelectedEntity = PlayerCharacterEntity.GetTargetEntity();
            base.Update();
        }

        protected override void UpdatePointClickInput()
        {
            // If it's building something, not allow point click movement
            if (CurrentBuildingEntity != null)
                return;

            if (controllerMode != PlayerCharacterControllerMode.PointClick &&
                controllerMode != PlayerCharacterControllerMode.Both)
                return;

            getMouseDown = Input.GetMouseButtonDown(0);
            getMouseUp = Input.GetMouseButtonUp(0);
            getMouse = Input.GetMouseButton(0);

            isPointerOverUI = CacheUISceneGameplay != null && CacheUISceneGameplay.IsPointerOverUIObject();
            if (isPointerOverUI)
                return;

            if (getMouseUp)
            {
                // Clear target when player release mouse button
                targetEntity = null;
                return;
            }

            if (getMouseDown)
            {
                targetEntity = null;
                int tempCount = FindClickObjects(out tempVector3);
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
                    if (targetPlayer != null && !targetPlayer.IsDead())
                    {
                        SetTarget(targetPlayer);
                        break;
                    }
                    else if (targetMonster != null && !targetMonster.IsDead())
                    {
                        SetTarget(targetMonster);
                        break;
                    }
                    else if (targetNpc != null)
                    {
                        SetTarget(targetNpc);
                        break;
                    }
                    else if (targetItemDrop != null)
                    {
                        SetTarget(targetItemDrop);
                        break;
                    }
                    else if (targetHarvestable != null && !targetHarvestable.IsDead())
                    {
                        SetTarget(targetHarvestable);
                        break;
                    }
                    else if (buildingMaterial != null && buildingMaterial.buildingEntity != null && !buildingMaterial.buildingEntity.IsDead())
                    {
                        SetTarget(buildingMaterial.buildingEntity);
                        break;
                    }
                }
            }

            if (getMouse)
            {
                // Close NPC dialog, when target changes
                if (CacheUISceneGameplay != null && CacheUISceneGameplay.uiNpcDialog != null)
                    CacheUISceneGameplay.uiNpcDialog.Hide();

                // Move to target
                if (targetEntity != null)
                {
                    // Hide destination when target is object (not map ground)
                    destination = null;
                    PlayerCharacterEntity.SetTargetEntity(targetEntity);
                }
                else
                {
                    // When moving, find target position which mouse click on
                    int tempCount = FindClickObjects(out tempVector3);
                    if (tempCount > 0)
                    {
                        targetPosition = GetRaycastPoint(0);
                        // When clicked on map (any non-collider position)
                        // tempVector3 is come from FindClickObjects()
                        // - Clear character target to make character stop doing actions
                        // - Clear selected target to hide selected entity UIs
                        // - Set target position to position where mouse clicked
                        if (gameInstance.DimensionType == DimensionType.Dimension2D)
                        {
                            PlayerCharacterEntity.SetTargetEntity(null);
                            tempVector3.z = 0;
                            targetPosition = tempVector3;
                        }
                    }
                    destination = targetPosition;
                    PlayerCharacterEntity.PointClickMovement(targetPosition.Value);
                }
            }
        }

        protected override void SetTarget(BaseGameEntity entity)
        {
            targetPosition = entity.CacheTransform.position;
            targetEntity = entity;
            PlayerCharacterEntity.SetTargetEntity(entity);
            SelectedEntity = entity;
        }

        private void ClearTargetEntityAfterAttack()
        {
            if (targetEntity == null)
            {
                // If target entity is not null it's means player still hold on mouse button
                // because target entity will be cleared when mouse up event fire
                queueUsingSkill = null;
                PlayerCharacterEntity.SetTargetEntity(null);
                PlayerCharacterEntity.StopMove();
            }
        }
    }
}
