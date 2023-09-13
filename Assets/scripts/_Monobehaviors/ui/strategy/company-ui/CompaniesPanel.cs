using System.Collections.Generic;
using component.strategy.army_components;
using component.strategy.army_components.ui;
using component.strategy.events;
using TMPro;
using Unity.Collections;
using Unity.Entities;
using UnityEngine;

namespace _Monobehaviors.ui
{
    public class CompaniesPanel : MonoBehaviour, DraggableUi
    {
        public static CompaniesPanel instance;
        [SerializeField] private GameObject panel;
        [SerializeField] private GameObject panelImage;
        [SerializeField] private GameObject companyTabPrefab;
        private EntityQuery companyMergeBufferQuery;

        //id - company, soldierCount, tabLink
        private List<(ArmyCompany, int, GameObject)> companyTabs = new();
        private EntityQuery createNewArmyEventQuery;
        private bool dragging;

        private EntityManager entityManager;
        private bool fixNeeded;
        private bool lockUiPanel;

        private void Awake()
        {
            entityManager = World.DefaultGameObjectInjectionWorld.EntityManager;
            companyMergeBufferQuery = entityManager.CreateEntityQuery(typeof(CompanyMergeBuffer));
            createNewArmyEventQuery = entityManager.CreateEntityQuery(typeof(CreateNewArmyEvent));
            instance = this;
        }

        public void updateDragging(bool newDraggingstate)
        {
            dragging = newDraggingstate;
        }

        public void finishDrag(long companyId, Vector3 targetPosition)
        {
            fixNeeded = true;

            if (targetPosition.y > 130 && companyTabs.Count > 1)
            {
                createNewArmy(companyId);
                lockUi(companyId);
                return;
            }

            var targetSlot = getSlotFromPosition(targetPosition);
            if (targetSlot > companyTabs.Count - 1 || targetSlot < 0)
            {
                lockUi(companyId);
                return;
            }

            var targetObject = companyTabs[targetSlot];
            if (targetObject.Item1.id == companyId)
            {
                lockUi(companyId);
                return;
            }

            var mergeBuffer = companyMergeBufferQuery.GetSingletonBuffer<CompanyMergeBuffer>();
            mergeBuffer.Add(new CompanyMergeBuffer
            {
                companyId1 = companyId,
                companyId2 = targetObject.Item1.id
            });
            lockUi(companyId);
        }

        private void createNewArmy(long companyId)
        {
            var newArmyEvenBuffer = createNewArmyEventQuery.GetSingletonBuffer<CreateNewArmyEvent>();
            var companies = new NativeList<long>(Allocator.TempJob);
            companies.Add(companyId);
            newArmyEvenBuffer.Add(new CreateNewArmyEvent
            {
                companiesToDeploy = companies
            });
        }

        public void changeActive(bool targetState)
        {
            if (!targetState)
            {
                destroyAll();
            }

            panel.SetActive(targetState);
        }

        private int getSlotFromPosition(Vector3 position)
        {
            if (position.y < 20 - 60 || position.y > 130 - 60 || position.x < 35 - 50)
            {
                return -1;
            }

            return (int) (position.x - 35 + 50) / 105;
        }

        private Vector3 getPositionFromSlot(int slot)
        {
            var result = new Vector3();
            result.x = 35 + slot * 105;
            result.y = 20;
            return result;
        }

        public void displayCompanies(NativeArray<ArmyCompany> newCompanies)
        {
            if (dragging || isLocked())
            {
                return;
            }

            if (!hasChanged(newCompanies))
            {
                return;
            }

            //todo prekontrolovat pocty vojaku, popripade udelat update

            destroyAll();
            createNewPanels(newCompanies);
        }

        private bool hasChanged(NativeArray<ArmyCompany> newCompanies)
        {
            if (fixNeeded)
            {
                return true;
            }

            if (companyTabs.Count != newCompanies.Length)
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

                index++;
            }

            return false;
        }

        private void createNewPanels(NativeArray<ArmyCompany> toCreate)
        {
            var index = 0;
            foreach (var company in toCreate)
            {
                var newPanel = Instantiate(companyTabPrefab, panel.transform);
                var position = getPositionFromSlot(index);
                newPanel.transform.position = position;
                newPanel.GetComponentInChildren<TextMeshProUGUI>().text = company.soldierCount.ToString();
                companyTabs.Add((company, company.soldierCount, newPanel));
                newPanel.GetComponent<CompaniesDragging>().setCompanyId(company.id);
                newPanel.GetComponent<CompaniesDragging>().setUi(instance);
                index++;
            }

            panelImage.transform.SetAsLastSibling();
        }

        private void destroyAll()
        {
            foreach (var (_, _, gameObjectReference) in companyTabs)
            {
                Destroy(gameObjectReference);
            }

            companyTabs.Clear();
            fixNeeded = false;
        }

        private bool isLocked()
        {
            if (lockUiPanel == false)
            {
                return false;
            }

            var mergeBuffer = companyMergeBufferQuery.GetSingletonBuffer<CompanyMergeBuffer>();
            var newArmyEvenBuffer = createNewArmyEventQuery.GetSingletonBuffer<CreateNewArmyEvent>();
            var hasPendingEvents = mergeBuffer.Length != 0 || newArmyEvenBuffer.Length != 0;
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
        }
    }
}