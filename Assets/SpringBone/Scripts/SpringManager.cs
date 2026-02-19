//
//SpingManager.cs for unity-chan!
//
//Original Script is here:
//ricopin / SpingManager.cs
//Rocket Jump : http://rocketjump.skr.jp/unity3d/109/
//https://twitter.com/ricopin416
//
//Revised by N.Kobayashi 2014/06/24
//           Y.Ebata
//
using UnityEngine;

namespace DynamicBone
{
	public class SpringManager : MonoBehaviour
	{
		//Kobayashi
		// DynamicRatio is paramater for activated level of dynamic animation 
		public float dynamicRatio = 1.0f;

		//Ebata
		public float stiffnessForce;
		public AnimationCurve stiffnessCurve;
		public float dragForce;
		public AnimationCurve dragCurve;
		public SpringBone[] springBones;

		void Start()
		{
			UpdateParameters();
		}

		void Update()
		{
#if UNITY_EDITOR
			//Kobayashi
			if (dynamicRatio >= 1.0f)
				dynamicRatio = 1.0f;
			else if (dynamicRatio <= 0.0f)
				dynamicRatio = 0.0f;
			//Ebata
			UpdateParameters();
#endif
		}

		void LateUpdate()
		{
			//Kobayashi
			if (dynamicRatio != 0.0f)
			{
				for (int i = 0; i < springBones.Length; i++)
				{
					if (dynamicRatio > springBones[i].threshold)
					{
						springBones[i].UpdateSpring();
					}
				}
			}
		}

		public void UpdateParameters()
		{
			for (int i = 0; i < springBones.Length; i++)
			{
				if (false  == springBones[i].enabled
					|| springBones[i].isUseEachBoneForceSettings)
				{
					continue;
				}

				float stiffstart = stiffnessCurve.keys[0].time;
				float stiffend = stiffnessCurve.keys[^1].time;
				float stiff = stiffnessCurve.Evaluate(stiffstart + (stiffend - stiffstart) * i / (springBones.Length - 1));
				springBones[i].stiffnessForce = stiffnessForce * stiff;

				float dragstart = dragCurve.keys[0].time;
				float dragend = dragCurve.keys[^1].time;
				float drag = dragCurve.Evaluate(dragstart + (dragend - dragstart) * i / (springBones.Length - 1));
				springBones[i].dragForce = dragForce * drag;
			}
		}

		public SpringManager Copy(GameObject target)
		{
			if (null == target)
			{
				return null;
			}

			var mng = target.AddComponent<SpringManager>();
			mng.dynamicRatio = dynamicRatio;
			mng.stiffnessForce = stiffnessForce;
			mng.stiffnessCurve = new();
			mng.stiffnessCurve.CopyFrom(stiffnessCurve);
			mng.dragForce = dragForce;
			mng.dragForce = new();
			mng.dragCurve.CopyFrom(dragCurve);
			mng.springBones = new SpringBone[springBones.Length];

			for (int i = 0; i < mng.springBones.Length; i++)
			{
				var path = springBones[i].transform.GetPath();
				var child = target.transform.Find(path);
				if (null == child)
				{
					Debug.LogWarning($"Can't find springbone {path}");
					continue;
				}
				mng.springBones[i] = springBones[i].Copy(child.gameObject);
				var colliders = mng.springBones[i].colliders;
				for (int j = 0; j < colliders.Length; j++)
				{
					path = springBones[i].colliders[j].transform.GetPath(transform);
					child = mng.springBones[i].colliders[j].transform.Find(path);
					if (null == child)
					{
						Debug.LogWarning($"Can't find springcollider {path}");
						continue;
					}
					mng.springBones[i].colliders[j] = child.gameObject.AddComponent<SpringCollider>();
					mng.springBones[i].colliders[j].radius = springBones[i].colliders[j].radius;
				}
			}
			return mng;
		}
	}
}