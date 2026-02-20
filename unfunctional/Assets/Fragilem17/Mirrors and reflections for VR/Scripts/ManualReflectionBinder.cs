using UnityEngine;

namespace Fragilem17.MirrorsAndPortals
{
	[ExecuteAlways]
	public class ManualReflectionBinder : MonoBehaviour
	{
		public ReflectionProbe probe;

		private static readonly int SpecCube0 = Shader.PropertyToID("unity_SpecCube0");
		private static readonly int SpecCube1 = Shader.PropertyToID("unity_SpecCube1");
		private static readonly int SpecCube0_HDR = Shader.PropertyToID("unity_SpecCube0_HDR");

		private static readonly int ProbePos = Shader.PropertyToID("unity_SpecCube0_ProbePosition");
		private static readonly int BoxMin = Shader.PropertyToID("unity_SpecCube0_BoxMin");
		private static readonly int BoxMax = Shader.PropertyToID("unity_SpecCube0_BoxMax");

		private Renderer _renderer;
		private MaterialPropertyBlock _mpb;

		private void OnEnable()
		{
			_renderer = GetComponent<Renderer>();
			_mpb = new MaterialPropertyBlock();
			UpdateProbe();
		}

		void Update()
		{
			UpdateProbe();
		}

		void UpdateProbe()
		{
			if (!probe || !_renderer || !probe.texture) return;

			_renderer.GetPropertyBlock(_mpb);

			_mpb.SetTexture(SpecCube0, probe.texture);
			_mpb.SetTexture(SpecCube1, probe.texture);
			_mpb.SetVector(SpecCube0_HDR, probe.textureHDRDecodeValues);

			// Apply offset and scale
			//Vector3 scale = probe.transform.lossyScale;
			Vector4 boxCenter = probe.transform.position; // probe.center;
			Vector4 boxSize = probe.size;

			Vector4 boxMin = boxCenter + ((Vector4)probe.center) - boxSize * .5f;
			Vector4 boxMax = boxCenter + ((Vector4)probe.center) + boxSize * .5f;

			boxCenter.w = 1;
			boxMin.w = 1;
			boxMax.w = 1;

			_mpb.SetVector(ProbePos, boxCenter);
			//_mpb.SetVector(BoxMin, boxMin);
			//_mpb.SetVector(BoxMax, boxMax);
			_mpb.SetVector(BoxMin, boxMin);
			_mpb.SetVector(BoxMax, boxMax);

			_renderer.SetPropertyBlock(_mpb);
		}
	}
}
