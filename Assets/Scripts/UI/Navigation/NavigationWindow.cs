﻿using System.Collections.Generic;
using Drones.Utils;
using Drones.Utils.Extensions;
using UnityEngine;

namespace Drones.UI
{
    public class NavigationWindow : AbstractWindow
    {
        public override WindowType Type { get; } = WindowType.Navigation;
        protected override Vector2 MinimizedSize { get; } = new Vector2(315, 85);
        protected override Vector2 MaximizedSize { get; } = new Vector2(315, 480);

        [SerializeField]
        private GameObject _GPS;


        public GameObject GPS
        {
            get
            {
                if (_GPS == null)
                {
                    _GPS = ContentPanel.transform.Find("GPS").gameObject;
                }
                return _GPS;
            }
        }

        protected override void Awake()
        {
            SetName("Navigation");
            MaximizeWindow();

            DisableOnMinimize = new List<GameObject>
            {
                ContentPanel.transform.Find("Minimap").gameObject,
                ContentPanel.transform.Find("Altimeter").gameObject,
                ContentPanel.transform.Find("Compass").gameObject,
            };

            base.Awake();
        }
    }

}