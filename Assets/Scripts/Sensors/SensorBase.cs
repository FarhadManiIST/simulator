/**
 * Copyright (c) 2019 LG Electronics, Inc.
 *
 * This software contains code licensed as described in LICENSE.
 *
 */

using UnityEngine;
using Simulator.Bridge;
using Simulator.Utilities;
using Simulator.Sensors.UI;
using System.Collections;
using Simulator.Analysis;
using System.Collections.Generic;
using System;

namespace Simulator.Sensors
{
    public abstract class SensorBase : MonoBehaviour
    {
        public enum SensorDistributionType
        {
            DoNotDistribute = 0,
            LowLoad = 1,
            HighLoad = 2,
            UltraHighLoad = 3
        }

        public List<AnalysisReportItem> SensorAnalysisData;
        public string Name;

        [SensorParameter]
        public string Topic;
        [SensorParameter]
        public string Frame;

        public virtual SensorDistributionType DistributionType => SensorDistributionType.DoNotDistribute;

        [HideInInspector]
        public Transform ParentTransform;

        public virtual Type GetDataBridgePlugin()
        {
            return null;
        }

        public abstract void OnBridgeSetup(BridgeInstance bridge);
        public abstract void OnVisualize(Visualizer visualizer);
        public abstract void OnVisualizeToggle(bool state);
        public virtual void OnAnalyze() { }
        public virtual void SetAnalysisData() { }
    }
}
