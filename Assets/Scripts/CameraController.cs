using UnityEngine;
using DG.Tweening;
using UnityEngine.Rendering.PostProcessing;
using System;

public interface ICameraEffect
{
    float Weight { get; set; }
    Vector2 CameraPosition { get; }
    float CameraRotation { get; }
    float CameraSizeScale { get; }
}

public enum CameraEffect
{
    None,
    Drunk,
    Intoxicated,
    Mushrooms
}

public class CameraController : MonoBehaviour
{
    private class DummyCameraEffect : ICameraEffect
    {
        public float Weight { get; set; } = 0f;
        public Vector2 CameraPosition { get; } = Vector2.zero;
        public float CameraRotation { get;} = 0f;
        public float CameraSizeScale { get; } = 1f;
    }

    public Camera mainCamera;

    // Only as reference for position
    public Camera introCamera;
    public Camera gameplayCamera;
    public float transitionTime = 2.5f;

    public float cameraEffectTransitionTime = 1f;

    private bool _isInGameplay = false;

    public PostProcessVolume volume;


    private ICameraEffect _currentCameraEffect;

    private ICameraEffect _previousCameraEffect;

    private DummyCameraEffect _dummyCameraEffect = new DummyCameraEffect();
    
    [SerializeField]
    public CameraEffectDrunk _cameraEffectDrunk;

    [SerializeField]
    public CameraEffectIntoxicated _cameraEffectIntoxicated;

    [SerializeField]
    public CameraEffectMushrooms _cameraEffectMushrooms;

    private Transform _cameraContainer;

    public void Start()
    {
        this._currentCameraEffect = this._dummyCameraEffect;
        this._previousCameraEffect = this._dummyCameraEffect;

        this._cameraContainer = this.mainCamera.transform.parent;

        this._dummyCameraEffect.Weight = 1f;
        this._cameraEffectDrunk.Weight = 0f;
        this._cameraEffectIntoxicated.Weight = 0f;
        this._cameraEffectMushrooms.Weight = 0f;

        this.TeleportToIntro();

        this.introCamera.gameObject.SetActive(false);
        this.gameplayCamera.gameObject.SetActive(false);
    }

    public void TeleportToIntro()
    {
        this.mainCamera.transform.position = this.introCamera.transform.position;
        this.mainCamera.orthographicSize = this.introCamera.orthographicSize;
        this._isInGameplay = false;
    }

    public void GoToIntro()
    {
        var targetPos = this.introCamera.transform.position;
        var targetSize = this.introCamera.orthographicSize;
        this.mainCamera.transform.DOMove(targetPos, this.transitionTime).SetEase(Ease.InOutSine);
        this.mainCamera.DOOrthoSize(targetSize, this.transitionTime).SetEase(Ease.InOutSine)
            .OnComplete(() => this._isInGameplay = false);
        
    }

    public void GoToGameplay()
    {
        var targetPos = this.gameplayCamera.transform.position;
        var targetSize = this.gameplayCamera.orthographicSize;
        Debug.Log(this.mainCamera.orthographicSize);
        this.mainCamera.transform.DOMove(targetPos, this.transitionTime).SetEase(Ease.InOutSine);
        this.mainCamera.DOOrthoSize(targetSize, this.transitionTime).SetEase(Ease.InOutSine)
            .OnComplete(() => this._isInGameplay = true);
    }

    
    private CameraEffect _cameraEffect = CameraEffect.None;

    public CameraEffect CameraEffect
    {
        get => this._cameraEffect;
        set
        {
            if (this._cameraEffect == value) return;

            this._cameraEffect = value;

            this._previousCameraEffect = this._currentCameraEffect;
            this._currentCameraEffect = value switch
            {
                CameraEffect.None => this._dummyCameraEffect,
                CameraEffect.Drunk => this._cameraEffectDrunk,
                CameraEffect.Intoxicated => this._cameraEffectIntoxicated,
                CameraEffect.Mushrooms => this._cameraEffectMushrooms,
                _ => throw new NotImplementedException()
            };

            Debug.Log($"Switching to {this._currentCameraEffect.GetType().Name}");

            DOTween.To(
                () => this._previousCameraEffect.Weight,
                (x) => this._previousCameraEffect.Weight = x,
                0,
                this.cameraEffectTransitionTime
            ).SetEase(Ease.InOutSine);
            
            DOTween.To(
                () => this._currentCameraEffect.Weight,
                (x) => this._currentCameraEffect.Weight = x,
                1,
                this.cameraEffectTransitionTime
            ).SetEase(Ease.InOutSine);
        }
    }

    public void Update()
    {
        if (Input.GetKeyDown(KeyCode.Q))
        {
            this.CameraEffect = CameraEffect.None;
        }
        else if (Input.GetKeyDown(KeyCode.W))
        {
            this.CameraEffect = CameraEffect.Drunk;
        }
        else if (Input.GetKeyDown(KeyCode.E))
        {
            this.CameraEffect = CameraEffect.Intoxicated;
        }
        else if (Input.GetKeyDown(KeyCode.R))
        {
            this.CameraEffect = CameraEffect.Mushrooms;
        }

        if (this._isInGameplay)
        {
            var currentPos = this._currentCameraEffect.CameraPosition;
            var previousPos = this._previousCameraEffect.CameraPosition;
            var pos = Vector2.Lerp(previousPos, currentPos, this._currentCameraEffect.Weight);

            var currentRot = this._currentCameraEffect.CameraRotation;
            var previousRot = this._previousCameraEffect.CameraRotation;
            var rot = Mathf.Lerp(previousRot, currentRot, this._currentCameraEffect.Weight);

            this._cameraContainer.localPosition = new Vector3(pos.x, pos.y, this._cameraContainer.localPosition.z);
            this._cameraContainer.localRotation = Quaternion.Euler(0, 0, rot);

            var camBaseSize = this.gameplayCamera.orthographicSize;
            this.mainCamera.orthographicSize = camBaseSize * this._currentCameraEffect.CameraSizeScale;
        }
    }
}
