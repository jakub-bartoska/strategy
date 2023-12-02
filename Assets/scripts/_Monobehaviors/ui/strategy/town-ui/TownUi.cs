using System;
using System.Collections.Generic;
using component.strategy.army_components;
using component.strategy.army_components.ui;
using TMPro;
using Unity.Collections;
using Unity.Entities;
using UnityEngine;

namespace _Monobehaviors.ui
{
    public class TownUi : MonoBehaviour, DraggableUi
    {
        public static TownUi instance;

        [SerializeField] private GameObject townUi;
        [SerializeField] private GameObject companyTabPrefab;
        private EntityQuery companyMergeBufferQuery;

        //id - company, soldierCount, tabLink, state
        private List<(ArmyCompany, int, GameObject)> companyTabs = new();
        private List<(ArmyCompany, int, GameObject)> companyTabsToDeploy = new();
        private EntityQuery companyToDifferentStateQuery;
        private bool dragging;

        private EntityManager entityManager;
        private bool fixNeeded;
        private bool lockUiPanel;
        private float maxX;
        private float maxY;

        private void Awake()
        {
            var canvas = GetComponent<Canvas>();
            maxX = canvas.pixelRect.width;
            maxY = canvas.pixelRect.height;
            instance = this;
            entityManager = World.DefaultGameObjectInjectionWorld.EntityManager;
            companyMergeBufferQuery = entityManager.CreateEntityQuery(typeof(CompanyMergeBuffer));
            companyToDifferentStateQuery = entityManager.CreateEntityQuery(typeof(CompanyToDifferentState));
        }

        public void updateDragging(bool newDraggingstate)
        {
            dragging = newDraggingstate;
        }

        public void finishDrag(long companyId, Vector3 targetPosition)
        {
            fixNeeded = true;

            var targetSlot = getSlotFromPosition(targetPosition);

            if (canMerge(targetSlot, companyId))
            {
                mergeSlots(targetSlot, companyId);
                lockUi(companyId);
                return;
            }

            moveBetweenStatesIfNeeded(targetSlot.Item2, companyId);
            lockUi(companyId);
        }

        public void changeActive(bool targetState)
        {
            if (!targetState)
            {
                destroyAll();
            }

            townUi.SetActive(targetState);
        }

        public void displayTown(NativeArray<ArmyCompany> newCompanies, NativeArray<ArmyCompany> newCompaniesToDeploy)
        {
            if (dragging || isLocked())
            {
                return;
            }

            if (!hasChanged(newCompanies, newCompaniesToDeploy))
            {
                return;
            }

            //todo prekontrolovat pocty vojaku, popripade udelat update
            destroyAll();
            createNewPanels(newCompanies, newCompaniesToDeploy);
        }

        private bool hasChanged(NativeArray<ArmyCompany> newCompanies, NativeArray<ArmyCompany> newCompaniesToDeploy)
        {
            if (fixNeeded)
            {
                return true;
            }

            if (companyTabs.Count != newCompanies.Length || companyTabsToDeploy.Count != newCompaniesToDeploy.Length)
            {
                return true;
            }

            var index = 0;
            foreach (var company in newCompanies)
            {
                if (companyTabs[index].Item1.id != company.id)
                {
                    return true;
                }

                if (companyTabs[index].Item1.soldierCount != company.soldierCount)
                {
                    return true;
                }

                index++;
            }

            index = 0;
            foreach (var company in newCompaniesToDeploy)
            {
                if (companyTabsToDeploy[index].Item1.id != company.id)
                {
                    return true;
                }

                if (companyTabsToDeploy[index].Item1.soldierCount != company.soldierCount)
                {
                    return true;
                }

                index++;
            }

            return false;
        }

        private bool canMerge((int, CompanyCardState) targetSlot, long companyId)
        {
            var targetList = targetSlot.Item2 switch
            {
                CompanyCardState.NORMAL => companyTabs,
                CompanyCardState.DEPLOY => companyTabsToDeploy,
                _ => throw new Exception("unknown type")
            };
            if (targetSlot.Item1 > targetList.Count - 1 || targetSlot.Item1 < 0)
            {
                return false;
            }

            var targetObject = targetList[targetSlot.Item1];
            if (targetObject.Item1.id == companyId)
            {
                return false;
            }

            return true;
        }

        private void mergeSlots((int, CompanyCardState) targetSlot, long companyId)
        {
            var targetObject = targetSlot.Item2 switch
            {
                CompanyCardState.NORMAL => companyTabs[targetSlot.Item1],
                CompanyCardState.DEPLOY => companyTabsToDeploy[targetSlot.Item1],
                _ => throw new Exception("unknown type")
            };
            var mergeBuffer = companyMergeBufferQuery.GetSingletonBuffer<CompanyMergeBuffer>();
            mergeBuffer.Add(new CompanyMergeBuffer
            {
                companyId1 = companyId,
                companyId2 = targetObject.Item1.id
            });
        }

        private void moveBetweenStatesIfNeeded(CompanyCardState newState, long companyId)
        {
            var companyState = getCompanyCardState(companyId);
            if (companyState == newState)
            {
                return;
            }

            var buffer = companyToDifferentStateQuery.GetSingletonBuffer<CompanyToDifferentState>();

            switch (newState)
            {
                case CompanyCardState.DEPLOY:
                    buffer.Add(new CompanyToDifferentState
                    {
                        companyId = companyId,
                        targetState = CompanyState.TOWN_TO_DEPLOY
                    });
                    break;
                case CompanyCardState.NORMAL:
                    buffer.Add(new CompanyToDifferentState
                    {
                        companyId = companyId,
                        targetState = CompanyState.TOWN
                    });
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(newState), newState, null);
            }
        }

        private CompanyCardState getCompanyCardState(long companyId)
        {
            foreach (var (army, _, _) in companyTabs)
            {
                if (army.id == companyId)
                {
                    return CompanyCardState.NORMAL;
                }
            }

            foreach (var (army, _, _) in companyTabsToDeploy)
            {
                if (army.id == companyId)
                {
                    return CompanyCardState.DEPLOY;
                }
            }

            throw new Exception("unknown company state");
        }

        private (int, CompanyCardState) getSlotFromPosition(Vector3 position)
        {
            if (position.y < (maxY * 0.3f) - 15 - 120 - 60)
            {
                return (-1, CompanyCardState.DEPLOY);
            }

            if (position.y < maxY * 0.3f - 60)
            {
                var deploySlot = (int) (position.x - 15 + 50) / 105;
                return (deploySlot, CompanyCardState.DEPLOY);
            }

            if (position.y < maxY - 15 - 120 - 60 || position.y > maxY - 20 - 60 || position.x < 15 - 50)
            {
                return (-1, CompanyCardState.NORMAL);
            }

            var slot = (int) (position.x - 15 + 50) / 105;
            return (slot, CompanyCardState.NORMAL);
        }

        private Vector3 getPositionFromSlot(int slot, CompanyCardState state)
        {
            var result = new Vector3();
            result.x = 15 + slot * 105;

            if (state == CompanyCardState.NORMAL)
            {
                result.y = maxY - 15 - 120;
            }
            else
            {
                result.y = (maxY * 0.3f) - 15 - 120;
            }

            return result;
        }

        private void createNewPanels(NativeArray<ArmyCompany> toCreate, NativeArray<ArmyCompany> toCreateToDeploy)
        {
            var index = 0;
            foreach (var company in toCreate)
            {
                var newPanel = Instantiate(companyTabPrefab, townUi.transform);
                var position = getPositionFromSlot(index, CompanyCardState.NORMAL);
                newPanel.transform.position = position;
                newPanel.GetComponentInChildren<TextMeshProUGUI>().text = company.soldierCount.ToString();
                newPanel.GetComponentInChildren<CompanyIconPicker>().setIcon(company.type);
                companyTabs.Add((company, company.soldierCount, newPanel));
                newPanel.GetComponent<CompaniesDragging>().setCompanyId(company.id);
                newPanel.GetComponent<CompaniesDragging>().setUi(instance);
                index++;
            }

            var indexToDeploy = 0;
            foreach (var company in toCreateToDeploy)
            {
                var newPanel = Instantiate(companyTabPrefab, townUi.transform);
                var position = getPositionFromSlot(indexToDeploy, CompanyCardState.DEPLOY);
                newPanel.transform.position = position;
                newPanel.GetComponentInChildren<TextMeshProUGUI>().text = company.soldierCount.ToString();
                newPanel.GetComponentInChildren<CompanyIconPicker>().setIcon(company.type);
                companyTabsToDeploy.Add((company, company.soldierCount, newPanel));
                newPanel.GetComponent<CompaniesDragging>().setCompanyId(company.id);
                newPanel.GetComponent<CompaniesDragging>().setUi(instance);
                indexToDeploy++;
            }
        }

        private void destroyAll()
        {
            foreach (var (_, _, gameObjectReference) in companyTabs)
            {
                Destroy(gameObjectReference);
            }

            foreach (var (_, _, gameObjectReference) in companyTabsToDeploy)
            {
                Destroy(gameObjectReference);
            }

            companyTabs.Clear();
            companyTabsToDeploy.Clear();

            fixNeeded = false;
        }

        public NativeList<long> getCompaniesReadyToDeploy()
        {
            var companyIdList = new NativeList<long>(Allocator.Persistent);
            foreach (var (company, _, _) in companyTabsToDeploy)
            {
                companyIdList.Add(company.id);
            }

            return companyIdList;
        }

        private bool isLocked()
        {
            if (lockUiPanel == false)
            {
                return false;
            }

            var mergeBuffer = companyMergeBufferQuery.GetSingletonBuffer<CompanyMergeBuffer>();
            var companyToDifferentState = companyToDifferentStateQuery.GetSingletonBuffer<CompanyToDifferentState>();
            var hasPendingEvents = mergeBuffer.Length != 0 || companyToDifferentState.Length != 0;
            if (!hasPendingEvents)
            {
                lockUiPanel = false;
            }

            return hasPendingEvents;
        }

        private void lockUi(long companyId)
        {
            lockUiPanel = true;
            companyTabs.RemoveAll(item =>
            {
                if (item.Item1.id == companyId)
                {
                    Destroy(item.Item3);
                    return true;
                }

                return false;
            });
            companyTabsToDeploy.RemoveAll(item =>
            {
                if (item.Item1.id == companyId)
                {
                    Destroy(item.Item3);
                    return true;
                }

                return false;
            });
        }

        private enum CompanyCardState
        {
            DEPLOY,
            NORMAL
        }
    }
}