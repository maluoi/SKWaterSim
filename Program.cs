using StereoKit;
using StereoKit.Framework;

class Program
{
	static void Main(string[] args)
	{
		if (!SK.Initialize(new SKSettings {
			depthMode = DepthMode.D32
			}))
			return;

		// Set up some envionment
		Matrix   floorTransform = Matrix.TS(0, -1.5f, 0, new Vec3(30, 0.1f, 30));
		Material floorMaterial  = new Material("floor.hlsl");
		floorMaterial.Transparency = Transparency.Blend;

		Renderer.SkyTex = Tex.FromCubemap("kloppenheim_06_puresky.ktx2");

		// Generate some terrain height values for the water sim to flow on
		int     width  = 64;
		int     height = 64;
		float[] heights = new float[width * height];
		for (int y = 0; y < height; y++)
		{
			float yp = y / (float)height;
			for (int x = 0; x < width; x++)
			{
				float xp = x/(float)width;
				heights[x+y*width] = Noise.Perlin2DTiledN(V.XY(xp, yp), 8)*100;
			}
		}
		// And our shallow water simulation object
		WaterSim sim = new(width, height, 1, heights);

		// A mesh that we'll use to draw terrain values with
		Mesh plane = Mesh.GeneratePlane(Vec2.One, 256);

		// Materials for drawing land and water
		var water = new MaterialWaterSim {
			Diffuse      = sim.tex,
			HeightMask   = new Vec4(0.99f,1.01f,0,0),
			ColorMask    = new Vec4(0,1,0,0),
			Transparency = Transparency.Blend,
			ColorA       = Color.HSV(0.66f, 0.6f, 0.3f, 0.5f),
			ColorB       = Color.HSV(0.6f,  0.5f, 0.7f, 0.5f),
		};
		var land = new MaterialWaterSim {
			HeightMask   = new Vec4(1,0,0,0),
			ColorMask    = new Vec4(1,0,0,0),
			Diffuse      = sim.tex,
			ColorA       = Color.HSV(0.3f,  0.6f, 0.3f),
			ColorB       = Color.HSV(0.36f, 0.5f, 0.7f),
		};

		// UI variables
		Pose  settingsPose = new Pose(0,0,-0.5f, Quat.LookDir(0,0,1));
		float pour   = 10;
		float step   = 0.01f;
		Vec3  pourPt = Vec3.Zero;

		// Core application loop
		SK.Run(() => {
			if (Device.DisplayBlend == DisplayBlend.Opaque)
				Mesh.Cube.Draw(floorMaterial, floorTransform);

			// Step the shallow water simulation
			sim.Step(step);

			// Draw the land and water
			Vec3   size = new Vec3(1,0.1f,1)*1.5f;
			Matrix t    = Matrix.TS(0, -0.5f, 0, size);
			plane.Draw(land, t);
			plane.Draw(water,t);

			// Add a UI to control simulation parameters
			Vec2 sz = new Vec2(0.07f,0);
			UI.WindowBegin("Shallow Water Sim", ref settingsPose, new Vec2(0.2f,0));
			UI.Label("Rain", sz);
			UI.SameLine();
			UI.HSlider("rain", ref sim.rain, 0, 10);

			UI.Label("Pour", sz);
			UI.SameLine();
			UI.HSlider("pour", ref pour, 1, 100);

			UI.HSeparator();

			UI.Label("Friction", sz);
			UI.SameLine();
			UI.HSlider("friction", ref sim.friction, 0, 1);

			UI.Label("Step", sz);
			UI.SameLine();
			UI.HSlider("step", ref step, 0.0001f, 0.05f);

			if (UI.Button("Clear Water")) sim.ResetWater();

			UI.WindowEnd();

			// Allow the user to pour water onto the simulation with either hand
			for (int i = 0; i < 2; i++)
			{
				if (UI.IsInteracting((Handed)i)) continue;
				Hand h = Input.Hand((Handed)i);

				if (h.IsPinched)
				{
					if (h.IsJustPinched)
						pourPt = h.pinchPt;

					Vec3 localPinchPt = t.Inverse.Transform(h.pinchPt);
					int x = (int)(localPinchPt.x * sim.Width ) + sim.Width/2;
					int y = (int)(localPinchPt.z * sim.Height) + sim.Height/2;

					Vec3  localPourPt = localPinchPt.X0Z;
					Color col         = new Color(1,0,0,1);
					if (x>=0 && x<width && y>=0 && y<height)
					{
						sim.AddWater(x,y, pour);
						
						localPourPt.y = sim.cells[x+y*sim.Width].height*0.01f;
						col = Color.HSV(0.6f, 0.7f, 0.7f);
					}
					Vec3 worldPourPt = t.Transform(localPourPt);
					pourPt = Vec3.Lerp(pourPt, worldPourPt, 16*Time.Stepf);

					Lines.Add([
						new LinePoint(h.pinchPt, col, 0f),
						new LinePoint(pourPt, new Color(col.r, col.g, col.b,0), 0.01f), ]);
				}
			}
		});
	}
}