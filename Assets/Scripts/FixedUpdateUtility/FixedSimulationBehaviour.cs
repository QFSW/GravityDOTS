using System;
using Unity.Entities;
using UnityEngine;

namespace QFSW.GravityDOTS
{
	public class FixedSimulationBehaviour : MonoBehaviour
	{
		private void Awake()
		{
			World.Active.GetExistingSystem<FixedSimulationSystemGroup>().Enabled = false;
		}

		private void FixedUpdate()
		{
			FixedSimulationSystemGroup fixedUpdateGroup = World.Active.GetExistingSystem<FixedSimulationSystemGroup>();
			fixedUpdateGroup.Enabled = true;
			fixedUpdateGroup.Update();
			fixedUpdateGroup.Enabled = false;
		}
	}
}
