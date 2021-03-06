﻿namespace Drones.UI
{
    using Data;
    using Utils.Extensions;
    using Interface;
    using Utils;

    public class HubTuple : ObjectTuple
    {
        public override void SetData(IData data)
        {
            var hub = (HubData)data;
            Data[0].SetField(((Hub)Source).Name);
            Data[1].SetField(hub.drones.Count.ToString());
            Data[2].SetField(hub.batteries.Count.ToString());
            Data[3].SetField(hub.Position.ToStringXZ());

        }

        public static HubTuple New(IListWindow window)
        {
            var pc = PoolController.Get(ListElementPool.Instance);
            var le = pc.Get<HubTuple>(window.TupleContainer.transform);
            le._Window = (AbstractWindow)window;
            return le;
        }
    }
}
