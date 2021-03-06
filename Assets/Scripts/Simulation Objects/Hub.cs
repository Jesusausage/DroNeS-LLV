﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Drones
{
    using Managers;
    using Utils;
    using DataStreamer;
    using UI;
    using Interface;
    using Serializable;
    using Drones.Data;

    public class Hub : MonoBehaviour, IDataSource, IPoolable
    {

        public static Hub New() => PoolController.Get(ObjectPool.Instance).Get<Hub>(null);

        #region IDataSource
        public override string ToString() => Name;

        public bool IsDataStatic => _Data.IsDataStatic;

        public void GetData(ISingleDataSourceReceiver receiver) => receiver.SetData(_Data);

        public AbstractInfoWindow InfoWindow { get; set; }

        public void OpenInfoWindow()
        {
            if (InfoWindow == null)
            {
                InfoWindow = HubWindow.New();
                InfoWindow.Source = this;
            }
            else
            {
                InfoWindow.transform.SetAsLastSibling();
            }
        }
        #endregion

        public uint UID => _Data.UID;

        public string Name => "H" + UID.ToString("000000");

        #region Fields
        private HubData _Data;

        [SerializeField]
        private PathClearer _DronePath;

        private int _rCount;
        #endregion

        #region IPoolable
        public PoolController PC() => PoolController.Get(ObjectPool.Instance);

        public bool InPool { get; private set; }

        public void Delete() => PC().Release(GetType(), this);

        public void OnRelease()
        {
            InPool = true;
            InfoWindow?.Close.onClick.Invoke();
            if (_Data.drones != null)
            {
                _Data.drones.ReSort();
                while (_Data.drones.Count > 0)
                {
                    ((Drone)_Data.drones.GetMin(false)).DestroySelf();
                }
            }
            _Data = null;
            SimManager.AllHubs.Remove(this);
            gameObject.SetActive(false);
            transform.SetParent(PC().PoolParent);
        }

        public void OnGet(Transform parent = null)
        {
            InPool = false;
            _Data = new HubData(this);
            SimManager.AllHubs.Add(UID, this);
            transform.SetParent(parent);
            gameObject.SetActive(true);
            StartCoroutine(DeployDrone());
        }
        #endregion

        public PathClearer DronePath
        {
            get
            {
                if (_DronePath == null)
                {
                    _DronePath = transform.GetComponentInChildren<PathClearer>();
                }
                return _DronePath;
            }
        }

        public void AddToDeploymentQueue(Drone drone) => _Data.deploymentQueue.Enqueue(drone);
        public Vector3 Position => transform.position;
        public void UpdateEnergy(float dE) => _Data.energyConsumption += dE;
        public SecureSortedSet<uint, IDataSource> Drones => _Data.drones;
        void Awake()
        {
            _Data = new HubData();
        }

        private void OnTriggerEnter(Collider other)
        {
            if (other.gameObject.layer == LayerMask.NameToLayer("Hub") ||
                other.gameObject.layer == LayerMask.NameToLayer("NoFlyZone"))
            {
                _rCount++;
                transform.position += new Vector3(Random.Range(-1f, 1f), 0, Random.Range(-1f, 1f));
                StartCoroutine(Repulsion(other));
            }
        }

        private void OnTriggerExit(Collider other)
        {
            if ((other.gameObject.layer == LayerMask.NameToLayer("Hub") ||
                other.gameObject.layer == LayerMask.NameToLayer("NoFlyZone")) && _rCount > 0)
            {
                _rCount--;
            }
        }

        public IEnumerator Repulsion(Collider other)
        {
            StopCoroutine(DeployDrone());
            yield return null;
            Vector3 acc;
            Vector3 vel = Vector3.zero;
            float dt = 0;
            float dist;
            do
            {
                transform.position += vel * dt;
                dist = Vector3.Distance(transform.position, other.transform.position);
                float k = 1e8f / Mathf.Pow(dist, 2);
                if (other.CompareTag("NoFlyZone")) k *= 100;
                k = Mathf.Clamp(k, 0, 1e5f);
                acc = k * (transform.position - other.transform.position).normalized;
                acc.y = 0;
                dt = Time.deltaTime;
                vel = acc * dt;
                yield return null;
            } while (_rCount > 0);
            StartCoroutine(Reposition(vel.normalized));
            yield break;
        }

        public IEnumerator Reposition(Vector3 inDirection)
        {
            StopCoroutine(DeployDrone());
            Vector3.Normalize(inDirection);
            while (!DronePath.IsClear) // Building
            {
                transform.position += 5 * DronePath.Direction + inDirection;
                yield return null;
            }
            StartCoroutine(DeployDrone());
            yield break;
        }

        IEnumerator DeployDrone()
        {
            var time = TimeKeeper.Chronos.Get();
            WaitUntil _DroneReady = new WaitUntil(() => time.Timer() > HubData.deploymentPeriod);
            Drone outgoing = null;
            while (true)
            {
                if (!DronePath.IsClear) yield return null;
                if (_Data.deploymentQueue.Count > 0)
                {
                    outgoing = _Data.deploymentQueue.Dequeue();
                    if (outgoing.InPool) continue;
                    _Data.freeDrones.Remove(outgoing);
                    GetBatteryForDrone(outgoing);
                    StopCharging(outgoing.GetBattery());
                    outgoing.GetBattery().SetStatus(BatteryStatus.Discharge);
                    outgoing.Deploy();
                }
                yield return _DroneReady;
                time.Now();
            }
        }

        #region Drone/Battery Interface
        public void OnDroneReturn(Drone drone)
        {
            if (drone != null && _Data.freeDrones.Add(drone.UID, drone))
            {
                _Data.chargingBatteries.Add(drone.GetBattery().UID, drone.GetBattery());
            }
            drone.WaitForDeployment();
        }

        private void RemoveBatteryFromDrone(Drone drone)
        {
            if (drone.GetHub() == this && _Data.freeBatteries.Add(drone.GetBattery().UID, drone.GetBattery()))
            {
                _Data.chargingBatteries.Add(drone.GetBattery().UID, drone.GetBattery());
                drone.AssignBattery(null);
            }
        }

        public void ReassignDrone(Drone drone, Hub hub)
        {
            _Data.drones.Remove(drone);
            _Data.batteries.Remove(drone.GetBattery());
            drone.AssignHub(hub);
            hub._Data.drones.Add(drone.UID, drone);
            hub._Data.batteries.Add(drone.GetBattery().UID, drone.GetBattery());
            drone.GetBattery().AssignHub(hub);
        }

        public void StopCharging(Battery battery) => _Data.chargingBatteries.Remove(battery);

        private void GetBatteryForDrone(Drone drone)
        {
            if (drone.GetBattery() != null) return;

            if (_Data.drones.Count >= _Data.batteries.Count)
            {
                drone.AssignBattery(BuyBattery(drone));
            }
            else
            {
                drone.AssignBattery(_Data.freeBatteries.GetMax(true));
                drone.GetBattery().AssignDrone(drone);
            }
        }

        public Drone BuyDrone()
        {
            Drone drone = Drone.New();
            drone.transform.position = transform.position;
            GetBatteryForDrone(drone);
            _Data.drones.Add(drone.UID, drone);
            return drone;
        }

        public void SellDrone()
        {
            if (_Data.freeDrones.Count > 0)
            {
                Drone drone = _Data.freeDrones.GetMin(false);
                var dd = new RetiredDrone(drone);
                SimManager.AllRetiredDrones.Add(dd.UID, dd);
                _Data.drones.Remove(drone);
                drone.Delete();
            }
        }

        public void DestroyBattery(Battery battery) => _Data.batteries.Remove(battery);

        public Battery BuyBattery(Drone drone = null)
        {
            var bat = new Battery(drone, this);
            _Data.batteries.Add(bat.UID, bat);

            if (drone is null) _Data.freeBatteries.Add(bat.UID, bat);
            return bat;
        }

        public void SellBattery()
        {
            if (_Data.freeBatteries.Count > 0)
            {
                var bat = _Data.freeBatteries.GetMin(true);
                _Data.batteries.Remove(bat);
            }
        }
        #endregion

        public SHub Serialize() => new SHub(_Data, this);

        public static Hub Load(SHub data, List<SDrone> drones, List<SBattery> batteries)
        {
            Hub hub = PoolController.Get(ObjectPool.Instance).Get<Hub>(null, true);
            hub.transform.position = data.position;
            hub.transform.SetParent(null);
            hub.gameObject.SetActive(true);
            hub.InPool = false;
            SimManager.AllHubs.Add(hub.UID, hub);

            hub._Data = new HubData(data, hub, drones, batteries);
            return hub;
        }

    }
}