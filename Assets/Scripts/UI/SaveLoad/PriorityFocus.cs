﻿

namespace Drones.UI
{
    using Drones.Managers;
    using Drones.Utils;

    public class PriorityFocus : UIFocus
    {
        public static uint Count;
        private int _index;
        private void OnEnable()
        {
            if (SimManager.SimStatus != SimulationStatus.Paused && SimManager.SimStatus != SimulationStatus.EditMode)
            {
                SimManager.SimStatus = SimulationStatus.Paused;
            }
            _index = (int)Count++;
        }

        private void LateUpdate()
        {
            Controlling = true;
            Window.transform.SetAsLastSibling();
            Window.transform.SetSiblingIndex(Window.transform.GetSiblingIndex() - (int)Count + _index + 1);
        }

        protected override void OnDisable()
        {
            base.OnDisable();
            Count--;
        }

    }

}
