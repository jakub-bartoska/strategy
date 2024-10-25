using System;
using UnityEngine;

namespace _Monobehaviors.camera
{
    public class CameraManager : MonoBehaviour
    {
        public static CameraManager instance;
        [SerializeField] private GameObject strategyCamera;
        [SerializeField] private GameObject preBattleCamera;
        [SerializeField] private GameObject battleCamera;

        public void Awake()
        {
            instance = this;
        }

        public void SwitchCamera(GameCameraType gameCameraType)
        {
            strategyCamera.SetActive(gameCameraType == GameCameraType.STRATEGY);
            preBattleCamera.SetActive(gameCameraType == GameCameraType.PRE_BATTLE);
            battleCamera.SetActive(gameCameraType == GameCameraType.BATTLE);
        }

        public Camera getCamera(GameCameraType type)
        {
            var cameraObject = type switch
            {
                GameCameraType.STRATEGY => strategyCamera,
                GameCameraType.PRE_BATTLE => preBattleCamera,
                GameCameraType.BATTLE => battleCamera,
                _ => throw new Exception("Invalid camera type")
            };
            return cameraObject.GetComponent<Camera>();
        }
    }

    public enum GameCameraType
    {
        STRATEGY,
        PRE_BATTLE,
        BATTLE
    }
}