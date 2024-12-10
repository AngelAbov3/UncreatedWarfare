﻿using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;
using Uncreated.Warfare.Configuration;

namespace Uncreated.Warfare.Components;

internal class LaserGuidedMissileComponent : MonoBehaviour
{
#nullable disable
    private Player _firer;
    private GameObject _projectile;
    private Transform _aim;
    private Rigidbody _rigidbody;
    private List<BoxCollider> _colliders;
#nullable restore

    private SpottedComponent? _laserTarget;
    private float _guiderDistance;
    private float _aquisitionRange;
    private float _projectileSpeed;
    private float _maxTurnDegrees;
    private float _armingDistance;
    private float _fullGuidanceDelay;
    private float _turnMultiplier;

    private bool _armed;
    private bool _isActive;

    private IAssetLink<EffectAsset> _fxSilent = null!;
    private IAssetLink<EffectAsset> _fxSound = null!;

    public float InitializationTime { get; private set; }
    public bool LockedOn => _laserTarget != null;

    public void Initialize(GameObject projectile, Player firer, IServiceProvider serviceProvider, float projectileSpeed, float responsiveness, float aquisitionRange, float armingDistance, float fullGuidanceDelay)
    {
        _projectile = projectile;
        _firer = firer;
        _maxTurnDegrees = responsiveness;
        _projectileSpeed = projectileSpeed;
        _aquisitionRange = aquisitionRange;
        _armingDistance = armingDistance;
        _fullGuidanceDelay = fullGuidanceDelay;
        _guiderDistance = 30;
        _turnMultiplier = 0;

        AssetConfiguration assetConfig = serviceProvider.GetRequiredService<AssetConfiguration>();
        _fxSilent = assetConfig.GetAssetLink<EffectAsset>("Effects:Projectiles:GuidedMissileSilent");
        _fxSound = assetConfig.GetAssetLink<EffectAsset>("Effects:Projectiles:GuidedMissileSound");

        _count = 0;

        _armed = false;

        InitializationTime = Time.realtimeSinceStartup;

        _isActive = false;

        _laserTarget = null;

        if (!projectile.TryGetComponent(out _rigidbody))
        {
            return;
        }

        InteractableVehicle? vehicle = firer.movement.getVehicle();
        if (vehicle != null)
        {
            foreach (Passenger turret in vehicle.turrets)
            {
                if (turret.player == null || turret.player.player != firer)
                    continue;

                _aim = turret.turretAim;
                _isActive = true;

                projectile.transform.position = vehicle.transform.TransformPoint(new Vector3(-4, 0, -4));
                projectile.transform.forward =
                    Quaternion.AngleAxis(20, vehicle.transform.right) * vehicle.transform.forward;

                _rigidbody.velocity = projectile.transform.forward * projectileSpeed;

                _colliders = projectile.GetComponents<BoxCollider>().ToList();
                _colliders.ForEach(c => c.enabled = false);

                return;
            }
        }
        else
        {
            _aim = firer.look.aim.transform;
            _isActive = true;
            projectile.transform.forward = _aim.forward;
            _rigidbody.velocity = projectile.transform.forward * projectileSpeed;
            _colliders = projectile.GetComponents<BoxCollider>().ToList();
            _colliders.ForEach(c => c.enabled = false);
        }
    }

    private bool TryAcquireTarget()
    {
        if (_laserTarget != null)
        {
            if (_laserTarget.IsActive)
                return true;
            _laserTarget = null;
        }

        float minAngle = 45;

        if (_firer is null)
            return false;

        foreach (SpottedComponent spotted in SpottedComponent.ActiveMarkers)
        {
            if (spotted.SpottingTeam == _firer.quests.groupID.m_SteamID && spotted.IsLaserTarget)
            {
                if ((spotted.transform.position - _projectile.transform.position).sqrMagnitude < _aquisitionRange * _aquisitionRange)
                {
                    float angleBetween = Vector3.Angle(spotted.transform.position - _projectile.transform.position, _projectile.transform.forward);
                    if (angleBetween < minAngle)
                    {
                        minAngle = angleBetween;
                        _laserTarget = spotted;
                    }
                }
            }
        }

        return _laserTarget != null;
    }

    private int _count;
    private float _lastSent;

    [UsedImplicitly]
    private void FixedUpdate()
    {
        if (!_isActive)
            return;
        
        if (_aim == null)
        {
            _isActive = false;
            return;
        }

        _guiderDistance += Time.fixedDeltaTime * _projectileSpeed;

        _turnMultiplier = Mathf.Clamp(_turnMultiplier + Time.fixedDeltaTime / _fullGuidanceDelay, 0, 1);

        if (_guiderDistance > 30 + _armingDistance && !_armed)
        {
            _colliders.ForEach(c => c.enabled = true);
            _armed = true;
        }

        Vector3 target = _aim.TransformPoint(new Vector3(0, 0, _guiderDistance));

        if (TryAcquireTarget())
        {
            target = _laserTarget!.transform.position;
        }

        Vector3 idealDirection = target - _projectile.transform.position;

        float maxAngle = Mathf.Deg2Rad * _maxTurnDegrees * _turnMultiplier;

        Vector3 targetDirection = Vector3.RotateTowards(transform.forward, idealDirection, maxAngle, maxAngle);

        _projectile.transform.forward = targetDirection;
        _rigidbody.velocity = _projectile.transform.forward * _projectileSpeed;

        if (Time.time - _lastSent > 0.05f)
        {
            IAssetLink<EffectAsset> id = _fxSilent;
            if (_count % 10 == 0 && _armed || !id.TryGetAsset(out _))
            {
                id = _fxSound;
            }

            if (id.TryGetAsset(out EffectAsset? effect))
            {
                TriggerEffectParameters parameters = new TriggerEffectParameters(effect)
                {
                    relevantDistance = 1200f,
                    position = _projectile.transform.position,
                    reliable = false
                };

                parameters.SetDirection(_projectile.transform.forward);
                EffectManager.triggerEffect(parameters);
            }
            
            _lastSent = Time.time;
        }

        ++_count;
    }
}
