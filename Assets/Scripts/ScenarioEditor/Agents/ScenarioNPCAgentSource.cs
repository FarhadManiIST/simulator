/**
 * Copyright (c) 2020 LG Electronics, Inc.
 *
 * This software contains code licensed as described in LICENSE.
 *
 */

namespace Simulator.ScenarioEditor.Agents
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using System.Threading.Tasks;
    using Elements.Agents;
    using Input;
    using Managers;
    using Undo;
    using Undo.Records;
    using UnityEngine;
    using Utilities;
    using Web;

    /// <inheritdoc/>
    /// <remarks>
    /// This scenario agent source handles NPC agents
    /// </remarks>
    public class ScenarioNPCAgentSource : ScenarioAgentSource
    {
        /// <summary>
        /// Cached reference to the scenario editor input manager
        /// </summary>
        private InputManager inputManager;

        /// <summary>
        /// Currently dragged agent instance
        /// </summary>
        private GameObject draggedInstance;

        /// <inheritdoc/>
        public override string ElementTypeName => "NPCAgent";

        /// <inheritdoc/>
        public override string ParameterType => "";

        /// <inheritdoc/>
        public override int AgentTypeId => 2;

        /// <inheritdoc/>
        public override List<SourceVariant> Variants { get; } = new List<SourceVariant>();

        /// <inheritdoc/>
        public override Task Initialize(IProgress<float> progress)
        {
            inputManager = ScenarioManager.Instance.GetExtension<InputManager>();
            var npcVehiclesInSimulation = Config.NPCVehicles;
            var npcsCount = npcVehiclesInSimulation.Count;
            var i = 0;
            foreach (var npcAssetData in npcVehiclesInSimulation)
            {
                var sb = new StringBuilder();
                Debug.Log($"Loading NPC {npcAssetData.Value.Name} from the config.");
                sb.Append("NPC type: ");
                sb.Append(npcAssetData.Value.NPCType);
                var npcVariant = new AgentVariant(this, npcAssetData.Value.Name, npcAssetData.Value.Prefab,
                    sb.ToString());
                Variants.Add(npcVariant);
                progress.Report((float)++i/npcsCount);
            }

            Behaviours = new List<string>();
            var npcsManager = ScenarioManager.Instance.GetExtension<ScenarioNPCsManager>();
            Behaviours.AddRange(npcsManager.AvailableBehaviourTypes.Select(t => t.Name));
            return Task.CompletedTask;
        }

        /// <inheritdoc/>
        public override void Deinitialize()
        {
        }

        /// <inheritdoc/>
        public override GameObject GetModelInstance(SourceVariant variant)
        {
            var instance = base.GetModelInstance(variant);
            if (instance.GetComponent<BoxCollider>() == null)
            {
                var collider = instance.AddComponent<BoxCollider>();
                collider.isTrigger = true;
                var b = new Bounds(instance.transform.position, Vector3.zero);
                foreach (Renderer r in instance.GetComponentsInChildren<Renderer>())
                    b.Encapsulate(r.bounds);
                collider.center = b.center - instance.transform.position;
                collider.size = b.size;
            }

            if (instance.GetComponent<Rigidbody>() == null)
            {
                var rigidbody = instance.AddComponent<Rigidbody>();
                rigidbody.collisionDetectionMode = CollisionDetectionMode.ContinuousSpeculative;
                rigidbody.isKinematic = true;
            }

            return instance;
        }

        /// <inheritdoc/>
        public override ScenarioAgent GetAgentInstance(AgentVariant variant)
        {
            var newGameObject = new GameObject(ElementTypeName);
            newGameObject.transform.SetParent(transform);
            var scenarioAgent = newGameObject.AddComponent<ScenarioAgent>();
            scenarioAgent.GetOrAddExtension<AgentBehaviour>();
            scenarioAgent.GetOrAddExtension<AgentColorExtension>();
            scenarioAgent.GetOrAddExtension<AgentWaypoints>();
            scenarioAgent.Setup(this, variant);
            return scenarioAgent;
        }

        /// <inheritdoc/>
        public override bool AgentSupportWaypoints(ScenarioAgent agent)
        {
            var behaviourExtension = agent.GetExtension<AgentBehaviour>();
            return behaviourExtension!=null && behaviourExtension.Behaviour == nameof(NPCWaypointBehaviour);
        }

        /// <inheritdoc/>
        public override void DragStarted()
        {
            draggedInstance = GetModelInstance(selectedVariant);
            draggedInstance.transform.SetParent(ScenarioManager.Instance.transform);
            draggedInstance.transform.SetPositionAndRotation(inputManager.MouseRaycastPosition,
                Quaternion.Euler(0.0f, 0.0f, 0.0f));
            ScenarioManager.Instance.GetExtension<ScenarioMapManager>().LaneSnapping.SnapToLane(
                LaneSnappingHandler.LaneType.Traffic,
                draggedInstance.transform,
                draggedInstance.transform);
        }

        /// <inheritdoc/>
        public override void DragMoved()
        {
            draggedInstance.transform.position = inputManager.MouseRaycastPosition;
            ScenarioManager.Instance.GetExtension<ScenarioMapManager>().LaneSnapping.SnapToLane(
                LaneSnappingHandler.LaneType.Traffic,
                draggedInstance.transform,
                draggedInstance.transform);
        }

        /// <inheritdoc/>
        public override void DragFinished()
        {
            var agent = GetAgentInstance(selectedVariant);
            agent.TransformToRotate.rotation = draggedInstance.transform.rotation;
            agent.ForceMove(draggedInstance.transform.position);
            ScenarioManager.Instance.prefabsPools.ReturnInstance(draggedInstance);
            ScenarioManager.Instance.GetExtension<ScenarioUndoManager>().RegisterRecord(new UndoAddElement(agent));
            draggedInstance = null;
        }

        /// <inheritdoc/>
        public override void DragCancelled()
        {
            ScenarioManager.Instance.prefabsPools.ReturnInstance(draggedInstance);
            draggedInstance = null;
        }
    }
}